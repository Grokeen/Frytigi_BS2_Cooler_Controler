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
Console.WriteLine($"PASS: {passed} protocol tests. No device or manufacturer libraries used.");
return;

void Run(string name, Action test) { test(); passed++; Console.WriteLine("PASS " + name); }
static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, actual {actual}"); }
static Bs2Frame Decode(byte[] bytes) => Bs2Protocol.TryDecode(bytes, out var frame) && frame != null ? frame : throw new Exception("Decode failed");
static void Throws<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new Exception("Expected " + typeof(T).Name);
}
