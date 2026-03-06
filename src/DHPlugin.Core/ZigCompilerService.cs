using System.Diagnostics;
using System.Text;
using DHPlugin.Abstractions;

namespace DHPlugin.Core;

/// <summary>
/// 使用 Zig 工具链（zig c++）编译 C++ 代码的服务。
/// Zig 内置了完整的 C/C++ 工具链，无需额外安装 GCC 或 Clang，
/// 并天然支持 win-x64、osx-arm64、linux-x64、linux-arm64 四个目标平台。
/// </summary>
public sealed class ZigCompilerService : ICompilerService
{
    private readonly ILogger _logger;
    private readonly string? _zigExecutable;

    public string CompilerName => "Zig";

    public bool IsAvailable => _zigExecutable != null;

    public ZigCompilerService(ILogger logger)
    {
        _logger = logger;
        _zigExecutable = FindZig();

        if (_zigExecutable != null)
            _logger.Info($"ZigCompilerService: 找到 zig 可执行文件 → {_zigExecutable}");
        else
            _logger.Info("ZigCompilerService: 未找到 zig，将由 GCC 兜底");
    }

    /// <inheritdoc />
    public async Task<CppCompileResult> CompileCppAsync(
        string sourceFile,
        string outputFile,
        IEnumerable<string>? extraArgs = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return new CppCompileResult
            {
                Success = false,
                ErrorOutput = "Zig 编译器不可用"
            };
        }

        // zig c++ -std=c++17 -o <output> <source> [extraArgs]
        // Use ArgumentList (not Arguments string) to avoid path-based command injection.
        var startInfo = new ProcessStartInfo
        {
            FileName = _zigExecutable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        startInfo.ArgumentList.Add("c++");
        startInfo.ArgumentList.Add("-std=c++17");
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add(outputFile);
        startInfo.ArgumentList.Add(sourceFile);
        if (extraArgs != null)
        {
            foreach (var arg in extraArgs)
                startInfo.ArgumentList.Add(arg);
        }

        _logger.Info($"ZigCompilerService: {_zigExecutable} c++ -std=c++17 -o \"{outputFile}\" \"{sourceFile}\"");

        try
        {
            using var process = new Process { StartInfo = startInfo };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // WaitForExitAsync respects cancellation; killing the process on cancellation.
            // A subsequent synchronous WaitForExit() (no timeout) ensures all async
            // stdout/stderr data has been flushed before we read the builders.
            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* best-effort */ }
                throw;
            }

            // Flush any remaining buffered output.
            process.WaitForExit();

            var success = process.ExitCode == 0;
            return new CppCompileResult
            {
                Success = success,
                ExecutablePath = success ? outputFile : null,
                Output = outputBuilder.ToString(),
                ErrorOutput = errorBuilder.ToString(),
                ExitCode = process.ExitCode
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error("ZigCompilerService: 编译过程异常", ex);
            return new CppCompileResult
            {
                Success = false,
                ErrorOutput = $"编译异常：{ex.Message}"
            };
        }
    }

    // ── 查找 zig 可执行文件 ──────────────────────────────────────────────────

    private string? FindZig()
    {
        var zigName = OperatingSystem.IsWindows() ? "zig.exe" : "zig";

        // 1. 从 PATH 中查找
        var fromPath = FindInPath(zigName);
        if (fromPath != null) return fromPath;

        // 2. 常见安装路径（按平台）
        var candidates = GetPlatformCandidates(zigName);
        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    private static string? FindInPath(string fileName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv)) return null;

        foreach (var dir in pathEnv.Split(Path.PathSeparator))
        {
            var full = Path.Combine(dir.Trim(), fileName);
            if (File.Exists(full))
                return full;
        }
        return null;
    }

    private static IEnumerable<string> GetPlatformCandidates(string zigName)
    {
        if (OperatingSystem.IsWindows())
        {
            return new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "zig", zigName),
                @"C:\zig\" + zigName,
                @"C:\Program Files\zig\" + zigName,
                @"C:\tools\zig\" + zigName
            };
        }

        if (OperatingSystem.IsMacOS())
        {
            return new[]
            {
                "/usr/local/bin/zig",
                "/opt/homebrew/bin/zig",
                "/opt/zig/zig",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".local", "bin", "zig")
            };
        }

        // Linux (x64 or arm64)
        return new[]
        {
            "/usr/local/bin/zig",
            "/usr/bin/zig",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local", "bin", "zig"),
            "/opt/zig/zig"
        };
    }
}
