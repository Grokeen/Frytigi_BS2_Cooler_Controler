using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Bs2Cooler.Core;
using Microsoft.Win32.SafeHandles;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

namespace Bs2Cooler.Windows;

public sealed record Bs2Device(string Path, string Product, ushort UsagePage, ushort Usage, int InputLength, int OutputLength)
{
    public bool IsSupported => UsagePage == 0xFFA0 && Usage == 1 && InputLength == Bs2Protocol.InputLength && OutputLength == Bs2Protocol.OutputLength;
}

public static class NativeHid
{
    public static IReadOnlyList<Bs2Device> Enumerate()
    {
        HidD_GetHidGuid(out Guid guid);
        nint set = SetupDiGetClassDevs(ref guid, null, 0, 0x12);
        if (set == new nint(-1)) throw new Win32Exception(Marshal.GetLastWin32Error());
        List<Bs2Device> result = [];
        try
        {
            for (uint index = 0; ; index++)
            {
                var info = new InterfaceData { Size = Marshal.SizeOf<InterfaceData>() };
                if (!SetupDiEnumDeviceInterfaces(set, 0, ref guid, index, ref info))
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error == 259) break;
                    throw new Win32Exception(error);
                }
                SetupDiGetDeviceInterfaceDetail(set, ref info, 0, 0, out uint required, 0);
                if (required == 0) continue;
                nint detail = Marshal.AllocHGlobal(checked((int)required));
                try
                {
                    Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                    if (!SetupDiGetDeviceInterfaceDetail(set, ref info, detail, required, out _, 0)) continue;
                    string? path = Marshal.PtrToStringUni(detail + 4);
                    if (path == null || !path.Contains("vid_37d7&pid_1002", StringComparison.OrdinalIgnoreCase)) continue;
                    using var handle = CreateFile(path, 0, 3, 0, 3, 0, 0);
                    if (handle.IsInvalid) continue;
                    var attributes = new Attributes { Size = Marshal.SizeOf<Attributes>() };
                    if (!HidD_GetAttributes(handle, ref attributes) || attributes.VendorId != Bs2Protocol.VendorId || attributes.ProductId != Bs2Protocol.ProductId) continue;
                    if (!HidD_GetPreparsedData(handle, out nint preparsed)) continue;
                    try
                    {
                        if (HidP_GetCaps(preparsed, out Caps caps) != 0x00110000) continue;
                        byte[] product = new byte[256];
                        string name = HidD_GetProductString(handle, product, product.Length) ? Encoding.Unicode.GetString(product).TrimEnd('\0') : "Flydigi BS2 Pro";
                        result.Add(new Bs2Device(path, name, caps.UsagePage, caps.Usage, caps.InputLength, caps.OutputLength));
                    }
                    finally { HidD_FreePreparsedData(preparsed); }
                }
                finally { Marshal.FreeHGlobal(detail); }
            }
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
        return result;
    }

    internal static FileStream Open(Bs2Device device)
    {
        if (!device.IsSupported) throw new NotSupportedException("이 USB 인터페이스는 검증된 BS2 Pro 보고서 형식과 다릅니다.");
        var handle = CreateFile(device.Path, 0xC0000000, 3, 0, 3, 0x40000000, 0);
        if (handle.IsInvalid)
        {
            int error = Marshal.GetLastWin32Error();
            handle.Dispose();
            throw new Win32Exception(error, "BS2 Pro USB 장치를 열 수 없습니다.");
        }
        try { return new FileStream(handle, FileAccess.ReadWrite, 1, true); }
        catch { handle.Dispose(); throw; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct InterfaceData { public int Size; public Guid ClassGuid; public uint Flags; public nint Reserved; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Attributes { public int Size; public ushort VendorId; public ushort ProductId; public ushort Version; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Caps
    {
        public ushort Usage, UsagePage, InputLength, OutputLength, FeatureLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)] public ushort[] Reserved;
        public ushort LinkCollections, InputButtons, InputValues, InputIndices, OutputButtons, OutputValues, OutputIndices, FeatureButtons, FeatureValues, FeatureIndices;
    }

    [DllImport("hid.dll")] private static extern void HidD_GetHidGuid(out Guid guid);
    [DllImport("hid.dll")] [return: MarshalAs(UnmanagedType.U1)] private static extern bool HidD_GetAttributes(SafeFileHandle device, ref Attributes attributes);
    [DllImport("hid.dll")] [return: MarshalAs(UnmanagedType.U1)] private static extern bool HidD_GetPreparsedData(SafeFileHandle device, out nint data);
    [DllImport("hid.dll")] [return: MarshalAs(UnmanagedType.U1)] private static extern bool HidD_FreePreparsedData(nint data);
    [DllImport("hid.dll")] private static extern int HidP_GetCaps(nint data, out Caps caps);
    [DllImport("hid.dll")] [return: MarshalAs(UnmanagedType.U1)] private static extern bool HidD_GetProductString(SafeFileHandle device, byte[] buffer, int length);
    [DllImport("setupapi.dll", EntryPoint = "SetupDiGetClassDevsW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern nint SetupDiGetClassDevs(ref Guid guid, string? enumerator, nint parent, uint flags);
    [DllImport("setupapi.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetupDiEnumDeviceInterfaces(nint set, nint device, ref Guid guid, uint index, ref InterfaceData data);
    [DllImport("setupapi.dll", EntryPoint = "SetupDiGetDeviceInterfaceDetailW", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetupDiGetDeviceInterfaceDetail(nint set, ref InterfaceData data, nint detail, uint size, out uint required, nint deviceInfo);
    [DllImport("setupapi.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetupDiDestroyDeviceInfoList(nint set);
    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern SafeFileHandle CreateFile(string path, uint access, uint share, nint security, uint creation, uint flags, nint template);
}
