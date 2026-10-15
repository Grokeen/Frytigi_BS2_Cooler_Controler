using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Bs2Cooler.Windows;
using Bs2Cooler.Core;

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
                string summary = ((TextBlock)window.FindName("TemperatureSummary")).Text;
                if (!summary.Contains("CPU " + cpuTemperature) || !summary.Contains("GPU " + temperature))
                    throw new Exception("Header does not show the same live CPU/GPU temperatures as the sensor panel.");
                string tray = window.TraySummary();
                if (!tray.Contains("CPU " + cpuTemperature) || !tray.Contains("GPU " + temperature))
                    throw new Exception("Tray summary does not show the live CPU/GPU temperatures.");
                Console.WriteLine("PASS: live temperature header, sensor panel and tray summary agree: " + summary);
                if (!summary.Contains("목표 70°C") || !tray.Contains("목표 70°C"))
                    throw new Exception("The requested 70 C target is missing from the header or tray.");
                if (args.Contains("--target-70"))
                {
                    var field = typeof(Bs2Cooler.App.MainWindow).GetField("_temperatures", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
                    var update = typeof(Bs2Cooler.App.MainWindow).GetMethod("UpdateTemperatureDisplay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
                    var original = (TemperatureSnapshot)field.GetValue(window)!;
                    try
                    {
                        var fixture = new TemperatureSnapshot(new(73, "UI fixture", DateTimeOffset.Now), new(45, "UI fixture", DateTimeOffset.Now), "UI fixture", "UI fixture");
                        field.SetValue(window, fixture); update.Invoke(window, null);
                        string warning = ((TextBlock)window.FindName("SensorDescription")).Text;
                        if (!warning.Contains("70°C 목표 초과")) throw new Exception("Above-target temperature warning is missing.");
                        Console.WriteLine("PASS: simulated 73 C CPU shows target-exceeded warning without heating hardware.");
                    }
                    finally { field.SetValue(window, original); update.Invoke(window, null); }
                }
                if (args.Contains("--expect-elevation"))
                {
                    if (((Button)window.FindName("CpuPermissionButton")).Visibility != Visibility.Visible)
                        throw new Exception("CPU permission button was not displayed.");
                    if (((Button)window.FindName("AutoButton")).IsEnabled)
                        throw new Exception("CPU/GPU automatic mode allowed missing CPU data.");
                    Console.WriteLine("PASS: CPU permission prompt and unavailable CPU/GPU control lockout.");
                }
                if (args.Contains("--theme")) await ThemeSmoke.MainAsync(window);
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
                if (args.Contains("--rgb")) await ExerciseRgbEditorAsync(window, args.Contains("--theme"));
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
        var firmware = (FrameworkElement)window.FindName("FirmwareText");
        Rect firmwareBounds = firmware.TransformToAncestor(root).TransformBounds(new Rect(firmware.RenderSize));
        if (firmwareBounds.Bottom > panelBounds.Bottom - 15) throw new Exception("Lighting controls are clipped at this window size.");
    }

    private static async Task ExerciseRgbEditorAsync(Window main, bool theme)
    {
        var baseline = RgbSettingsStore.Load() ?? throw new Exception("RGB UI exercise requires a previously saved app palette for exact restoration.");
        bool enabled = ((Button)main.FindName("RgbButton")).Content.ToString()!.StartsWith("켜짐");
        var completion = new TaskCompletionSource<Exception?>();
        _ = main.Dispatcher.BeginInvoke(async () =>
        {
            Bs2Cooler.App.RgbWindow? editor = null;
            Exception? failure = null;
            try
            {
                editor = Application.Current.Windows.OfType<Bs2Cooler.App.RgbWindow>().Single();
                await Task.Delay(250);
                if (theme) await ThemeSmoke.RgbAsync(editor);
                ((TextBox)editor.FindName("Color0")).Text = "#GG1122";
                ((Button)editor.FindName("ApplyRgb")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (!((TextBlock)editor.FindName("RgbStatus")).Text.Contains("#RRGGBB")) throw new Exception("Invalid RGB input was not rejected.");
                var test = baseline with { Effect = RgbEffect.Individual, Brightness = 35, Speed = 8, Colors = [RgbColor.Parse("#123456"), ..baseline.Colors.Skip(1)] };
                SetRgbEditor(editor, test, enabled);
                await ApplyRgbEditorAsync(editor);
                var saved = RgbSettingsStore.Load()!;
                if (saved.Effect != RgbEffect.Individual || saved.Brightness != 35 || saved.Colors[0].Hex != "#123456") throw new Exception("RGB settings were not applied and saved.");
                editor.UpdateLayout();
                SaveWindowImage(editor, ".analysis/rgb-ui-preview.png");
                editor.Width = editor.MinWidth; editor.Height = editor.MinHeight;
                await Task.Delay(150); editor.UpdateLayout();
                var root = (FrameworkElement)editor.Content;
                var apply = (FrameworkElement)editor.FindName("ApplyRgb");
                Rect bounds = apply.TransformToAncestor(root).TransformBounds(new Rect(apply.RenderSize));
                if (bounds.Bottom > root.ActualHeight || bounds.Right > root.ActualWidth) throw new Exception("RGB apply control is clipped.");
                Console.WriteLine("PASS: RGB dialog input validation, real device apply, local save, preview and minimum-size layout.");
            }
            catch (Exception ex) { failure = ex; }
            finally
            {
                if (editor != null)
                {
                    try
                    {
                        SetRgbEditor(editor, baseline, enabled);
                        await ApplyRgbEditorAsync(editor);
                        Console.WriteLine("RGB UI: previous app palette and enable state restored.");
                    }
                    catch (Exception ex) { failure = failure == null ? ex : new AggregateException(failure, ex); }
                    editor.Close();
                }
                completion.TrySetResult(failure);
            }
        });
        ((Button)main.FindName("RgbDetailsButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (await completion.Task is { } error) throw error;
    }

    private static void SetRgbEditor(Bs2Cooler.App.RgbWindow editor, RgbSettings settings, bool enabled)
    {
        ((ComboBox)editor.FindName("EffectChoice")).SelectedIndex = (int)settings.Effect;
        ((Slider)editor.FindName("Brightness")).Value = settings.Brightness;
        ((Slider)editor.FindName("EffectSpeed")).Value = settings.Speed;
        ((ComboBox)editor.FindName("BreathingCount")).SelectedIndex = settings.BreathingColors - 1;
        ((CheckBox)editor.FindName("EnableLight")).IsChecked = enabled;
        for (int i = 0; i < 6; i++) ((TextBox)editor.FindName($"Color{i}")).Text = settings.Colors[i].Hex;
    }

    private static async Task ApplyRgbEditorAsync(Bs2Cooler.App.RgbWindow editor)
    {
        var apply = (Button)editor.FindName("ApplyRgb");
        apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var until = DateTime.UtcNow.AddSeconds(15);
        while (!apply.IsEnabled && DateTime.UtcNow < until) await Task.Delay(50);
        if (!apply.IsEnabled || !((TextBlock)editor.FindName("RgbStatus")).Text.StartsWith("조명 설정을 적용하고"))
            throw new Exception("RGB UI device apply failed: " + ((TextBlock)editor.FindName("RgbStatus")).Text);
    }

    private static void SaveWindowImage(Window window, string path)
    {
        var root = (FrameworkElement)window.Content;
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth + root.Margin.Left + root.Margin.Right), (int)Math.Ceiling(root.ActualHeight + root.Margin.Top + root.Margin.Bottom), 96, 96, PixelFormats.Pbgra32);
        var background = new DrawingVisual();
        using (var drawing = background.RenderOpen()) drawing.DrawRectangle(window.Background, null, new Rect(0, 0, bitmap.Width, bitmap.Height));
        bitmap.Render(background); bitmap.Render(root);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var output = File.Create(path); encoder.Save(output);
    }
}
