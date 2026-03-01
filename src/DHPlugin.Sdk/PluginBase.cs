using DHPlugin.Abstractions;

namespace DHPlugin.Sdk;

/// <summary>
/// 插件基类，提供常用功能的便捷实现
/// </summary>
public abstract class PluginBase : IPlugin
{
    protected IPluginContext? Context { get; private set; }
    protected ILogger Logger => Context?.GetLogger() ?? new NullLogger();
    
    public abstract PluginManifest Manifest { get; }
    public PluginState State { get; protected set; } = PluginState.Unloaded;

    public virtual Task InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {
        Context = context;
        State = PluginState.Loaded;
        Logger.Info($"插件 {Manifest.Name} 已初始化");
        return Task.CompletedTask;
    }

    public virtual Task EnableAsync(CancellationToken cancellationToken = default)
    {
        State = PluginState.Enabled;
        Logger.Info($"插件 {Manifest.Name} 已启用");
        return Task.CompletedTask;
    }

    public virtual Task DisableAsync(CancellationToken cancellationToken = default)
    {
        State = PluginState.Disabled;
        Logger.Info($"插件 {Manifest.Name} 已禁用");
        return Task.CompletedTask;
    }

    public virtual Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        State = PluginState.Unloaded;
        Context = null;
        Logger.Info($"插件 {Manifest.Name} 已关闭");
        return Task.CompletedTask;
    }
}

/// <summary>
/// 空日志器
/// </summary>
internal class NullLogger : ILogger
{
    public void Debug(string message) { }
    public void Info(string message) { }
    public void Warning(string message) { }
    public void Error(string message) { }
    public void Error(string message, Exception exception) { }
}
