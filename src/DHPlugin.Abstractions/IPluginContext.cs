namespace DHPlugin.Abstractions;

/// <summary>
/// 插件上下文接口，提供插件与主机通信的能力
/// </summary>
public interface IPluginContext
{
    /// <summary>
    /// 获取主机版本
    /// </summary>
    string HostVersion { get; }
    
    /// <summary>
    /// 获取主机服务
    /// </summary>
    T? GetService<T>() where T : class;
    
    /// <summary>
    /// 注册服务到主机
    /// </summary>
    void RegisterService<T>(T service) where T : class;
    
    /// <summary>
    /// 获取插件数据目录
    /// </summary>
    string GetDataDirectory();
    
    /// <summary>
    /// 获取插件配置目录
    /// </summary>
    string GetConfigDirectory();
    
    /// <summary>
    /// 日志输出
    /// </summary>
    ILogger GetLogger();

    /// <summary>
    /// 注册 UI 贡献点（如菜单、工具栏等）
    /// </summary>
    void RegisterUiContribution(string contributionPoint, object uiElement);

    /// <summary>
    /// 注册 UI 贡献点（使用贡献对象）
    /// </summary>
    void RegisterUiContribution(UiContribution contribution);
    
    /// <summary>
    /// 获取其他已启用的插件
    /// </summary>
    IPlugin? GetPlugin(string pluginId);
    
    /// <summary>
    /// 发布事件
    /// </summary>
    void PublishEvent(string eventName, object? data = null);
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    void SubscribeEvent(string eventName, Action<string, object?> handler);
    
    /// <summary>
    /// 取消订阅事件
    /// </summary>
    void UnsubscribeEvent(string eventName, Action<string, object?> handler);
}

/// <summary>
/// 日志接口
/// </summary>
public interface ILogger
{
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message);
    void Error(string message, Exception exception);
}
