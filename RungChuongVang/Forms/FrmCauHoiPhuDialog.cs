using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RungChuongVang.Controls;
using RungChuongVang.DataAccess;
using RungChuongVang.Helpers;
using RungChuongVang.Models;

namespace RungChuongVang.Forms
{
    /// <summary>
    /// Hộp thoại chọn Câu hỏi phụ khi hết bộ câu hỏi chính mà vẫn còn nhiều thí sinh/đội.
    /// </summary>
    public class FrmCauHoiPhuDialog : Form
    {
        private readonly CauHoiRepository _repo = new();
        private readonly HashSet<int> _usedQuestionIds;
        private List<CauHoi> _candidates = new();

        private TextBox txtSearch = null!;
        private StyledDataGridView dgv = null!;
        public CauHoi? SelectedQuestion { get; private set; }

        public FrmCauHoiPhuDialog(IEnumerable<int> alreadyUsedQuestionIds)
        {
            _usedQuestionIds = alreadyUsedQuestionIds.ToHashSet();
            Text = "Chọn Câu hỏi phụ phân định thắng thua";
            Size = new Size(820, 540);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = ThemeColors.AppBackground;
            Font = ThemeColors.FontBody;

            BuildUi();
            LoadCandidates();
        }

        private void BuildUi()
        {
            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(14) };
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));

            txtSearch = new TextBox { Dock = DockStyle.Top, PlaceholderText = "Tìm câu hỏi phụ chưa sử dụng trong chương trình..." };
            txtSearch.TextChanged += (_, _) => LoadCandidates();

            dgv = new StyledDataGridView { Dock = DockStyle.Fill };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ma", HeaderText = "Mã", FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "NoiDung", HeaderText = "Nội dung câu hỏi", FillWeight = 58 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Mon", HeaderText = "Môn học", FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Giay", HeaderText = "Giây", FillWeight = 12 });
            dgv.CellDoubleClick += (_, _) => PickSelected();

            var pnlBottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 6, 0, 0) };
            var btnCancel = new ModernButton { Text = "Hủy", Size = new Size(100, 36), NormalBackColor = ThemeColors.TextMuted, HoverBackColor = ThemeColors.TextPrimary };
            btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
            var btnPick = new ModernButton { Text = "Thêm câu hỏi phụ", Size = new Size(165, 36), Margin = new Padding(0, 0, 10, 0) };
            btnPick.Click += (_, _) => PickSelected();

            pnlBottom.Controls.Add(btnCancel);
            pnlBottom.Controls.Add(btnPick);

            tlp.Controls.Add(txtSearch, 0, 0);
            tlp.Controls.Add(dgv, 0, 1);
            tlp.Controls.Add(pnlBottom, 0, 2);
            Controls.Add(tlp);
        }

        private void LoadCandidates()
        {
            _candidates = _repo.SearchCauHoi(txtSearch.Text, trangThai: true)
                .Where(q => !_usedQuestionIds.Contains(q.CauHoiID))
                .ToList();

            dgv.Rows.Clear();
            foreach (var q in _candidates)
            {
                dgv.Rows.Add(q.MaCauHoi, q.NoiDung, q.TenMonHoc, q.ThoiGianTraLoi);
            }
        }

        private void PickSelected()
        {
            if (dgv.CurrentRow == null || dgv.CurrentRow.Index < 0 || dgv.CurrentRow.Index >= _candidates.Count) return;
            SelectedQuestion = _candidates[dgv.CurrentRow.Index];
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
