using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using RungChuongVang.Controls;
using RungChuongVang.DataAccess;
using RungChuongVang.Helpers;
using RungChuongVang.Models;
using RungChuongVang.Services;

namespace RungChuongVang.Forms
{
    /// <summary>
    /// Phân hệ Báo cáo & Thống kê tổng hợp (4 nhóm: Câu hỏi, Chương trình, Thí sinh, Đội thi) + Xuất Excel.
    /// </summary>
    public class FrmBaoCao : Form
    {
        private readonly ChuongTrinhRepository _ctRepo = new();
        private readonly ReportService _reportService = new();

        private ComboBox cboProgram = null!;
        private StyledDataGridView dgvQuestions = null!;
        private StyledDataGridView dgvPrograms = null!;
        private StyledDataGridView dgvStudents = null!;
        private StyledDataGridView dgvTeams = null!;

        public FrmBaoCao()
        {
            BuildUi();
            LoadPrograms();
            LoadAllReports();
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
            pnlTop.Controls.Add(new Label { Text = "Lọc theo chương trình:", Location = new Point(12, 16), AutoSize = true, Font = ThemeColors.FontBodyBold });
            cboProgram = new ComboBox { Location = new Point(175, 12), Width = 360, DropDownStyle = ComboBoxStyle.DropDownList };
            cboProgram.SelectedIndexChanged += (_, _) => LoadProgramScopedReports();

            var btnRefresh = new ModernButton { Text = "Làm mới số liệu", Location = new Point(550, 9), Size = new Size(145, 36), NormalBackColor = ThemeColors.PrimaryBlue, HoverBackColor = ThemeColors.PrimaryBlueHover };
            btnRefresh.Click += (_, _) => LoadAllReports();

            var btnExportAll = new ModernButton { Text = "Xuất toàn bộ báo cáo Excel", Location = new Point(706, 9), Size = new Size(220, 36), NormalBackColor = ThemeColors.Gold, HoverBackColor = ThemeColors.GoldBright, ForeColor = ThemeColors.NavyDark };
            btnExportAll.Click += (_, _) => ExportAllExcel();

            pnlTop.Controls.AddRange(new Control[] { cboProgram, btnRefresh, btnExportAll });

            var tabs = new TabControl { Dock = DockStyle.Fill };
            dgvQuestions = AddGridTab(tabs, "1. Báo cáo Câu hỏi");
            dgvPrograms = AddGridTab(tabs, "2. Báo cáo Chương trình");
            dgvStudents = AddGridTab(tabs, "3. Báo cáo Thí sinh");
            dgvTeams = AddGridTab(tabs, "4. Báo cáo Đội thi");

            tlp.Controls.Add(pnlTop, 0, 0);
            tlp.Controls.Add(tabs, 0, 1);
            Controls.Add(tlp);
        }

        private static StyledDataGridView AddGridTab(TabControl tabs, string title)
        {
            var page = new TabPage(title) { BackColor = ThemeColors.SurfaceWhite, Padding = new Padding(12) };
            var grid = new StyledDataGridView { Dock = DockStyle.Fill };
            page.Controls.Add(grid);
            tabs.TabPages.Add(page);
            return grid;
        }

        private void LoadPrograms()
        {
            var programs = _ctRepo.GetAllPrograms();
            cboProgram.DataSource = programs;
            cboProgram.DisplayMember = "TenChuongTrinh";
            cboProgram.ValueMember = "ChuongTrinhID";
        }

        private void LoadAllReports()
        {
            dgvQuestions.DataSource = _reportService.GetQuestionReportTable();
            dgvPrograms.DataSource = _reportService.GetProgramReportTable();
            LoadProgramScopedReports();
        }

        private void LoadProgramScopedReports()
        {
            if (cboProgram.SelectedItem is not ChuongTrinh ct) return;
            dgvStudents.DataSource = _reportService.GetStudentResultReportTable(ct.ChuongTrinhID);
            dgvTeams.DataSource = _reportService.GetTeamResultReportTable(ct.ChuongTrinhID);
        }

        private void ExportAllExcel()
        {
            int? progId = cboProgram.SelectedItem is ChuongTrinh ct ? ct.ChuongTrinhID : null;
            using var sfd = new SaveFileDialog
            {
                Title = "Lưu báo cáo tổng hợp Excel",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"BaoCaoTongHop_RungChuongVang_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                _reportService.ExportAllReportsToExcel(progId, sfd.FileName);
                MessageBox.Show("Đã xuất toàn bộ báo cáo ra Excel thành công.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
