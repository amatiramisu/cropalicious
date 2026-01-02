using System.Drawing;
using System.Windows.Forms;

namespace Cropalicious
{
    public class ConfirmDialog : Form
    {
        public ConfirmDialog(string message, string title, AppTheme theme)
        {
            Text = title;
            Size = new Size(300, 150);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Theme.Apply(this, theme);

            var label = new Label
            {
                Text = message,
                AutoSize = false,
                Size = new Size(260, 50),
                Location = new Point(20, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var yesButton = new Button
            {
                Text = "Yes",
                DialogResult = DialogResult.Yes,
                Size = new Size(75, 28),
                Location = new Point(60, 75)
            };
            Theme.StyleButton(yesButton, theme);

            var noButton = new Button
            {
                Text = "No",
                DialogResult = DialogResult.No,
                Size = new Size(75, 28),
                Location = new Point(160, 75)
            };
            Theme.StyleButton(noButton, theme);

            Controls.AddRange(new Control[] { label, yesButton, noButton });
        }

        public static DialogResult Show(string message, string title, AppTheme theme, Form? owner = null)
        {
            using var dialog = new ConfirmDialog(message, title, theme);
            if (owner != null) dialog.TopMost = owner.TopMost;
            return dialog.ShowDialog(owner);
        }
    }
}
