using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using Bs2Cooler.Core;
using Microsoft.Win32.SafeHandles;

namespace Bs2Cooler.Windows;

// Never installs or starts a driver. It can only use an already configured, signed PawnIO driver.
// Only two package-temperature-related reads are exposed; no write-MSR function is invoked.
public sealed class IntelTemperatureReader : IDisposable
{
    private const string ModuleHash = "D6ED85D65AB17A22F813EF98207D6D537155EE2DED5976A21CB48413C9B92E5F";
    private SafeFileHandle? _handle;
    private DateTimeOffset _retryAt;
    public string Status { get; private set; } = "CPU 센서 확인 중";

    public TemperatureReading? Read()
    {
        if (_handle == null && !TryOpen()) return null;
        if (!TryRead(0x1A2, out ulong target)) { Status = "CPU 온도 한계를 읽을 수 없음"; return null; }
        if (TryRead(0x1B1, out ulong package) && IntelThermalDecoder.TryDecode(target, package, out double temperature))
        { Status = "Intel CPU Package / PawnIO"; return new(temperature, Status, DateTimeOffset.Now); }
        Status = "유효한 CPU 패키지 온도를 읽을 수 없음";
        return null;
    }

    private bool TryOpen()
    {
        if (DateTimeOffset.Now < _retryAt) return false;
        _retryAt = DateTimeOffset.Now.AddSeconds(5);
        if (!X86Base.IsSupported) { Status = "Intel CPU 센서 지원 대상 아님"; return false; }
        var vendor = X86Base.CpuId(0, 0);
        if (vendor.Ebx != 0x756E6547 || vendor.Edx != 0x49656E69 || vendor.Ecx != 0x6C65746E)
        { Status = "현재 CPU 센서 경로는 Intel만 지원"; return false; }
        var handle = CreateFile(@"\\?\GLOBALROOT\Device\PawnIO", 3, 3, 0, 3, 0, 0);
        if (handle.IsInvalid)
        {
            int error = Marshal.GetLastWin32Error(); handle.Dispose();
            Status = error == 5 ? "CPU 센서 접근 권한 부족" : "CPU 센서 드라이버 미설정";
            return false;
        }
        using var resource = typeof(IntelTemperatureReader).Assembly.GetManifestResourceStream("Bs2Cooler.IntelMSR.bin")
            ?? throw new InvalidDataException("CPU 센서 모듈 리소스가 없습니다.");
        using var memory = new MemoryStream(); resource.CopyTo(memory); byte[] module = memory.ToArray();
        if (Convert.ToHexString(SHA256.HashData(module)) != ModuleHash)
        { handle.Dispose(); throw new InvalidDataException("CPU 센서 모듈 해시가 다릅니다."); }
        if (!DeviceIoControl(handle, 0xA1B22084, module, module.Length, null, 0, out _, 0))
        { handle.Dispose(); Status = "CPU 센서 모듈의 서명/버전 확인 실패"; return false; }
        _handle = handle;
        return true;
    }

    private bool TryRead(uint index, out ulong value)
    {
        value = 0;
        if (_handle == null || index is not (0x1A2 or 0x1B1)) return false;
        byte[] request = new byte[40];
        Encoding.ASCII.GetBytes("ioctl_read_msr").CopyTo(request, 0);
        BinaryPrimitives.WriteInt64LittleEndian(request.AsSpan(32), index);
        byte[] output = new byte[8];
        if (!DeviceIoControl(_handle, 0xA1B22104, request, request.Length, output, output.Length, out int returned, 0) || returned != 8)
            return false;
        value = BinaryPrimitives.ReadUInt64LittleEndian(output);
        return true;
    }

    public void Dispose() { _handle?.Dispose(); _handle = null; }
    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern SafeFileHandle CreateFile(string path, uint access, uint share, nint security, uint creation, uint flags, nint template);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DeviceIoControl(SafeFileHandle device, uint code, byte[] input, int inputLength, byte[]? output, int outputLength, out int returned, nint overlapped);
}
