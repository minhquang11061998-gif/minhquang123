using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RungChuongVang.Controls;
using RungChuongVang.DataAccess;
using RungChuongVang.Helpers;
using RungChuongVang.Models;
using RungChuongVang.Services;

namespace RungChuongVang.Forms
{
    /// <summary>
    /// Quản lý Đội thi và Thành viên các đội (thêm/xóa thành viên, chuyển đội, gán đội trưởng, Excel).
    /// </summary>
    public class FrmQuanLyDoiThi : Form
    {
        private readonly DoiThiRepository _repo = new();
        private readonly ThiSinhRepository _thiSinhRepo = new();
        private readonly ExcelService _excelService = new();

        private List<DoiThi> _teams = new();
        private List<ThiSinh> _allStudents = new();
        private DoiThi? _selectedTeam;
        private int _editingTeamId;
        private string _currentImg = string.Empty;
        private string? _pendingImg;

        private TextBox txtSearch = null!;
        private StyledDataGridView dgvTeams = null!;
        private TextBox txtMaDoi = null!;
        private TextBox txtTenDoi = null!;
        private TextBox txtTenLop = null!;
        private TextBox txtTenTruong = null!;
        private TextBox txtGhiChu = null!;
        private PictureBox picTeam = null!;

        private Label lblMembersTitle = null!;
        private StyledDataGridView dgvMembers = null!;
        private ComboBox cboCandidateStudent = null!;
        private ComboBox cboTargetTransferTeam = null!;

        public FrmQuanLyDoiThi()
        {
            BuildUi();
            LoadStudentsList();
            LoadTeams();
            ResetTeamForm();
        }

        private void BuildUi()
        {
            BackColor = ThemeColors.AppBackground;
            Padding = new Padding(20);
            Font = ThemeColors.FontBody;

            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));

            // ===== TRÁI: DANH SÁCH & FORM ĐỘI THI =====
            var pnlLeft = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0), Padding = new Padding(14) };

            var pnlTopBar = new Panel { Dock = DockStyle.Top, Height = 46 };
            txtSearch = new TextBox { Location = new Point(0, 8), Width = 150, PlaceholderText = "Tìm mã, tên đội, lớp..." };
            txtSearch.TextChanged += (_, _) => LoadTeams();

            var btnTpl = new ModernButton { Text = "Mẫu Excel", Location = new Point(156, 5), Size = new Size(92, 34), NormalBackColor = ThemeColors.TextMuted, HoverBackColor = ThemeColors.TextPrimary };
            btnTpl.Click += (_, _) => DownloadTeamTemplate();
            var btnImp = new ModernButton { Text = "Nhập Excel", Location = new Point(254, 5), Size = new Size(96, 34), NormalBackColor = ThemeColors.SuccessGreen, HoverBackColor = Color.FromArgb(16, 130, 58) };
            btnImp.Click += (_, _) => ImportTeams();
            var btnExp = new ModernButton { Text = "Xuất Excel", Location = new Point(356, 5), Size = new Size(96, 34), NormalBackColor = ThemeColors.Gold, HoverBackColor = ThemeColors.GoldBright, ForeColor = ThemeColors.NavyDark };
            btnExp.Click += (_, _) => ExportTeams();
            var btnDel = new ModernButton { Text = "Xóa đội", Location = new Point(458, 5), Size = new Size(80, 34), NormalBackColor = ThemeColors.DangerRed, HoverBackColor = Color.FromArgb(185, 28, 28) };
            btnDel.Click += (_, _) => DeleteSelectedTeam();
            pnlTopBar.Controls.AddRange(new Control[] { txtSearch, btnTpl, btnImp, btnExp, btnDel });

            dgvTeams = new StyledDataGridView { Dock = DockStyle.Fill };
            dgvTeams.Columns.Add(new DataGridViewTextBoxColumn { Name = "MaDoi", HeaderText = "Mã đội", FillWeight = 18 });
            dgvTeams.Columns.Add(new DataGridViewTextBoxColumn { Name = "TenDoi", HeaderText = "Tên đội", FillWeight = 32 });
            dgvTeams.Columns.Add(new DataGridViewTextBoxColumn { Name = "Lop", HeaderText = "Lớp", FillWeight = 16 });
            dgvTeams.Columns.Add(new DataGridViewTextBoxColumn { Name = "DoiTruong", HeaderText = "Đội trưởng", FillWeight = 22 });
            dgvTeams.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoTV", HeaderText = "TV", FillWeight = 12 });
            dgvTeams.SelectionChanged += (_, _) => SelectTeamFromGrid();

            var pnlTeamForm = new Panel { Dock = DockStyle.Bottom, Height = 235, Padding = new Padding(0, 10, 0, 0) };
            pnlTeamForm.Controls.Add(new Label { Text = "Mã đội (*):", Location = new Point(0, 6), AutoSize = true, Font = ThemeColors.FontBodyBold });
            txtMaDoi = new TextBox { Location = new Point(0, 26), Width = 140 };
            pnlTeamForm.Controls.Add(new Label { Text = "Tên đội (*):", Location = new Point(155, 6), AutoSize = true, Font = ThemeColors.FontBodyBold });
            txtTenDoi = new TextBox { Location = new Point(155, 26), Width = 235 };

            pnlTeamForm.Controls.Add(new Label { Text = "Lớp:", Location = new Point(0, 60), AutoSize = true, Font = ThemeColors.FontBodyBold });
            txtTenLop = new TextBox { Location = new Point(0, 80), Width = 140 };
            pnlTeamForm.Controls.Add(new Label { Text = "Trường:", Location = new Point(155, 60), AutoSize = true, Font = ThemeColors.FontBodyBold });
            txtTenTruong = new TextBox { Location = new Point(155, 80), Width = 235 };

            pnlTeamForm.Controls.Add(new Label { Text = "Ghi chú:", Location = new Point(0, 114), AutoSize = true, Font = ThemeColors.FontBodyBold });
            txtGhiChu = new TextBox { Location = new Point(0, 134), Width = 390, Height = 46, Multiline = true };

            picTeam = new PictureBox { Location = new Point(405, 26), Size = new Size(125, 110), BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom, BackColor = ThemeColors.AppBackground };
            var btnPickTeamImg = new ModernButton { Text = "Ảnh đội...", Location = new Point(405, 144), Size = new Size(125, 32) };
            btnPickTeamImg.Click += (_, _) => ChooseTeamImage();

            var btnSaveTeam = new ModernButton { Text = "Lưu đội thi", Location = new Point(0, 190), Size = new Size(140, 36) };
            btnSaveTeam.Click += (_, _) => SaveTeam();
            var btnNewTeam = new ModernButton { Text = "Thêm đội mới", Location = new Point(150, 190), Size = new Size(140, 36), NormalBackColor = ThemeColors.TextMuted, HoverBackColor = ThemeColors.TextPrimary };
            btnNewTeam.Click += (_, _) => ResetTeamForm();

            pnlTeamForm.Controls.AddRange(new Control[] { txtMaDoi, txtTenDoi, txtTenLop, txtTenTruong, txtGhiChu, picTeam, btnPickTeamImg, btnSaveTeam, btnNewTeam });

            pnlLeft.Controls.Add(dgvTeams);
            pnlLeft.Controls.Add(pnlTeamForm);
            pnlLeft.Controls.Add(pnlTopBar);

            // ===== PHẢI: QUẢN LÝ THÀNH VIÊN ĐỘI =====
            var pnlRight = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(10, 0, 0, 0), Padding = new Padding(14), AccentTopColor = ThemeColors.Gold };
            lblMembersTitle = new Label { Dock = DockStyle.Top, Height = 34, Font = ThemeColors.FontSubTitle, ForeColor = ThemeColors.NavyDark, Text = "Thành viên của đội: (Chưa chọn)" };

            var pnlAddMember = new Panel { Dock = DockStyle.Top, Height = 50 };
            cboCandidateStudent = new ComboBox { Location = new Point(0, 8), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
            var btnAddMem = new ModernButton { Text = "+ Thêm vào đội", Location = new Point(330, 5), Size = new Size(140, 34) };
            btnAddMem.Click += (_, _) => AddSelectedMember();
            pnlAddMember.Controls.AddRange(new Control[] { cboCandidateStudent, btnAddMem });

            dgvMembers = new StyledDataGridView { Dock = DockStyle.Fill };
            dgvMembers.Columns.Add(new DataGridViewTextBoxColumn { Name = "SBD", HeaderText = "SBD", FillWeight = 16 });
            dgvMembers.Columns.Add(new DataGridViewTextBoxColumn { Name = "HoTen", HeaderText = "Họ và tên", FillWeight = 40 });
            dgvMembers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Lop", HeaderText = "Lớp", FillWeight = 18 });
            dgvMembers.Columns.Add(new DataGridViewTextBoxColumn { Name = "VaiTro", HeaderText = "Vai trò", FillWeight = 26 });

            var pnlMemberActions = new Panel { Dock = DockStyle.Bottom, Height = 96, Padding = new Padding(0, 10, 0, 0) };
            var btnCaptain = new ModernButton { Text = "Đặt làm Đội trưởng", Location = new Point(0, 6), Size = new Size(165, 34), NormalBackColor = ThemeColors.Gold, HoverBackColor = ThemeColors.GoldBright, ForeColor = ThemeColors.NavyDark };
            btnCaptain.Click += (_, _) => SetCaptain();
            var btnRemoveMem = new ModernButton { Text = "Xóa khỏi đội", Location = new Point(175, 6), Size = new Size(130, 34), NormalBackColor = ThemeColors.DangerRed, HoverBackColor = Color.FromArgb(185, 28, 28) };
            btnRemoveMem.Click += (_, _) => RemoveMember();

            pnlMemberActions.Controls.Add(new Label { Text = "Chuyển sang đội:", Location = new Point(0, 52), AutoSize = true, Font = ThemeColors.FontBodyBold });
            cboTargetTransferTeam = new ComboBox { Location = new Point(125, 48), Width = 215, DropDownStyle = ComboBoxStyle.DropDownList };
            var btnTransfer = new ModernButton { Text = "Chuyển đội", Location = new Point(350, 45), Size = new Size(120, 34), NormalBackColor = ThemeColors.RoyalBlue, HoverBackColor = ThemeColors.NavyDark };
            btnTransfer.Click += (_, _) => TransferMember();
            pnlMemberActions.Controls.AddRange(new Control[] { btnCaptain, btnRemoveMem, cboTargetTransferTeam, btnTransfer });

            pnlRight.Controls.Add(dgvMembers);
            pnlRight.Controls.Add(pnlMemberActions);
            pnlRight.Controls.Add(pnlAddMember);
            pnlRight.Controls.Add(lblMembersTitle);

            tlp.Controls.Add(pnlLeft, 0, 0);
            tlp.Controls.Add(pnlRight, 1, 0);
            Controls.Add(tlp);
        }

        private void LoadStudentsList()
        {
            _allStudents = _thiSinhRepo.SearchThiSinh(onlyActive: true);
            cboCandidateStudent.DataSource = _allStudents.Select(s => new StudentChoice(s.ThiSinhID, $"{s.SoBaoDanh} - {s.HoTen} ({s.TenLop})")).ToList();
            cboCandidateStudent.DisplayMember = "Display";
            cboCandidateStudent.ValueMember = "ThiSinhID";
        }

        private void LoadTeams()
        {
            _teams = _repo.SearchDoiThi(txtSearch.Text);
            dgvTeams.Rows.Clear();
            foreach (var t in _teams)
            {
                dgvTeams.Rows.Add(t.MaDoi, t.TenDoi, t.TenLop, t.TenDoiTruong, t.ThanhVien.Count);
            }

            cboTargetTransferTeam.DataSource = new List<DoiThi>(_teams);
            cboTargetTransferTeam.DisplayMember = "TenDoi";
            cboTargetTransferTeam.ValueMember = "DoiThiID";
        }

        private void SelectTeamFromGrid()
        {
            if (dgvTeams.CurrentRow == null || dgvTeams.CurrentRow.Index < 0 || dgvTeams.CurrentRow.Index >= _teams.Count) return;
            _selectedTeam = _teams[dgvTeams.CurrentRow.Index];
            _editingTeamId = _selectedTeam.DoiThiID;

            txtMaDoi.Text = _selectedTeam.MaDoi;
            txtTenDoi.Text = _selectedTeam.TenDoi;
            txtTenLop.Text = _selectedTeam.TenLop;
            txtTenTruong.Text = _selectedTeam.TenTruong;
            txtGhiChu.Text = _selectedTeam.GhiChu;
            _currentImg = _selectedTeam.DuongDanHinhAnh;
            _pendingImg = null;
            picTeam.Image?.Dispose();
            picTeam.Image = ImageHelper.LoadImageSafe(_currentImg);

            RefreshMembersGrid();
        }

        private void RefreshMembersGrid()
        {
            dgvMembers.Rows.Clear();
            if (_selectedTeam == null)
            {
                lblMembersTitle.Text = "Thành viên của đội: (Chưa chọn)";
                return;
            }

            _selectedTeam.ThanhVien = _repo.GetThanhVienByDoi(_selectedTeam.DoiThiID);
            lblMembersTitle.Text = $"Thành viên đội: {_selectedTeam.TenDoi} ({_selectedTeam.ThanhVien.Count} người)";
            foreach (var m in _selectedTeam.ThanhVien)
            {
                dgvMembers.Rows.Add(
                    m.SoBaoDanh,
                    m.HoTenThiSinh,
                    m.TenLop,
                    m.VaiTro == "DoiTruong" ? "Đội trưởng" : "Thành viên");
            }
        }

        private void ResetTeamForm()
        {
            _editingTeamId = 0;
            _selectedTeam = null;
            txtMaDoi.Text = _repo.GenerateNextMaDoi();
            txtTenDoi.Clear();
            txtTenLop.Clear();
            txtTenTruong.Clear();
            txtGhiChu.Clear();
            _currentImg = string.Empty;
            _pendingImg = null;
            picTeam.Image?.Dispose();
            picTeam.Image = null;
            RefreshMembersGrid();
        }

        private void ChooseTeamImage()
        {
            using var ofd = new OpenFileDialog { Filter = "Hình ảnh (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                _pendingImg = ofd.FileName;
                picTeam.Image?.Dispose();
                picTeam.Image = ImageHelper.LoadImageSafe(ofd.FileName);
            }
        }

        private void SaveTeam()
        {
            if (string.IsNullOrWhiteSpace(txtMaDoi.Text) || string.IsNullOrWhiteSpace(txtTenDoi.Text))
            {
                MessageBox.Show("Vui lòng nhập Mã đội và Tên đội.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_repo.ExistsMaDoi(txtMaDoi.Text.Trim(), _editingTeamId))
            {
                MessageBox.Show("Mã đội đã tồn tại.", "Trùng mã", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string img = _currentImg;
            if (!string.IsNullOrWhiteSpace(_pendingImg))
            {
                img = ImageHelper.CopyImageToDataFolder(_pendingImg, FileHelper.TeamImagesDirectory, "TEAM");
            }

            var team = new DoiThi
            {
                DoiThiID = _editingTeamId,
                MaDoi = txtMaDoi.Text.Trim(),
                TenDoi = txtTenDoi.Text.Trim(),
                TenLop = txtTenLop.Text.Trim(),
                TenTruong = txtTenTruong.Text.Trim(),
                TenDoiTruong = _selectedTeam?.TenDoiTruong ?? string.Empty,
                DuongDanHinhAnh = img,
                GhiChu = txtGhiChu.Text.Trim()
            };

            if (_editingTeamId == 0) _repo.AddDoiThi(team);
            else _repo.UpdateDoiThi(team);

            LoadTeams();
            ResetTeamForm();
        }

        private void DeleteSelectedTeam()
        {
            if (_selectedTeam == null) return;
            if (MessageBox.Show($"Xác nhận xóa đội '{_selectedTeam.TenDoi}'?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            _repo.DeleteDoiThi(_selectedTeam.DoiThiID, out string msg);
            MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadTeams();
            ResetTeamForm();
        }

        private void AddSelectedMember()
        {
            if (_selectedTeam == null || cboCandidateStudent.SelectedValue is not int studentId)
            {
                MessageBox.Show("Vui lòng chọn một đội ở bảng bên trái trước khi thêm thành viên.", "Chưa chọn đội", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string role = _selectedTeam.ThanhVien.Count == 0 ? "DoiTruong" : "ThanhVien";
                _repo.AddMemberToTeam(_selectedTeam.DoiThiID, studentId, role);
                LoadTeams();
                RefreshMembersGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không thể thêm thành viên", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private ThanhVienDoi? GetSelectedMember()
        {
            if (_selectedTeam == null || dgvMembers.CurrentRow == null || dgvMembers.CurrentRow.Index < 0 || dgvMembers.CurrentRow.Index >= _selectedTeam.ThanhVien.Count)
                return null;
            return _selectedTeam.ThanhVien[dgvMembers.CurrentRow.Index];
        }

        private void SetCaptain()
        {
            var mem = GetSelectedMember();
            if (_selectedTeam == null || mem == null) return;
            _repo.SetTeamCaptain(_selectedTeam.DoiThiID, mem.ThiSinhID);
            LoadTeams();
            RefreshMembersGrid();
        }

        private void RemoveMember()
        {
            var mem = GetSelectedMember();
            if (_selectedTeam == null || mem == null) return;
            _repo.RemoveMemberFromTeam(_selectedTeam.DoiThiID, mem.ThiSinhID);
            LoadTeams();
            RefreshMembersGrid();
        }

        private void TransferMember()
        {
            var mem = GetSelectedMember();
            if (_selectedTeam == null || mem == null || cboTargetTransferTeam.SelectedValue is not int targetTeamId) return;

            try
            {
                _repo.TransferMember(mem.ThiSinhID, _selectedTeam.DoiThiID, targetTeamId);
                LoadTeams();
                RefreshMembersGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không thể chuyển đội", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DownloadTeamTemplate()
        {
            using var sfd = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = "Mau_Nhap_Doi_Thi.xlsx" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                _excelService.CreateTeamTemplate(sfd.FileName);
                MessageBox.Show("Đã tạo file Excel mẫu đội thi.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ImportTeams()
        {
            using var ofd = new OpenFileDialog { Filter = "Excel (*.xlsx)|*.xlsx" };
            if (ofd.ShowDialog() != DialogResult.OK) return;
            var summary = _excelService.ImportTeamsFromExcel(ofd.FileName);
            MessageBox.Show($"Thêm mới: {summary.Inserted} | Bỏ qua: {summary.Skipped} | Lỗi: {summary.Failed}", "Nhập đội thi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadTeams();
        }

        private void ExportTeams()
        {
            using var sfd = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = $"DanhSachDoiThi_{DateTime.Now:yyyyMMdd}.xlsx" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                _excelService.ExportTeamsToExcel(_teams, sfd.FileName);
                MessageBox.Show("Đã xuất danh sách đội thi ra Excel.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private sealed class StudentChoice
        {
            public int ThiSinhID { get; }
            public string Display { get; }
            public StudentChoice(int id, string display) { ThiSinhID = id; Display = display; }
        }
    }
}
