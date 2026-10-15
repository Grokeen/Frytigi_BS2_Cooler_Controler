using System.IO;
using System.Text.Json;
using Bs2Cooler.Core;

namespace Bs2Cooler.Windows;

public static class RgbSettingsStore
{
    public static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Bs2Cooler", "rgb-settings.json");
    public static RgbSettings? Load()
    {
        if (!File.Exists(FilePath)) return null;
        if (new FileInfo(FilePath).Length > 16384) throw new InvalidDataException("저장된 RGB 설정 파일이 너무 큽니다.");
        var settings = JsonSerializer.Deserialize<RgbSettings>(File.ReadAllText(FilePath)) ?? throw new InvalidDataException("저장된 RGB 설정이 비어 있습니다.");
        settings.Validate();
        return settings;
    }
    public static void Save(RgbSettings settings)
    {
        settings.Validate();
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        string temporary = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, FilePath, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
