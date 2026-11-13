using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RungChuongVang.Controls;
using RungChuongVang.DataAccess;
using RungChuongVang.Enums;
using RungChuongVang.Helpers;
using RungChuongVang.Models;

namespace RungChuongVang.Forms
{
    /// <summary>
    /// Quản lý danh sách Chương trình thi Rung Chuông Vàng.
    /// </summary>
    public class FrmQuanLyChuongTrinh : Form
    {
        private readonly ChuongTrinhRepository _repo = new();
        private List<ChuongTrinh> _programs = new();

        private TextBox txtSearch = null!;
        private ComboBox cboStatus = null!;
        private StyledDataGridView dgv = null!;

        public event Action<int>? LaunchControlRequested;

        public FrmQuanLyChuongTrinh()
        {
            BuildUi();
            LoadPrograms();
        }

        private void BuildUi()
        {
            BackColor = ThemeColors.AppBackground;
            Padding = new Padding(20);
            Font = ThemeColors.FontBody;

            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 68F));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var pnlTop = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 12), Padding = new Padding(12) };
            txtSearch = new TextBox { Location = new Point(12, 12), Width = 220, PlaceholderText = "Tìm mã, tên chương trình..." };
            txtSearch.TextChanged += (_, _) => LoadPrograms();

            cboStatus = new ComboBox { Location = new Point(240, 12), Width = 155, DropDownStyle = ComboBoxStyle.DropDownList };
            cboStatus.DataSource = new List<KeyValuePair<int, string>>
            {
                new(-1, "Tất cả trạng thái"),
                new(0, "Chưa bắt đầu"),
                new(1, "Đang diễn ra"),
                new(2, "Tạm dừng"),
                new(3, "Đã kết thúc")
            };
            cboStatus.DisplayMember = "Value";
            cboStatus.ValueMember = "Key";
            cboStatus.SelectedIndexChanged += (_, _) => LoadPrograms();

            var flpBtns = new FlowLayoutPanel { Location = new Point(408, 8), Width = 680, Height = 40 };
            var btnNew = new ModernButton { Text = "+ Tạo chương trình", Size = new Size(150, 36), Margin = new Padding(0, 0, 8, 0) };
            btnNew.Click += (_, _) => CreateProgram();

            var btnEdit = new ModernButton { Text = "Sửa / Thí sinh", Size = new Size(130, 36), Margin = new Padding(0, 0, 8, 0), NormalBackColor = ThemeColors.RoyalBlue, HoverBackColor = ThemeColors.NavyDark };
            btnEdit.Click += (_, _) => EditSelected();

            var btnQuestions = new ModernButton { Text = "Bộ câu hỏi", Size = new Size(115, 36), Margin = new Padding(0, 0, 8, 0), NormalBackColor = ThemeColors.InfoCyan, HoverBackColor = Color.FromArgb(14, 116, 144) };
            btnQuestions.Click += (_, _) => OpenQuestionSet();

            var btnControl = new ModernButton { Text = "Điều khiển thi", Size = new Size(135, 36), Margin = new Padding(0, 0, 8, 0), NormalBackColor = ThemeColors.Gold, HoverBackColor = ThemeColors.GoldBright, ForeColor = ThemeColors.NavyDark };
            btnControl.Click += (_, _) => LaunchSelectedControl();

            var btnDel = new ModernButton { Text = "Xóa", Size = new Size(80, 36), NormalBackColor = ThemeColors.DangerRed, HoverBackColor = Color.FromArgb(185, 28, 28) };
            btnDel.Click += (_, _) => DeleteSelected();

            flpBtns.Controls.AddRange(new Control[] { btnNew, btnEdit, btnQuestions, btnControl, btnDel });
            pnlTop.Controls.AddRange(new Control[] { txtSearch, cboStatus, flpBtns });

            var pnlGrid = new RoundedPanel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            dgv = new StyledDataGridView { Dock = DockStyle.Fill };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "STT", HeaderText = "STT", FillWeight = 6 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ma", HeaderText = "Mã CT", FillWeight = 11 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ten", HeaderText = "Tên chương trình", FillWeight = 28 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ngay", HeaderText = "Ngày tổ chức", FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "PhuongThuc", HeaderText = "Phương thức", FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoCau", HeaderText = "Số câu", FillWeight = 8 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "KhoaDe", HeaderText = "Bộ đề", FillWeight = 11 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TrangThai", HeaderText = "Trạng thái", FillWeight = 11 });
            dgv.CellDoubleClick += (_, _) => EditSelected();
            pnlGrid.Controls.Add(dgv);

            tlp.Controls.Add(pnlTop, 0, 0);
            tlp.Controls.Add(pnlGrid, 0, 1);
            Controls.Add(tlp);
        }

        private void LoadPrograms()
        {
            if (cboStatus.SelectedValue == null) return;
            TrangThaiChuongTrinhEnum? st = cboStatus.SelectedValue is int v && v >= 0 ? (TrangThaiChuongTrinhEnum)v : null;
            _programs = _repo.GetAllPrograms(txtSearch.Text, st);

            dgv.Rows.Clear();
            for (int i = 0; i < _programs.Count; i++)
            {
                var p = _programs[i];
                dgv.Rows.Add(
                    i + 1,
                    p.MaChuongTrinh,
                    p.TenChuongTrinh,
                    p.NgayToChuc.ToString("dd/MM/yyyy"),
                    p.PhuongThucThi == PhuongThucThi.ThiCaNhan ? "Cá nhân" : "Theo đội",
                    p.SoLuongCauHoi,
                    p.DaKhoaBoCauHoi ? "Đã khóa" : "Mở",
                    FormatStatus(p.TrangThai));
            }
        }

        private ChuongTrinh? GetSelected()
        {
            if (dgv.CurrentRow == null || dgv.CurrentRow.Index < 0 || dgv.CurrentRow.Index >= _programs.Count) return null;
            return _programs[dgv.CurrentRow.Index];
        }

        private void CreateProgram()
        {
            using var frm = new FrmTaoChuongTrinh();
            if (frm.ShowDialog() == DialogResult.OK) LoadPrograms();
        }

        private void EditSelected()
        {
            var p = GetSelected();
            if (p == null) return;
            using var frm = new FrmTaoChuongTrinh(p.ChuongTrinhID);
            if (frm.ShowDialog() == DialogResult.OK) LoadPrograms();
        }

        private void OpenQuestionSet()
        {
            var p = GetSelected();
            if (p == null) return;
            using var frm = new FrmChonCauHoi(p.ChuongTrinhID);
            frm.ShowDialog();
            LoadPrograms();
        }

        private void LaunchSelectedControl()
        {
            var p = GetSelected();
            if (p == null) return;
            LaunchControlRequested?.Invoke(p.ChuongTrinhID);
        }

        private void DeleteSelected()
        {
            var p = GetSelected();
            if (p == null) return;
            if (MessageBox.Show($"Xác nhận xóa chương trình '{p.TenChuongTrinh}'?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            _repo.DeleteProgram(p.ChuongTrinhID, out string msg);
            MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadPrograms();
        }

        private static string FormatStatus(TrangThaiChuongTrinhEnum st) => st switch
        {
            TrangThaiChuongTrinhEnum.ChuaBatDau => "Chưa bắt đầu",
            TrangThaiChuongTrinhEnum.DangDienRa => "Đang diễn ra",
            TrangThaiChuongTrinhEnum.TamDung => "Tạm dừng",
            TrangThaiChuongTrinhEnum.DaKetThuc => "Đã kết thúc",
            _ => "Khác"
        };
    }
}
