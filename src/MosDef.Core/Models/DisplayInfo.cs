using MosDef.Core.DisplayConfig;

namespace MosDef.Core.Models;

/// <summary>
/// Represents information about a display monitor for the MOS-DEF utility.
/// Contains all necessary data for display identification and rotation operations.
/// </summary>
public class DisplayInfo
{
    /// <summary>
    /// MOS-DEF assigned identifier (M1, M2, M3, etc.) based on left-to-right ordering.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Friendly display name as reported by Windows.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Connection type (HDMI, DISPLAYPORT, INTERNAL, etc.).
    /// </summary>
    public string ConnectionType { get; set; } = string.Empty;

    /// <summary>
    /// Display resolution in "WIDTHxHEIGHT" format.
    /// </summary>
    public string Resolution { get; set; } = string.Empty;

    /// <summary>
    /// Current rotation in degrees (0, 90, 180, 270).
    /// </summary>
    public int RotationDegrees { get; set; }

    /// <summary>
    /// Stable device path hash key for reliable identification (e.g., "7a1c-2f9e").
    /// </summary>
    public string PathKey { get; set; } = string.Empty;

    /// <summary>
    /// Internal Windows adapter LUID for API calls.
    /// </summary>
    internal LUID AdapterId { get; set; }

    /// <summary>
    /// Internal Windows target ID for API calls.
    /// </summary>
    internal uint TargetId { get; set; }

    /// <summary>
    /// Full device path from Windows (used for PathKey generation).
    /// </summary>
    internal string DevicePath { get; set; } = string.Empty;

    /// <summary>
    /// Index in the display path array for configuration updates.
    /// </summary>
    internal int PathIndex { get; set; }

    /// <summary>
    /// Creates a new DisplayInfo instance.
    /// </summary>
    public DisplayInfo() { }

    /// <summary>
    /// Creates a new DisplayInfo instance from Windows display configuration data.
    /// </summary>
    /// <param name="pathIndex">Index in the display paths array</param>
    /// <param name="pathInfo">Windows display path information</param>
    /// <param name="deviceName">Windows target device name information</param>
    /// <param name="sourceMode">Source mode information for resolution</param>
    /// <param name="mosDefIndex">MOS-DEF assigned index (1-based)</param>
    internal DisplayInfo(int pathIndex, DISPLAYCONFIG_PATH_INFO pathInfo, DISPLAYCONFIG_TARGET_DEVICE_NAME deviceName, DISPLAYCONFIG_SOURCE_MODE? sourceMode, int mosDefIndex)
    {
        PathIndex = pathIndex;
        Id = $"M{mosDefIndex}";
        Name = deviceName.monitorFriendlyDeviceName?.Trim() ?? "Unknown Monitor";
        ConnectionType = User32DisplayConfig.GetConnectionTypeName(deviceName.outputTechnology);
        RotationDegrees = User32DisplayConfig.GetRotationDegrees(pathInfo.targetInfo.rotation);
        DevicePath = deviceName.monitorDevicePath?.Trim() ?? string.Empty;
        PathKey = User32DisplayConfig.GeneratePathKey(DevicePath);
        AdapterId = pathInfo.targetInfo.adapterId;
        TargetId = pathInfo.targetInfo.id;

        // Set resolution from source mode if available
        if (sourceMode.HasValue)
        {
            var mode = sourceMode.Value;
            Resolution = $"{mode.width}x{mode.height}";
        }
        else
        {
            Resolution = "Unknown";
        }
    }

    /// <summary>
    /// Determines if this display matches the given selector.
    /// </summary>
    /// <param name="selector">Selector string (M#, name:, conn:, path:, name:/regex/)</param>
    /// <returns>True if the display matches the selector</returns>
    public bool MatchesSelector(string selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
            return false;

        selector = selector.Trim();

        // M# selector (e.g., M1, M2)
        if (selector.StartsWith("M", StringComparison.OrdinalIgnoreCase) && 
            selector.Length > 1 && 
            char.IsDigit(selector[1]))
        {
            return string.Equals(Id, selector, StringComparison.OrdinalIgnoreCase);
        }

        // path: selector (e.g., path:7a1c-2f9e)
        if (selector.StartsWith("path:", StringComparison.OrdinalIgnoreCase))
        {
            var pathValue = selector[5..].Trim();
            return string.Equals(PathKey, pathValue, StringComparison.OrdinalIgnoreCase);
        }

        // conn: selector (e.g., conn:HDMI, conn:DISPLAYPORT)
        if (selector.StartsWith("conn:", StringComparison.OrdinalIgnoreCase))
        {
            var connValue = selector[5..].Trim();
            return string.Equals(ConnectionType, connValue, StringComparison.OrdinalIgnoreCase);
        }

        // name: selector with regex support (e.g., name:"DELL U2720Q", name:DELL, name:/DELL.*/)
        if (selector.StartsWith("name:", StringComparison.OrdinalIgnoreCase))
        {
            var nameValue = selector[5..].Trim();

            // Remove quotes if present
            if (nameValue.StartsWith('"') && nameValue.EndsWith('"') && nameValue.Length > 1)
            {
                nameValue = nameValue[1..^1];
                // Exact match for quoted strings
                return string.Equals(Name, nameValue, StringComparison.OrdinalIgnoreCase);
            }

            // Regex pattern (e.g., name:/DELL.*/)
            if (nameValue.StartsWith('/') && nameValue.EndsWith('/') && nameValue.Length > 2)
            {
                try
                {
                    var pattern = nameValue[1..^1];
                    return System.Text.RegularExpressions.Regex.IsMatch(Name, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
                catch
                {
                    // Invalid regex, fall back to partial match
                    return Name.Contains(nameValue[1..^1], StringComparison.OrdinalIgnoreCase);
                }
            }

            // Partial match for unquoted strings
            return Name.Contains(nameValue, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    /// <summary>
    /// Gets the next rotation in the toggle sequence (0° → 90° → 0°).
    /// </summary>
    /// <returns>Next rotation in degrees</returns>
    public int GetNextToggleRotation()
    {
        return RotationDegrees switch
        {
            0 => 90,   // landscape → portrait
            90 => 0,   // portrait → landscape
            180 => 90, // upside down landscape → portrait
            270 => 0,  // upside down portrait → landscape
            _ => 90    // unknown → portrait
        };
    }

    /// <summary>
    /// Returns a string representation suitable for the --list output.
    /// </summary>
    /// <returns>Formatted string for display listing</returns>
    public override string ToString()
    {
        return $"{Id,-3} {Name,-15} {ConnectionType,-8} {Resolution,-11} {RotationDegrees,3}  {PathKey}";
    }

    /// <summary>
    /// Returns a string representation with a specific width for table formatting.
    /// </summary>
    /// <param name="idWidth">Width for ID column</param>
    /// <param name="nameWidth">Width for name column</param>
    /// <param name="connWidth">Width for connection column</param>
    /// <param name="resWidth">Width for resolution column</param>
    /// <returns>Formatted string for display listing</returns>
    public string ToString(int idWidth, int nameWidth, int connWidth, int resWidth)
    {
        var truncatedName = Name.Length > nameWidth ? Name[..(nameWidth - 3)] + "..." : Name;
        return $"{Id.PadRight(idWidth)} {truncatedName.PadRight(nameWidth)} {ConnectionType.PadRight(connWidth)} {Resolution.PadRight(resWidth)} {RotationDegrees,3}  {PathKey}";
    }
}
