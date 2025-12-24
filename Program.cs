using System;
using System.Windows.Forms;

namespace Cropalicious
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // High-DPI support
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            using var app = new CropaliciousApp();
            Application.Run();
        }
    }
}