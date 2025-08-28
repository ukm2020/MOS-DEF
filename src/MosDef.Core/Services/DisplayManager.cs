using System.ComponentModel;
using MosDef.Core.DisplayConfig;
using MosDef.Core.Models;

namespace MosDef.Core.Services;

/// <summary>
/// Manages display configuration operations for the MOS-DEF utility.
/// Provides high-level methods for discovering displays and applying rotation changes.
/// </summary>
public class DisplayManager
{
    /// <summary>
    /// Discovers all active displays connected to the system.
    /// </summary>
    /// <returns>List of DisplayInfo objects representing active displays</returns>
    /// <exception cref="InvalidOperationException">Thrown when display discovery fails</exception>
    public static List<DisplayInfo> GetActiveDisplays()
    {
        try
        {
            var (paths, modes) = User32DisplayConfig.GetActiveDisplayConfiguration();
            var displays = new List<DisplayInfo>();

            // Create a dictionary for quick mode lookup
            var modeDict = modes.ToDictionary(
                mode => new { mode.adapterId, mode.id },
                mode => mode
            );

            // Sort paths by horizontal position to assign M1, M2, M3 from left to right
            var sortedPaths = paths
                .Select((path, index) => new { Path = path, Index = index })
                .Where(item => item.Path.targetInfo.targetAvailable)
                .OrderBy(item =>
                {
                    // Try to get source mode for position information
                    var sourceKey = new { item.Path.sourceInfo.adapterId, id = item.Path.sourceInfo.id };
                    if (modeDict.TryGetValue(sourceKey, out var sourceMode) && 
                        sourceMode.infoType == DISPLAYCONFIG_MODE_INFO_TYPE.DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE)
                    {
                        return sourceMode.modeInfo.sourceMode.position.x;
                    }
                    // Fallback to target ID for ordering
                    return (int)item.Path.targetInfo.id;
                })
                .ToList();

            int mosDefIndex = 1;

            foreach (var pathItem in sortedPaths)
            {
                try
                {
                    var path = pathItem.Path;
                    var pathIndex = pathItem.Index;

                    // Get device name information
                    var deviceName = User32DisplayConfig.GetTargetDeviceName(
                        path.targetInfo.adapterId,
                        path.targetInfo.id);

                    // Get source mode for resolution if available
                    DISPLAYCONFIG_SOURCE_MODE? sourceMode = null;
                    var sourceKey = new { path.sourceInfo.adapterId, id = path.sourceInfo.id };
                    if (modeDict.TryGetValue(sourceKey, out var mode) && 
                        mode.infoType == DISPLAYCONFIG_MODE_INFO_TYPE.DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE)
                    {
                        sourceMode = mode.modeInfo.sourceMode;
                    }

                    var display = new DisplayInfo(pathIndex, path, deviceName, sourceMode, mosDefIndex);
                    displays.Add(display);
                    mosDefIndex++;
                }
                catch (Win32Exception ex)
                {
                    // Log and continue with other displays
                    Console.WriteLine($"Warning: Failed to get information for display {pathItem.Index}: {ex.Message}");
                }
            }

            return displays;
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException($"Failed to discover active displays: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Applies rotation to all specified displays.
    /// </summary>
    /// <param name="targetDisplays">List of displays to rotate</param>
    /// <param name="rotationDegrees">Target rotation in degrees (0, 90, 180, 270)</param>
    /// <param name="dryRun">If true, only simulates the operation without applying changes</param>
    /// <param name="verbose">If true, provides detailed output about the operation</param>
    /// <returns>Number of displays that were changed</returns>
    /// <exception cref="ArgumentException">Thrown for invalid rotation values</exception>
    /// <exception cref="InvalidOperationException">Thrown when the operation fails</exception>
    public static int ApplyRotationToDisplays(List<DisplayInfo> targetDisplays, int rotationDegrees, bool dryRun = false, bool verbose = false)
    {
        if (targetDisplays == null || targetDisplays.Count == 0)
            return 0;

        // Validate rotation
        if (rotationDegrees != 0 && rotationDegrees != 90 && rotationDegrees != 180 && rotationDegrees != 270)
        {
            throw new ArgumentException($"Invalid rotation: {rotationDegrees}. Supported rotations are 0, 90, 180, 270.", nameof(rotationDegrees));
        }

        if (dryRun)
        {
            // Count how many displays would actually change
            int changeCount = 0;
            foreach (var display in targetDisplays)
            {
                if (display.RotationDegrees != rotationDegrees)
                {
                    changeCount++;
                    if (verbose)
                    {
                        Console.WriteLine($"[DRY RUN] Would rotate {display.Id} ({display.Name}) from {display.RotationDegrees}° to {rotationDegrees}°");
                    }
                }
                else if (verbose)
                {
                    Console.WriteLine($"[DRY RUN] {display.Id} ({display.Name}) already at {rotationDegrees}°, no change needed");
                }
            }
            return changeCount;
        }

        try
        {
            // Get current configuration
            var (paths, modes) = User32DisplayConfig.GetActiveDisplayConfiguration();
            int changedCount = 0;

            // Apply rotation changes to the paths array
            foreach (var display in targetDisplays)
            {
                if (display.PathIndex >= 0 && display.PathIndex < paths.Length)
                {
                    var currentRotation = User32DisplayConfig.GetRotationDegrees(paths[display.PathIndex].targetInfo.rotation);
                    
                    if (currentRotation != rotationDegrees)
                    {
                        paths[display.PathIndex].targetInfo.rotation = User32DisplayConfig.GetRotationFromDegrees(rotationDegrees);
                        changedCount++;
                        
                        if (verbose)
                        {
                            Console.WriteLine($"Rotating {display.Id} ({display.Name}) from {currentRotation}° to {rotationDegrees}°");
                        }
                    }
                    else if (verbose)
                    {
                        Console.WriteLine($"{display.Id} ({display.Name}) already at {rotationDegrees}°, no change needed");
                    }
                }
            }

            // Apply the configuration if any changes were made
            if (changedCount > 0)
            {
                User32DisplayConfig.ApplyDisplayConfiguration(paths, modes, saveToDatabase: true);
                
                if (verbose)
                {
                    Console.WriteLine($"Successfully applied rotation changes to {changedCount} display(s)");
                }
            }

            return changedCount;
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException($"Failed to apply rotation changes: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Toggles rotation for all specified displays between landscape (0°) and portrait (90°).
    /// </summary>
    /// <param name="targetDisplays">List of displays to toggle</param>
    /// <param name="dryRun">If true, only simulates the operation without applying changes</param>
    /// <param name="verbose">If true, provides detailed output about the operation</param>
    /// <returns>Number of displays that were changed</returns>
    /// <exception cref="InvalidOperationException">Thrown when the operation fails</exception>
    public static int ToggleDisplayRotation(List<DisplayInfo> targetDisplays, bool dryRun = false, bool verbose = false)
    {
        if (targetDisplays == null || targetDisplays.Count == 0)
            return 0;

        if (dryRun)
        {
            // Count how many displays would change and show what would happen
            int changeCount = 0;
            foreach (var display in targetDisplays)
            {
                var nextRotation = display.GetNextToggleRotation();
                if (display.RotationDegrees != nextRotation)
                {
                    changeCount++;
                    if (verbose)
                    {
                        Console.WriteLine($"[DRY RUN] Would toggle {display.Id} ({display.Name}) from {display.RotationDegrees}° to {nextRotation}°");
                    }
                }
                else if (verbose)
                {
                    Console.WriteLine($"[DRY RUN] {display.Id} ({display.Name}) already at target rotation {nextRotation}°");
                }
            }
            return changeCount;
        }

        try
        {
            // Get current configuration
            var (paths, modes) = User32DisplayConfig.GetActiveDisplayConfiguration();
            int changedCount = 0;

            // Apply toggle changes to the paths array
            foreach (var display in targetDisplays)
            {
                if (display.PathIndex >= 0 && display.PathIndex < paths.Length)
                {
                    var currentRotation = User32DisplayConfig.GetRotationDegrees(paths[display.PathIndex].targetInfo.rotation);
                    var nextRotation = display.GetNextToggleRotation();
                    
                    if (currentRotation != nextRotation)
                    {
                        paths[display.PathIndex].targetInfo.rotation = User32DisplayConfig.GetRotationFromDegrees(nextRotation);
                        changedCount++;
                        
                        if (verbose)
                        {
                            Console.WriteLine($"Toggling {display.Id} ({display.Name}) from {currentRotation}° to {nextRotation}°");
                        }
                    }
                    else if (verbose)
                    {
                        Console.WriteLine($"{display.Id} ({display.Name}) already at target rotation {nextRotation}°");
                    }
                }
            }

            // Apply the configuration if any changes were made
            if (changedCount > 0)
            {
                User32DisplayConfig.ApplyDisplayConfiguration(paths, modes, saveToDatabase: true);
                
                if (verbose)
                {
                    Console.WriteLine($"Successfully toggled rotation for {changedCount} display(s)");
                }
            }

            return changedCount;
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException($"Failed to toggle display rotation: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Filters displays based on selector criteria.
    /// </summary>
    /// <param name="allDisplays">All available displays</param>
    /// <param name="selectors">List of selector strings</param>
    /// <returns>Filtered list of displays matching any of the selectors</returns>
    public static List<DisplayInfo> FilterDisplaysBySelectors(List<DisplayInfo> allDisplays, List<string> selectors)
    {
        if (allDisplays == null || allDisplays.Count == 0 || selectors == null || selectors.Count == 0)
            return new List<DisplayInfo>();

        var matches = new HashSet<DisplayInfo>();

        foreach (var selector in selectors.Where(s => !string.IsNullOrWhiteSpace(s)))
        {
            foreach (var display in allDisplays)
            {
                if (display.MatchesSelector(selector))
                {
                    matches.Add(display);
                }
            }
        }

        return matches.ToList();
    }

    /// <summary>
    /// Generates a formatted table of displays for the --list command.
    /// </summary>
    /// <param name="displays">List of displays to format</param>
    /// <returns>Formatted table string</returns>
    public static string FormatDisplayList(List<DisplayInfo> displays)
    {
        if (displays == null || displays.Count == 0)
            return "No active displays found.";

        var lines = new List<string>();
        
        // Calculate column widths
        var maxNameLength = Math.Max(4, displays.Max(d => d.Name.Length));
        var maxConnLength = Math.Max(4, displays.Max(d => d.ConnectionType.Length));
        var maxResLength = Math.Max(10, displays.Max(d => d.Resolution.Length));
        
        // Limit column widths for readability
        maxNameLength = Math.Min(maxNameLength, 20);
        maxConnLength = Math.Min(maxConnLength, 12);
        maxResLength = Math.Min(maxResLength, 15);

        // Header
        var header = $"{"ID".PadRight(3)} {"NAME".PadRight(maxNameLength)} {"CONN".PadRight(maxConnLength)} {"RESOLUTION".PadRight(maxResLength)} {"ROT"} {"KEY"}";
        lines.Add(header);

        // Data rows
        foreach (var display in displays)
        {
            lines.Add(display.ToString(3, maxNameLength, maxConnLength, maxResLength));
        }

        return string.Join(Environment.NewLine, lines);
    }
}
