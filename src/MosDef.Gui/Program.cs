namespace MosDef.Gui;

/// <summary>
/// Main entry point for the MOS-DEF GUI application.
/// This is a placeholder for future development in v0.3.0.
/// </summary>
internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        
        // Placeholder for GUI implementation
        MessageBox.Show(
            "MOS-DEF GUI is planned for v0.3.0.\n\nFor now, please use the command line version 'mos-def.exe'.",
            "MOS-DEF GUI - Coming Soon",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}
