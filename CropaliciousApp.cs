using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
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
                Icon = CreateAppIcon(),
                ContextMenuStrip = trayMenu,
                Visible = true
            };
            trayIcon.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) ShowMainWindow(); };
            trayIcon.BalloonTipClicked += OnBalloonTipClicked;
        }

        private void OnBalloonTipClicked(object? sender, EventArgs e)
        {
            try
            {
                if (System.IO.Directory.Exists(settings.OutputFolder))
                {
                    System.Diagnostics.Process.Start("explorer.exe", settings.OutputFolder);
                }
            }
            catch { }
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
                if (settings.HotkeyMode == HotkeyMode.FixedSize)
                {
                    settings.CaptureWidth = settings.FixedCaptureWidth;
                    settings.CaptureHeight = settings.FixedCaptureHeight;
                }
                overlayWindow = new OverlayWindow(settings);
                overlayWindow.ScreenshotTaken += OnScreenshotTaken;
                overlayWindow.Show();
            }
        }

        private void OnScreenshotTaken(object? sender, ScreenshotEventArgs e)
        {
            try
            {
                ScreenCapture.SaveScreenshot(e.CaptureArea, settings.OutputFolder);
                if (settings.ShowNotifications)
                {
                    trayIcon?.ShowBalloonTip(2000, "Cropalicious", "Screenshot saved!", ToolTipIcon.Info);
                }
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
                mainWindow.SettingsChanged += OnSettingsChanged;
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
                window.SettingsChanged -= OnSettingsChanged;
            }
        }

        private void OnSettingsChanged(object? sender, EventArgs e)
        {
            hotkey?.Dispose();
            InitializeHotkey();
        }

        private void OnSettings(object? sender, EventArgs e)
        {
            ShowMainWindow();
            mainWindow?.OpenSettings();
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

        public static Icon CreateAppIcon(int size = 16)
        {
            var bitmap = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Black);
                float scale = size / 16f;
                float lineWidth = Math.Max(1, 2 * scale);
                float cornerLen = 5 * scale;
                using var greenPen = new Pen(Color.Lime, lineWidth);
                float m = lineWidth / 2;
                float e = size - m - 1;
                g.DrawLine(greenPen, m, m, m + cornerLen, m);
                g.DrawLine(greenPen, m, m, m, m + cornerLen);
                g.DrawLine(greenPen, e, m, e - cornerLen, m);
                g.DrawLine(greenPen, e, m, e, m + cornerLen);
                g.DrawLine(greenPen, m, e, m + cornerLen, e);
                g.DrawLine(greenPen, m, e, m, e - cornerLen);
                g.DrawLine(greenPen, e, e, e - cornerLen, e);
                g.DrawLine(greenPen, e, e, e, e - cornerLen);
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