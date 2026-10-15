using System.Globalization;

namespace Bs2Cooler.Core;

public enum RgbEffect { Solid, Breathing, Rainbow, Wave, Flash, Streaming, Individual, Rotation }

public readonly record struct RgbColor(byte Red, byte Green, byte Blue)
{
    public string Hex => $"#{Red:X2}{Green:X2}{Blue:X2}";
    public static RgbColor Parse(string text)
    {
        string hex = text.Trim();
        if (hex.StartsWith('#')) hex = hex[1..];
        if (hex.Length != 6 || !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint value))
            throw new FormatException("색상은 #RRGGBB 형식으로 입력해 주세요. 예: #55CBB0");
        return new((byte)(value >> 16), (byte)(value >> 8), (byte)value);
    }
}

public sealed record RgbSettings(RgbEffect Effect, int Brightness, int Speed, int BreathingColors, RgbColor[] Colors)
{
    public static RgbSettings Default => new(RgbEffect.Solid, 50, 5, 1,
        [new(85,203,176), new(255,80,80), new(255,170,0), new(80,220,90), new(50,130,255), new(180,80,255)]);
    public void Validate()
    {
        if (!Enum.IsDefined(Effect) || Brightness is < 0 or > 100 || Speed is < 1 or > 10
            || BreathingColors is < 1 or > 5 || Colors is not { Length: 6 })
            throw new ArgumentException("RGB 효과/밝기(0–100)/속도(1–10)/색상 6개를 확인해 주세요.");
    }
}

public static class RgbLighting
{
    public const int DataLength = 186;
    private static readonly RgbColor[] Spectrum = [new(255,0,0), new(255,127,0), new(255,255,0), new(0,255,0), new(0,0,255), new(75,0,130), new(148,0,211)];

    public static byte[] Serialize(RgbSettings settings)
    {
        settings.Validate();
        int frames = settings.Effect switch
        {
            RgbEffect.Breathing => settings.BreathingColors * 2,
            RgbEffect.Rainbow => 7,
            RgbEffect.Wave or RgbEffect.Streaming or RgbEffect.Rotation => 6,
            RgbEffect.Flash => 2,
            _ => 1
        };
        byte[] data = new byte[DataLength];
        data[1] = 2; // RGB table version 0.2; six LEDs, ten RGB frames per LED.
        data[3] = (byte)(frames - 1);
        data[4] = (byte)(frames == 1 ? 11 : 11 - settings.Speed);
        data[5] = (byte)settings.Brightness;
        for (int led = 0; led < 6; led++)
        for (int frame = 0; frame < 10; frame++)
        {
            var first = settings.Colors[0];
            RgbColor color = frame >= frames ? default : settings.Effect switch
            {
                RgbEffect.Solid => first,
                RgbEffect.Individual => settings.Colors[led],
                RgbEffect.Breathing => frame % 2 == 0 ? default : settings.Colors[frame / 2],
                RgbEffect.Flash => frame == 0 ? first : default,
                RgbEffect.Rainbow => Spectrum[(led + frame) % 7],
                RgbEffect.Streaming => settings.Colors[(led + frame) % 6],
                RgbEffect.Rotation => settings.Colors[(led + 6 - frame) % 6],
                RgbEffect.Wave => Scale(first, Math.Min((led + frame) % 6, 6 - (led + frame) % 6) / 3d),
                _ => throw new ArgumentException("지원하지 않는 RGB 효과입니다.")
            };
            int offset = 6 + (led * 10 + frame) * 3;
            data[offset] = color.Red; data[offset + 1] = color.Green; data[offset + 2] = color.Blue;
        }
        return data;
    }

    public static IReadOnlyList<byte[]> BuildUpload(RgbSettings settings)
    {
        byte[] data = Serialize(settings);
        var commands = new List<byte[]> { Bs2Protocol.RgbPrepare() };
        for (int offset = 0; offset < data.Length; offset += 10)
            commands.Add(Bs2Protocol.RgbChunk(offset / 10, data.AsSpan(offset, Math.Min(10, data.Length - offset))));
        commands.Add(Bs2Protocol.RgbComplete());
        return commands;
    }

    private static RgbColor Scale(RgbColor color, double amount) => new((byte)(color.Red * amount), (byte)(color.Green * amount), (byte)(color.Blue * amount));
}
