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
    /// Form Thêm / Sửa / Xem chi tiết Câu hỏi với đầy đủ 3 loại câu hỏi, nhiều lớp học và hình ảnh minh họa.
    /// </summary>
    public class FrmThemSuaCauHoi : Form
    {
        private readonly CauHoiRepository _cauHoiRepo = new();
        private readonly DanhMucRepository _danhMucRepo = new();
        private readonly int _cauHoiId;
        private readonly bool _readOnlyMode;

        private string _currentImageRelPath = string.Empty;
        private bool _imageRemoved;
        private string? _pendingLocalImagePath;

        private TextBox txtMaCauHoi = null!;
        private ComboBox cboLoaiCauHoi = null!;
        private ComboBox cboMonHoc = null!;
        private ComboBox cboChuDe = null!;
        private ComboBox cboMucDo = null!;
        private NumericUpDown numThoiGian = null!;
        private CheckBox chkTrangThai = null!;
        private TextBox txtNoiDung = null!;
        private TextBox txtGiaiThich = null!;
        private CheckedListBox clbLopHoc = null!;
        private PictureBox picHinhAnh = null!;

        private Panel pnlDapAnContainer = null!;
        private TextBox txtTuLuanDapAn = null!;
        private readonly List<(TextBox TxtNoiDung, RadioButton RbDung, CheckBox ChkDung, string KyHieu)> _optionControls = new();

        private ModernButton btnSave = null!;

        public FrmThemSuaCauHoi(int cauHoiId = 0, bool readOnlyMode = false)
        {
            _cauHoiId = cauHoiId;
            _readOnlyMode = readOnlyMode;
            BuildUi();
            LoadReferenceLists();

            if (_cauHoiId > 0)
            {
                LoadExistingQuestion(_cauHoiId);
            }
            else
            {
                txtMaCauHoi.Text = _cauHoiRepo.GenerateNextMaCauHoi();
                SwitchAnswerModeUI();
            }

            if (_readOnlyMode)
            {
                ApplyReadOnlyMode();
            }
        }

        private void BuildUi()
        {
            Text = _readOnlyMode ? "Chi tiết câu hỏi" : (_cauHoiId > 0 ? "Chỉnh sửa câu hỏi" : "Thêm câu hỏi mới");
            Size = new Size(1060, 740);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = ThemeColors.AppBackground;
            Font = ThemeColors.FontBody;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            var tlpMain = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(16) };
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64F));
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));

            // ===== CỘT TRÁI: THÔNG TIN CHÍNH & ĐÁP ÁN =====
            var pnlLeft = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0), Padding = new Padding(16), AutoScroll = true };
            int y = 8;

            pnlLeft.Controls.Add(new Label { Text = "Mã câu hỏi:", Location = new Point(16, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            pnlLeft.Controls.Add(new Label { Text = "Loại câu hỏi (*):", Location = new Point(185, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            pnlLeft.Controls.Add(new Label { Text = "Thời gian (s):", Location = new Point(470, y), AutoSize = true, Font = ThemeColors.FontBodyBold });

            txtMaCauHoi = new TextBox { Location = new Point(16, y + 22), Width = 155 };
            cboLoaiCauHoi = new ComboBox { Location = new Point(185, y + 22), Width = 270, DropDownStyle = ComboBoxStyle.DropDownList };
            cboLoaiCauHoi.SelectedIndexChanged += (_, _) => SwitchAnswerModeUI();
            numThoiGian = new NumericUpDown { Location = new Point(470, y + 22), Width = 110, Minimum = 5, Maximum = 300, Value = 15 };
            pnlLeft.Controls.AddRange(new Control[] { txtMaCauHoi, cboLoaiCauHoi, numThoiGian });

            y += 62;
            pnlLeft.Controls.Add(new Label { Text = "Môn học:", Location = new Point(16, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            pnlLeft.Controls.Add(new Label { Text = "Chủ đề:", Location = new Point(220, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            pnlLeft.Controls.Add(new Label { Text = "Mức độ khó (*):", Location = new Point(430, y), AutoSize = true, Font = ThemeColors.FontBodyBold });

            cboMonHoc = new ComboBox { Location = new Point(16, y + 22), Width = 190, DropDownStyle = ComboBoxStyle.DropDownList };
            cboMonHoc.SelectedIndexChanged += (_, _) => LoadTopicsForSelectedSubject();
            cboChuDe = new ComboBox { Location = new Point(220, y + 22), Width = 195, DropDownStyle = ComboBoxStyle.DropDownList };
            cboMucDo = new ComboBox { Location = new Point(430, y + 22), Width = 165, DropDownStyle = ComboBoxStyle.DropDownList };
            pnlLeft.Controls.AddRange(new Control[] { cboMonHoc, cboChuDe, cboMucDo });

            y += 62;
            pnlLeft.Controls.Add(new Label { Text = "Nội dung câu hỏi (*):", Location = new Point(16, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            txtNoiDung = new TextBox { Location = new Point(16, y + 22), Width = 580, Height = 85, Multiline = true, ScrollBars = ScrollBars.Vertical };
            pnlLeft.Controls.Add(txtNoiDung);

            y += 118;
            pnlLeft.Controls.Add(new Label { Text = "Phương án & Đáp án đúng (*):", Location = new Point(16, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            pnlDapAnContainer = new Panel { Location = new Point(16, y + 24), Width = 580, Height = 205, BorderStyle = BorderStyle.FixedSingle, BackColor = ThemeColors.AppBackground };
            BuildAnswerControls();
            pnlLeft.Controls.Add(pnlDapAnContainer);

            y += 240;
            pnlLeft.Controls.Add(new Label { Text = "Giải thích đáp án:", Location = new Point(16, y), AutoSize = true, Font = ThemeColors.FontBodyBold });
            txtGiaiThich = new TextBox { Location = new Point(16, y + 22), Width = 580, Height = 58, Multiline = true, ScrollBars = ScrollBars.Vertical };
            pnlLeft.Controls.Add(txtGiaiThich);

            chkTrangThai = new CheckBox { Text = "Đang sử dụng trong ngân hàng", Checked = true, Location = new Point(16, y + 88), AutoSize = true };
            pnlLeft.Controls.Add(chkTrangThai);

            // ===== CỘT PHẢI: LỚP ÁP DỤNG & HÌNH ẢNH =====
            var pnlRight = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0), Padding = new Padding(16) };

            var lblLop = new Label { Text = "Áp dụng cho Lớp học (*):", Dock = DockStyle.Top, Height = 26, Font = ThemeColors.FontBodyBold };
            clbLopHoc = new CheckedListBox { Dock = DockStyle.Top, Height = 210, CheckOnClick = true };

            var lblImg = new Label { Text = "Hình ảnh minh họa (JPG/PNG/WEBP):", Dock = DockStyle.Top, Height = 30, Padding = new Padding(0, 8, 0, 0), Font = ThemeColors.FontBodyBold };
            picHinhAnh = new PictureBox
            {
                Dock = DockStyle.Top,
                Height = 220,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = ThemeColors.AppBackground
            };

            var pnlImgBtns = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(0, 8, 0, 0) };
            var btnChooseImg = new ModernButton { Text = "Chọn ảnh...", Size = new Size(140, 34), Margin = new Padding(0, 0, 8, 0) };
            btnChooseImg.Click += (_, _) => ChooseImage();
            var btnClearImg = new ModernButton { Text = "Xóa ảnh", Size = new Size(105, 34), NormalBackColor = ThemeColors.DangerRed, HoverBackColor = Color.FromArgb(185, 28, 28) };
            btnClearImg.Click += (_, _) => ClearImage();
            pnlImgBtns.Controls.Add(btnChooseImg);
            pnlImgBtns.Controls.Add(btnClearImg);

            pnlRight.Controls.Add(pnlImgBtns);
            pnlRight.Controls.Add(picHinhAnh);
            pnlRight.Controls.Add(lblImg);
            pnlRight.Controls.Add(clbLopHoc);
            pnlRight.Controls.Add(lblLop);

            // ===== HÀNG NÚT DƯỚI =====
            var pnlBottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 10, 0, 0) };
            var btnCancel = new ModernButton { Text = "Đóng", Size = new Size(110, 38), NormalBackColor = ThemeColors.TextMuted, HoverBackColor = ThemeColors.TextPrimary };
            btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

            btnSave = new ModernButton { Text = "Lưu câu hỏi", Size = new Size(150, 38), Margin = new Padding(0, 0, 10, 0) };
            btnSave.Click += (_, _) => SaveQuestion();

            pnlBottom.Controls.Add(btnCancel);
            pnlBottom.Controls.Add(btnSave);

            tlpMain.Controls.Add(pnlLeft, 0, 0);
            tlpMain.Controls.Add(pnlRight, 1, 0);
            tlpMain.Controls.Add(pnlBottom, 0, 1);
            tlpMain.SetColumnSpan(pnlBottom, 2);
            Controls.Add(tlpMain);
        }

        private void BuildAnswerControls()
        {
            _optionControls.Clear();
            string[] keys = { "A", "B", "C", "D" };
            for (int i = 0; i < keys.Length; i++)
            {
                int rowY = 12 + i * 46;
                var lbl = new Label { Text = $"{keys[i]}.", Location = new Point(12, rowY + 4), Width = 24, Font = ThemeColors.FontBodyBold };
                var txt = new TextBox { Location = new Point(40, rowY), Width = 410 };
                var rb = new RadioButton { Text = "Đúng", Location = new Point(462, rowY + 2), Width = 75, AutoSize = true };
                var chk = new CheckBox { Text = "Đúng", Location = new Point(462, rowY + 2), Width = 75, AutoSize = true, Visible = false };

                // Chỉ cho chọn 1 RadioButton khi ở chế độ 1 đáp án
                int idx = i;
                rb.CheckedChanged += (_, _) =>
                {
                    if (rb.Checked)
                    {
                        for (int j = 0; j < _optionControls.Count; j++)
                        {
                            if (j != idx) _optionControls[j].RbDung.Checked = false;
                        }
                    }
                };

                _optionControls.Add((txt, rb, chk, keys[i]));
                pnlDapAnContainer.Controls.AddRange(new Control[] { lbl, txt, rb, chk });
            }

            txtTuLuanDapAn = new TextBox
            {
                Location = new Point(14, 42),
                Width = 546,
                Height = 120,
                Multiline = true,
                Visible = false,
                PlaceholderText = "Nhập đáp án chuẩn để MC đối chiếu..."
            };
            pnlDapAnContainer.Controls.Add(txtTuLuanDapAn);
        }

        private void LoadReferenceLists()
        {
            cboLoaiCauHoi.DataSource = new List<KeyValuePair<LoaiCauHoi, string>>
            {
                new(LoaiCauHoi.TracNghiemMotDapAn, "Trắc nghiệm một đáp án đúng"),
                new(LoaiCauHoi.TracNghiemNhieuDapAn, "Trắc nghiệm nhiều đáp án đúng"),
                new(LoaiCauHoi.TuLuanTraLoiNgan, "Tự luận / Trả lời ngắn")
            };
            cboLoaiCauHoi.DisplayMember = "Value";
            cboLoaiCauHoi.ValueMember = "Key";

            var mucDoMap = _danhMucRepo.GetMucDoConfig();
            cboMucDo.DataSource = mucDoMap.Select(kv => new KeyValuePair<MucDoKho, string>(kv.Key, kv.Value.Ten)).ToList();
            cboMucDo.DisplayMember = "Value";
            cboMucDo.ValueMember = "Key";

            var subjects = _danhMucRepo.GetMonHoc(onlyActive: true);
            subjects.Insert(0, new MonHoc { MonHocID = 0, TenMonHoc = "-- Chọn môn học --" });
            cboMonHoc.DataSource = subjects;
            cboMonHoc.DisplayMember = "TenMonHoc";
            cboMonHoc.ValueMember = "MonHocID";

            var classes = _danhMucRepo.GetLopHoc(onlyActive: true);
            clbLopHoc.Items.Clear();
            foreach (var c in classes)
            {
                clbLopHoc.Items.Add(new ClassCheckItem(c.LopHocID, c.TenLop), false);
            }
        }

        private void LoadTopicsForSelectedSubject()
        {
            int? monId = cboMonHoc.SelectedValue is int id && id > 0 ? id : null;
            var topics = _danhMucRepo.GetChuDe(monId, onlyActive: true);
            topics.Insert(0, new ChuDe { ChuDeID = 0, TenChuDe = "-- Chọn chủ đề --" });
            cboChuDe.DataSource = topics;
            cboChuDe.DisplayMember = "TenChuDe";
            cboChuDe.ValueMember = "ChuDeID";
        }

        private void SwitchAnswerModeUI()
        {
            if (cboLoaiCauHoi.SelectedValue is not LoaiCauHoi loai) return;

            bool isEssay = loai == LoaiCauHoi.TuLuanTraLoiNgan;
            bool isMulti = loai == LoaiCauHoi.TracNghiemNhieuDapAn;

            txtTuLuanDapAn.Visible = isEssay;
            foreach (var ctrl in _optionControls)
            {
                ctrl.TxtNoiDung.Visible = !isEssay;
                ctrl.RbDung.Visible = !isEssay && !isMulti;
                ctrl.ChkDung.Visible = !isEssay && isMulti;
            }
        }

        private void LoadExistingQuestion(int id)
        {
            CauHoi? q = _cauHoiRepo.GetById(id);
            if (q == null)
            {
                MessageBox.Show("Không tìm thấy câu hỏi.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            txtMaCauHoi.Text = q.MaCauHoi;
            cboLoaiCauHoi.SelectedValue = q.LoaiCauHoi;
            numThoiGian.Value = Math.Clamp(q.ThoiGianTraLoi, 5, 300);
            cboMonHoc.SelectedValue = q.MonHocID ?? 0;
            LoadTopicsForSelectedSubject();
            cboChuDe.SelectedValue = q.ChuDeID ?? 0;
            cboMucDo.SelectedValue = q.MucDo;
            txtNoiDung.Text = q.NoiDung;
            txtGiaiThich.Text = q.GiaiThich;
            chkTrangThai.Checked = q.TrangThai;

            for (int i = 0; i < clbLopHoc.Items.Count; i++)
            {
                if (clbLopHoc.Items[i] is ClassCheckItem ci)
                {
                    clbLopHoc.SetItemChecked(i, q.LopHocIds.Contains(ci.LopHocID));
                }
            }

            SwitchAnswerModeUI();
            if (q.LoaiCauHoi == LoaiCauHoi.TuLuanTraLoiNgan)
            {
                txtTuLuanDapAn.Text = q.DanhSachDapAn.FirstOrDefault()?.NoiDungDapAn ?? string.Empty;
            }
            else
            {
                foreach (var opt in _optionControls)
                {
                    var matched = q.DanhSachDapAn.FirstOrDefault(d => d.KyHieu.Equals(opt.KyHieu, StringComparison.OrdinalIgnoreCase));
                    if (matched != null)
                    {
                        opt.TxtNoiDung.Text = matched.NoiDungDapAn;
                        opt.RbDung.Checked = matched.LaDapAnDung;
                        opt.ChkDung.Checked = matched.LaDapAnDung;
                    }
                }
            }

            _currentImageRelPath = q.DuongDanHinhAnh;
            picHinhAnh.Image = ImageHelper.LoadImageSafe(_currentImageRelPath);
        }

        private void ChooseImage()
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Chọn hình ảnh minh họa câu hỏi",
                Filter = "Hình ảnh (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                _pendingLocalImagePath = ofd.FileName;
                _imageRemoved = false;
                picHinhAnh.Image?.Dispose();
                picHinhAnh.Image = ImageHelper.LoadImageSafe(ofd.FileName);
            }
        }

        private void ClearImage()
        {
            _pendingLocalImagePath = null;
            _imageRemoved = true;
            picHinhAnh.Image?.Dispose();
            picHinhAnh.Image = null;
        }

        private void SaveQuestion()
        {
            string ma = txtMaCauHoi.Text.Trim();
            string noiDung = txtNoiDung.Text.Trim();
            if (string.IsNullOrWhiteSpace(ma) || string.IsNullOrWhiteSpace(noiDung))
            {
                MessageBox.Show("Vui lòng nhập Mã câu hỏi và Nội dung câu hỏi.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_cauHoiRepo.ExistsMaCauHoi(ma, _cauHoiId))
            {
                MessageBox.Show("Mã câu hỏi đã tồn tại.", "Trùng mã", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var lopIds = new List<int>();
            foreach (object item in clbLopHoc.CheckedItems)
            {
                if (item is ClassCheckItem ci) lopIds.Add(ci.LopHocID);
            }
            if (lopIds.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một Lớp học áp dụng.", "Thiếu lớp học", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var loai = (LoaiCauHoi)cboLoaiCauHoi.SelectedValue!;
            var answers = new List<DapAn>();

            if (loai == LoaiCauHoi.TuLuanTraLoiNgan)
            {
                if (string.IsNullOrWhiteSpace(txtTuLuanDapAn.Text))
                {
                    MessageBox.Show("Vui lòng nhập đáp án chuẩn cho câu hỏi tự luận.", "Thiếu đáp án", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                answers.Add(new DapAn
                {
                    KyHieu = "DA",
                    NoiDungDapAn = txtTuLuanDapAn.Text.Trim(),
                    LaDapAnDung = true,
                    ThuTu = 1
                });
            }
            else
            {
                int thuTu = 1;
                foreach (var opt in _optionControls)
                {
                    if (string.IsNullOrWhiteSpace(opt.TxtNoiDung.Text)) continue;
                    bool isCorrect = loai == LoaiCauHoi.TracNghiemMotDapAn ? opt.RbDung.Checked : opt.ChkDung.Checked;
                    answers.Add(new DapAn
                    {
                        KyHieu = opt.KyHieu,
                        NoiDungDapAn = opt.TxtNoiDung.Text.Trim(),
                        LaDapAnDung = isCorrect,
                        ThuTu = thuTu++
                    });
                }

                if (answers.Count < 2)
                {
                    MessageBox.Show("Câu hỏi trắc nghiệm phải có ít nhất 2 phương án trả lời.", "Thiếu phương án", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int correctCount = answers.Count(a => a.LaDapAnDung);
                if (loai == LoaiCauHoi.TracNghiemMotDapAn && correctCount != 1)
                {
                    MessageBox.Show("Vui lòng chọn duy nhất 1 đáp án đúng.", "Chưa chọn đáp án đúng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (loai == LoaiCauHoi.TracNghiemNhieuDapAn && correctCount < 1)
                {
                    MessageBox.Show("Vui lòng chọn ít nhất 1 đáp án đúng cho câu hỏi nhiều lựa chọn.", "Chưa chọn đáp án đúng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            try
            {
                string finalImgPath = _imageRemoved ? string.Empty : _currentImageRelPath;
                if (!string.IsNullOrWhiteSpace(_pendingLocalImagePath))
                {
                    finalImgPath = ImageHelper.CopyImageToDataFolder(_pendingLocalImagePath, FileHelper.QuestionImagesDirectory, "Q");
                }

                var model = new CauHoi
                {
                    CauHoiID = _cauHoiId,
                    MaCauHoi = ma,
                    NoiDung = noiDung,
                    MonHocID = cboMonHoc.SelectedValue is int mId && mId > 0 ? mId : null,
                    ChuDeID = cboChuDe.SelectedValue is int cId && cId > 0 ? cId : null,
                    MucDo = (MucDoKho)cboMucDo.SelectedValue!,
                    LoaiCauHoi = loai,
                    GiaiThich = txtGiaiThich.Text.Trim(),
                    ThoiGianTraLoi = (int)numThoiGian.Value,
                    DuongDanHinhAnh = finalImgPath,
                    TrangThai = chkTrangThai.Checked,
                    LopHocIds = lopIds,
                    DanhSachDapAn = answers
                };

                if (_cauHoiId == 0) _cauHoiRepo.AddCauHoi(model);
                else _cauHoiRepo.UpdateCauHoi(model);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu câu hỏi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyReadOnlyMode()
        {
            txtMaCauHoi.ReadOnly = true;
            txtNoiDung.ReadOnly = true;
            txtGiaiThich.ReadOnly = true;
            txtTuLuanDapAn.ReadOnly = true;
            cboLoaiCauHoi.Enabled = false;
            cboMonHoc.Enabled = false;
            cboChuDe.Enabled = false;
            cboMucDo.Enabled = false;
            numThoiGian.Enabled = false;
            chkTrangThai.Enabled = false;
            clbLopHoc.Enabled = false;
            btnSave.Visible = false;
        }

        private sealed class ClassCheckItem
        {
            public int LopHocID { get; }
            public string TenLop { get; }
            public ClassCheckItem(int id, string name) { LopHocID = id; TenLop = name; }
            public override string ToString() => TenLop;
        }
    }
}
