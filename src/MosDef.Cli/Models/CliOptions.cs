namespace MosDef.Cli.Models;

/// <summary>
/// Represents parsed command line options for the MOS-DEF CLI.
/// </summary>
public class CliOptions
{
    /// <summary>
    /// The primary action to perform (landscape, portrait, toggle, or null for special commands).
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// Whether to show the list of displays (--list flag).
    /// </summary>
    public bool ShowList { get; set; }

    /// <summary>
    /// Whether to show verbose output (--verbose flag).
    /// </summary>
    public bool Verbose { get; set; }

    /// <summary>
    /// Whether to perform a dry run without applying changes (--dry-run flag).
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Selectors for --only flag (exclusive selection).
    /// </summary>
    public List<string> OnlySelectors { get; set; } = new List<string>();

    /// <summary>
    /// Selectors for --include flag (additive selection).
    /// </summary>
    public List<string> IncludeSelectors { get; set; } = new List<string>();

    /// <summary>
    /// Selectors for --exclude flag (subtractive selection).
    /// </summary>
    public List<string> ExcludeSelectors { get; set; } = new List<string>();

    /// <summary>
    /// Selector to save as default (--save-default flag).
    /// </summary>
    public string? SaveDefaultSelector { get; set; }

    /// <summary>
    /// Whether to clear the saved default selector (--clear-default flag).
    /// </summary>
    public bool ClearDefault { get; set; }

    /// <summary>
    /// Whether to show help information.
    /// </summary>
    public bool ShowHelp { get; set; }

    /// <summary>
    /// Whether to show version information.
    /// </summary>
    public bool ShowVersion { get; set; }

    /// <summary>
    /// Raw arguments for error reporting.
    /// </summary>
    public string[] RawArgs { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Validation errors found during parsing.
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new List<string>();

    /// <summary>
    /// Checks if the options represent a valid state.
    /// </summary>
    public bool IsValid => ValidationErrors.Count == 0;

    /// <summary>
    /// Checks if this is a special command that doesn't require display operations.
    /// </summary>
    public bool IsSpecialCommand => ShowList || ShowHelp || ShowVersion || ClearDefault;

    /// <summary>
    /// Checks if any selector flags were specified.
    /// </summary>
    public bool HasSelectorFlags => OnlySelectors.Count > 0 || IncludeSelectors.Count > 0 || ExcludeSelectors.Count > 0;

    /// <summary>
    /// Gets a formatted string of all validation errors.
    /// </summary>
    public string GetValidationErrorsText()
    {
        if (ValidationErrors.Count == 0)
            return string.Empty;

        return string.Join(Environment.NewLine, ValidationErrors);
    }

    /// <summary>
    /// Validates the options and populates ValidationErrors.
    /// </summary>
    public void Validate()
    {
        // Validate action
        if (!IsSpecialCommand && string.IsNullOrEmpty(Action))
        {
            ValidationErrors.Add("No action specified. Use 'landscape', 'portrait', or 'toggle'.");
        }

        if (!string.IsNullOrEmpty(Action))
        {
            var validActions = new[] { "landscape", "portrait", "toggle" };
            if (!validActions.Contains(Action.ToLowerInvariant()))
            {
                ValidationErrors.Add($"Invalid action: '{Action}'. Valid actions are: {string.Join(", ", validActions)}");
            }
        }

        // Validate selector combinations
        if (OnlySelectors.Count > 0 && IncludeSelectors.Count > 0)
        {
            ValidationErrors.Add("Cannot use both --only and --include flags. Use --only for exclusive selection or --include for additive selection.");
        }

        if (OnlySelectors.Count > 0 && ExcludeSelectors.Count > 0)
        {
            ValidationErrors.Add("Cannot use --exclude with --only. Use --only for exclusive selection, or --include with --exclude for filtered selection.");
        }

        // Validate save-default usage
        if (!string.IsNullOrEmpty(SaveDefaultSelector) && IsSpecialCommand)
        {
            ValidationErrors.Add("Cannot use --save-default with special commands like --list or --clear-default.");
        }

        // Validate clear-default usage
        if (ClearDefault && !string.IsNullOrEmpty(Action))
        {
            ValidationErrors.Add("Cannot use --clear-default with action commands. Use --clear-default by itself.");
        }

        if (ClearDefault && !string.IsNullOrEmpty(SaveDefaultSelector))
        {
            ValidationErrors.Add("Cannot use both --clear-default and --save-default in the same command.");
        }

        // Validate dry-run usage
        if (DryRun && IsSpecialCommand)
        {
            ValidationErrors.Add("--dry-run can only be used with action commands (landscape, portrait, toggle).");
        }
    }

    /// <summary>
    /// Creates a copy of the options for testing or manipulation.
    /// </summary>
    public CliOptions Clone()
    {
        return new CliOptions
        {
            Action = Action,
            ShowList = ShowList,
            Verbose = Verbose,
            DryRun = DryRun,
            OnlySelectors = new List<string>(OnlySelectors),
            IncludeSelectors = new List<string>(IncludeSelectors),
            ExcludeSelectors = new List<string>(ExcludeSelectors),
            SaveDefaultSelector = SaveDefaultSelector,
            ClearDefault = ClearDefault,
            ShowHelp = ShowHelp,
            ShowVersion = ShowVersion,
            RawArgs = (string[])RawArgs.Clone(),
            ValidationErrors = new List<string>(ValidationErrors)
        };
    }
}
