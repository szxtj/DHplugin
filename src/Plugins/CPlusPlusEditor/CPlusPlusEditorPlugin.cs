using DHPlugin.Abstractions;

namespace CPlusPlusEditor;

public class CPlusPlusEditorPlugin : IPlugin
{
    private IPluginContext? _context;
    private CppEditorViewModel? _viewModel;
    
    public PluginManifest Manifest { get; } = new()
    {
        Id = "CPlusPlusEditor",
        Name = "C++ 代码编辑器",
        Version = "1.0.0",
        Description = "C++ 代码编辑器插件，支持语法高亮、代码编译和运行。调用系统 GCC 编译器，支持一键运行 C++ 代码。",
        Author = "DHPlugin Team",
        IconPath = "icon.png",
        MinimumHostVersion = "1.0.0"
    };

    public PluginState State { get; private set; } = PluginState.Unloaded;

    public Task InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {
        _context = context;
        State = PluginState.Loaded;
        
        _context.GetLogger().Info("C++ 编辑器插件已初始化");
        
        return Task.CompletedTask;
    }

    public Task EnableAsync(CancellationToken cancellationToken = default)
    {
        if (_context == null) return Task.CompletedTask;
        
        State = PluginState.Enabled;
        
        // 创建编辑器视图并注册到主内容区
        _viewModel = new CppEditorViewModel(_context);
        var editorView = new CppEditorView
        {
            DataContext = _viewModel
        };
        
        _context.RegisterUiContribution(new UiContribution
        {
            ContributionPoint = UiContributionPoints.MainContent,
            Id = "CPlusPlusEditor.MainView",
            Name = "C++ 编辑器",
            UiElement = editorView,
            Order = 10
        });
        
        _context.GetLogger().Info("C++ 编辑器插件已启用");
        
        return Task.CompletedTask;
    }

    public Task DisableAsync(CancellationToken cancellationToken = default)
    {
        State = PluginState.Disabled;
        
        _viewModel?.Dispose();
        
        _context?.GetLogger().Info("C++ 编辑器插件已禁用");
        
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _viewModel?.Dispose();
        _context = null;
        State = PluginState.Unloaded;
        
        return Task.CompletedTask;
    }
}
