using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Bs2Cooler.App;

internal static class ThemeSmoke
{
    public static async Task MainAsync(Window window)
    {
        CheckCaption(window);
        var tabs = (TabControl)window.FindName("ControlTabs");
        int selected = tabs.SelectedIndex;
        tabs.SelectedIndex = 1;
        await Task.Delay(250);
        await ComboAsync((ComboBox)window.FindName("TemperatureSource"), ".analysis/dark-combo-popup.png");
        await ComboAsync((ComboBox)window.FindName("CurvePreset"), null);
        var content = (FrameworkElement)tabs.Template.FindName("PART_SelectedContentHost", tabs);
        if (Math.Abs(content.Opacity - 1) > .001) throw new Exception("Tab animation did not settle.");
        tabs.SelectedIndex = selected;
        await Task.Delay(250);
        await MotionAsync((TextBlock)window.FindName("StatusText"));
        Console.WriteLine("PASS: main dark combo surfaces/items, keyboard selection, tab transition and status motion.");
    }

    public static async Task RgbAsync(Window editor)
    {
        CheckCaption(editor);
        var effect = (ComboBox)editor.FindName("EffectChoice");
        int originalEffect = effect.SelectedIndex;
        await ComboAsync(effect, ".analysis/dark-rgb-popup.png");
        effect.SelectedIndex = 1;
        await ComboAsync((ComboBox)editor.FindName("BreathingCount"), null);
        effect.SelectedIndex = originalEffect;
        var text = (TextBox)editor.FindName("Color0");
        CheckDark(text.Background); CheckLight(text.Foreground);
        text.Focus();
        if (text.Template.FindName("PART_ContentHost", text) is not ScrollViewer)
            throw new Exception("Styled text input has no editing host.");
        var slider = (Slider)editor.FindName("Brightness");
        slider.ApplyTemplate();
        if (slider.Template.FindName("PART_Track", slider) is not Track track || track.Thumb == null)
            throw new Exception("Styled slider is missing its track/thumb.");
        double value = slider.Value;
        Slider.DecreaseLarge.Execute(null, slider);
        if (value > slider.Minimum && slider.Value >= value) throw new Exception("Styled slider command failed.");
        slider.Value = value;
        var check = (CheckBox)editor.FindName("EnableLight");
        bool? wasChecked = check.IsChecked;
        check.IsChecked = true;
        if (((FrameworkElement)check.Template.FindName("Check", check)).Visibility != Visibility.Visible)
            throw new Exception("Styled checkbox check is missing.");
        check.IsChecked = wasChecked;
        await MotionAsync((TextBlock)editor.FindName("RgbStatus"));
        Console.WriteLine("PASS: RGB dark combos/input, checkbox glyph, slider command, caption and motion.");
    }

    private static async Task ComboAsync(ComboBox combo, string? imagePath)
    {
        if (!combo.IsVisible || !combo.IsEnabled) throw new Exception("Combo is not available for interaction: " + combo.Name);
        combo.ApplyTemplate(); CheckDark(combo.Background); CheckLight(combo.Foreground);
        int original = combo.SelectedIndex;
        try
        {
            combo.Focus(); combo.IsDropDownOpen = true;
            await Task.Delay(260); combo.UpdateLayout();
            var popup = (Popup)combo.Template.FindName("PART_Popup", combo);
            if (!popup.IsOpen || popup.Child is not Border surface || surface.ActualWidth < combo.ActualWidth)
                throw new Exception("Dark combo popup did not open at the correct width.");
            CheckDark(surface.Background);
            foreach (var item in combo.Items.OfType<ComboBoxItem>()) CheckLight(item.Foreground);
            if (imagePath != null) SaveImage(surface, imagePath);
            // Closed-combo arrow navigation commits immediately; an open list only moves its highlight.
            combo.IsDropDownOpen = false;
            await Task.Delay(50);
            var source = PresentationSource.FromVisual(combo)!;
            var key = new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, Key.Down) { RoutedEvent = Keyboard.KeyDownEvent };
            combo.RaiseEvent(key);
            if (combo.SelectedIndex != Math.Min(original + 1, combo.Items.Count - 1))
                throw new Exception($"Combo keyboard Down selection failed: {combo.Name}, {original} -> {combo.SelectedIndex}, handled={key.Handled}.");
        }
        finally { combo.IsDropDownOpen = false; combo.SelectedIndex = original; }
    }

    private static async Task MotionAsync(TextBlock text)
    {
        string original = text.Text;
        bool enabled = UiMotion.GetEnabled(text);
        try
        {
            text.Text = "Theme smoke status";
            if (SystemParameters.ClientAreaAnimation && !text.HasAnimatedProperties)
                throw new Exception("Status change did not start an animation.");
            await Task.Delay(280);
            if (Math.Abs(text.Opacity - 1) > .001) throw new Exception("Status animation did not settle.");
            // WPF retains a stopped clock after FillBehavior.Stop; isolate the next change from it.
            text.BeginAnimation(UIElement.OpacityProperty, null);
            UiMotion.SetEnabled(text, false);
            text.Text = "Theme smoke reduced motion";
            if (text.HasAnimatedProperties || Math.Abs(text.Opacity - 1) > .001)
                throw new Exception("Disabled motion still animates status.");
        }
        finally { text.Text = original; UiMotion.SetEnabled(text, enabled); }
    }

    private static void CheckDark(Brush brush)
    {
        if (brush is not SolidColorBrush solid || Math.Max(solid.Color.R, Math.Max(solid.Color.G, solid.Color.B)) > 90)
            throw new Exception("Expected dark control surface.");
    }
    private static void CheckLight(Brush brush)
    {
        if (brush is not SolidColorBrush solid || Math.Min(solid.Color.R, Math.Min(solid.Color.G, solid.Color.B)) < 160)
            throw new Exception("Expected readable light control text.");
    }
    private static void CheckCaption(Window window)
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            int result = DwmGetWindowAttribute(new WindowInteropHelper(window).Handle, 20, out int dark, sizeof(int));
            if (result != 0 || dark != 1) throw new Exception("Dark native caption was not enabled.");
        }
    }
    private static void SaveImage(FrameworkElement root, string path)
    {
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth), (int)Math.Ceiling(root.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var output = File.Create(path); encoder.Save(output);
    }
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(nint window, int attribute, out int value, int size);
}
