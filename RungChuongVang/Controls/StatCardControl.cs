using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using RungChuongVang.Helpers;

namespace RungChuongVang.Controls
{
    /// <summary>
    /// Thẻ thống kê KPI trên Dashboard.
    /// </summary>
    public class StatCardControl : RoundedPanel
    {
        private readonly Label _lblTitle;
        private readonly Label _lblValue;
        private readonly Label _lblSub;

        public StatCardControl()
        {
            Size = new Size(240, 118);
            Padding = new Padding(16, 14, 16, 12);
            AccentTopColor = ThemeColors.PrimaryBlue;
            Cursor = Cursors.Hand;

            _lblTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                Font = ThemeColors.FontSmall,
                ForeColor = ThemeColors.TextMuted,
                Text = "TIÊU ĐỀ"
            };

            _lblValue = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                ForeColor = ThemeColors.NavyDark,
                Text = "0",
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblSub = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 22,
                Font = ThemeColors.FontSmall,
                ForeColor = ThemeColors.PrimaryBlue,
                Text = ""
            };

            Controls.Add(_lblValue);
            Controls.Add(_lblSub);
            Controls.Add(_lblTitle);

            foreach (Control c in Controls)
            {
                c.Click += (_, e) => OnClick(e);
            }
        }

        [Category("Data")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string CardTitle
        {
            get => _lblTitle.Text;
            set => _lblTitle.Text = value;
        }

        [Category("Data")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string CardValue
        {
            get => _lblValue.Text;
            set => _lblValue.Text = value;
        }

        [Category("Data")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string CardSubtitle
        {
            get => _lblSub.Text;
            set => _lblSub.Text = value;
        }
    }
}
