using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Cropalicious
{
    public class GlowButton : Button
    {
        private bool isHovered;
        private bool glowEnabled = true;
        private const int GlowSize = 10;

        public GlowButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
        }

        public void SetGlow(bool enabled) => glowEnabled = enabled;

        protected override void OnMouseEnter(EventArgs e)
        {
            isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            isHovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? Color.Black);

            var rect = new Rectangle(GlowSize, GlowSize, Width - GlowSize * 2, Height - GlowSize * 2);

            if (glowEnabled && isHovered)
            {
                for (int i = GlowSize; i > 0; i--)
                {
                    int alpha = 50 * i / GlowSize;
                    using var glowBrush = new SolidBrush(Color.FromArgb(alpha, BackColor));
                    var glowRect = new Rectangle(GlowSize - i, GlowSize - i, Width - (GlowSize - i) * 2, Height - (GlowSize - i) * 2);
                    using var path = CreateRoundedRect(glowRect, 6 + i);
                    g.FillPath(glowBrush, path);
                }
            }

            var buttonColor = isHovered ? ControlPaint.Light(BackColor, 0.15f) : BackColor;
            using (var bgBrush = new SolidBrush(buttonColor))
            using (var path = CreateRoundedRect(rect, 6))
            {
                g.FillPath(bgBrush, path);
            }

            TextRenderer.DrawText(g, Text, Font, rect, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
        }

        private GraphicsPath CreateRoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
