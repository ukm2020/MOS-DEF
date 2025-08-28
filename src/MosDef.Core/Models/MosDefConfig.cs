using System.Text.Json;
using System.Text.Json.Serialization;

namespace MosDef.Core.Models;

/// <summary>
/// Configuration model for MOS-DEF application settings.
/// Stores user preferences like default selector and command history.
/// </summary>
public class MosDefConfig
{
    /// <summary>
    /// Default selector to use when no --only or --include flags are specified.
    /// Can be any valid selector format (M#, name:, conn:, path:).
    /// </summary>
    [JsonPropertyName("default_selector")]
    public string? DefaultSelector { get; set; }

    /// <summary>
    /// Last action performed (landscape, portrait, toggle) for analytics and debugging.
    /// </summary>
    [JsonPropertyName("last_action")]
    public string? LastAction { get; set; }

    /// <summary>
    /// History of selectors used, for potential future autocomplete or suggestions.
    /// Most recent selectors appear first in the list.
    /// </summary>
    [JsonPropertyName("selector_history")]
    public List<string> SelectorHistory { get; set; } = new List<string>();

    /// <summary>
    /// Maximum number of items to keep in selector history.
    /// </summary>
    [JsonIgnore]
    public const int MaxHistoryItems = 20;

    /// <summary>
    /// Creates a new MosDefConfig with default values.
    /// </summary>
    public MosDefConfig()
    {
    }

    /// <summary>
    /// Adds a selector to the history, maintaining the maximum history size.
    /// Moves existing entries to the front if they already exist.
    /// </summary>
    /// <param name="selector">Selector to add to history</param>
    public void AddToHistory(string selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
            return;

        selector = selector.Trim();

        // Remove existing instance if present
        SelectorHistory.Remove(selector);

        // Add to front
        SelectorHistory.Insert(0, selector);

        // Trim to max size
        if (SelectorHistory.Count > MaxHistoryItems)
        {
            SelectorHistory.RemoveRange(MaxHistoryItems, SelectorHistory.Count - MaxHistoryItems);
        }
    }

    /// <summary>
    /// Clears the default selector.
    /// </summary>
    public void ClearDefaultSelector()
    {
        DefaultSelector = null;
    }

    /// <summary>
    /// Sets the default selector and adds it to history.
    /// </summary>
    /// <param name="selector">Selector to set as default</param>
    public void SetDefaultSelector(string selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            ClearDefaultSelector();
            return;
        }

        DefaultSelector = selector.Trim();
        AddToHistory(DefaultSelector);
    }

    /// <summary>
    /// Updates the last action performed.
    /// </summary>
    /// <param name="action">Action name (landscape, portrait, toggle)</param>
    public void SetLastAction(string action)
    {
        LastAction = string.IsNullOrWhiteSpace(action) ? null : action.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Creates a deep copy of the configuration.
    /// </summary>
    /// <returns>New MosDefConfig instance with copied values</returns>
    public MosDefConfig Clone()
    {
        return new MosDefConfig
        {
            DefaultSelector = DefaultSelector,
            LastAction = LastAction,
            SelectorHistory = new List<string>(SelectorHistory)
        };
    }

    /// <summary>
    /// Validates the configuration and fixes any issues.
    /// </summary>
    /// <returns>True if any changes were made during validation</returns>
    public bool Validate()
    {
        bool changed = false;

        // Ensure history doesn't exceed max size
        if (SelectorHistory.Count > MaxHistoryItems)
        {
            SelectorHistory.RemoveRange(MaxHistoryItems, SelectorHistory.Count - MaxHistoryItems);
            changed = true;
        }

        // Remove duplicate entries in history (keep first occurrence)
        var seen = new HashSet<string>();
        var cleaned = new List<string>();
        foreach (var item in SelectorHistory)
        {
            if (!string.IsNullOrWhiteSpace(item) && seen.Add(item.Trim()))
            {
                cleaned.Add(item.Trim());
            }
        }

        if (cleaned.Count != SelectorHistory.Count)
        {
            SelectorHistory = cleaned;
            changed = true;
        }

        // Validate last action
        if (!string.IsNullOrEmpty(LastAction))
        {
            var validActions = new[] { "landscape", "portrait", "toggle" };
            if (!validActions.Contains(LastAction.ToLowerInvariant()))
            {
                LastAction = null;
                changed = true;
            }
        }

        return changed;
    }

    /// <summary>
    /// JSON serialization options for consistent formatting.
    /// </summary>
    [JsonIgnore]
    public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes the configuration to JSON string.
    /// </summary>
    /// <returns>JSON string representation</returns>
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "MosDefConfig properties are simple types that are preserved")]
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonOptions);
    }

    /// <summary>
    /// Deserializes configuration from JSON string.
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <returns>MosDefConfig instance, or new instance if deserialization fails</returns>
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "MosDefConfig properties are simple types that are preserved")]
    public static MosDefConfig FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new MosDefConfig();

        try
        {
            var config = JsonSerializer.Deserialize<MosDefConfig>(json, JsonOptions);
            if (config == null)
                return new MosDefConfig();

            config.Validate();
            return config;
        }
        catch (JsonException)
        {
            // Return default config if JSON is invalid
            return new MosDefConfig();
        }
    }
}
