using System;
using System.Runtime.CompilerServices;

namespace Panosse.WinUI;

/// <summary>
/// Required for unpackaged WinUI single-file publish (Windows App SDK runtime path).
/// </summary>
internal static class SingleFileBootstrap
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        Environment.SetEnvironmentVariable(
            "MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY",
            AppContext.BaseDirectory);
    }
}
