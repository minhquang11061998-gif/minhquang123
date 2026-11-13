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
using RungChuongVang.Services;

namespace RungChuongVang.Forms
{
    /// <summary>
    /// Màn hình Điều khiển dành cho MC và Ban Tổ Chức (Control Screen - Screen 1).
    /// Thiết kế 5 khu vực: (A) Xem trước trình chiếu, (B) Thông tin chương trình,
    /// (C) 17 nút điều khiển lớn, (D) Quản lý xác nhận thí sinh/đội, (E) Nhật ký điều hành.
    /// </summary>
    public class FrmDieuKhien : Form
    {
        private readonly ChuongTrinhRepository _ctRepo = new();
        private readonly DoiThiRepository _doiRepo = new();
        private readonly LichSuRepository _lsRepo = new();
        private readonly ExamService _examService = new();
        private readonly ScoreService _scoreService = new();

        private readonly ExamStateManager _state = new();
        private readonly PresentationService _presService = new();

        private readonly int _initialProgramId;
        private List<ChuongTrinhThiSinh> _filteredParticipants = new();

        // Top Bar chọn chương trình & màn hình
        private ComboBox cboProgram = null!;
        private ComboBox cboScreens = null!;
        private CheckBox chkWindowTestMode = null!;

        // Khu vực A: Xem trước trình chiếu
        private PresentationStageControl previewStage = null!;

        // Khu vực B: Thông tin chương trình & đồng hồ MC
        private Label lblProgName = null!;
        private Label lblProgMode = null!;
        private Label lblQuestionProgress = null!;
        private Label lblCountInitial = null!;
        private Label lblCountAlive = null!;
        private Label lblCountEliminated = null!;
        private Label lblMcTimer = null!;

        // Khu vực D: Quản lý thí sinh / đội
        private TextBox txtSearchPart = null!;
        private ComboBox cboFilterPart = null!;
        private StyledDataGridView dgvParticipants = null!;
        private TextBox txtBatchAnswer = null!;

        // Khu vực E: Nhật ký điều hành
        private ListBox lstAuditLog = null!;

        public event Action<int>? OpenWinnerSpotlightRequested;
        public event Action<int>? OpenResultsRequested;

        public FrmDieuKhien(int initialProgramId = 0)
        {
            _initialProgramId = initialProgramId;
            BuildUi();
            BindStateEvents();
            LoadScreensList();
            LoadProgramsDropdown();
        }

        private void BuildUi()
        {
            Text = "ĐIỀU KHIỂN CHƯƠNG TRÌNH RUNG CHUÔNG VÀNG (MÀN HÌNH MC)";
            Size = new Size(1420, 880);
            MinimumSize = new Size(1240, 740);
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ThemeColors.AppBackground;
            Font = ThemeColors.FontBody;

            var tlpRoot = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(12) };
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // ===== THANH CẤU HÌNH NHANH CHƯƠNG TRÌNH & HAI MÀN HÌNH =====
            var pnlTopBar = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10), Padding = new Padding(12, 8, 12, 8) };
            pnlTopBar.Controls.Add(new Label { Text = "Chương trình:", Location = new Point(12, 14), AutoSize = true, Font = ThemeColors.FontBodyBold });
            cboProgram = new ComboBox { Location = new Point(112, 10), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
            cboProgram.SelectedIndexChanged += (_, _) => OnSelectedProgramChanged();

            pnlTopBar.Controls.Add(new Label { Text = "Màn hình chiếu:", Location = new Point(450, 14), AutoSize = true, Font = ThemeColors.FontBodyBold });
            cboScreens = new ComboBox { Location = new Point(560, 10), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            chkWindowTestMode = new CheckBox { Text = "Chạy thử 1 màn hình", Location = new Point(792, 13), AutoSize = true };

            var btnOpenPres = new ModernButton { Text = "Mở màn hình chiếu", Location = new Point(960, 7), Size = new Size(165, 34), NormalBackColor = ThemeColors.SuccessGreen, HoverBackColor = Color.FromArgb(16, 130, 58) };
            btnOpenPres.Click += (_, _) => OpenPresentationScreen();

            var btnSwap = new ModernButton { Text = "Đảo 2 màn hình", Location = new Point(1134, 7), Size = new Size(135, 34), NormalBackColor = ThemeColors.RoyalBlue, HoverBackColor = ThemeColors.NavyDark };
            btnSwap.Click += (_, _) => _presService.SwapScreens(this, _state);

            pnlTopBar.Controls.AddRange(new Control[] { cboProgram, cboScreens, chkWindowTestMode, btnOpenPres, btnSwap });

            // ===== LƯỚI 5 KHU VỰC (3 CỘT CHÍNH) =====
            var tlpCols = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
            tlpCols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F)); // A + B
            tlpCols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F)); // C (17 nút)
            tlpCols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F)); // D + E

            // ----- CỘT 1: KHU VỰC A (PREVIEW) + KHU VỰC B (THÔNG TIN) -----
            var tlpCol1 = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Margin = new Padding(0, 0, 8, 0) };
            tlpCol1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCol1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            var pnlZoneA = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 8), Padding = new Padding(10), AccentTopColor = ThemeColors.Gold };
            var lblATitle = new Label { Dock = DockStyle.Top, Height = 26, Font = ThemeColors.FontBodyBold, ForeColor = ThemeColors.NavyDark, Text = "KHU VỰC A: XEM TRƯỚC TRÌNH CHIẾU" };
            previewStage = new PresentationStageControl { Dock = DockStyle.Fill, EnableAnimation = false };
            previewStage.BindState(_state);
            pnlZoneA.Controls.Add(previewStage);
            pnlZoneA.Controls.Add(lblATitle);

            var pnlZoneB = new RoundedPanel { Dock = DockStyle.Fill, Padding = new Padding(12), AccentTopColor = ThemeColors.PrimaryBlue };
            var lblBTitle = new Label { Dock = DockStyle.Top, Height = 26, Font = ThemeColors.FontBodyBold, ForeColor = ThemeColors.NavyDark, Text = "KHU VỰC B: THÔNG TIN CHƯƠNG TRÌNH" };
            var flpStats = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
            lblProgName = new Label { Width = 330, Height = 40, Font = ThemeColors.FontBodyBold, ForeColor = ThemeColors.RoyalBlue, Text = "Chương trình: --" };
            lblProgMode = new Label { Width = 330, Height = 24, Text = "Hình thức: --" };
            lblQuestionProgress = new Label { Width = 330, Height = 26, Font = ThemeColors.FontBodyBold, Text = "Câu hiện tại: 0 / 0" };
            lblCountInitial = new Label { Width = 330, Height = 24, Text = "Ban đầu: 0" };
            lblCountAlive = new Label { Width = 330, Height = 24, ForeColor = ThemeColors.SuccessGreen, Font = ThemeColors.FontBodyBold, Text = "Còn lại: 0" };
            lblCountEliminated = new Label { Width = 330, Height = 24, ForeColor = ThemeColors.DangerRed, Font = ThemeColors.FontBodyBold, Text = "Bị loại: 0" };
            lblMcTimer = new Label
            {
                Width = 330,
                Height = 56,
                Font = new Font("Segoe UI", 28F, FontStyle.Bold),
                ForeColor = ThemeColors.NavyDark,
                BackColor = ThemeColors.GoldSoft,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "00:15"
            };
            flpStats.Controls.AddRange(new Control[] { lblProgName, lblProgMode, lblQuestionProgress, lblCountInitial, lblCountAlive, lblCountEliminated, lblMcTimer });
            pnlZoneB.Controls.Add(flpStats);
            pnlZoneB.Controls.Add(lblBTitle);

            tlpCol1.Controls.Add(pnlZoneA, 0, 0);
            tlpCol1.Controls.Add(pnlZoneB, 0, 1);

            // ----- CỘT 2: KHU VỰC C (17+ NÚT ĐIỀU KHIỂN LỚN) -----
            var pnlZoneC = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(12), AccentTopColor = ThemeColors.Gold };
            var lblCTitle = new Label { Dock = DockStyle.Top, Height = 28, Font = ThemeColors.FontBodyBold, ForeColor = ThemeColors.NavyDark, Text = "KHU VỰC C: ĐIỀU KHIỂN CHƯƠNG TRÌNH" };
            var flpControls = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoScroll = true, Padding = new Padding(0, 6, 0, 0) };

            AddControlButton(flpControls, "1. Bắt đầu chương trình", ThemeColors.SuccessGreen, () => _state.StartProgram());
            AddControlButton(flpControls, "Màn hình chờ", ThemeColors.TextMuted, () => _state.ReturnToStandby());
            AddControlButton(flpControls, "2. Hiển thị câu hỏi", ThemeColors.PrimaryBlue, () => _state.ShowQuestion());
            AddControlButton(flpControls, "Hiện đáp án A,B,C,D", ThemeColors.RoyalBlue, () => _state.ShowOptions());
            AddControlButton(flpControls, "3. Bắt đầu đếm giờ", ThemeColors.SuccessGreen, () => _state.StartCountdown());
            AddControlButton(flpControls, "4. Tạm dừng", ThemeColors.WarningOrange, () => _state.PauseCountdown());
            AddControlButton(flpControls, "5. Tiếp tục", ThemeColors.InfoCyan, () => _state.ResumeCountdown());
            AddControlButton(flpControls, "Đặt lại giờ", ThemeColors.TextMuted, () => _state.ResetCountdown());
            AddControlButton(flpControls, "6. Công bố đáp án", ThemeColors.Gold, () => _state.EndQuestionAndRevealAnswer(), ThemeColors.NavyDark);
            AddControlButton(flpControls, "7. Xác nhận đúng/sai", ThemeColors.PrimaryBlue, () => ConfirmAllActiveAsPrompted());
            AddControlButton(flpControls, "8. Chuyển câu tiếp", ThemeColors.RoyalBlue, () => MoveNextQuestionAndCheck());
            AddControlButton(flpControls, "9. Quay lại câu trước", ThemeColors.TextMuted, () => _state.PreviousQuestion());
            AddControlButton(flpControls, "10. Kết thúc câu hỏi", ThemeColors.NavyDark, () => EndCurrentQuestionFlow());
            AddControlButton(flpControls, "11. DS còn thi", ThemeColors.InfoCyan, () => _state.ShowParticipantsStats(eliminatedOnly: false));
            AddControlButton(flpControls, "12. DS bị loại", ThemeColors.DangerRed, () => _state.ShowParticipantsStats(eliminatedOnly: true));
            AddControlButton(flpControls, "13. Cứu trợ", ThemeColors.SuccessGreen, () => OpenRescueDialog());
            AddControlButton(flpControls, "14. Khôi phục 1 người/đội", ThemeColors.PrimaryBlue, () => RestoreSelectedParticipant());
            AddControlButton(flpControls, "15. Bảng kết quả", ThemeColors.RoyalBlue, () => OpenResultsForm());
            AddControlButton(flpControls, "Thêm câu hỏi phụ", ThemeColors.WarningOrange, () => OpenSupplementaryQuestionDialog());
            AddControlButton(flpControls, "16. Kết thúc chương trình", ThemeColors.DangerRed, () => FinishProgramFlow());
            AddControlButton(flpControls, "17. VINH DANH NGƯỜI CHIẾN THẮNG", ThemeColors.GoldBright, () => OpenWinnerSpotlight(), ThemeColors.NavyDark, wide: true);

            pnlZoneC.Controls.Add(flpControls);
            pnlZoneC.Controls.Add(lblCTitle);

            // ----- CỘT 3: KHU VỰC D (THÍ SINH/ĐỘI) + KHU VỰC E (NHẬT KÝ) -----
            var tlpCol3 = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            tlpCol3.RowStyles.Add(new RowStyle(SizeType.Percent, 68F));
            tlpCol3.RowStyles.Add(new RowStyle(SizeType.Percent, 32F));

            var pnlZoneD = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 8), Padding = new Padding(12), AccentTopColor = ThemeColors.PrimaryBlue };
            var lblDTitle = new Label { Dock = DockStyle.Top, Height = 26, Font = ThemeColors.FontBodyBold, ForeColor = ThemeColors.NavyDark, Text = "KHU VỰC D: QUẢN LÝ & XÁC NHẬN THÍ SINH / ĐỘI THI" };

            var pnlDTools = new Panel { Dock = DockStyle.Top, Height = 82 };
            txtSearchPart = new TextBox { Location = new Point(0, 4), Width = 175, PlaceholderText = "Tìm SBD, tên..." };
            txtSearchPart.TextChanged += (_, _) => RefreshParticipantsGrid();
            cboFilterPart = new ComboBox { Location = new Point(182, 4), Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            cboFilterPart.Items.AddRange(new object[] { "Tất cả", "Còn thi", "Bị loại" });
            cboFilterPart.SelectedIndex = 0;
            cboFilterPart.SelectedIndexChanged += (_, _) => RefreshParticipantsGrid();

            txtBatchAnswer = new TextBox { Location = new Point(330, 4), Width = 170, PlaceholderText = "Đáp án ghi nhận (tùy chọn)" };

            var btnConfirmRight = new ModernButton { Text = "✓ Xác nhận ĐÚNG", Location = new Point(0, 40), Size = new Size(140, 34), NormalBackColor = ThemeColors.SuccessGreen, HoverBackColor = Color.FromArgb(16, 130, 58) };
            btnConfirmRight.Click += (_, _) => ConfirmSelectedRows(KetQuaDungSai.Dung);
            var btnConfirmWrong = new ModernButton { Text = "✕ Xác nhận SAI (Loại)", Location = new Point(146, 40), Size = new Size(165, 34), NormalBackColor = ThemeColors.DangerRed, HoverBackColor = Color.FromArgb(185, 28, 28) };
            btnConfirmWrong.Click += (_, _) => ConfirmSelectedRows(KetQuaDungSai.Sai);
            var btnNoAns = new ModernButton { Text = "Không trả lời", Location = new Point(317, 40), Size = new Size(115, 34), NormalBackColor = ThemeColors.WarningOrange, HoverBackColor = Color.FromArgb(180, 95, 5) };
            btnNoAns.Click += (_, _) => ConfirmSelectedRows(KetQuaDungSai.KhongTraLoi);
            var btnUndo = new ModernButton { Text = "Hoàn tác", Location = new Point(438, 40), Size = new Size(95, 34), NormalBackColor = ThemeColors.TextMuted, HoverBackColor = ThemeColors.TextPrimary };
            btnUndo.Click += (_, _) => PerformUndo();

            pnlDTools.Controls.AddRange(new Control[] { txtSearchPart, cboFilterPart, txtBatchAnswer, btnConfirmRight, btnConfirmWrong, btnNoAns, btnUndo });

            dgvParticipants = new StyledDataGridView { Dock = DockStyle.Fill, MultiSelect = true, ReadOnly = false };
            var colCheck = new DataGridViewCheckBoxColumn { Name = "Chon", HeaderText = "✓", FillWeight = 7 };
            dgvParticipants.Columns.Add(colCheck);
            dgvParticipants.Columns.Add(new DataGridViewTextBoxColumn { Name = "SBD", HeaderText = "SBD/Mã", FillWeight = 14, ReadOnly = true });
            dgvParticipants.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ten", HeaderText = "Họ tên / Đội", FillWeight = 33, ReadOnly = true });
            dgvParticipants.Columns.Add(new DataGridViewTextBoxColumn { Name = "Lop", HeaderText = "Lớp", FillWeight = 14, ReadOnly = true });
            dgvParticipants.Columns.Add(new DataGridViewTextBoxColumn { Name = "Dung", HeaderText = "Đúng", FillWeight = 10, ReadOnly = true });
            dgvParticipants.Columns.Add(new DataGridViewTextBoxColumn { Name = "Loai", HeaderText = "Câu loại", FillWeight = 11, ReadOnly = true });
            dgvParticipants.Columns.Add(new DataGridViewTextBoxColumn { Name = "TrangThai", HeaderText = "Trạng thái", FillWeight = 19, ReadOnly = true });

            pnlZoneD.Controls.Add(dgvParticipants);
            pnlZoneD.Controls.Add(pnlDTools);
            pnlZoneD.Controls.Add(lblDTitle);

            var pnlZoneE = new RoundedPanel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            var lblETitle = new Label { Dock = DockStyle.Top, Height = 24, Font = ThemeColors.FontBodyBold, ForeColor = ThemeColors.NavyDark, Text = "KHU VỰC E: NHẬT KÝ ĐIỀU HÀNH" };
            lstAuditLog = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Font = ThemeColors.FontSmall };
            pnlZoneE.Controls.Add(lstAuditLog);
            pnlZoneE.Controls.Add(lblETitle);

            tlpCol3.Controls.Add(pnlZoneD, 0, 0);
            tlpCol3.Controls.Add(pnlZoneE, 0, 1);

            tlpCols.Controls.Add(tlpCol1, 0, 0);
            tlpCols.Controls.Add(pnlZoneC, 1, 0);
            tlpCols.Controls.Add(tlpCol3, 2, 0);

            tlpRoot.Controls.Add(pnlTopBar, 0, 0);
            tlpRoot.Controls.Add(tlpCols, 0, 1);
            Controls.Add(tlpRoot);

            FormClosing += (_, _) =>
            {
                _presService.ClosePresentation();
                _state.Dispose();
            };
        }

        private static void AddControlButton(FlowLayoutPanel parent, string text, Color bg, Action onClick, Color? fg = null, bool wide = false)
        {
            var btn = new ModernButton
            {
                Text = text,
                Size = new Size(wide ? 372 : 182, 44),
                Margin = new Padding(0, 0, 8, 8),
                NormalBackColor = bg,
                HoverBackColor = ControlPaint.Dark(bg, 0.08f),
                ForeColor = fg ?? Color.White,
                Font = ThemeColors.FontBodyBold
            };
            btn.Click += (_, _) => onClick();
            parent.Controls.Add(btn);
        }

        private void BindStateEvents()
        {
            _state.StateChanged += () => PresentationService.RunOnUi(this, UpdateUiFromState);
            _state.TimerChanged += (_, disp, warn) => PresentationService.RunOnUi(this, () =>
            {
                lblMcTimer.Text = $"00:{disp:00}";
                lblMcTimer.BackColor = warn || disp == 0 ? ThemeColors.DangerRed : ThemeColors.GoldSoft;
                lblMcTimer.ForeColor = warn || disp == 0 ? Color.White : ThemeColors.NavyDark;
            });
            _state.AuditLogUpdated += () => PresentationService.RunOnUi(this, RefreshAuditLog);
        }

        private void LoadScreensList()
        {
            cboScreens.Items.Clear();
            var screens = _presService.GetAllScreens();
            for (int i = 0; i < screens.Length; i++)
            {
                string tag = screens[i].Primary ? " (Chính)" : " (Phụ - Máy chiếu)";
                cboScreens.Items.Add($"Màn hình {i + 1}: {screens[i].Bounds.Width}x{screens[i].Bounds.Height}{tag}");
            }

            int defIdx = _presService.GetDefaultPresentationScreenIndex();
            if (cboScreens.Items.Count > 0) cboScreens.SelectedIndex = defIdx;
            chkWindowTestMode.Checked = screens.Length == 1;
        }

        private void LoadProgramsDropdown()
        {
            var programs = _ctRepo.GetAllPrograms();
            cboProgram.DataSource = programs;
            cboProgram.DisplayMember = "TenChuongTrinh";
            cboProgram.ValueMember = "ChuongTrinhID";

            if (_initialProgramId > 0)
            {
                cboProgram.SelectedValue = _initialProgramId;
            }
        }

        private void OnSelectedProgramChanged()
        {
            if (cboProgram.SelectedItem is not ChuongTrinh ct) return;
            _state.LoadProgram(ct.ChuongTrinhID);
            UpdateUiFromState();
            RefreshAuditLog();
        }

        private void OpenPresentationScreen()
        {
            int idx = cboScreens.SelectedIndex >= 0 ? cboScreens.SelectedIndex : 0;
            _presService.OpenOrUpdatePresentation(_state, idx, chkWindowTestMode.Checked);
        }

        private void UpdateUiFromState()
        {
            var p = _state.CurrentProgram;
            if (p == null) return;

            lblProgName.Text = $"Chương trình: {p.TenChuongTrinh}";
            lblProgMode.Text = p.PhuongThucThi == PhuongThucThi.ThiCaNhan
                ? "Phương thức: Thi cá nhân"
                : $"Phương thức: Thi theo đội ({p.QuyTacTraLoiDoi})";
            lblQuestionProgress.Text = _state.Questions.Count > 0
                ? $"Câu hiện tại: {_state.CurrentQuestionIndex + 1} / {_state.Questions.Count}"
                : "Câu hiện tại: Chưa có bộ câu hỏi";
            lblCountInitial.Text = $"Số lượng ban đầu: {_state.TotalParticipants}";
            lblCountAlive.Text = $"Còn lại trên sàn: {_state.RemainingParticipantsCount}";
            lblCountEliminated.Text = $"Đã bị loại: {_state.EliminatedParticipantsCount}";

            lblMcTimer.Text = $"00:{_state.DisplaySeconds:00}";
            RefreshParticipantsGrid();
        }

        private void RefreshParticipantsGrid()
        {
            string kw = txtSearchPart.Text.Trim();
            string filterMode = cboFilterPart.SelectedItem?.ToString() ?? "Tất cả";

            _filteredParticipants = _state.Participants.Where(item =>
            {
                if (!string.IsNullOrWhiteSpace(kw) &&
                    !item.TenHienThi.Contains(kw, StringComparison.OrdinalIgnoreCase) &&
                    !item.SoBaoDanh.Contains(kw, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                if (filterMode == "Còn thi" && !ExamStateManager.IsParticipantActive(item)) return false;
                if (filterMode == "Bị loại" && item.TrangThaiThi != TrangThaiThi.BiLoai) return false;
                return true;
            }).ToList();

            dgvParticipants.Rows.Clear();
            foreach (var item in _filteredParticipants)
            {
                bool active = ExamStateManager.IsParticipantActive(item);
                int idx = dgvParticipants.Rows.Add(
                    active, // Mặc định tick sẵn những đối tượng còn đang thi để MC thao tác nhanh
                    item.SoBaoDanh,
                    item.TenHienThi,
                    item.TenLop,
                    item.SoCauDung,
                    item.CauHoiBiLoai?.ToString() ?? "-",
                    FormatTrangThaiThi(item.TrangThaiThi));

                if (item.TrangThaiThi == TrangThaiThi.BiLoai)
                {
                    dgvParticipants.Rows[idx].DefaultCellStyle.ForeColor = ThemeColors.DangerRed;
                }
                else if (item.TrangThaiThi == TrangThaiThi.ChienThang)
                {
                    dgvParticipants.Rows[idx].DefaultCellStyle.ForeColor = ThemeColors.Gold;
                    dgvParticipants.Rows[idx].DefaultCellStyle.Font = ThemeColors.FontBodyBold;
                }
            }
        }

        private void RefreshAuditLog()
        {
            if (_state.CurrentProgram == null) return;
            var logs = _lsRepo.GetAuditLog(_state.CurrentProgram.ChuongTrinhID, 60);
            lstAuditLog.Items.Clear();
            foreach (var log in logs)
            {
                lstAuditLog.Items.Add($"[{log.ThoiGian:HH:mm:ss}] {log.LoaiThaoTac}: {log.DuLieuMoi}");
            }
        }

        private List<ChuongTrinhThiSinh> GetCheckedParticipants()
        {
            var result = new List<ChuongTrinhThiSinh>();
            for (int i = 0; i < dgvParticipants.Rows.Count && i < _filteredParticipants.Count; i++)
            {
                bool isChecked = Convert.ToBoolean(dgvParticipants.Rows[i].Cells["Chon"].Value ?? false);
                if (isChecked)
                {
                    result.Add(_filteredParticipants[i]);
                }
            }
            return result;
        }

        private void ConfirmAllActiveAsPrompted()
        {
            var chosen = GetCheckedParticipants();
            if (chosen.Count == 0)
            {
                MessageBox.Show("Hãy tích chọn các thí sinh/đội ở Khu vực D trước khi xác nhận.", "Chưa chọn đối tượng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dr = MessageBox.Show(
                $"Xác nhận {chosen.Count} đối tượng đang tích chọn là TRẢ LỜI ĐÚNG?\n(Chọn No để đánh dấu SAI / Bị loại, Cancel để hủy)",
                "Xác nhận kết quả hàng loạt",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (dr == DialogResult.Yes) ConfirmSelectedRows(KetQuaDungSai.Dung);
            else if (dr == DialogResult.No) ConfirmSelectedRows(KetQuaDungSai.Sai);
        }

        private void ConfirmSelectedRows(KetQuaDungSai verdict)
        {
            if (_state.CurrentProgram == null || _state.CurrentQuestionSlot == null)
            {
                MessageBox.Show("Chưa có chương trình hoặc câu hỏi hiện tại.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var chosen = GetCheckedParticipants().Where(ExamStateManager.IsParticipantActive).ToList();
            if (chosen.Count == 0)
            {
                MessageBox.Show("Không có thí sinh/đội đang thi nào được tích chọn.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double elapsed = Math.Max(0, _state.CurrentQuestionSlot.ThoiGianTraLoi - _state.Timer.RemainingSeconds);
            var list = new List<ParticipantAnswerConfirmation>();

            foreach (var p in chosen)
            {
                var confirm = new ParticipantAnswerConfirmation
                {
                    ParticipantRecordId = p.ID,
                    ThiSinhId = p.ThiSinhID,
                    DoiThiId = p.DoiThiID,
                    DapAnDaChon = txtBatchAnswer.Text.Trim(),
                    KetQua = verdict,
                    ThoiGianTraLoi = Math.Round(elapsed, 2)
                };

                // Nếu áp dụng Quy tắc 3: Tất cả thành viên đội phải đúng -> ghi nhận chi tiết từng thành viên
                if (_state.CurrentProgram.PhuongThucThi == PhuongThucThi.ThiTheoDoi &&
                    _state.CurrentProgram.QuyTacTraLoiDoi == QuyTacTraLoiDoi.TatCaThanhVienPhaiDung &&
                    p.DoiThiID.HasValue)
                {
                    var members = _doiRepo.GetThanhVienByDoi(p.DoiThiID.Value);
                    foreach (var m in members)
                    {
                        confirm.MemberAnswers.Add(new MemberAnswerItem
                        {
                            ThiSinhId = m.ThiSinhID,
                            DapAnDaChon = txtBatchAnswer.Text.Trim(),
                            KetQua = verdict
                        });
                    }
                }

                list.Add(confirm);
            }

            _examService.ConfirmParticipantResults(
                _state.CurrentProgram.ChuongTrinhID,
                _state.CurrentQuestionSlot.CauHoiID,
                _state.CurrentQuestionIndex + 1,
                list,
                _state.CurrentProgram.QuyTacTraLoiDoi);

            if (verdict == KetQuaDungSai.Dung) SoundHelper.PlayCorrect();
            else SoundHelper.PlayWrong();

            _state.NotifyParticipantsUpdated();
            EvaluateEndRoundNotice();
        }

        private void MoveNextQuestionAndCheck()
        {
            if (_state.CurrentProgram == null) return;
            var check = _examService.CheckRoundEndCondition(
                _state.CurrentProgram.ChuongTrinhID,
                _state.CurrentProgram.PhuongThucThi,
                _state.CurrentQuestionIndex + 1,
                _state.Questions.Count);

            if (check.Status == RoundEndStatus.OnlyOneRemaining)
            {
                var dr = MessageBox.Show(check.Message + "\n\nXác nhận người chiến thắng ngay bây giờ?", "Còn 1 thí sinh/đội", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (dr == DialogResult.Yes)
                {
                    FinishProgramFlow();
                    return;
                }
            }
            else if (check.Status == RoundEndStatus.OutOfQuestionsWithMultipleRemaining)
            {
                MessageBox.Show(check.Message, "Hết bộ câu hỏi chính", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _state.NextQuestion();
        }

        private void EndCurrentQuestionFlow()
        {
            _state.EndQuestionAndRevealAnswer();
            EvaluateEndRoundNotice();
        }

        private void EvaluateEndRoundNotice()
        {
            if (_state.CurrentProgram == null) return;
            var check = _examService.CheckRoundEndCondition(
                _state.CurrentProgram.ChuongTrinhID,
                _state.CurrentProgram.PhuongThucThi,
                _state.CurrentQuestionIndex + 1,
                _state.Questions.Count);

            if (check.Status == RoundEndStatus.OnlyOneRemaining)
            {
                MessageBox.Show(check.Message, "Thông báo chung kết", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (check.Status == RoundEndStatus.ZeroRemaining)
            {
                MessageBox.Show(check.Message, "Sàn đấu trống", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenRescueDialog()
        {
            if (_state.CurrentProgram == null) return;
            var eliminated = _state.Participants.Where(p => p.TrangThaiThi == TrangThaiThi.BiLoai).ToList();
            if (eliminated.Count == 0)
            {
                MessageBox.Show("Hiện không có thí sinh/đội nào bị loại để cứu trợ.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool isTeam = _state.CurrentProgram.PhuongThucThi == PhuongThucThi.ThiTheoDoi;
            using var dlg = new FrmCuuTroDialog(eliminated, isTeam);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                int rescued = _examService.RescueParticipants(
                    _state.CurrentProgram.ChuongTrinhID,
                    _state.CurrentQuestionSlot?.CauHoiID,
                    dlg.SelectedRecordIds,
                    dlg.ReasonText);

                if (rescued > 0)
                {
                    _state.ShowRescueScreen(rescued, dlg.SelectedNames, dlg.ReasonText);
                }
            }
        }

        private void RestoreSelectedParticipant()
        {
            if (_state.CurrentProgram == null) return;
            var chosen = GetCheckedParticipants().Where(p => p.TrangThaiThi == TrangThaiThi.BiLoai).ToList();
            if (chosen.Count == 0)
            {
                MessageBox.Show("Hãy tích chọn thí sinh/đội đang ở trạng thái Bị loại để khôi phục.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int restored = _examService.RescueParticipants(
                _state.CurrentProgram.ChuongTrinhID,
                _state.CurrentQuestionSlot?.CauHoiID,
                chosen.Select(x => x.ID).ToList(),
                "Khôi phục trực tiếp từ MC",
                isSingleRestore: true);

            if (restored > 0)
            {
                _state.ShowRescueScreen(restored, chosen.Select(x => x.TenHienThi).ToList(), "KHÔI PHỤC THÍ SINH");
            }
        }

        private void PerformUndo()
        {
            if (_state.CurrentProgram == null) return;
            string? desc = _examService.PreviewLastUndoDescription(_state.CurrentProgram.ChuongTrinhID);
            if (string.IsNullOrWhiteSpace(desc))
            {
                MessageBox.Show("Không có thao tác nào khả dụng để hoàn tác.", "Hoàn tác", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show($"Bạn có chắc chắn muốn hoàn tác thao tác gần nhất:\n\"{desc}\"?", "Xác nhận hoàn tác", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            if (_examService.UndoLastAction(_state.CurrentProgram.ChuongTrinhID, out string msg))
            {
                _state.NotifyParticipantsUpdated();
                MessageBox.Show(msg, "Hoàn tác thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(msg, "Không thể hoàn tác", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenSupplementaryQuestionDialog()
        {
            if (_state.CurrentProgram == null) return;
            var usedIds = _state.Questions.Select(q => q.CauHoiID);
            using var dlg = new FrmCauHoiPhuDialog(usedIds);
            if (dlg.ShowDialog() == DialogResult.OK && dlg.SelectedQuestion != null)
            {
                _ctRepo.AddSupplementaryQuestion(_state.CurrentProgram.ChuongTrinhID, dlg.SelectedQuestion.CauHoiID, dlg.SelectedQuestion.ThoiGianTraLoi);
                _state.ReloadQuestions(jumpToLast: true);
                _state.ShowQuestion();
            }
        }

        private void FinishProgramFlow()
        {
            if (_state.CurrentProgram == null) return;
            var alive = _state.Participants.Where(ExamStateManager.IsParticipantActive).ToList();
            if (alive.Count == 0)
            {
                if (MessageBox.Show("Không còn thí sinh/đội nào trụ lại. Bạn vẫn muốn kết thúc chương trình và chốt bảng xếp hạng?", "Kết thúc", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }
                _examService.ConfirmWinnersAndFinish(_state.CurrentProgram.ChuongTrinhID, _state.CurrentProgram.PhuongThucThi, new List<int>());
                _state.NotifyParticipantsUpdated();
                return;
            }

            string names = string.Join(", ", alive.Select(a => a.TenHienThi));
            if (MessageBox.Show($"Xác nhận chiến thắng cho: {names} và kết thúc chương trình?", "Xác nhận chiến thắng", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _examService.ConfirmWinnersAndFinish(_state.CurrentProgram.ChuongTrinhID, _state.CurrentProgram.PhuongThucThi, alive.Select(a => a.ID).ToList());
                _state.ShowWinnerStageScreen();
            }
        }

        private void OpenResultsForm()
        {
            if (_state.CurrentProgram == null) return;
            _scoreService.CalculateAndSaveRankings(_state.CurrentProgram.ChuongTrinhID, _state.CurrentProgram.PhuongThucThi);
            OpenResultsRequested?.Invoke(_state.CurrentProgram.ChuongTrinhID);
        }

        private void OpenWinnerSpotlight()
        {
            if (_state.CurrentProgram == null) return;
            var spotlight = new FrmVinhDanhControl(_state.CurrentProgram.ChuongTrinhID, _state, _presService);
            spotlight.Show();
            OpenWinnerSpotlightRequested?.Invoke(_state.CurrentProgram.ChuongTrinhID);
        }

        private static string FormatTrangThaiThi(TrangThaiThi st) => st switch
        {
            TrangThaiThi.ChuaThamGia => "Chưa thi",
            TrangThaiThi.DangThi => "Đang thi",
            TrangThaiThi.TraLoiDung => "Đúng",
            TrangThaiThi.BiLoai => "Bị loại",
            TrangThaiThi.DuocCuuTro => "Được cứu trợ",
            TrangThaiThi.DuocKhoiPhuc => "Khôi phục",
            TrangThaiThi.ChienThang => "CHIẾN THẮNG",
            _ => "Khác"
        };
    }
}
