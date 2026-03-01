using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Avalonia.Threading;
using DHPlugin.Abstractions;

namespace CPlusPlusEditor;

public class CppEditorViewModel : ViewModelBase, IDisposable
{
    private readonly IPluginContext _context;
    private readonly ILogger _logger;
    private string _code = DefaultCppCode;
    private string _output = "";
    private string _status = "就绪";
    private bool _isCompiling;
    private bool _isRunning;
    
    private const string DefaultCppCode = @"#include <iostream>

int main() {
    std::cout << ""Hello, DHPlugin!"" << std::endl;
    std::cout << ""Welcome to C++ Editor Plugin!"" << std::endl;
    
    // 尝试一些 C++ 特性
    auto message = ""Modern C++ is awesome!"";
    std::cout << message << std::endl;
    
    return 0;
}";

    public string Code
    {
        get => _code;
        set
        {
            _code = value;
            OnPropertyChanged();
        }
    }

    public string Output
    {
        get => _output;
        set
        {
            _output = value;
            OnPropertyChanged();
        }
    }

    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
        }
    }

    public bool IsCompiling
    {
        get => _isCompiling;
        set
        {
            _isCompiling = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCompile));
            OnPropertyChanged(nameof(CanRun));
        }
    }

    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            _isRunning = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCompile));
            OnPropertyChanged(nameof(CanRun));
        }
    }

    public bool CanCompile => !IsCompiling && !IsRunning;
    public bool CanRun => !IsCompiling && !IsRunning;

    public ICommand CompileCommand { get; }
    public ICommand RunCommand { get; }
    public ICommand ClearOutputCommand { get; }

    public CppEditorViewModel(IPluginContext context)
    {
        _context = context;
        _logger = context.GetLogger();
        _logger.Info("C++ 编辑器视图模型已创建");
        
        CompileCommand = new RelayCommand(async _ => await CompileAsync());
        RunCommand = new RelayCommand(async _ => await RunAsync());
        ClearOutputCommand = new RelayCommand(_ => ClearOutput());
    }

    public async Task CompileAsync()
    {
        if (IsCompiling || IsRunning) return;

        IsCompiling = true;
        Status = "正在编译...";
        Output = "编译中...\n";

        try
        {
            var compiler = new GccCompiler(_context);
            var result = await compiler.CompileAsync(Code);

            if (result.Success)
            {
                Output += $"✓ 编译成功!\n可执行文件：{result.ExecutablePath}\n";
                Status = "编译成功";
                _logger.Info($"C++ 代码编译成功：{result.ExecutablePath}");
            }
            else
            {
                Output += $"✗ 编译失败:\n{result.ErrorOutput}\n";
                Status = "编译失败";
                _logger.Warning($"C++ 代码编译失败：{result.ErrorOutput}");
            }
        }
        catch (Exception ex)
        {
            Output += $"✗ 编译错误：{ex.Message}\n";
            Status = "编译错误";
            _logger.Error("C++ 编译异常", ex);
        }
        finally
        {
            IsCompiling = false;
        }
    }

    public async Task RunAsync()
    {
        if (IsCompiling || IsRunning) return;

        IsRunning = true;
        Status = "正在运行...";
        Output += "\n--- 程序输出 ---\n";

        try
        {
            var compiler = new GccCompiler(_context);
            
            // 先编译
            var compileResult = await compiler.CompileAsync(Code);
            if (!compileResult.Success || compileResult.ExecutablePath == null)
            {
                Output += "无法运行：编译失败\n";
                Status = "运行失败 - 编译错误";
                return;
            }

            // 运行程序
            var runResult = await compiler.RunAsync(compileResult.ExecutablePath);
            
            if (!string.IsNullOrEmpty(runResult.Output))
            {
                Output += runResult.Output + "\n";
            }
            
            if (!string.IsNullOrEmpty(runResult.Error))
            {
                Output += $"[STDERR]: {runResult.Error}\n";
            }

            Output += $"\n--- 程序退出，返回码：{runResult.ExitCode} ---\n";
            Status = runResult.ExitCode == 0 ? "运行完成" : $"运行完成 (退出码：{runResult.ExitCode})";
            _logger.Info($"C++ 程序运行完成，退出码：{runResult.ExitCode}");
        }
        catch (Exception ex)
        {
            Output += $"✗ 运行错误：{ex.Message}\n";
            Status = "运行错误";
            _logger.Error("C++ 运行异常", ex);
        }
        finally
        {
            IsRunning = false;
        }
    }

    public void ClearOutput()
    {
        Output = "";
        Status = "就绪";
    }

    public void Dispose()
    {
        // 清理资源
    }
}

public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RelayCommand : ICommand
{
    private readonly Func<object?, Task>? _executeAsync;
    private readonly Action<object?>? _execute;
    private readonly Func<object?, bool>? _canExecute;
    private EventHandler? _canExecuteChanged;

    public RelayCommand(Func<object?, Task> executeAsync)
    {
        _executeAsync = executeAsync;
    }

    public RelayCommand(Action<object?> execute)
    {
        _execute = execute;
    }

    public RelayCommand(Action<object?> execute, Func<object?, bool> canExecute)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => _canExecuteChanged += value;
        remove => _canExecuteChanged -= value;
    }

    public void RaiseCanExecuteChanged()
    {
        _canExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke(parameter) ?? true;
    }

    public void Execute(object? parameter)
    {
        _execute?.Invoke(parameter);
        _executeAsync?.Invoke(parameter);
    }
}
