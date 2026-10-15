using System.Text;
using Bs2Cooler.Core;
using Bs2Cooler.Windows;

Console.OutputEncoding = Encoding.UTF8;
try
{
    string operation = args.FirstOrDefault() ?? "query";
    if (operation is not ("scan" or "query" or "exercise" or "set")) throw new ArgumentException("Use scan, query, exercise, or set <gear> <rpm> <rgb:on|off> <light:on|off>.");
    var devices = NativeHid.Enumerate();
    foreach (var item in devices)
        Console.WriteLine($"{item.Product}: 37D7:1002 usage={item.UsagePage:X4}:{item.Usage} input={item.InputLength} output={item.OutputLength} supported={item.IsSupported}");
    if (operation == "scan") return 0;
    var device = devices.FirstOrDefault(d => d.IsSupported) ?? throw new IOException("BS2 Pro USB device not found.");
    await using var connection = new Bs2Connection(device);
    connection.Disconnected += ex => Console.WriteLine("DISCONNECTED: " + ex.Message);
    string firmware = Bs2Protocol.ParseFirmware(await connection.SendAsync(Bs2Protocol.Query(Bs2Command.Firmware)));
    FanSpeed speed = await connection.ReadRpmAsync();
    int[] gears = Bs2Protocol.ParseGearSpeeds(await connection.SendAsync(Bs2Protocol.Query(Bs2Command.GearSpeeds)));
    Console.WriteLine($"Firmware={firmware} actual={speed.ActualRpm} target={speed.TargetRpm} gears={string.Join(',', gears)}");
    var until = DateTime.UtcNow.AddSeconds(4);
    while (connection.LatestStatus == null && DateTime.UtcNow < until) await Task.Delay(100);
    var status = connection.LatestStatus;
    Console.WriteLine(status == null ? "No heartbeat observed." : $"Heartbeat: gear={status.Gear} max={status.MaximumRpm} power={status.PowerMode} smart={status.IsSmart} rgb={status.RgbEnabled} gearLight={status.GearLightEnabled}");
    if (operation == "query") return 0;
    if (operation == "set")
    {
        if (args.Length != 5 || !int.TryParse(args[1], out int gear) || !int.TryParse(args[2], out int rpm)
            || args[3] is not ("on" or "off") || args[4] is not ("on" or "off")) throw new ArgumentException("set <gear> <rpm> <on|off> <on|off>");
        if (status == null) throw new InvalidOperationException("No live power status.");
        Bs2Protocol.SetGear(gear, rpm, status.MaximumRpm); // Validate every setting before sending any writes.
        await connection.SetManualAsync(gear, rpm);
        await connection.SetRgbAsync(args[3] == "on");
        await connection.SetGearLightAsync(args[4] == "on");
        Console.WriteLine($"Applied: gear={gear}, target={rpm}, RGB={args[3]}, gear light={args[4]}");
        return 0;
    }
    if (status == null || status.IsSmart || status.IsShutdown) throw new InvalidOperationException("Exercise requires a running manual-mode cooler with live heartbeat.");
    if (gears[status.Gear - 1] != speed.TargetRpm) throw new InvalidOperationException("Exercise requires a target matching its stored gear speed so it can be restored exactly.");
    int testRpm = speed.TargetRpm + 200 <= status.MaximumRpm ? speed.TargetRpm + 200 : speed.TargetRpm - 200;
    Bs2Protocol.ValidateRpm(testRpm, status.MaximumRpm);
    try
    {
        await connection.SetManualAsync(status.Gear, testRpm);
        var settleUntil = DateTime.UtcNow.AddSeconds(6);
        bool speedChanged = false;
        while (DateTime.UtcNow < settleUntil)
        {
            await Task.Delay(650);
            var changed = await connection.ReadRpmAsync();
            Console.WriteLine($"Speed exercise: requested={testRpm} actual={changed.ActualRpm} target={changed.TargetRpm}");
            if (changed.TargetRpm != testRpm) throw new InvalidOperationException("Device target RPM did not change.");
            if (Math.Abs(changed.ActualRpm - testRpm) <= 150 && Math.Abs(changed.ActualRpm - speed.ActualRpm) >= 100) { speedChanged = true; break; }
        }
        if (!speedChanged) throw new InvalidOperationException("Actual fan speed change not observed within six seconds.");
        await connection.SetRgbAsync(!status.RgbEnabled);
        Console.WriteLine("RGB opposite-state command acknowledged.");
        await connection.SetGearLightAsync(!status.GearLightEnabled);
        Console.WriteLine("Gear-light opposite-state command acknowledged.");
        await WaitForLights(!status.RgbEnabled, !status.GearLightEnabled);
        Console.WriteLine("Heartbeat confirms both light settings changed.");
    }
    finally
    {
        // Restore both the stored gear speed and the previous live target, even if the exercise fails.
        List<Exception> restoreErrors = [];
        async Task Restore(Func<Task> action) { try { await action(); } catch (Exception ex) { restoreErrors.Add(ex); } }
        await Restore(() => connection.SetManualAsync(status.Gear, speed.TargetRpm));
        await Restore(() => connection.SetRgbAsync(status.RgbEnabled));
        await Restore(() => connection.SetGearLightAsync(status.GearLightEnabled));
        await Task.Delay(500);
        var restored = await connection.ReadRpmAsync();
        Console.WriteLine($"Restored: actual={restored.ActualRpm} target={restored.TargetRpm} RGB={status.RgbEnabled} gearLight={status.GearLightEnabled}");
        if (restored.TargetRpm != speed.TargetRpm) throw new InvalidOperationException("Original target RPM was not restored.");
        if (restoreErrors.Count > 0) throw new AggregateException("One or more restore commands failed.", restoreErrors);
        await WaitForLights(status.RgbEnabled, status.GearLightEnabled);
        var restoredGears = Bs2Protocol.ParseGearSpeeds(await connection.SendAsync(Bs2Protocol.Query(Bs2Command.GearSpeeds)));
        if (!restoredGears.SequenceEqual(gears)) throw new InvalidOperationException("Stored gear speeds were not restored.");
        Console.WriteLine("Heartbeat confirms lighting restored; all four saved gear speeds match baseline.");
    }
    Console.WriteLine("PASS: native Windows HID read/write exercise completed; original settings restored.");
    return 0;

    async Task WaitForLights(bool rgb, bool light)
    {
        var deadline = DateTime.UtcNow.AddSeconds(4);
        while (DateTime.UtcNow < deadline)
        {
            if (connection.LatestStatus is { } latest && latest.RgbEnabled == rgb && latest.GearLightEnabled == light) return;
            await Task.Delay(100);
        }
        throw new InvalidOperationException("Lighting feedback did not match the requested state.");
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}
