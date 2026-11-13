using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RungChuongVang.Controls;
using RungChuongVang.Enums;
using RungChuongVang.Helpers;
using RungChuongVang.Models;

namespace RungChuongVang.Forms
{
    /// <summary>
    /// Hộp thoại Cứu trợ / Khôi phục thí sinh hoặc đội đã bị loại.
    /// </summary>
    public class FrmCuuTroDialog : Form
    {
        private readonly List<ChuongTrinhThiSinh> _eliminated;
        private RadioButton rbAll = null!;
        private RadioButton rbRandomN = null!;
        private RadioButton rbManual = null!;
        private NumericUpDown numCount = null!;
        private CheckedListBox clbList = null!;
        private TextBox txtReason = null!;

        public List<int> SelectedRecordIds { get; private set; } = new();
        public List<string> SelectedNames { get; private set; } = new();
        public string ReasonText => txtReason.Text.Trim();

        public FrmCuuTroDialog(List<ChuongTrinhThiSinh> eliminated, bool isTeamMode)
        {
            _eliminated = eliminated;
            Text = isTeamMode ? "Cứu trợ / Khôi phục Đội thi" : "Cứu trợ / Khôi phục Thí sinh";
            Size = new Size(560, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = ThemeColors.AppBackground;
            Font = ThemeColors.FontBody;

            BuildUi(isTeamMode);
        }

        private void BuildUi(bool isTeamMode)
        {
            string unit = isTeamMode ? "đội" : "thí sinh";
            var pnl = new RoundedPanel { Dock = DockStyle.Fill, Padding = new Padding(16) };

            pnl.Controls.Add(new Label
            {
                Text = $"Hiện có {_eliminated.Count} {unit} đang bị loại:",
                Location = new Point(16, 14),
                AutoSize = true,
                Font = ThemeColors.FontSubTitle,
                ForeColor = ThemeColors.NavyDark
            });

            rbAll = new RadioButton { Text = $"Cứu trợ toàn bộ ({_eliminated.Count} {unit})", Location = new Point(16, 52), AutoSize = true, Checked = true };
            rbRandomN = new RadioButton { Text = $"Cứu trợ ngẫu nhiên số lượng:", Location = new Point(16, 82), AutoSize = true };
            numCount = new NumericUpDown { Location = new Point(255, 79), Width = 80, Minimum = 1, Maximum = Math.Max(1, _eliminated.Count), Value = Math.Min(5, Math.Max(1, _eliminated.Count)), Enabled = false };
            rbManual = new RadioButton { Text = $"Chọn cụ thể từng {unit}:", Location = new Point(16, 112), AutoSize = true };

            rbRandomN.CheckedChanged += (_, _) => numCount.Enabled = rbRandomN.Checked;
            rbManual.CheckedChanged += (_, _) => clbList.Enabled = rbManual.Checked;

            clbList = new CheckedListBox { Location = new Point(16, 142), Width = 500, Height = 190, CheckOnClick = true, Enabled = false };
            foreach (var p in _eliminated)
            {
                clbList.Items.Add(new ItemChoice(p.ID, $"{p.SoBaoDanh} - {p.TenHienThi} ({p.TenLop})"));
            }

            pnl.Controls.Add(new Label { Text = "Lý do cứu trợ:", Location = new Point(16, 344), AutoSize = true, Font = ThemeColors.FontBodyBold });
            txtReason = new TextBox { Location = new Point(16, 368), Width = 500, Text = "Cứu trợ từ Ban Tổ Chức / Thầy Cô" };

            var btnConfirm = new ModernButton { Text = "Xác nhận Cứu trợ", Location = new Point(240, 418), Size = new Size(165, 38), NormalBackColor = ThemeColors.SuccessGreen, HoverBackColor = Color.FromArgb(16, 130, 58) };
            btnConfirm.Click += (_, _) => OnConfirm();
            var btnCancel = new ModernButton { Text = "Hủy", Location = new Point(416, 418), Size = new Size(100, 38), NormalBackColor = ThemeColors.TextMuted, HoverBackColor = ThemeColors.TextPrimary };
            btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

            pnl.Controls.AddRange(new Control[] { rbAll, rbRandomN, numCount, rbManual, clbList, txtReason, btnConfirm, btnCancel });
            Controls.Add(pnl);
        }

        private void OnConfirm()
        {
            if (_eliminated.Count == 0)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            List<ChuongTrinhThiSinh> picked;
            if (rbAll.Checked)
            {
                picked = _eliminated;
            }
            else if (rbRandomN.Checked)
            {
                int n = (int)numCount.Value;
                picked = _eliminated.OrderBy(_ => Random.Shared.Next()).Take(n).ToList();
            }
            else
            {
                var ids = clbList.CheckedItems.OfType<ItemChoice>().Select(x => x.Id).ToHashSet();
                picked = _eliminated.Where(x => ids.Contains(x.ID)).ToList();
            }

            if (picked.Count == 0)
            {
                MessageBox.Show("Chưa có đối tượng nào được chọn để cứu trợ.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SelectedRecordIds = picked.Select(x => x.ID).ToList();
            SelectedNames = picked.Select(x => $"{x.SoBaoDanh} - {x.TenHienThi}").ToList();
            DialogResult = DialogResult.OK;
            Close();
        }

        private sealed class ItemChoice
        {
            public int Id { get; }
            public string Display { get; }
            public ItemChoice(int id, string display) { Id = id; Display = display; }
            public override string ToString() => Display;
        }
    }
}
