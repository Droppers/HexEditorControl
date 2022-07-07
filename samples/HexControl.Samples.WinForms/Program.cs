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
        // ***this line is added***
        if (Environment.OSVersion.Version.Major >= 6)
        {
            SetProcessDPIAware();
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Form1());
    }

    // ***also dllimport of that function***
    [DllImport("user32.dll")]
    private static extern bool SetProcessDPIAware();
}