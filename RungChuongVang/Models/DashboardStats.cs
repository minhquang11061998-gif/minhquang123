using System;
using System.Collections.Generic;

namespace RungChuongVang.Models
{
    public class DashboardStats
    {
        public int TongCauHoi { get; set; }
        public int CauHoiDangDung { get; set; }
        public int TongLopHoc { get; set; }
        public int TongMonHoc { get; set; }
        public int TongThiSinh { get; set; }
        public int TongDoiThi { get; set; }
        public int TongChuongTrinh { get; set; }
        public int ChuongTrinhDangDienRa { get; set; }
        public List<CauHoiTheoMonSummary> ThongKeTheoMon { get; set; } = new();
        public List<ChuongTrinh> ChuongTrinhGanDay { get; set; } = new();
    }

    public class CauHoiTheoMonSummary
    {
        public string TenMonHoc { get; set; } = string.Empty;
        public int TongSo { get; set; }
        public int NhanBiet { get; set; }
        public int ThongHieu { get; set; }
        public int VanDung { get; set; }
        public int VanDungCao { get; set; }
    }

    public class CauHinhItem
    {
        public int CauHinhID { get; set; }
        public string TenCauHinh { get; set; } = string.Empty;
        public string GiaTri { get; set; } = string.Empty;
        public string MoTa { get; set; } = string.Empty;
    }
}
