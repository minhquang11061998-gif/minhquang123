using System;
using System.Windows.Forms;
using RungChuongVang.DataAccess;
using RungChuongVang.Enums;
using RungChuongVang.Helpers;
using RungChuongVang.Models;

namespace RungChuongVang.Forms
{
    /// <summary>
    /// Màn hình tổng quan Dashboard kết nối dữ liệu thực từ Access.
    /// </summary>
    public partial class FrmDashboard : Form
    {
        private readonly DashboardRepository _dashboardRepo = new();

        public event Action<string>? NavigateRequested;

        public FrmDashboard()
        {
            InitializeComponent();
            ConfigureGrids();
            WireEvents();
            LoadDashboardData();
        }

        private void ConfigureGrids()
        {
            dgvSubjectStats.Columns.Clear();
            dgvSubjectStats.Columns.Add(new DataGridViewTextBoxColumn { Name = "TenMonHoc", HeaderText = "Môn học", FillWeight = 34 });
            dgvSubjectStats.Columns.Add(new DataGridViewTextBoxColumn { Name = "TongSo", HeaderText = "Tổng", FillWeight = 14 });
            dgvSubjectStats.Columns.Add(new DataGridViewTextBoxColumn { Name = "NhanBiet", HeaderText = "NB", FillWeight = 13 });
            dgvSubjectStats.Columns.Add(new DataGridViewTextBoxColumn { Name = "ThongHieu", HeaderText = "TH", FillWeight = 13 });
            dgvSubjectStats.Columns.Add(new DataGridViewTextBoxColumn { Name = "VanDung", HeaderText = "VD", FillWeight = 13 });
            dgvSubjectStats.Columns.Add(new DataGridViewTextBoxColumn { Name = "VanDungCao", HeaderText = "VDC", FillWeight = 13 });

            dgvRecentPrograms.Columns.Clear();
            dgvRecentPrograms.Columns.Add(new DataGridViewTextBoxColumn { Name = "MaChuongTrinh", HeaderText = "Mã CT", FillWeight = 18 });
            dgvRecentPrograms.Columns.Add(new DataGridViewTextBoxColumn { Name = "TenChuongTrinh", HeaderText = "Tên chương trình", FillWeight = 36 });
            dgvRecentPrograms.Columns.Add(new DataGridViewTextBoxColumn { Name = "NgayToChuc", HeaderText = "Ngày tổ chức", FillWeight = 18 });
            dgvRecentPrograms.Columns.Add(new DataGridViewTextBoxColumn { Name = "PhuongThuc", HeaderText = "Phương thức", FillWeight = 16 });
            dgvRecentPrograms.Columns.Add(new DataGridViewTextBoxColumn { Name = "TrangThai", HeaderText = "Trạng thái", FillWeight = 12 });
        }

        private void WireEvents()
        {
            cardCauHoi.Click += (_, _) => NavigateRequested?.Invoke("CauHoi");
            cardThiSinh.Click += (_, _) => NavigateRequested?.Invoke("ThiSinh");
            cardDoiThi.Click += (_, _) => NavigateRequested?.Invoke("DoiThi");
            cardChuongTrinh.Click += (_, _) => NavigateRequested?.Invoke("ChuongTrinh");

            btnQuickAddQuestion.Click += (_, _) => NavigateRequested?.Invoke("CauHoi");
            btnQuickCreateProgram.Click += (_, _) => NavigateRequested?.Invoke("ChuongTrinh");
            btnQuickControl.Click += (_, _) => NavigateRequested?.Invoke("DieuKhien");
            btnQuickRefresh.Click += (_, _) => LoadDashboardData();
        }

        public void LoadDashboardData()
        {
            try
            {
                DashboardStats stats = _dashboardRepo.GetDashboardStats();

                cardCauHoi.CardValue = stats.TongCauHoi.ToString("N0");
                cardCauHoi.CardSubtitle = $"Đang sử dụng: {stats.CauHoiDangDung:N0}";

                cardThiSinh.CardValue = stats.TongThiSinh.ToString("N0");
                cardThiSinh.CardSubtitle = $"Số lớp học: {stats.TongLopHoc:N0}";

                cardDoiThi.CardValue = stats.TongDoiThi.ToString("N0");
                cardDoiThi.CardSubtitle = $"Số môn học: {stats.TongMonHoc:N0}";

                cardChuongTrinh.CardValue = stats.TongChuongTrinh.ToString("N0");
                cardChuongTrinh.CardSubtitle = $"Đang diễn ra: {stats.ChuongTrinhDangDienRa:N0}";

                dgvSubjectStats.Rows.Clear();
                foreach (var item in stats.ThongKeTheoMon)
                {
                    dgvSubjectStats.Rows.Add(
                        item.TenMonHoc,
                        item.TongSo,
                        item.NhanBiet,
                        item.ThongHieu,
                        item.VanDung,
                        item.VanDungCao);
                }

                dgvRecentPrograms.Rows.Clear();
                foreach (var ct in stats.ChuongTrinhGanDay)
                {
                    dgvRecentPrograms.Rows.Add(
                        ct.MaChuongTrinh,
                        ct.TenChuongTrinh,
                        ct.NgayToChuc.ToString("dd/MM/yyyy"),
                        ct.PhuongThucThi == PhuongThucThi.ThiCaNhan ? "Cá nhân" : "Theo đội",
                        FormatTrangThai(ct.TrangThai));
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteErrorLog("FrmDashboard.LoadDashboardData", ex);
                MessageBox.Show(
                    $"Không thể tải dữ liệu Dashboard từ Access:\n{ex.Message}",
                    "Lỗi dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static string FormatTrangThai(TrangThaiChuongTrinhEnum tt) => tt switch
        {
            TrangThaiChuongTrinhEnum.ChuaBatDau => "Chưa thi",
            TrangThaiChuongTrinhEnum.DangDienRa => "Đang thi",
            TrangThaiChuongTrinhEnum.TamDung => "Tạm dừng",
            TrangThaiChuongTrinhEnum.DaKetThuc => "Đã kết thúc",
            _ => "Khác"
        };
    }
}
