using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RungChuongVang.Controls;
using RungChuongVang.DataAccess;
using RungChuongVang.Enums;
using RungChuongVang.Helpers;
using RungChuongVang.Models;
using RungChuongVang.Services;

namespace RungChuongVang.Forms
{
    /// <summary>
    /// Phân hệ Quản lý Ngân hàng Câu hỏi với bảng 9 cột chuẩn, lọc đa tiêu chí, sao chép, ngừng dùng và Excel.
    /// </summary>
    public class FrmQuanLyCauHoi : Form
    {
        private readonly CauHoiRepository _repo = new();
        private readonly DanhMucRepository _danhMucRepo = new();
        private readonly ExcelService _excelService = new();
        private List<CauHoi> _questions = new();
        private Dictionary<MucDoKho, (string Ten, Color Mau)> _mucDoConfig = new();

        private TextBox txtSearch = null!;
        private ComboBox cboLop = null!;
        private ComboBox cboMon = null!;
        private ComboBox cboMucDo = null!;
        private ComboBox cboLoai = null!;
        private ComboBox cboTrangThai = null!;
        private StyledDataGridView dgv = null!;
        private Label lblCount = null!;

        public FrmQuanLyCauHoi()
        {
            BuildUi();
            LoadFilterDropdowns();
            LoadQuestions();
        }

        private void BuildUi()
        {
            BackColor = ThemeColors.AppBackground;
            Padding = new Padding(20);
            Font = ThemeColors.FontBody;

            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 114F));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));

            // Thanh tìm kiếm & nút thao tác
            var pnlTop = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 12), Padding = new Padding(14) };

            txtSearch = new TextBox { Location = new Point(14, 12), Width = 250, PlaceholderText = "Tìm mã hoặc nội dung câu hỏi..." };
            txtSearch.TextChanged += (_, _) => LoadQuestions();

            cboLop = new ComboBox { Location = new Point(274, 12), Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            cboLop.SelectedIndexChanged += (_, _) => LoadQuestions();

            cboMon = new ComboBox { Location = new Point(414, 12), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cboMon.SelectedIndexChanged += (_, _) => LoadQuestions();

            cboMucDo = new ComboBox { Location = new Point(574, 12), Width = 135, DropDownStyle = ComboBoxStyle.DropDownList };
            cboMucDo.SelectedIndexChanged += (_, _) => LoadQuestions();

            cboLoai = new ComboBox { Location = new Point(719, 12), Width = 165, DropDownStyle = ComboBoxStyle.DropDownList };
            cboLoai.SelectedIndexChanged += (_, _) => LoadQuestions();

            cboTrangThai = new ComboBox { Location = new Point(894, 12), Width = 145, DropDownStyle = ComboBoxStyle.DropDownList };
            cboTrangThai.SelectedIndexChanged += (_, _) => LoadQuestions();

            var flpBtns = new FlowLayoutPanel { Location = new Point(14, 52), Width = 1040, Height = 42, FlowDirection = FlowDirection.LeftToRight };
            var btnAdd = new ModernButton { Text = "+ Thêm câu hỏi", Size = new Size(135, 36), Margin = new Padding(0, 0, 8, 0) };
            btnAdd.Click += (_, _) => OpenAddEdit(0, false);

            var btnEdit = new ModernButton { Text = "Sửa", Size = new Size(85, 36), Margin = new Padding(0, 0, 8, 0), NormalBackColor = ThemeColors.RoyalBlue, HoverBackColor = ThemeColors.NavyDark };
            btnEdit.Click += (_, _) => EditSelected();

            var btnView = new ModernButton { Text = "Xem chi tiết", Size = new Size(115, 36), Margin = new Padding(0, 0, 8, 0), NormalBackColor = ThemeColors.InfoCyan, HoverBackColor = Color.FromArgb(14, 116, 144) };
            btnView.Click += (_, _) => ViewSelected();

            var btnClone = new ModernButton { Text = "Sao chép", Size = new Size(100, 36), Margin = new Padding(0, 0, 8, 0), NormalBackColor = ThemeColors.TextMuted, HoverBackColor = ThemeColors.TextPrimary };
            btnClone.Click += (_, _) => CloneSelected();

            var btnToggle = new ModernButton { Text = "Ngừng / Khôi phục", Size = new Size(150, 36), Margin = new Padding(0, 0, 8, 0), NormalBackColor = ThemeColors.WarningOrange, HoverBackColor = Color.FromArgb(180, 95, 5) };
            btnToggle.Click += (_, _) => ToggleSelected();

            var btnDel = new ModernButton { Text = "Xóa", Size = new Size(80, 36), Margin = new Padding(0, 0, 14, 0), NormalBackColor = ThemeColors.DangerRed, HoverBackColor = Color.FromArgb(185, 28, 28) };
            btnDel.Click += (_, _) => DeleteSelected();

            var btnImport = new ModernButton { Text = "Nhập Excel", Size = new Size(115, 36), Margin = new Padding(0, 0, 8, 0), NormalBackColor = ThemeColors.SuccessGreen, HoverBackColor = Color.FromArgb(16, 130, 58) };
            btnImport.Click += (_, _) => OpenImportExcel();

            var btnExport = new ModernButton { Text = "Xuất Excel", Size = new Size(115, 36), NormalBackColor = ThemeColors.Gold, HoverBackColor = ThemeColors.GoldBright, ForeColor = ThemeColors.NavyDark };
            btnExport.Click += (_, _) => ExportExcel();

            flpBtns.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnView, btnClone, btnToggle, btnDel, btnImport, btnExport });
            pnlTop.Controls.AddRange(new Control[] { txtSearch, cboLop, cboMon, cboMucDo, cboLoai, cboTrangThai, flpBtns });

            // Bảng 9 cột theo Mục IV.1
            var pnlGrid = new RoundedPanel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            dgv = new StyledDataGridView { Dock = DockStyle.Fill };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "STT", HeaderText = "STT", FillWeight = 6 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "MaCauHoi", HeaderText = "Mã câu hỏi", FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "NoiDung", HeaderText = "Nội dung câu hỏi", FillWeight = 28 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Lop", HeaderText = "Lớp", FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "MonHoc", HeaderText = "Môn học", FillWeight = 11 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ChuDe", HeaderText = "Chủ đề", FillWeight = 11 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "MucDo", HeaderText = "Mức độ khó", FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "LoaiCauHoi", HeaderText = "Loại câu hỏi", FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TrangThai", HeaderText = "Trạng thái", FillWeight = 11 });
            dgv.CellDoubleClick += (_, _) => EditSelected();
            pnlGrid.Controls.Add(dgv);

            lblCount = new Label { Dock = DockStyle.Fill, Font = ThemeColors.FontBodyBold, ForeColor = ThemeColors.TextMuted, TextAlign = ContentAlignment.MiddleLeft };

            tlp.Controls.Add(pnlTop, 0, 0);
            tlp.Controls.Add(pnlGrid, 0, 1);
            tlp.Controls.Add(lblCount, 0, 2);
            Controls.Add(tlp);
        }

        private void LoadFilterDropdowns()
        {
            _mucDoConfig = _danhMucRepo.GetMucDoConfig();

            var classes = _danhMucRepo.GetLopHoc(onlyActive: true);
            classes.Insert(0, new LopHoc { LopHocID = 0, TenLop = "Tất cả lớp" });
            cboLop.DataSource = classes;
            cboLop.DisplayMember = "TenLop";
            cboLop.ValueMember = "LopHocID";

            var subjects = _danhMucRepo.GetMonHoc(onlyActive: true);
            subjects.Insert(0, new MonHoc { MonHocID = 0, TenMonHoc = "Tất cả môn" });
            cboMon.DataSource = subjects;
            cboMon.DisplayMember = "TenMonHoc";
            cboMon.ValueMember = "MonHocID";

            var levels = new List<KeyValuePair<int, string>> { new(0, "Tất cả mức độ") };
            foreach (var kv in _mucDoConfig) levels.Add(new((int)kv.Key, kv.Value.Ten));
            cboMucDo.DataSource = levels;
            cboMucDo.DisplayMember = "Value";
            cboMucDo.ValueMember = "Key";

            cboLoai.DataSource = new List<KeyValuePair<int, string>>
            {
                new(0, "Tất cả loại"),
                new(1, "Một đáp án đúng"),
                new(2, "Nhiều đáp án đúng"),
                new(3, "Tự luận / Trả lời ngắn")
            };
            cboLoai.DisplayMember = "Value";
            cboLoai.ValueMember = "Key";

            cboTrangThai.DataSource = new List<KeyValuePair<int, string>>
            {
                new(1, "Đang sử dụng"),
                new(0, "Ngừng sử dụng"),
                new(-1, "Tất cả trạng thái")
            };
            cboTrangThai.DisplayMember = "Value";
            cboTrangThai.ValueMember = "Key";
        }

        private void LoadQuestions()
        {
            if (cboLop.SelectedValue == null || cboMon.SelectedValue == null) return;

            int? lopId = cboLop.SelectedValue is int l && l > 0 ? l : null;
            int? monId = cboMon.SelectedValue is int m && m > 0 ? m : null;
            MucDoKho? mucDo = cboMucDo.SelectedValue is int md && md > 0 ? (MucDoKho)md : null;
            LoaiCauHoi? loai = cboLoai.SelectedValue is int lo && lo > 0 ? (LoaiCauHoi)lo : null;
            bool? trangThai = cboTrangThai.SelectedValue is int tt && tt >= 0 ? (tt == 1) : null;

            _questions = _repo.SearchCauHoi(txtSearch.Text, lopId, monId, null, mucDo, loai, trangThai);
            dgv.Rows.Clear();

            for (int i = 0; i < _questions.Count; i++)
            {
                var q = _questions[i];
                string tenMucDo = _mucDoConfig.TryGetValue(q.MucDo, out var info) ? info.Ten : q.MucDo.ToString();
                int idx = dgv.Rows.Add(
                    i + 1,
                    q.MaCauHoi,
                    q.NoiDung,
                    q.DanhSachTenLop,
                    q.TenMonHoc,
                    q.TenChuDe,
                    tenMucDo,
                    ExcelService.FormatLoaiCauHoi(q.LoaiCauHoi),
                    q.TrangThai ? "Đang sử dụng" : "Ngừng sử dụng");

                if (_mucDoConfig.TryGetValue(q.MucDo, out var colorInfo))
                {
                    dgv.Rows[idx].Cells["MucDo"].Style.ForeColor = colorInfo.Mau;
                    dgv.Rows[idx].Cells["MucDo"].Style.Font = ThemeColors.FontBodyBold;
                }
            }

            lblCount.Text = $"Tổng số câu hỏi hiển thị: {_questions.Count}";
        }

        private CauHoi? GetSelected()
        {
            if (dgv.CurrentRow == null || dgv.CurrentRow.Index < 0 || dgv.CurrentRow.Index >= _questions.Count) return null;
            return _questions[dgv.CurrentRow.Index];
        }

        private void OpenAddEdit(int id, bool readOnly)
        {
            using var frm = new FrmThemSuaCauHoi(id, readOnly);
            if (frm.ShowDialog() == DialogResult.OK)
            {
                LoadQuestions();
            }
        }

        private void EditSelected()
        {
            var q = GetSelected();
            if (q != null) OpenAddEdit(q.CauHoiID, false);
        }

        private void ViewSelected()
        {
            var q = GetSelected();
            if (q != null) OpenAddEdit(q.CauHoiID, true);
        }

        private void CloneSelected()
        {
            var q = GetSelected();
            if (q == null) return;
            _repo.CloneQuestion(q.CauHoiID);
            LoadQuestions();
        }

        private void ToggleSelected()
        {
            var q = GetSelected();
            if (q == null) return;
            _repo.SetActiveStatus(q.CauHoiID, !q.TrangThai);
            LoadQuestions();
        }

        private void DeleteSelected()
        {
            var q = GetSelected();
            if (q == null) return;
            if (MessageBox.Show($"Bạn có chắc muốn xóa câu hỏi '{q.MaCauHoi}'?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            _repo.DeleteCauHoi(q.CauHoiID, out string msg);
            MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadQuestions();
        }

        private void OpenImportExcel()
        {
            using var frm = new FrmNhapExcel();
            if (frm.ShowDialog() == DialogResult.OK)
            {
                LoadQuestions();
            }
        }

        private void ExportExcel()
        {
            if (_questions.Count == 0)
            {
                MessageBox.Show("Không có câu hỏi nào để xuất.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Title = "Xuất danh sách câu hỏi ra Excel",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"NganHangCauHoi_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                _excelService.ExportQuestionsToExcel(_questions, sfd.FileName);
                MessageBox.Show("Đã xuất danh sách câu hỏi ra Excel thành công.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
