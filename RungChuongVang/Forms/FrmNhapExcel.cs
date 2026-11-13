using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using RungChuongVang.Controls;
using RungChuongVang.Helpers;
using RungChuongVang.Services;

namespace RungChuongVang.Forms
{
    /// <summary>
    /// Nhập ngân hàng câu hỏi từ Excel: tải mẫu, xem trước, báo lỗi từng dòng, xử lý câu trùng và nhập bất đồng bộ.
    /// </summary>
    public class FrmNhapExcel : Form
    {
        private readonly ExcelService _excelService = new();
        private List<QuestionImportRow> _previewRows = new();

        private TextBox txtFilePath = null!;
        private RadioButton rbSkipDuplicate = null!;
        private RadioButton rbUpdateDuplicate = null!;
        private StyledDataGridView dgvPreview = null!;
        private ListBox lstErrors = null!;
        private Label lblSummary = null!;
        private ModernButton btnImport = null!;

        public FrmNhapExcel()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "Nhập ngân hàng câu hỏi từ Excel";
            Size = new Size(1120, 720);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = ThemeColors.AppBackground;
            Font = ThemeColors.FontBody;

            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new Padding(16) };
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));

            // Thanh điều khiển trên
            var pnlTop = new RoundedPanel { Dock = DockStyle.Fill, Padding = new Padding(14), Margin = new Padding(0, 0, 0, 10) };
            var btnDownloadTpl = new ModernButton { Text = "Tải file Excel mẫu", Location = new Point(14, 12), Size = new Size(165, 36), NormalBackColor = ThemeColors.Gold, HoverBackColor = ThemeColors.GoldBright, ForeColor = ThemeColors.NavyDark };
            btnDownloadTpl.Click += (_, _) => DownloadTemplate();

            var btnBrowse = new ModernButton { Text = "Chọn file Excel...", Location = new Point(190, 12), Size = new Size(155, 36) };
            btnBrowse.Click += async (_, _) => await BrowseAndPreviewAsync();

            txtFilePath = new TextBox { Location = new Point(356, 17), Width = 410, ReadOnly = true };
            rbSkipDuplicate = new RadioButton { Text = "Bỏ qua câu hỏi trùng", Location = new Point(782, 10), AutoSize = true, Checked = true };
            rbUpdateDuplicate = new RadioButton { Text = "Cập nhật câu hỏi đã tồn tại", Location = new Point(782, 36), AutoSize = true };

            lblSummary = new Label { Location = new Point(14, 54), Width = 750, ForeColor = ThemeColors.RoyalBlue, Font = ThemeColors.FontBodyBold, Text = "Chưa chọn tập tin Excel." };

            pnlTop.Controls.AddRange(new Control[] { btnDownloadTpl, btnBrowse, txtFilePath, rbSkipDuplicate, rbUpdateDuplicate, lblSummary });

            // Bảng xem trước
            var pnlGrid = new RoundedPanel { Dock = DockStyle.Fill, Padding = new Padding(12), Margin = new Padding(0, 0, 0, 10) };
            dgvPreview = new StyledDataGridView { Dock = DockStyle.Fill };
            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "Dong", HeaderText = "Dòng", FillWeight = 7 });
            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "NoiDung", HeaderText = "Nội dung câu hỏi", FillWeight = 30 });
            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "Lop", HeaderText = "Lớp", FillWeight = 9 });
            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "Mon", HeaderText = "Môn học", FillWeight = 11 });
            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "MucDo", HeaderText = "Mức độ", FillWeight = 10 });
            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "Loai", HeaderText = "Loại", FillWeight = 11 });
            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "DapAnDung", HeaderText = "Đáp án đúng", FillWeight = 9 });
            dgvPreview.Columns.Add(new DataGridViewTextBoxColumn { Name = "TrangThai", HeaderText = "Kiểm tra", FillWeight = 23 });
            pnlGrid.Controls.Add(dgvPreview);

            // Danh sách lỗi từng dòng
            var pnlErrors = new RoundedPanel { Dock = DockStyle.Fill, Padding = new Padding(12), Margin = new Padding(0, 0, 0, 10) };
            var lblErrTitle = new Label { Dock = DockStyle.Top, Height = 24, Font = ThemeColors.FontBodyBold, ForeColor = ThemeColors.DangerRed, Text = "Chi tiết lỗi kiểm tra dữ liệu theo từng dòng Excel:" };
            lstErrors = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, ForeColor = ThemeColors.DangerRed };
            pnlErrors.Controls.Add(lstErrors);
            pnlErrors.Controls.Add(lblErrTitle);

            // Nút hành động dưới
            var pnlBottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var btnClose = new ModernButton { Text = "Đóng", Size = new Size(110, 38), NormalBackColor = ThemeColors.TextMuted, HoverBackColor = ThemeColors.TextPrimary };
            btnClose.Click += (_, _) => Close();

            btnImport = new ModernButton { Text = "Tiến hành nhập dữ liệu hợp lệ", Size = new Size(240, 38), Margin = new Padding(0, 0, 10, 0), Enabled = false, NormalBackColor = ThemeColors.SuccessGreen, HoverBackColor = Color.FromArgb(16, 130, 58) };
            btnImport.Click += async (_, _) => await RunImportAsync();

            pnlBottom.Controls.Add(btnClose);
            pnlBottom.Controls.Add(btnImport);

            tlp.Controls.Add(pnlTop, 0, 0);
            tlp.Controls.Add(pnlGrid, 0, 1);
            tlp.Controls.Add(pnlErrors, 0, 2);
            tlp.Controls.Add(pnlBottom, 0, 3);
            Controls.Add(tlp);
        }

        private void DownloadTemplate()
        {
            using var sfd = new SaveFileDialog
            {
                Title = "Lưu file Excel mẫu câu hỏi",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = "Mau_Nhap_Cau_Hoi_RungChuongVang.xlsx"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                _excelService.CreateQuestionTemplate(sfd.FileName);
                MessageBox.Show("Đã tạo file Excel mẫu thành công.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async Task BrowseAndPreviewAsync()
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Chọn file Excel câu hỏi",
                Filter = "Excel Files (*.xlsx)|*.xlsx"
            };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            txtFilePath.Text = ofd.FileName;
            lblSummary.Text = "Đang đọc và kiểm tra dữ liệu Excel...";
            btnImport.Enabled = false;

            try
            {
                _previewRows = await Task.Run(() => _excelService.ReadAndValidateQuestionsExcel(ofd.FileName));

                dgvPreview.Rows.Clear();
                lstErrors.Items.Clear();

                foreach (var row in _previewRows)
                {
                    int idx = dgvPreview.Rows.Add(
                        row.ExcelRowNumber,
                        row.NoiDung,
                        row.LopText,
                        row.MonHocText,
                        row.MucDoText,
                        row.LoaiCauHoiText,
                        row.DapAnDung,
                        row.TrangThaiHienThi);

                    if (!row.IsValid)
                    {
                        dgvPreview.Rows[idx].DefaultCellStyle.ForeColor = ThemeColors.DangerRed;
                        foreach (string err in row.Errors) lstErrors.Items.Add(err);
                    }
                    else if (row.IsDuplicate)
                    {
                        dgvPreview.Rows[idx].DefaultCellStyle.ForeColor = ThemeColors.WarningOrange;
                    }
                    else
                    {
                        dgvPreview.Rows[idx].DefaultCellStyle.ForeColor = ThemeColors.SuccessGreen;
                    }
                }

                int validCount = _previewRows.Count(r => r.IsValid && !r.IsDuplicate);
                int dupCount = _previewRows.Count(r => r.IsValid && r.IsDuplicate);
                int errCount = _previewRows.Count(r => !r.IsValid);

                lblSummary.Text = $"Tổng: {_previewRows.Count} dòng | Hợp lệ mới: {validCount} | Trùng: {dupCount} | Lỗi (không nhập): {errCount}";
                btnImport.Enabled = (validCount + dupCount) > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể đọc file Excel:\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task RunImportAsync()
        {
            if (_previewRows.Count == 0) return;

            bool updateDup = rbUpdateDuplicate.Checked;
            btnImport.Enabled = false;
            lblSummary.Text = "Đang nhập dữ liệu vào Access, vui lòng chờ...";

            try
            {
                var summary = await Task.Run(() => _excelService.ImportValidatedQuestions(_previewRows, updateDup));

                MessageBox.Show(
                    $"Kết quả nhập Excel:\n" +
                    $"- Thêm mới: {summary.Inserted}\n" +
                    $"- Cập nhật câu trùng: {summary.Updated}\n" +
                    $"- Bỏ qua câu trùng: {summary.Skipped}\n" +
                    $"- Dòng lỗi không nhập: {summary.Failed}",
                    "Hoàn tất nhập Excel",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi nhập dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnImport.Enabled = true;
            }
        }
    }
}
