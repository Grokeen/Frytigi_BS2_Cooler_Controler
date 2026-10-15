namespace Bs2Cooler.Core;

public enum CoolingPreset { Quiet, Performance }
public enum TemperatureScope { CpuAndGpu, GpuOnly }

public sealed record TemperatureReading(double Celsius, string Source, DateTimeOffset CapturedAt)
{
    public bool IsUsable(DateTimeOffset now) => double.IsFinite(Celsius) && Celsius is > 0 and <= 125
        && CapturedAt <= now.AddSeconds(2) && now - CapturedAt <= TimeSpan.FromSeconds(8);
}

public sealed record TemperatureSnapshot(TemperatureReading? Cpu, TemperatureReading? Gpu, string CpuStatus, string GpuStatus, bool CpuElevationRequired = false);
public sealed record CoolingDecision(int Rpm, double? Temperature, bool FailSafe, string Description);

public static class IntelThermalDecoder
{
    public static bool TryDecode(ulong temperatureTarget, ulong thermalStatus, out double celsius)
    {
        celsius = 0;
        int tjMaximum = (int)((temperatureTarget >> 16) & 0xFF);
        if (tjMaximum is < 60 or > 125 || (thermalStatus & (1UL << 31)) == 0) return false;
        int value = tjMaximum - (int)((thermalStatus >> 16) & 0x7F);
        if (value is <= 0 or > 125) return false;
        celsius = value;
        return true;
    }
}

public sealed class ThermalControl
{
    public const int TargetCelsius = 70;
    public const int FullSpeedCelsius = 60;
    public const int FullSpeedReleaseCelsius = 57;
    private int _previousRpm;
    private bool _critical;
    private bool _targetCooling;
    private static readonly int[] Thresholds = [30, 40, 50, 60, 70, 80, 90, 100];

    public CoolingDecision Evaluate(TemperatureSnapshot sample, TemperatureScope scope, CoolingPreset preset, int maximumRpm, DateTimeOffset now)
    {
        Bs2Protocol.ValidateRpm(Bs2Protocol.MinimumRpm, maximumRpm);
        bool cpuValid = sample.Cpu?.IsUsable(now) == true;
        bool gpuValid = sample.Gpu?.IsUsable(now) == true;
        if (!gpuValid || (scope == TemperatureScope.CpuAndGpu && !cpuValid))
        {
            _previousRpm = maximumRpm;
            return new(maximumRpm, null, true, "센서 데이터 없음/만료 · 최대 풍량 유지");
        }
        double temperature = scope == TemperatureScope.CpuAndGpu
            ? Math.Max(sample.Cpu!.Celsius, sample.Gpu!.Celsius) : sample.Gpu!.Celsius;
        if (temperature >= 90) _critical = true;
        else if (temperature < 87) _critical = false;
        if (temperature >= FullSpeedCelsius) _targetCooling = true;
        else if (temperature < FullSpeedReleaseCelsius) _targetCooling = false;
        if (_critical)
        {
            _previousRpm = maximumRpm;
            return new(maximumRpm, temperature, false, "90°C 고온 보호 · 최대 풍량 · 70°C 목표 초과");
        }
        if (_targetCooling)
        {
            _previousRpm = maximumRpm;
            string reason = temperature > TargetCelsius ? "70°C 목표 초과 · 최대 풍량" : temperature == TargetCelsius
                ? "70°C 목표 도달 · 최대 풍량" : "70°C 목표 · 선제 최대 풍량";
            return new(maximumRpm, temperature, false, reason);
        }
        int desired = Lookup(temperature, preset, maximumRpm);
        // A cooling reading must cross a threshold by 2 C before fan speed decreases.
        if (_previousRpm > 0 && desired < _previousRpm)
            desired = Math.Min(_previousRpm, Lookup(temperature + 2, preset, maximumRpm));
        _previousRpm = Math.Clamp(desired, Bs2Protocol.MinimumRpm, maximumRpm);
        return new(_previousRpm, temperature, false, preset == CoolingPreset.Quiet ? "저소음 온도 곡선" : "성능 온도 곡선");
    }

    public void Reset() { _previousRpm = 0; _critical = false; _targetCooling = false; }

    private static int Lookup(double temperature, CoolingPreset preset, int maximum)
    {
        int[] speeds = (maximum, preset) switch
        {
            (4000, CoolingPreset.Quiet) => [1300, 1300, 1700, 2100, 2700, 3000, 3300, 3500],
            (4000, _) => [1700, 1700, 2100, 2700, 3300, 3500, 4000, 4000],
            (3300, CoolingPreset.Quiet) => [1300, 1300, 1700, 2100, 2700, 3000, 3300, 3300],
            (3300, _) => [1700, 1700, 2100, 2700, 3300, 3300, 3300, 3300],
            (_, CoolingPreset.Quiet) => [1300, 1300, 1700, 2100, 2400, 2700, 2700, 2700],
            _ => [1300, 1700, 2100, 2400, 2700, 2700, 2700, 2700]
        };
        int index = 0;
        for (int i = 0; i < Thresholds.Length; i++) { if (temperature >= Thresholds[i]) index = i; }
        return Math.Min(speeds[index], maximum);
    }
}
