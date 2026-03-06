using System;

namespace DHPlugin.Runner;

internal static class RootProgram
{
    [STAThread]
    private static void Main(string[] args)
    {
        // Delegate startup to the host entry point so `dotnet run` works from repo root.
        DHPlugin.Host.Program.Main(args);
    }
}
