using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
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
    /// Xem kết quả xếp hạng chương trình thi, lịch sử trả lời chi tiết và điều chỉnh chính thức của Ban Tổ Chức.
    /// </summary>
    public class FrmKetQua : Form
    {
        private readonly ChuongTrinhRepository _ctRepo = new();
        private readonly KetQuaRepository _kqRepo = new();
        private readonly LichSuRepository _lsRepo = new();
        private readonly ScoreService _scoreService = new();
        private readonly ReportService _reportService = new();

        private ComboBox cboProgram = null!;
        private StyledDataGridView dgvRankings = null!;
        private StyledDataGridView dgvDetailAnswers = null!;
        private ListBox lstElimHistory = null!;

        private ChuongTrinh? _currentProgram;
        private List<ChuongTrinhThiSinh> _participants = new();

        public FrmKetQua()
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
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var pnlTop = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 12), Padding = new Padding(12) };
            pnlTop.Controls.Add(new Label { Text = "Chương trình:", Location = new Point(12, 16), AutoSize = true, Font = ThemeColors.FontBodyBold });
            cboProgram = new ComboBox { Location = new Point(116, 12), Width = 360, DropDownStyle = ComboBoxStyle.DropDownList };
            cboProgram.SelectedIndexChanged += (_, _) => LoadProgramResults();

            var btnRecalc = new ModernButton { Text = "Tính lại xếp hạng", Location = new Point(490, 9), Size = new Size(155, 36), NormalBackColor = ThemeColors.PrimaryBlue, HoverBackColor = ThemeColors.PrimaryBlueHover };
            btnRecalc.Click += (_, _) => RecalculateRankings();

            var btnAdjust = new ModernButton { Text = "Điều chỉnh kết quả BTC", Location = new Point(655, 9), Size = new Size(185, 36), NormalBackColor = ThemeColors.WarningOrange, HoverBackColor = Color.FromArgb(180, 95, 5) };
            btnAdjust.Click += (_, _) => AdjustSelectedParticipant();

            var btnExport = new ModernButton { Text = "Xuất kết quả Excel", Location = new Point(850, 9), Size = new Size(165, 36), NormalBackColor = ThemeColors.Gold, HoverBackColor = ThemeColors.GoldBright, ForeColor = ThemeColors.NavyDark };
            btnExport.Click += (_, _) => ExportExcel();

            pnlTop.Controls.AddRange(new Control[] { cboProgram, btnRecalc, btnAdjust, btnExport });

            var tlpBody = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            tlpBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
            tlpBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));

            var pnlLeft = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0), Padding = new Padding(12), AccentTopColor = ThemeColors.Gold };
            var lblRankTitle = new Label { Dock = DockStyle.Top, Height = 28, Font = ThemeColors.FontSubTitle, ForeColor = ThemeColors.NavyDark, Text = "Bảng xếp hạng & Thống kê thí sinh / đội thi" };
            dgvRankings = new StyledDataGridView { Dock = DockStyle.Fill };
            dgvRankings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Hang", HeaderText = "Hạng", FillWeight = 9 });
            dgvRankings.Columns.Add(new DataGridViewTextBoxColumn { Name = "SBD", HeaderText = "SBD/Mã", FillWeight = 13 });
            dgvRankings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ten", HeaderText = "Họ tên / Đội", FillWeight = 28 });
            dgvRankings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Lop", HeaderText = "Lớp", FillWeight = 13 });
            dgvRankings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Dung", HeaderText = "Đúng", FillWeight = 9 });
            dgvRankings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Loai", HeaderText = "Câu loại", FillWeight = 11 });
            dgvRankings.Columns.Add(new DataGridViewTextBoxColumn { Name = "CuuTro", HeaderText = "Cứu trợ", FillWeight = 10 });
            dgvRankings.Columns.Add(new DataGridViewTextBoxColumn { Name = "TrangThai", HeaderText = "Trạng thái", FillWeight = 17 });
            dgvRankings.SelectionChanged += (_, _) => LoadSelectedDetail();
            pnlLeft.Controls.Add(dgvRankings);
            pnlLeft.Controls.Add(lblRankTitle);

            var tlpRight = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            tlpRight.RowStyles.Add(new RowStyle(SizeType.Percent, 58F));
            tlpRight.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));

            var pnlDet = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 8), Padding = new Padding(12) };
            var lblDet = new Label { Dock = DockStyle.Top, Height = 26, Font = ThemeColors.FontBodyBold, ForeColor = ThemeColors.NavyDark, Text = "Chi tiết trả lời theo từng câu hỏi" };
            dgvDetailAnswers = new StyledDataGridView { Dock = DockStyle.Fill };
            dgvDetailAnswers.Columns.Add(new DataGridViewTextBoxColumn { Name = "CauHoiID", HeaderText = "Câu ID", FillWeight = 16 });
            dgvDetailAnswers.Columns.Add(new DataGridViewTextBoxColumn { Name = "DapAn", HeaderText = "Đáp án", FillWeight = 24 });
            dgvDetailAnswers.Columns.Add(new DataGridViewTextBoxColumn { Name = "KetQua", HeaderText = "Kết quả", FillWeight = 24 });
            dgvDetailAnswers.Columns.Add(new DataGridViewTextBoxColumn { Name = "ThoiGian", HeaderText = "Giây", FillWeight = 14 });
            dgvDetailAnswers.Columns.Add(new DataGridViewTextBoxColumn { Name = "TrangThai", HeaderText = "Xử lý", FillWeight = 22 });
            pnlDet.Controls.Add(dgvDetailAnswers);
            pnlDet.Controls.Add(lblDet);

            var pnlLog = new RoundedPanel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            var lblLog = new Label { Dock = DockStyle.Top, Height = 26, Font = ThemeColors.FontBodyBold, ForeColor = ThemeColors.NavyDark, Text = "Lịch sử loại & Điều chỉnh BTC" };
            lstElimHistory = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Font = ThemeColors.FontSmall };
            pnlLog.Controls.Add(lstElimHistory);
            pnlLog.Controls.Add(lblLog);

            tlpRight.Controls.Add(pnlDet, 0, 0);
            tlpRight.Controls.Add(pnlLog, 0, 1);

            tlpBody.Controls.Add(pnlLeft, 0, 0);
            tlpBody.Controls.Add(tlpRight, 1, 0);

            tlp.Controls.Add(pnlTop, 0, 0);
            tlp.Controls.Add(tlpBody, 0, 1);
            Controls.Add(tlp);
        }

        private void LoadPrograms()
        {
            var programs = _ctRepo.GetAllPrograms();
            cboProgram.DataSource = programs;
            cboProgram.DisplayMember = "TenChuongTrinh";
            cboProgram.ValueMember = "ChuongTrinhID";
        }

        private void LoadProgramResults()
        {
            if (cboProgram.SelectedItem is not ChuongTrinh ct) return;
            _currentProgram = ct;

            _participants = _ctRepo.GetParticipants(ct.ChuongTrinhID, ct.PhuongThucThi)
                .OrderBy(p => p.ThuHang ?? int.MaxValue)
                .ThenByDescending(p => p.SoCauDung)
                .ToList();

            dgvRankings.Rows.Clear();
            foreach (var p in _participants)
            {
                dgvRankings.Rows.Add(
                    p.ThuHang?.ToString() ?? "-",
                    p.SoBaoDanh,
                    p.TenHienThi,
                    p.TenLop,
                    p.SoCauDung,
                    p.CauHoiBiLoai?.ToString() ?? "-",
                    p.SoLanCuuTro,
                    p.TrangThaiThi.ToString());
            }

            lstElimHistory.Items.Clear();
            var elim = _lsRepo.GetLichSuLoai(ct.ChuongTrinhID);
            foreach (var e in elim)
            {
                lstElimHistory.Items.Add($"[{e.ThoiGianLoai:HH:mm:ss}] Câu ID {e.CauHoiID} - {e.LyDoLoai} ({e.NguoiThucHien})");
            }
            var audit = _lsRepo.GetAuditLog(ct.ChuongTrinhID, 30).Where(a => a.LoaiThaoTac == "DIEU_CHINH_BTC");
            foreach (var a in audit)
            {
                lstElimHistory.Items.Add($"[BTC {a.ThoiGian:HH:mm:ss}] {a.DuLieuMoi}");
            }
        }

        private void LoadSelectedDetail()
        {
            dgvDetailAnswers.Rows.Clear();
            if (_currentProgram == null || dgvRankings.CurrentRow == null || dgvRankings.CurrentRow.Index < 0 || dgvRankings.CurrentRow.Index >= _participants.Count)
                return;

            var p = _participants[dgvRankings.CurrentRow.Index];
            var allKq = _kqRepo.GetKetQuaByProgram(_currentProgram.ChuongTrinhID);
            var filtered = allKq.Where(k =>
                (_currentProgram.PhuongThucThi == PhuongThucThi.ThiCaNhan && k.ThiSinhID == p.ThiSinhID) ||
                (_currentProgram.PhuongThucThi == PhuongThucThi.ThiTheoDoi && k.DoiThiID == p.DoiThiID));

            foreach (var k in filtered)
            {
                dgvDetailAnswers.Rows.Add(k.CauHoiID, k.DapAnDaChon, k.KetQuaDungSai.ToString(), k.ThoiGianTraLoi, k.TrangThaiXuLy.ToString());
            }
        }

        private void RecalculateRankings()
        {
            if (_currentProgram == null) return;
            _scoreService.CalculateAndSaveRankings(_currentProgram.ChuongTrinhID, _currentProgram.PhuongThucThi);
            LoadProgramResults();
            MessageBox.Show("Đã tính toán và lưu bảng xếp hạng mới nhất.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AdjustSelectedParticipant()
        {
            if (_currentProgram == null || dgvRankings.CurrentRow == null || dgvRankings.CurrentRow.Index < 0 || dgvRankings.CurrentRow.Index >= _participants.Count)
                return;

            var p = _participants[dgvRankings.CurrentRow.Index];
            using var dlg = new Form
            {
                Text = $"Điều chỉnh kết quả BTC: {p.TenHienThi}",
                Size = new Size(420, 310),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false
            };

            var cboSt = new ComboBox { Location = new Point(20, 34), Width = 360, DropDownStyle = ComboBoxStyle.DropDownList };
            cboSt.DataSource = Enum.GetValues<TrangThaiThi>();
            cboSt.SelectedItem = p.TrangThaiThi;

            var numDung = new NumericUpDown { Location = new Point(20, 92), Width = 160, Minimum = 0, Maximum = 500, Value = Math.Clamp(p.SoCauDung, 0, 500) };
            var numHang = new NumericUpDown { Location = new Point(210, 92), Width = 170, Minimum = 1, Maximum = 1000, Value = Math.Clamp(p.ThuHang ?? 1, 1, 1000) };
            var txtReason = new TextBox { Location = new Point(20, 150), Width = 360, PlaceholderText = "Lý do điều chỉnh của Ban Tổ Chức (*)" };

            var btnOk = new ModernButton { Text = "Lưu điều chỉnh", Location = new Point(220, 204), Size = new Size(160, 36) };
            btnOk.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(txtReason.Text))
                {
                    MessageBox.Show("Vui lòng nhập lý do điều chỉnh để lưu lịch sử.", "Thiếu lý do", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                _scoreService.AdjustOfficialResult(
                    _currentProgram.ChuongTrinhID,
                    p.ID,
                    (TrangThaiThi)cboSt.SelectedItem!,
                    (int)numDung.Value,
                    (int)numHang.Value,
                    txtReason.Text.Trim());
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };

            dlg.Controls.AddRange(new Control[]
            {
                new Label { Text = "Trạng thái mới:", Location = new Point(20, 12), AutoSize = true },
                cboSt,
                new Label { Text = "Số câu đúng:", Location = new Point(20, 70), AutoSize = true },
                new Label { Text = "Thứ hạng mới:", Location = new Point(210, 70), AutoSize = true },
                numDung, numHang,
                new Label { Text = "Lý do điều chỉnh (*):", Location = new Point(20, 128), AutoSize = true },
                txtReason, btnOk
            });

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                LoadProgramResults();
            }
        }

        private void ExportExcel()
        {
            if (_currentProgram == null) return;
            using var sfd = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"KetQuaThi_{_currentProgram.MaChuongTrinh}.xlsx"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                DataTable dt = _currentProgram.PhuongThucThi == PhuongThucThi.ThiCaNhan
                    ? _reportService.GetStudentResultReportTable(_currentProgram.ChuongTrinhID)
                    : _reportService.GetTeamResultReportTable(_currentProgram.ChuongTrinhID);

                _reportService.ExportSingleTableToExcel("KetQuaThi", $"KẾT QUẢ {_currentProgram.TenChuongTrinh.ToUpperInvariant()}", dt, sfd.FileName);
                MessageBox.Show("Đã xuất bảng kết quả ra Excel.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
