using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Cropalicious
{
    public class GlobalHotkey : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;
        private readonly HiddenWindow window;
        private bool disposed = false;

        public event EventHandler? HotkeyPressed;

        public GlobalHotkey(Keys modifiers, Keys key)
        {
            window = new HiddenWindow();
            window.HotkeyPressed += (s, e) => HotkeyPressed?.Invoke(this, EventArgs.Empty);

            uint mod = 0;
            if ((modifiers & Keys.Control) != 0) mod |= 0x0002;
            if ((modifiers & Keys.Alt) != 0) mod |= 0x0001;
            if ((modifiers & Keys.Shift) != 0) mod |= 0x0004;
            if ((modifiers & Keys.LWin) != 0) mod |= 0x0008;

            if (!RegisterHotKey(window.Handle, HOTKEY_ID, mod, (uint)key))
            {
                throw new InvalidOperationException("Failed to register hotkey");
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                UnregisterHotKey(window.Handle, HOTKEY_ID);
                window.Dispose();
                disposed = true;
            }
        }

        private class HiddenWindow : Form
        {
            public event EventHandler? HotkeyPressed;

            public HiddenWindow()
            {
                WindowState = FormWindowState.Minimized;
                ShowInTaskbar = false;
                Visible = false;
            }

            protected override void WndProc(ref Message m)
            {
                const int WM_HOTKEY = 0x0312;
                if (m.Msg == WM_HOTKEY)
                {
                    HotkeyPressed?.Invoke(this, EventArgs.Empty);
                }
                base.WndProc(ref m);
            }
        }
    }
}