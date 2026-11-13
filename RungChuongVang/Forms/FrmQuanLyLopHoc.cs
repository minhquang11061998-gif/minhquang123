using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RungChuongVang.Controls;
using RungChuongVang.DataAccess;
using RungChuongVang.Helpers;
using RungChuongVang.Models;

namespace RungChuongVang.Forms
{
    /// <summary>
    /// Quản lý danh mục Lớp học (Thêm, sửa, xóa an toàn, tìm kiếm, kích hoạt/ngừng sử dụng).
    /// </summary>
    public class FrmQuanLyLopHoc : Form
    {
        private readonly DanhMucRepository _repo = new();
        private List<LopHoc> _currentList = new();
        private int _editingId;

        private TextBox txtSearch = null!;
        private CheckBox chkShowInactive = null!;
        private StyledDataGridView dgv = null!;

        private TextBox txtMaLop = null!;
        private TextBox txtTenLop = null!;
        private NumericUpDown numKhoi = null!;
        private TextBox txtTenTruong = null!;
        private TextBox txtGhiChu = null!;
        private CheckBox chkTrangThai = null!;
        private Label lblFormMode = null!;
        private ModernButton btnSave = null!;

        public FrmQuanLyLopHoc()
        {
            BuildUi();
            LoadData();
        }

        private void BuildUi()
        {
            BackColor = ThemeColors.AppBackground;
            Padding = new Padding(20);
            Font = ThemeColors.FontBody;

            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));

            // Trái: Bảng danh sách lớp
            var pnlLeft = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0), Padding = new Padding(16) };
            var pnlFilter = new Panel { Dock = DockStyle.Top, Height = 48 };

            txtSearch = new TextBox { Location = new Point(0, 8), Width = 240, PlaceholderText = "Tìm mã lớp, tên lớp, trường..." };
            txtSearch.TextChanged += (_, _) => LoadData();

            chkShowInactive = new CheckBox { Location = new Point(256, 10), AutoSize = true, Text = "Hiện cả lớp ngừng dùng" };
            chkShowInactive.CheckedChanged += (_, _) => LoadData();

            var flpActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 250,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 4, 0, 0)
            };

            var btnDelete = new ModernButton
            {
                Text = "Xóa",
                Size = new Size(95, 34),
                NormalBackColor = ThemeColors.DangerRed,
                HoverBackColor = Color.FromArgb(185, 28, 28)
            };
            btnDelete.Click += (_, _) => DeleteSelected();

            var btnToggle = new ModernButton
            {
                Text = "Bật/Ngừng dùng",
                Size = new Size(140, 34),
                Margin = new Padding(0, 0, 8, 0),
                NormalBackColor = ThemeColors.WarningOrange,
                HoverBackColor = Color.FromArgb(180, 95, 5)
            };
            btnToggle.Click += (_, _) => ToggleSelectedStatus();

            flpActions.Controls.Add(btnDelete);
            flpActions.Controls.Add(btnToggle);
            pnlFilter.Controls.AddRange(new Control[] { txtSearch, chkShowInactive, flpActions });

            dgv = new StyledDataGridView { Dock = DockStyle.Fill };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "STT", HeaderText = "STT", FillWeight = 8 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "MaLop", HeaderText = "Mã lớp", FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TenLop", HeaderText = "Tên lớp", FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "KhoiLop", HeaderText = "Khối", FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TenTruong", HeaderText = "Trường", FillWeight = 28 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TrangThai", HeaderText = "Trạng thái", FillWeight = 16 });
            dgv.SelectionChanged += (_, _) => PopulateFromSelectedRow();

            pnlLeft.Controls.Add(dgv);
            pnlLeft.Controls.Add(pnlFilter);

            // Phải: Form nhập liệu
            var pnlRight = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                AccentTopColor = ThemeColors.Gold,
                Padding = new Padding(20)
            };

            lblFormMode = new Label
            {
                Dock = DockStyle.Top,
                Height = 36,
                Font = ThemeColors.FontSubTitle,
                ForeColor = ThemeColors.NavyDark,
                Text = "Thêm lớp học mới"
            };

            var formFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 8, 0, 0)
            };

            txtMaLop = AddLabeledTextBox(formFlow, "Mã lớp (*):", 340);
            txtTenLop = AddLabeledTextBox(formFlow, "Tên lớp (*):", 340);

            formFlow.Controls.Add(new Label { Text = "Khối lớp:", AutoSize = true, Margin = new Padding(0, 8, 0, 4), Font = ThemeColors.FontBodyBold });
            numKhoi = new NumericUpDown { Minimum = 1, Maximum = 12, Value = 5, Width = 140 };
            formFlow.Controls.Add(numKhoi);

            txtTenTruong = AddLabeledTextBox(formFlow, "Tên trường:", 340);
            txtGhiChu = AddLabeledTextBox(formFlow, "Ghi chú:", 340, multiline: true);

            chkTrangThai = new CheckBox { Text = "Đang sử dụng", Checked = true, AutoSize = true, Margin = new Padding(0, 12, 0, 12) };
            formFlow.Controls.Add(chkTrangThai);

            var btnRow = new FlowLayoutPanel { Width = 350, Height = 46, FlowDirection = FlowDirection.LeftToRight };
            btnSave = new ModernButton { Text = "Lưu lớp học", Size = new Size(155, 38), Margin = new Padding(0, 0, 10, 0) };
            btnSave.Click += (_, _) => SaveItem();

            var btnReset = new ModernButton
            {
                Text = "Làm mới / Thêm mới",
                Size = new Size(175, 38),
                NormalBackColor = ThemeColors.TextMuted,
                HoverBackColor = ThemeColors.TextPrimary
            };
            btnReset.Click += (_, _) => ResetForm();

            btnRow.Controls.Add(btnSave);
            btnRow.Controls.Add(btnReset);
            formFlow.Controls.Add(btnRow);

            pnlRight.Controls.Add(formFlow);
            pnlRight.Controls.Add(lblFormMode);

            tlp.Controls.Add(pnlLeft, 0, 0);
            tlp.Controls.Add(pnlRight, 1, 0);
            Controls.Add(tlp);
        }

        private static TextBox AddLabeledTextBox(FlowLayoutPanel parent, string label, int width, bool multiline = false)
        {
            parent.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 8, 0, 4), Font = ThemeColors.FontBodyBold });
            var tb = new TextBox
            {
                Width = width,
                Multiline = multiline,
                Height = multiline ? 70 : 28
            };
            parent.Controls.Add(tb);
            return tb;
        }

        private void LoadData()
        {
            try
            {
                bool? activeFilter = chkShowInactive.Checked ? null : true;
                _currentList = _repo.GetLopHoc(txtSearch.Text, activeFilter);

                dgv.Rows.Clear();
                for (int i = 0; i < _currentList.Count; i++)
                {
                    var item = _currentList[i];
                    dgv.Rows.Add(
                        i + 1,
                        item.MaLop,
                        item.TenLop,
                        item.KhoiLop,
                        item.TenTruong,
                        item.TrangThai ? "Đang sử dụng" : "Ngừng sử dụng");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách lớp: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PopulateFromSelectedRow()
        {
            if (dgv.CurrentRow == null || dgv.CurrentRow.Index < 0 || dgv.CurrentRow.Index >= _currentList.Count)
            {
                return;
            }

            var item = _currentList[dgv.CurrentRow.Index];
            _editingId = item.LopHocID;
            lblFormMode.Text = $"Cập nhật lớp: {item.TenLop}";
            txtMaLop.Text = item.MaLop;
            txtTenLop.Text = item.TenLop;
            numKhoi.Value = Math.Clamp(item.KhoiLop, 1, 12);
            txtTenTruong.Text = item.TenTruong;
            txtGhiChu.Text = item.GhiChu;
            chkTrangThai.Checked = item.TrangThai;
        }

        private void ResetForm()
        {
            _editingId = 0;
            lblFormMode.Text = "Thêm lớp học mới";
            txtMaLop.Clear();
            txtTenLop.Clear();
            numKhoi.Value = 5;
            txtTenTruong.Clear();
            txtGhiChu.Clear();
            chkTrangThai.Checked = true;
            txtMaLop.Focus();
        }

        private void SaveItem()
        {
            string ma = txtMaLop.Text.Trim();
            string ten = txtTenLop.Text.Trim();

            if (string.IsNullOrWhiteSpace(ma) || string.IsNullOrWhiteSpace(ten))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ Mã lớp và Tên lớp.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (_repo.ExistsMaLop(ma, _editingId))
                {
                    MessageBox.Show("Mã lớp đã tồn tại. Vui lòng nhập mã khác.", "Trùng mã", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var model = new LopHoc
                {
                    LopHocID = _editingId,
                    MaLop = ma,
                    TenLop = ten,
                    KhoiLop = (int)numKhoi.Value,
                    TenTruong = txtTenTruong.Text.Trim(),
                    GhiChu = txtGhiChu.Text.Trim(),
                    TrangThai = chkTrangThai.Checked
                };

                if (_editingId == 0)
                {
                    _repo.AddLopHoc(model);
                    MessageBox.Show("Đã thêm lớp học mới thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    _repo.UpdateLopHoc(model);
                    MessageBox.Show("Đã cập nhật thông tin lớp học.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                ResetForm();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu lớp học: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ToggleSelectedStatus()
        {
            if (dgv.CurrentRow == null || dgv.CurrentRow.Index < 0 || dgv.CurrentRow.Index >= _currentList.Count) return;
            var item = _currentList[dgv.CurrentRow.Index];
            _repo.SetLopHocActive(item.LopHocID, !item.TrangThai);
            LoadData();
        }

        private void DeleteSelected()
        {
            if (dgv.CurrentRow == null || dgv.CurrentRow.Index < 0 || dgv.CurrentRow.Index >= _currentList.Count) return;
            var item = _currentList[dgv.CurrentRow.Index];
            if (MessageBox.Show($"Bạn có chắc muốn xóa lớp '{item.TenLop}'?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            _repo.DeleteLopHoc(item.LopHocID, out string msg);
            MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ResetForm();
            LoadData();
        }
    }
}
