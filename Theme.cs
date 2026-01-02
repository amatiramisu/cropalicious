using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Cropalicious
{
    public static class Theme
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public static readonly Color DarkBg = Color.FromArgb(32, 32, 32);
        public static readonly Color DarkControl = Color.FromArgb(45, 45, 45);
        public static readonly Color DarkBorder = Color.FromArgb(60, 60, 60);
        public static readonly Color DarkButtonBg = Color.FromArgb(60, 60, 60);
        public static readonly Color DarkButtonBorder = Color.FromArgb(80, 80, 80);

        public static void StyleButton(Button btn, AppTheme theme)
        {
            if (theme == AppTheme.Dark)
            {
                btn.BackColor = DarkButtonBg;
                btn.ForeColor = Color.White;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = DarkButtonBorder;
                btn.FlatAppearance.MouseOverBackColor = DarkButtonBorder;
            }
            else
            {
                btn.BackColor = SystemColors.Control;
                btn.ForeColor = SystemColors.ControlText;
                btn.FlatStyle = FlatStyle.Standard;
            }
        }

        public static void Apply(Form form, AppTheme theme)
        {
            if (theme == AppTheme.Dark)
            {
                int value = 1;
                DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
                form.BackColor = DarkBg;
                form.ForeColor = Color.White;
                ApplyToControls(form.Controls);
            }
            else
            {
                int value = 0;
                DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
                form.BackColor = SystemColors.Control;
                form.ForeColor = SystemColors.ControlText;
                ApplyLightToControls(form.Controls);
            }
        }

        private static void ApplyToControls(Control.ControlCollection controls)
        {
            foreach (Control c in controls)
            {
                if (c is ComboBox combo)
                {
                    combo.BackColor = DarkControl;
                    combo.ForeColor = Color.White;
                    combo.FlatStyle = FlatStyle.Flat;
                }
                else if (c is TextBox txt)
                {
                    txt.BackColor = DarkControl;
                    txt.ForeColor = Color.White;
                    txt.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (c is NumericUpDown num)
                {
                    num.BackColor = DarkControl;
                    num.ForeColor = Color.White;
                }
                else if (c is Button btn && c.GetType() != typeof(GlowButton))
                {
                    btn.BackColor = DarkControl;
                    btn.ForeColor = Color.White;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderColor = DarkBorder;
                }
                else if (c is CheckBox chk)
                {
                    chk.ForeColor = Color.White;
                }
                else if (c is Label lbl)
                {
                    lbl.ForeColor = Color.White;
                }
                else
                {
                    c.ForeColor = Color.White;
                }

                if (c.HasChildren && c.GetType() != typeof(GlowButton))
                    ApplyToControls(c.Controls);
            }
        }

        private static void ApplyLightToControls(Control.ControlCollection controls)
        {
            foreach (Control c in controls)
            {
                if (c is ComboBox combo)
                {
                    combo.BackColor = SystemColors.Window;
                    combo.ForeColor = SystemColors.ControlText;
                    combo.FlatStyle = FlatStyle.Standard;
                }
                else if (c is TextBox txt)
                {
                    txt.BackColor = SystemColors.Window;
                    txt.ForeColor = SystemColors.ControlText;
                    txt.BorderStyle = BorderStyle.Fixed3D;
                }
                else if (c is NumericUpDown num)
                {
                    num.BackColor = SystemColors.Window;
                    num.ForeColor = SystemColors.ControlText;
                }
                else if (c is Button btn && c.GetType() != typeof(GlowButton))
                {
                    btn.BackColor = SystemColors.Control;
                    btn.ForeColor = SystemColors.ControlText;
                    btn.FlatStyle = FlatStyle.Standard;
                }
                else if (c is CheckBox chk)
                {
                    chk.ForeColor = SystemColors.ControlText;
                }
                else if (c is Label lbl)
                {
                    lbl.ForeColor = SystemColors.ControlText;
                }
                else
                {
                    c.ForeColor = SystemColors.ControlText;
                }

                if (c.HasChildren && c.GetType() != typeof(GlowButton))
                    ApplyLightToControls(c.Controls);
            }
        }
    }
}
