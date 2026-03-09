using System.Linq;
using Avalonia;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml.Styling;
using DHPlugin.Abstractions;
using DHPlugin.Sdk;
using OxyPlot.Avalonia;
using VisualProgramming;
using VisualProgramming.Views;

namespace VisualProgrammingPlugin;

public class VisualProgrammingPlugin : PluginBase
{
    private const string MainViewContributionId = "VisualProgrammingPlugin.MainView";
    private static bool s_oxyPlotThemeLoaded;
    private static bool s_validationDisabled;

    private DesignerWorkbenchView? _mainView;

    public override PluginManifest Manifest { get; } = new()
    {
        Id = "VisualProgrammingPlugin",
        Name = "图形化编程",
        Version = "1.0.0",
        Description = "图形化编程插件，集成设计器、流程图编辑器、算法导入与 C++ 导出能力。",
        Author = "DHPlugin Team",
        MinimumHostVersion = "1.0.0"
    };

    public override Task InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {
        var initializeTask = base.InitializeAsync(context, cancellationToken);

        var pluginRootDirectory = Path.GetDirectoryName(typeof(VisualProgrammingPlugin).Assembly.Location)
            ?? AppContext.BaseDirectory;
        var dataDirectory = Path.Combine(context.GetDataDirectory(), Manifest.Id);
        var configDirectory = Path.Combine(context.GetConfigDirectory(), Manifest.Id);

        PluginRuntimeEnvironment.Initialize(pluginRootDirectory, dataDirectory, configDirectory);

        OxyPlotModule.EnsureLoaded();
        EnsureOxyPlotThemeLoaded();
        DisableAvaloniaDataAnnotationValidation();

        return initializeTask;
    }

    public override Task EnableAsync(CancellationToken cancellationToken = default)
    {
        var enableTask = base.EnableAsync(cancellationToken);

        if (Context == null)
        {
            return enableTask;
        }

        _mainView = new DesignerWorkbenchView();

        Context.RegisterUiContribution(new UiContribution
        {
            PluginId = Manifest.Id,
            ContributionPoint = UiContributionPoints.MainContent,
            Id = MainViewContributionId,
            Name = Manifest.Name,
            UiElement = _mainView,
            Order = 20
        });

        return enableTask;
    }

    public override Task DisableAsync(CancellationToken cancellationToken = default)
    {
        Context?.UnregisterUiContribution(MainViewContributionId);
        _mainView = null;
        return base.DisableAsync(cancellationToken);
    }

    public override Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        Context?.UnregisterUiContribution(MainViewContributionId);
        _mainView = null;
        return base.ShutdownAsync(cancellationToken);
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        if (s_validationDisabled)
        {
            return;
        }

        var dataValidationPluginsToRemove = BindingPlugins.DataValidators
            .OfType<DataAnnotationsValidationPlugin>()
            .ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }

        s_validationDisabled = true;
    }

    private static void EnsureOxyPlotThemeLoaded()
    {
        if (s_oxyPlotThemeLoaded)
        {
            return;
        }

        var application = Application.Current;
        if (application == null)
        {
            return;
        }

        application.Styles.Add(new StyleInclude(new Uri($"avares://{typeof(VisualProgrammingPlugin).Assembly.GetName().Name}/"))
        {
            Source = new Uri("avares://OxyPlot.Avalonia/Themes/Default.axaml")
        });

        s_oxyPlotThemeLoaded = true;
    }
}