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
        private Label? outputLabel;
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
            MinimumSize = new Size(600, 520);
            
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

            var titleLabel = new Label
            {
                Text = "Choose a capture size:",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 20)
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
                Margin = new Padding(0, 0, 0, 20)
            };

            for (int i = 0; i < 3; i++)
                customTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            CreateCustomButtons();

            var bottomPanel = CreateBottomPanel();

            mainLayout.Controls.Add(titleLabel, 0, 0);
            mainLayout.Controls.Add(presetsTable, 0, 1);
            mainLayout.Controls.Add(customTable, 0, 2);
            mainLayout.Controls.Add(bottomPanel, 0, 3);

            Controls.Add(mainLayout);
        }

        private void CreatePresetButtons()
        {
            for (int i = 0; i < presets.Length; i++)
            {
                var preset = presets[i];
                int col = i % 3;
                int row = i / 3;
                
                var button = new Button
                {
                    Text = preset.name,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    BackColor = Color.FromArgb(45, 125, 255),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(180, 80),
                    Margin = new Padding(5),
                    Tag = (preset.width, preset.height)
                };

                button.FlatAppearance.BorderSize = 0;
                button.Click += OnPresetButtonClick;
                presetsTable!.Controls.Add(button, col, row);
            }

            var addButton = new Button
            {
                Text = "+\nAdd Custom",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(34, 139, 34),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(180, 60),
                Margin = new Padding(5)
            };
            addButton.FlatAppearance.BorderSize = 0;
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

                var container = new Panel
                {
                    Size = new Size(190, 90),
                    Margin = new Padding(5)
                };

                var button = new Button
                {
                    Text = $"{custom.Name}\n{custom.Width}×{custom.Height}",
                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    BackColor = Color.FromArgb(128, 128, 128),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(180, 80),
                    Location = new Point(0, 5),
                    Tag = (custom.Width, custom.Height)
                };
                button.FlatAppearance.BorderSize = 0;
                button.Click += OnPresetButtonClick;

                var deleteButton = new Button
                {
                    Text = "×",
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    BackColor = Color.FromArgb(180, 50, 50),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(24, 24),
                    Location = new Point(156, 0),
                    Tag = index
                };
                deleteButton.FlatAppearance.BorderSize = 0;
                deleteButton.Click += OnDeleteCustomClick;

                container.Controls.Add(deleteButton);
                container.Controls.Add(button);
                customTable.Controls.Add(container, col, row);
            }
        }

        private void OnDeleteCustomClick(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is int index)
            {
                var custom = settings.CustomSizes[index];
                var result = MessageBox.Show(
                    $"Delete '{custom.Name}'?",
                    "Delete Preset",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    settings.CustomSizes.RemoveAt(index);
                    settings.Save();
                    CreateCustomButtons();
                }
            }
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
                RowCount = 3,
                AutoSize = true
            };

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var outputPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 10)
            };

            outputLabel = new Label
            {
                Text = $"Output: {TruncatePath(settings.OutputFolder, 50)}",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };

            toolTip = new ToolTip();
            toolTip.SetToolTip(outputLabel, settings.OutputFolder);

            var openFolderButton = new Button
            {
                Text = "Open Folder",
                AutoSize = true,
                Margin = new Padding(10, 0, 0, 0)
            };
            openFolderButton.Click += OnOpenFolderClick;

            var changeFolderButton = new Button
            {
                Text = "Change Folder",
                AutoSize = true,
                Margin = new Padding(5, 0, 0, 0)
            };
            changeFolderButton.Click += OnChangeFolderClick;

            outputPanel.Controls.AddRange(new Control[] { outputLabel, openFolderButton, changeFolderButton });

            var hotkeyPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false
            };

            var hotkeyLabel = new Label
            {
                Text = $"Global Hotkey: {GetHotkeyText()}",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };

            var changeHotkeyButton = new Button
            {
                Text = "Change Hotkey",
                AutoSize = true,
                Margin = new Padding(10, 0, 0, 0)
            };
            changeHotkeyButton.Click += OnChangeHotkeyClick;

            hotkeyPanel.Controls.AddRange(new Control[] { hotkeyLabel, changeHotkeyButton });

            var stayOnTopCheckbox = new CheckBox
            {
                Text = "Stay on Top",
                AutoSize = true,
                Checked = settings.StayOnTop,
                Margin = new Padding(0, 10, 0, 0)
            };
            stayOnTopCheckbox.CheckedChanged += OnStayOnTopChanged;

            layout.Controls.Add(outputPanel, 0, 0);
            layout.Controls.Add(hotkeyPanel, 0, 1);
            layout.Controls.Add(stayOnTopCheckbox, 0, 2);

            panel.Controls.Add(layout);
            return panel;
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
                StartCapture();
            }
        }

        private void OnAddCustomClick(object? sender, EventArgs e)
        {
            using var dialog = new CustomSizeDialog();
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

        private void OnChangeHotkeyClick(object? sender, EventArgs e)
        {
            using var settingsForm = new SettingsForm(settings);
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                settings.HotkeyKey = settingsForm.Settings.HotkeyKey;
                settings.HotkeyModifiers = settingsForm.Settings.HotkeyModifiers;
                settings.Save();
            }
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
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

            if ((settings.HotkeyModifiers & System.Windows.Forms.Keys.Control) != 0) parts.Add("Ctrl");
            if ((settings.HotkeyModifiers & System.Windows.Forms.Keys.Alt) != 0) parts.Add("Alt");
            if ((settings.HotkeyModifiers & System.Windows.Forms.Keys.Shift) != 0) parts.Add("Shift");

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