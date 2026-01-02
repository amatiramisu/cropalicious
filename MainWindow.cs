using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Cropalicious
{
    public partial class MainWindow : Form
    {
        private readonly AppSettings settings;
        private OverlayWindow? overlayWindow;
        private TableLayoutPanel? presetsTable;
        private TableLayoutPanel? customTable;
        private Panel? customScrollPanel;
        private Label? outputLabel;
        private Label? hotkeyLabel;
        private ToolTip? toolTip;
        
        public event EventHandler<ScreenshotEventArgs>? ScreenshotTaken;

        private readonly (int width, int height, string name)[] presets = new[]
        {
            (1024, 1024, "1024×1024\nSquare"),
            (1216, 832, "1216×832\nWide"),
            (832, 1216, "832×1216\nTall"),
            (1344, 768, "1344×768\nUltrawide"),
            (768, 1344, "768×1344\nUltra Tall")
        };

        public MainWindow(AppSettings settings)
        {
            this.settings = settings;
            InitializeComponent();

            this.FormClosing += OnFormClosing;
            this.SizeChanged += OnSizeChanged;
            this.LocationChanged += OnLocationChanged;

            TopMost = settings.StayOnTop;
        }

        private void InitializeComponent()
        {
            Text = "Cropalicious";
            Size = new Size(settings.WindowWidth, settings.WindowHeight);
            Icon = CropaliciousApp.CreateAppIcon(32);
            
            if (settings.WindowX >= 0 && settings.WindowY >= 0)
            {
                StartPosition = FormStartPosition.Manual;
                Location = new Point(settings.WindowX, settings.WindowY);
            }
            else
            {
                StartPosition = FormStartPosition.CenterScreen;
            }
            
            FormBorderStyle = FormBorderStyle.Sizable;
            Padding = new Padding(20);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                AutoSize = true
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var hintLabel = new Label
            {
                Text = "Left click to capture, right click or ESC to cancel",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = SystemColors.GrayText,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, 0, 0, 15),
                TextAlign = ContentAlignment.MiddleCenter
            };

            presetsTable = new TableLayoutPanel
            {
                ColumnCount = 3,
                RowCount = 2,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 20)
            };

            for (int i = 0; i < 3; i++)
                presetsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            for (int i = 0; i < 2; i++)
                presetsTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            CreatePresetButtons();

            customTable = new TableLayoutPanel
            {
                ColumnCount = 3,
                AutoSize = true,
                Margin = new Padding(0)
            };

            for (int i = 0; i < 3; i++)
                customTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            customScrollPanel = new Panel
            {
                AutoScroll = true,
                MaximumSize = new Size(0, 260),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(0, 0, 20, 0)
            };
            customScrollPanel.HorizontalScroll.Enabled = false;
            customScrollPanel.HorizontalScroll.Visible = false;
            customScrollPanel.Controls.Add(customTable);

            CreateCustomButtons();

            var bottomPanel = CreateBottomPanel();

            mainLayout.Controls.Add(hintLabel, 0, 0);
            mainLayout.Controls.Add(presetsTable, 0, 1);
            mainLayout.Controls.Add(customScrollPanel, 0, 2);
            mainLayout.Controls.Add(bottomPanel, 0, 3);

            Controls.Add(mainLayout);
            ApplyTheme();

            mainLayout.PerformLayout();
            var preferredSize = mainLayout.PreferredSize;
            MinimumSize = new Size(
                preferredSize.Width + Padding.Horizontal + SystemInformation.FrameBorderSize.Width * 2 + 20,
                preferredSize.Height + Padding.Vertical + SystemInformation.CaptionHeight + SystemInformation.FrameBorderSize.Height * 2 + 20
            );
        }

        private void CreatePresetButtons()
        {
            bool isDark = settings.Theme == AppTheme.Dark;

            for (int i = 0; i < presets.Length; i++)
            {
                var preset = presets[i];
                int col = i % 3;
                int row = i / 3;

                var button = new GlowButton
                {
                    Text = preset.name,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    BackColor = Color.FromArgb(50, 180, 200),
                    ForeColor = Color.White,
                    Size = new Size(180, 80),
                    Margin = new Padding(0),
                    Tag = (preset.width, preset.height)
                };
                button.SetGlow(isDark);

                button.Click += OnPresetButtonClick;
                presetsTable!.Controls.Add(button, col, row);
            }

            var addButton = new GlowButton
            {
                Text = "+\nAdd Custom",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(160, 200, 50),
                ForeColor = Color.White,
                Size = new Size(180, 80),
                Margin = new Padding(0)
            };
            addButton.SetGlow(isDark);
            addButton.Click += OnAddCustomClick;
            presetsTable!.Controls.Add(addButton, 2, 1);
        }

        private void CreateCustomButtons()
        {
            customTable!.Controls.Clear();
            customTable.RowStyles.Clear();

            if (settings.CustomSizes.Count == 0)
            {
                customTable.Visible = false;
                return;
            }

            customTable.Visible = true;
            bool isDark = settings.Theme == AppTheme.Dark;
            int rows = (settings.CustomSizes.Count + 2) / 3;
            customTable.RowCount = rows;

            for (int i = 0; i < rows; i++)
                customTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            for (int i = 0; i < settings.CustomSizes.Count; i++)
            {
                var custom = settings.CustomSizes[i];
                int col = i % 3;
                int row = i / 3;
                int index = i;

                var button = new GlowButton
                {
                    Text = $"{custom.Width}×{custom.Height}\n{custom.Name}",
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    BackColor = Color.FromArgb(200, 60, 120),
                    ForeColor = Color.White,
                    Size = new Size(180, 80),
                    Margin = new Padding(0),
                    Tag = (custom.Width, custom.Height)
                };
                button.SetGlow(isDark);
                button.Click += OnPresetButtonClick;

                var deleteButton = new Button
                {
                    Text = "×",
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    BackColor = Color.FromArgb(180, 50, 50),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(22, 22),
                    Location = new Point(148, 0),
                    Tag = index
                };
                deleteButton.FlatAppearance.BorderSize = 0;
                deleteButton.Click += OnDeleteCustomClick;

                button.Controls.Add(deleteButton);
                customTable.Controls.Add(button, col, row);
            }
        }

        private void OnDeleteCustomClick(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is int index)
            {
                var custom = settings.CustomSizes[index];
                var result = ConfirmDialog.Show($"Delete '{custom.Name}'?", "Delete Preset", settings.Theme, this);

                if (result == DialogResult.Yes)
                {
                    settings.CustomSizes.RemoveAt(index);
                    settings.Save();
                    CreateCustomButtons();
                    AdjustWindowSize();
                }
            }
        }

        private void AdjustWindowSize()
        {
            customScrollPanel!.Visible = settings.CustomSizes.Count > 0;
        }

        private Panel CreateBottomPanel()
        {
            var panel = new Panel
            {
                AutoSize = true,
                Dock = DockStyle.Top
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                AutoSize = true
            };

            for (int i = 0; i < 6; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            outputLabel = new Label
            {
                Text = $"Output: {TruncatePath(settings.OutputFolder, 50)}",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 3)
            };

            toolTip = new ToolTip();
            toolTip.SetToolTip(outputLabel, settings.OutputFolder);

            var outputButtonsPanel = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            outputButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            outputButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var openFolderButton = CreateSmallButton("Open Folder");
            openFolderButton.Margin = new Padding(0, 0, 5, 0);
            openFolderButton.Click += OnOpenFolderClick;

            var changeFolderButton = CreateSmallButton("Change Folder");
            changeFolderButton.Margin = new Padding(0);
            changeFolderButton.Click += OnChangeFolderClick;

            outputButtonsPanel.Controls.Add(openFolderButton, 0, 0);
            outputButtonsPanel.Controls.Add(changeFolderButton, 1, 0);

            hotkeyLabel = new Label
            {
                Text = $"Hotkey: {GetHotkeyLabelText()}",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 3)
            };

            var settingsButton = CreateSmallButton("Settings");
            settingsButton.Margin = new Padding(0, 0, 0, 5);
            settingsButton.Click += OnSettingsClick;

            var stayOnTopCheckbox = new CheckBox
            {
                Text = "Stay on Top",
                AutoSize = true,
                Checked = settings.StayOnTop
            };
            stayOnTopCheckbox.CheckedChanged += OnStayOnTopChanged;

            layout.Controls.Add(outputLabel, 0, 0);
            layout.Controls.Add(outputButtonsPanel, 0, 1);
            layout.Controls.Add(hotkeyLabel, 0, 2);
            layout.Controls.Add(settingsButton, 0, 3);
            layout.Controls.Add(stayOnTopCheckbox, 0, 4);

            panel.Controls.Add(layout);
            return panel;
        }

        private void ApplyTheme() => Theme.Apply(this, settings.Theme);

        private void RebuildButtons()
        {
            presetsTable!.Controls.Clear();
            CreatePresetButtons();
            CreateCustomButtons();
        }

        private Button CreateSmallButton(string text)
        {
            var button = new Button { Text = text, Size = new Size(100, 25) };
            Theme.StyleButton(button, settings.Theme);
            return button;
        }

        private void OnStayOnTopChanged(object? sender, EventArgs e)
        {
            if (sender is CheckBox checkbox)
            {
                settings.StayOnTop = checkbox.Checked;
                TopMost = checkbox.Checked;
                settings.Save();
            }
        }

        private void OnPresetButtonClick(object? sender, EventArgs e)
        {
            if (sender is Button button && button.Tag is (int width, int height))
            {
                settings.CaptureWidth = width;
                settings.CaptureHeight = height;
                UpdateHotkeyLabel();
                StartCapture();
            }
        }

        private void OnAddCustomClick(object? sender, EventArgs e)
        {
            using var dialog = new CustomSizeDialog(settings.CustomSizes.Count, settings.Theme);
            dialog.TopMost = TopMost;
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                var customSize = new CustomSize
                {
                    Width = dialog.CustomWidth,
                    Height = dialog.CustomHeight,
                    Name = dialog.CustomName
                };

                settings.CustomSizes.Add(customSize);
                settings.Save();
                CreateCustomButtons();
                AdjustWindowSize();
            }
        }

        private void StartCapture()
        {
            if (overlayWindow == null || overlayWindow.IsDisposed)
            {
                overlayWindow = new OverlayWindow(settings);
                overlayWindow.ScreenshotTaken += OnOverlayScreenshotTaken;
                overlayWindow.Show();
                if (!settings.StayOnTop)
                {
                    WindowState = FormWindowState.Minimized;
                }
            }
        }

        private void OnOverlayScreenshotTaken(object? sender, ScreenshotEventArgs e)
        {
            ScreenshotTaken?.Invoke(this, e);
            WindowState = FormWindowState.Normal;
            BringToFront();
        }

        private void OnOpenFolderClick(object? sender, EventArgs e)
        {
            try
            {
                if (Directory.Exists(settings.OutputFolder))
                {
                    System.Diagnostics.Process.Start("explorer.exe", settings.OutputFolder);
                }
                else
                {
                    MessageBox.Show("Output folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnChangeFolderClick(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                SelectedPath = settings.OutputFolder,
                Description = "Select output folder for screenshots"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                settings.OutputFolder = dialog.SelectedPath;
                settings.Save();
                UpdateOutputLabel();
            }
        }

        private void OnSettingsClick(object? sender, EventArgs e) => OpenSettings();

        public event EventHandler? SettingsChanged;

        public void OpenSettings()
        {
            using var settingsForm = new SettingsForm(settings);
            settingsForm.TopMost = TopMost;
            if (settingsForm.ShowDialog(this) == DialogResult.OK)
            {
                settings.HotkeyKey = settingsForm.Settings.HotkeyKey;
                settings.HotkeyModifiers = settingsForm.Settings.HotkeyModifiers;
                settings.HotkeyMode = settingsForm.Settings.HotkeyMode;
                settings.FixedCaptureWidth = settingsForm.Settings.FixedCaptureWidth;
                settings.FixedCaptureHeight = settingsForm.Settings.FixedCaptureHeight;
                settings.SnapMode = settingsForm.Settings.SnapMode;
                settings.MinimizeToTray = settingsForm.Settings.MinimizeToTray;
                settings.ContinuousCaptureMode = settingsForm.Settings.ContinuousCaptureMode;
                settings.ShowNotifications = settingsForm.Settings.ShowNotifications;
                settings.Theme = settingsForm.Settings.Theme;
                settings.Save();
                ApplyTheme();
                RebuildButtons();
                UpdateHotkeyLabel();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void UpdateHotkeyLabel()
        {
            if (hotkeyLabel == null) return;
            hotkeyLabel.Text = $"Hotkey: {GetHotkeyLabelText()}";
        }

        private string GetHotkeyLabelText()
        {
            if (settings.HotkeyMode == HotkeyMode.FixedSize)
            {
                return $"{GetHotkeyText()} (fixed: {settings.FixedCaptureWidth}×{settings.FixedCaptureHeight})";
            }
            else
            {
                return $"{GetHotkeyText()} (last-used: {settings.CaptureWidth}×{settings.CaptureHeight})";
            }
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (settings.MinimizeToTray && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            SaveWindowState();
            Application.Exit();
        }

        private void OnSizeChanged(object? sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Normal)
            {
                SaveWindowState();
            }
        }

        private void OnLocationChanged(object? sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Normal)
            {
                SaveWindowState();
            }
        }

        private void SaveWindowState()
        {
            if (WindowState == FormWindowState.Normal)
            {
                settings.WindowWidth = Width;
                settings.WindowHeight = Height;
                settings.WindowX = Location.X;
                settings.WindowY = Location.Y;
                settings.Save();
            }
        }

        private string GetHotkeyText()
        {
            var parts = new List<string>();

            if ((settings.HotkeyModifiers & Keys.Control) != 0) parts.Add("Ctrl");
            if ((settings.HotkeyModifiers & Keys.Alt) != 0) parts.Add("Alt");
            if ((settings.HotkeyModifiers & Keys.Shift) != 0) parts.Add("Shift");

            parts.Add(settings.HotkeyKey.ToString());

            return string.Join("+", parts);
        }

        private static string TruncatePath(string path, int maxLength)
        {
            if (string.IsNullOrEmpty(path) || path.Length <= maxLength)
                return path;

            // Try to show drive + ... + last folder/filename
            var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (parts.Length <= 2)
                return path.Substring(0, maxLength - 3) + "...";

            var drive = parts[0] + Path.DirectorySeparatorChar;
            var lastPart = parts[^1];

            if (drive.Length + 4 + lastPart.Length >= maxLength)
                return path.Substring(0, maxLength - 3) + "...";

            return $"{drive}...{Path.DirectorySeparatorChar}{lastPart}";
        }

        private void UpdateOutputLabel()
        {
            if (outputLabel != null)
            {
                outputLabel.Text = $"Output: {TruncatePath(settings.OutputFolder, 50)}";
                toolTip?.SetToolTip(outputLabel, settings.OutputFolder);
            }
        }
    }
}