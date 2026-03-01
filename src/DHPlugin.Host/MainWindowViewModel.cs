using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Threading;
using DHPlugin.Abstractions;
using DHPlugin.Core;

namespace DHPlugin.Host;

public class MainWindowViewModel : ViewModelBase
{
    private readonly PluginManager _pluginManager;
    private PluginInfo? _selectedPlugin;
    private string? _activeTabPluginId;
    private bool _isViewReady;
    private bool _hasLoadedTabs;

    public ObservableCollection<PluginInfo> Plugins { get; } = new();
    public ObservableCollection<PluginTab> PluginTabs { get; } = new();

    public PluginInfo? SelectedPlugin
    {
        get => _selectedPlugin;
        set
        {
            _selectedPlugin = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsPluginSelected));
        }
    }

    public string? ActiveTabPluginId
    {
        get => _activeTabPluginId;
        set
        {
            _activeTabPluginId = value;
            OnPropertyChanged();
        }
    }

    public bool IsPluginSelected => SelectedPlugin != null;
    
    private int _pluginTabsCount;
    public bool HasActivePlugin => _pluginTabsCount > 0;
    public bool HasNoActivePlugin => _pluginTabsCount == 0;

    public MainWindowViewModel(PluginManager pluginManager)
    {
        _pluginManager = pluginManager;

        // 订阅插件事件
        _pluginManager.PluginEnabled += OnPluginEnabled;
        _pluginManager.PluginDisabled += OnPluginDisabled;
        _pluginManager.PluginInstalled += OnPluginInstalled;
        _pluginManager.PluginUninstalled += OnPluginUninstalled;
        _pluginManager.PluginsChanged += OnPluginsChanged;
        
        // 监听 PluginTabs 集合变化
        PluginTabs.CollectionChanged += (s, e) =>
        {
            _pluginTabsCount = PluginTabs.Count;
            OnPropertyChanged(nameof(HasActivePlugin));
            OnPropertyChanged(nameof(HasNoActivePlugin));
        };

        // 加载现有插件
        RefreshPlugins();
    }

    /// <summary>
    /// 视图已准备好，可以显示插件 UI
    /// </summary>
    public void OnViewReady()
    {
        _isViewReady = true;
    }

    /// <summary>
    /// 插件加载完成
    /// </summary>
    public void OnPluginsLoaded()
    {
        RefreshPlugins();
        if (_isViewReady && !_hasLoadedTabs)
        {
            _hasLoadedTabs = true;
            RefreshTabs();
        }
    }

    private void OnPluginsChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            RefreshPlugins();
            if (_isViewReady && _hasLoadedTabs)
            {
                RefreshTabs();
            }
        });
    }

    private void OnPluginInstalled(object? sender, PluginInfo e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Plugins.Add(e);
            OnPropertyChanged(nameof(Plugins));
        });
    }

    private void OnPluginUninstalled(object? sender, PluginInfo e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Plugins.Remove(e);
            if (SelectedPlugin == e)
            {
                SelectedPlugin = null;
            }

            // 关闭对应的标签页
            var tab = PluginTabs.FirstOrDefault(t => t.PluginId == e.Manifest.Id);
            if (tab != null)
            {
                PluginTabs.Remove(tab);
                if (ActiveTabPluginId == e.Manifest.Id)
                {
                    ActiveTabPluginId = PluginTabs.FirstOrDefault()?.PluginId;
                }
            }

            OnPropertyChanged(nameof(Plugins));
        });
    }

    private void OnPluginEnabled(object? sender, PluginInfo e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var index = Plugins.IndexOf(e);
            if (index >= 0)
            {
                Plugins[index] = e;
            }

            // 如果已加载过标签页，刷新
            if (_isViewReady && _hasLoadedTabs)
            {
                RefreshTabs();
            }

            OnPropertyChanged(nameof(Plugins));
        });
    }

    private void OnPluginDisabled(object? sender, PluginInfo e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var index = Plugins.IndexOf(e);
            if (index >= 0)
            {
                Plugins[index] = e;
            }

            // 如果已加载过标签页，刷新
            if (_isViewReady && _hasLoadedTabs)
            {
                RefreshTabs();
            }

            OnPropertyChanged(nameof(Plugins));
            OnPropertyChanged(nameof(PluginTabs));
        });
    }

    public void RefreshPlugins()
    {
        Plugins.Clear();
        foreach (var plugin in _pluginManager.GetAllPlugins())
        {
            Plugins.Add(plugin);
        }
        OnPropertyChanged(nameof(Plugins));
    }

    /// <summary>
    /// 刷新标签页（只在视图准备好后调用）
    /// </summary>
    private void RefreshTabs()
    {
        if (!_isViewReady) return;

        var enabledPluginIds = _pluginManager.GetEnabledPlugins().Select(p => p.Manifest.Id).ToHashSet();

        // 移除已禁用或已卸载的标签
        var tabsToRemove = PluginTabs.Where(t => !enabledPluginIds.Contains(t.PluginId)).ToList();
        foreach (var tab in tabsToRemove)
        {
            PluginTabs.Remove(tab);
        }

        // 添加新启用的插件标签（只添加一次）
        foreach (var plugin in _pluginManager.GetEnabledPlugins())
        {
            if (!PluginTabs.Any(t => t.PluginId == plugin.Manifest.Id))
            {
                var contributions = _pluginManager.GetUiContributions(UiContributionPoints.MainContent);
                foreach (var contribution in contributions)
                {
                    if (contribution.UiElement is Control control)
                    {
                        PluginTabs.Add(new PluginTab
                        {
                            PluginId = plugin.Manifest.Id,
                            PluginName = plugin.Manifest.Name,
                            Content = control
                        });
                        break;
                    }
                }
            }
        }

        // 更新当前标签
        if (ActiveTabPluginId == null || !enabledPluginIds.Contains(ActiveTabPluginId))
        {
            ActiveTabPluginId = PluginTabs.FirstOrDefault()?.PluginId;
        }

        // 触发属性通知
        _pluginTabsCount = PluginTabs.Count;
        OnPropertyChanged(nameof(PluginTabs));
        OnPropertyChanged(nameof(HasActivePlugin));
        OnPropertyChanged(nameof(HasNoActivePlugin));
    }

    public async System.Threading.Tasks.Task EnableSelectedPluginAsync()
    {
        if (SelectedPlugin != null)
        {
            await _pluginManager.EnablePluginAsync(SelectedPlugin.Manifest.Id);
        }
    }

    public async System.Threading.Tasks.Task DisableSelectedPluginAsync()
    {
        if (SelectedPlugin != null)
        {
            await _pluginManager.DisablePluginAsync(SelectedPlugin.Manifest.Id);
        }
    }

    public async System.Threading.Tasks.Task UninstallSelectedPluginAsync()
    {
        if (SelectedPlugin != null)
        {
            await _pluginManager.UninstallPluginAsync(SelectedPlugin.Manifest.Id);
            SelectedPlugin = null;
        }
    }
}

public class PluginTab
{
    public string PluginId { get; set; } = string.Empty;
    public string PluginName { get; set; } = string.Empty;
    public Control? Content { get; set; }
}

public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
