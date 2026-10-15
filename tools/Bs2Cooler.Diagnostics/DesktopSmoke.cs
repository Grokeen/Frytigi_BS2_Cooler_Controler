using System.Diagnostics;
using Bs2Cooler.Core;
using Bs2Cooler.Windows;

internal static class DesktopSmoke
{
    public static async Task RunAsync()
    {
        if (!StartupTaskManager.IsEnabled()) throw new Exception("Install and register the desktop app first.");
        var preferences = DesktopPreferencesStore.Load();
        var baseline = await ReadFanAsync();
        try
        {
            var original = await WaitAsync(s => s is { Initialized: true, Connected: true, Tray: true, Elevated: true });
            if (original!.Visible) throw new Exception("Startup did not stay in the background.");
            Console.WriteLine($"BACKGROUND: PID={original.ProcessId}, CPU={original.Cpu}, GPU={original.Gpu}, elevated={original.Elevated}, tray={original.Tray}");
            using var duplicate = Process.Start(new ProcessStartInfo(StartupTaskManager.InstalledExecutable) { UseShellExecute = false })!;
            await duplicate.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));
            var shown = await WaitAsync(s => s?.Visible == true);
            if (shown!.ProcessId != original.ProcessId || Process.GetProcessesByName("Bs2Cooler").Count(p => p.SessionId == Process.GetCurrentProcess().SessionId) != 1)
                throw new Exception("A second app instance was created.");
            DesktopIpc.Send(DesktopCommand.Hide, out _);
            await WaitAsync(s => s?.Visible == false);
            Console.WriteLine("PASS: standard-user relaunch restores the elevated existing window; one instance only.");
            await StopAsync();

            using var shortcutLaunch = Process.Start(new ProcessStartInfo(StartupTaskManager.InstalledExecutable) { UseShellExecute = false })!;
            await shortcutLaunch.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(20));
            await WaitAsync(s => s is { Initialized: true, Visible: true, Elevated: true, Tray: true });
            if (Process.GetProcessesByName("Bs2Cooler").Count(p => p.SessionId == Process.GetCurrentProcess().SessionId) != 1)
                throw new Exception("Shortcut launch created more than one app instance.");
            Console.WriteLine("PASS: standard-user launch with no existing instance starts the registered elevated app and opens its window.");
            await StopAsync();

            DesktopPreferencesStore.Save(preferences with { AutoStartCooling = true, Scope = TemperatureScope.CpuAndGpu, Preset = CoolingPreset.Performance });
            StartupTaskManager.Run();
            var automatic = await WaitAsync(s => s is { Automatic: true, Visible: false, Tray: true } && s.Cpu != null && s.Gpu != null);
            Console.WriteLine($"STARTUP AUTO: CPU={automatic!.Cpu}, GPU={automatic.Gpu}, automatic={automatic.Automatic}, hidden={!automatic.Visible}");
            DesktopIpc.Send(DesktopCommand.Show, out _);
            await WaitAsync(s => s?.Visible == true);
            DesktopIpc.Send(DesktopCommand.CloseToTray, out _);
            await WaitAsync(s => s is { Visible: false, Automatic: true });
            await Task.Delay(9000); // Beyond the sensor freshness limit: hidden processing must continue.
            var hidden = DesktopIpc.ReadStatus();
            if (hidden is not { Automatic: true, Visible: false, Cpu: not null, Gpu: not null }) throw new Exception("Closing the window stopped background control or sensing.");
            Console.WriteLine("PASS: close-to-tray preserves automatic control and fresh CPU/GPU readings.");
            await StopAsync();
            var restored = await ReadFanAsync();
            if (baseline.Target != restored.Target || !baseline.Gears.SequenceEqual(restored.Gears)) throw new Exception("Explicit tray exit failed to restore the original fan target/presets.");
            Console.WriteLine($"PASS: explicit exit restored target={restored.Target} and all four presets.");
        }
        finally
        {
            await StopAsync();
            DesktopPreferencesStore.Save(preferences);
            StartupTaskManager.Run();
            await WaitAsync(s => s is { Initialized: true, Tray: true, Visible: false });
        }
        Console.WriteLine("PASS: desktop startup/tray lifecycle completed; original startup preferences restored and background app restarted.");
    }
    private static async Task StopAsync()
    {
        if (DesktopIpc.ReadStatus() == null) return;
        DesktopIpc.Send(DesktopCommand.Exit, out _);
        await WaitAsync(s => s == null);
        await Task.Delay(200);
    }
    private static async Task<DesktopProcessStatus?> WaitAsync(Func<DesktopProcessStatus?, bool> condition)
    {
        var until = DateTime.UtcNow.AddSeconds(20);
        do
        {
            var state = DesktopIpc.ReadStatus();
            if (condition(state)) return state;
            await Task.Delay(150);
        } while (DateTime.UtcNow < until);
        throw new Exception("Desktop state transition timed out.");
    }
    private static async Task<(int Target, int[] Gears)> ReadFanAsync()
    {
        var device = NativeHid.Enumerate().First(d => d.IsSupported);
        await using var connection = new Bs2Connection(device);
        int target = (await connection.ReadRpmAsync()).TargetRpm;
        int[] gears = Bs2Protocol.ParseGearSpeeds(await connection.SendAsync(Bs2Protocol.Query(Bs2Command.GearSpeeds)));
        return (target, gears);
    }
}
