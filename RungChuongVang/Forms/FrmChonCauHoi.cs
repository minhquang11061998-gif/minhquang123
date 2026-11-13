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
    /// Tạo bộ câu hỏi thi theo 2 chế độ: Chọn thủ công và Trộn ngẫu nhiên theo tỷ lệ mức độ khó.
    /// Hỗ trợ xem trước thứ tự, sắp xếp, lưu và cố định (khóa) bộ câu hỏi.
    /// </summary>
    public class FrmChonCauHoi : Form
    {
        private readonly ChuongTrinhRepository _ctRepo = new();
        private readonly CauHoiRepository _chRepo = new();
        private readonly DanhMucRepository _dmRepo = new();
        private readonly QuestionRandomService _randomService = new();

        private readonly int _initialProgramId;
        private ChuongTrinh? _currentProgram;
        private List<ChuongTrinhCauHoi> _selectedItems = new();
        private List<CauHoi> _manualBank = new();
        private Dictionary<MucDoKho, (string Ten, Color Mau)> _mucDoConfig = new();

        private ComboBox cboProgram = null!;
        private Label lblLockStatus = null!;

        // Tab Trộn ngẫu nhiên
        private CheckedListBox clbRandomClasses = null!;
        private CheckedListBox clbRandomSubjects = null!;
        private CheckedListBox clbRandomTopics = null!;
        private NumericUpDown numTotalQuestions = null!;
        private NumericUpDown numPct1 = null!;
        private NumericUpDown numPct2 = null!;
        private NumericUpDown numPct3 = null!;
        private NumericUpDown numPct4 = null!;
        private CheckBox chkAutoFill = null!;
        private Label lblPercentSum = null!;

        // Tab Chọn thủ công
        private TextBox txtManualSearch = null!;
        private ComboBox cboManualClass = null!;
        private ComboBox cboManualSubject = null!;
        private StyledDataGridView dgvManualBank = null!;

        // Bảng xem trước bộ câu hỏi đã chọn
        private StyledDataGridView dgvSelected = null!;
        private Label lblSelectedCount = null!;
        private ModernButton btnSaveSet = null!;
        private ModernButton btnLockSet = null!;

        public FrmChonCauHoi(int programId = 0)
        {
            _initialProgramId = programId;
            BuildUi();
            LoadReferenceLists();
            LoadProgramsList();
        }

        private void BuildUi()
        {
            Text = "Tạo bộ câu hỏi thi";
            BackColor = ThemeColors.AppBackground;
            Padding = new Padding(18);
            Font = ThemeColors.FontBody;
            if (_initialProgramId > 0)
            {
                Size = new Size(1280, 780);
                StartPosition = FormStartPosition.CenterParent;
            }

            var tlpRoot = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Header chọn chương trình
            var pnlHeader = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 12), Padding = new Padding(14, 10, 14, 10) };
            pnlHeader.Controls.Add(new Label { Text = "Chọn chương trình:", Location = new Point(14, 15), AutoSize = true, Font = ThemeColors.FontBodyBold });
            cboProgram = new ComboBox { Location = new Point(150, 11), Width = 440, DropDownStyle = ComboBoxStyle.DropDownList };
            cboProgram.SelectedIndexChanged += (_, _) => OnProgramChanged();
            lblLockStatus = new Label { Location = new Point(610, 15), Width = 420, Font = ThemeColors.FontBodyBold, ForeColor = ThemeColors.PrimaryBlue };
            pnlHeader.Controls.AddRange(new Control[] { cboProgram, lblLockStatus });

            // Nội dung 2 cột: Trái (Tab Trộn / Thủ công) - Phải (Danh sách bộ câu hỏi chương trình)
            var tlpBody = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            tlpBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var tabs = new TabControl { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0) };
            var tabRandom = new TabPage("Trộn ngẫu nhiên theo tỷ lệ") { BackColor = ThemeColors.SurfaceWhite, Padding = new Padding(14) };
            var tabManual = new TabPage("Chọn thủ công từ ngân hàng") { BackColor = ThemeColors.SurfaceWhite, Padding = new Padding(14) };
            BuildRandomTab(tabRandom);
            BuildManualTab(tabManual);
            tabs.TabPages.Add(tabRandom);
            tabs.TabPages.Add(tabManual);

            // Phải: Xem trước & Sắp xếp bộ câu hỏi
            var pnlRight = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0), Padding = new Padding(14), AccentTopColor = ThemeColors.Gold };
            lblSelectedCount = new Label { Dock = DockStyle.Top, Height = 32, Font = ThemeColors.FontSubTitle, ForeColor = ThemeColors.NavyDark, Text = "Bộ câu hỏi của chương trình (0 câu)" };

            dgvSelected = new StyledDataGridView { Dock = DockStyle.Fill };
            dgvSelected.Columns.Add(new DataGridViewTextBoxColumn { Name = "ThuTu", HeaderText = "STT", FillWeight = 8 });
            dgvSelected.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ma", HeaderText = "Mã", FillWeight = 13 });
            dgvSelected.Columns.Add(new DataGridViewTextBoxColumn { Name = "NoiDung", HeaderText = "Nội dung câu hỏi", FillWeight = 45 });
            dgvSelected.Columns.Add(new DataGridViewTextBoxColumn { Name = "MucDo", HeaderText = "Mức độ", FillWeight = 18 });
            dgvSelected.Columns.Add(new DataGridViewTextBoxColumn { Name = "ThoiGian", HeaderText = "Giây", FillWeight = 10 });
            dgvSelected.CellDoubleClick += (_, _) => PreviewSelectedQuestion();

            var pnlActions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 92, Padding = new Padding(0, 8, 0, 0) };
            var btnUp = new ModernButton { Text = "Lên", Size = new Size(74, 34), Margin = new Padding(0, 0, 6, 6), NormalBackColor = ThemeColors.RoyalBlue, HoverBackColor = ThemeColors.NavyDark };
            btnUp.Click += (_, _) => MoveSelected(-1);
            var btnDown = new ModernButton { Text = "Xuống", Size = new Size(80, 34), Margin = new Padding(0, 0, 6, 6), NormalBackColor = ThemeColors.RoyalBlue, HoverBackColor = ThemeColors.NavyDark };
            btnDown.Click += (_, _) => MoveSelected(1);
            var btnViewQ = new ModernButton { Text = "Xem trước", Size = new Size(105, 34), Margin = new Padding(0, 0, 6, 6), NormalBackColor = ThemeColors.InfoCyan, HoverBackColor = Color.FromArgb(14, 116, 144) };
            btnViewQ.Click += (_, _) => PreviewSelectedQuestion();
            var btnRemove = new ModernButton { Text = "Bỏ câu", Size = new Size(88, 34), Margin = new Padding(0, 0, 6, 6), NormalBackColor = ThemeColors.DangerRed, HoverBackColor = Color.FromArgb(185, 28, 28) };
            btnRemove.Click += (_, _) => RemoveSelectedQuestion();
            var btnClear = new ModernButton { Text = "Xóa hết", Size = new Size(88, 34), Margin = new Padding(0, 0, 6, 6), NormalBackColor = ThemeColors.TextMuted, HoverBackColor = ThemeColors.TextPrimary };
            btnClear.Click += (_, _) => ClearSelectedQuestions();

            btnSaveSet = new ModernButton { Text = "Lưu bộ câu hỏi", Size = new Size(165, 38), Margin = new Padding(0, 2, 8, 0), NormalBackColor = ThemeColors.SuccessGreen, HoverBackColor = Color.FromArgb(16, 130, 58) };
            btnSaveSet.Click += (_, _) => SaveSet(lockAfter: false);
            btnLockSet = new ModernButton { Text = "Lưu & Cố định (Khóa)", Size = new Size(190, 38), Margin = new Padding(0, 2, 0, 0), NormalBackColor = ThemeColors.Gold, HoverBackColor = ThemeColors.GoldBright, ForeColor = ThemeColors.NavyDark };
            btnLockSet.Click += (_, _) => SaveSet(lockAfter: true);

            pnlActions.Controls.AddRange(new Control[] { btnUp, btnDown, btnViewQ, btnRemove, btnClear, btnSaveSet, btnLockSet });

            pnlRight.Controls.Add(dgvSelected);
            pnlRight.Controls.Add(pnlActions);
            pnlRight.Controls.Add(lblSelectedCount);

            tlpBody.Controls.Add(tabs, 0, 0);
            tlpBody.Controls.Add(pnlRight, 1, 0);

            tlpRoot.Controls.Add(pnlHeader, 0, 0);
            tlpRoot.Controls.Add(tlpBody, 0, 1);
            Controls.Add(tlpRoot);
        }

        private void BuildRandomTab(TabPage tab)
        {
            tab.AutoScroll = true;
            int y = 6;

            tab.Controls.Add(new Label { Text = "1. Phạm vi Lớp học (không chọn = tất cả):", Location = new Point(10, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            tab.Controls.Add(new Label { Text = "2. Môn học:", Location = new Point(280, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            clbRandomClasses = new CheckedListBox { Location = new Point(10, y + 22), Width = 255, Height = 135, CheckOnClick = true };
            clbRandomSubjects = new CheckedListBox { Location = new Point(280, y + 22), Width = 265, Height = 135, CheckOnClick = true };
            tab.Controls.AddRange(new Control[] { clbRandomClasses, clbRandomSubjects });
            y += 168;

            tab.Controls.Add(new Label { Text = "3. Chủ đề (tùy chọn):", Location = new Point(10, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            clbRandomTopics = new CheckedListBox { Location = new Point(10, y + 22), Width = 535, Height = 105, CheckOnClick = true };
            tab.Controls.Add(clbRandomTopics);
            y += 138;

            tab.Controls.Add(new Label { Text = "4. Tổng số câu hỏi:", Location = new Point(10, y + 4), AutoSize = true, Font = ThemeColors.FontBodyBold });
            numTotalQuestions = new NumericUpDown { Location = new Point(165, y), Width = 100, Minimum = 1, Maximum = 200, Value = 20 };
            tab.Controls.Add(numTotalQuestions);
            y += 38;

            tab.Controls.Add(new Label { Text = "5. Tỷ lệ từng mức độ khó (Tổng = 100%):", Location = new Point(10, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            y += 26;

            numPct1 = AddPctRow(tab, "Nhận biết (%):", 20, 10, y);
            numPct2 = AddPctRow(tab, "Thông hiểu (%):", 30, 280, y);
            y += 38;
            numPct3 = AddPctRow(tab, "Vận dụng (%):", 30, 10, y);
            numPct4 = AddPctRow(tab, "Vận dụng cao (%):", 20, 280, y);
            y += 40;

            lblPercentSum = new Label { Location = new Point(10, y), Width = 260, Font = ThemeColors.FontBodyBold, ForeColor = ThemeColors.SuccessGreen, Text = "Tổng tỷ lệ: 100%" };
            chkAutoFill = new CheckBox { Location = new Point(280, y), AutoSize = true, Text = "Tự động bù từ mức độ khác nếu thiếu" };
            tab.Controls.AddRange(new Control[] { lblPercentSum, chkAutoFill });
            y += 36;

            var btnMix = new ModernButton { Text = "Trộn câu hỏi ngẫu nhiên", Location = new Point(10, y), Size = new Size(220, 40) };
            btnMix.Click += (_, _) => RunRandomSelection();

            var btnRemix = new ModernButton { Text = "Trộn lại", Location = new Point(242, y), Size = new Size(125, 40), NormalBackColor = ThemeColors.Gold, HoverBackColor = ThemeColors.GoldBright, ForeColor = ThemeColors.NavyDark };
            btnRemix.Click += (_, _) => RunRandomSelection();

            tab.Controls.AddRange(new Control[] { btnMix, btnRemix });
        }

        private NumericUpDown AddPctRow(Control parent, string label, int defaultVal, int x, int y)
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(x, y + 4), AutoSize = true });
            var num = new NumericUpDown { Location = new Point(x + 145, y), Width = 85, Minimum = 0, Maximum = 100, Value = defaultVal };
            num.ValueChanged += (_, _) => UpdatePercentSum();
            parent.Controls.Add(num);
            return num;
        }

        private void BuildManualTab(TabPage tab)
        {
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 82 };
            txtManualSearch = new TextBox { Location = new Point(0, 4), Width = 210, PlaceholderText = "Tìm câu hỏi..." };
            txtManualSearch.TextChanged += (_, _) => LoadManualBank();

            cboManualClass = new ComboBox { Location = new Point(218, 4), Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            cboManualClass.SelectedIndexChanged += (_, _) => LoadManualBank();

            cboManualSubject = new ComboBox { Location = new Point(366, 4), Width = 155, DropDownStyle = ComboBoxStyle.DropDownList };
            cboManualSubject.SelectedIndexChanged += (_, _) => LoadManualBank();

            var btnAddManual = new ModernButton { Text = "+ Thêm câu đã chọn vào bộ đề", Location = new Point(0, 40), Size = new Size(240, 34) };
            btnAddManual.Click += (_, _) => AddSelectedManualQuestion();

            pnlTop.Controls.AddRange(new Control[] { txtManualSearch, cboManualClass, cboManualSubject, btnAddManual });

            dgvManualBank = new StyledDataGridView { Dock = DockStyle.Fill };
            dgvManualBank.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ma", HeaderText = "Mã", FillWeight = 16 });
            dgvManualBank.Columns.Add(new DataGridViewTextBoxColumn { Name = "NoiDung", HeaderText = "Nội dung", FillWeight = 52 });
            dgvManualBank.Columns.Add(new DataGridViewTextBoxColumn { Name = "Lop", HeaderText = "Lớp", FillWeight = 16 });
            dgvManualBank.Columns.Add(new DataGridViewTextBoxColumn { Name = "MucDo", HeaderText = "Mức độ", FillWeight = 16 });
            dgvManualBank.CellDoubleClick += (_, _) => AddSelectedManualQuestion();

            tab.Controls.Add(dgvManualBank);
            tab.Controls.Add(pnlTop);
        }

        private void LoadReferenceLists()
        {
            _mucDoConfig = _dmRepo.GetMucDoConfig();

            var classes = _dmRepo.GetLopHoc(onlyActive: true);
            clbRandomClasses.Items.Clear();
            foreach (var c in classes) clbRandomClasses.Items.Add(new IdNameItem(c.LopHocID, c.TenLop));

            var subjects = _dmRepo.GetMonHoc(onlyActive: true);
            clbRandomSubjects.Items.Clear();
            foreach (var s in subjects) clbRandomSubjects.Items.Add(new IdNameItem(s.MonHocID, s.TenMonHoc));

            var topics = _dmRepo.GetChuDe(onlyActive: true);
            clbRandomTopics.Items.Clear();
            foreach (var t in topics) clbRandomTopics.Items.Add(new IdNameItem(t.ChuDeID, t.TenChuDe));

            var clsFilter = new List<LopHoc>(classes);
            clsFilter.Insert(0, new LopHoc { LopHocID = 0, TenLop = "Tất cả lớp" });
            cboManualClass.DataSource = clsFilter;
            cboManualClass.DisplayMember = "TenLop";
            cboManualClass.ValueMember = "LopHocID";

            var subFilter = new List<MonHoc>(subjects);
            subFilter.Insert(0, new MonHoc { MonHocID = 0, TenMonHoc = "Tất cả môn" });
            cboManualSubject.DataSource = subFilter;
            cboManualSubject.DisplayMember = "TenMonHoc";
            cboManualSubject.ValueMember = "MonHocID";

            LoadManualBank();
        }

        private void LoadProgramsList()
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
            _currentProgram = _ctRepo.GetById(ct.ChuongTrinhID);
            if (_currentProgram == null) return;

            bool locked = _currentProgram.DaKhoaBoCauHoi || _currentProgram.TrangThai != TrangThaiChuongTrinhEnum.ChuaBatDau;
            lblLockStatus.Text = locked
                ? "Trạng thái: ĐÃ CỐ ĐỊNH BỘ CÂU HỎI (Chỉ xem)"
                : "Trạng thái: Đang mở cấu hình bộ câu hỏi";
            lblLockStatus.ForeColor = locked ? ThemeColors.DangerRed : ThemeColors.SuccessGreen;
            btnSaveSet.Enabled = !locked;
            btnLockSet.Enabled = !locked;

            _selectedItems = _ctRepo.GetProgramQuestions(_currentProgram.ChuongTrinhID, laCauHoiPhu: false);
            RefreshSelectedGrid();
        }

        private void LoadManualBank()
        {
            if (cboManualClass.SelectedValue == null || cboManualSubject.SelectedValue == null) return;
            int? lopId = cboManualClass.SelectedValue is int l && l > 0 ? l : null;
            int? monId = cboManualSubject.SelectedValue is int m && m > 0 ? m : null;

            _manualBank = _chRepo.SearchCauHoi(txtManualSearch.Text, lopId, monId, trangThai: true);
            dgvManualBank.Rows.Clear();
            foreach (var q in _manualBank)
            {
                string md = _mucDoConfig.TryGetValue(q.MucDo, out var info) ? info.Ten : q.MucDo.ToString();
                dgvManualBank.Rows.Add(q.MaCauHoi, q.NoiDung, q.DanhSachTenLop, md);
            }
        }

        private void UpdatePercentSum()
        {
            int sum = (int)(numPct1.Value + numPct2.Value + numPct3.Value + numPct4.Value);
            lblPercentSum.Text = $"Tổng tỷ lệ: {sum}%";
            lblPercentSum.ForeColor = sum == 100 ? ThemeColors.SuccessGreen : ThemeColors.DangerRed;
        }

        private bool EnsureUnlocked()
        {
            if (_currentProgram == null)
            {
                MessageBox.Show("Chưa có chương trình nào được chọn.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (_currentProgram.DaKhoaBoCauHoi || _currentProgram.TrangThai != TrangThaiChuongTrinhEnum.ChuaBatDau)
            {
                MessageBox.Show("Bộ câu hỏi đã bị khóa hoặc chương trình đã bắt đầu nên không thể thay đổi.", "Đã khóa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void RunRandomSelection()
        {
            if (!EnsureUnlocked()) return;

            var req = new QuestionRandomRequest
            {
                LopHocIds = clbRandomClasses.CheckedItems.OfType<IdNameItem>().Select(x => x.Id).ToList(),
                MonHocIds = clbRandomSubjects.CheckedItems.OfType<IdNameItem>().Select(x => x.Id).ToList(),
                ChuDeIds = clbRandomTopics.CheckedItems.OfType<IdNameItem>().Select(x => x.Id).ToList(),
                TotalQuestions = (int)numTotalQuestions.Value,
                AutoFillIfShortage = chkAutoFill.Checked,
                PercentageByLevel = new Dictionary<MucDoKho, int>
                {
                    [MucDoKho.NhanBiet] = (int)numPct1.Value,
                    [MucDoKho.ThongHieu] = (int)numPct2.Value,
                    [MucDoKho.VanDung] = (int)numPct3.Value,
                    [MucDoKho.VanDungCao] = (int)numPct4.Value
                }
            };

            var res = _randomService.GenerateRandomQuestions(req);
            if (!res.IsSuccess)
            {
                MessageBox.Show(res.Message, "Không thể trộn câu hỏi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _selectedItems.Clear();
            int order = 1;
            foreach (var q in res.SelectedQuestions)
            {
                _selectedItems.Add(new ChuongTrinhCauHoi
                {
                    ChuongTrinhID = _currentProgram!.ChuongTrinhID,
                    CauHoiID = q.CauHoiID,
                    ThuTu = order++,
                    ThoiGianTraLoi = q.ThoiGianTraLoi > 0 ? q.ThoiGianTraLoi : _currentProgram.ThoiGianMacDinh,
                    CauHoiChiTiet = q
                });
            }

            RefreshSelectedGrid();
            MessageBox.Show(res.Message, "Trộn câu hỏi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AddSelectedManualQuestion()
        {
            if (!EnsureUnlocked()) return;
            if (dgvManualBank.CurrentRow == null || dgvManualBank.CurrentRow.Index < 0 || dgvManualBank.CurrentRow.Index >= _manualBank.Count) return;

            var q = _manualBank[dgvManualBank.CurrentRow.Index];
            if (_selectedItems.Any(x => x.CauHoiID == q.CauHoiID))
            {
                MessageBox.Show("Câu hỏi này đã có trong bộ đề.", "Trùng câu hỏi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _selectedItems.Add(new ChuongTrinhCauHoi
            {
                ChuongTrinhID = _currentProgram!.ChuongTrinhID,
                CauHoiID = q.CauHoiID,
                ThuTu = _selectedItems.Count + 1,
                ThoiGianTraLoi = q.ThoiGianTraLoi > 0 ? q.ThoiGianTraLoi : _currentProgram.ThoiGianMacDinh,
                CauHoiChiTiet = q
            });
            RefreshSelectedGrid();
        }

        private void RefreshSelectedGrid()
        {
            dgvSelected.Rows.Clear();
            for (int i = 0; i < _selectedItems.Count; i++)
            {
                var item = _selectedItems[i];
                item.ThuTu = i + 1;
                var q = item.CauHoiChiTiet;
                string md = q != null && _mucDoConfig.TryGetValue(q.MucDo, out var info) ? info.Ten : string.Empty;
                dgvSelected.Rows.Add(
                    item.ThuTu,
                    q?.MaCauHoi ?? string.Empty,
                    q?.NoiDung ?? string.Empty,
                    md,
                    item.ThoiGianTraLoi);
            }
            lblSelectedCount.Text = $"Bộ câu hỏi của chương trình ({_selectedItems.Count} câu)";
        }

        private void MoveSelected(int delta)
        {
            if (!EnsureUnlocked() || dgvSelected.CurrentRow == null) return;
            int idx = dgvSelected.CurrentRow.Index;
            int newIdx = idx + delta;
            if (idx < 0 || newIdx < 0 || newIdx >= _selectedItems.Count) return;

            (_selectedItems[idx], _selectedItems[newIdx]) = (_selectedItems[newIdx], _selectedItems[idx]);
            RefreshSelectedGrid();
            dgvSelected.ClearSelection();
            dgvSelected.Rows[newIdx].Selected = true;
            dgvSelected.CurrentCell = dgvSelected.Rows[newIdx].Cells[0];
        }

        private void RemoveSelectedQuestion()
        {
            if (!EnsureUnlocked() || dgvSelected.CurrentRow == null) return;
            int idx = dgvSelected.CurrentRow.Index;
            if (idx >= 0 && idx < _selectedItems.Count)
            {
                _selectedItems.RemoveAt(idx);
                RefreshSelectedGrid();
            }
        }

        private void ClearSelectedQuestions()
        {
            if (!EnsureUnlocked()) return;
            if (MessageBox.Show("Xóa toàn bộ câu hỏi đang chọn khỏi bộ đề?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _selectedItems.Clear();
                RefreshSelectedGrid();
            }
        }

        private void PreviewSelectedQuestion()
        {
            if (dgvSelected.CurrentRow == null || dgvSelected.CurrentRow.Index < 0 || dgvSelected.CurrentRow.Index >= _selectedItems.Count) return;
            int qId = _selectedItems[dgvSelected.CurrentRow.Index].CauHoiID;
            using var frm = new FrmThemSuaCauHoi(qId, readOnlyMode: true);
            frm.ShowDialog();
        }

        private void SaveSet(bool lockAfter)
        {
            if (!EnsureUnlocked()) return;
            if (_selectedItems.Count == 0)
            {
                MessageBox.Show("Bộ câu hỏi chưa có câu nào.", "Thiếu câu hỏi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (lockAfter && MessageBox.Show(
                "Sau khi cố định (khóa), thứ tự và danh sách câu hỏi sẽ không thể thay đổi để đảm bảo an toàn khi tổ chức thi.\nBạn có chắc chắn muốn lưu và cố định?",
                "Xác nhận cố định bộ câu hỏi",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                _ctRepo.SaveProgramQuestions(_currentProgram!.ChuongTrinhID, _selectedItems, lockAfter);
                MessageBox.Show(lockAfter ? "Đã lưu và cố định bộ câu hỏi." : "Đã lưu bộ câu hỏi.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                OnProgramChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu bộ câu hỏi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private sealed class IdNameItem
        {
            public int Id { get; }
            public string Name { get; }
            public IdNameItem(int id, string name) { Id = id; Name = name; }
            public override string ToString() => Name;
        }
    }
}
