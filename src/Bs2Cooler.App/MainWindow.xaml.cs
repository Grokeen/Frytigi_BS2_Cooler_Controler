using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Bs2Cooler.Core;
using Bs2Cooler.Windows;

namespace Bs2Cooler.App;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private Bs2Connection? _connection;
    private CoolerStatus? _status;
    private int[] _gearSpeeds = [1700, 2400, 3000, 4000];
    private int _selectedGear = 2;
    private bool _busy;
    private bool _polling;
    private bool _closing;
    private bool _draftInitialized;
    private int _failures;

    public MainWindow()
    {
        InitializeComponent();
        _timer.Tick += async (_, _) =>
        {
            if (_busy || _polling || _closing) return;
            if (_connection == null) await ConnectAsync();
            else await PollAsync();
        };
        UpdateControls();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) { await ConnectAsync(); _timer.Start(); }

    private async Task ConnectAsync()
    {
        if (_busy || _closing) return;
        _busy = true; UpdateControls();
        try
        {
            var devices = await Task.Run(NativeHid.Enumerate);
            var device = devices.FirstOrDefault(d => d.IsSupported);
            if (device == null)
            {
                ConnectionBadge.Text = "연결 대기";
                StatusText.Text = devices.Count > 0 ? "이 장치의 USB 형식은 아직 지원하지 않습니다." : "BS2 Pro를 데이터 USB 케이블로 연결해 주세요. 자동으로 다시 찾습니다.";
                return;
            }
            var connection = new Bs2Connection(device);
            _connection = connection;
            connection.StatusReceived += status => Dispatcher.BeginInvoke(() =>
            {
                if (ReferenceEquals(_connection, connection) && !_closing) ShowStatus(status);
            });
            connection.Disconnected += error => Dispatcher.BeginInvoke(async () =>
            {
                if (ReferenceEquals(_connection, connection) && !_closing) await DisconnectAsync(error.Message);
            });
            FirmwareText.Text = "펌웨어 " + Bs2Protocol.ParseFirmware(await connection.SendAsync(Bs2Protocol.Query(Bs2Command.Firmware)));
            _gearSpeeds = Bs2Protocol.ParseGearSpeeds(await connection.SendAsync(Bs2Protocol.Query(Bs2Command.GearSpeeds)));
            ShowSpeed(await connection.ReadRpmAsync());
            if (connection.LatestStatus is { } status) ShowStatus(status);
            ConnectionBadge.Text = "USB 연결됨";
            StatusText.Text = "연결되었습니다. 현재 속도를 읽고 있습니다.";
            _failures = 0;
        }
        catch (Exception ex) { await DisconnectAsync(ex.Message); }
        finally { _busy = false; UpdateControls(); }
    }

    private async Task PollAsync()
    {
        var connection = _connection;
        if (connection == null) return;
        _polling = true;
        try
        {
            var speed = await connection.ReadRpmAsync();
            if (ReferenceEquals(connection, _connection))
            {
                ShowSpeed(speed);
                if (connection.LatestStatus is { } status) ShowStatus(status);
                _failures = 0;
            }
        }
        catch (Exception ex)
        {
            if (ReferenceEquals(connection, _connection) && !_closing)
            {
                if (++_failures >= 2) await DisconnectAsync(ex.Message);
                else StatusText.Text = ex.Message;
            }
        }
        finally { _polling = false; UpdateControls(); }
    }

    private void ShowSpeed(FanSpeed speed)
    {
        ActualRpm.Text = speed.ActualRpm.ToString("N0", CultureInfo.InvariantCulture);
        TargetRpm.Text = $"{speed.TargetRpm:N0} RPM";
        LastUpdatedText.Text = "마지막 조회 " + DateTime.Now.ToString("HH:mm:ss");
        if (!_draftInitialized && speed.TargetRpm >= Bs2Protocol.MinimumRpm)
        {
            RpmSlider.Value = Math.Clamp(speed.TargetRpm, Bs2Protocol.MinimumRpm, RpmSlider.Maximum);
            RpmInput.Text = ((int)RpmSlider.Value).ToString(CultureInfo.InvariantCulture);
        }
    }

    private void ShowStatus(CoolerStatus status)
    {
        _status = status;
        RpmSlider.Maximum = status.MaximumRpm;
        SliderMaximum.Text = status.MaximumRpm.ToString(CultureInfo.InvariantCulture);
        PowerText.Text = status.PowerDescription;
        MaximumText.Text = $"최대 {status.MaximumRpm:N0} RPM";
        ModeText.Text = status.IsShutdown ? "장치 전원이 꺼져 있습니다" : status.IsSmart ? "현재 스마트 모드 · 적용하면 수동 모드로 전환" : $"현재 {status.Gear}단 · 수동 모드";
        RgbButton.Content = status.RgbEnabled ? "켜짐 · 끄기" : "꺼짐 · 켜기";
        GearLightButton.Content = status.GearLightEnabled ? "켜짐 · 끄기" : "꺼짐 · 켜기";
        if (!_draftInitialized)
        {
            _selectedGear = status.Gear;
            RpmSlider.Value = Math.Clamp(status.Speed.TargetRpm, Bs2Protocol.MinimumRpm, status.MaximumRpm);
            RpmInput.Text = ((int)RpmSlider.Value).ToString(CultureInfo.InvariantCulture);
            _draftInitialized = true;
        }
        UpdateControls();
    }

    private async Task DisconnectAsync(string reason)
    {
        var connection = _connection;
        _connection = null;
        _status = null;
        _draftInitialized = false;
        if (connection != null) await connection.DisposeAsync();
        ConnectionBadge.Text = "연결 대기";
        ActualRpm.Text = "—"; TargetRpm.Text = "— RPM";
        PowerText.Text = "확인 중"; MaximumText.Text = "USB 연결을 기다립니다";
        FirmwareText.Text = "펌웨어 —"; ModeText.Text = "장치 연결 후 설정할 수 있습니다";
        RgbButton.Content = "확인 중"; GearLightButton.Content = "확인 중";
        LastUpdatedText.Text = "";
        StatusText.Text = reason;
        UpdateControls();
    }

    private void UpdateControls()
    {
        bool live = _connection != null && _status != null && DateTimeOffset.Now - _status.ReceivedAt < TimeSpan.FromSeconds(6);
        bool enabled = live && _status?.IsShutdown == false && !_busy && !_closing;
        ApplyButton.IsEnabled = enabled;
        RgbButton.IsEnabled = enabled;
        GearLightButton.IsEnabled = enabled;
        RpmSlider.IsEnabled = enabled;
        RpmInput.IsEnabled = enabled;
        RefreshButton.IsEnabled = !_busy && !_closing;
        foreach (var button in new[] { Gear1, Gear2, Gear3, Gear4 })
        {
            button.IsEnabled = enabled;
            button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(button.Tag.ToString() == _selectedGear.ToString() ? "#356858" : "#2A3B52"));
        }
        ApplyButton.Content = $"{_selectedGear}단 · 속도 적용";
    }

    private void OnGearClick(object sender, RoutedEventArgs e)
    {
        _selectedGear = int.Parse(((Button)sender).Tag.ToString()!, CultureInfo.InvariantCulture);
        RpmSlider.Value = Math.Clamp(_gearSpeeds[_selectedGear - 1], Bs2Protocol.MinimumRpm, RpmSlider.Maximum);
        RpmInput.Text = ((int)RpmSlider.Value).ToString(CultureInfo.InvariantCulture);
        UpdateControls();
    }

    private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RpmInput != null) RpmInput.Text = ((int)e.NewValue).ToString(CultureInfo.InvariantCulture);
    }

    private async Task ApplyAsync()
    {
        if (!int.TryParse(RpmInput.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int rpm)) { StatusText.Text = "RPM을 숫자로 입력해 주세요."; return; }
        var connection = _connection;
        if (connection == null) return;
        int gear = _selectedGear;
        await RunActionAsync(async () =>
        {
            await connection.SetManualAsync(gear, rpm);
            _gearSpeeds[gear - 1] = rpm;
            RpmSlider.Value = rpm;
            ShowSpeed(await connection.ReadRpmAsync());
        }, $"{gear}단을 {rpm:N0} RPM으로 설정했습니다.");
    }

    private async Task RunActionAsync(Func<Task> action, string success)
    {
        if (_busy || _closing) return;
        _busy = true; UpdateControls();
        try { await action(); StatusText.Text = success; }
        catch (Exception ex) { StatusText.Text = ex.Message; }
        finally { _busy = false; UpdateControls(); }
    }

    private async void OnApplyClick(object sender, RoutedEventArgs e) => await ApplyAsync();
    private async void OnRpmKeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) { e.Handled = true; await ApplyAsync(); } }
    private async void OnRgbClick(object sender, RoutedEventArgs e)
    {
        if (_connection is not { } connection || _status is not { } status) return;
        await RunActionAsync(() => connection.SetRgbAsync(!status.RgbEnabled), status.RgbEnabled ? "RGB 조명을 껐습니다." : "RGB 조명을 켰습니다.");
    }
    private async void OnGearLightClick(object sender, RoutedEventArgs e)
    {
        if (_connection is not { } connection || _status is not { } status) return;
        await RunActionAsync(() => connection.SetGearLightAsync(!status.GearLightEnabled), status.GearLightEnabled ? "단계 표시등을 껐습니다." : "단계 표시등을 켰습니다.");
    }
    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        await DisconnectAsync("다시 연결하고 있습니다…");
        await ConnectAsync();
    }
    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_closing) return;
        e.Cancel = true; _closing = true; _timer.Stop(); UpdateControls();
        if (_connection != null) await _connection.DisposeAsync();
        Close();
    }
}
