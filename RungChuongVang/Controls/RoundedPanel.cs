using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RungChuongVang.Helpers;

namespace RungChuongVang.Controls
{
    /// <summary>
    /// Panel bo góc hiện đại, chống nhấp nháy (DoubleBuffered) và có đường viền tinh tế.
    /// </summary>
    public class RoundedPanel : Panel
    {
        private int _borderRadius = 12;
        private Color _borderColor = ThemeColors.BorderLight;
        private int _borderThickness = 1;
        private Color _accentTopColor = Color.Transparent;

        public RoundedPanel()
        {
            DoubleBuffered = true;
            BackColor = ThemeColors.SurfaceWhite;
            Padding = new Padding(16);
        }

        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int BorderRadius
        {
            get => _borderRadius;
            set { _borderRadius = Math.Max(0, value); Invalidate(); }
        }

        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int BorderThickness
        {
            get => _borderThickness;
            set { _borderThickness = Math.Max(0, value); Invalidate(); }
        }

        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color AccentTopColor
        {
            get => _accentTopColor;
            set { _accentTopColor = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using GraphicsPath path = CreateRoundedRect(rect, _borderRadius);
            using var bgBrush = new SolidBrush(BackColor);
            e.Graphics.FillPath(bgBrush, path);

            if (_accentTopColor != Color.Transparent)
            {
                e.Graphics.SetClip(path);
                using var accentBrush = new SolidBrush(_accentTopColor);
                e.Graphics.FillRectangle(accentBrush, 0, 0, Width, 4);
                e.Graphics.ResetClip();
            }

            if (_borderThickness > 0)
            {
                using var pen = new Pen(_borderColor, _borderThickness);
                e.Graphics.DrawPath(pen, path);
            }

            base.OnPaint(e);
        }

        private static GraphicsPath CreateRoundedRect(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            int d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
