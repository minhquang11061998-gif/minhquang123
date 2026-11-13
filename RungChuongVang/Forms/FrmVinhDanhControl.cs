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
    /// Phân hệ Vinh danh người chiến thắng: nhập/lấy tự động thông tin, cắt/chỉnh ảnh,
    /// quản lý nhiều giải thưởng, xem trước và xác nhận trình chiếu lên màn hình Vinh danh.
    /// </summary>
    public class FrmVinhDanhControl : Form
    {
        private readonly ChuongTrinhRepository _ctRepo = new();
        private readonly VinhDanhRepository _vdRepo = new();
        private readonly WinnerService _winnerService = new();

        private readonly ExamStateManager _stateManager;
        private readonly PresentationService _presService;
        private readonly bool _ownsServices;

        private readonly int _initialProgramId;
        private ChuongTrinh? _currentProgram;
        private List<VinhDanh> _winners = new();
        private int _editingId;
        private string _currentImgRelPath = string.Empty;

        private ComboBox cboProgram = null!;
        private StyledDataGridView dgvWinners = null!;

        private TextBox txtHoTen = null!;
        private TextBox txtSbd = null!;
        private TextBox txtLop = null!;
        private TextBox txtTruong = null!;
        private TextBox txtDoi = null!;
        private ComboBox cboGiaiThuong = null!;
        private TextBox txtThanhTich = null!;
        private NumericUpDown numDung = null!;
        private NumericUpDown numVuotQua = null!;
        private TextBox txtGhiChu = null!;
        private PictureBox picWinner = null!;

        // Xem trước riêng trước khi bấm Xác nhận & Trình chiếu (đảm bảo không đẩy dữ liệu đang sửa lên máy chiếu)
        private readonly ExamStateManager _previewState = new();
        private PresentationStageControl previewCanvas = null!;

        public FrmVinhDanhControl(int programId = 0, ExamStateManager? sharedState = null, PresentationService? sharedPres = null)
        {
            _initialProgramId = programId;
            if (sharedState != null && sharedPres != null)
            {
                _stateManager = sharedState;
                _presService = sharedPres;
                _ownsServices = false;
            }
            else
            {
                _stateManager = new ExamStateManager();
                _presService = new PresentationService();
                _ownsServices = true;
            }

            BuildUi();
            LoadPrograms();
        }

        private void BuildUi()
        {
            Text = "VINH DANH NGƯỜI CHIẾN THẮNG";
            BackColor = ThemeColors.AppBackground;
            Padding = new Padding(16);
            Font = ThemeColors.FontBody;
            if (!_ownsServices)
            {
                Size = new Size(1340, 820);
                StartPosition = FormStartPosition.CenterScreen;
            }

            var tlpRoot = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Header bar
            var pnlHeader = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10), Padding = new Padding(12, 8, 12, 8) };
            pnlHeader.Controls.Add(new Label { Text = "Chương trình:", Location = new Point(12, 14), AutoSize = true, Font = ThemeColors.FontBodyBold });
            cboProgram = new ComboBox { Location = new Point(120, 10), Width = 340, DropDownStyle = ComboBoxStyle.DropDownList };
            cboProgram.SelectedIndexChanged += (_, _) => OnProgramChanged();

            var btnAutoPull = new ModernButton { Text = "Lấy tự động từ kết quả thi", Location = new Point(474, 7), Size = new Size(210, 34), NormalBackColor = ThemeColors.RoyalBlue, HoverBackColor = ThemeColors.NavyDark };
            btnAutoPull.Click += (_, _) => AutoPopulateFromResults();

            var btnOpenScreen = new ModernButton { Text = "1. Mở màn hình vinh danh", Location = new Point(694, 7), Size = new Size(190, 34), NormalBackColor = ThemeColors.SuccessGreen, HoverBackColor = Color.FromArgb(16, 130, 58) };
            btnOpenScreen.Click += (_, _) => OpenSpotlightScreen();

            var btnExport = new ModernButton { Text = "Xuất danh sách Excel", Location = new Point(894, 7), Size = new Size(165, 34), NormalBackColor = ThemeColors.Gold, HoverBackColor = ThemeColors.GoldBright, ForeColor = ThemeColors.NavyDark };
            btnExport.Click += (_, _) => ExportExcel();

            pnlHeader.Controls.AddRange(new Control[] { cboProgram, btnAutoPull, btnOpenScreen, btnExport });

            // 3 cột: DS giải thưởng | Nhập thông tin & Ảnh | Xem trước & Điều khiển trình chiếu
            var tlpBody = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
            tlpBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            tlpBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
            tlpBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));

            // ----- CỘT 1: DANH SÁCH GIẢI THƯỞNG -----
            var pnlList = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(12), AccentTopColor = ThemeColors.PrimaryBlue };
            var lblListTitle = new Label { Dock = DockStyle.Top, Height = 28, Font = ThemeColors.FontSubTitle, ForeColor = ThemeColors.NavyDark, Text = "Danh sách Vinh danh" };

            dgvWinners = new StyledDataGridView { Dock = DockStyle.Fill };
            dgvWinners.Columns.Add(new DataGridViewTextBoxColumn { Name = "TT", HeaderText = "TT", FillWeight = 10 });
            dgvWinners.Columns.Add(new DataGridViewTextBoxColumn { Name = "Giai", HeaderText = "Giải thưởng", FillWeight = 38 });
            dgvWinners.Columns.Add(new DataGridViewTextBoxColumn { Name = "HoTen", HeaderText = "Họ tên / Đội", FillWeight = 34 });
            dgvWinners.Columns.Add(new DataGridViewTextBoxColumn { Name = "Lop", HeaderText = "Lớp", FillWeight = 18 });
            dgvWinners.SelectionChanged += (_, _) => PopulateFromSelected();

            var flpOrder = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(0, 6, 0, 0) };
            var btnAddNew = new ModernButton { Text = "+ Thêm mới", Size = new Size(105, 34), Margin = new Padding(0, 0, 6, 0) };
            btnAddNew.Click += (_, _) => ResetForm();
            var btnUp = new ModernButton { Text = "Lên", Size = new Size(64, 34), Margin = new Padding(0, 0, 6, 0), NormalBackColor = ThemeColors.RoyalBlue, HoverBackColor = ThemeColors.NavyDark };
            btnUp.Click += (_, _) => MoveWinnerOrder(-1);
            var btnDown = new ModernButton { Text = "Xuống", Size = new Size(72, 34), Margin = new Padding(0, 0, 6, 0), NormalBackColor = ThemeColors.RoyalBlue, HoverBackColor = ThemeColors.NavyDark };
            btnDown.Click += (_, _) => MoveWinnerOrder(1);
            var btnDel = new ModernButton { Text = "Xóa", Size = new Size(70, 34), NormalBackColor = ThemeColors.DangerRed, HoverBackColor = Color.FromArgb(185, 28, 28) };
            btnDel.Click += (_, _) => DeleteSelected();
            flpOrder.Controls.AddRange(new Control[] { btnAddNew, btnUp, btnDown, btnDel });

            pnlList.Controls.Add(dgvWinners);
            pnlList.Controls.Add(flpOrder);
            pnlList.Controls.Add(lblListTitle);

            // ----- CỘT 2: NHẬP THÔNG TIN & CHỈNH ẢNH -----
            var pnlForm = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(14), AccentTopColor = ThemeColors.Gold, AutoScroll = true };
            int y = 8;
            pnlForm.Controls.Add(new Label { Text = "Thông tin người / đội nhận giải", Location = new Point(14, y), AutoSize = true, Font = ThemeColors.FontSubTitle, ForeColor = ThemeColors.NavyDark });
            y += 34;

            picWinner = new PictureBox { Location = new Point(14, y), Size = new Size(110, 130), BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom, BackColor = ThemeColors.NavyDark };
            var btnPickImg = new ModernButton { Text = "Chọn & Cắt ảnh...", Location = new Point(136, y + 8), Size = new Size(150, 34) };
            btnPickImg.Click += (_, _) => ChooseAndCropImage();
            var btnClearImg = new ModernButton { Text = "Xóa ảnh", Location = new Point(136, y + 48), Size = new Size(150, 34), NormalBackColor = ThemeColors.DangerRed, HoverBackColor = Color.FromArgb(185, 28, 28) };
            btnClearImg.Click += (_, _) => ClearImage();
            pnlForm.Controls.AddRange(new Control[] { picWinner, btnPickImg, btnClearImg });
            y += 140;

            pnlForm.Controls.Add(new Label { Text = "Giải thưởng (*):", Location = new Point(14, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            cboGiaiThuong = new ComboBox { Location = new Point(14, y + 22), Width = 385, DropDownStyle = ComboBoxStyle.DropDown };
            cboGiaiThuong.Items.AddRange(WinnerService.DefaultAwardTitles);
            cboGiaiThuong.SelectedIndex = 0;
            pnlForm.Controls.Add(cboGiaiThuong);
            y += 56;

            txtHoTen = AddBox(pnlForm, "Họ tên / Tên người chiến thắng (*):", ref y, 385);

            pnlForm.Controls.Add(new Label { Text = "Số báo danh / Mã đội:", Location = new Point(14, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            pnlForm.Controls.Add(new Label { Text = "Lớp:", Location = new Point(205, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            txtSbd = new TextBox { Location = new Point(14, y + 22), Width = 175 };
            txtLop = new TextBox { Location = new Point(205, y + 22), Width = 194 };
            pnlForm.Controls.AddRange(new Control[] { txtSbd, txtLop });
            y += 56;

            txtTruong = AddBox(pnlForm, "Tên trường / Đơn vị:", ref y, 385);
            txtDoi = AddBox(pnlForm, "Tên đội (nếu thi theo đội):", ref y, 385);
            txtThanhTich = AddBox(pnlForm, "Thành tích:", ref y, 385);

            pnlForm.Controls.Add(new Label { Text = "Số câu đúng:", Location = new Point(14, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            pnlForm.Controls.Add(new Label { Text = "Số câu vượt qua:", Location = new Point(205, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            numDung = new NumericUpDown { Location = new Point(14, y + 22), Width = 175, Minimum = 0, Maximum = 500 };
            numVuotQua = new NumericUpDown { Location = new Point(205, y + 22), Width = 194, Minimum = 0, Maximum = 500 };
            pnlForm.Controls.AddRange(new Control[] { numDung, numVuotQua });
            y += 56;

            txtGhiChu = AddBox(pnlForm, "Lời chúc mừng / Thành viên / Ghi chú:", ref y, 385, multi: true);

            var btnSaveConfirm = new ModernButton { Text = "Xác nhận & Lưu thông tin", Location = new Point(14, y + 4), Size = new Size(210, 38), NormalBackColor = ThemeColors.SuccessGreen, HoverBackColor = Color.FromArgb(16, 130, 58) };
            btnSaveConfirm.Click += (_, _) => SaveWinnerInfo();
            var btnPreviewLocal = new ModernButton { Text = "Xem trước", Location = new Point(234, y + 4), Size = new Size(165, 38), NormalBackColor = ThemeColors.InfoCyan, HoverBackColor = Color.FromArgb(14, 116, 144) };
            btnPreviewLocal.Click += (_, _) => UpdatePreviewFromDraft();
            pnlForm.Controls.AddRange(new Control[] { btnSaveConfirm, btnPreviewLocal });

            // ----- CỘT 3: XEM TRƯỚC & ĐIỀU KHIỂN TRÌNH CHIẾU VINH DANH -----
            var pnlControl = new RoundedPanel { Dock = DockStyle.Fill, Padding = new Padding(12), AccentTopColor = ThemeColors.NavyDark };
            var lblPrevTitle = new Label { Dock = DockStyle.Top, Height = 28, Font = ThemeColors.FontSubTitle, ForeColor = ThemeColors.NavyDark, Text = "Xem trước & Điều khiển Trình chiếu" };

            previewCanvas = new PresentationStageControl { Dock = DockStyle.Top, Height = 250, EnableAnimation = false };
            previewCanvas.BindState(_previewState);

            var flpProjBtns = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0, 14, 0, 0), AutoScroll = true };

            var btnProjectNow = new ModernButton { Text = "XÁC NHẬN & TRÌNH CHIẾU LÊN MÁY CHIẾU", Size = new Size(390, 46), Margin = new Padding(0, 0, 0, 10), NormalBackColor = ThemeColors.Gold, HoverBackColor = ThemeColors.GoldBright, ForeColor = ThemeColors.NavyDark };
            btnProjectNow.Click += (_, _) => ProjectSelectedWinner();

            var btnNextAward = new ModernButton { Text = "Giải thưởng tiếp theo >>", Size = new Size(390, 40), Margin = new Padding(0, 0, 0, 8), NormalBackColor = ThemeColors.PrimaryBlue, HoverBackColor = ThemeColors.PrimaryBlueHover };
            btnNextAward.Click += (_, _) => NavigateWinnerAndProject(1);

            var btnPrevAward = new ModernButton { Text = "<< Giải thưởng trước", Size = new Size(390, 40), Margin = new Padding(0, 0, 0, 8), NormalBackColor = ThemeColors.RoyalBlue, HoverBackColor = ThemeColors.NavyDark };
            btnPrevAward.Click += (_, _) => NavigateWinnerAndProject(-1);

            var btnEndSpotlight = new ModernButton { Text = "Kết thúc vinh danh (Về màn hình chờ)", Size = new Size(390, 40), NormalBackColor = ThemeColors.DangerRed, HoverBackColor = Color.FromArgb(185, 28, 28) };
            btnEndSpotlight.Click += (_, _) => _stateManager.ReturnToStandby();

            flpProjBtns.Controls.AddRange(new Control[] { btnProjectNow, btnNextAward, btnPrevAward, btnEndSpotlight });

            pnlControl.Controls.Add(flpProjBtns);
            pnlControl.Controls.Add(previewCanvas);
            pnlControl.Controls.Add(lblPrevTitle);

            tlpBody.Controls.Add(pnlList, 0, 0);
            tlpBody.Controls.Add(pnlForm, 1, 0);
            tlpBody.Controls.Add(pnlControl, 2, 0);

            tlpRoot.Controls.Add(pnlHeader, 0, 0);
            tlpRoot.Controls.Add(tlpBody, 0, 1);
            Controls.Add(tlpRoot);

            FormClosing += (_, _) =>
            {
                if (_ownsServices)
                {
                    _presService.ClosePresentation();
                    _stateManager.Dispose();
                }
                _previewState.Dispose();
            };
        }

        private static TextBox AddBox(Control parent, string label, ref int y, int width, bool multi = false)
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(14, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            var tb = new TextBox { Location = new Point(14, y + 22), Width = width, Multiline = multi, Height = multi ? 54 : 28 };
            parent.Controls.Add(tb);
            y += multi ? 84 : 56;
            return tb;
        }

        private void LoadPrograms()
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

        private void OnProgramChanged()
        {
            if (cboProgram.SelectedItem is not ChuongTrinh ct) return;
            _currentProgram = ct;
            if (_ownsServices)
            {
                _stateManager.LoadProgram(ct.ChuongTrinhID);
            }
            _previewState.LoadProgram(ct.ChuongTrinhID);
            LoadWinnersList();
        }

        private void LoadWinnersList()
        {
            if (_currentProgram == null) return;
            _winners = _vdRepo.GetByProgram(_currentProgram.ChuongTrinhID);
            dgvWinners.Rows.Clear();
            foreach (var w in _winners)
            {
                dgvWinners.Rows.Add(w.ThuTuTrinhChieu, w.TenGiaiThuong, w.HoTen, w.TenLop);
            }
        }

        private void PopulateFromSelected()
        {
            if (dgvWinners.CurrentRow == null || dgvWinners.CurrentRow.Index < 0 || dgvWinners.CurrentRow.Index >= _winners.Count) return;
            var w = _winners[dgvWinners.CurrentRow.Index];
            _editingId = w.VinhDanhID;
            cboGiaiThuong.Text = w.TenGiaiThuong;
            txtHoTen.Text = w.HoTen;
            txtSbd.Text = w.SoBaoDanh;
            txtLop.Text = w.TenLop;
            txtTruong.Text = w.TenTruong;
            txtDoi.Text = w.TenDoi;
            txtThanhTich.Text = w.ThanhTich;
            numDung.Value = Math.Clamp(w.SoCauDung, 0, 500);
            numVuotQua.Value = Math.Clamp(w.SoCauVuotQua, 0, 500);
            txtGhiChu.Text = w.GhiChu;
            _currentImgRelPath = w.DuongDanHinhAnh;

            picWinner.Image?.Dispose();
            picWinner.Image = ImageHelper.LoadImageSafe(_currentImgRelPath);

            UpdatePreviewFromDraft();
        }

        private void ResetForm()
        {
            _editingId = 0;
            cboGiaiThuong.SelectedIndex = 0;
            txtHoTen.Clear();
            txtSbd.Clear();
            txtLop.Clear();
            txtTruong.Clear();
            txtDoi.Clear();
            txtThanhTich.Text = "Người chiến thắng Rung Chuông Vàng";
            numDung.Value = 0;
            numVuotQua.Value = 0;
            txtGhiChu.Text = "CHÚC MỪNG BẠN ĐÃ XUẤT SẮC VƯỢT QUA CÁC THỬ THÁCH!";
            _currentImgRelPath = string.Empty;
            picWinner.Image?.Dispose();
            picWinner.Image = null;
        }

        private void ChooseAndCropImage()
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Chọn hình ảnh người/đội chiến thắng",
                Filter = "Hình ảnh (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp"
            };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            using var cropDlg = new FrmCropImageDialog(ofd.FileName);
            if (cropDlg.ShowDialog() == DialogResult.OK)
            {
                _currentImgRelPath = cropDlg.SavedRelativePath;
                picWinner.Image?.Dispose();
                picWinner.Image = ImageHelper.LoadImageSafe(_currentImgRelPath);
                UpdatePreviewFromDraft();
            }
        }

        private void ClearImage()
        {
            _currentImgRelPath = string.Empty;
            picWinner.Image?.Dispose();
            picWinner.Image = null;
            UpdatePreviewFromDraft();
        }

        private VinhDanh BuildDraftModel()
        {
            return new VinhDanh
            {
                VinhDanhID = _editingId,
                ChuongTrinhID = _currentProgram?.ChuongTrinhID ?? 0,
                HoTen = txtHoTen.Text.Trim(),
                SoBaoDanh = txtSbd.Text.Trim(),
                TenLop = txtLop.Text.Trim(),
                TenTruong = txtTruong.Text.Trim(),
                TenDoi = txtDoi.Text.Trim(),
                TenGiaiThuong = cboGiaiThuong.Text.Trim(),
                ThanhTich = txtThanhTich.Text.Trim(),
                SoCauDung = (int)numDung.Value,
                SoCauVuotQua = (int)numVuotQua.Value,
                DuongDanHinhAnh = _currentImgRelPath,
                ThuTuTrinhChieu = _winners.Count + 1,
                NgayVinhDanh = DateTime.Now,
                GhiChu = txtGhiChu.Text.Trim()
            };
        }

        private void UpdatePreviewFromDraft()
        {
            if (_currentProgram == null) return;
            _previewState.ShowSpotlightWinner(BuildDraftModel());
        }

        private void SaveWinnerInfo()
        {
            if (_currentProgram == null) return;
            if (string.IsNullOrWhiteSpace(txtHoTen.Text) || string.IsNullOrWhiteSpace(cboGiaiThuong.Text))
            {
                MessageBox.Show("Vui lòng nhập Họ tên / Tên đội và Giải thưởng.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var model = BuildDraftModel();
            if (_editingId == 0)
            {
                _vdRepo.Add(model);
            }
            else
            {
                var existing = _winners.FirstOrDefault(x => x.VinhDanhID == _editingId);
                if (existing != null)
                {
                    model.ThiSinhID = existing.ThiSinhID;
                    model.DoiThiID = existing.DoiThiID;
                    model.ThuTuTrinhChieu = existing.ThuTuTrinhChieu;
                }
                _vdRepo.Update(model);
            }

            LoadWinnersList();
            MessageBox.Show("Đã xác nhận và lưu thông tin vinh danh.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AutoPopulateFromResults()
        {
            if (_currentProgram == null) return;
            var candidates = _winnerService.BuildAutoCandidatesFromResults(_currentProgram.ChuongTrinhID);
            if (candidates.Count == 0)
            {
                MessageBox.Show("Chương trình chưa có dữ liệu thí sinh/đội để tự động lấy.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (var c in candidates)
            {
                _vdRepo.Add(c);
            }

            LoadWinnersList();
            MessageBox.Show($"Đã tự động tạo {candidates.Count} mục vinh danh từ kết quả thi. Bạn có thể chọn từng mục để chỉnh sửa và thêm ảnh trước khi trình chiếu.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void MoveWinnerOrder(int delta)
        {
            if (dgvWinners.CurrentRow == null) return;
            int idx = dgvWinners.CurrentRow.Index;
            int targetIdx = idx + delta;
            if (idx < 0 || targetIdx < 0 || targetIdx >= _winners.Count) return;

            _vdRepo.SwapOrder(_winners[idx], _winners[targetIdx]);
            LoadWinnersList();
            dgvWinners.ClearSelection();
            dgvWinners.Rows[targetIdx].Selected = true;
        }

        private void DeleteSelected()
        {
            if (dgvWinners.CurrentRow == null || dgvWinners.CurrentRow.Index < 0 || dgvWinners.CurrentRow.Index >= _winners.Count) return;
            var w = _winners[dgvWinners.CurrentRow.Index];
            if (MessageBox.Show($"Xác nhận xóa mục vinh danh '{w.HoTen}'?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            _vdRepo.Delete(w.VinhDanhID);
            ResetForm();
            LoadWinnersList();
        }

        private void OpenSpotlightScreen()
        {
            _presService.OpenOrUpdatePresentation(_stateManager);
        }

        private void ProjectSelectedWinner()
        {
            if (dgvWinners.CurrentRow == null || dgvWinners.CurrentRow.Index < 0 || dgvWinners.CurrentRow.Index >= _winners.Count)
            {
                MessageBox.Show("Vui lòng lưu và chọn một mục trong Danh sách Vinh danh bên trái để trình chiếu.", "Chưa chọn giải thưởng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_presService.IsPresentationOpen)
            {
                _presService.OpenOrUpdatePresentation(_stateManager);
            }

            var w = _winners[dgvWinners.CurrentRow.Index];
            _stateManager.ShowSpotlightWinner(w);
        }

        private void NavigateWinnerAndProject(int delta)
        {
            if (_winners.Count == 0) return;
            int cur = dgvWinners.CurrentRow?.Index ?? 0;
            int next = Math.Clamp(cur + delta, 0, _winners.Count - 1);
            dgvWinners.ClearSelection();
            dgvWinners.Rows[next].Selected = true;
            dgvWinners.CurrentCell = dgvWinners.Rows[next].Cells[0];
            ProjectSelectedWinner();
        }

        private void ExportExcel()
        {
            if (_currentProgram == null || _winners.Count == 0) return;
            using var sfd = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"DanhSachVinhDanh_{_currentProgram.MaChuongTrinh}.xlsx"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                _winnerService.ExportWinnersToExcel(_currentProgram, _winners, sfd.FileName);
                MessageBox.Show("Đã xuất danh sách vinh danh ra Excel.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
