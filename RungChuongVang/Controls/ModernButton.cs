using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RungChuongVang.Helpers;

namespace RungChuongVang.Controls
{
    /// <summary>
    /// Nút bấm bo góc phong cách thương mại chuyên nghiệp, hỗ trợ trạng thái Active cho Sidebar.
    /// </summary>
    public class ModernButton : Button
    {
        private int _borderRadius = 8;
        private bool _isActive;
        private Color _normalBackColor = ThemeColors.PrimaryBlue;
        private Color _hoverBackColor = ThemeColors.PrimaryBlueHover;
        private Color _activeBackColor = ThemeColors.Gold;
        private Color _activeForeColor = ThemeColors.NavyDark;
        private bool _isHovered;

        public ModernButton()
        {
            DoubleBuffered = true;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = ThemeColors.PrimaryBlue;
            ForeColor = Color.White;
            Font = ThemeColors.FontBodyBold;
            Cursor = Cursors.Hand;
            Size = new Size(140, 38);
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
        public Color NormalBackColor
        {
            get => _normalBackColor;
            set { _normalBackColor = value; BackColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color HoverBackColor
        {
            get => _hoverBackColor;
            set { _hoverBackColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color ActiveBackColor
        {
            get => _activeBackColor;
            set { _activeBackColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color ActiveForeColor
        {
            get => _activeForeColor;
            set { _activeForeColor = value; Invalidate(); }
        }

        [Category("Behavior")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color parentBg = Parent?.BackColor ?? ThemeColors.AppBackground;
            pevent.Graphics.Clear(parentBg);

            Color fill = _isActive
                ? _activeBackColor
                : (_isHovered ? _hoverBackColor : _normalBackColor);

            if (!Enabled)
            {
                fill = Color.FromArgb(180, 190, 205);
            }

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = new GraphicsPath();
            if (_borderRadius > 0)
            {
                int d = _borderRadius * 2;
                path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
            }
            else
            {
                path.AddRectangle(rect);
            }

            using var brush = new SolidBrush(fill);
            pevent.Graphics.FillPath(brush, path);

            Color textColor = _isActive ? _activeForeColor : ForeColor;
            TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
            if (TextAlign == ContentAlignment.MiddleLeft)
            {
                flags |= TextFormatFlags.Left;
            }
            else if (TextAlign == ContentAlignment.MiddleRight)
            {
                flags |= TextFormatFlags.Right;
            }
            else
            {
                flags |= TextFormatFlags.HorizontalCenter;
            }

            var textRect = new Rectangle(Padding.Left, 0, Width - Padding.Horizontal, Height);
            TextRenderer.DrawText(pevent.Graphics, Text, Font, textRect, textColor, flags);
        }
    }
}
