using System;
using System.Threading;
using System.Windows.Forms;

namespace Cropalicious
{
    internal static class Program
    {
        private static Mutex? mutex;

        [STAThread]
        static void Main()
        {
            mutex = new Mutex(true, "Cropalicious_SingleInstance", out bool createdNew);
            if (!createdNew)
            {
                MessageBox.Show("Cropalicious is already running.", "Cropalicious",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using var app = new CropaliciousApp();
            Application.Run();
        }
    }
}