using System;
using System.Drawing;
using System.Windows.Forms;
using RungChuongVang.Controls;
using RungChuongVang.Helpers;

namespace RungChuongVang.Forms
{
    partial class FrmDashboard
    {
        private System.ComponentModel.IContainer? components = null;
        private TableLayoutPanel tlpRoot = null!;
        private TableLayoutPanel tlpCards = null!;
        private StatCardControl cardCauHoi = null!;
        private StatCardControl cardThiSinh = null!;
        private StatCardControl cardDoiThi = null!;
        private StatCardControl cardChuongTrinh = null!;
        private RoundedPanel pnlQuickActions = null!;
        private FlowLayoutPanel flpQuickActions = null!;
        private ModernButton btnQuickAddQuestion = null!;
        private ModernButton btnQuickCreateProgram = null!;
        private ModernButton btnQuickControl = null!;
        private ModernButton btnQuickRefresh = null!;
        private TableLayoutPanel tlpTables = null!;
        private RoundedPanel pnlSubjectStats = null!;
        private Label lblSubjectTitle = null!;
        private StyledDataGridView dgvSubjectStats = null!;
        private RoundedPanel pnlRecentPrograms = null!;
        private Label lblRecentTitle = null!;
        private StyledDataGridView dgvRecentPrograms = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            tlpRoot = new TableLayoutPanel();
            tlpCards = new TableLayoutPanel();
            cardCauHoi = new StatCardControl();
            cardThiSinh = new StatCardControl();
            cardDoiThi = new StatCardControl();
            cardChuongTrinh = new StatCardControl();
            pnlQuickActions = new RoundedPanel();
            flpQuickActions = new FlowLayoutPanel();
            btnQuickAddQuestion = new ModernButton();
            btnQuickCreateProgram = new ModernButton();
            btnQuickControl = new ModernButton();
            btnQuickRefresh = new ModernButton();
            tlpTables = new TableLayoutPanel();
            pnlSubjectStats = new RoundedPanel();
            lblSubjectTitle = new Label();
            dgvSubjectStats = new StyledDataGridView();
            pnlRecentPrograms = new RoundedPanel();
            lblRecentTitle = new Label();
            dgvRecentPrograms = new StyledDataGridView();

            SuspendLayout();

            BackColor = ThemeColors.AppBackground;
            Padding = new Padding(20);

            // Root layout: 3 hàng (KPI Cards, Quick Actions, 2 Bảng thống kê)
            tlpRoot.Dock = DockStyle.Fill;
            tlpRoot.ColumnCount = 1;
            tlpRoot.RowCount = 3;
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 132F));
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 74F));
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Hàng 1: 4 Thẻ KPI đều nhau
            tlpCards.Dock = DockStyle.Fill;
            tlpCards.ColumnCount = 4;
            tlpCards.RowCount = 1;
            tlpCards.Margin = new Padding(0, 0, 0, 12);
            for (int i = 0; i < 4; i++)
            {
                tlpCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            }

            cardCauHoi.Dock = DockStyle.Fill;
            cardCauHoi.Margin = new Padding(0, 0, 10, 0);
            cardCauHoi.AccentTopColor = ThemeColors.PrimaryBlue;
            cardCauHoi.CardTitle = "NGÂN HÀNG CÂU HỎI";
            cardCauHoi.CardValue = "0";
            cardCauHoi.CardSubtitle = "Đang sử dụng: 0";

            cardThiSinh.Dock = DockStyle.Fill;
            cardThiSinh.Margin = new Padding(5, 0, 10, 0);
            cardThiSinh.AccentTopColor = ThemeColors.SuccessGreen;
            cardThiSinh.CardTitle = "THÍ SINH";
            cardThiSinh.CardValue = "0";
            cardThiSinh.CardSubtitle = "Số lớp học: 0";

            cardDoiThi.Dock = DockStyle.Fill;
            cardDoiThi.Margin = new Padding(5, 0, 10, 0);
            cardDoiThi.AccentTopColor = ThemeColors.Gold;
            cardDoiThi.CardTitle = "ĐỘI THI";
            cardDoiThi.CardValue = "0";
            cardDoiThi.CardSubtitle = "Số môn học: 0";

            cardChuongTrinh.Dock = DockStyle.Fill;
            cardChuongTrinh.Margin = new Padding(5, 0, 0, 0);
            cardChuongTrinh.AccentTopColor = ThemeColors.DangerRed;
            cardChuongTrinh.CardTitle = "CHƯƠNG TRÌNH THI";
            cardChuongTrinh.CardValue = "0";
            cardChuongTrinh.CardSubtitle = "Đang diễn ra: 0";

            tlpCards.Controls.Add(cardCauHoi, 0, 0);
            tlpCards.Controls.Add(cardThiSinh, 1, 0);
            tlpCards.Controls.Add(cardDoiThi, 2, 0);
            tlpCards.Controls.Add(cardChuongTrinh, 3, 0);

            // Hàng 2: Thanh tác vụ nhanh
            pnlQuickActions.Dock = DockStyle.Fill;
            pnlQuickActions.Margin = new Padding(0, 0, 0, 12);
            pnlQuickActions.Padding = new Padding(14, 10, 14, 10);

            flpQuickActions.Dock = DockStyle.Fill;
            flpQuickActions.FlowDirection = FlowDirection.LeftToRight;
            flpQuickActions.WrapContents = false;

            btnQuickAddQuestion.Text = "+ Quản lý câu hỏi";
            btnQuickAddQuestion.Size = new Size(175, 40);
            btnQuickAddQuestion.Margin = new Padding(0, 0, 10, 0);

            btnQuickCreateProgram.Text = "Tạo chương trình thi";
            btnQuickCreateProgram.Size = new Size(185, 40);
            btnQuickCreateProgram.NormalBackColor = ThemeColors.RoyalBlue;
            btnQuickCreateProgram.HoverBackColor = ThemeColors.NavyDark;
            btnQuickCreateProgram.Margin = new Padding(0, 0, 10, 0);

            btnQuickControl.Text = "Điều khiển trình chiếu";
            btnQuickControl.Size = new Size(195, 40);
            btnQuickControl.NormalBackColor = ThemeColors.Gold;
            btnQuickControl.HoverBackColor = ThemeColors.GoldBright;
            btnQuickControl.ForeColor = ThemeColors.NavyDark;
            btnQuickControl.Margin = new Padding(0, 0, 10, 0);

            btnQuickRefresh.Text = "Làm mới thống kê";
            btnQuickRefresh.Size = new Size(165, 40);
            btnQuickRefresh.NormalBackColor = ThemeColors.TextMuted;
            btnQuickRefresh.HoverBackColor = ThemeColors.TextPrimary;

            flpQuickActions.Controls.Add(btnQuickAddQuestion);
            flpQuickActions.Controls.Add(btnQuickCreateProgram);
            flpQuickActions.Controls.Add(btnQuickControl);
            flpQuickActions.Controls.Add(btnQuickRefresh);
            pnlQuickActions.Controls.Add(flpQuickActions);

            // Hàng 3: 2 Bảng thống kê song song
            tlpTables.Dock = DockStyle.Fill;
            tlpTables.ColumnCount = 2;
            tlpTables.RowCount = 1;
            tlpTables.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
            tlpTables.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));

            pnlSubjectStats.Dock = DockStyle.Fill;
            pnlSubjectStats.Margin = new Padding(0, 0, 8, 0);
            pnlSubjectStats.Padding = new Padding(14);

            lblSubjectTitle.Dock = DockStyle.Top;
            lblSubjectTitle.Height = 32;
            lblSubjectTitle.Font = ThemeColors.FontSubTitle;
            lblSubjectTitle.ForeColor = ThemeColors.NavyDark;
            lblSubjectTitle.Text = "Thống kê câu hỏi theo Môn học & Mức độ";

            dgvSubjectStats.Dock = DockStyle.Fill;
            pnlSubjectStats.Controls.Add(dgvSubjectStats);
            pnlSubjectStats.Controls.Add(lblSubjectTitle);

            pnlRecentPrograms.Dock = DockStyle.Fill;
            pnlRecentPrograms.Margin = new Padding(8, 0, 0, 0);
            pnlRecentPrograms.Padding = new Padding(14);

            lblRecentTitle.Dock = DockStyle.Top;
            lblRecentTitle.Height = 32;
            lblRecentTitle.Font = ThemeColors.FontSubTitle;
            lblRecentTitle.ForeColor = ThemeColors.NavyDark;
            lblRecentTitle.Text = "Chương trình thi gần đây";

            dgvRecentPrograms.Dock = DockStyle.Fill;
            pnlRecentPrograms.Controls.Add(dgvRecentPrograms);
            pnlRecentPrograms.Controls.Add(lblRecentTitle);

            tlpTables.Controls.Add(pnlSubjectStats, 0, 0);
            tlpTables.Controls.Add(pnlRecentPrograms, 1, 0);

            tlpRoot.Controls.Add(tlpCards, 0, 0);
            tlpRoot.Controls.Add(pnlQuickActions, 0, 1);
            tlpRoot.Controls.Add(tlpTables, 0, 2);

            Controls.Add(tlpRoot);
            Name = "FrmDashboard";
            Text = "Dashboard";
            ResumeLayout(false);
        }
    }
}
