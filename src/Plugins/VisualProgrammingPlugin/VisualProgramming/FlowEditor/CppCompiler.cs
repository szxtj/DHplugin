using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VisualProgramming.FlowEditor
{
    /// <summary>
    /// C++代码编译器
    /// </summary>
    public class CppCompiler
    {
        private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        private static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        private static string DetectToolchainSubDir()
        {
            if (IsWindows) return "mingw64";
            if (IsLinux)
            {
                return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64";
            }
            if (IsMacOS) return "osx-arm64";
            return "";
        }

        private static string? ResolveBundledToolchainBinDir()
        {
            try
            {
                var env = Environment.GetEnvironmentVariable("DH_CPP_TOOLCHAIN_BIN");
                if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env)) return env;

                var baseDir = VisualProgramming.PluginRuntimeEnvironment.PluginRootDirectory;
                var toolchainSubDir = DetectToolchainSubDir();
                var candidates = new[]
                {
                    Path.Combine(baseDir, "Toolchains", toolchainSubDir, "bin"),
                    Path.Combine(baseDir, "toolchains", toolchainSubDir, "bin"),
                    Path.Combine(baseDir, "ThirdParty", toolchainSubDir, "bin"),
                    Path.Combine(baseDir, "third_party", toolchainSubDir, "bin"),
                    // Dev environment fallback: search up to project root relative to bin/Debug/net8.0
                    Path.GetFullPath(Path.Combine(baseDir, $"../../../../dist/windows/Toolchains/{toolchainSubDir}/bin")),
                    Path.GetFullPath(Path.Combine(baseDir, $"../../../../dist/windows7/Toolchains/{toolchainSubDir}/bin")),
                };
                foreach (var c in candidates)
                {
                    if (!string.IsNullOrWhiteSpace(toolchainSubDir) && Directory.Exists(c)) return c;
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// 在 PATH 中查找可执行文件的完整路径
        /// </summary>
        private static string? FindInPath(string fileName)
        {
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathEnv)) return null;

            foreach (var dir in pathEnv.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(dir.Trim(), fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }

        /// <summary>
        /// 在系统中查找可用的 C++ 编译器（参照 GccCompiler 的多路径探测策略）
        /// </summary>
        private static string? FindSystemCompiler()
        {
            // 尝试常见编译器名称（PATH 查找）
            var compilerNames = IsWindows
                ? new[] { "g++.exe", "clang++.exe", "c++.exe" }
                : new[] { "g++", "clang++", "c++" };

            foreach (var name in compilerNames)
            {
                var path = FindInPath(name);
                if (path != null) return path;
            }

            // macOS：尝试 Xcode 命令行工具
            if (IsMacOS)
            {
                if (File.Exists("/usr/bin/clang++")) return "/usr/bin/clang++";
                if (File.Exists("/usr/bin/g++")) return "/usr/bin/g++";
            }

            // Linux：尝试常见安装路径（包括 ARM64）
            if (IsLinux)
            {
                var linuxPaths = new[]
                {
                    "/usr/bin/g++",
                    "/usr/local/bin/g++",
                    "/usr/bin/clang++",
                    "/usr/local/bin/clang++",
                    // ARM64 交叉编译器
                    "/usr/bin/aarch64-linux-gnu-g++",
                    "/usr/bin/arm-linux-gnueabihf-g++",
                    "/opt/gcc/bin/g++",
                };
                foreach (var p in linuxPaths)
                {
                    if (File.Exists(p)) return p;
                }
                // 通配符路径：/usr/lib64/gcc/*/bin/g++
                var gccLibDir = "/usr/lib64/gcc";
                if (Directory.Exists(gccLibDir))
                {
                    foreach (var d in Directory.GetDirectories(gccLibDir))
                    {
                        var candidate = Path.Combine(d, "bin", "g++");
                        if (File.Exists(candidate)) return candidate;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 解析编译器可执行文件路径：优先使用捆绑工具链，其次搜索系统路径
        /// </summary>
        private static string ResolveCompilerExecutable(string compilerName)
        {
            // 对于 g++ / clang++ 先尝试捆绑工具链
            if (string.Equals(compilerName, "g++", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(compilerName, "clang++", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var bin = ResolveBundledToolchainBinDir();
                    if (!string.IsNullOrWhiteSpace(bin))
                    {
                        var executableName = IsWindows ? compilerName + ".exe" : compilerName;
                        var path = Path.Combine(bin!, executableName);
                        if (File.Exists(path)) return path;
                    }
                }
                catch { }

                // 捆绑工具链未找到时，搜索系统
                var systemPath = FindSystemCompiler();
                if (systemPath != null) return systemPath;
            }

            // 对于其他编译器（如 cl），直接用名称，由 PATH 解析
            return compilerName;
        }

        private static void ApplyToolchainEnvironment(ProcessStartInfo startInfo)
        {
            try
            {
                var bin = ResolveBundledToolchainBinDir();
                if (string.IsNullOrWhiteSpace(bin)) return;

                var existing = startInfo.Environment.ContainsKey("PATH")
                    ? startInfo.Environment["PATH"]
                    : (Environment.GetEnvironmentVariable("PATH") ?? string.Empty);

                if (existing?.IndexOf(bin, StringComparison.OrdinalIgnoreCase) >= 0) return;
                startInfo.Environment["PATH"] = bin + Path.PathSeparator + existing;
            }
            catch { }
        }

        private static string GetSharedLibraryCompileArgs(string cppSourcePath, string outputDllPath)
        {
            if (IsWindows)
            {
                return $"-shared -O2 -m64 -static-libgcc -static-libstdc++ \"{cppSourcePath}\" -o \"{outputDllPath}\" -std=c++17";
            }
            return $"-shared -fPIC -O2 \"{cppSourcePath}\" -o \"{outputDllPath}\" -std=c++17";
        }

        private static string GetExecutableCompileArgs(string cppSourcePath, string outputExePath)
        {
            if (IsWindows)
            {
                return $"-O2 -m64 -static-libgcc -static-libstdc++ \"{cppSourcePath}\" -o \"{outputExePath}\" -std=c++17";
            }
            return $"-O2 \"{cppSourcePath}\" -o \"{outputExePath}\" -std=c++17";
        }

        /// <summary>
        /// 封装进程执行：使用异步事件读取 stdout/stderr，避免缓冲区死锁；统一设置 UTF-8 编码
        /// </summary>
        private static async Task<(int ExitCode, string Output, string Error)> RunProcessAsync(
            ProcessStartInfo startInfo, CancellationToken cancellationToken = default)
        {
            startInfo.StandardOutputEncoding = Encoding.UTF8;
            startInfo.StandardErrorEncoding = Encoding.UTF8;

            using var process = new Process { StartInfo = startInfo };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) outputBuilder.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await Task.Run(() => process.WaitForExit(), cancellationToken);

            return (process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
        }

        /// <summary>
        /// 编译C++代码为动态库
        /// </summary>
        public static async Task<bool> CompileToDll(string cppSourcePath, string outputDllPath,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(cppSourcePath))
                {
                    Console.WriteLine($"C++源文件不存在: {cppSourcePath}");
                    return false;
                }

                var sourceDir = Path.GetDirectoryName(cppSourcePath) ?? Directory.GetCurrentDirectory();

                var startInfo = new ProcessStartInfo
                {
                    FileName = ResolveCompilerExecutable("g++"),
                    Arguments = GetSharedLibraryCompileArgs(cppSourcePath, outputDllPath),
                    WorkingDirectory = sourceDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                ApplyToolchainEnvironment(startInfo);

                var (exitCode, output, error) = await RunProcessAsync(startInfo, cancellationToken);

                if (exitCode == 0)
                {
                    Console.WriteLine("C++代码编译成功!");
                    if (!string.IsNullOrWhiteSpace(output)) Console.WriteLine(output);
                    return true;
                }
                else
                {
                    Console.WriteLine($"C++代码编译失败，退出代码: {exitCode}");
                    Console.WriteLine(error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"编译过程中出现异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 编译C++代码为可执行文件 (EXE)
        /// </summary>
        public static async Task<bool> CompileToExe(string cppSourcePath, string outputExePath,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(cppSourcePath)) return false;

                var sourceDir = Path.GetDirectoryName(cppSourcePath) ?? Directory.GetCurrentDirectory();

                var startInfo = new ProcessStartInfo
                {
                    FileName = ResolveCompilerExecutable("g++"),
                    Arguments = GetExecutableCompileArgs(cppSourcePath, outputExePath),
                    WorkingDirectory = sourceDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                ApplyToolchainEnvironment(startInfo);

                var (exitCode, _, error) = await RunProcessAsync(startInfo, cancellationToken);

                if (exitCode == 0)
                {
                    Console.WriteLine("EXE编译成功!");
                    return true;
                }
                else
                {
                    Console.WriteLine($"EXE编译失败: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXE编译异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 使用Visual Studio编译器编译为动态库（Windows）
        /// </summary>
        public static async Task<bool> CompileToDllWithMSVC(string cppSourcePath, string outputDllPath,
            CancellationToken cancellationToken = default)
        {
            if (!IsWindows)
            {
                Console.WriteLine("当前系统非 Windows，跳过 MSVC 编译器");
                return false;
            }

            try
            {
                if (!File.Exists(cppSourcePath))
                {
                    Console.WriteLine($"C++源文件不存在: {cppSourcePath}");
                    return false;
                }

                var sourceDir = Path.GetDirectoryName(cppSourcePath) ?? Directory.GetCurrentDirectory();

                var startInfo = new ProcessStartInfo
                {
                    FileName = ResolveCompilerExecutable("cl"),
                    Arguments = $"/LD \"{cppSourcePath}\" /Fe:\"{outputDllPath}\" /EHsc /std:c++17 /arch:AVX2",
                    WorkingDirectory = sourceDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                ApplyToolchainEnvironment(startInfo);

                var (exitCode, output, error) = await RunProcessAsync(startInfo, cancellationToken);

                if (exitCode == 0 || error.Contains("LINK : warning LNK4044:"))
                {
                    Console.WriteLine("C++代码编译成功!");
                    if (!string.IsNullOrWhiteSpace(output)) Console.WriteLine(output);
                    return true;
                }
                else
                {
                    Console.WriteLine($"C++代码编译失败，退出代码: {exitCode}");
                    Console.WriteLine(error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"编译过程中出现异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 使用Visual Studio编译器编译为EXE（Windows）
        /// </summary>
        public static async Task<bool> CompileToExeWithMSVC(string cppSourcePath, string outputExePath,
            CancellationToken cancellationToken = default)
        {
            if (!IsWindows)
            {
                Console.WriteLine("当前系统非 Windows，跳过 MSVC 编译器");
                return false;
            }

            try
            {
                if (!File.Exists(cppSourcePath)) return false;

                var sourceDir = Path.GetDirectoryName(cppSourcePath) ?? Directory.GetCurrentDirectory();

                var startInfo = new ProcessStartInfo
                {
                    FileName = ResolveCompilerExecutable("cl"),
                    // 不含 /LD，编译为普通 EXE
                    Arguments = $"\"{cppSourcePath}\" /Fe:\"{outputExePath}\" /EHsc /std:c++17 /arch:AVX2",
                    WorkingDirectory = sourceDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                ApplyToolchainEnvironment(startInfo);

                var (exitCode, _, error) = await RunProcessAsync(startInfo, cancellationToken);

                if (exitCode == 0 || error.Contains("LINK : warning LNK4044:"))
                {
                    Console.WriteLine("EXE编译成功!");
                    return true;
                }
                else
                {
                    Console.WriteLine($"EXE编译失败: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXE编译异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 自动检测可用编译器并编译为EXE（MSVC → g++ / clang++）
        /// </summary>
        public static async Task<bool> CompileToExeAuto(string cppSourcePath, string outputExePath,
            CancellationToken cancellationToken = default)
        {
            if (IsWindows && await IsCompilerAvailable("cl"))
            {
                return await CompileToExeWithMSVC(cppSourcePath, outputExePath, cancellationToken);
            }

            var gpp = FindSystemCompiler();
            if (!string.IsNullOrEmpty(gpp))
            {
                Console.WriteLine($"使用编译器: {gpp}");
                return await CompileToExe(cppSourcePath, outputExePath, cancellationToken);
            }

            Console.WriteLine("未找到可用的C++编译器");
            return false;
        }

        /// <summary>
        /// 自动检测可用编译器并编译为动态库（MSVC → g++ / clang++）
        /// </summary>
        public static async Task<bool> CompileToDllAuto(string cppSourcePath, string outputDllPath,
            CancellationToken cancellationToken = default)
        {
            if (IsWindows && await IsCompilerAvailable("cl"))
            {
                Console.WriteLine("检测到Visual Studio编译器，使用MSVC编译...");
                return await CompileToDllWithMSVC(cppSourcePath, outputDllPath, cancellationToken);
            }

            var gpp = FindSystemCompiler();
            if (!string.IsNullOrEmpty(gpp))
            {
                Console.WriteLine($"使用编译器: {gpp}");
                return await CompileToDll(cppSourcePath, outputDllPath, cancellationToken);
            }

            Console.WriteLine("未找到可用的C++编译器 (需要安装Visual Studio、g++ 或 clang++)");
            return false;
        }

        /// <summary>
        /// 检查指定编译器是否可用
        /// </summary>
        public static async Task<bool> IsCompilerAvailable(string compilerName)
        {
            try
            {
                if (!IsWindows && string.Equals(compilerName, "cl", StringComparison.OrdinalIgnoreCase))
                    return false;

                Console.WriteLine($"正在检测编译器: {compilerName}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = ResolveCompilerExecutable(compilerName),
                    Arguments = compilerName == "cl" ? "/?" : "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                ApplyToolchainEnvironment(startInfo);

                var (exitCode, output, error) = await RunProcessAsync(startInfo);

                Console.WriteLine($"{compilerName} 检测结果 - 退出码: {exitCode}");
                if (!string.IsNullOrWhiteSpace(output)) Console.WriteLine($"{compilerName} 输出: {output}");
                if (!string.IsNullOrWhiteSpace(error)) Console.WriteLine($"{compilerName} 错误: {error}");

                // cl 只要能启动即视为可用（帮助信息输出到 stderr，退出码非 0）
                return compilerName == "cl" ? exitCode >= 0 : exitCode == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检测编译器 {compilerName} 时发生异常: {ex.Message}");
                return false;
            }
        }
    }
}