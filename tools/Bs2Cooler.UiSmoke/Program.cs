using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Bs2Cooler.Windows;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        int result = 1;
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Bs2Cooler;component/Styles.xaml", UriKind.Relative) });
        var window = new Bs2Cooler.App.MainWindow { WindowStartupLocation = WindowStartupLocation.Manual, Left = -12000, Top = -12000, ShowInTaskbar = false };
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        window.Closed += (_, _) => closed.TrySetResult();
        app.Startup += async (_, _) =>
        {
            try
            {
                window.Show();
                await Task.Delay(4500);
                string badge = ((TextBlock)window.FindName("ConnectionBadge")).Text;
                string rpm = ((TextBlock)window.FindName("ActualRpm")).Text;
                string firmware = ((TextBlock)window.FindName("FirmwareText")).Text;
                bool enabled = ((Button)window.FindName("ApplyButton")).IsEnabled;
                string temperature = ((TextBlock)window.FindName("GpuTemperature")).Text;
                Console.WriteLine($"UI: {badge}, actual={rpm}, {firmware}, applyEnabled={enabled}");
                if (badge != "USB 연결됨" || rpm == "—" || !enabled) throw new Exception("Live UI did not become ready.");
                if (!temperature.Contains("°C")) throw new Exception("Live GPU temperature was not displayed.");
                Console.WriteLine("Live GPU temperature: " + temperature);
                bool cpuGpu = args.Contains("--cpu-gpu");
                string cpuTemperature = ((TextBlock)window.FindName("CpuTemperature")).Text;
                if (cpuGpu && !cpuTemperature.Contains("°C")) throw new Exception("Live CPU temperature was not displayed.");
                Console.WriteLine("CPU temperature: " + cpuTemperature);
                if (args.Contains("--expect-elevation"))
                {
                    if (((Button)window.FindName("CpuPermissionButton")).Visibility != Visibility.Visible)
                        throw new Exception("CPU permission button was not displayed.");
                    if (((Button)window.FindName("AutoButton")).IsEnabled)
                        throw new Exception("CPU/GPU automatic mode allowed missing CPU data.");
                    Console.WriteLine("PASS: CPU permission prompt and unavailable CPU/GPU control lockout.");
                }
                bool exerciseAutomatic = args.Contains("--automatic") || cpuGpu;
                string baselineTarget = ((TextBlock)window.FindName("TargetRpm")).Text;
                if (args.Contains("--close-during-start"))
                {
                    if (!cpuGpu) ((ComboBox)window.FindName("TemperatureSource")).SelectedIndex = 1;
                    ((Button)window.FindName("AutoButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    window.Close();
                    window.Close();
                    await closed.Task.WaitAsync(TimeSpan.FromSeconds(15));
                    var device = NativeHid.Enumerate().First(d => d.IsSupported);
                    await using var connection = new Bs2Connection(device);
                    var restored = await connection.ReadRpmAsync();
                    if ($"{restored.TargetRpm:N0} RPM" != baselineTarget) throw new Exception("Closing during automatic startup did not restore the target.");
                    await Task.Delay(1100);
                    if (connection.LatestStatus?.IsSmart != false) throw new Exception("Closing during automatic startup left the device in smart mode.");
                    Console.WriteLine("PASS: repeated close during automatic startup restored manual mode and original target.");
                    result = 0;
                    return;
                }
                if (exerciseAutomatic)
                {
                    ((ComboBox)window.FindName("TemperatureSource")).SelectedIndex = cpuGpu ? 0 : 1;
                    ((TabControl)window.FindName("ControlTabs")).SelectedIndex = 1;
                    var automatic = (Button)window.FindName("AutoButton");
                    automatic.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    await Task.Delay(3500);
                    if (automatic.Content.ToString() != "자동 제어 중지") throw new Exception("Automatic mode did not start.");
                    if (((Button)window.FindName("ApplyButton")).IsEnabled) throw new Exception("Manual writes remain enabled during automatic mode.");
                    Console.WriteLine("AUTOMATIC UI: " + ((TextBlock)window.FindName("AutoState")).Text);
                }
                window.UpdateLayout();
                var root = (FrameworkElement)window.Content;
                if (!exerciseAutomatic) CheckLayout(window, root);
                var bitmap = new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth + 56), (int)Math.Ceiling(root.ActualHeight + 48), 96, 96, PixelFormats.Pbgra32);
                var background = new DrawingVisual();
                using (var drawing = background.RenderOpen()) drawing.DrawRectangle(window.Background, null, new Rect(0, 0, bitmap.Width, bitmap.Height));
                bitmap.Render(background);
                bitmap.Render(root);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                string path = Path.GetFullPath(args.FirstOrDefault(a => !a.StartsWith("--")) ?? ".analysis/ui-preview.png");
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using (var output = File.Create(path)) encoder.Save(output);
                if (exerciseAutomatic)
                {
                    ((Button)window.FindName("AutoButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    await Task.Delay(1500);
                    if (((Button)window.FindName("AutoButton")).Content.ToString() != "자동 제어 시작") throw new Exception("Automatic mode did not stop.");
                    if (((TextBlock)window.FindName("TargetRpm")).Text != baselineTarget) throw new Exception("Previous target RPM was not restored.");
                    ((TabControl)window.FindName("ControlTabs")).SelectedIndex = 0;
                    Console.WriteLine("PASS: automatic start/control/stop, manual-control lockout and original target restoration.");
                }
                window.Width = window.MinWidth; window.Height = window.MinHeight;
                await Task.Delay(200); window.UpdateLayout();
                CheckLayout(window, (FrameworkElement)window.Content);
                Console.WriteLine("PASS: WPF startup, live device data, controls and layout rendered: " + path);
                result = 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); }
            finally
            {
                if (!closed.Task.IsCompleted) window.Close();
                await closed.Task.WaitAsync(TimeSpan.FromSeconds(15));
                app.Shutdown(result);
            }
        };
        app.Run();
        return result;
    }

    private static void CheckLayout(Window window, FrameworkElement root)
    {
        var note = (FrameworkElement)window.FindName("ApplyNote");
        var panel = (FrameworkElement)window.FindName("ManualPanel");
        Rect noteBounds = note.TransformToAncestor(root).TransformBounds(new Rect(note.RenderSize));
        Rect panelBounds = panel.TransformToAncestor(root).TransformBounds(new Rect(panel.RenderSize));
        if (noteBounds.Bottom > panelBounds.Bottom - 10) throw new Exception("Fan controls are clipped at this window size.");
    }
}
