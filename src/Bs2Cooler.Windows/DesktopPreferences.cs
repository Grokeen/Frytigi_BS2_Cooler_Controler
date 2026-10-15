using System.Text.Json;
using Bs2Cooler.Core;

namespace Bs2Cooler.Windows;

public sealed record DesktopPreferences(bool AutoStartCooling = false, TemperatureScope Scope = TemperatureScope.CpuAndGpu, CoolingPreset Preset = CoolingPreset.Performance)
{
    public void Validate()
    {
        if (!Enum.IsDefined(Scope) || !Enum.IsDefined(Preset)) throw new InvalidDataException("자동 실행 제어 설정을 확인해 주세요.");
    }
}

public static class DesktopPreferencesStore
{
    public static string DirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Bs2Cooler");
    public static string FilePath => Path.Combine(DirectoryPath, "app-settings.json");
    public static DesktopPreferences Load()
    {
        if (!File.Exists(FilePath)) return new();
        if (new FileInfo(FilePath).Length > 16384) throw new InvalidDataException("자동 실행 설정 파일이 너무 큽니다.");
        var preferences = JsonSerializer.Deserialize<DesktopPreferences>(File.ReadAllText(FilePath)) ?? throw new InvalidDataException("자동 실행 설정이 비어 있습니다.");
        preferences.Validate(); return preferences;
    }
    public static void Save(DesktopPreferences preferences)
    {
        preferences.Validate(); Directory.CreateDirectory(DirectoryPath);
        string temporary = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, JsonSerializer.Serialize(preferences)); File.Move(temporary, FilePath, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            string log = Path.Combine(DirectoryPath, "desktop.log");
            if (File.Exists(log) && new FileInfo(log).Length > 262144) File.Move(log, log + ".previous", true);
            File.AppendAllText(log, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
