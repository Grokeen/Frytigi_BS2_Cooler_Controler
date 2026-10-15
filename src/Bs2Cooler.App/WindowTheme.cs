using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Bs2Cooler.App;

internal static class WindowTheme
{
    public static void Apply(Window window) => window.SourceInitialized += (_, _) =>
    {
        nint handle = new WindowInteropHelper(window).Handle;
        int enabled = 1;
        _ = DwmSetWindowAttribute(handle, 20, ref enabled, sizeof(int));
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            int border = 0x0069503C, caption = 0x0022160E, text = 0x00FFF4ED;
            _ = DwmSetWindowAttribute(handle, 34, ref border, sizeof(int));
            _ = DwmSetWindowAttribute(handle, 35, ref caption, sizeof(int));
            _ = DwmSetWindowAttribute(handle, 36, ref text, sizeof(int));
        }
    };
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);
}
