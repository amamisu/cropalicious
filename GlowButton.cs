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

            var glowSize = ScaleByFont(GlowSize);
            var radius = ScaleByFont(6);
            var rect = new Rectangle(glowSize, glowSize, Width - glowSize * 2, Height - glowSize * 2);

            if (glowEnabled && isHovered)
            {
                for (int i = glowSize; i > 0; i--)
                {
                    int alpha = 50 * i / glowSize;
                    using var glowBrush = new SolidBrush(Color.FromArgb(alpha, BackColor));
                    var glowRect = new Rectangle(glowSize - i, glowSize - i, Width - (glowSize - i) * 2, Height - (glowSize - i) * 2);
                    using var path = CreateRoundedRect(glowRect, radius + i);
                    g.FillPath(glowBrush, path);
                }
            }

            var buttonColor = isHovered ? ControlPaint.Light(BackColor, 0.15f) : BackColor;
            using (var bgBrush = new SolidBrush(buttonColor))
            using (var path = CreateRoundedRect(rect, radius))
            {
                g.FillPath(bgBrush, path);
            }

            TextRenderer.DrawText(g, Text, Font, rect, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
        }

        private int ScaleByFont(int value)
        {
            const float baseFontHeight = 15f;
            return Math.Max(1, (int)Math.Round(value * Font.Height / baseFontHeight));
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
