using Avalonia;
using System;

namespace DHPlugin.Host;

public class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

        // On Linux/X11, configure rendering mode to fall back to software
        // rendering when hardware acceleration (GLX/EGL) is unavailable.
        // This fixes blank icons and missing UI elements on X11 displays.
        if (OperatingSystem.IsLinux())
        {
            builder = builder.With(new X11PlatformOptions
            {
                RenderingMode = new[]
                {
                    X11RenderingMode.Glx,
                    X11RenderingMode.Egl,
                    X11RenderingMode.Software,
                }
            });
        }

        return builder;
    }
}
