using System.Diagnostics;
using System.IO;
using System.Text;
using DHPlugin.Abstractions;

namespace CPlusPlusEditor;

public class GccCompiler
{
    private readonly IPluginContext _context;
    private readonly ILogger _logger;
    
    public GccCompiler(IPluginContext context)
    {
        _context = context;
        _logger = context.GetLogger();
    }

    public async Task<CompileResult> CompileAsync(string code, CancellationToken cancellationToken = default)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "DHPlugin", "CppEditor", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var sourceFile = Path.Combine(tempDir, "main.cpp");
        var executableFile = OperatingSystem.IsWindows() 
            ? Path.Combine(tempDir, "main.exe") 
            : Path.Combine(tempDir, "main");

        try
        {
            // 写入源代码
            await File.WriteAllTextAsync(sourceFile, code, cancellationToken);

            // 优先尝试宿主注册的 ICompilerService（如 Zig 编译器）
            var compilerService = _context.GetService<ICompilerService>();
            if (compilerService != null && compilerService.IsAvailable)
            {
                _logger.Info($"使用宿主编译器服务：{compilerService.CompilerName}");
                var svcResult = await compilerService.CompileCppAsync(
                    sourceFile, executableFile, cancellationToken: cancellationToken);

                return new CompileResult
                {
                    Success = svcResult.Success,
                    ExecutablePath = svcResult.ExecutablePath,
                    Output = svcResult.Output,
                    ErrorOutput = svcResult.ErrorOutput,
                    ExitCode = svcResult.ExitCode,
                    TempDirectory = tempDir
                };
            }

            // 回退到系统 GCC / Clang
            var compilerPath = FindCompiler();
            if (string.IsNullOrEmpty(compilerPath))
            {
                return new CompileResult
                {
                    Success = false,
                    ErrorOutput = "未找到编译器。请安装 Zig（推荐）或 g++，并确保其在 PATH 中。"
                };
            }

            _logger.Info($"使用编译器：{compilerPath}");

            // 编译参数
            var arguments = $"-std=c++17 -o \"{executableFile}\" \"{sourceFile}\"";
            
            _logger.Info($"编译命令：{compilerPath} {arguments}");

            // 执行编译
            var startInfo = new ProcessStartInfo
            {
                FileName = compilerPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = startInfo };
            
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    outputBuilder.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await Task.Run(() =>
            {
                process.WaitForExit();
            }, cancellationToken);

            var success = process.ExitCode == 0;
            
            return new CompileResult
            {
                Success = success,
                ExecutablePath = success ? executableFile : null,
                Output = outputBuilder.ToString(),
                ErrorOutput = errorBuilder.ToString(),
                ExitCode = process.ExitCode,
                TempDirectory = tempDir
            };
        }
        catch (Exception ex)
        {
            _logger.Error("编译过程异常", ex);
            return new CompileResult
            {
                Success = false,
                ErrorOutput = $"编译异常：{ex.Message}"
            };
        }
    }

    public async Task<RunResult> RunAsync(string executablePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(executablePath))
        {
            return new RunResult
            {
                ExitCode = -1,
                Error = "可执行文件不存在"
            };
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = startInfo };
            
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    outputBuilder.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await Task.Run(() =>
            {
                process.WaitForExit();
            }, cancellationToken);

            return new RunResult
            {
                ExitCode = process.ExitCode,
                Output = outputBuilder.ToString(),
                Error = errorBuilder.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.Error("运行过程异常", ex);
            return new RunResult
            {
                ExitCode = -1,
                Error = $"运行异常：{ex.Message}"
            };
        }
    }

    private string? FindCompiler()
    {
        // 尝试常见的编译器名称
        var compilerNames = OperatingSystem.IsWindows()
            ? new[] { "g++.exe", "c++.exe", "clang++.exe" }
            : new[] { "g++", "c++", "clang++" };

        foreach (var name in compilerNames)
        {
            var path = FindInPath(name);
            if (!string.IsNullOrEmpty(path))
            {
                return path;
            }
        }

        // 在 macOS 上尝试 Xcode 命令行工具
        if (OperatingSystem.IsMacOS())
        {
            var xcodePath = "/usr/bin/clang++";
            if (File.Exists(xcodePath))
            {
                return xcodePath;
            }
        }

        // 在 Linux 上尝试常见路径（包括 ARM64）
        if (OperatingSystem.IsLinux())
        {
            var linuxPaths = new[]
            {
                // 通用路径
                "/usr/bin/g++",
                "/usr/local/bin/g++",
                "/usr/bin/clang++",
                "/usr/local/bin/clang++",
                
                // ARM64 特定路径（树莓派等）
                "/usr/bin/aarch64-linux-gnu-g++",
                "/usr/bin/arm-linux-gnueabihf-g++",
                
                // 其他常见位置
                "/opt/gcc/bin/g++",
                "/usr/lib64/gcc/*/bin/g++"
            };

            foreach (var path in linuxPaths)
            {
                // 处理通配符路径
                if (path.Contains("*"))
                {
                    var dir = Path.GetDirectoryName(path);
                    var pattern = Path.GetFileName(path);
                    if (Directory.Exists(dir))
                    {
                        var dirs = Directory.GetDirectories(dir, pattern);
                        foreach (var d in dirs)
                        {
                            var fullPath = Path.Combine(d, "g++");
                            if (File.Exists(fullPath))
                                return fullPath;
                        }
                    }
                }
                else if (File.Exists(path))
                {
                    return path;
                }
            }
        }

        return null;
    }

    private string? FindInPath(string fileName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv)) return null;

        foreach (var path in pathEnv.Split(Path.PathSeparator))
        {
            var fullPath = Path.Combine(path.Trim(), fileName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }
}

public class CompileResult
{
    public bool Success { get; set; }
    public string? ExecutablePath { get; set; }
    public string Output { get; set; } = "";
    public string ErrorOutput { get; set; } = "";
    public int ExitCode { get; set; }
    public string? TempDirectory { get; set; }
}

public class RunResult
{
    public int ExitCode { get; set; }
    public string Output { get; set; } = "";
    public string Error { get; set; } = "";
}
