using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RungChuongVang.Controls;
using RungChuongVang.DataAccess;
using RungChuongVang.Helpers;
using RungChuongVang.Models;
using RungChuongVang.Services;

namespace RungChuongVang.Forms
{
    /// <summary>
    /// Quản lý danh sách Thí sinh: CRUD, lọc theo lớp, hình ảnh đại diện, nhập và xuất Excel.
    /// </summary>
    public class FrmQuanLyThiSinh : Form
    {
        private readonly ThiSinhRepository _repo = new();
        private readonly DanhMucRepository _danhMucRepo = new();
        private readonly ExcelService _excelService = new();
        private List<ThiSinh> _list = new();
        private int _editingId;
        private string _currentImgPath = string.Empty;
        private string? _pendingImgPath;
        private bool _imgRemoved;

        private TextBox txtSearch = null!;
        private ComboBox cboFilterLop = null!;
        private StyledDataGridView dgv = null!;

        private TextBox txtMa = null!;
        private TextBox txtSbd = null!;
        private TextBox txtHoTen = null!;
        private ComboBox cboLop = null!;
        private TextBox txtTruong = null!;
        private ComboBox cboGioiTinh = null!;
        private TextBox txtGhiChu = null!;
        private CheckBox chkTrangThai = null!;
        private PictureBox picAvatar = null!;

        public FrmQuanLyThiSinh()
        {
            BuildUi();
            LoadDropdowns();
            LoadData();
            ResetForm();
        }

        private void BuildUi()
        {
            BackColor = ThemeColors.AppBackground;
            Padding = new Padding(20);
            Font = ThemeColors.FontBody;

            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 63F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37F));

            // Trái: Tìm kiếm, Excel, Bảng thí sinh
            var pnlLeft = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0), Padding = new Padding(14) };
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 86 };

            txtSearch = new TextBox { Location = new Point(0, 6), Width = 220, PlaceholderText = "Tìm mã, SBD, họ tên..." };
            txtSearch.TextChanged += (_, _) => LoadData();

            cboFilterLop = new ComboBox { Location = new Point(230, 6), Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
            cboFilterLop.SelectedIndexChanged += (_, _) => LoadData();

            var flpExcel = new FlowLayoutPanel { Location = new Point(0, 42), Width = 680, Height = 40 };
            var btnTpl = new ModernButton { Text = "Tải mẫu Excel", Size = new Size(125, 34), Margin = new Padding(0, 0, 8, 0), NormalBackColor = ThemeColors.TextMuted, HoverBackColor = ThemeColors.TextPrimary };
            btnTpl.Click += (_, _) => DownloadTemplate();
            var btnImp = new ModernButton { Text = "Nhập Excel", Size = new Size(115, 34), Margin = new Padding(0, 0, 8, 0), NormalBackColor = ThemeColors.SuccessGreen, HoverBackColor = Color.FromArgb(16, 130, 58) };
            btnImp.Click += (_, _) => ImportExcel();
            var btnExp = new ModernButton { Text = "Xuất Excel", Size = new Size(115, 34), Margin = new Padding(0, 0, 8, 0), NormalBackColor = ThemeColors.Gold, HoverBackColor = ThemeColors.GoldBright, ForeColor = ThemeColors.NavyDark };
            btnExp.Click += (_, _) => ExportExcel();
            var btnDel = new ModernButton { Text = "Xóa", Size = new Size(85, 34), NormalBackColor = ThemeColors.DangerRed, HoverBackColor = Color.FromArgb(185, 28, 28) };
            btnDel.Click += (_, _) => DeleteSelected();

            flpExcel.Controls.AddRange(new Control[] { btnTpl, btnImp, btnExp, btnDel });
            pnlTop.Controls.AddRange(new Control[] { txtSearch, cboFilterLop, flpExcel });

            dgv = new StyledDataGridView { Dock = DockStyle.Fill };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "STT", HeaderText = "STT", FillWeight = 8 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ma", HeaderText = "Mã TS", FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "SBD", HeaderText = "SBD", FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "HoTen", HeaderText = "Họ và tên", FillWeight = 27 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Lop", HeaderText = "Lớp", FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Truong", HeaderText = "Trường", FillWeight = 24 });
            dgv.SelectionChanged += (_, _) => PopulateFromSelected();

            pnlLeft.Controls.Add(dgv);
            pnlLeft.Controls.Add(pnlTop);

            // Phải: Form chi tiết thí sinh
            var pnlRight = new RoundedPanel { Dock = DockStyle.Fill, AccentTopColor = ThemeColors.Gold, Padding = new Padding(18), AutoScroll = true };
            int y = 10;

            pnlRight.Controls.Add(new Label { Text = "Thông tin thí sinh", Location = new Point(16, y), AutoSize = true, Font = ThemeColors.FontSubTitle, ForeColor = ThemeColors.NavyDark });
            y += 38;

            picAvatar = new PictureBox { Location = new Point(16, y), Size = new Size(100, 120), BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom, BackColor = ThemeColors.AppBackground };
            var btnPickImg = new ModernButton { Text = "Chọn ảnh", Location = new Point(128, y + 10), Size = new Size(110, 32) };
            btnPickImg.Click += (_, _) => ChooseAvatar();
            var btnClearImg = new ModernButton { Text = "Xóa ảnh", Location = new Point(128, y + 50), Size = new Size(110, 32), NormalBackColor = ThemeColors.DangerRed, HoverBackColor = Color.FromArgb(185, 28, 28) };
            btnClearImg.Click += (_, _) => ClearAvatar();
            pnlRight.Controls.AddRange(new Control[] { picAvatar, btnPickImg, btnClearImg });

            y += 132;
            txtMa = AddField(pnlRight, "Mã thí sinh (*):", ref y, 160);
            y -= 56;
            pnlRight.Controls.Add(new Label { Text = "Số báo danh (*):", Location = new Point(195, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            txtSbd = new TextBox { Location = new Point(195, y + 22), Width = 165 };
            pnlRight.Controls.Add(txtSbd);
            y += 56;

            txtHoTen = AddField(pnlRight, "Họ và tên (*):", ref y, 344);

            pnlRight.Controls.Add(new Label { Text = "Lớp học:", Location = new Point(16, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            pnlRight.Controls.Add(new Label { Text = "Giới tính:", Location = new Point(210, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            cboLop = new ComboBox { Location = new Point(16, y + 22), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cboGioiTinh = new ComboBox { Location = new Point(210, y + 22), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cboGioiTinh.Items.AddRange(new object[] { "", "Nam", "Nữ" });
            pnlRight.Controls.AddRange(new Control[] { cboLop, cboGioiTinh });
            y += 58;

            txtTruong = AddField(pnlRight, "Trường:", ref y, 344);
            txtGhiChu = AddField(pnlRight, "Ghi chú:", ref y, 344, true);

            chkTrangThai = new CheckBox { Text = "Đang tham gia / kích hoạt", Checked = true, Location = new Point(16, y), AutoSize = true };
            pnlRight.Controls.Add(chkTrangThai);
            y += 32;

            var btnSave = new ModernButton { Text = "Lưu thí sinh", Location = new Point(16, y), Size = new Size(155, 38) };
            btnSave.Click += (_, _) => SaveItem();
            var btnNew = new ModernButton { Text = "Làm mới / Thêm mới", Location = new Point(182, y), Size = new Size(175, 38), NormalBackColor = ThemeColors.TextMuted, HoverBackColor = ThemeColors.TextPrimary };
            btnNew.Click += (_, _) => ResetForm();
            pnlRight.Controls.AddRange(new Control[] { btnSave, btnNew });

            tlp.Controls.Add(pnlLeft, 0, 0);
            tlp.Controls.Add(pnlRight, 1, 0);
            Controls.Add(tlp);
        }

        private static TextBox AddField(Control parent, string label, ref int y, int width, bool multi = false)
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(16, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            var tb = new TextBox { Location = new Point(16, y + 22), Width = width, Multiline = multi, Height = multi ? 54 : 28 };
            parent.Controls.Add(tb);
            y += multi ? 84 : 56;
            return tb;
        }

        private void LoadDropdowns()
        {
            var filterList = _danhMucRepo.GetLopHoc(onlyActive: true);
            filterList.Insert(0, new LopHoc { LopHocID = 0, TenLop = "Tất cả lớp" });
            cboFilterLop.DataSource = filterList;
            cboFilterLop.DisplayMember = "TenLop";
            cboFilterLop.ValueMember = "LopHocID";

            var formList = _danhMucRepo.GetLopHoc(onlyActive: true);
            formList.Insert(0, new LopHoc { LopHocID = 0, TenLop = "-- Chọn lớp --" });
            cboLop.DataSource = formList;
            cboLop.DisplayMember = "TenLop";
            cboLop.ValueMember = "LopHocID";
        }

        private void LoadData()
        {
            if (cboFilterLop.SelectedValue == null) return;
            int? lopId = cboFilterLop.SelectedValue is int l && l > 0 ? l : null;
            _list = _repo.SearchThiSinh(txtSearch.Text, lopId, true);

            dgv.Rows.Clear();
            for (int i = 0; i < _list.Count; i++)
            {
                var t = _list[i];
                dgv.Rows.Add(i + 1, t.MaThiSinh, t.SoBaoDanh, t.HoTen, t.TenLop, t.TenTruong);
            }
        }

        private void PopulateFromSelected()
        {
            if (dgv.CurrentRow == null || dgv.CurrentRow.Index < 0 || dgv.CurrentRow.Index >= _list.Count) return;
            var t = _list[dgv.CurrentRow.Index];
            _editingId = t.ThiSinhID;
            txtMa.Text = t.MaThiSinh;
            txtSbd.Text = t.SoBaoDanh;
            txtHoTen.Text = t.HoTen;
            cboLop.SelectedValue = t.LopHocID ?? 0;
            txtTruong.Text = t.TenTruong;
            cboGioiTinh.Text = t.GioiTinh;
            txtGhiChu.Text = t.GhiChu;
            chkTrangThai.Checked = t.TrangThai;

            _currentImgPath = t.DuongDanHinhAnh;
            _pendingImgPath = null;
            _imgRemoved = false;
            picAvatar.Image?.Dispose();
            picAvatar.Image = ImageHelper.LoadImageSafe(_currentImgPath);
        }

        private void ResetForm()
        {
            _editingId = 0;
            txtMa.Text = _repo.GenerateNextMaThiSinh();
            txtSbd.Clear();
            txtHoTen.Clear();
            if (cboLop.Items.Count > 0) cboLop.SelectedIndex = 0;
            txtTruong.Clear();
            cboGioiTinh.SelectedIndex = 0;
            txtGhiChu.Clear();
            chkTrangThai.Checked = true;
            _currentImgPath = string.Empty;
            _pendingImgPath = null;
            _imgRemoved = false;
            picAvatar.Image?.Dispose();
            picAvatar.Image = null;
        }

        private void ChooseAvatar()
        {
            using var ofd = new OpenFileDialog { Filter = "Hình ảnh (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                _pendingImgPath = ofd.FileName;
                _imgRemoved = false;
                picAvatar.Image?.Dispose();
                picAvatar.Image = ImageHelper.LoadImageSafe(ofd.FileName);
            }
        }

        private void ClearAvatar()
        {
            _pendingImgPath = null;
            _imgRemoved = true;
            picAvatar.Image?.Dispose();
            picAvatar.Image = null;
        }

        private void SaveItem()
        {
            if (string.IsNullOrWhiteSpace(txtMa.Text) || string.IsNullOrWhiteSpace(txtSbd.Text) || string.IsNullOrWhiteSpace(txtHoTen.Text))
            {
                MessageBox.Show("Vui lòng nhập Mã thí sinh, Số báo danh và Họ tên.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_repo.ExistsMaThiSinh(txtMa.Text.Trim(), _editingId))
            {
                MessageBox.Show("Mã thí sinh đã tồn tại.", "Trùng mã", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string imgPath = _imgRemoved ? string.Empty : _currentImgPath;
            if (!string.IsNullOrWhiteSpace(_pendingImgPath))
            {
                imgPath = ImageHelper.CopyImageToDataFolder(_pendingImgPath, FileHelper.ContestantImagesDirectory, "TS");
            }

            var model = new ThiSinh
            {
                ThiSinhID = _editingId,
                MaThiSinh = txtMa.Text.Trim(),
                SoBaoDanh = txtSbd.Text.Trim(),
                HoTen = txtHoTen.Text.Trim(),
                LopHocID = cboLop.SelectedValue is int id && id > 0 ? id : null,
                TenTruong = txtTruong.Text.Trim(),
                GioiTinh = cboGioiTinh.Text.Trim(),
                DuongDanHinhAnh = imgPath,
                GhiChu = txtGhiChu.Text.Trim(),
                TrangThai = chkTrangThai.Checked
            };

            if (_editingId == 0) _repo.AddThiSinh(model);
            else _repo.UpdateThiSinh(model);

            ResetForm();
            LoadData();
        }

        private void DeleteSelected()
        {
            if (dgv.CurrentRow == null || dgv.CurrentRow.Index < 0 || dgv.CurrentRow.Index >= _list.Count) return;
            var t = _list[dgv.CurrentRow.Index];
            if (MessageBox.Show($"Xác nhận xóa thí sinh '{t.HoTen}'?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            _repo.DeleteThiSinh(t.ThiSinhID, out string msg);
            MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ResetForm();
            LoadData();
        }

        private void DownloadTemplate()
        {
            using var sfd = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = "Mau_Nhap_Thi_Sinh.xlsx" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                _excelService.CreateContestantTemplate(sfd.FileName);
                MessageBox.Show("Đã tạo file Excel mẫu thí sinh.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ImportExcel()
        {
            using var ofd = new OpenFileDialog { Filter = "Excel (*.xlsx)|*.xlsx" };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            var summary = _excelService.ImportContestantsFromExcel(ofd.FileName);
            MessageBox.Show(
                $"Thêm mới: {summary.Inserted} | Bỏ qua: {summary.Skipped} | Lỗi: {summary.Failed}" +
                (summary.RowMessages.Count > 0 ? "\n\n" + string.Join("\n", summary.RowMessages.GetRange(0, Math.Min(8, summary.RowMessages.Count))) : ""),
                "Kết quả nhập thí sinh",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            LoadData();
        }

        private void ExportExcel()
        {
            using var sfd = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = $"DanhSachThiSinh_{DateTime.Now:yyyyMMdd}.xlsx" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                _excelService.ExportContestantsToExcel(_list, sfd.FileName);
                MessageBox.Show("Đã xuất danh sách thí sinh ra Excel.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
