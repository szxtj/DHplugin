using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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

        private static string ResolveCompilerExecutable(string compilerName)
        {
            try
            {
                if (string.Equals(compilerName, "g++", StringComparison.OrdinalIgnoreCase))
                {
                    var bin = ResolveBundledToolchainBinDir();
                    if (!string.IsNullOrWhiteSpace(bin))
                    {
                        var executableName = IsWindows ? "g++.exe" : "g++";
                        var path = Path.Combine(bin, executableName);
                        if (File.Exists(path)) return path;
                    }
                }
            }
            catch { }
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
        /// 编译C++代码为动态库
        /// </summary>
        /// <param name="cppSourcePath">C++源文件路径</param>
        /// <param name="outputDllPath">输出DLL路径</param>
        /// <returns>编译是否成功</returns>
        public static async Task<bool> CompileToDll(string cppSourcePath, string outputDllPath)
        {
            try
            {
                // 检查源文件是否存在
                if (!File.Exists(cppSourcePath))
                {
                    Console.WriteLine($"C++源文件不存在: {cppSourcePath}");
                    return false;
                }

                // 获取源文件所在目录
                var sourceDir = Path.GetDirectoryName(cppSourcePath);
                var sourceFileName = Path.GetFileNameWithoutExtension(cppSourcePath);

                // 创建编译命令（这里假使用g++，实际应用中可能需要检测可用的编译器）
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

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();

                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine("C++代码编译成功!");
                        Console.WriteLine(output);
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"C++代码编译失败，退出代码: {process.ExitCode}");
                        Console.WriteLine(error);
                        return false;
                    }
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
        /// <param name="cppSourcePath">C++源文件路径</param>
        /// <param name="outputExePath">输出EXE路径</param>
        /// <returns>编译是否成功</returns>
        public static async Task<bool> CompileToExe(string cppSourcePath, string outputExePath)
        {
            try
            {
                if (!File.Exists(cppSourcePath)) return false;

                var sourceDir = Path.GetDirectoryName(cppSourcePath);

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

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXE编译异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 使用Visual Studio编译器编译（Windows）
        /// </summary>
        /// <param name="cppSourcePath">C++源文件路径</param>
        /// <param name="outputDllPath">输出DLL路径</param>
        /// <returns>编译是否成功</returns>
        public static async Task<bool> CompileToDllWithMSVC(string cppSourcePath, string outputDllPath)
        {
            if (!IsWindows)
            {
                Console.WriteLine("当前系统非 Windows，跳过 MSVC 编译器");
                return false;
            }

            try
            {
                // 检查源文件是否存在
                if (!File.Exists(cppSourcePath))
                {
                    Console.WriteLine($"C++源文件不存在: {cppSourcePath}");
                    return false;
                }

                // 获取源文件所在目录
                var sourceDir = Path.GetDirectoryName(cppSourcePath);
                var sourceFileName = Path.GetFileNameWithoutExtension(cppSourcePath);

                // 使用MSVC编译器编译（需要VS环境）
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

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();

                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0 || error.Contains("LINK : warning LNK4044:"))
                    {
                        Console.WriteLine("C++代码编译成功!");
                        Console.WriteLine(output);
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"C++代码编译失败，退出代码: {process.ExitCode}");
                        Console.WriteLine(error);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"编译过程中出现异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 使用Visual Studio编译器编译为EXE
        /// </summary>
        public static async Task<bool> CompileToExeWithMSVC(string cppSourcePath, string outputExePath)
        {
            if (!IsWindows)
            {
                Console.WriteLine("当前系统非 Windows，跳过 MSVC 编译器");
                return false;
            }

            try
            {
                if (!File.Exists(cppSourcePath)) return false;

                var sourceDir = Path.GetDirectoryName(cppSourcePath);

                var startInfo = new ProcessStartInfo
                {
                    FileName = ResolveCompilerExecutable("cl"),
                    // 移除 /LD，编译为普通 EXE
                    Arguments = $"\"{cppSourcePath}\" /Fe:\"{outputExePath}\" /EHsc /std:c++17 /arch:AVX2",
                    WorkingDirectory = sourceDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                ApplyToolchainEnvironment(startInfo);

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0 || error.Contains("LINK : warning LNK4044:"))
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXE编译异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 自动检测并编译为EXE
        /// </summary>
        public static async Task<bool> CompileToExeAuto(string cppSourcePath, string outputExePath)
        {
            if (await IsCompilerAvailable("cl"))
            {
                return await CompileToExeWithMSVC(cppSourcePath, outputExePath);
            }
            else if (await IsCompilerAvailable("g++"))
            {
                return await CompileToExe(cppSourcePath, outputExePath);
            }
            else
            {
                Console.WriteLine("未找到可用的C++编译器");
                return false;
            }
        }

        /// <summary>
        /// 自动检测并使用可用的编译器进行编译
        /// </summary>
        /// <param name="cppSourcePath">C++源文件路径</param>
        /// <param name="outputDllPath">输出DLL路径</param>
        /// <returns>编译是否成功</returns>
        public static async Task<bool> CompileToDllAuto(string cppSourcePath, string outputDllPath)
        {
            // 首先尝试使用MSVC (Visual Studio编译器)
            if (await IsCompilerAvailable("cl"))
            {
                Console.WriteLine("检测到Visual Studio编译器，使用MSVC编译...");
                return await CompileToDllWithMSVC(cppSourcePath, outputDllPath);
            }
            // 如果MSVC不可用，则尝试g++
            else if (await IsCompilerAvailable("g++"))
            {
                Console.WriteLine("检测到g++编译器，使用g++编译...");
                return await CompileToDll(cppSourcePath, outputDllPath);
            }
            else
            {
                Console.WriteLine("未找到可用的C++编译器 (需要安装Visual Studio或MinGW-w64)");
                return false;
            }
        }

        /// <summary>
        /// 检查编译器是否可用
        /// </summary>
        /// <param name="compilerName">编译器名称</param>
        /// <returns>编译器是否可用</returns>
        public static async Task<bool> IsCompilerAvailable(string compilerName)
        {
            try
            {
                if (!IsWindows && string.Equals(compilerName, "cl", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                Console.WriteLine($"正在检测编译器: {compilerName}");

                var compilerExe = ResolveCompilerExecutable(compilerName);

                var startInfo = new ProcessStartInfo
                {
                    FileName = compilerExe,
                    Arguments = compilerName == "cl" ? "/?" : "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                ApplyToolchainEnvironment(startInfo);

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    Console.WriteLine($"{compilerName} 检测结果 - 退出码: {process.ExitCode}");
                    Console.WriteLine($"{compilerName} 输出: {output}");
                    Console.WriteLine($"{compilerName} 错误: {error}");

                    // 对于 cl 编译器，只要能启动就说明存在
                    if (compilerName == "cl")
                    {
                        return process.ExitCode >= 0; // cl 启动成功（即使显示帮助）
                    }
                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检测编译器 {compilerName} 时发生异常: {ex.Message}");
                return false;
            }
        }
    }
}