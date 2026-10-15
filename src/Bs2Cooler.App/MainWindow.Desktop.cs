using System.Windows;
using Bs2Cooler.Core;
using Bs2Cooler.Windows;

namespace Bs2Cooler.App;

public partial class MainWindow
{
    private bool _trayMode, _exitRequested, _desktopReady, _initialized;
    private Task? _initializeTask;
    private bool _startupAutomaticPending;
    private DateTimeOffset _startupRetryAt;
    private DesktopPreferences _desktopPreferences = new();
    private readonly TaskCompletionSource _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool TrayAvailable { get; set; }
    public bool AutomaticActive => _automatic;
    public bool StartupCoolingEnabled => _desktopPreferences.AutoStartCooling;

    private void ConfigureDesktopMode(bool trayMode)
    {
        _trayMode = trayMode;
        if (trayMode)
        {
            try { _desktopPreferences = DesktopPreferencesStore.Load(); }
            catch (Exception ex) { DesktopPreferencesStore.Log("Settings load failed: " + ex.Message); }
            TemperatureSource.SelectedIndex = _desktopPreferences.Scope == TemperatureScope.CpuAndGpu ? 0 : 1;
            CurvePreset.SelectedIndex = _desktopPreferences.Preset == CoolingPreset.Performance ? 0 : 1;
            StateChanged += (_, _) => { if (WindowState == WindowState.Minimized && !_closing) HideToTray(); };
        }
        Closed += (_, _) => _closed.TrySetResult();
        _desktopReady = true;
    }

    public Task InitializeDesktopAsync(bool startup = false) => _initializeTask ??= InitializeCoreAsync(startup);
    private async Task InitializeCoreAsync(bool startup)
    {
        _startupAutomaticPending = startup && _desktopPreferences.AutoStartCooling;
        await ConnectAsync(); _timer.Start(); _initialized = true;
    }

    private void SaveDesktopSelection()
    {
        if (!_trayMode || !_desktopReady) return;
        _desktopPreferences = _desktopPreferences with { Scope = SelectedScope, Preset = SelectedPreset };
        try { DesktopPreferencesStore.Save(_desktopPreferences); }
        catch (Exception ex) { DesktopPreferencesStore.Log("Settings save failed: " + ex.Message); }
    }
    public void SetStartupCooling(bool enabled)
    {
        _desktopPreferences = _desktopPreferences with { AutoStartCooling = enabled, Scope = SelectedScope, Preset = SelectedPreset };
        DesktopPreferencesStore.Save(_desktopPreferences);
        if (!enabled) _startupAutomaticPending = false;
    }

    private async Task TryStartupAutomaticAsync()
    {
        if (!_startupAutomaticPending || _busy || _polling || _closing || _automatic || _connection == null || _status == null
            || _status.IsShutdown || !SensorsReady() || DateTimeOffset.Now < _startupRetryAt) return;
        _startupRetryAt = DateTimeOffset.Now.AddSeconds(5);
        await RunActionAsync(StartAutomaticAsync, "로그인 자동 실행: 온도 기반 제어를 시작했습니다.");
        if (_automatic) { _startupAutomaticPending = false; DesktopPreferencesStore.Log("Startup cooling started."); }
    }

    public void HideToTray() { if (!_closing) Hide(); }
    public void ShowControlWindow()
    {
        if (_closing) return;
        Show(); WindowState = WindowState.Normal; Activate();
    }
    public async Task RequestExitAsync()
    {
        _exitRequested = true; Close(); await _closed.Task;
    }
    public async Task ToggleAutomaticFromTrayAsync()
    {
        _startupAutomaticPending = false;
        bool stopping = _automatic;
        await RunActionAsync(stopping ? StopAutomaticAsync : StartAutomaticAsync,
            stopping ? "자동 제어 중지 · 기존 설정 복원" : "온도 자동 제어 시작");
    }

    public nint HandleDesktopCommand(DesktopCommand command)
    {
        switch (command)
        {
            case DesktopCommand.Show: ShowControlWindow(); return 1;
            case DesktopCommand.Hide: HideToTray(); return 1;
            case DesktopCommand.CloseToTray: Close(); return 1;
            case DesktopCommand.Exit: _ = RequestExitAsync(); return 1;
            case DesktopCommand.Status:
                return 0x4000 | (IsVisible ? 1 : 0) | (_connection != null && _status != null ? 2 : 0) | (_automatic ? 4 : 0)
                    | (StartupTaskManager.IsAdministrator ? 8 : 0) | (_initialized ? 16 : 0) | (TrayAvailable ? 32 : 0);
            case DesktopCommand.Temperatures:
                var now = DateTimeOffset.Now;
                int cpu = _temperatures.Cpu?.IsUsable(now) == true ? (int)Math.Round(_temperatures.Cpu.Celsius) + 1 : 0;
                int gpu = _temperatures.Gpu?.IsUsable(now) == true ? (int)Math.Round(_temperatures.Gpu.Celsius) + 1 : 0;
                return cpu | (gpu << 8);
            default: return 0;
        }
    }
    public string TraySummary()
    {
        var temperatures = TemperatureLabels();
        return $"BS2 Cooler · CPU {temperatures.Cpu} / GPU {temperatures.Gpu}\n{(_automatic ? "자동 제어" : _startupAutomaticPending ? "자동 제어 대기" : "수동 모드")} · {_status?.Speed.TargetRpm ?? 0} RPM";
    }
}
