using System;
using System.Drawing;
using System.Windows.Forms;
using RungChuongVang.Controls;
using RungChuongVang.Helpers;

namespace RungChuongVang.Forms
{
    partial class FrmMain
    {
        private System.ComponentModel.IContainer? components = null;
        private Panel pnlSidebar = null!;
        private Panel pnlBrand = null!;
        private Label lblBrandTitle = null!;
        private Label lblBrandSub = null!;
        private FlowLayoutPanel flpMenu = null!;
        private Panel pnlHeader = null!;
        private Label lblPageTitle = null!;
        private Label lblClock = null!;
        private ModernButton btnHeaderLaunchControl = null!;
        private Panel pnlContent = null!;
        private System.Windows.Forms.Timer uiClockTimer = null!;

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
            components = new System.ComponentModel.Container();
            pnlSidebar = new Panel();
            pnlBrand = new Panel();
            lblBrandTitle = new Label();
            lblBrandSub = new Label();
            flpMenu = new FlowLayoutPanel();
            pnlHeader = new Panel();
            lblPageTitle = new Label();
            lblClock = new Label();
            btnHeaderLaunchControl = new ModernButton();
            pnlContent = new Panel();
            uiClockTimer = new System.Windows.Forms.Timer(components);

            SuspendLayout();

            // Form chính
            Text = "PHẦN MỀM RUNG CHUÔNG VÀNG";
            Size = new Size(1380, 840);
            MinimumSize = new Size(1180, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ThemeColors.AppBackground;
            Font = ThemeColors.FontBody;

            // Sidebar trái (250px)
            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Width = 250;
            pnlSidebar.BackColor = ThemeColors.NavyDark;

            pnlBrand.Dock = DockStyle.Top;
            pnlBrand.Height = 86;
            pnlBrand.BackColor = Color.FromArgb(8, 22, 45);
            pnlBrand.Padding = new Padding(16, 14, 16, 10);

            lblBrandTitle.Dock = DockStyle.Top;
            lblBrandTitle.Height = 32;
            lblBrandTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblBrandTitle.ForeColor = ThemeColors.GoldBright;
            lblBrandTitle.Text = "RUNG CHUÔNG VÀNG";

            lblBrandSub.Dock = DockStyle.Fill;
            lblBrandSub.Font = ThemeColors.FontSmall;
            lblBrandSub.ForeColor = Color.FromArgb(180, 200, 228);
            lblBrandSub.Text = "Quản trị & Trình chiếu Gameshow";

            pnlBrand.Controls.Add(lblBrandSub);
            pnlBrand.Controls.Add(lblBrandTitle);

            flpMenu.Dock = DockStyle.Fill;
            flpMenu.FlowDirection = FlowDirection.TopDown;
            flpMenu.WrapContents = false;
            flpMenu.AutoScroll = true;
            flpMenu.Padding = new Padding(10, 10, 10, 10);
            flpMenu.BackColor = ThemeColors.NavyDark;

            pnlSidebar.Controls.Add(flpMenu);
            pnlSidebar.Controls.Add(pnlBrand);

            // Header trên
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 64;
            pnlHeader.BackColor = ThemeColors.SurfaceWhite;
            pnlHeader.Padding = new Padding(22, 12, 22, 12);

            lblPageTitle.Dock = DockStyle.Left;
            lblPageTitle.Width = 560;
            lblPageTitle.Font = ThemeColors.FontTitle;
            lblPageTitle.ForeColor = ThemeColors.NavyDark;
            lblPageTitle.TextAlign = ContentAlignment.MiddleLeft;
            lblPageTitle.Text = "Dashboard";

            btnHeaderLaunchControl.Dock = DockStyle.Right;
            btnHeaderLaunchControl.Width = 210;
            btnHeaderLaunchControl.Text = "Mở Điều khiển trình chiếu";
            btnHeaderLaunchControl.NormalBackColor = ThemeColors.Gold;
            btnHeaderLaunchControl.HoverBackColor = ThemeColors.GoldBright;
            btnHeaderLaunchControl.ForeColor = ThemeColors.NavyDark;

            lblClock.Dock = DockStyle.Right;
            lblClock.Width = 190;
            lblClock.Font = ThemeColors.FontBodyBold;
            lblClock.ForeColor = ThemeColors.TextMuted;
            lblClock.TextAlign = ContentAlignment.MiddleRight;
            lblClock.Padding = new Padding(0, 0, 16, 0);
            lblClock.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            pnlHeader.Controls.Add(lblPageTitle);
            pnlHeader.Controls.Add(lblClock);
            pnlHeader.Controls.Add(btnHeaderLaunchControl);

            // Vùng nội dung chính
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.BackColor = ThemeColors.AppBackground;

            uiClockTimer.Interval = 1000;
            uiClockTimer.Enabled = true;

            Controls.Add(pnlContent);
            Controls.Add(pnlHeader);
            Controls.Add(pnlSidebar);

            ResumeLayout(false);
        }
    }
}
