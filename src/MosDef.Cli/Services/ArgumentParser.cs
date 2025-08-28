using MosDef.Cli.Models;
using MosDef.Core.Services;

namespace MosDef.Cli.Services;

/// <summary>
/// Parses command line arguments for the MOS-DEF CLI application.
/// Handles all supported flags, actions, and selector options according to the CLI contract.
/// </summary>
public static class ArgumentParser
{
    /// <summary>
    /// Parses command line arguments into a CliOptions object.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Parsed CliOptions with validation results</returns>
    public static CliOptions Parse(string[]? args)
    {
        var options = new CliOptions
        {
            RawArgs = args ?? Array.Empty<string>()
        };

        if (args == null || args.Length == 0)
        {
            options.ShowHelp = true;
            return options;
        }

        var argList = new List<string>(args);
        int currentIndex = 0;

        while (currentIndex < argList.Count)
        {
            var arg = argList[currentIndex];

            switch (arg.ToLowerInvariant())
            {
                case "landscape":
                case "portrait":
                case "toggle":
                    if (!string.IsNullOrEmpty(options.Action))
                    {
                        options.ValidationErrors.Add($"Multiple actions specified: '{options.Action}' and '{arg}'");
                    }
                    else
                    {
                        options.Action = arg.ToLowerInvariant();
                    }
                    break;

                case "--list":
                case "-l":
                    options.ShowList = true;
                    break;

                case "--verbose":
                case "-v":
                    options.Verbose = true;
                    break;

                case "--dry-run":
                case "-n":
                    options.DryRun = true;
                    break;

                case "--help":
                case "-h":
                case "/?":
                    options.ShowHelp = true;
                    break;

                case "--version":
                    options.ShowVersion = true;
                    break;

                case "--clear-default":
                    options.ClearDefault = true;
                    break;

                case "--only":
                    currentIndex = ParseSelectorArgument(argList, currentIndex, options.OnlySelectors, options, "--only");
                    break;

                case "--include":
                    currentIndex = ParseSelectorArgument(argList, currentIndex, options.IncludeSelectors, options, "--include");
                    break;

                case "--exclude":
                    currentIndex = ParseSelectorArgument(argList, currentIndex, options.ExcludeSelectors, options, "--exclude");
                    break;

                case "--save-default":
                    currentIndex = ParseSaveDefaultArgument(argList, currentIndex, options);
                    break;

                default:
                    // Handle combined flags like --only=M1 or unknown arguments
                    if (arg.StartsWith("--only="))
                    {
                        var selector = arg[7..];
                        if (!string.IsNullOrEmpty(selector))
                        {
                            var selectors = SelectorParser.ParseSelectorList(selector);
                            options.OnlySelectors.AddRange(selectors);
                        }
                        else
                        {
                            options.ValidationErrors.Add("--only flag requires a selector value");
                        }
                    }
                    else if (arg.StartsWith("--include="))
                    {
                        var selector = arg[10..];
                        if (!string.IsNullOrEmpty(selector))
                        {
                            var selectors = SelectorParser.ParseSelectorList(selector);
                            options.IncludeSelectors.AddRange(selectors);
                        }
                        else
                        {
                            options.ValidationErrors.Add("--include flag requires a selector value");
                        }
                    }
                    else if (arg.StartsWith("--exclude="))
                    {
                        var selector = arg[10..];
                        if (!string.IsNullOrEmpty(selector))
                        {
                            var selectors = SelectorParser.ParseSelectorList(selector);
                            options.ExcludeSelectors.AddRange(selectors);
                        }
                        else
                        {
                            options.ValidationErrors.Add("--exclude flag requires a selector value");
                        }
                    }
                    else if (arg.StartsWith("--save-default="))
                    {
                        var selector = arg[15..];
                        if (!string.IsNullOrEmpty(selector))
                        {
                            if (!string.IsNullOrEmpty(options.SaveDefaultSelector))
                            {
                                options.ValidationErrors.Add("Multiple --save-default values specified");
                            }
                            else
                            {
                                options.SaveDefaultSelector = selector;
                            }
                        }
                        else
                        {
                            options.ValidationErrors.Add("--save-default flag requires a selector value");
                        }
                    }
                    else if (arg.StartsWith("-"))
                    {
                        options.ValidationErrors.Add($"Unknown flag: '{arg}'");
                    }
                    else
                    {
                        options.ValidationErrors.Add($"Unknown argument: '{arg}'");
                    }
                    break;
            }

            currentIndex++;
        }

        // Validate selector syntax
        ValidateSelectors(options.OnlySelectors, "--only", options);
        ValidateSelectors(options.IncludeSelectors, "--include", options);
        ValidateSelectors(options.ExcludeSelectors, "--exclude", options);

        if (!string.IsNullOrEmpty(options.SaveDefaultSelector))
        {
            var (isValid, errorMessage) = SelectorParser.ValidateSelector(options.SaveDefaultSelector);
            if (!isValid)
            {
                options.ValidationErrors.Add($"Invalid --save-default selector: {errorMessage}");
            }
        }

        // Final validation
        options.Validate();

        return options;
    }

    /// <summary>
    /// Parses a selector argument that expects a value (--only selector, --include selector, etc.).
    /// </summary>
    /// <param name="args">All arguments</param>
    /// <param name="currentIndex">Current position in arguments</param>
    /// <param name="targetList">List to add parsed selectors to</param>
    /// <param name="options">Options object for error reporting</param>
    /// <param name="flagName">Name of the flag for error messages</param>
    /// <returns>New current index after processing</returns>
    private static int ParseSelectorArgument(List<string> args, int currentIndex, List<string> targetList, CliOptions options, string flagName)
    {
        if (currentIndex + 1 >= args.Count)
        {
            options.ValidationErrors.Add($"{flagName} flag requires a selector value");
            return currentIndex;
        }

        var selectorValue = args[currentIndex + 1];

        if (string.IsNullOrEmpty(selectorValue) || selectorValue.StartsWith("-"))
        {
            options.ValidationErrors.Add($"{flagName} flag requires a selector value");
            return currentIndex;
        }

        var selectors = SelectorParser.ParseSelectorList(selectorValue);
        targetList.AddRange(selectors);

        return currentIndex + 1; // Skip the selector value
    }

    /// <summary>
    /// Parses the --save-default argument.
    /// </summary>
    /// <param name="args">All arguments</param>
    /// <param name="currentIndex">Current position in arguments</param>
    /// <param name="options">Options object to update</param>
    /// <returns>New current index after processing</returns>
    private static int ParseSaveDefaultArgument(List<string> args, int currentIndex, CliOptions options)
    {
        if (currentIndex + 1 >= args.Count)
        {
            options.ValidationErrors.Add("--save-default flag requires a selector value");
            return currentIndex;
        }

        var selectorValue = args[currentIndex + 1];

        if (string.IsNullOrEmpty(selectorValue) || selectorValue.StartsWith("-"))
        {
            options.ValidationErrors.Add("--save-default flag requires a selector value");
            return currentIndex;
        }

        if (!string.IsNullOrEmpty(options.SaveDefaultSelector))
        {
            options.ValidationErrors.Add("Multiple --save-default values specified");
        }
        else
        {
            options.SaveDefaultSelector = selectorValue;
        }

        return currentIndex + 1; // Skip the selector value
    }

    /// <summary>
    /// Validates a list of selectors and adds any errors to the options.
    /// </summary>
    /// <param name="selectors">Selectors to validate</param>
    /// <param name="flagName">Flag name for error messages</param>
    /// <param name="options">Options object for error reporting</param>
    private static void ValidateSelectors(List<string> selectors, string flagName, CliOptions options)
    {
        var validationErrors = SelectorParser.ValidateSelectors(selectors);
        foreach (var error in validationErrors)
        {
            options.ValidationErrors.Add($"Invalid {flagName} selector: {error}");
        }
    }

    /// <summary>
    /// Gets the help text for the application.
    /// </summary>
    /// <returns>Formatted help text</returns>
    public static string GetHelpText()
    {
        return @"MOS-DEF (Monitor Orientation Switcher - Desktop Efficiency Fixer)
A Windows 11 utility for one-click display rotation control.

USAGE:
  mos-def <action> [options]
  mos-def --list
  mos-def --clear-default

ACTIONS:
  landscape          Set displays to landscape orientation (0°)
  portrait           Set displays to portrait orientation (90°)
  toggle             Toggle between landscape and portrait

OPTIONS:
  --list, -l         List all active displays with their identifiers
  --verbose, -v      Show detailed output during operations
  --dry-run, -n      Show what would happen without applying changes
  --help, -h         Show this help message
  --version          Show version information

MONITOR SELECTION:
  --only <selector>      Operate only on specified monitors (exclusive)
  --include <selector>   Include specified monitors (additive)
  --exclude <selector>   Exclude specified monitors (subtractive)

  Multiple selectors can be comma-separated: --only M1,M3

DEFAULT MONITOR:
  --save-default <selector>   Save a monitor as the default target
  --clear-default             Clear the saved default monitor

SELECTOR FORMATS:
  M#                    MOS-DEF index (M1, M2, M3) - left to right ordering
  name:""DELL U2720Q""    Exact name match (use quotes for spaces)
  name:DELL             Partial name match
  name:/DELL.*/         Regex pattern match
  conn:HDMI             Connection type (HDMI, DISPLAYPORT, DVI, etc.)
  path:7a1c-2f9e        Device path hash (from --list output)

EXAMPLES:
  mos-def portrait                    Set all displays to portrait
  mos-def landscape --only M2         Set only second monitor to landscape
  mos-def toggle --exclude M1         Toggle all except first monitor
  mos-def portrait --save-default M2  Set M2 to portrait and save as default
  mos-def --list                      Show all available displays
  mos-def toggle --dry-run            Show what toggle would do

For more information, visit: https://github.com/your-org/mos-def";
    }

    /// <summary>
    /// Gets the version information for the application.
    /// </summary>
    /// <returns>Version string</returns>
    public static string GetVersionText()
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        return $"MOS-DEF v{version?.ToString(3) ?? "0.1.0"}";
    }
}
