using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.IO;

namespace DHPlugin.Host;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // 使用 DataContextChanged 事件确保 DataContext 设置后再调用
        DataContextChanged += OnDataContextChanged;
    }
    
    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.OnViewReady();
        }
    }

    private async void OnInstallPlugin(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择插件包",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("DHPlugin 插件包")
                {
                    Patterns = new[] { "*.dhplugin" }
                },
                new FilePickerFileType("所有文件")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        if (files.Count > 0)
        {
            var path = files[0].Path.LocalPath;
            if (App.PluginManager != null)
            {
                await App.PluginManager.InstallPluginAsync(path);
            }
        }
    }

    private void OnRefresh(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.RefreshPlugins();
        }
    }

    private async void OnEnablePlugin(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            await vm.EnableSelectedPluginAsync();
        }
    }

    private async void OnDisablePlugin(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            await vm.DisableSelectedPluginAsync();
        }
    }

    private async void OnUninstallPlugin(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            await vm.UninstallSelectedPluginAsync();
        }
    }
}
