using System.Windows.Interop;
using Bs2Cooler.Windows;

namespace Bs2Cooler.App;

internal sealed class DesktopServer : IDisposable
{
    private readonly MainWindow _window;
    private readonly HwndSource _source;
    public DesktopServer(MainWindow window)
    {
        _window = window;
        nint handle = new WindowInteropHelper(window).EnsureHandle();
        _source = HwndSource.FromHwnd(handle)!;
        _source.AddHook(OnMessage);
        DesktopIpc.AllowShowMessages(handle);
    }
    private nint OnMessage(nint hwnd, int message, nint wparam, nint lparam, ref bool handled)
    {
        if ((uint)message != DesktopIpc.MessageId) return 0;
        handled = true;
        return _window.HandleDesktopCommand((DesktopCommand)(int)wparam);
    }
    public void Dispose() { if (!_source.IsDisposed) _source.RemoveHook(OnMessage); }
}
