using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MosDef.Core.DisplayConfig;

/// <summary>
/// P/Invoke declarations for Windows Display Configuration APIs.
/// Provides low-level access to Windows display configuration functionality.
/// </summary>
public static class User32DisplayConfig
{
    /// <summary>
    /// Retrieves information about all possible display paths for a given set of adapters.
    /// Reference: https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-querydisplayconfig
    /// </summary>
    /// <param name="flags">Flags specifying the type of information to retrieve</param>
    /// <param name="numPathArrayElements">Number of elements in the pathArray</param>
    /// <param name="pathArray">Array of DISPLAYCONFIG_PATH_INFO structures</param>
    /// <param name="numModeInfoArrayElements">Number of elements in the modeInfoArray</param>
    /// <param name="modeInfoArray">Array of DISPLAYCONFIG_MODE_INFO structures</param>
    /// <param name="currentTopologyId">Current topology identifier</param>
    /// <returns>ERROR_SUCCESS if successful, otherwise a Win32 error code</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint QueryDisplayConfig(
        QDC_FLAGS flags,
        ref uint numPathArrayElements,
        [Out] DISPLAYCONFIG_PATH_INFO[]? pathArray,
        ref uint numModeInfoArrayElements,
        [Out] DISPLAYCONFIG_MODE_INFO[]? modeInfoArray,
        IntPtr currentTopologyId);

    /// <summary>
    /// Sets the display configuration.
    /// Reference: https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setdisplayconfig
    /// </summary>
    /// <param name="numPathArrayElements">Number of elements in the pathArray</param>
    /// <param name="pathArray">Array of DISPLAYCONFIG_PATH_INFO structures</param>
    /// <param name="numModeInfoArrayElements">Number of elements in the modeInfoArray</param>
    /// <param name="modeInfoArray">Array of DISPLAYCONFIG_MODE_INFO structures</param>
    /// <param name="flags">Flags specifying how to set the configuration</param>
    /// <returns>ERROR_SUCCESS if successful, otherwise a Win32 error code</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SetDisplayConfig(
        uint numPathArrayElements,
        [In] DISPLAYCONFIG_PATH_INFO[]? pathArray,
        uint numModeInfoArrayElements,
        [In] DISPLAYCONFIG_MODE_INFO[]? modeInfoArray,
        SDC_FLAGS flags);

    /// <summary>
    /// Retrieves display configuration information about the device.
    /// Reference: https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-displayconfiggetdeviceinfo
    /// </summary>
    /// <param name="requestPacket">Pointer to device information header</param>
    /// <returns>ERROR_SUCCESS if successful, otherwise a Win32 error code</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_TARGET_DEVICE_NAME requestPacket);

    /// <summary>
    /// Gets the last Win32 error code.
    /// Reference: https://docs.microsoft.com/en-us/windows/win32/api/errhandlingapi/nf-errhandlingapi-getlasterror
    /// </summary>
    /// <returns>The last error code</returns>
    [DllImport("kernel32.dll")]
    public static extern uint GetLastError();

    /// <summary>
    /// Gets all active display paths and mode information from Windows.
    /// </summary>
    /// <returns>Tuple containing path info array and mode info array, or null if failed</returns>
    /// <exception cref="Win32Exception">Thrown when Windows API call fails</exception>
    public static (DISPLAYCONFIG_PATH_INFO[] paths, DISPLAYCONFIG_MODE_INFO[] modes) GetActiveDisplayConfiguration()
    {
        uint pathCount = 0;
        uint modeCount = 0;

        // First call to get the array sizes
        uint result = QueryDisplayConfig(
            QDC_FLAGS.QDC_ONLY_ACTIVE_PATHS,
            ref pathCount,
            null,
            ref modeCount,
            null,
            IntPtr.Zero);

        if (result != (uint)DisplayConfigResult.ERROR_SUCCESS)
        {
            throw new Win32Exception((int)result, $"QueryDisplayConfig failed to get array sizes. Error code: {result}");
        }

        if (pathCount == 0)
        {
            return (Array.Empty<DISPLAYCONFIG_PATH_INFO>(), Array.Empty<DISPLAYCONFIG_MODE_INFO>());
        }

        // Allocate arrays and get the actual data
        var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
        var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

        result = QueryDisplayConfig(
            QDC_FLAGS.QDC_ONLY_ACTIVE_PATHS,
            ref pathCount,
            paths,
            ref modeCount,
            modes,
            IntPtr.Zero);

        if (result != (uint)DisplayConfigResult.ERROR_SUCCESS)
        {
            throw new Win32Exception((int)result, $"QueryDisplayConfig failed to get display configuration. Error code: {result}");
        }

        // Resize arrays if Windows returned fewer elements than initially requested
        if (pathCount < paths.Length)
        {
            Array.Resize(ref paths, (int)pathCount);
        }
        if (modeCount < modes.Length)
        {
            Array.Resize(ref modes, (int)modeCount);
        }

        return (paths, modes);
    }

    /// <summary>
    /// Applies display configuration changes to Windows.
    /// </summary>
    /// <param name="paths">Array of display path information</param>
    /// <param name="modes">Array of display mode information</param>
    /// <param name="saveToDatabase">Whether to save changes to the Windows display database</param>
    /// <exception cref="Win32Exception">Thrown when Windows API call fails</exception>
    public static void ApplyDisplayConfiguration(DISPLAYCONFIG_PATH_INFO[] paths, DISPLAYCONFIG_MODE_INFO[] modes, bool saveToDatabase = true)
    {
        var flags = SDC_FLAGS.SDC_APPLY | SDC_FLAGS.SDC_USE_SUPPLIED_DISPLAY_CONFIG;
        if (saveToDatabase)
        {
            flags |= SDC_FLAGS.SDC_SAVE_TO_DATABASE;
        }

        uint result = SetDisplayConfig(
            (uint)paths.Length,
            paths,
            (uint)modes.Length,
            modes,
            flags);

        if (result != (uint)DisplayConfigResult.ERROR_SUCCESS)
        {
            throw new Win32Exception((int)result, $"SetDisplayConfig failed to apply configuration. Error code: {result}");
        }
    }

    /// <summary>
    /// Gets detailed information about a target display device.
    /// </summary>
    /// <param name="adapterId">Adapter LUID</param>
    /// <param name="targetId">Target ID</param>
    /// <returns>Target device name information</returns>
    /// <exception cref="Win32Exception">Thrown when Windows API call fails</exception>
    public static DISPLAYCONFIG_TARGET_DEVICE_NAME GetTargetDeviceName(LUID adapterId, uint targetId)
    {
        var deviceName = new DISPLAYCONFIG_TARGET_DEVICE_NAME
        {
            header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
            {
                type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME,
                size = (uint)Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>(),
                adapterId = adapterId,
                id = targetId
            }
        };

        uint result = DisplayConfigGetDeviceInfo(ref deviceName);

        if (result != (uint)DisplayConfigResult.ERROR_SUCCESS)
        {
            throw new Win32Exception((int)result, $"DisplayConfigGetDeviceInfo failed to get target device name. Error code: {result}");
        }

        return deviceName;
    }

    /// <summary>
    /// Converts a DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY to a friendly connection type string.
    /// </summary>
    /// <param name="outputTechnology">The output technology enum value</param>
    /// <returns>Friendly connection type name</returns>
    public static string GetConnectionTypeName(DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY outputTechnology)
    {
        return outputTechnology switch
        {
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_HDMI => "HDMI",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_EXTERNAL => "DISPLAYPORT",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_EMBEDDED => "DISPLAYPORT",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_USB_TUNNEL => "DISPLAYPORT",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DVI => "DVI",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INTERNAL => "INTERNAL",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_LVDS => "INTERNAL",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_HD15 => "VGA",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_SVIDEO => "SVIDEO",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_COMPOSITE_VIDEO => "COMPOSITE",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_COMPONENT_VIDEO => "COMPONENT",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_MIRACAST => "MIRACAST",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INDIRECT_WIRED => "INDIRECT",
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INDIRECT_VIRTUAL => "VIRTUAL",
            _ => "OTHER"
        };
    }

    /// <summary>
    /// Converts DISPLAYCONFIG_ROTATION to rotation degrees.
    /// </summary>
    /// <param name="rotation">The rotation enum value</param>
    /// <returns>Rotation in degrees (0, 90, 180, 270)</returns>
    public static int GetRotationDegrees(DISPLAYCONFIG_ROTATION rotation)
    {
        return rotation switch
        {
            DISPLAYCONFIG_ROTATION.DISPLAYCONFIG_ROTATION_IDENTITY => 0,
            DISPLAYCONFIG_ROTATION.DISPLAYCONFIG_ROTATION_ROTATE90 => 90,
            DISPLAYCONFIG_ROTATION.DISPLAYCONFIG_ROTATION_ROTATE180 => 180,
            DISPLAYCONFIG_ROTATION.DISPLAYCONFIG_ROTATION_ROTATE270 => 270,
            _ => 0
        };
    }

    /// <summary>
    /// Converts rotation degrees to DISPLAYCONFIG_ROTATION.
    /// </summary>
    /// <param name="degrees">Rotation in degrees (0, 90, 180, 270)</param>
    /// <returns>DISPLAYCONFIG_ROTATION enum value</returns>
    /// <exception cref="ArgumentException">Thrown for unsupported rotation angles</exception>
    public static DISPLAYCONFIG_ROTATION GetRotationFromDegrees(int degrees)
    {
        return degrees switch
        {
            0 => DISPLAYCONFIG_ROTATION.DISPLAYCONFIG_ROTATION_IDENTITY,
            90 => DISPLAYCONFIG_ROTATION.DISPLAYCONFIG_ROTATION_ROTATE90,
            180 => DISPLAYCONFIG_ROTATION.DISPLAYCONFIG_ROTATION_ROTATE180,
            270 => DISPLAYCONFIG_ROTATION.DISPLAYCONFIG_ROTATION_ROTATE270,
            _ => throw new ArgumentException($"Unsupported rotation angle: {degrees}. Supported angles are 0, 90, 180, 270.", nameof(degrees))
        };
    }

    /// <summary>
    /// Generates a stable hash key from a device path for monitor identification.
    /// </summary>
    /// <param name="devicePath">The device path string</param>
    /// <returns>8-character hash key</returns>
    public static string GeneratePathKey(string devicePath)
    {
        if (string.IsNullOrEmpty(devicePath))
            return "0000-0000";

        var hash = devicePath.GetHashCode();
        var hashBytes = BitConverter.GetBytes(hash);
        return $"{hashBytes[0]:x2}{hashBytes[1]:x2}-{hashBytes[2]:x2}{hashBytes[3]:x2}";
    }
}
