namespace DHPlugin.Abstractions;

/// <summary>
/// 插件基础接口
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// 插件元数据
    /// </summary>
    PluginManifest Manifest { get; }
    
    /// <summary>
    /// 插件当前状态
    /// </summary>
    PluginState State { get; }
    
    /// <summary>
    /// 初始化插件（加载时调用）
    /// </summary>
    Task InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 启用插件
    /// </summary>
    Task EnableAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 禁用插件
    /// </summary>
    Task DisableAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 卸载插件前清理资源
    /// </summary>
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}
