using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using DHPlugin.Abstractions;

namespace DHPlugin.Host;

public class StateColorConverter : IValueConverter
{
    public static StateColorConverter Instance { get; } = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is PluginState state)
        {
            return state switch
            {
                PluginState.Enabled => new SolidColorBrush(Color.Parse("#4CAF50")),
                PluginState.Disabled => new SolidColorBrush(Color.Parse("#FF9800")),
                PluginState.Error => new SolidColorBrush(Color.Parse("#F44336")),
                PluginState.Loaded => new SolidColorBrush(Color.Parse("#2196F3")),
                _ => new SolidColorBrush(Color.Parse("#9E9E9E"))
            };
        }
        return new SolidColorBrush(Color.Parse("#9E9E9E"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public static NullToVisibilityConverter Instance { get; } = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value as string);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
