using System.Windows;
using System.Windows.Threading;
using Bs2Cooler.Windows;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace Bs2Cooler.App;

internal sealed class TrayController : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly Drawing.Icon _image;
    private readonly Forms.ContextMenuStrip _menu = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(2) };
    public TrayController(MainWindow window)
    {
        using var resource = Application.GetResourceStream(new Uri("/Bs2Cooler;component/Assets/Bs2Cooler.ico", UriKind.Relative)).Stream;
        using var source = new Drawing.Icon(resource);
        _image = (Drawing.Icon)source.Clone();
        _icon = new Forms.NotifyIcon { Icon = _image, Text = "BS2 Cooler", ContextMenuStrip = _menu, Visible = true };
        _icon.DoubleClick += (_, _) => window.ShowControlWindow();
        _menu.Items.Add("창 열기", null, (_, _) => window.ShowControlWindow());
        var automatic = new Forms.ToolStripMenuItem("온도 자동 제어 시작");
        automatic.Click += async (_, _) => await window.ToggleAutomaticFromTrayAsync();
        _menu.Items.Add(automatic);
        _menu.Items.Add(new Forms.ToolStripSeparator());
        var startup = new Forms.ToolStripMenuItem("로그인 시 자동 실행");
        startup.Click += (_, _) =>
        {
            try { StartupTaskManager.SetEnabled(!StartupTaskManager.IsEnabled()); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "BS2 Cooler"); }
        };
        _menu.Items.Add(startup);
        var autoOnStartup = new Forms.ToolStripMenuItem("자동 실행 시 온도 제어도 시작");
        autoOnStartup.Click += (_, _) =>
        {
            try { window.SetStartupCooling(!window.StartupCoolingEnabled); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "BS2 Cooler"); }
        };
        _menu.Items.Add(autoOnStartup);
        _menu.Items.Add(new Forms.ToolStripSeparator());
        _menu.Items.Add("종료 · 기존 팬 설정 복원", null, async (_, _) => await window.RequestExitAsync());
        _menu.Opening += (_, _) =>
        {
            automatic.Text = window.AutomaticActive ? "온도 자동 제어 중지" : "온도 자동 제어 시작";
            autoOnStartup.Checked = window.StartupCoolingEnabled;
            try { startup.Checked = StartupTaskManager.IsEnabled(); } catch { startup.Checked = false; }
        };
        _timer.Tick += (_, _) => { string text = window.TraySummary(); _icon.Text = text.Length > 63 ? text[..63] : text; };
        _timer.Start();
    }
    public void Dispose() { _timer.Stop(); _icon.Visible = false; _icon.Dispose(); _menu.Dispose(); _image.Dispose(); }
}
