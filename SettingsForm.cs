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
        private NumericUpDown widthUpDown = null!;
        private NumericUpDown heightUpDown = null!;
        private TextBox outputFolderTextBox = null!;
        private Button browseButton = null!;

        public SettingsForm(AppSettings currentSettings)
        {
            Settings = new AppSettings
            {
                HotkeyKey = currentSettings.HotkeyKey,
                HotkeyModifiers = currentSettings.HotkeyModifiers,
                CaptureWidth = currentSettings.CaptureWidth,
                CaptureHeight = currentSettings.CaptureHeight,
                OutputFolder = currentSettings.OutputFolder
            };

            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            Text = "Cropalicious Settings";
            Size = new Size(450, 280);
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

            var dimensionsLabel = new Label { Text = "Capture Size:", Location = new Point(15, 60), Size = new Size(80, 23) };
            
            widthUpDown = new NumericUpDown
            {
                Location = new Point(100, 57),
                Size = new Size(70, 23),
                Minimum = 100,
                Maximum = 4000,
                Value = 1024
            };

            var xLabel = new Label { Text = "×", Location = new Point(180, 60), Size = new Size(15, 23) };

            heightUpDown = new NumericUpDown
            {
                Location = new Point(200, 57),
                Size = new Size(70, 23),
                Minimum = 100,
                Maximum = 4000,
                Value = 1024
            };

            var pixelsLabel = new Label { Text = "pixels", Location = new Point(280, 60), Size = new Size(40, 23) };

            var folderLabel = new Label { Text = "Output Folder:", Location = new Point(15, 100), Size = new Size(80, 23) };
            
            outputFolderTextBox = new TextBox
            {
                Location = new Point(100, 97),
                Size = new Size(240, 23),
                ReadOnly = true
            };

            browseButton = new Button
            {
                Text = "...",
                Location = new Point(350, 97),
                Size = new Size(30, 23)
            };
            browseButton.Click += OnBrowseFolder;

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(275, 210),
                Size = new Size(75, 25)
            };
            okButton.Click += OnOK;

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(360, 210),
                Size = new Size(75, 25)
            };

            Controls.AddRange(new Control[] { 
                hotkeyLabel, hotkeyModifiersCombo, plusLabel, hotkeyKeyCombo,
                dimensionsLabel, widthUpDown, xLabel, heightUpDown, pixelsLabel,
                folderLabel, outputFolderTextBox, browseButton,
                okButton, cancelButton
            });
        }

        private void LoadSettings()
        {
            string modifierText = GetModifierText(Settings.HotkeyModifiers);
            hotkeyModifiersCombo.SelectedItem = modifierText;

            hotkeyKeyCombo.SelectedItem = Settings.HotkeyKey.ToString();

            widthUpDown.Value = Settings.CaptureWidth;
            heightUpDown.Value = Settings.CaptureHeight;

            outputFolderTextBox.Text = Settings.OutputFolder;
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

                Settings.CaptureWidth = (int)widthUpDown.Value;
                Settings.CaptureHeight = (int)heightUpDown.Value;
                Settings.OutputFolder = outputFolderTextBox.Text;

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