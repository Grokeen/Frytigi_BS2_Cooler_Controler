using Bs2Cooler.Core;

namespace Bs2Cooler.Windows;

public sealed class TemperatureMonitor : IDisposable
{
    private readonly NvidiaTemperatureReader _gpu = new();
    private readonly IntelTemperatureReader _cpu = new();
    public TemperatureSnapshot Read()
    {
        TemperatureReading? cpu = null, gpu = null;
        string cpuStatus, gpuStatus;
        try { cpu = _cpu.Read(); cpuStatus = _cpu.Status; } catch (Exception ex) { cpuStatus = "CPU 센서 조회 실패: " + ex.Message; }
        try { gpu = _gpu.Read(); gpuStatus = _gpu.Status; } catch (Exception ex) { gpuStatus = "GPU 센서 조회 실패: " + ex.Message; }
        return new(cpu, gpu, cpuStatus, gpuStatus, _cpu.RequiresElevation);
    }
    public void Dispose() { _cpu.Dispose(); _gpu.Dispose(); }
}
