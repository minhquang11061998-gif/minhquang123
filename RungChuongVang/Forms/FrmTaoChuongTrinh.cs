using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RungChuongVang.Controls;
using RungChuongVang.DataAccess;
using RungChuongVang.Enums;
using RungChuongVang.Helpers;
using RungChuongVang.Models;

namespace RungChuongVang.Forms
{
    /// <summary>
    /// Form Tạo hoặc Chỉnh sửa Chương trình thi và chọn danh sách Thí sinh / Đội thi tham gia.
    /// </summary>
    public class FrmTaoChuongTrinh : Form
    {
        private readonly ChuongTrinhRepository _repo = new();
        private readonly DanhMucRepository _danhMucRepo = new();
        private readonly ThiSinhRepository _thiSinhRepo = new();
        private readonly DoiThiRepository _doiThiRepo = new();
        private readonly int _chuongTrinhId;

        private TextBox txtMa = null!;
        private TextBox txtTen = null!;
        private DateTimePicker dtpNgay = null!;
        private TextBox txtDonVi = null!;
        private TextBox txtDiaDiem = null!;
        private TextBox txtMoTa = null!;
        private ComboBox cboPhuongThuc = null!;
        private ComboBox cboQuyTacDoi = null!;
        private NumericUpDown numThoiGian = null!;
        private NumericUpDown numSoLanCuuTro = null!;
        private ComboBox cboQuyTacHetCau = null!;

        private ComboBox cboLocLop = null!;
        private CheckedListBox clbParticipants = null!;
        private Label lblSelectedCount = null!;

        private HashSet<int> _preselectedIds = new();
        private bool _isLoading;

        public int SavedProgramId { get; private set; }

        public FrmTaoChuongTrinh(int chuongTrinhId = 0)
        {
            _chuongTrinhId = chuongTrinhId;
            BuildUi();
            LoadReferenceDropdowns();

            if (_chuongTrinhId > 0)
            {
                LoadExisting(_chuongTrinhId);
            }
            else
            {
                txtMa.Text = _repo.GenerateNextMaChuongTrinh();
                RefreshParticipantChecklist();
            }
        }

        private void BuildUi()
        {
            Text = _chuongTrinhId > 0 ? "Cập nhật Chương trình thi" : "Tạo Chương trình thi mới";
            Size = new Size(1060, 720);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = ThemeColors.AppBackground;
            Font = ThemeColors.FontBody;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(16) };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));

            // ===== TRÁI: THÔNG TIN & CẤU HÌNH CHƯƠNG TRÌNH =====
            var pnlLeft = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0), Padding = new Padding(16), AutoScroll = true };
            int y = 8;

            pnlLeft.Controls.Add(new Label { Text = "Hình thức thi:", Location = new Point(16, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            var lblHinhThuc = new Label
            {
                Text = "RUNG CHUÔNG VÀNG TRUYỀN THỐNG (TRẢ LỜI SAI BỊ LOẠI)",
                Location = new Point(16, y + 22),
                Width = 465,
                Height = 28,
                BackColor = ThemeColors.GoldSoft,
                ForeColor = ThemeColors.NavyDark,
                Font = ThemeColors.FontBodyBold,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
            pnlLeft.Controls.Add(lblHinhThuc);
            y += 60;

            pnlLeft.Controls.Add(new Label { Text = "Mã chương trình (*):", Location = new Point(16, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            pnlLeft.Controls.Add(new Label { Text = "Ngày tổ chức:", Location = new Point(210, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            txtMa = new TextBox { Location = new Point(16, y + 22), Width = 180 };
            dtpNgay = new DateTimePicker { Location = new Point(210, y + 22), Width = 270, Format = DateTimePickerFormat.Short };
            pnlLeft.Controls.AddRange(new Control[] { txtMa, dtpNgay });
            y += 58;

            txtTen = AddBox(pnlLeft, "Tên chương trình (*):", ref y, 464);
            txtDonVi = AddBox(pnlLeft, "Đơn vị tổ chức:", ref y, 464);
            txtDiaDiem = AddBox(pnlLeft, "Địa điểm:", ref y, 464);

            pnlLeft.Controls.Add(new Label { Text = "Phương thức tổ chức (*):", Location = new Point(16, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            pnlLeft.Controls.Add(new Label { Text = "Quy tắc trả lời đội:", Location = new Point(240, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            cboPhuongThuc = new ComboBox { Location = new Point(16, y + 22), Width = 210, DropDownStyle = ComboBoxStyle.DropDownList };
            cboPhuongThuc.SelectedIndexChanged += (_, _) =>
            {
                if (!_isLoading)
                {
                    _preselectedIds.Clear();
                    ToggleTeamRuleState();
                    RefreshParticipantChecklist();
                }
            };
            cboQuyTacDoi = new ComboBox { Location = new Point(240, y + 22), Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
            pnlLeft.Controls.AddRange(new Control[] { cboPhuongThuc, cboQuyTacDoi });
            y += 60;

            pnlLeft.Controls.Add(new Label { Text = "Thời gian mặc định (s):", Location = new Point(16, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            pnlLeft.Controls.Add(new Label { Text = "Số lần cứu trợ tối đa:", Location = new Point(210, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            numThoiGian = new NumericUpDown { Location = new Point(16, y + 22), Width = 175, Minimum = 5, Maximum = 300, Value = 15 };
            numSoLanCuuTro = new NumericUpDown { Location = new Point(210, y + 22), Width = 175, Minimum = 0, Maximum = 10, Value = 1 };
            pnlLeft.Controls.AddRange(new Control[] { numThoiGian, numSoLanCuuTro });
            y += 58;

            pnlLeft.Controls.Add(new Label { Text = "Quy tắc khi hết câu hỏi mà còn nhiều thí sinh/đội:", Location = new Point(16, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            cboQuyTacHetCau = new ComboBox { Location = new Point(16, y + 22), Width = 464, DropDownStyle = ComboBoxStyle.DropDownList };
            pnlLeft.Controls.Add(cboQuyTacHetCau);
            y += 58;

            txtMoTa = AddBox(pnlLeft, "Mô tả chương trình:", ref y, 464, multi: true);

            // ===== PHẢI: CHỌN THÍ SINH / ĐỘI THAM GIA =====
            var pnlRight = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0), Padding = new Padding(16), AccentTopColor = ThemeColors.Gold };

            var lblPartTitle = new Label { Dock = DockStyle.Top, Height = 30, Font = ThemeColors.FontSubTitle, ForeColor = ThemeColors.NavyDark, Text = "Chọn Thí sinh / Đội tham gia" };

            var pnlPartTools = new Panel { Dock = DockStyle.Top, Height = 44 };
            cboLocLop = new ComboBox { Location = new Point(0, 8), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cboLocLop.SelectedIndexChanged += (_, _) => { if (!_isLoading) RefreshParticipantChecklist(); };

            var btnAll = new ModernButton { Text = "Chọn hết", Location = new Point(190, 6), Size = new Size(95, 32), NormalBackColor = ThemeColors.RoyalBlue, HoverBackColor = ThemeColors.NavyDark };
            btnAll.Click += (_, _) => SetAllParticipantsChecked(true);
            var btnNone = new ModernButton { Text = "Bỏ chọn", Location = new Point(292, 6), Size = new Size(95, 32), NormalBackColor = ThemeColors.TextMuted, HoverBackColor = ThemeColors.TextPrimary };
            btnNone.Click += (_, _) => SetAllParticipantsChecked(false);
            pnlPartTools.Controls.AddRange(new Control[] { cboLocLop, btnAll, btnNone });

            clbParticipants = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true };
            clbParticipants.ItemCheck += (_, e) =>
            {
                if (clbParticipants.Items[e.Index] is ParticipantItem pi)
                {
                    if (e.NewValue == CheckState.Checked) _preselectedIds.Add(pi.Id);
                    else _preselectedIds.Remove(pi.Id);
                    BeginInvoke(UpdateSelectedCountLabel);
                }
            };

            lblSelectedCount = new Label { Dock = DockStyle.Bottom, Height = 28, Font = ThemeColors.FontBodyBold, ForeColor = ThemeColors.PrimaryBlue, TextAlign = ContentAlignment.MiddleLeft };

            pnlRight.Controls.Add(clbParticipants);
            pnlRight.Controls.Add(lblSelectedCount);
            pnlRight.Controls.Add(pnlPartTools);
            pnlRight.Controls.Add(lblPartTitle);

            // ===== HÀNG NÚT DƯỚI =====
            var pnlBottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
            var btnClose = new ModernButton { Text = "Hủy", Size = new Size(105, 38), NormalBackColor = ThemeColors.TextMuted, HoverBackColor = ThemeColors.TextPrimary };
            btnClose.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnSave = new ModernButton { Text = "Lưu chương trình", Size = new Size(170, 38), Margin = new Padding(0, 0, 10, 0) };
            btnSave.Click += (_, _) => SaveProgram();

            pnlBottom.Controls.Add(btnClose);
            pnlBottom.Controls.Add(btnSave);

            tlp.Controls.Add(pnlLeft, 0, 0);
            tlp.Controls.Add(pnlRight, 1, 0);
            tlp.Controls.Add(pnlBottom, 0, 1);
            tlp.SetColumnSpan(pnlBottom, 2);
            Controls.Add(tlp);
        }

        private static TextBox AddBox(Control parent, string label, ref int y, int width, bool multi = false)
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(16, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            var tb = new TextBox { Location = new Point(16, y + 22), Width = width, Multiline = multi, Height = multi ? 54 : 28 };
            parent.Controls.Add(tb);
            y += multi ? 84 : 56;
            return tb;
        }

        private void LoadReferenceDropdowns()
        {
            _isLoading = true;
            cboPhuongThuc.DataSource = new List<KeyValuePair<PhuongThucThi, string>>
            {
                new(PhuongThucThi.ThiCaNhan, "Phương thức A: Thi cá nhân"),
                new(PhuongThucThi.ThiTheoDoi, "Phương thức B: Thi theo đội / lớp")
            };
            cboPhuongThuc.DisplayMember = "Value";
            cboPhuongThuc.ValueMember = "Key";

            cboQuyTacDoi.DataSource = new List<KeyValuePair<QuyTacTraLoiDoi, string>>
            {
                new(QuyTacTraLoiDoi.DoiTruongQuyetDinh, "Đội trưởng quyết định (Mặc định)"),
                new(QuyTacTraLoiDoi.DoiThongNhatDapAn, "Đội thống nhất đáp án"),
                new(QuyTacTraLoiDoi.TatCaThanhVienPhaiDung, "Tất cả thành viên phải trả lời đúng")
            };
            cboQuyTacDoi.DisplayMember = "Value";
            cboQuyTacDoi.ValueMember = "Key";

            cboQuyTacHetCau.DataSource = new List<KeyValuePair<int, string>>
            {
                new(1, "Xét số câu đúng, nếu bằng nhau thi câu hỏi phụ"),
                new(2, "Hiển thị thống kê để MC xác nhận đồng chiến thắng")
            };
            cboQuyTacHetCau.DisplayMember = "Value";
            cboQuyTacHetCau.ValueMember = "Key";

            var classes = _danhMucRepo.GetLopHoc(onlyActive: true);
            classes.Insert(0, new LopHoc { LopHocID = 0, TenLop = "Tất cả lớp" });
            cboLocLop.DataSource = classes;
            cboLocLop.DisplayMember = "TenLop";
            cboLocLop.ValueMember = "LopHocID";

            _isLoading = false;
            ToggleTeamRuleState();
        }

        private void ToggleTeamRuleState()
        {
            bool isTeam = cboPhuongThuc.SelectedValue is PhuongThucThi pt && pt == PhuongThucThi.ThiTheoDoi;
            cboQuyTacDoi.Enabled = isTeam;
            cboLocLop.Enabled = !isTeam;
        }

        private void LoadExisting(int id)
        {
            var ct = _repo.GetById(id);
            if (ct == null) return;

            _isLoading = true;
            txtMa.Text = ct.MaChuongTrinh;
            txtTen.Text = ct.TenChuongTrinh;
            dtpNgay.Value = ct.NgayToChuc;
            txtDonVi.Text = ct.DonViToChuc;
            txtDiaDiem.Text = ct.DiaDiem;
            txtMoTa.Text = ct.MoTa;
            cboPhuongThuc.SelectedValue = ct.PhuongThucThi;
            cboQuyTacDoi.SelectedValue = ct.QuyTacTraLoiDoi;
            numThoiGian.Value = Math.Clamp(ct.ThoiGianMacDinh, 5, 300);
            numSoLanCuuTro.Value = Math.Clamp(ct.SoLanCuuTroToiDa, 0, 10);
            cboQuyTacHetCau.SelectedValue = ct.QuyTacHetCauHoi;

            // Khóa đổi phương thức thi nếu chương trình đã bắt đầu
            cboPhuongThuc.Enabled = ct.TrangThai == TrangThaiChuongTrinhEnum.ChuaBatDau;

            var participants = _repo.GetParticipants(id, ct.PhuongThucThi);
            _preselectedIds = participants
                .Select(p => ct.PhuongThucThi == PhuongThucThi.ThiCaNhan ? (p.ThiSinhID ?? 0) : (p.DoiThiID ?? 0))
                .Where(x => x > 0)
                .ToHashSet();

            _isLoading = false;
            ToggleTeamRuleState();
            RefreshParticipantChecklist();
        }

        private void RefreshParticipantChecklist()
        {
            if (cboPhuongThuc.SelectedValue is not PhuongThucThi mode) return;

            clbParticipants.Items.Clear();
            if (mode == PhuongThucThi.ThiCaNhan)
            {
                int? lopId = cboLocLop.SelectedValue is int id && id > 0 ? id : null;
                var students = _thiSinhRepo.SearchThiSinh(lopHocId: lopId, onlyActive: true);
                foreach (var s in students)
                {
                    var item = new ParticipantItem(s.ThiSinhID, $"{s.SoBaoDanh} - {s.HoTen} ({s.TenLop})");
                    clbParticipants.Items.Add(item, _preselectedIds.Contains(s.ThiSinhID));
                }
            }
            else
            {
                var teams = _doiThiRepo.SearchDoiThi();
                foreach (var t in teams)
                {
                    var item = new ParticipantItem(t.DoiThiID, $"{t.MaDoi} - {t.TenDoi} ({t.ThanhVien.Count} TV - {t.TenLop})");
                    clbParticipants.Items.Add(item, _preselectedIds.Contains(t.DoiThiID));
                }
            }

            UpdateSelectedCountLabel();
        }

        private void SetAllParticipantsChecked(bool state)
        {
            for (int i = 0; i < clbParticipants.Items.Count; i++)
            {
                clbParticipants.SetItemChecked(i, state);
            }
            UpdateSelectedCountLabel();
        }

        private void UpdateSelectedCountLabel()
        {
            bool isTeam = cboPhuongThuc.SelectedValue is PhuongThucThi pt && pt == PhuongThucThi.ThiTheoDoi;
            lblSelectedCount.Text = isTeam
                ? $"Đã chọn: {_preselectedIds.Count} đội thi"
                : $"Đã chọn: {_preselectedIds.Count} thí sinh";
        }

        private void SaveProgram()
        {
            if (string.IsNullOrWhiteSpace(txtMa.Text) || string.IsNullOrWhiteSpace(txtTen.Text))
            {
                MessageBox.Show("Vui lòng nhập Mã chương trình và Tên chương trình.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_repo.ExistsMaChuongTrinh(txtMa.Text.Trim(), _chuongTrinhId))
            {
                MessageBox.Show("Mã chương trình đã tồn tại.", "Trùng mã", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var mode = (PhuongThucThi)cboPhuongThuc.SelectedValue!;
            if (_preselectedIds.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một Thí sinh hoặc Đội thi tham gia chương trình.", "Chưa chọn đối tượng thi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                ChuongTrinh? existing = _chuongTrinhId > 0 ? _repo.GetById(_chuongTrinhId) : null;
                var ct = existing ?? new ChuongTrinh();

                ct.MaChuongTrinh = txtMa.Text.Trim();
                ct.TenChuongTrinh = txtTen.Text.Trim();
                ct.NgayToChuc = dtpNgay.Value.Date;
                ct.DonViToChuc = txtDonVi.Text.Trim();
                ct.DiaDiem = txtDiaDiem.Text.Trim();
                ct.MoTa = txtMoTa.Text.Trim();
                ct.PhuongThucThi = mode;
                ct.QuyTacTraLoiDoi = (QuyTacTraLoiDoi)cboQuyTacDoi.SelectedValue!;
                ct.ThoiGianMacDinh = (int)numThoiGian.Value;
                ct.SoLanCuuTroToiDa = (int)numSoLanCuuTro.Value;
                ct.QuyTacHetCauHoi = (int)cboQuyTacHetCau.SelectedValue!;

                if (_chuongTrinhId == 0)
                {
                    SavedProgramId = _repo.AddProgram(ct);
                }
                else
                {
                    _repo.UpdateProgram(ct);
                    SavedProgramId = ct.ChuongTrinhID;
                }

                if (ct.TrangThai == TrangThaiChuongTrinhEnum.ChuaBatDau)
                {
                    _repo.SaveParticipants(SavedProgramId, mode, _preselectedIds.ToList());
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể lưu chương trình:\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private sealed class ParticipantItem
        {
            public int Id { get; }
            public string Display { get; }
            public ParticipantItem(int id, string display) { Id = id; Display = display; }
            public override string ToString() => Display;
        }
    }
}
