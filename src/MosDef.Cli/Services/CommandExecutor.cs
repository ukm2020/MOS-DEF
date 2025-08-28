using MosDef.Cli.Models;
using MosDef.Core.Models;
using MosDef.Core.Services;

namespace MosDef.Cli.Services;

/// <summary>
/// Executes MOS-DEF commands based on parsed CLI options.
/// Handles all command logic including display operations, configuration management, and output formatting.
/// </summary>
public class CommandExecutor
{
    private readonly ConfigManager _configManager;

    /// <summary>
    /// Creates a new CommandExecutor with the specified configuration manager.
    /// </summary>
    /// <param name="configManager">Configuration manager for persistent settings</param>
    public CommandExecutor(ConfigManager configManager)
    {
        _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
    }

    /// <summary>
    /// Creates a new CommandExecutor with a default configuration manager.
    /// </summary>
    public CommandExecutor() : this(new ConfigManager())
    {
    }

    /// <summary>
    /// Executes a command based on the provided CLI options.
    /// </summary>
    /// <param name="options">Parsed command line options</param>
    /// <returns>Exit code (0 = success, 2 = invalid args, 3 = Windows API failure)</returns>
    public int Execute(CliOptions options)
    {
        if (options == null)
        {
            Console.Error.WriteLine("Error: No options provided");
            return 2;
        }

        // Validate options
        if (!options.IsValid)
        {
            Console.Error.WriteLine("Error: " + options.GetValidationErrorsText());
            return 2;
        }

        try
        {
            // Handle special commands first
            if (options.ShowHelp)
            {
                Console.WriteLine(ArgumentParser.GetHelpText());
                return 0;
            }

            if (options.ShowVersion)
            {
                Console.WriteLine(ArgumentParser.GetVersionText());
                return 0;
            }

            if (options.ShowList)
            {
                return ExecuteListCommand(options);
            }

            if (options.ClearDefault)
            {
                return ExecuteClearDefaultCommand(options);
            }

            // Handle action commands
            if (!string.IsNullOrEmpty(options.Action))
            {
                return ExecuteActionCommand(options);
            }

            // If we get here, no valid command was found
            Console.Error.WriteLine("Error: No valid command specified. Use --help for usage information.");
            return 2;
        }
        catch (InvalidOperationException ex)
        {
            if (options.Verbose)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine($"Details: {ex.InnerException.Message}");
                }
            }
            else
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
            return 3;
        }
        catch (Exception ex)
        {
            if (options.Verbose)
            {
                Console.Error.WriteLine($"Unexpected error: {ex}");
            }
            else
            {
                Console.Error.WriteLine($"Unexpected error: {ex.Message}");
            }
            return 3;
        }
    }

    /// <summary>
    /// Executes the --list command to show available displays.
    /// </summary>
    /// <param name="options">CLI options</param>
    /// <returns>Exit code</returns>
    private int ExecuteListCommand(CliOptions options)
    {
        try
        {
            var displays = DisplayManager.GetActiveDisplays();
            
            if (displays.Count == 0)
            {
                Console.WriteLine("No active displays found.");
                return 0;
            }

            Console.WriteLine(DisplayManager.FormatDisplayList(displays));
            
            if (options.Verbose)
            {
                Console.WriteLine();
                Console.WriteLine($"Found {displays.Count} active display(s)");
                
                var defaultSelector = _configManager.GetDefaultSelector();
                if (!string.IsNullOrEmpty(defaultSelector))
                {
                    Console.WriteLine($"Default selector: {defaultSelector}");
                }
            }

            return 0;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Error: Failed to enumerate displays: {ex.Message}");
            return 3;
        }
    }

    /// <summary>
    /// Executes the --clear-default command.
    /// </summary>
    /// <param name="options">CLI options</param>
    /// <returns>Exit code</returns>
    private int ExecuteClearDefaultCommand(CliOptions options)
    {
        try
        {
            var currentDefault = _configManager.GetDefaultSelector();
            
            if (string.IsNullOrEmpty(currentDefault))
            {
                if (options.Verbose)
                {
                    Console.WriteLine("No default selector is currently set.");
                }
                return 0;
            }

            _configManager.ClearDefaultSelector();
            
            if (options.Verbose)
            {
                Console.WriteLine($"Cleared default selector: {currentDefault}");
            }
            else
            {
                Console.WriteLine("Default selector cleared.");
            }

            return 0;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Error: Failed to clear default selector: {ex.Message}");
            return 3;
        }
    }

    /// <summary>
    /// Executes action commands (landscape, portrait, toggle).
    /// </summary>
    /// <param name="options">CLI options</param>
    /// <returns>Exit code</returns>
    private int ExecuteActionCommand(CliOptions options)
    {
        try
        {
            // Get all displays
            var allDisplays = DisplayManager.GetActiveDisplays();
            
            if (allDisplays.Count == 0)
            {
                Console.WriteLine("No active displays found.");
                return 0;
            }

            // Save default selector if requested
            if (!string.IsNullOrEmpty(options.SaveDefaultSelector))
            {
                _configManager.SetDefaultSelector(options.SaveDefaultSelector);
                if (options.Verbose)
                {
                    Console.WriteLine($"Saved default selector: {options.SaveDefaultSelector}");
                }
            }

            // Get current default selector
            var defaultSelector = _configManager.GetDefaultSelector();

            // Apply selector filtering
            var targetDisplays = SelectorParser.ApplySelectors(
                allDisplays,
                options.OnlySelectors.Count > 0 ? options.OnlySelectors : null,
                options.IncludeSelectors.Count > 0 ? options.IncludeSelectors : null,
                options.ExcludeSelectors.Count > 0 ? options.ExcludeSelectors : null,
                defaultSelector);

            // Check if any displays were selected
            if (targetDisplays.Count == 0)
            {
                var allSelectors = new List<string>();
                allSelectors.AddRange(options.OnlySelectors);
                allSelectors.AddRange(options.IncludeSelectors);
                allSelectors.AddRange(options.ExcludeSelectors);
                
                if (!string.IsNullOrEmpty(defaultSelector) && !options.HasSelectorFlags)
                {
                    allSelectors.Add(defaultSelector);
                }

                Console.Error.WriteLine("Error: No displays match the specified selectors.");
                Console.Error.WriteLine();
                Console.Error.WriteLine(SelectorParser.SuggestSelectors(allDisplays, allSelectors));
                return 2;
            }

            // Handle --only with multiple matches (require exact disambiguation)
            if (options.OnlySelectors.Count > 0 && targetDisplays.Count > 1)
            {
                var exactMatches = new List<DisplayInfo>();
                foreach (var selector in options.OnlySelectors)
                {
                    foreach (var display in targetDisplays)
                    {
                        if (display.MatchesSelector(selector))
                        {
                            // Check for exact matches with high precedence selectors
                            if (selector.StartsWith("path:", StringComparison.OrdinalIgnoreCase) ||
                                (selector.StartsWith("M", StringComparison.OrdinalIgnoreCase) && char.IsDigit(selector[1])) ||
                                (selector.StartsWith("name:\"", StringComparison.OrdinalIgnoreCase) && selector.EndsWith("\"")))
                            {
                                exactMatches.Add(display);
                                break;
                            }
                        }
                    }
                }

                if (exactMatches.Count == 0 && targetDisplays.Count > 1)
                {
                    Console.Error.WriteLine($"Error: --only selector matches {targetDisplays.Count} displays. Use more specific selectors:");
                    foreach (var display in targetDisplays)
                    {
                        Console.Error.WriteLine($"  {display.Id}: {display.Name} (use path:{display.PathKey} for exact match)");
                    }
                    return 2;
                }
            }

            // Add selectors to history
            foreach (var selector in options.OnlySelectors.Concat(options.IncludeSelectors))
            {
                _configManager.AddSelectorToHistory(selector);
            }

            // Execute the action
            int changedCount = 0;
            
            switch (options.Action!.ToLowerInvariant())
            {
                case "landscape":
                    changedCount = DisplayManager.ApplyRotationToDisplays(targetDisplays, 0, options.DryRun, options.Verbose);
                    break;
                    
                case "portrait":
                    changedCount = DisplayManager.ApplyRotationToDisplays(targetDisplays, 90, options.DryRun, options.Verbose);
                    break;
                    
                case "toggle":
                    changedCount = DisplayManager.ToggleDisplayRotation(targetDisplays, options.DryRun, options.Verbose);
                    break;
                    
                default:
                    Console.Error.WriteLine($"Error: Unknown action '{options.Action}'");
                    return 2;
            }

            // Update last action if not dry run
            if (!options.DryRun)
            {
                _configManager.SetLastAction(options.Action);
            }

            // Report results
            if (options.DryRun)
            {
                if (changedCount == 0)
                {
                    Console.WriteLine("[DRY RUN] No changes would be made.");
                }
                else
                {
                    Console.WriteLine($"[DRY RUN] Would change {changedCount} display(s).");
                }
            }
            else
            {
                if (changedCount == 0)
                {
                    if (options.Verbose)
                    {
                        Console.WriteLine("No changes were necessary.");
                    }
                }
                else if (!options.Verbose)
                {
                    // Provide minimal output for successful operations when not verbose
                    Console.WriteLine($"Successfully {options.Action}d {changedCount} display(s).");
                }
            }

            return 0;
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 2;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 3;
        }
    }
}
