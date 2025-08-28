using System.Runtime.InteropServices;

namespace MosDef.Core.DisplayConfig;

/// <summary>
/// Display configuration rotation values from Windows API.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ne-wingdi-displayconfig_rotation
/// </summary>
public enum DISPLAYCONFIG_ROTATION : uint
{
    /// <summary>
    /// 0 degrees - landscape orientation
    /// </summary>
    DISPLAYCONFIG_ROTATION_IDENTITY = 1,

    /// <summary>
    /// 90 degrees clockwise - portrait orientation
    /// </summary>
    DISPLAYCONFIG_ROTATION_ROTATE90 = 2,

    /// <summary>
    /// 180 degrees - upside down landscape
    /// </summary>
    DISPLAYCONFIG_ROTATION_ROTATE180 = 3,

    /// <summary>
    /// 270 degrees clockwise - upside down portrait
    /// </summary>
    DISPLAYCONFIG_ROTATION_ROTATE270 = 4
}

/// <summary>
/// Display configuration mode information type.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ne-wingdi-displayconfig_mode_info_type
/// </summary>
public enum DISPLAYCONFIG_MODE_INFO_TYPE : uint
{
    DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE = 1,
    DISPLAYCONFIG_MODE_INFO_TYPE_TARGET = 2,
    DISPLAYCONFIG_MODE_INFO_TYPE_DESKTOP_IMAGE = 3
}

/// <summary>
/// Display configuration topology identifiers.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ne-wingdi-displayconfig_topology_id
/// </summary>
public enum DISPLAYCONFIG_TOPOLOGY_ID : uint
{
    DISPLAYCONFIG_TOPOLOGY_INTERNAL = 0x00000001,
    DISPLAYCONFIG_TOPOLOGY_CLONE = 0x00000002,
    DISPLAYCONFIG_TOPOLOGY_EXTEND = 0x00000004,
    DISPLAYCONFIG_TOPOLOGY_EXTERNAL = 0x00000008,
    DISPLAYCONFIG_TOPOLOGY_FORCE_UINT32 = 0xFFFFFFFF
}

/// <summary>
/// SetDisplayConfig flags for applying display configuration changes.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setdisplayconfig
/// </summary>
[Flags]
public enum SDC_FLAGS : uint
{
    /// <summary>
    /// Apply the settings immediately
    /// </summary>
    SDC_APPLY = 0x00000080,

    /// <summary>
    /// Save the settings to the database
    /// </summary>
    SDC_SAVE_TO_DATABASE = 0x00000200,

    /// <summary>
    /// Use the supplied display configuration
    /// </summary>
    SDC_USE_SUPPLIED_DISPLAY_CONFIG = 0x00000020,

    /// <summary>
    /// Force projection
    /// </summary>
    SDC_FORCE_MODE_ENUMERATION = 0x00001000,

    /// <summary>
    /// Allow changes in topology
    /// </summary>
    SDC_ALLOW_CHANGES = 0x00000400
}

/// <summary>
/// QueryDisplayConfig flags for retrieving display configuration.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-querydisplayconfig
/// </summary>
[Flags]
public enum QDC_FLAGS : uint
{
    /// <summary>
    /// Query all display paths that are currently active
    /// </summary>
    QDC_ALL_PATHS = 0x00000001,

    /// <summary>
    /// Query only active display paths
    /// </summary>
    QDC_ONLY_ACTIVE_PATHS = 0x00000002,

    /// <summary>
    /// Query all display paths in the database
    /// </summary>
    QDC_DATABASE_CURRENT = 0x00000004
}

/// <summary>
/// Display configuration device information types.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ne-wingdi-displayconfig_device_info_type
/// </summary>
public enum DISPLAYCONFIG_DEVICE_INFO_TYPE : uint
{
    DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1,
    DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2,
    DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_PREFERRED_MODE = 3,
    DISPLAYCONFIG_DEVICE_INFO_GET_ADAPTER_NAME = 4,
    DISPLAYCONFIG_DEVICE_INFO_SET_TARGET_PERSISTENCE = 5,
    DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_BASE_TYPE = 6,
    DISPLAYCONFIG_DEVICE_INFO_GET_SUPPORT_VIRTUAL_RESOLUTION = 7,
    DISPLAYCONFIG_DEVICE_INFO_SET_SUPPORT_VIRTUAL_RESOLUTION = 8,
    DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO = 9,
    DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE = 10,
    DISPLAYCONFIG_DEVICE_INFO_GET_SDR_WHITE_LEVEL = 11,
    DISPLAYCONFIG_DEVICE_INFO_FORCE_UINT32 = 0xFFFFFFFF
}

/// <summary>
/// Display configuration video output technology types.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ne-wingdi-displayconfig_video_output_technology
/// </summary>
public enum DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY : uint
{
    DISPLAYCONFIG_OUTPUT_TECHNOLOGY_OTHER = 0xFFFFFFFF,
    DISPLAYCONFIG_OUTPUT_TECHNOLOGY_HD15 = 0,
    DISPLAYCONFIG_OUTPUT_TECHNOLOGY_SVIDEO = 1,
    DISPLAYCONFIG_OUTPUT_TECHNOLOGY_COMPOSITE_VIDEO = 2,
    DISPLAYCONFIG_OUTPUT_TECHNOLOGY_COMPONENT_VIDEO = 3,
    DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DVI = 4,
    DISPLAYCONFIG_OUTPUT_TECHNOLOGY_HDMI = 5,
    DISPLAYCONFIG_OUTPUT_TECHNOLOGY_LVDS = 6,
    DISPLAYCONFIG_OUTPUT_TECHNOLOGY_D_JPN = 8,
    DISPLAYCONFIG_OUTPUT_TECHNOLOGY_SDI = 9,
    DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_EXTERNAL = 10,
    DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_EMBEDDED = 11,
    DISPLAYCONFIG_OUTPUT_TECHNOLOGY_UDI_EXTERNAL = 12,
    DISPLAYCONFIG_OUTPUT_TECHNOLOGY_UDI_EMBEDDED = 13,
    DISPLAYCONFIG_OUTPUT_TECHNOLOGY_SDTVDONGLE = 14,
    DISPLAYCONFIG_OUTPUT_TECHNOLOGY_MIRACAST = 15,
    DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INDIRECT_WIRED = 16,
    DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INDIRECT_VIRTUAL = 17,
    DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_USB_TUNNEL = 18,
    DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INTERNAL = 0x80000000,
    DISPLAYCONFIG_OUTPUT_TECHNOLOGY_FORCE_UINT32 = 0xFFFFFFFF
}

/// <summary>
/// Common error codes returned by display configuration APIs.
/// </summary>
public enum DisplayConfigResult : uint
{
    ERROR_SUCCESS = 0,
    ERROR_INVALID_PARAMETER = 87,
    ERROR_NOT_SUPPORTED = 50,
    ERROR_ACCESS_DENIED = 5,
    ERROR_INSUFFICIENT_BUFFER = 122,
    ERROR_CALL_NOT_IMPLEMENTED = 120
}
