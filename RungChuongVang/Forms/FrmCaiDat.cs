using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RungChuongVang.Controls;
using RungChuongVang.DataAccess;
using RungChuongVang.Enums;
using RungChuongVang.Helpers;
using RungChuongVang.Services;

namespace RungChuongVang.Forms
{
    /// <summary>
    /// Phân hệ Cài đặt hệ thống (mức độ khó, âm thanh, màn hình) và Sao lưu / Phục hồi dữ liệu.
    /// </summary>
    public class FrmCaiDat : Form
    {
        private readonly DanhMucRepository _dmRepo = new();
        private readonly BackupService _backupService = new();

        private TextBox txtOrgName = null!;
        private NumericUpDown numDefaultTime = null!;
        private CheckBox chkSound = null!;
        private CheckBox chkAutoBackup = null!;

        private readonly Dictionary<int, (TextBox TxtName, Button BtnColor)> _levelInputs = new();

        private TextBox txtBackupFolder = null!;
        private StyledDataGridView dgvBackups = null!;
        private List<System.IO.FileInfo> _backupFiles = new();

        public FrmCaiDat()
        {
            BuildUi();
            LoadSettings();
            LoadBackupsList();
        }

        private void BuildUi()
        {
            BackColor = ThemeColors.AppBackground;
            Padding = new Padding(20);
            Font = ThemeColors.FontBody;

            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));

            // ===== TRÁI: CÀI ĐẶT CHUNG & MỨC ĐỘ KHÓ =====
            var pnlLeft = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0), Padding = new Padding(18), AccentTopColor = ThemeColors.PrimaryBlue, AutoScroll = true };
            int y = 10;

            pnlLeft.Controls.Add(new Label { Text = "Cấu hình chung & Mức độ khó", Location = new Point(16, y), AutoSize = true, Font = ThemeColors.FontSubTitle, ForeColor = ThemeColors.NavyDark });
            y += 38;

            pnlLeft.Controls.Add(new Label { Text = "Tên đơn vị hiển thị mặc định:", Location = new Point(16, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            txtOrgName = new TextBox { Location = new Point(16, y + 22), Width = 430 };
            pnlLeft.Controls.Add(txtOrgName);
            y += 58;

            pnlLeft.Controls.Add(new Label { Text = "Thời gian trả lời mặc định (giây):", Location = new Point(16, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            numDefaultTime = new NumericUpDown { Location = new Point(16, y + 22), Width = 140, Minimum = 5, Maximum = 300, Value = 15 };
            chkSound = new CheckBox { Text = "Bật âm thanh gameshow (5s cuối, chuông, chúc mừng)", Location = new Point(16, y + 60), AutoSize = true };
            chkAutoBackup = new CheckBox { Text = "Tự động sao lưu mỗi ngày khi khởi động phần mềm", Location = new Point(16, y + 88), AutoSize = true };
            pnlLeft.Controls.AddRange(new Control[] { numDefaultTime, chkSound, chkAutoBackup });
            y += 126;

            pnlLeft.Controls.Add(new Label { Text = "Tùy chỉnh tên & màu 4 mức độ khó:", Location = new Point(16, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            y += 28;

            for (int lv = 1; lv <= 4; lv++)
            {
                int levelKey = lv;
                pnlLeft.Controls.Add(new Label { Text = $"Mức {lv}:", Location = new Point(16, y + 4), AutoSize = true });
                var tb = new TextBox { Location = new Point(80, y), Width = 240 };
                var btnCol = new Button { Location = new Point(332, y - 1), Size = new Size(112, 28), Text = "Chọn màu", FlatStyle = FlatStyle.Flat, ForeColor = Color.White };
                btnCol.Click += (_, _) =>
                {
                    using var cd = new ColorDialog { Color = btnCol.BackColor };
                    if (cd.ShowDialog() == DialogResult.OK) btnCol.BackColor = cd.Color;
                };
                _levelInputs[levelKey] = (tb, btnCol);
                pnlLeft.Controls.AddRange(new Control[] { tb, btnCol });
                y += 38;
            }

            var btnSaveCfg = new ModernButton { Text = "Lưu cấu hình", Location = new Point(16, y + 10), Size = new Size(175, 40) };
            btnSaveCfg.Click += (_, _) => SaveSettings();
            pnlLeft.Controls.Add(btnSaveCfg);

            // ===== PHẢI: SAO LƯU & PHỤC HỒI DỮ LIỆU =====
            var pnlRight = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0), Padding = new Padding(18), AccentTopColor = ThemeColors.Gold };
            var lblBackTitle = new Label { Dock = DockStyle.Top, Height = 32, Font = ThemeColors.FontSubTitle, ForeColor = ThemeColors.NavyDark, Text = "Sao lưu & Phục hồi dữ liệu (Access + Hình ảnh)" };

            var pnlBackupTools = new Panel { Dock = DockStyle.Top, Height = 100 };
            pnlBackupTools.Controls.Add(new Label { Text = "Thư mục lưu bản sao lưu:", Location = new Point(0, 4), AutoSize = true, Font = ThemeColors.FontBodyBold });
            txtBackupFolder = new TextBox { Location = new Point(0, 26), Width = 360, Text = FileHelper.BackupDirectory };
            var btnPickDir = new ModernButton { Text = "Chọn...", Location = new Point(368, 24), Size = new Size(90, 30), NormalBackColor = ThemeColors.TextMuted, HoverBackColor = ThemeColors.TextPrimary };
            btnPickDir.Click += (_, _) => ChooseBackupFolder();

            var btnBackupNow = new ModernButton { Text = "Sao lưu ngay", Location = new Point(0, 60), Size = new Size(118, 34), NormalBackColor = ThemeColors.SuccessGreen, HoverBackColor = Color.FromArgb(16, 130, 58) };
            btnBackupNow.Click += (_, _) => RunManualBackup();

            var btnBrowseZip = new ModernButton { Text = "Chọn file .zip...", Location = new Point(124, 60), Size = new Size(122, 34), NormalBackColor = ThemeColors.PrimaryBlue, HoverBackColor = ThemeColors.PrimaryBlueHover };
            btnBrowseZip.Click += (_, _) => BrowseAndVerifyOrRestoreExternalZip();

            var btnVerify = new ModernButton { Text = "Kiểm tra ZIP", Location = new Point(252, 60), Size = new Size(112, 34), NormalBackColor = ThemeColors.InfoCyan, HoverBackColor = Color.FromArgb(14, 116, 144) };
            btnVerify.Click += (_, _) => VerifySelectedBackup();

            var btnRestore = new ModernButton { Text = "Phục hồi", Location = new Point(370, 60), Size = new Size(106, 34), NormalBackColor = ThemeColors.DangerRed, HoverBackColor = Color.FromArgb(185, 28, 28) };
            btnRestore.Click += (_, _) => RestoreSelectedBackup();

            pnlBackupTools.Controls.AddRange(new Control[] { txtBackupFolder, btnPickDir, btnBackupNow, btnBrowseZip, btnVerify, btnRestore });

            dgvBackups = new StyledDataGridView { Dock = DockStyle.Fill };
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "TenFile", HeaderText = "Tên tệp sao lưu", FillWeight = 52 });
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "NgayTao", HeaderText = "Ngày tạo", FillWeight = 28 });
            dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "DungLuong", HeaderText = "Dung lượng", FillWeight = 20 });

            pnlRight.Controls.Add(dgvBackups);
            pnlRight.Controls.Add(pnlBackupTools);
            pnlRight.Controls.Add(lblBackTitle);

            tlp.Controls.Add(pnlLeft, 0, 0);
            tlp.Controls.Add(pnlRight, 1, 0);
            Controls.Add(tlp);
        }

        private void LoadSettings()
        {
            txtOrgName.Text = GetConfig("TenDonViMacDinh", "BAN TỔ CHỨC CHƯƠNG TRÌNH RUNG CHUÔNG VÀNG");
            if (int.TryParse(GetConfig("ThoiGianTraLoiMacDinh", "15"), out int sec))
            {
                numDefaultTime.Value = Math.Clamp(sec, 5, 300);
            }
            chkSound.Checked = !string.Equals(GetConfig("AmThanhBat", "True"), "False", StringComparison.OrdinalIgnoreCase);
            chkAutoBackup.Checked = string.Equals(GetConfig("TuDongSaoLuu", "True"), "True", StringComparison.OrdinalIgnoreCase);

            var mucDo = _dmRepo.GetMucDoConfig();
            for (int lv = 1; lv <= 4; lv++)
            {
                if (mucDo.TryGetValue((MucDoKho)lv, out var info))
                {
                    _levelInputs[lv].TxtName.Text = info.Ten;
                    _levelInputs[lv].BtnColor.BackColor = info.Mau;
                }
            }
        }

        private void SaveSettings()
        {
            UpsertConfig("TenDonViMacDinh", txtOrgName.Text.Trim());
            UpsertConfig("ThoiGianTraLoiMacDinh", ((int)numDefaultTime.Value).ToString());
            UpsertConfig("AmThanhBat", chkSound.Checked ? "True" : "False");
            UpsertConfig("TuDongSaoLuu", chkAutoBackup.Checked ? "True" : "False");

            for (int lv = 1; lv <= 4; lv++)
            {
                UpsertConfig($"MucDo_{lv}_Ten", _levelInputs[lv].TxtName.Text.Trim());
                UpsertConfig($"MucDo_{lv}_Mau", ColorTranslator.ToHtml(_levelInputs[lv].BtnColor.BackColor));
            }

            MessageBox.Show("Đã lưu cấu hình hệ thống thành công.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static string GetConfig(string key, string fallback)
        {
            object? val = DatabaseHelper.ExecuteScalar("SELECT GiaTri FROM CauHinh WHERE TenCauHinh = ?", key);
            return val?.ToString() ?? fallback;
        }

        private static void UpsertConfig(string key, string val)
        {
            int count = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM CauHinh WHERE TenCauHinh = ?", key) ?? 0);
            if (count > 0)
            {
                DatabaseHelper.ExecuteNonQuery("UPDATE CauHinh SET GiaTri = ? WHERE TenCauHinh = ?", val, key);
            }
            else
            {
                DatabaseHelper.ExecuteNonQuery("INSERT INTO CauHinh (TenCauHinh, GiaTri, MoTa) VALUES (?, ?, ?)", key, val, key);
            }
        }

        private void LoadBackupsList()
        {
            _backupFiles = _backupService.GetAvailableBackups();
            dgvBackups.Rows.Clear();
            foreach (var f in _backupFiles)
            {
                dgvBackups.Rows.Add(f.Name, f.CreationTime.ToString("dd/MM/yyyy HH:mm"), $"{f.Length / 1024.0:N0} KB");
            }
        }

        private void ChooseBackupFolder()
        {
            using var fbd = new FolderBrowserDialog { SelectedPath = txtBackupFolder.Text };
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtBackupFolder.Text = fbd.SelectedPath;
            }
        }

        private void RunManualBackup()
        {
            try
            {
                string path = _backupService.CreateBackup(txtBackupFolder.Text.Trim());
                LoadBackupsList();
                MessageBox.Show($"Đã tạo bản sao lưu thành công tại:\n{path}", "Sao lưu hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi sao lưu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string? PickBackupFilePath()
        {
            if (dgvBackups.CurrentRow != null && dgvBackups.CurrentRow.Index >= 0 && dgvBackups.CurrentRow.Index < _backupFiles.Count)
            {
                return _backupFiles[dgvBackups.CurrentRow.Index].FullName;
            }

            using var ofd = new OpenFileDialog { Title = "Chọn file sao lưu ZIP", Filter = "Backup Archive (*.zip)|*.zip" };
            return ofd.ShowDialog() == DialogResult.OK ? ofd.FileName : null;
        }

        private void BrowseAndVerifyOrRestoreExternalZip()
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Chọn tệp sao lưu (.zip) từ máy tính hoặc USB",
                Filter = "Backup Archive (*.zip)|*.zip",
                InitialDirectory = FileHelper.BackupDirectory
            };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            if (!_backupService.ValidateBackupArchive(ofd.FileName, out string report))
            {
                MessageBox.Show(report, "Bản sao lưu không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (MessageBox.Show(
                $"{report}\n\nBạn có muốn phục hồi dữ liệu từ tệp vừa chọn ngay bây giờ không?",
                "Kiểm tra & Phục hồi bản sao lưu",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ExecuteRestoreWithConfirmation(ofd.FileName, report);
            }
        }

        private void VerifySelectedBackup()
        {
            string? path = PickBackupFilePath();
            if (string.IsNullOrWhiteSpace(path)) return;

            bool ok = _backupService.ValidateBackupArchive(path, out string report);
            MessageBox.Show(report, ok ? "Bản sao lưu hợp lệ" : "Bản sao lưu lỗi", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }

        private void RestoreSelectedBackup()
        {
            string? path = PickBackupFilePath();
            if (string.IsNullOrWhiteSpace(path)) return;

            if (!_backupService.ValidateBackupArchive(path, out string report))
            {
                MessageBox.Show(report, "Không thể phục hồi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ExecuteRestoreWithConfirmation(path, report);
        }

        private void ExecuteRestoreWithConfirmation(string path, string report)
        {

            if (MessageBox.Show(
                $"{report}\n\nCẢNH BÁO: Phục hồi sẽ thay thế cơ sở dữ liệu hiện tại.\nHệ thống sẽ tự động sao lưu bản hiện tại trước khi ghi đè.\n\nBạn có chắc chắn muốn tiếp tục?",
                "Xác nhận phục hồi dữ liệu",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                string safetyPath = _backupService.RestoreFromBackup(path, userConfirmed: true);
                LoadSettings();
                LoadBackupsList();
                MessageBox.Show(
                    $"Đã phục hồi dữ liệu thành công!\nBản sao lưu an toàn trước phục hồi được lưu tại:\n{safetyPath}",
                    "Phục hồi hoàn tất",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi phục hồi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
