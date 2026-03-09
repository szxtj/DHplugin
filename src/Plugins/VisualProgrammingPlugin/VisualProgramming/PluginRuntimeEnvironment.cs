using System.IO;

namespace VisualProgramming;

public static class PluginRuntimeEnvironment
{
    private static readonly object SyncRoot = new();
    private static string? s_pluginRootDirectory;
    private static string? s_dataDirectory;
    private static string? s_configDirectory;

    public static void Initialize(string pluginRootDirectory, string dataDirectory, string configDirectory)
    {
        lock (SyncRoot)
        {
            s_pluginRootDirectory = EnsureDirectory(pluginRootDirectory);
            s_dataDirectory = EnsureDirectory(dataDirectory);
            s_configDirectory = EnsureDirectory(configDirectory);
        }
    }

    public static string PluginRootDirectory => EnsureDirectory(s_pluginRootDirectory ?? AppContext.BaseDirectory);

    public static string DataDirectory => EnsureDirectory(s_dataDirectory ?? Path.Combine(Environment.CurrentDirectory, "PluginData"));

    public static string ConfigDirectory => EnsureDirectory(s_configDirectory ?? Path.Combine(Environment.CurrentDirectory, "PluginConfig"));

    public static string ExportDirectory
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(s_dataDirectory))
            {
                return EnsureDirectory(Path.Combine(s_dataDirectory, "download"));
            }

            return EnsureDirectory(Path.Combine(Environment.CurrentDirectory, "download"));
        }
    }

    public static string SamplesDirectory => EnsureDirectory(Path.Combine(PluginRootDirectory, "samples"));

    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}