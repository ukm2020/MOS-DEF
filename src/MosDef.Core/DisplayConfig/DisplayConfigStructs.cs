using System.Runtime.InteropServices;

namespace MosDef.Core.DisplayConfig;

/// <summary>
/// Contains information about a display path.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-displayconfig_path_info
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct DISPLAYCONFIG_PATH_INFO
{
    public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
    public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
    public uint flags;
}

/// <summary>
/// Contains information about the source display device.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-displayconfig_path_source_info
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct DISPLAYCONFIG_PATH_SOURCE_INFO
{
    public LUID adapterId;
    public uint id;
    public uint modeInfoIdx;
    public uint statusFlags;
}

/// <summary>
/// Contains information about the target display device.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-displayconfig_path_target_info
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct DISPLAYCONFIG_PATH_TARGET_INFO
{
    public LUID adapterId;
    public uint id;
    public uint modeInfoIdx;
    public DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY outputTechnology;
    public DISPLAYCONFIG_ROTATION rotation;
    public DISPLAYCONFIG_SCALING scaling;
    public DISPLAYCONFIG_RATIONAL refreshRate;
    public DISPLAYCONFIG_SCANLINE_ORDERING scanLineOrdering;
    public bool targetAvailable;
    public uint statusFlags;
}

/// <summary>
/// Contains mode information for a display.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-displayconfig_mode_info
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct DISPLAYCONFIG_MODE_INFO
{
    public DISPLAYCONFIG_MODE_INFO_TYPE infoType;
    public uint id;
    public LUID adapterId;
    public DISPLAYCONFIG_MODE_INFO_UNION modeInfo;
}

/// <summary>
/// Union for mode information that can be either source or target mode.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-displayconfig_mode_info
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct DISPLAYCONFIG_MODE_INFO_UNION
{
    [FieldOffset(0)]
    public DISPLAYCONFIG_TARGET_MODE targetMode;

    [FieldOffset(0)]
    public DISPLAYCONFIG_SOURCE_MODE sourceMode;

    [FieldOffset(0)]
    public DISPLAYCONFIG_DESKTOP_IMAGE_INFO desktopImageInfo;
}

/// <summary>
/// Contains information about a target display mode.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-displayconfig_target_mode
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct DISPLAYCONFIG_TARGET_MODE
{
    public DISPLAYCONFIG_VIDEO_SIGNAL_INFO targetVideoSignalInfo;
}

/// <summary>
/// Contains information about a source display mode.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-displayconfig_source_mode
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct DISPLAYCONFIG_SOURCE_MODE
{
    public uint width;
    public uint height;
    public DISPLAYCONFIG_PIXELFORMAT pixelFormat;
    public POINTL position;
}

/// <summary>
/// Contains information about desktop image.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-displayconfig_desktop_image_info
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct DISPLAYCONFIG_DESKTOP_IMAGE_INFO
{
    public POINTL PathSourceSize;
    public RECTL DesktopImageRegion;
    public RECTL DesktopImageClip;
}

/// <summary>
/// Contains video signal information.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-displayconfig_video_signal_info
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct DISPLAYCONFIG_VIDEO_SIGNAL_INFO
{
    public ulong pixelRate;
    public DISPLAYCONFIG_RATIONAL hSyncFreq;
    public DISPLAYCONFIG_RATIONAL vSyncFreq;
    public DISPLAYCONFIG_2DREGION activeSize;
    public DISPLAYCONFIG_2DREGION totalSize;
    public uint videoStandard;
    public DISPLAYCONFIG_SCANLINE_ORDERING scanLineOrdering;
}

/// <summary>
/// Represents a rational number as used in display configuration.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-displayconfig_rational
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct DISPLAYCONFIG_RATIONAL
{
    public uint Numerator;
    public uint Denominator;
}

/// <summary>
/// Represents a 2D region with width and height.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-displayconfig_2dregion
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct DISPLAYCONFIG_2DREGION
{
    public uint cx;
    public uint cy;
}

/// <summary>
/// Base header for device information structures.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-displayconfig_device_info_header
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct DISPLAYCONFIG_DEVICE_INFO_HEADER
{
    public DISPLAYCONFIG_DEVICE_INFO_TYPE type;
    public uint size;
    public LUID adapterId;
    public uint id;
}

/// <summary>
/// Contains target device name information.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-displayconfig_target_device_name
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct DISPLAYCONFIG_TARGET_DEVICE_NAME
{
    public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
    public DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAGS flags;
    public DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY outputTechnology;
    public ushort edidManufactureId;
    public ushort edidProductCodeId;
    public uint connectorInstance;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string monitorFriendlyDeviceName;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string monitorDevicePath;
}

/// <summary>
/// Flags for target device name.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-displayconfig_target_device_name
/// </summary>
[Flags]
public enum DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAGS : uint
{
    DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAG_FRIENDLY_NAME_FROM_EDID = 0x00000001,
    DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAG_FRIENDLY_NAME_FORCED = 0x00000002,
    DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAG_EDID_IDS_VALID = 0x00000004
}

/// <summary>
/// Additional enums needed for the structs.
/// </summary>
public enum DISPLAYCONFIG_SCALING : uint
{
    DISPLAYCONFIG_SCALING_IDENTITY = 1,
    DISPLAYCONFIG_SCALING_CENTERED = 2,
    DISPLAYCONFIG_SCALING_STRETCHED = 3,
    DISPLAYCONFIG_SCALING_ASPECTRATIOCENTEREDMAX = 4,
    DISPLAYCONFIG_SCALING_CUSTOM = 5,
    DISPLAYCONFIG_SCALING_PREFERRED = 128,
    DISPLAYCONFIG_SCALING_FORCE_UINT32 = 0xFFFFFFFF
}

public enum DISPLAYCONFIG_SCANLINE_ORDERING : uint
{
    DISPLAYCONFIG_SCANLINE_ORDERING_UNSPECIFIED = 0,
    DISPLAYCONFIG_SCANLINE_ORDERING_PROGRESSIVE = 1,
    DISPLAYCONFIG_SCANLINE_ORDERING_INTERLACED = 2,
    DISPLAYCONFIG_SCANLINE_ORDERING_INTERLACED_UPPERFIELDFIRST = 2,
    DISPLAYCONFIG_SCANLINE_ORDERING_INTERLACED_LOWERFIELDFIRST = 3,
    DISPLAYCONFIG_SCANLINE_ORDERING_FORCE_UINT32 = 0xFFFFFFFF
}

public enum DISPLAYCONFIG_PIXELFORMAT : uint
{
    DISPLAYCONFIG_PIXELFORMAT_8BPP = 1,
    DISPLAYCONFIG_PIXELFORMAT_16BPP = 2,
    DISPLAYCONFIG_PIXELFORMAT_24BPP = 3,
    DISPLAYCONFIG_PIXELFORMAT_32BPP = 4,
    DISPLAYCONFIG_PIXELFORMAT_NONGDI = 5,
    DISPLAYCONFIG_PIXELFORMAT_FORCE_UINT32 = 0xFFFFFFFF
}

/// <summary>
/// LUID structure as used by Windows APIs.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-luid
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct LUID
{
    public uint LowPart;
    public int HighPart;

    public override bool Equals(object? obj)
    {
        return obj is LUID luid && LowPart == luid.LowPart && HighPart == luid.HighPart;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(LowPart, HighPart);
    }

    public static bool operator ==(LUID left, LUID right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LUID left, LUID right)
    {
        return !(left == right);
    }
}

/// <summary>
/// POINTL structure for coordinates.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/windef/ns-windef-pointl
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct POINTL
{
    public int x;
    public int y;
}

/// <summary>
/// RECTL structure for rectangles.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/windef/ns-windef-rectl
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RECTL
{
    public int left;
    public int top;
    public int right;
    public int bottom;
}
