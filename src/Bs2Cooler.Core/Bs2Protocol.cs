using System.Buffers.Binary;

namespace Bs2Cooler.Core;

// Only the BS2 Pro commands used by this app are exposed. No firmware, reset, or bootloader commands.
public enum Bs2Command : byte
{
    Firmware = 0x01,
    Rpm = 0x22,
    ExitSmart = 0x24,
    FanMode = 0x25,
    SetGear = 0x26,
    GearSpeeds = 0x27,
    RgbStatus = 0x45,
    SetRgb = 0x46,
    SetGearLight = 0x48,
    Status = 0xEF
}

public sealed record Bs2Frame(byte Command, byte[] Payload);
public sealed record FanSpeed(int ActualRpm, int TargetRpm);

public sealed record CoolerStatus(
    FanSpeed Speed, int Gear, int PowerMode, bool IsShutdown, bool IsSmart,
    bool RgbEnabled, bool GearLightEnabled, DateTimeOffset ReceivedAt)
{
    public int MaximumRpm => PowerMode switch { 2 => 3300, 3 => 4000, _ => 2700 };
    public string PowerDescription => PowerMode switch
    {
        1 => "일반 전원", 2 => "QC 고속 전원", 3 => "PD 고속 전원", _ => "전원 정보 확인 중"
    };
}

public static class Bs2Protocol
{
    public const int VendorId = 0x37D7;
    public const int ProductId = 0x1002;
    public const int InputLength = 33;
    public const int OutputLength = 32;
    public const int MinimumRpm = 1300;

    public static byte[] Query(Bs2Command command) => command switch
    {
        Bs2Command.Firmware or Bs2Command.Rpm or Bs2Command.FanMode or Bs2Command.GearSpeeds or Bs2Command.RgbStatus => Encode(command, []),
        _ => throw new ArgumentOutOfRangeException(nameof(command))
    };

    public static byte[] ExitSmart() => Encode(Bs2Command.ExitSmart, []);
    public static byte[] Rgb(bool enabled) => Encode(Bs2Command.SetRgb, [(byte)(enabled ? 1 : 0)]);
    public static byte[] GearLight(bool enabled) => Encode(Bs2Command.SetGearLight, [(byte)(enabled ? 1 : 0)]);

    public static byte[] SetGear(int gear, int rpm, int maximumRpm)
    {
        if (gear is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(gear));
        ValidateRpm(rpm, maximumRpm);
        return Encode(Bs2Command.SetGear, [(byte)(gear - 1), (byte)rpm, (byte)(rpm >> 8)]);
    }

    public static void ValidateRpm(int rpm, int maximumRpm)
    {
        if (maximumRpm is < MinimumRpm or > 4000) throw new ArgumentOutOfRangeException(nameof(maximumRpm));
        if (rpm < MinimumRpm || rpm > maximumRpm)
            throw new ArgumentOutOfRangeException(nameof(rpm), $"속도는 {MinimumRpm}–{maximumRpm} RPM 범위여야 합니다.");
    }

    private static byte[] Encode(Bs2Command command, ReadOnlySpan<byte> payload)
    {
        byte[] result = new byte[OutputLength];
        // The validated USB interface uses unnumbered HID reports: byte zero must be 0.
        result[1] = 0x5A;
        result[2] = 0xA5;
        result[3] = (byte)command;
        result[4] = checked((byte)(payload.Length + 2));
        payload.CopyTo(result.AsSpan(5));
        int sum = (byte)command + result[4];
        foreach (byte value in payload) sum += value;
        result[5 + payload.Length] = unchecked((byte)sum);
        return result;
    }

    public static bool TryDecode(ReadOnlySpan<byte> input, out Bs2Frame? frame)
    {
        frame = null;
        // Raw Windows input: 00 03 5A A5 ... . Other two forms support saved, normalized captures.
        int offset = input.Length >= 4 && input[0] == 0 && input[1] == 3 && input[2] == 0x5A && input[3] == 0xA5 ? 2
            : input.Length >= 3 && input[0] == 3 && input[1] == 0x5A && input[2] == 0xA5 ? 1 : 0;
        var data = input[offset..];
        if (data.Length < 5 || data[0] != 0x5A || data[1] != 0xA5 || data[3] < 2) return false;
        int length = data[3] + 3;
        if (length > data.Length) return false;
        int sum = 0;
        for (int index = 2; index < length - 1; index++) sum += data[index];
        if (unchecked((byte)sum) != data[length - 1]) return false;
        frame = new Bs2Frame(data[2], data.Slice(4, data[3] - 2).ToArray());
        return true;
    }

    public static FanSpeed ParseRpm(Bs2Frame frame)
    {
        Require(frame, Bs2Command.Rpm, 4);
        return ReadSpeed(frame.Payload);
    }

    public static CoolerStatus ParseStatus(Bs2Frame frame)
    {
        Require(frame, Bs2Command.Status, 9);
        var p = frame.Payload;
        return new CoolerStatus(ReadSpeed(p.AsSpan(3)), ((p[0] >> 1) & 3) + 1, (p[0] >> 5) & 3,
            (p[0] & 1) != 0, (p[1] & 1) != 0, (p[2] & 4) != 0, (p[2] & 1) != 0, DateTimeOffset.Now);
    }

    public static string ParseFirmware(Bs2Frame frame)
    {
        Require(frame, Bs2Command.Firmware, 4);
        return string.Join('.', frame.Payload.Take(4));
    }

    public static int[] ParseGearSpeeds(Bs2Frame frame)
    {
        Require(frame, Bs2Command.GearSpeeds, 8);
        return Enumerable.Range(0, 4).Select(i => (int)BinaryPrimitives.ReadUInt16LittleEndian(frame.Payload.AsSpan(i * 2))).ToArray();
    }

    public static void RequireSuccess(Bs2Frame frame, Bs2Command command)
    {
        Require(frame, command, 1);
        if (frame.Payload[0] != 1 && !(command == Bs2Command.ExitSmart && frame.Payload[0] == 2))
            throw new IOException($"장치가 명령 0x{(byte)command:X2}을 거부했습니다 (응답 {frame.Payload[0]}).");
    }

    private static FanSpeed ReadSpeed(ReadOnlySpan<byte> data) => new(
        BinaryPrimitives.ReadUInt16LittleEndian(data), BinaryPrimitives.ReadUInt16LittleEndian(data[2..]));

    private static void Require(Bs2Frame frame, Bs2Command command, int payloadLength)
    {
        if (frame.Command != (byte)command || frame.Payload.Length < payloadLength)
            throw new InvalidDataException("장치 응답의 명령 또는 길이가 올바르지 않습니다.");
    }
}
