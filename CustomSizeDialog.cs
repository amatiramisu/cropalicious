using System;
using System.Drawing;
using System.Windows.Forms;

namespace Cropalicious
{
    public partial class CustomSizeDialog : Form
    {
        public int CustomWidth { get; private set; }
        public int CustomHeight { get; private set; }
        public string CustomName { get; private set; } = string.Empty;

        private NumericUpDown widthUpDown = null!;
        private NumericUpDown heightUpDown = null!;
        private TextBox nameTextBox = null!;

        public CustomSizeDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Add Custom Size";
            Size = new Size(300, 200);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var nameLabel = new Label
            {
                Text = "Name:",
                Location = new Point(20, 20),
                Size = new Size(50, 20)
            };

            nameTextBox = new TextBox
            {
                Location = new Point(80, 18),
                Size = new Size(180, 23)
            };

            var widthLabel = new Label
            {
                Text = "Width:",
                Location = new Point(20, 60),
                Size = new Size(50, 20)
            };

            widthUpDown = new NumericUpDown
            {
                Location = new Point(80, 58),
                Size = new Size(80, 23),
                Minimum = 100,
                Maximum = 4000,
                Value = 1024
            };

            var heightLabel = new Label
            {
                Text = "Height:",
                Location = new Point(170, 60),
                Size = new Size(50, 20)
            };

            heightUpDown = new NumericUpDown
            {
                Location = new Point(220, 58),
                Size = new Size(80, 23),
                Minimum = 100,
                Maximum = 4000,
                Value = 1024
            };

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(125, 110),
                Size = new Size(75, 25)
            };
            okButton.Click += OnOK;

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(210, 110),
                Size = new Size(75, 25)
            };

            Controls.AddRange(new Control[] {
                nameLabel, nameTextBox,
                widthLabel, widthUpDown,
                heightLabel, heightUpDown,
                okButton, cancelButton
            });
        }

        private void OnOK(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                MessageBox.Show("Please enter a name for the custom size.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CustomName = nameTextBox.Text.Trim();
            CustomWidth = (int)widthUpDown.Value;
            CustomHeight = (int)heightUpDown.Value;
            
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}