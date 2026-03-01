namespace DHPlugin.Abstractions;

/// <summary>
/// 插件元数据信息
/// </summary>
public class PluginManifest
{
    /// <summary>
    /// 插件唯一标识符
    /// </summary>
    public required string Id { get; set; }
    
    /// <summary>
    /// 插件显示名称
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// 插件版本
    /// </summary>
    public required string Version { get; set; }
    
    /// <summary>
    /// 插件描述
    /// </summary>
    public required string Description { get; set; }
    
    /// <summary>
    /// 插件作者
    /// </summary>
    public string? Author { get; set; }
    
    /// <summary>
    /// 插件图标路径（相对路径）
    /// </summary>
    public string? IconPath { get; set; }
    
    /// <summary>
    /// 最低主机版本要求
    /// </summary>
    public string? MinimumHostVersion { get; set; }
    
    /// <summary>
    /// 插件依赖的其他插件 ID 列表
    /// </summary>
    public List<string> Dependencies { get; set; } = new();
}
