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
    /// Quản lý Môn học và Chủ đề câu hỏi trên hai khu vực song song.
    /// </summary>
    public class FrmQuanLyMonHocChuDe : Form
    {
        private readonly DanhMucRepository _repo = new();
        private List<MonHoc> _subjects = new();
        private List<ChuDe> _topics = new();
        private int _editingSubjectId;
        private int _editingTopicId;

        private StyledDataGridView dgvSubjects = null!;
        private TextBox txtSubName = null!;
        private TextBox txtSubDesc = null!;
        private CheckBox chkSubActive = null!;

        private StyledDataGridView dgvTopics = null!;
        private ComboBox cboTopicSubject = null!;
        private TextBox txtTopicName = null!;
        private TextBox txtTopicDesc = null!;
        private CheckBox chkTopicActive = null!;

        public FrmQuanLyMonHocChuDe()
        {
            BuildUi();
            LoadSubjects();
            LoadTopics();
        }

        private void BuildUi()
        {
            BackColor = ThemeColors.AppBackground;
            Padding = new Padding(20);
            Font = ThemeColors.FontBody;

            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));

            // ===== TRÁI: MÔN HỌC =====
            var pnlSub = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0), Padding = new Padding(16), AccentTopColor = ThemeColors.PrimaryBlue };
            var lblSubHeader = new Label { Dock = DockStyle.Top, Height = 32, Font = ThemeColors.FontSubTitle, ForeColor = ThemeColors.NavyDark, Text = "Danh mục Môn học" };

            var pnlSubForm = new Panel { Dock = DockStyle.Bottom, Height = 195, Padding = new Padding(0, 10, 0, 0) };
            pnlSubForm.Controls.Add(new Label { Text = "Tên môn học (*):", Location = new Point(0, 8), AutoSize = true, Font = ThemeColors.FontBodyBold });
            txtSubName = new TextBox { Location = new Point(0, 30), Width = 340 };
            pnlSubForm.Controls.Add(txtSubName);

            pnlSubForm.Controls.Add(new Label { Text = "Mô tả:", Location = new Point(0, 64), AutoSize = true, Font = ThemeColors.FontBodyBold });
            txtSubDesc = new TextBox { Location = new Point(0, 86), Width = 340, Height = 48, Multiline = true };
            pnlSubForm.Controls.Add(txtSubDesc);

            chkSubActive = new CheckBox { Text = "Đang sử dụng", Checked = true, Location = new Point(360, 32), AutoSize = true };
            pnlSubForm.Controls.Add(chkSubActive);

            var btnSubSave = new ModernButton { Text = "Lưu môn", Size = new Size(110, 36), Location = new Point(0, 146) };
            btnSubSave.Click += (_, _) => SaveSubject();
            var btnSubNew = new ModernButton { Text = "Thêm mới", Size = new Size(110, 36), Location = new Point(118, 146), NormalBackColor = ThemeColors.TextMuted, HoverBackColor = ThemeColors.TextPrimary };
            btnSubNew.Click += (_, _) => ResetSubjectForm();
            var btnSubDel = new ModernButton { Text = "Xóa môn", Size = new Size(105, 36), Location = new Point(236, 146), NormalBackColor = ThemeColors.DangerRed, HoverBackColor = Color.FromArgb(185, 28, 28) };
            btnSubDel.Click += (_, _) => DeleteSubject();
            pnlSubForm.Controls.AddRange(new Control[] { btnSubSave, btnSubNew, btnSubDel });

            dgvSubjects = new StyledDataGridView { Dock = DockStyle.Fill };
            dgvSubjects.Columns.Add(new DataGridViewTextBoxColumn { Name = "STT", HeaderText = "STT", FillWeight = 12 });
            dgvSubjects.Columns.Add(new DataGridViewTextBoxColumn { Name = "TenMonHoc", HeaderText = "Tên môn học", FillWeight = 58 });
            dgvSubjects.Columns.Add(new DataGridViewTextBoxColumn { Name = "TrangThai", HeaderText = "Trạng thái", FillWeight = 30 });
            dgvSubjects.SelectionChanged += (_, _) =>
            {
                if (dgvSubjects.CurrentRow != null && dgvSubjects.CurrentRow.Index >= 0 && dgvSubjects.CurrentRow.Index < _subjects.Count)
                {
                    var s = _subjects[dgvSubjects.CurrentRow.Index];
                    _editingSubjectId = s.MonHocID;
                    txtSubName.Text = s.TenMonHoc;
                    txtSubDesc.Text = s.MoTa;
                    chkSubActive.Checked = s.TrangThai;
                    LoadTopics(s.MonHocID);
                }
            };

            pnlSub.Controls.Add(dgvSubjects);
            pnlSub.Controls.Add(pnlSubForm);
            pnlSub.Controls.Add(lblSubHeader);

            // ===== PHẢI: CHỦ ĐỀ =====
            var pnlTopic = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(10, 0, 0, 0), Padding = new Padding(16), AccentTopColor = ThemeColors.Gold };
            var lblTopicHeader = new Label { Dock = DockStyle.Top, Height = 32, Font = ThemeColors.FontSubTitle, ForeColor = ThemeColors.NavyDark, Text = "Danh mục Chủ đề (theo Môn học)" };

            var pnlTopicForm = new Panel { Dock = DockStyle.Bottom, Height = 235, Padding = new Padding(0, 10, 0, 0) };
            pnlTopicForm.Controls.Add(new Label { Text = "Thuộc môn học:", Location = new Point(0, 4), AutoSize = true, Font = ThemeColors.FontBodyBold });
            cboTopicSubject = new ComboBox { Location = new Point(0, 26), Width = 340, DropDownStyle = ComboBoxStyle.DropDownList };
            pnlTopicForm.Controls.Add(cboTopicSubject);

            pnlTopicForm.Controls.Add(new Label { Text = "Tên chủ đề (*):", Location = new Point(0, 60), AutoSize = true, Font = ThemeColors.FontBodyBold });
            txtTopicName = new TextBox { Location = new Point(0, 82), Width = 340 };
            pnlTopicForm.Controls.Add(txtTopicName);

            pnlTopicForm.Controls.Add(new Label { Text = "Mô tả:", Location = new Point(0, 114), AutoSize = true, Font = ThemeColors.FontBodyBold });
            txtTopicDesc = new TextBox { Location = new Point(0, 136), Width = 340, Height = 44, Multiline = true };
            pnlTopicForm.Controls.Add(txtTopicDesc);

            chkTopicActive = new CheckBox { Text = "Đang sử dụng", Checked = true, Location = new Point(360, 84), AutoSize = true };
            pnlTopicForm.Controls.Add(chkTopicActive);

            var btnTopicSave = new ModernButton { Text = "Lưu chủ đề", Size = new Size(120, 36), Location = new Point(0, 190) };
            btnTopicSave.Click += (_, _) => SaveTopic();
            var btnTopicNew = new ModernButton { Text = "Thêm mới", Size = new Size(110, 36), Location = new Point(128, 190), NormalBackColor = ThemeColors.TextMuted, HoverBackColor = ThemeColors.TextPrimary };
            btnTopicNew.Click += (_, _) => ResetTopicForm();
            var btnTopicDel = new ModernButton { Text = "Xóa chủ đề", Size = new Size(115, 36), Location = new Point(246, 190), NormalBackColor = ThemeColors.DangerRed, HoverBackColor = Color.FromArgb(185, 28, 28) };
            btnTopicDel.Click += (_, _) => DeleteTopic();
            var btnShowAllTopics = new ModernButton { Text = "Hiện tất cả", Size = new Size(110, 36), Location = new Point(368, 190), NormalBackColor = ThemeColors.RoyalBlue, HoverBackColor = ThemeColors.NavyDark };
            btnShowAllTopics.Click += (_, _) => LoadTopics(null);
            pnlTopicForm.Controls.AddRange(new Control[] { btnTopicSave, btnTopicNew, btnTopicDel, btnShowAllTopics });

            dgvTopics = new StyledDataGridView { Dock = DockStyle.Fill };
            dgvTopics.Columns.Add(new DataGridViewTextBoxColumn { Name = "STT", HeaderText = "STT", FillWeight = 12 });
            dgvTopics.Columns.Add(new DataGridViewTextBoxColumn { Name = "TenChuDe", HeaderText = "Tên chủ đề", FillWeight = 58 });
            dgvTopics.Columns.Add(new DataGridViewTextBoxColumn { Name = "TrangThai", HeaderText = "Trạng thái", FillWeight = 30 });
            dgvTopics.SelectionChanged += (_, _) =>
            {
                if (dgvTopics.CurrentRow != null && dgvTopics.CurrentRow.Index >= 0 && dgvTopics.CurrentRow.Index < _topics.Count)
                {
                    var t = _topics[dgvTopics.CurrentRow.Index];
                    _editingTopicId = t.ChuDeID;
                    txtTopicName.Text = t.TenChuDe;
                    txtTopicDesc.Text = t.MoTa;
                    chkTopicActive.Checked = t.TrangThai;
                    if (t.MonHocID.HasValue)
                    {
                        cboTopicSubject.SelectedValue = t.MonHocID.Value;
                    }
                }
            };

            pnlTopic.Controls.Add(dgvTopics);
            pnlTopic.Controls.Add(pnlTopicForm);
            pnlTopic.Controls.Add(lblTopicHeader);

            tlp.Controls.Add(pnlSub, 0, 0);
            tlp.Controls.Add(pnlTopic, 1, 0);
            Controls.Add(tlp);
        }

        private void LoadSubjects()
        {
            _subjects = _repo.GetMonHoc();
            dgvSubjects.Rows.Clear();
            for (int i = 0; i < _subjects.Count; i++)
            {
                var s = _subjects[i];
                dgvSubjects.Rows.Add(i + 1, s.TenMonHoc, s.TrangThai ? "Đang dùng" : "Ngừng dùng");
            }

            cboTopicSubject.DataSource = null;
            cboTopicSubject.DataSource = new List<MonHoc>(_subjects);
            cboTopicSubject.DisplayMember = "TenMonHoc";
            cboTopicSubject.ValueMember = "MonHocID";
        }

        private void LoadTopics(int? monHocId = null)
        {
            _topics = _repo.GetChuDe(monHocId);
            dgvTopics.Rows.Clear();
            for (int i = 0; i < _topics.Count; i++)
            {
                var t = _topics[i];
                dgvTopics.Rows.Add(i + 1, t.TenChuDe, t.TrangThai ? "Đang dùng" : "Ngừng dùng");
            }
        }

        private void ResetSubjectForm()
        {
            _editingSubjectId = 0;
            txtSubName.Clear();
            txtSubDesc.Clear();
            chkSubActive.Checked = true;
            txtSubName.Focus();
        }

        private void SaveSubject()
        {
            string name = txtSubName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Vui lòng nhập tên môn học.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_repo.ExistsTenMonHoc(name, _editingSubjectId))
            {
                MessageBox.Show("Tên môn học đã tồn tại.", "Trùng dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var model = new MonHoc { MonHocID = _editingSubjectId, TenMonHoc = name, MoTa = txtSubDesc.Text.Trim(), TrangThai = chkSubActive.Checked };
            if (_editingSubjectId == 0) _repo.AddMonHoc(model);
            else _repo.UpdateMonHoc(model);

            ResetSubjectForm();
            LoadSubjects();
        }

        private void DeleteSubject()
        {
            if (_editingSubjectId == 0) return;
            if (MessageBox.Show("Xác nhận xóa môn học đang chọn?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            _repo.DeleteMonHoc(_editingSubjectId, out string msg);
            MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ResetSubjectForm();
            LoadSubjects();
        }

        private void ResetTopicForm()
        {
            _editingTopicId = 0;
            txtTopicName.Clear();
            txtTopicDesc.Clear();
            chkTopicActive.Checked = true;
            txtTopicName.Focus();
        }

        private void SaveTopic()
        {
            string name = txtTopicName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Vui lòng nhập tên chủ đề.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int? monId = cboTopicSubject.SelectedValue is int id ? id : null;
            var model = new ChuDe { ChuDeID = _editingTopicId, MonHocID = monId, TenChuDe = name, MoTa = txtTopicDesc.Text.Trim(), TrangThai = chkTopicActive.Checked };
            if (_editingTopicId == 0) _repo.AddChuDe(model);
            else _repo.UpdateChuDe(model);

            ResetTopicForm();
            LoadTopics(monId);
        }

        private void DeleteTopic()
        {
            if (_editingTopicId == 0) return;
            if (MessageBox.Show("Xác nhận xóa chủ đề đang chọn?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            _repo.DeleteChuDe(_editingTopicId, out string msg);
            MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ResetTopicForm();
            LoadTopics();
        }
    }
}
