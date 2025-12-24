using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Cropalicious
{
    public class CropaliciousApp : IDisposable
    {
        private NotifyIcon? trayIcon;
        private ContextMenuStrip? trayMenu;
        private GlobalHotkey? hotkey;
        private OverlayWindow? overlayWindow;
        private MainWindow? mainWindow;
        private AppSettings settings;

        public CropaliciousApp()
        {
            settings = AppSettings.Load();
            InitializeTrayIcon();
            InitializeHotkey();
            InitializeDisplayChangeHandling();
            ShowMainWindow();
        }

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Open", null, OnOpen);
            trayMenu.Items.Add("Settings", null, OnSettings);
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("Exit", null, OnExit);

            trayIcon = new NotifyIcon()
            {
                Text = "Cropalicious",
                Icon = CreateDefaultIcon(),
                ContextMenuStrip = trayMenu,
                Visible = true
            };
        }

        private void InitializeHotkey()
        {
            try
            {
                hotkey = new GlobalHotkey(settings.HotkeyModifiers, settings.HotkeyKey);
                hotkey.HotkeyPressed += OnHotkeyPressed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to register hotkey: {ex.Message}", "Cropalicious", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnHotkeyPressed(object? sender, EventArgs e)
        {
            if (overlayWindow == null || overlayWindow.IsDisposed)
            {
                overlayWindow = new OverlayWindow(settings);
                overlayWindow.ScreenshotTaken += OnScreenshotTaken;
                overlayWindow.Show();
            }
        }

        private void OnScreenshotTaken(object? sender, ScreenshotEventArgs e)
        {
            try
            {
                // Capture immediately for maximum responsiveness
                ScreenCapture.SaveScreenshot(e.CaptureArea, settings.OutputFolder);
                trayIcon?.ShowBalloonTip(2000, "Cropalicious", "Screenshot saved!", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save screenshot: {ex.Message}", "Cropalicious",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnOpen(object? sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void ShowMainWindow()
        {
            if (mainWindow == null || mainWindow.IsDisposed)
            {
                mainWindow = new MainWindow(settings);
                mainWindow.ScreenshotTaken += OnScreenshotTaken;
                mainWindow.FormClosed += OnMainWindowClosed;
            }

            mainWindow.Show();
            mainWindow.WindowState = FormWindowState.Normal;
            mainWindow.BringToFront();
        }

        private void OnMainWindowClosed(object? sender, FormClosedEventArgs e)
        {
            if (sender is MainWindow window)
            {
                window.ScreenshotTaken -= OnScreenshotTaken;
                window.FormClosed -= OnMainWindowClosed;
            }
        }

        private void OnSettings(object? sender, EventArgs e)
        {
            using var settingsForm = new SettingsForm(settings);
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                settings = settingsForm.Settings;
                settings.Save();
                
                hotkey?.Dispose();
                InitializeHotkey();
            }
        }

        private void InitializeDisplayChangeHandling()
        {
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            if (overlayWindow != null && !overlayWindow.IsDisposed)
            {
                overlayWindow.UpdateScreenBounds();
            }
        }

        private void OnExit(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        private Icon CreateDefaultIcon()
        {
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Blue);
                g.DrawRectangle(Pens.White, 2, 2, 11, 11);
            }
            return Icon.FromHandle(bitmap.GetHicon());
        }

        public void Dispose()
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            hotkey?.Dispose();
            overlayWindow?.Dispose();
            mainWindow?.Dispose();
            trayIcon?.Dispose();
            trayMenu?.Dispose();
        }
    }
}