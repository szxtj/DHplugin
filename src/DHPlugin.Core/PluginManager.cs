using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using DHPlugin.Abstractions;

namespace DHPlugin.Core;

/// <summary>
/// 插件管理器
/// </summary>
public class PluginManager : IPluginContext
{
    private readonly string _pluginsDirectory;
    private readonly string _configDirectory;
    private readonly Dictionary<string, PluginInfo> _plugins = new();
    private readonly Dictionary<string, List<Action<string, object?>>> _eventSubscribers = new();
    private readonly Dictionary<string, object> _services = new();
    private readonly Dictionary<string, UiContribution> _uiContributions = new();
    private readonly ILogger _logger;
    
    public string HostVersion { get; } = "1.0.0";
    
    public event EventHandler<PluginInfo>? PluginInstalled;
    public event EventHandler<PluginInfo>? PluginUninstalled;
    public event EventHandler<PluginInfo>? PluginEnabled;
    public event EventHandler<PluginInfo>? PluginDisabled;
    public event EventHandler? PluginsChanged;
    
    public PluginManager(string pluginsDirectory, string configDirectory)
    {
        _pluginsDirectory = pluginsDirectory;
        _configDirectory = configDirectory;
        _logger = new PluginLogger("Host");
        
        Directory.CreateDirectory(_pluginsDirectory);
        Directory.CreateDirectory(_configDirectory);
        Directory.CreateDirectory(GetPluginDataDirectory());
    }
    
    #region Plugin Management
    
    /// <summary>
    /// 扫描并加载所有插件
    /// </summary>
    public async Task LoadPluginsAsync(CancellationToken cancellationToken = default)
    {
        _logger.Info("开始扫描插件目录...");
        
        // 清理影子插件（启动时清理未启用或已卸载的）
        await CleanupShadowPluginsAsync();
        
        if (!Directory.Exists(_pluginsDirectory))
        {
            _logger.Info("插件目录不存在，创建空目录");
            Directory.CreateDirectory(_pluginsDirectory);
            return;
        }
        
        // 扫描所有插件目录和包
        var pluginDirs = Directory.GetDirectories(_pluginsDirectory);
        var pluginPackages = Directory.GetFiles(_pluginsDirectory, "*.dhplugin");
        
        // 先解压所有包
        foreach (var package in pluginPackages)
        {
            await ExtractPackageAsync(package, cancellationToken);
        }

        pluginDirs = Directory.GetDirectories(_pluginsDirectory);
        
        // 加载所有插件
        foreach (var dir in pluginDirs)
        {
            var manifestPath = Path.Combine(dir, "manifest.json");
            if (File.Exists(manifestPath))
            {
                await LoadPluginAsync(dir, cancellationToken);
            }
        }

        // 自动启用已标记为启用的插件（调用 EnableAsync 注册 UI 贡献）
        var config = LoadConfig();
        foreach (var plugin in _plugins.Values)
        {
            if (config.EnabledPlugins.Contains(plugin.Manifest.Id))
            {
                plugin.IsEnabled = true;
                if (plugin.State == PluginState.Loaded || plugin.State == PluginState.Disabled)
                {
                    await EnablePluginAsync(plugin.Manifest.Id, cancellationToken);
                }
                else
                {
                    _logger.Info($"插件已标记为启用：{plugin.Manifest.Name}");
                }
            }
        }

        _logger.Info($"插件扫描完成，共发现 {_plugins.Count} 个插件");
        PluginsChanged?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// 清理影子插件（启动时清理未启用或已卸载的）
    /// </summary>
    private async Task CleanupShadowPluginsAsync()
    {
        _logger.Info("清理影子插件...");
        
        if (!Directory.Exists(_pluginsDirectory))
        {
            return;
        }
        
        var config = LoadConfig();
        var pluginDirs = Directory.GetDirectories(_pluginsDirectory);
        
        foreach (var dir in pluginDirs)
        {
            var dirName = Path.GetFileName(dir);
            var manifestPath = Path.Combine(dir, "manifest.json");
            
            // 检查是否是影子插件目录（以插件 ID 命名）
            if (!File.Exists(manifestPath))
            {
                // 没有 manifest.json，可能是残留的影子，删除
                _logger.Info($"删除残留影子目录：{dirName}");
                Directory.Delete(dir, true);
                continue;
            }
            
            // 读取插件 ID
            try
            {
                var manifestJson = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<PluginManifest>(manifestJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (manifest != null && !config.EnabledPlugins.Contains(manifest.Id))
                {
                    // 插件未启用，删除影子
                    _logger.Info($"删除未启用插件影子：{manifest.Id}");
                    Directory.Delete(dir, true);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"清理影子插件时出错：{dirName}, {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 从包文件解压插件
    /// </summary>
    private async Task ExtractPackageAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        var extractDir = Path.Combine(_pluginsDirectory, Path.GetFileNameWithoutExtension(packagePath));
        
        if (Directory.Exists(extractDir))
        {
            Directory.Delete(extractDir, true);
        }
        
        ZipFile.ExtractToDirectory(packagePath, extractDir);
        
        _logger.Info($"插件包已解压：{packagePath}");
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// 加载单个插件
    /// </summary>
    private async Task LoadPluginAsync(string pluginPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var manifestPath = Path.Combine(pluginPath, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                _logger.Warning($"插件 {pluginPath} 缺少 manifest.json");
                return;
            }
            
            var manifestJson = await File.ReadAllTextAsync(manifestPath, cancellationToken);
            var manifest = JsonSerializer.Deserialize<PluginManifest>(manifestJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (manifest == null)
            {
                _logger.Error($"插件 {pluginPath} 的 manifest.json 格式无效");
                return;
            }
            
            if (_plugins.ContainsKey(manifest.Id))
            {
                _logger.Warning($"插件 {manifest.Id} 已存在，跳过加载");
                return;
            }
            
            var pluginInfo = new PluginInfo
            {
                Manifest = manifest,
                Path = pluginPath,
                State = PluginState.Unloaded
            };
            
            _plugins[manifest.Id] = pluginInfo;
            
            // 加载插件程序集
            var assemblyPath = Path.Combine(pluginPath, $"{manifest.Id}.dll");
            if (File.Exists(assemblyPath))
            {
                var loadContext = new PluginLoadContext(assemblyPath);
                pluginInfo.LoadContext = loadContext;

                var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
                var pluginType = assembly.GetTypes()
                    .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
                
                if (pluginType != null)
                {
                    var instance = (IPlugin)Activator.CreateInstance(pluginType)!;
                    pluginInfo.Instance = instance;
                    
                    // 初始化插件
                    await instance.InitializeAsync(this, cancellationToken);
                    pluginInfo.State = PluginState.Loaded;
                    
                    _logger.Info($"插件已加载：{manifest.Name} v{manifest.Version}");
                }
                else
                {
                    _logger.Warning($"插件 {manifest.Id} 未找到 IPlugin 实现类");
                    pluginInfo.State = PluginState.Error;
                    pluginInfo.ErrorMessage = "未找到 IPlugin 实现类";
                }
            }
            else
            {
                // 无代码的纯 UI 插件
                _logger.Info($"插件已加载（UI 插件）: {manifest.Name} v{manifest.Version}");
                pluginInfo.State = PluginState.Loaded;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"加载插件失败：{pluginPath}", ex);

            var pluginId = Path.GetFileName(pluginPath);
            if (_plugins.TryGetValue(pluginId, out var pluginInfo))
            {
                pluginInfo.State = PluginState.Error;
                pluginInfo.ErrorMessage = ex.Message;
                pluginInfo.ErrorException = ex;
            }
        }
    }
    
    /// <summary>
    /// 安装插件包
    /// </summary>
    public async Task<PluginInfo?> InstallPluginAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(packagePath))
        {
            _logger.Error($"插件包不存在：{packagePath}");
            return null;
        }
        
        try
        {
            // 验证包格式
            var extractDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                ZipFile.ExtractToDirectory(packagePath, extractDir);
                
                var manifestPath = Path.Combine(extractDir, "manifest.json");
                if (!File.Exists(manifestPath))
                {
                    _logger.Error("插件包缺少 manifest.json");
                    return null;
                }
                
                var manifestJson = await File.ReadAllTextAsync(manifestPath, cancellationToken);
                var manifest = JsonSerializer.Deserialize<PluginManifest>(manifestJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (manifest == null)
                {
                    _logger.Error("manifest.json 格式无效");
                    return null;
                }
                
                // 检查是否已存在
                if (_plugins.ContainsKey(manifest.Id))
                {
                    _logger.Warning($"插件 {manifest.Id} 已存在，将先卸载旧版本");
                    await UninstallPluginAsync(manifest.Id, cancellationToken);
                }
                
                // 解压到插件目录
                var targetDir = Path.Combine(_pluginsDirectory, manifest.Id);
                if (Directory.Exists(targetDir))
                {
                    Directory.Delete(targetDir, true);
                }
                
                ZipFile.ExtractToDirectory(packagePath, targetDir);
                
                // 加载插件
                await LoadPluginAsync(targetDir, cancellationToken);
                
                var pluginInfo = _plugins[manifest.Id];
                PluginInstalled?.Invoke(this, pluginInfo);
                PluginsChanged?.Invoke(this, EventArgs.Empty);
                
                _logger.Info($"插件已安装：{manifest.Name} v{manifest.Version}");
                return pluginInfo;
            }
            finally
            {
                if (Directory.Exists(extractDir))
                {
                    Directory.Delete(extractDir, true);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"安装插件失败：{packagePath}", ex);
            return null;
        }
    }
    
    /// <summary>
    /// 卸载插件
    /// </summary>
    public async Task<bool> UninstallPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (!_plugins.TryGetValue(pluginId, out var pluginInfo))
        {
            _logger.Warning($"插件不存在：{pluginId}");
            return false;
        }
        
        try
        {
            // 先禁用
            if (pluginInfo.State == PluginState.Enabled || pluginInfo.State == PluginState.Loaded)
            {
                await DisablePluginAsync(pluginId, cancellationToken);
            }
            
            // 清理资源
            if (pluginInfo.Instance != null)
            {
                await pluginInfo.Instance.ShutdownAsync(cancellationToken);
            }
            
            // 删除插件目录
            if (Directory.Exists(pluginInfo.Path))
            {
                Directory.Delete(pluginInfo.Path, true);
            }
            
            // 删除插件数据
            var dataDir = Path.Combine(GetPluginDataDirectory(), pluginId);
            if (Directory.Exists(dataDir))
            {
                Directory.Delete(dataDir, true);
            }
            
            _plugins.Remove(pluginId);
            
            // 更新配置
            var config = LoadConfig();
            config.EnabledPlugins.Remove(pluginId);
            SaveConfig(config);
            
            PluginUninstalled?.Invoke(this, pluginInfo);
            PluginsChanged?.Invoke(this, EventArgs.Empty);
            
            _logger.Info($"插件已卸载：{pluginId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"卸载插件失败：{pluginId}", ex);
            return false;
        }
    }
    
    /// <summary>
    /// 启用插件
    /// </summary>
    public async Task<bool> EnablePluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (!_plugins.TryGetValue(pluginId, out var pluginInfo))
        {
            _logger.Warning($"插件不存在：{pluginId}");
            return false;
        }
        
        if (pluginInfo.State == PluginState.Enabled)
        {
            return true;
        }
        
        if (pluginInfo.State != PluginState.Loaded && pluginInfo.State != PluginState.Disabled)
        {
            _logger.Warning($"插件状态不允许启用：{pluginId}, 当前状态：{pluginInfo.State}");
            return false;
        }
        
        try
        {
            // 检查依赖
            foreach (var dep in pluginInfo.Manifest.Dependencies)
            {
                if (!_plugins.TryGetValue(dep, out var depInfo) || depInfo.State != PluginState.Enabled)
                {
                    _logger.Error($"插件依赖未满足：{dep}");
                    pluginInfo.State = PluginState.Error;
                    pluginInfo.ErrorMessage = $"依赖插件未启用：{dep}";
                    return false;
                }
            }
            
            if (pluginInfo.Instance != null)
            {
                await pluginInfo.Instance.EnableAsync(cancellationToken);
            }
            
            pluginInfo.State = PluginState.Enabled;
            pluginInfo.IsEnabled = true;
            
            // 保存配置
            var config = LoadConfig();
            if (!config.EnabledPlugins.Contains(pluginId))
            {
                config.EnabledPlugins.Add(pluginId);
                SaveConfig(config);
            }
            
            PluginEnabled?.Invoke(this, pluginInfo);
            PluginsChanged?.Invoke(this, EventArgs.Empty);
            
            _logger.Info($"插件已启用：{pluginId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"启用插件失败：{pluginId}", ex);
            pluginInfo.State = PluginState.Error;
            pluginInfo.ErrorMessage = ex.Message;
            return false;
        }
    }
    
    /// <summary>
    /// 禁用插件
    /// </summary>
    public async Task<bool> DisablePluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (!_plugins.TryGetValue(pluginId, out var pluginInfo))
        {
            _logger.Warning($"插件不存在：{pluginId}");
            return false;
        }
        
        if (pluginInfo.State == PluginState.Disabled || pluginInfo.State == PluginState.Unloaded)
        {
            return true;
        }
        
        try
        {
            if (pluginInfo.Instance != null)
            {
                await pluginInfo.Instance.DisableAsync(cancellationToken);
            }
            
            pluginInfo.State = PluginState.Disabled;
            pluginInfo.IsEnabled = false;
            
            // 保存配置
            var config = LoadConfig();
            config.EnabledPlugins.Remove(pluginId);
            SaveConfig(config);
            
            PluginDisabled?.Invoke(this, pluginInfo);
            PluginsChanged?.Invoke(this, EventArgs.Empty);
            
            _logger.Info($"插件已禁用：{pluginId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"禁用插件失败：{pluginId}", ex);
            return false;
        }
    }
    
    /// <summary>
    /// 获取所有插件
    /// </summary>
    public IEnumerable<PluginInfo> GetAllPlugins() => _plugins.Values.ToList();
    
    /// <summary>
    /// 获取插件
    /// </summary>
    public PluginInfo? GetPluginInfo(string pluginId)
    {
        _plugins.TryGetValue(pluginId, out var info);
        return info;
    }
    
    /// <summary>
    /// 获取已启用的插件
    /// </summary>
    public IEnumerable<PluginInfo> GetEnabledPlugins()
    {
        return _plugins.Values.Where(p => p.State == PluginState.Enabled);
    }
    
    #endregion
    
    #region IPluginContext Implementation
    
    public T? GetService<T>() where T : class
    {
        _services.TryGetValue(typeof(T).FullName!, out var service);
        return service as T;
    }
    
    public void RegisterService<T>(T service) where T : class
    {
        _services[typeof(T).FullName!] = service;
    }
    
    public string GetDataDirectory() => Path.Combine(_configDirectory, "PluginData");
    
    private string GetPluginDataDirectory() => Path.Combine(GetDataDirectory(), "Plugins");
    
    public string GetConfigDirectory() => _configDirectory;
    
    public ILogger GetLogger() => _logger;
    
    public void RegisterUiContribution(string contributionPoint, object uiElement)
    {
        // 由插件自己调用，这里需要知道是哪个插件注册的
        // 实际使用时需要通过调用栈或其他方式获取当前插件 ID
    }
    
    public void RegisterUiContribution(UiContribution contribution)
    {
        _uiContributions[contribution.Id] = contribution;
    }

    public void UnregisterUiContribution(string contributionId)
    {
        _uiContributions.Remove(contributionId);
    }
    
    public IEnumerable<UiContribution> GetUiContributions(string contributionPoint)
    {
        return _uiContributions.Values
            .Where(c => c.ContributionPoint == contributionPoint)
            .OrderBy(c => c.Order);
    }
    
    public IEnumerable<UiContribution> GetAllUiContributions()
    {
        return _uiContributions.Values.ToList();
    }
    
    public IPlugin? GetPlugin(string pluginId)
    {
        if (_plugins.TryGetValue(pluginId, out var info) && info.State == PluginState.Enabled)
        {
            return info.Instance;
        }
        return null;
    }
    
    public void PublishEvent(string eventName, object? data = null)
    {
        if (_eventSubscribers.TryGetValue(eventName, out var handlers))
        {
            foreach (var handler in handlers.ToList())
            {
                try
                {
                    handler(eventName, data);
                }
                catch (Exception ex)
                {
                    _logger.Error($"事件处理失败：{eventName}", ex);
                }
            }
        }
    }
    
    public void SubscribeEvent(string eventName, Action<string, object?> handler)
    {
        if (!_eventSubscribers.ContainsKey(eventName))
        {
            _eventSubscribers[eventName] = new List<Action<string, object?>>();
        }
        _eventSubscribers[eventName].Add(handler);
    }
    
    public void UnsubscribeEvent(string eventName, Action<string, object?> handler)
    {
        if (_eventSubscribers.TryGetValue(eventName, out var handlers))
        {
            handlers.Remove(handler);
        }
    }
    
    #endregion
    
    #region Configuration
    
    private PluginConfig LoadConfig()
    {
        var configPath = Path.Combine(_configDirectory, "plugins.config.json");
        if (File.Exists(configPath))
        {
            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<PluginConfig>(json) ?? new PluginConfig();
        }
        return new PluginConfig();
    }
    
    private void SaveConfig(PluginConfig config)
    {
        var configPath = Path.Combine(_configDirectory, "plugins.config.json");
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, json);
    }
    
    private class PluginConfig
    {
        public List<string> EnabledPlugins { get; set; } = new();
    }
    
    #endregion
}

/// <summary>
/// 插件日志器
/// </summary>
internal class PluginLogger : ILogger
{
    private readonly string _category;
    
    public PluginLogger(string category)
    {
        _category = category;
    }
    
    public void Debug(string message) => Log("DEBUG", message);
    public void Info(string message) => Log("INFO", message);
    public void Warning(string message) => Log("WARNING", message);
    public void Error(string message) => Log("ERROR", message);
    public void Error(string message, Exception exception) => Log("ERROR", $"{message} - {exception}");
    
    private void Log(string level, string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{level}] [{_category}] {message}");
    }
}
