using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Cropalicious
{
    public partial class SettingsForm : Form
    {
        public AppSettings Settings { get; private set; }
        
        private ComboBox hotkeyModifiersCombo = null!;
        private ComboBox hotkeyKeyCombo = null!;
        private ComboBox hotkeyModeCombo = null!;
        private NumericUpDown widthUpDown = null!;
        private NumericUpDown heightUpDown = null!;
        private Label? dimensionsLabel;
        private Label? xLabel;
        private Label? pixelsLabel;
        private TextBox outputFolderTextBox = null!;
        private Button browseButton = null!;
        private ComboBox snapModeCombo = null!;
        private CheckBox minimizeToTrayCheckBox = null!;
        private CheckBox continuousCaptureCheckBox = null!;
        private CheckBox showNotificationsCheckBox = null!;
        private ComboBox themeCombo = null!;

        public SettingsForm(AppSettings currentSettings)
        {
            Settings = new AppSettings
            {
                HotkeyKey = currentSettings.HotkeyKey,
                HotkeyModifiers = currentSettings.HotkeyModifiers,
                CaptureWidth = currentSettings.CaptureWidth,
                CaptureHeight = currentSettings.CaptureHeight,
                OutputFolder = currentSettings.OutputFolder,
                SnapMode = currentSettings.SnapMode,
                MinimizeToTray = currentSettings.MinimizeToTray,
                ContinuousCaptureMode = currentSettings.ContinuousCaptureMode,
                ShowNotifications = currentSettings.ShowNotifications,
                HotkeyMode = currentSettings.HotkeyMode,
                FixedCaptureWidth = currentSettings.FixedCaptureWidth,
                FixedCaptureHeight = currentSettings.FixedCaptureHeight,
                Theme = currentSettings.Theme
            };

            InitializeComponent();
            Theme.Apply(this, Settings.Theme);
            LoadSettings();
        }

        private void InitializeComponent()
        {
            Text = "Cropalicious Settings";
            Size = new Size(450, 420);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var hotkeyLabel = new Label { Text = "Hotkey:", Location = new Point(15, 20), Size = new Size(80, 23) };

            hotkeyModifiersCombo = new ComboBox
            {
                Location = new Point(100, 17),
                Size = new Size(120, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            hotkeyModifiersCombo.Items.AddRange(new[] { "Ctrl+Shift", "Ctrl+Alt", "Alt+Shift", "Ctrl", "Alt" });

            var plusLabel = new Label { Text = "+", Location = new Point(230, 20), Size = new Size(15, 23) };

            hotkeyKeyCombo = new ComboBox
            {
                Location = new Point(250, 17),
                Size = new Size(60, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            hotkeyKeyCombo.Items.AddRange(new[] { "C", "X", "S", "A", "Q", "Z", "F1", "F2", "F3", "F4" });

            var hotkeyModeLabel = new Label { Text = "Hotkey Size:", Location = new Point(15, 60), Size = new Size(80, 23) };

            hotkeyModeCombo = new ComboBox
            {
                Location = new Point(100, 57),
                Size = new Size(150, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            hotkeyModeCombo.Items.AddRange(new[] { "Last preset", "Fixed" });
            hotkeyModeCombo.SelectedIndexChanged += OnHotkeyModeChanged;

            dimensionsLabel = new Label { Text = "Fixed Size:", Location = new Point(15, 95), Size = new Size(80, 23) };

            widthUpDown = new NumericUpDown
            {
                Location = new Point(100, 92),
                Size = new Size(70, 23),
                Minimum = 100,
                Maximum = 4000,
                Value = 1024
            };

            xLabel = new Label { Text = "×", Location = new Point(180, 95), Size = new Size(15, 23) };

            heightUpDown = new NumericUpDown
            {
                Location = new Point(200, 92),
                Size = new Size(70, 23),
                Minimum = 100,
                Maximum = 4000,
                Value = 1024
            };

            pixelsLabel = new Label { Text = "pixels", Location = new Point(280, 95), Size = new Size(40, 23) };

            var folderLabel = new Label { Text = "Output Folder:", Location = new Point(15, 135), Size = new Size(80, 23) };

            outputFolderTextBox = new TextBox
            {
                Location = new Point(100, 132),
                Size = new Size(240, 23),
                ReadOnly = true
            };

            browseButton = new Button
            {
                Text = "...",
                Location = new Point(350, 132),
                Size = new Size(30, 23)
            };
            browseButton.Click += OnBrowseFolder;

            var snapModeLabel = new Label { Text = "Snap Mode:", Location = new Point(15, 175), Size = new Size(80, 23) };

            snapModeCombo = new ComboBox
            {
                Location = new Point(100, 172),
                Size = new Size(150, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            snapModeCombo.Items.AddRange(new[] { "Snap", "Span", "Off (black fill)" });

            minimizeToTrayCheckBox = new CheckBox
            {
                Text = "Minimize to system tray on close",
                Location = new Point(15, 210),
                Size = new Size(250, 23)
            };

            continuousCaptureCheckBox = new CheckBox
            {
                Text = "Continuous capture mode",
                Location = new Point(15, 240),
                Size = new Size(250, 23)
            };

            showNotificationsCheckBox = new CheckBox
            {
                Text = "Show notifications",
                Location = new Point(15, 270),
                Size = new Size(250, 23)
            };

            var themeLabel = new Label { Text = "Theme:", Location = new Point(15, 310), Size = new Size(80, 23) };

            themeCombo = new ComboBox
            {
                Location = new Point(100, 307),
                Size = new Size(150, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            themeCombo.Items.AddRange(new[] { "Light", "Dark" });

            var versionLabel = new Label
            {
                Text = "Version 1.1.0.0",
                Location = new Point(15, 355),
                AutoSize = true,
                ForeColor = SystemColors.GrayText
            };

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(260, 350),
                Size = new Size(75, 25)
            };
            okButton.Click += OnOK;

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(345, 350),
                Size = new Size(75, 25)
            };

            Controls.AddRange(new Control[] {
                hotkeyLabel, hotkeyModifiersCombo, plusLabel, hotkeyKeyCombo,
                hotkeyModeLabel, hotkeyModeCombo,
                dimensionsLabel, widthUpDown, xLabel, heightUpDown, pixelsLabel,
                folderLabel, outputFolderTextBox, browseButton,
                snapModeLabel, snapModeCombo,
                minimizeToTrayCheckBox, continuousCaptureCheckBox, showNotificationsCheckBox,
                themeLabel, themeCombo,
                versionLabel, okButton, cancelButton
            });
        }

        private void OnHotkeyModeChanged(object? sender, EventArgs e)
        {
            bool isFixed = hotkeyModeCombo.SelectedIndex == 1;
            dimensionsLabel!.Enabled = isFixed;
            widthUpDown.Enabled = isFixed;
            xLabel!.Enabled = isFixed;
            heightUpDown.Enabled = isFixed;
            pixelsLabel!.Enabled = isFixed;
        }

        private void LoadSettings()
        {
            string modifierText = GetModifierText(Settings.HotkeyModifiers);
            hotkeyModifiersCombo.SelectedItem = modifierText;

            hotkeyKeyCombo.SelectedItem = Settings.HotkeyKey.ToString();

            hotkeyModeCombo.SelectedIndex = (int)Settings.HotkeyMode;
            widthUpDown.Value = Settings.FixedCaptureWidth;
            heightUpDown.Value = Settings.FixedCaptureHeight;
            OnHotkeyModeChanged(null, EventArgs.Empty);

            outputFolderTextBox.Text = Settings.OutputFolder;

            snapModeCombo.SelectedIndex = (int)Settings.SnapMode;
            minimizeToTrayCheckBox.Checked = Settings.MinimizeToTray;
            continuousCaptureCheckBox.Checked = Settings.ContinuousCaptureMode;
            showNotificationsCheckBox.Checked = Settings.ShowNotifications;
            themeCombo.SelectedIndex = (int)Settings.Theme;
        }

        private string GetModifierText(Keys modifiers)
        {
            if ((modifiers & Keys.Control) != 0 && (modifiers & Keys.Shift) != 0)
                return "Ctrl+Shift";
            if ((modifiers & Keys.Control) != 0 && (modifiers & Keys.Alt) != 0)
                return "Ctrl+Alt";
            if ((modifiers & Keys.Alt) != 0 && (modifiers & Keys.Shift) != 0)
                return "Alt+Shift";
            if ((modifiers & Keys.Control) != 0)
                return "Ctrl";
            if ((modifiers & Keys.Alt) != 0)
                return "Alt";
            return "Ctrl+Shift";
        }

        private Keys GetModifierKeys(string modifierText)
        {
            return modifierText switch
            {
                "Ctrl+Shift" => Keys.Control | Keys.Shift,
                "Ctrl+Alt" => Keys.Control | Keys.Alt,
                "Alt+Shift" => Keys.Alt | Keys.Shift,
                "Ctrl" => Keys.Control,
                "Alt" => Keys.Alt,
                _ => Keys.Control | Keys.Shift
            };
        }

        private void OnBrowseFolder(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                SelectedPath = Settings.OutputFolder,
                Description = "Select output folder for screenshots"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                outputFolderTextBox.Text = dialog.SelectedPath;
            }
        }

        private void OnOK(object? sender, EventArgs e)
        {
            try
            {
                Settings.HotkeyModifiers = GetModifierKeys(hotkeyModifiersCombo.SelectedItem?.ToString() ?? "Ctrl+Shift");
                
                if (Enum.TryParse<Keys>(hotkeyKeyCombo.SelectedItem?.ToString(), out var key))
                    Settings.HotkeyKey = key;

                Settings.HotkeyMode = (HotkeyMode)hotkeyModeCombo.SelectedIndex;
                Settings.FixedCaptureWidth = (int)widthUpDown.Value;
                Settings.FixedCaptureHeight = (int)heightUpDown.Value;
                Settings.OutputFolder = outputFolderTextBox.Text;
                Settings.SnapMode = (SnapMode)snapModeCombo.SelectedIndex;
                Settings.MinimizeToTray = minimizeToTrayCheckBox.Checked;
                Settings.ContinuousCaptureMode = continuousCaptureCheckBox.Checked;
                Settings.ShowNotifications = showNotificationsCheckBox.Checked;
                Settings.Theme = (AppTheme)themeCombo.SelectedIndex;

                if (!Directory.Exists(Settings.OutputFolder))
                {
                    try
                    {
                        Directory.CreateDirectory(Settings.OutputFolder);
                    }
                    catch
                    {
                        MessageBox.Show("Invalid output folder path.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}