using System.Diagnostics;
using System.Windows;
using Bs2Cooler.Windows;

namespace Bs2Cooler.App;

public partial class App : Application
{
    private Mutex? _instance;
    private bool _ownsInstance;
    private TrayController? _tray;
    private DesktopServer? _desktop;
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            DesktopPreferencesStore.Log("Unhandled UI error: " + args.Exception);
            if (MainWindow?.IsVisible == true) MessageBox.Show(args.Exception.Message, "BS2 Cooler", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        try
        {
            if (e.Args.Contains("--enable-startup") || e.Args.Contains("--disable-startup"))
            { StartupTaskManager.SetEnabled(e.Args.Contains("--enable-startup")); Shutdown(); return; }
            if (e.Args.FirstOrDefault(a => a.StartsWith("--startup-cooling=")) is { } option)
            {
                bool enabled = option == "--startup-cooling=on";
                if (!enabled && option != "--startup-cooling=off") throw new ArgumentException("Use --startup-cooling=on or off.");
                DesktopPreferencesStore.Save(DesktopPreferencesStore.Load() with { AutoStartCooling = enabled }); Shutdown(); return;
            }
            int replace = Array.IndexOf(e.Args, "--replace");
            if (replace >= 0 && replace + 1 < e.Args.Length && int.TryParse(e.Args[replace + 1], out int previousId) && previousId != Environment.ProcessId)
            {
                try { using var previous = Process.GetProcessById(previousId); await previous.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(20)); }
                catch (ArgumentException) { }
            }
            // An existing elevated instance exposes only these fixed commands to the same desktop.
            // Check it before opening a mutex that may have been created with a higher integrity level.
            if (DesktopIpc.ReadStatus() is not null)
            {
                DesktopIpc.Send(e.Args.Contains("--quit") ? DesktopCommand.Exit : e.Args.Contains("--background") ? DesktopCommand.Status : DesktopCommand.Show, out _);
                Shutdown(); return;
            }
            _instance = new Mutex(false, @"Local\Bs2Cooler." + StartupTaskManager.UserSid);
            try { _ownsInstance = _instance.WaitOne(0); } catch (AbandonedMutexException) { _ownsInstance = true; }
            if (!_ownsInstance)
            {
                var command = e.Args.Contains("--quit") ? DesktopCommand.Exit : e.Args.Contains("--background") ? DesktopCommand.Status : DesktopCommand.Show;
                for (int i = 0; i < 20 && !DesktopIpc.Send(command, out _); i++) await Task.Delay(100);
                Shutdown(); return;
            }
            if (e.Args.Contains("--quit")) { Shutdown(); return; }
            if (!StartupTaskManager.IsAdministrator && string.Equals(Environment.ProcessPath, StartupTaskManager.InstalledExecutable, StringComparison.OrdinalIgnoreCase)
                && StartupTaskManager.IsEnabled())
            {
                _instance.ReleaseMutex(); _ownsInstance = false;
                StartupTaskManager.Run();
                for (int i = 0; i < 150; i++)
                {
                    if (DesktopIpc.ReadStatus() is not null) { DesktopIpc.Send(DesktopCommand.Show, out _); Shutdown(); return; }
                    await Task.Delay(100);
                }
                throw new TimeoutException("자동 실행 작업이 시작되지 않았습니다. 작업 스케줄러의 BS2 Cooler 상태를 확인해 주세요.");
            }
            var window = new MainWindow(true);
            MainWindow = window;
            _desktop = new DesktopServer(window);
            _tray = new TrayController(window);
            window.TrayAvailable = true;
            window.Closed += (_, _) => Shutdown();
            SessionEnding += (_, _) => { _ = window.RequestExitAsync(); };
            _ = window.InitializeDesktopAsync(e.Args.Contains("--startup"));
            if (!e.Args.Contains("--background")) window.ShowControlWindow();
            DesktopPreferencesStore.Log($"Started {typeof(App).Assembly.GetName().Version}; background={e.Args.Contains("--background")}; elevated={StartupTaskManager.IsAdministrator}.");
        }
        catch (Exception ex)
        {
            DesktopPreferencesStore.Log("Startup failed: " + ex);
            if (!e.Args.Contains("--background")) MessageBox.Show(ex.Message, "BS2 Cooler", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose(); _desktop?.Dispose();
        if (_ownsInstance) _instance?.ReleaseMutex();
        _instance?.Dispose();
        DesktopPreferencesStore.Log("Exited: " + e.ApplicationExitCode);
        base.OnExit(e);
    }
}
