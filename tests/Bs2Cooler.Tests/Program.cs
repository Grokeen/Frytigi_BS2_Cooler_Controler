using Bs2Cooler.Core;

int passed = 0;
Run("Known captured RPM query", () => Equal("005AA52202240000000000000000000000000000000000000000000000000000", Convert.ToHexString(Bs2Protocol.Query(Bs2Command.Rpm))));
Run("Known gear-2 / 1700 RPM vector", () => Equal("005AA5260501A406D60000000000000000000000000000000000000000000000", Convert.ToHexString(Bs2Protocol.SetGear(2, 1700, 2700))));
byte[] capture = Convert.FromHexString("00035AA52206FC0860099500000000000000000000000000000000000000000000");
Run("Decode captured Windows HID RPM response", () =>
{
    var speed = Bs2Protocol.ParseRpm(Decode(capture));
    Equal(2300, speed.ActualRpm); Equal(2400, speed.TargetRpm);
});
Run("Decode all supported framing layers", () =>
{
    Equal((byte)0x22, Decode(capture.AsSpan(1).ToArray()).Command);
    Equal((byte)0x22, Decode(capture.AsSpan(2).ToArray()).Command);
});
Run("Captured heartbeat and power limit", () =>
{
    var status = Bs2Protocol.ParseStatus(Decode(Convert.FromHexString("00035AA5EF0B32000560096009B400B70000000000000000000000000000000000")));
    Equal(2400, status.Speed.ActualRpm); Equal(2, status.Gear); Equal(2700, status.MaximumRpm);
    Equal(false, status.IsSmart); Equal(true, status.RgbEnabled); Equal(true, status.GearLightEnabled);
    Equal(3300, (status with { PowerMode = 2 }).MaximumRpm);
    Equal(4000, (status with { PowerMode = 3 }).MaximumRpm);
    Equal(2700, (status with { PowerMode = 0 }).MaximumRpm);
});
Run("Truncated reports rejected without exceptions", () =>
{
    for (int length = 0; length < 11; length++) Equal(false, Bs2Protocol.TryDecode(capture.AsSpan(0, length), out _));
});
Run("Corrupt bytes and length rejected", () =>
{
    foreach (int index in new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })
    {
        byte[] corrupt = (byte[])capture.Clone(); corrupt[index] ^= 0x80;
        Equal(false, Bs2Protocol.TryDecode(corrupt, out _));
    }
});
Run("Reject wrong response command and short payload", () =>
{
    Throws<InvalidDataException>(() => Bs2Protocol.ParseRpm(new(0x01, new byte[4])));
    Throws<InvalidDataException>(() => Bs2Protocol.ParseStatus(new(0xEF, new byte[8])));
});
Run("Do not allow out-of-range speed or gear", () =>
{
    foreach (int rpm in new[] { -1, 0, 1299, 2701, 4000, int.MaxValue }) Throws<ArgumentOutOfRangeException>(() => Bs2Protocol.SetGear(2, rpm, 2700));
    foreach (int gear in new[] { -1, 0, 5, 255 }) Throws<ArgumentOutOfRangeException>(() => Bs2Protocol.SetGear(gear, 1700, 2700));
    Throws<ArgumentOutOfRangeException>(() => Bs2Protocol.SetGear(2, 1700, 65535));
});
Run("No arbitrary or firmware-update query", () => Throws<ArgumentOutOfRangeException>(() => Bs2Protocol.Query((Bs2Command)0xDF)));
Run("Device refusal must be an error", () =>
{
    Bs2Protocol.RequireSuccess(new(0x26, [1]), Bs2Command.SetGear);
    Bs2Protocol.RequireSuccess(new(0x24, [2]), Bs2Command.ExitSmart);
    Throws<IOException>(() => Bs2Protocol.RequireSuccess(new(0x26, [0]), Bs2Command.SetGear));
    Throws<IOException>(() => Bs2Protocol.RequireSuccess(new(0x26, [2]), Bs2Command.SetGear));
});
var thermalNow = DateTimeOffset.UtcNow;
TemperatureReading Reading(double value, int age = 0) => new(value, "test fixture", thermalNow.AddSeconds(-age));
TemperatureSnapshot Sample(double? cpu, double? gpu) => new(cpu.HasValue ? Reading(cpu.Value) : null, gpu.HasValue ? Reading(gpu.Value) : null, "test", "test");
CoolingDecision Decide(ThermalControl control, double? cpu, double? gpu, int max = 2700, TemperatureScope scope = TemperatureScope.GpuOnly, CoolingPreset preset = CoolingPreset.Performance) => control.Evaluate(Sample(cpu, gpu), scope, preset, max, thermalNow);
Run("Auto uses higher CPU/GPU temperature", () => Equal(3500, Decide(new(), 80, 45, 4000, TemperatureScope.CpuAndGpu).Rpm));
Run("GPU-only scope does not invent a CPU reading", () => Equal(1700, Decide(new(), null, 45).Rpm));
Run("Missing required CPU enters maximum-RPM fail-safe", () =>
{
    var decision = Decide(new(), null, 45, 3300, TemperatureScope.CpuAndGpu);
    Equal(3300, decision.Rpm); Equal(true, decision.FailSafe); Equal<double?>(null, decision.Temperature);
});
Run("Missing GPU enters fail-safe", () => Equal(true, Decide(new(), 40, null).FailSafe));
Run("Invalid temperatures never select quiet speed", () =>
{
    foreach (double invalid in new[] { 0d, -1, 126, double.NaN, double.PositiveInfinity }) Equal(2700, Decide(new(), null, invalid).Rpm);
});
Run("Stale and future samples fail safe", () =>
{
    foreach (int age in new[] { 9, -3 })
        Equal(true, new ThermalControl().Evaluate(new(null, Reading(40, age), "", ""), TemperatureScope.GpuOnly, CoolingPreset.Quiet, 2700, thermalNow).FailSafe);
});
Run("Power supply caps every automatic curve", () =>
{
    foreach (int maximum in new[] { 2700, 3300, 4000 })
        foreach (double value in new[] { 10d, 40, 50, 60, 70, 80, 100 })
        {
            var rpm = Decide(new(), null, value, maximum).Rpm;
            if (rpm < 1300 || rpm > maximum) throw new Exception("Automatic RPM exceeded supply bounds.");
        }
});
Run("Cooling hysteresis avoids threshold chatter", () =>
{
    var control = new ThermalControl();
    Equal(2700, Decide(control, null, 70).Rpm);
    Equal(2700, Decide(control, null, 69).Rpm);
    Equal(2700, Decide(control, null, 68).Rpm);
    Equal(2400, Decide(control, null, 67).Rpm);
});
Run("Heating response is immediate", () =>
{
    var control = new ThermalControl(); Decide(control, null, 40);
    Equal(2700, Decide(control, null, 75).Rpm);
});
Run("Critical temperature has recovery hysteresis", () =>
{
    var control = new ThermalControl();
    Equal(4000, Decide(control, null, 90, 4000, preset: CoolingPreset.Quiet).Rpm);
    Equal(4000, Decide(control, null, 88, 4000, preset: CoolingPreset.Quiet).Rpm);
    Equal(3000, Decide(control, null, 86, 4000, preset: CoolingPreset.Quiet).Rpm);
});
Run("Quiet and performance profiles differ", () =>
{
    Equal(1300, Decide(new(), null, 40, 2700, preset: CoolingPreset.Quiet).Rpm);
    Equal(1700, Decide(new(), null, 40, 2700, preset: CoolingPreset.Performance).Rpm);
});
Run("Automatic RPM command remains bounded", () =>
{
    Equal((byte)0x21, Bs2Protocol.SetAutomaticRpm(1700, 2700)[3]);
    Throws<ArgumentOutOfRangeException>(() => Bs2Protocol.SetAutomaticRpm(4000, 2700));
});
Run("Intel temperature decode validates hardware flags and target", () =>
{
    Equal(true, IntelThermalDecoder.TryDecode(100UL << 16, (1UL << 31) | (45UL << 16), out double temperature));
    Equal(55d, temperature);
    Equal(false, IntelThermalDecoder.TryDecode(100UL << 16, 45UL << 16, out _));
    Equal(false, IntelThermalDecoder.TryDecode(0, 1UL << 31, out _));
    Equal(false, IntelThermalDecoder.TryDecode(100UL << 16, (1UL << 31) | (127UL << 16), out _));
});
Console.WriteLine($"PASS: {passed} protocol/thermal tests. No device or manufacturer libraries used.");
return;

void Run(string name, Action test) { test(); passed++; Console.WriteLine("PASS " + name); }
static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, actual {actual}"); }
static Bs2Frame Decode(byte[] bytes) => Bs2Protocol.TryDecode(bytes, out var frame) && frame != null ? frame : throw new Exception("Decode failed");
static void Throws<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new Exception("Expected " + typeof(T).Name);
}
