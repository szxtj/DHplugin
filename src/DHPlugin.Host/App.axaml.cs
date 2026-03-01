using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DHPlugin.Core;
using System.IO;

namespace DHPlugin.Host;

public partial class App : Application
{
    public static PluginManager? PluginManager { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 初始化插件管理器
        var appDataPath = GetAppDataPath();
        PluginManager = new PluginManager(
            Path.Combine(appDataPath, "Plugins"),
            Path.Combine(appDataPath, "Config")
        );

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = new MainWindowViewModel(PluginManager);
            
            // 先创建 ViewModel，再创建窗口并设置 DataContext
            var mainWindow = new MainWindow();
            mainWindow.DataContext = viewModel;
            desktop.MainWindow = mainWindow;
            
            // 在窗口打开后加载插件
            mainWindow.Opened += async (s, e) =>
            {
                await PluginManager.LoadPluginsAsync();
                viewModel.OnPluginsLoaded();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static string GetAppDataPath()
    {
        var appName = "DHPlugin";

        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, appName);
        }
        else if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", appName);
        }
        else // Linux
        {
            var xdgData = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            if (string.IsNullOrEmpty(xdgData))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                xdgData = Path.Combine(home, ".local", "share");
            }
            return Path.Combine(xdgData, appName);
        }
    }
}
