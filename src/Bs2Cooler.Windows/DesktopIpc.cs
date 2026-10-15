using System.Runtime.InteropServices;

namespace Bs2Cooler.Windows;

public enum DesktopCommand { Show = 1, Hide = 2, Status = 3, Exit = 4, Temperatures = 5, CloseToTray = 6 }
public sealed record DesktopProcessStatus(int ProcessId, bool Visible, bool Connected, bool Automatic, bool Elevated, bool Initialized, bool Tray, double? Cpu, double? Gpu);

// Only fixed window/tray commands and read-only status are accepted; no command text or paths cross this boundary.
public static class DesktopIpc
{
    public static uint MessageId { get; } = RegisterWindowMessage("Bs2Cooler.Desktop.v1." + StartupTaskManager.UserSid);
    public static void AllowShowMessages(nint handle) => ChangeWindowMessageFilterEx(handle, MessageId, 1, 0);
    public static bool Send(DesktopCommand command, out nint result)
    {
        nint handle = FindWindow(null, "BS2 Cooler");
        result = 0;
        return handle != 0 && SendMessageTimeout(handle, MessageId, (nuint)command, 0, 2, 2000, out result) != 0;
    }
    public static DesktopProcessStatus? ReadStatus()
    {
        nint handle = FindWindow(null, "BS2 Cooler");
        if (handle == 0 || !Send(DesktopCommand.Status, out nint reply) || ((int)reply & 0x4000) == 0) return null;
        GetWindowThreadProcessId(handle, out uint pid);
        int flags = (int)reply;
        Send(DesktopCommand.Temperatures, out nint temperatures);
        int cpu = (int)temperatures & 0xFF, gpu = ((int)temperatures >> 8) & 0xFF;
        return new((int)pid, (flags & 1) != 0, (flags & 2) != 0, (flags & 4) != 0, (flags & 8) != 0, (flags & 16) != 0, (flags & 32) != 0,
            cpu == 0 ? null : cpu - 1, gpu == 0 ? null : gpu - 1);
    }
    [DllImport("user32.dll", EntryPoint = "RegisterWindowMessageW", CharSet = CharSet.Unicode)] private static extern uint RegisterWindowMessage(string message);
    [DllImport("user32.dll", EntryPoint = "FindWindowW", CharSet = CharSet.Unicode)] private static extern nint FindWindow(string? className, string title);
    [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW")] private static extern nint SendMessageTimeout(nint window, uint message, nuint wparam, nint lparam, uint flags, uint timeout, out nint result);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ChangeWindowMessageFilterEx(nint window, uint message, uint action, nint filter);
}
