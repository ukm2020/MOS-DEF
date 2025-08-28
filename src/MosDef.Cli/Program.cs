using MosDef.Cli.Services;

namespace MosDef.Cli;

/// <summary>
/// Main entry point for the MOS-DEF command line utility.
/// Handles argument parsing, command execution, and exit code management.
/// </summary>
internal class Program
{
    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Exit code: 0 = success, 2 = invalid arguments, 3 = Windows API failure</returns>
    public static int Main(string[] args)
    {
        try
        {
            // Parse command line arguments
            var options = ArgumentParser.Parse(args);

            // Execute the command
            var executor = new CommandExecutor();
            return executor.Execute(options);
        }
        catch (Exception ex)
        {
            // Handle any unexpected exceptions at the top level
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            
#if DEBUG
            // In debug builds, show the full stack trace
            Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
#endif
            
            return 3;
        }
    }
}
