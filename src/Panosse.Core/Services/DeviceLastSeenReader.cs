using System;
using System.Runtime.InteropServices;

namespace Panosse.Services;

internal static class DeviceLastSeenReader
{
    private const uint CmLocateDevnodePhantom = 0x1;
    private const uint CrSuccess = 0;
    private const uint DevpropTypeFiletime = 0x10;

    private static readonly Guid DeviceDatesFmtId = new("83da6326-97a6-4088-9453-a1923f573b29");

    public static DateTime? GetLastSeen(string instanceId)
    {
        try
        {
            if (CM_Locate_DevNodeW(out uint devInst, instanceId, CmLocateDevnodePhantom) != CrSuccess)
            {
                return null;
            }

            DateTime? arrival = ReadFileTime(devInst, new DevPropKey(DeviceDatesFmtId, 102));
            DateTime? removal = ReadFileTime(devInst, new DevPropKey(DeviceDatesFmtId, 103));
            return Nullable.Compare(arrival, removal) >= 0 ? arrival : removal;
        }
        catch
        {
            return null;
        }
    }

    private static DateTime? ReadFileTime(uint devInst, DevPropKey key)
    {
        byte[] buffer = new byte[sizeof(long)];
        uint size = (uint)buffer.Length;
        uint result = CM_Get_DevNode_PropertyW(devInst, ref key, out uint propertyType, buffer, ref size, 0);
        if (result != CrSuccess || propertyType != DevpropTypeFiletime || size != sizeof(long))
        {
            return null;
        }

        long fileTime = BitConverter.ToInt64(buffer, 0);
        return fileTime > 0 ? DateTime.FromFileTimeUtc(fileTime).ToLocalTime() : null;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DevPropKey(Guid fmtId, uint pid)
    {
        public Guid FmtId = fmtId;
        public uint Pid = pid;
    }

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    private static extern uint CM_Locate_DevNodeW(out uint pdnDevInst, string pDeviceID, uint ulFlags);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    private static extern uint CM_Get_DevNode_PropertyW(
        uint dnDevInst,
        ref DevPropKey propertyKey,
        out uint propertyType,
        byte[] propertyBuffer,
        ref uint propertyBufferSize,
        uint ulFlags);
}
