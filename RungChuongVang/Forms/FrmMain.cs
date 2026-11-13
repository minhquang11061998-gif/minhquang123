using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RungChuongVang.Controls;
using RungChuongVang.Helpers;

namespace RungChuongVang.Forms
{
    /// <summary>
    /// Form quản trị chính (Shell) chứa 13 phân hệ điều hướng bên trái và vùng hiển thị trung tâm.
    /// </summary>
    public partial class FrmMain : Form
    {
        private readonly List<(string Key, string Title, string StageNote)> _menuItems = new()
        {
            ("Dashboard", "Dashboard", "Đã hoàn thiện"),
            ("CauHoi", "Quản lý câu hỏi", "Đã hoàn thiện"),
            ("LopHoc", "Quản lý lớp học", "Đã hoàn thiện"),
            ("MonHoc", "Quản lý môn học", "Đã hoàn thiện"),
            ("ThiSinh", "Quản lý thí sinh", "Đã hoàn thiện"),
            ("DoiThi", "Quản lý đội thi", "Đã hoàn thiện"),
            ("ChuongTrinh", "Quản lý chương trình", "Đã hoàn thiện"),
            ("BoCauHoi", "Tạo bộ câu hỏi thi", "Đã hoàn thiện"),
            ("DieuKhien", "Điều khiển trình chiếu", "Đã hoàn thiện"),
            ("KetQua", "Kết quả thi", "Đã hoàn thiện"),
            ("VinhDanh", "Vinh danh", "Đã hoàn thiện"),
            ("BaoCao", "Báo cáo", "Đã hoàn thiện"),
            ("CaiDat", "Cài đặt", "Đã hoàn thiện")
        };

        private readonly Dictionary<string, ModernButton> _navButtons = new();
        private Form? _currentChildForm;

        public FrmMain()
        {
            InitializeComponent();
            BuildSidebarMenu();
            WireEvents();
            NavigateTo("Dashboard");
        }

        private void BuildSidebarMenu()
        {
            flpMenu.Controls.Clear();
            _navButtons.Clear();

            foreach (var item in _menuItems)
            {
                var btn = new ModernButton
                {
                    Text = "  " + item.Title,
                    Size = new Size(224, 42),
                    Margin = new Padding(0, 2, 0, 4),
                    Padding = new Padding(10, 0, 8, 0),
                    TextAlign = ContentAlignment.MiddleLeft,
                    NormalBackColor = ThemeColors.NavyDark,
                    HoverBackColor = ThemeColors.RoyalBlue,
                    ActiveBackColor = ThemeColors.Gold,
                    ActiveForeColor = ThemeColors.NavyDark,
                    ForeColor = Color.FromArgb(226, 234, 246),
                    Font = ThemeColors.FontBodyBold,
                    BorderRadius = 8,
                    Tag = item.Key
                };

                btn.Click += (_, _) => NavigateTo(item.Key);
                _navButtons[item.Key] = btn;
                flpMenu.Controls.Add(btn);
            }
        }

        private void WireEvents()
        {
            uiClockTimer.Tick += (_, _) => lblClock.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            btnHeaderLaunchControl.Click += (_, _) => NavigateTo("DieuKhien");
        }

        public void NavigateTo(string key)
        {
            var meta = _menuItems.Find(m => m.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(meta.Key))
            {
                return;
            }

            foreach (var kv in _navButtons)
            {
                kv.Value.IsActive = kv.Key.Equals(meta.Key, StringComparison.OrdinalIgnoreCase);
            }

            lblPageTitle.Text = meta.Title;

            switch (meta.Key)
            {
                case "Dashboard":
                    var dashboard = new FrmDashboard();
                    dashboard.NavigateRequested += NavigateTo;
                    ShowChildForm(dashboard);
                    break;

                case "CauHoi":
                    ShowChildForm(new FrmQuanLyCauHoi());
                    break;

                case "LopHoc":
                    ShowChildForm(new FrmQuanLyLopHoc());
                    break;

                case "MonHoc":
                    ShowChildForm(new FrmQuanLyMonHocChuDe());
                    break;

                case "ThiSinh":
                    ShowChildForm(new FrmQuanLyThiSinh());
                    break;

                case "DoiThi":
                    ShowChildForm(new FrmQuanLyDoiThi());
                    break;

                case "ChuongTrinh":
                    var frmCt = new FrmQuanLyChuongTrinh();
                    frmCt.LaunchControlRequested += LaunchControlWindow;
                    ShowChildForm(frmCt);
                    break;

                case "BoCauHoi":
                    ShowChildForm(new FrmChonCauHoi());
                    break;

                case "DieuKhien":
                    LaunchControlWindow(0);
                    break;

                case "VinhDanh":
                    ShowChildForm(new FrmVinhDanhControl());
                    break;

                case "KetQua":
                    ShowChildForm(new FrmKetQua());
                    break;

                case "BaoCao":
                    ShowChildForm(new FrmBaoCao());
                    break;

                case "CaiDat":
                    ShowChildForm(new FrmCaiDat());
                    break;

                default:
                    ShowStagePendingNotice(meta.Title, meta.StageNote);
                    break;
            }
        }

        private void LaunchControlWindow(int programId)
        {
            var ctrl = new FrmDieuKhien(programId);
            ctrl.OpenResultsRequested += _ =>
            {
                if (WindowState == FormWindowState.Minimized)
                {
                    WindowState = FormWindowState.Normal;
                }
                Activate();
                NavigateTo("KetQua");
            };
            ctrl.Show();
        }

        private void ShowChildForm(Form child)
        {
            ClearCurrentContent();

            _currentChildForm = child;
            child.TopLevel = false;
            child.FormBorderStyle = FormBorderStyle.None;
            child.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(child);
            child.Show();
        }

        private void ShowStagePendingNotice(string moduleTitle, string stageNote)
        {
            ClearCurrentContent();

            var wrapper = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeColors.AppBackground,
                Padding = new Padding(28)
            };

            var card = new RoundedPanel
            {
                Dock = DockStyle.Top,
                Height = 190,
                AccentTopColor = ThemeColors.Gold,
                Padding = new Padding(24)
            };

            var lblTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 36,
                Font = ThemeColors.FontTitle,
                ForeColor = ThemeColors.NavyDark,
                Text = moduleTitle
            };

            var lblInfo = new Label
            {
                Dock = DockStyle.Fill,
                Font = ThemeColors.FontBody,
                ForeColor = ThemeColors.TextMuted,
                Text = $"Phân hệ \"{moduleTitle}\" đã có sẵn khung điều hướng và cấu trúc bảng trong CSDL Access.\n" +
                       $"Trạng thái triển khai: {stageNote}.\n\n" +
                       $"CSDL Access hiện tại: {DataAccess.DatabaseHelper.DatabasePath}"
            };

            card.Controls.Add(lblInfo);
            card.Controls.Add(lblTitle);
            wrapper.Controls.Add(card);
            pnlContent.Controls.Add(wrapper);
        }

        private void ClearCurrentContent()
        {
            if (_currentChildForm != null)
            {
                pnlContent.Controls.Remove(_currentChildForm);
                _currentChildForm.Close();
                _currentChildForm.Dispose();
                _currentChildForm = null;
            }

            foreach (Control ctrl in pnlContent.Controls)
            {
                ctrl.Dispose();
            }
            pnlContent.Controls.Clear();
        }
    }
}
