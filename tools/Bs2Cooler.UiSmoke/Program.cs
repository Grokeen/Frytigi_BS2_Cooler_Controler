using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        int result = 1;
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Bs2Cooler;component/Styles.xaml", UriKind.Relative) });
        var window = new Bs2Cooler.App.MainWindow { WindowStartupLocation = WindowStartupLocation.Manual, Left = -12000, Top = -12000, ShowInTaskbar = false };
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
                Console.WriteLine($"UI: {badge}, actual={rpm}, {firmware}, applyEnabled={enabled}");
                if (badge != "USB 연결됨" || rpm == "—" || !enabled) throw new Exception("Live UI did not become ready.");
                window.UpdateLayout();
                var root = (FrameworkElement)window.Content;
                CheckLayout(window, root);
                var bitmap = new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth + 56), (int)Math.Ceiling(root.ActualHeight + 48), 96, 96, PixelFormats.Pbgra32);
                var background = new DrawingVisual();
                using (var drawing = background.RenderOpen()) drawing.DrawRectangle(window.Background, null, new Rect(0, 0, bitmap.Width, bitmap.Height));
                bitmap.Render(background);
                bitmap.Render(root);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                string path = Path.GetFullPath(args.FirstOrDefault() ?? ".analysis/ui-preview.png");
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using (var output = File.Create(path)) encoder.Save(output);
                window.Width = window.MinWidth; window.Height = window.MinHeight;
                await Task.Delay(200); window.UpdateLayout();
                CheckLayout(window, (FrameworkElement)window.Content);
                Console.WriteLine("PASS: WPF startup, live device data, controls and layout rendered: " + path);
                result = 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); }
            finally { window.Close(); app.Shutdown(result); }
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
