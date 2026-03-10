namespace DHPlugin.Abstractions;

/// <summary>
/// 跨平台 C++ 编译器服务接口，由宿主程序注册，插件通过 IPluginContext.GetService 获取。
/// </summary>
public interface ICompilerService
{
    /// <summary>编译器名称（如 "Zig"、"GCC"）</summary>
    string CompilerName { get; }

    /// <summary>编译器是否在当前系统上可用</summary>
    bool IsAvailable { get; }

    /// <summary>
    /// 将 C++ 源文件编译为可执行文件。
    /// </summary>
    /// <param name="sourceFile">输入的 .cpp 源文件完整路径</param>
    /// <param name="outputFile">输出的可执行文件完整路径</param>
    /// <param name="extraArgs">传递给编译器的额外参数（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<CppCompileResult> CompileCppAsync(
        string sourceFile,
        string outputFile,
        IEnumerable<string>? extraArgs = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// C++ 编译结果
/// </summary>
public class CppCompileResult
{
    public bool Success { get; set; }
    public string? ExecutablePath { get; set; }
    public string Output { get; set; } = "";
    public string ErrorOutput { get; set; } = "";
    public int ExitCode { get; set; }
}
