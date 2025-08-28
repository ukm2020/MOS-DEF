using MosDef.Core.Models;

namespace MosDef.Core.Services;

/// <summary>
/// Parses and evaluates monitor selector expressions for the MOS-DEF utility.
/// Supports M#, name:, conn:, path:, and regex selectors with precedence rules.
/// </summary>
public static class SelectorParser
{
    /// <summary>
    /// Parses a comma-separated list of selectors into individual selector strings.
    /// </summary>
    /// <param name="selectorString">Comma-separated selector string (e.g., "M1,M3" or "name:DELL,conn:HDMI")</param>
    /// <returns>List of individual selector strings</returns>
    public static List<string> ParseSelectorList(string selectorString)
    {
        if (string.IsNullOrWhiteSpace(selectorString))
            return new List<string>();

        // Split by comma and trim whitespace
        return selectorString
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }

    /// <summary>
    /// Applies selector filtering to a list of displays based on operation type.
    /// </summary>
    /// <param name="allDisplays">All available displays</param>
    /// <param name="onlySelectors">Selectors for --only flag (exclusive)</param>
    /// <param name="includeSelectors">Selectors for --include flag (additive)</param>
    /// <param name="excludeSelectors">Selectors for --exclude flag (subtractive)</param>
    /// <param name="defaultSelector">Default selector from configuration</param>
    /// <returns>Filtered list of displays</returns>
    public static List<DisplayInfo> ApplySelectors(
        List<DisplayInfo> allDisplays,
        List<string>? onlySelectors = null,
        List<string>? includeSelectors = null,
        List<string>? excludeSelectors = null,
        string? defaultSelector = null)
    {
        if (allDisplays == null || allDisplays.Count == 0)
            return new List<DisplayInfo>();

        // If --only is specified, use only those displays
        if (onlySelectors != null && onlySelectors.Count > 0)
        {
            var onlyMatches = DisplayManager.FilterDisplaysBySelectors(allDisplays, onlySelectors);
            return ApplyPrecedenceRules(onlyMatches, onlySelectors);
        }

        // If --include is specified, start with those displays
        var targetDisplays = new HashSet<DisplayInfo>();
        if (includeSelectors != null && includeSelectors.Count > 0)
        {
            var includeMatches = DisplayManager.FilterDisplaysBySelectors(allDisplays, includeSelectors);
            var orderedIncludes = ApplyPrecedenceRules(includeMatches, includeSelectors);
            foreach (var display in orderedIncludes)
            {
                targetDisplays.Add(display);
            }
        }
        else if (!string.IsNullOrWhiteSpace(defaultSelector))
        {
            // Use default selector if no include/only flags are specified
            var defaultMatches = DisplayManager.FilterDisplaysBySelectors(allDisplays, new List<string> { defaultSelector });
            foreach (var display in defaultMatches)
            {
                targetDisplays.Add(display);
            }
        }
        else
        {
            // No selectors, default to all displays
            foreach (var display in allDisplays)
            {
                targetDisplays.Add(display);
            }
        }

        // Apply --exclude filters
        if (excludeSelectors != null && excludeSelectors.Count > 0)
        {
            var excludeMatches = DisplayManager.FilterDisplaysBySelectors(allDisplays, excludeSelectors);
            foreach (var display in excludeMatches)
            {
                targetDisplays.Remove(display);
            }
        }

        return targetDisplays.ToList();
    }

    /// <summary>
    /// Applies selector precedence rules to resolve conflicts when multiple selectors match the same display.
    /// Precedence order: 1) path:, 2) M#, 3) name: (exact > partial > regex), 4) conn:
    /// </summary>
    /// <param name="displays">Displays to filter</param>
    /// <param name="selectors">Selectors that were used to match</param>
    /// <returns>Displays ordered by selector precedence</returns>
    public static List<DisplayInfo> ApplyPrecedenceRules(List<DisplayInfo> displays, List<string> selectors)
    {
        if (displays == null || displays.Count == 0 || selectors == null || selectors.Count == 0)
            return displays ?? new List<DisplayInfo>();

        // Group selectors by type and precedence
        var selectorsByPrecedence = selectors
            .Select(s => new { Selector = s, Precedence = GetSelectorPrecedence(s) })
            .OrderBy(x => x.Precedence)
            .ThenBy(x => x.Selector)
            .ToList();

        var result = new List<DisplayInfo>();
        var addedDisplays = new HashSet<string>(); // Track by display ID to avoid duplicates

        // Add displays in precedence order
        foreach (var selectorInfo in selectorsByPrecedence)
        {
            foreach (var display in displays)
            {
                if (!addedDisplays.Contains(display.Id) && display.MatchesSelector(selectorInfo.Selector))
                {
                    result.Add(display);
                    addedDisplays.Add(display.Id);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the precedence value for a selector (lower values = higher precedence).
    /// </summary>
    /// <param name="selector">Selector string</param>
    /// <returns>Precedence value (1 = highest, 4 = lowest)</returns>
    private static int GetSelectorPrecedence(string selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
            return 999;

        selector = selector.Trim();

        // 1. path: selectors (highest precedence)
        if (selector.StartsWith("path:", StringComparison.OrdinalIgnoreCase))
            return 1;

        // 2. M# selectors
        if (selector.StartsWith("M", StringComparison.OrdinalIgnoreCase) && 
            selector.Length > 1 && 
            char.IsDigit(selector[1]))
            return 2;

        // 3. name: selectors (with sub-precedence for exact vs partial vs regex)
        if (selector.StartsWith("name:", StringComparison.OrdinalIgnoreCase))
        {
            var nameValue = selector[5..].Trim();
            
            // Exact match (quoted) has highest precedence within name selectors
            if (nameValue.StartsWith('"') && nameValue.EndsWith('"') && nameValue.Length > 1)
                return 3;
            
            // Regex patterns have lower precedence
            if (nameValue.StartsWith('/') && nameValue.EndsWith('/') && nameValue.Length > 2)
                return 5;
            
            // Partial match has middle precedence
            return 4;
        }

        // 4. conn: selectors (lowest precedence)
        if (selector.StartsWith("conn:", StringComparison.OrdinalIgnoreCase))
            return 6;

        // Unknown selector types get lowest precedence
        return 999;
    }

    /// <summary>
    /// Validates that a selector string is properly formatted.
    /// </summary>
    /// <param name="selector">Selector string to validate</param>
    /// <returns>Validation result with success flag and error message</returns>
    public static (bool IsValid, string ErrorMessage) ValidateSelector(string selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
            return (false, "Selector cannot be empty");

        selector = selector.Trim();

        // M# selector validation
        if (selector.StartsWith("M", StringComparison.OrdinalIgnoreCase))
        {
            if (selector.Length == 1)
                return (false, "M# selector requires a number (e.g., M1, M2)");
            
            if (!char.IsDigit(selector[1]))
                return (false, "M# selector requires a number after M (e.g., M1, M2)");
            
            if (!int.TryParse(selector[1..], out var number) || number < 1)
                return (false, "M# selector requires a positive number (e.g., M1, M2)");
            
            return (true, string.Empty);
        }

        // path: selector validation
        if (selector.StartsWith("path:", StringComparison.OrdinalIgnoreCase))
        {
            var pathValue = selector[5..].Trim();
            if (string.IsNullOrEmpty(pathValue))
                return (false, "path: selector requires a value (e.g., path:7a1c-2f9e)");
            
            return (true, string.Empty);
        }

        // conn: selector validation
        if (selector.StartsWith("conn:", StringComparison.OrdinalIgnoreCase))
        {
            var connValue = selector[5..].Trim();
            if (string.IsNullOrEmpty(connValue))
                return (false, "conn: selector requires a value (e.g., conn:HDMI, conn:DISPLAYPORT)");
            
            var validConnections = new[] { "HDMI", "DISPLAYPORT", "DVI", "VGA", "INTERNAL", "SVIDEO", "COMPOSITE", "COMPONENT", "MIRACAST", "INDIRECT", "VIRTUAL", "OTHER" };
            if (!validConnections.Contains(connValue.ToUpperInvariant()))
            {
                return (false, $"conn: selector '{connValue}' is not a recognized connection type. Valid types: {string.Join(", ", validConnections)}");
            }
            
            return (true, string.Empty);
        }

        // name: selector validation
        if (selector.StartsWith("name:", StringComparison.OrdinalIgnoreCase))
        {
            var nameValue = selector[5..].Trim();
            if (string.IsNullOrEmpty(nameValue))
                return (false, "name: selector requires a value (e.g., name:\"DELL U2720Q\", name:DELL, name:/DELL.*/)");
            
            // Validate regex patterns
            if (nameValue.StartsWith('/') && nameValue.EndsWith('/') && nameValue.Length > 2)
            {
                try
                {
                    var pattern = nameValue[1..^1];
                    _ = new System.Text.RegularExpressions.Regex(pattern);
                }
                catch (ArgumentException ex)
                {
                    return (false, $"Invalid regex pattern in name selector: {ex.Message}");
                }
            }
            
            return (true, string.Empty);
        }

        return (false, $"Unknown selector format: '{selector}'. Supported formats: M#, name:value, conn:type, path:hash");
    }

    /// <summary>
    /// Validates a list of selectors and returns any validation errors.
    /// </summary>
    /// <param name="selectors">List of selector strings to validate</param>
    /// <returns>List of validation error messages (empty if all valid)</returns>
    public static List<string> ValidateSelectors(List<string> selectors)
    {
        var errors = new List<string>();
        
        if (selectors == null || selectors.Count == 0)
            return errors;

        foreach (var selector in selectors)
        {
            var (isValid, errorMessage) = ValidateSelector(selector);
            if (!isValid)
            {
                // Include the offending selector to aid diagnostics and satisfy tests
                errors.Add(string.IsNullOrEmpty(errorMessage)
                    ? $"Invalid selector: '{selector}'"
                    : $"{errorMessage} ('{selector}')");
            }
        }

        return errors;
    }

    /// <summary>
    /// Suggests valid selectors for displays that don't match any provided selectors.
    /// </summary>
    /// <param name="allDisplays">All available displays</param>
    /// <param name="attemptedSelectors">Selectors that were attempted</param>
    /// <returns>Formatted string with suggested selectors</returns>
    public static string SuggestSelectors(List<DisplayInfo> allDisplays, List<string> attemptedSelectors)
    {
        if (allDisplays == null || allDisplays.Count == 0)
            return "No displays available.";

        var suggestions = new List<string>
        {
            "Available displays and suggested selectors:",
            ""
        };

        foreach (var display in allDisplays)
        {
            suggestions.Add($"  {display.Id}: {display.Name}");
            suggestions.Add($"    Use: {display.Id}");
            suggestions.Add($"    Or:  name:\"{display.Name}\"");
            suggestions.Add($"    Or:  conn:{display.ConnectionType}");
            suggestions.Add($"    Or:  path:{display.PathKey}");
            suggestions.Add("");
        }

        if (attemptedSelectors != null && attemptedSelectors.Count > 0)
        {
            suggestions.Add("Attempted selectors that didn't match:");
            foreach (var selector in attemptedSelectors)
            {
                suggestions.Add($"  {selector}");
            }
        }

        return string.Join(Environment.NewLine, suggestions);
    }
}
