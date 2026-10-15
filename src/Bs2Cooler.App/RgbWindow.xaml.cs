using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Bs2Cooler.Core;
using Bs2Cooler.Windows;

namespace Bs2Cooler.App;

public partial class RgbWindow : Window
{
    private readonly Func<RgbSettings, bool, Task> _apply;
    private bool _ready;
    private bool _applying;
    public RgbWindow(Func<RgbSettings, bool, Task> apply, bool enabled)
    {
        _apply = apply;
        InitializeComponent();
        EnableLight.IsChecked = enabled;
        RgbSettings settings = RgbSettings.Default;
        try
        {
            if (RgbSettingsStore.Load() is { } saved) { settings = saved; RgbStatus.Text = "이 PC의 마지막 적용값을 불러왔습니다."; }
            else RgbStatus.Text = "기본 제안값입니다. 적용하기 전에는 장치를 변경하지 않습니다.";
        }
        catch (Exception ex) { RgbStatus.Text = "저장값을 읽지 못해 기본값을 표시합니다. " + ex.Message; }
        _ready = true;
        LoadSettings(settings);
    }
    private TextBox ColorInput(int index) => (TextBox)FindName($"Color{index}");
    private void LoadSettings(RgbSettings settings)
    {
        EffectChoice.SelectedIndex = (int)settings.Effect;
        Brightness.Value = settings.Brightness; EffectSpeed.Value = settings.Speed;
        BreathingCount.SelectedIndex = settings.BreathingColors - 1;
        for (int i = 0; i < 6; i++) ColorInput(i).Text = settings.Colors[i].Hex;
        UpdateEffect();
    }
    private void UpdateEffect()
    {
        if (!_ready) return;
        var effect = (RgbEffect)EffectChoice.SelectedIndex;
        EffectSpeed.IsEnabled = effect is not (RgbEffect.Solid or RgbEffect.Individual);
        BreathingCount.IsEnabled = effect == RgbEffect.Breathing;
        for (int i = 0; i < 6; i++) ColorInput(i).IsEnabled = effect != RgbEffect.Rainbow;
        ColorHint.Text = effect switch
        {
            RgbEffect.Rainbow => "무지개는 고정된 7색을 사용합니다.",
            RgbEffect.Breathing => "앞에서 선택한 개수의 색상을 차례로 어둡게/밝게 반복합니다.",
            RgbEffect.Solid or RgbEffect.Wave or RgbEffect.Flash => "색상 1을 사용합니다. 나머지 색상은 다른 효과를 위해 보관합니다.",
            _ => "색상 1~6을 LED 순서로 사용합니다. LED 번호는 프로토콜 순서입니다."
        };
    }
    private void OnEffectChanged(object sender, SelectionChangedEventArgs e) => UpdateEffect();
    private void OnColorChanged(object sender, TextChangedEventArgs e)
    {
        if (!_ready) return;
        for (int i = 0; i < 6; i++)
        {
            var swatch = (Border)FindName($"Swatch{i}");
            try { var color = RgbColor.Parse(ColorInput(i).Text); swatch.Background = new SolidColorBrush(Color.FromRgb(color.Red, color.Green, color.Blue)); }
            catch (FormatException) { swatch.Background = Brushes.Transparent; }
        }
    }
    private async void OnApplyClick(object sender, RoutedEventArgs e)
    {
        if (_applying) return;
        try
        {
            var settings = new RgbSettings((RgbEffect)EffectChoice.SelectedIndex, (int)Brightness.Value, (int)EffectSpeed.Value,
                BreathingCount.SelectedIndex + 1, Enumerable.Range(0, 6).Select(i => RgbColor.Parse(ColorInput(i).Text)).ToArray());
            settings.Validate();
            _applying = true; EditorPanel.IsEnabled = false; ApplyRgb.IsEnabled = false;
            RgbStatus.Text = "조명 설정을 전송하고 있습니다…";
            await _apply(settings, EnableLight.IsChecked == true);
            try { RgbSettingsStore.Save(settings); RgbStatus.Text = "조명 설정을 적용하고 이 PC에 저장했습니다."; }
            catch (Exception ex) { RgbStatus.Text = "장치 적용 완료 · PC 저장 실패: " + ex.Message; }
        }
        catch (Exception ex) { RgbStatus.Text = ex.Message; }
        finally { _applying = false; EditorPanel.IsEnabled = true; ApplyRgb.IsEnabled = true; }
    }
    private void OnDefaultsClick(object sender, RoutedEventArgs e) { LoadSettings(RgbSettings.Default); RgbStatus.Text = "기본 제안값입니다. 적용하면 장치에 전송합니다."; }
    private void OnLoadClick(object sender, RoutedEventArgs e)
    {
        try { LoadSettings(RgbSettingsStore.Load() ?? throw new InvalidOperationException("이 PC에 저장된 적용값이 없습니다.")); RgbStatus.Text = "마지막 적용값을 불러왔습니다. 적용 전에는 장치를 변경하지 않습니다."; }
        catch (Exception ex) { RgbStatus.Text = ex.Message; }
    }
    private void OnClosing(object? sender, CancelEventArgs e) { if (_applying) e.Cancel = true; }
}
