using DHPlugin.Abstractions;

namespace DHPlugin.Core;

/// <summary>
/// 插件运行时信息
/// </summary>
public class PluginInfo
{
    /// <summary>
    /// 插件实例
    /// </summary>
    public IPlugin? Instance { get; set; }
    
    /// <summary>
    /// 插件元数据
    /// </summary>
    public PluginManifest Manifest { get; set; } = null!;
    
    /// <summary>
    /// 插件路径
    /// </summary>
    public string Path { get; set; } = string.Empty;
    
    /// <summary>
    /// 插件状态
    /// </summary>
    public PluginState State { get; set; } = PluginState.Unloaded;
    
    /// <summary>
    /// 是否启用（持久化状态）
    /// </summary>
    public bool IsEnabled { get; set; }
    
    /// <summary>
    /// 加载错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// 加载错误异常
    /// </summary>
    public Exception? ErrorException { get; set; }
}
