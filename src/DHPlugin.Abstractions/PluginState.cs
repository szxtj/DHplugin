namespace DHPlugin.Abstractions;

/// <summary>
/// 插件状态
/// </summary>
public enum PluginState
{
    /// <summary>
    /// 未加载
    /// </summary>
    Unloaded,
    
    /// <summary>
    /// 已加载但未启用
    /// </summary>
    Loaded,
    
    /// <summary>
    /// 已启用
    /// </summary>
    Enabled,
    
    /// <summary>
    /// 已禁用
    /// </summary>
    Disabled,
    
    /// <summary>
    /// 错误状态
    /// </summary>
    Error
}
