using System.Runtime.InteropServices;
using Bs2Cooler.Core;

namespace Bs2Cooler.Windows;

// Uses the existing NVIDIA display driver's read-only NVML API from Windows System32.
public sealed class NvidiaTemperatureReader : IDisposable
{
    private bool _initialized;
    public string Status { get; private set; } = "GPU 센서 확인 중";

    public TemperatureReading? Read()
    {
        try
        {
            if (!_initialized)
            {
                int status = nvmlInit_v2();
                if (status != 0) { Status = $"NVIDIA 센서 초기화 실패 ({status})"; return null; }
                _initialized = true;
            }
            if (nvmlDeviceGetCount_v2(out uint count) != 0 || count == 0) { Status = "NVIDIA GPU 없음"; return null; }
            double? maximum = null;
            for (uint i = 0; i < Math.Min(count, 16); i++)
            {
                if (nvmlDeviceGetHandleByIndex_v2(i, out nint handle) == 0
                    && nvmlDeviceGetTemperature(handle, 0, out uint value) == 0 && value is > 0 and <= 125)
                    maximum = Math.Max(maximum ?? value, value);
            }
            Status = maximum.HasValue ? "NVIDIA NVML" : "GPU 온도를 읽을 수 없음";
            return maximum is { } temperature ? new(temperature, Status, DateTimeOffset.Now) : null;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        { Status = "지원하는 NVIDIA 드라이버 센서가 없음"; return null; }
    }

    public void Dispose()
    {
        if (!_initialized) return;
        _initialized = false;
        nvmlShutdown();
    }

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int nvmlInit_v2();
    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int nvmlShutdown();
    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int nvmlDeviceGetCount_v2(out uint count);
    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int nvmlDeviceGetHandleByIndex_v2(uint index, out nint handle);
    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int nvmlDeviceGetTemperature(nint device, uint sensor, out uint temperature);
}
