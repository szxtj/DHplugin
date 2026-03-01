namespace DHPlugin.Abstractions;

/// <summary>
/// UI 贡献点类型
/// </summary>
public static class UiContributionPoints
{
    /// <summary>
    /// 主菜单栏
    /// </summary>
    public const string MainMenu = "mainMenu";
    
    /// <summary>
    /// 主窗口内容区域
    /// </summary>
    public const string MainContent = "mainContent";
    
    /// <summary>
    /// 工具栏
    /// </summary>
    public const string ToolBar = "toolBar";
    
    /// <summary>
    /// 状态栏
    /// </summary>
    public const string StatusBar = "statusBar";
    
    /// <summary>
    /// 侧边栏
    /// </summary>
    public const string SideBar = "sideBar";
    
    /// <summary>
    /// 设置页面
    /// </summary>
    public const string SettingsPage = "settingsPage";
}

/// <summary>
/// UI 贡献描述
/// </summary>
public class UiContribution
{
    /// <summary>
    /// 贡献点名称
    /// </summary>
    public required string ContributionPoint { get; set; }
    
    /// <summary>
    /// UI 元素标识
    /// </summary>
    public required string Id { get; set; }
    
    /// <summary>
    /// UI 元素显示名称
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// UI 元素（Avalonia 控件）
    /// </summary>
    public required object UiElement { get; set; }
    
    /// <summary>
    /// 排序优先级（数字越小越靠前）
    /// </summary>
    public int Order { get; set; } = 100;
}
