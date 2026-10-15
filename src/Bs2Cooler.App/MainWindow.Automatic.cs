using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Threading;
using Bs2Cooler.Core;
using Bs2Cooler.Windows;

namespace Bs2Cooler.App;

public partial class MainWindow
{
    private readonly TemperatureMonitor _temperatureMonitor = new();
    private readonly DispatcherTimer _temperatureTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly ThermalControl _thermalControl = new();
    private readonly SemaphoreSlim _automaticGate = new(1, 1);
    private TemperatureSnapshot _temperatures = new(null, null, "확인 중", "확인 중");
    private bool _readingTemperatures;
    private bool _automatic;
    private CoolerStatus? _beforeAutomatic;
    private int _beforeAutomaticTarget;

    private TemperatureScope SelectedScope => TemperatureSource.SelectedIndex == 0 ? TemperatureScope.CpuAndGpu : TemperatureScope.GpuOnly;
    private CoolingPreset SelectedPreset => CurvePreset.SelectedIndex == 0 ? CoolingPreset.Performance : CoolingPreset.Quiet;

    private void InitializeTemperatureMonitoring()
    {
        _temperatureTimer.Tick += async (_, _) => await RefreshTemperaturesAsync();
        _temperatureTimer.Start();
        _ = RefreshTemperaturesAsync();
    }

    private async Task RefreshTemperaturesAsync()
    {
        if (_readingTemperatures || _closing) return;
        _readingTemperatures = true;
        try
        {
            _temperatures = await Task.Run(_temperatureMonitor.Read);
            if (_closing) return;
            CpuTemperature.Text = _temperatures.Cpu is { } cpu ? $"{cpu.Celsius:F0}°C" : "—";
            GpuTemperature.Text = _temperatures.Gpu is { } gpu ? $"{gpu.Celsius:F0}°C" : "—";
            SensorDescription.Text = _temperatures.CpuElevationRequired ? "CPU 온도에 관리자 권한 필요" : (_temperatures.Cpu != null, _temperatures.Gpu != null) switch
            {
                (true, true) => "CPU/GPU 온도 수집 중",
                (false, true) => "CPU 센서 없음 · GPU 기준 사용 가능",
                (true, false) => "GPU 센서를 읽을 수 없음",
                _ => "온도 센서를 읽을 수 없음"
            };
            CpuTemperature.ToolTip = _temperatures.CpuStatus;
            GpuTemperature.ToolTip = _temperatures.GpuStatus;
        }
        catch (Exception ex)
        {
            _temperatures = new(null, null, ex.Message, ex.Message);
            CpuTemperature.Text = "—"; GpuTemperature.Text = "—";
            SensorDescription.Text = "센서 조회 실패 · 자동 제어 시 최대 풍량";
        }
        finally { _readingTemperatures = false; UpdateControls(); }
    }

    private bool SensorsReady()
    {
        var now = DateTimeOffset.Now;
        return _temperatures.Gpu?.IsUsable(now) == true
            && (SelectedScope == TemperatureScope.GpuOnly || _temperatures.Cpu?.IsUsable(now) == true);
    }

    private async Task AutomaticStepAsync()
    {
        await _automaticGate.WaitAsync();
        try
        {
            if (!_automatic || _connection is not { } connection || connection.LatestStatus is not { } status) return;
            if (!status.IsSmart || status.IsShutdown)
            {
                _automatic = false;
                AutoState.Text = "장치의 수동 전환/전원 끄기로 자동 제어 중지";
                return;
            }
            var decision = _thermalControl.Evaluate(_temperatures, SelectedScope, SelectedPreset, status.MaximumRpm, DateTimeOffset.Now);
            AutoState.Text = decision.Temperature is { } value
                ? $"기준 {value:F0}°C → {decision.Rpm:N0} RPM · {decision.Description}"
                : $"{decision.Rpm:N0} RPM · {decision.Description}";
            // Refresh the volatile target each control tick, including while the motor ramps.
            await connection.SetAutomaticRpmAsync(decision.Rpm);
        }
        finally { _automaticGate.Release(); UpdateControls(); }
    }

    private async Task StartAutomaticAsync()
    {
        if (_connection is not { } connection || _status is not { } status) return;
        if (!SensorsReady()) throw new InvalidOperationException("선택한 CPU/GPU 온도 센서를 먼저 확인해 주세요.");
        _beforeAutomatic = status;
        _beforeAutomaticTarget = (await connection.ReadRpmAsync()).TargetRpm;
        try
        {
            await connection.EnterAutomaticAsync();
            _thermalControl.Reset(); _automatic = true;
            await AutomaticStepAsync();
        }
        catch
        {
            _automatic = false;
            await RestoreBeforeAutomaticAsync(connection);
            throw;
        }
    }

    private async Task StopAutomaticAsync()
    {
        _automatic = false;
        await _automaticGate.WaitAsync();
        try
        {
            if (_connection is { } connection) await RestoreBeforeAutomaticAsync(connection);
            AutoState.Text = "자동 제어 중지 · 이전 설정 복원";
            _draftInitialized = false;
        }
        finally { _automaticGate.Release(); UpdateControls(); }
    }

    private async Task RestoreBeforeAutomaticAsync(Bs2Connection connection)
    {
        if (_beforeAutomatic is not { } before) return;
        int maximum = connection.LatestStatus?.MaximumRpm ?? before.MaximumRpm;
        int target = Math.Clamp(_beforeAutomaticTarget, Bs2Protocol.MinimumRpm, maximum);
        if (before.IsSmart) await connection.SetAutomaticRpmAsync(target);
        else
        {
            await connection.ExitAutomaticAsync();
            var restored = await connection.ReadRpmAsync();
            if (restored.TargetRpm != target) await connection.SetManualAsync(before.Gear, target);
        }
        _beforeAutomatic = null;
    }

    private async void OnAutomaticClick(object sender, RoutedEventArgs e)
    {
        _startupAutomaticPending = false;
        bool stopping = _automatic;
        await RunActionAsync(stopping ? StopAutomaticAsync : StartAutomaticAsync,
            stopping ? "자동 제어를 중지하고 이전 설정을 복원했습니다." : "온도 기반 자동 제어를 시작했습니다.");
    }

    private void OnAutomaticSettingsChanged(object sender, SelectionChangedEventArgs e)
    {
        _thermalControl.Reset();
        if (AutoButton == null) return;
        SaveDesktopSelection();
        if (!_automatic) AutoState.Text = "온도 센서를 확인하고 있습니다.";
        UpdateControls();
    }

    private async void OnCpuPermissionClick(object sender, RoutedEventArgs e)
    {
        bool started = false;
        await RunActionAsync(async () =>
        {
            if (_automatic) await StopAutomaticAsync();
            string executable = Environment.ProcessPath ?? throw new IOException("실행 파일 경로를 확인할 수 없습니다.");
            using var process = Process.Start(new ProcessStartInfo(executable)
            {
                UseShellExecute = true,
                Verb = "runas",
                Arguments = "--replace " + Environment.ProcessId,
                WorkingDirectory = AppContext.BaseDirectory
            }) ?? throw new IOException("관리자 권한 앱을 시작하지 못했습니다.");
            started = true;
        }, "CPU 온도 확인 권한으로 다시 실행합니다.");
        if (started) await RequestExitAsync();
    }

    private void UpdateAutomaticControls(bool connected)
    {
        AutoButton.Content = _automatic ? "자동 제어 중지" : "자동 제어 시작";
        AutoButton.IsEnabled = connected && !_busy && !_closing && (_automatic || SensorsReady());
        TemperatureSource.IsEnabled = !_busy && !_closing;
        CurvePreset.IsEnabled = !_busy && !_closing;
        CpuPermissionButton.Visibility = _temperatures.CpuElevationRequired ? Visibility.Visible : Visibility.Collapsed;
        CpuPermissionButton.IsEnabled = !_busy && !_closing;
        if (!_automatic && !SensorsReady()) AutoState.Text = SelectedScope == TemperatureScope.CpuAndGpu
            ? _temperatures.CpuElevationRequired ? "CPU 온도 권한을 허용한 뒤 시작해 주세요." : "CPU 센서가 필요합니다. GPU 기준을 선택할 수도 있습니다."
            : "GPU 온도 센서를 기다리고 있습니다.";
        else if (!_automatic && AutoState.Text.Contains("센서"))
            AutoState.Text = SelectedScope == TemperatureScope.CpuAndGpu ? "CPU/GPU 중 높은 온도 기준 · 시작 준비됨" : "GPU 온도 기준 · 시작 준비됨";
    }
}
