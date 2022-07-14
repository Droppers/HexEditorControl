using System.Runtime.InteropServices;

namespace HexControl.Samples.WinForms;

internal static class Program
{
    /// <summary>
    ///     The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }
}