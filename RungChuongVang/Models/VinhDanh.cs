using System;

namespace RungChuongVang.Models
{
    public class VinhDanh
    {
        public int VinhDanhID { get; set; }
        public int ChuongTrinhID { get; set; }
        public int? ThiSinhID { get; set; }
        public int? DoiThiID { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string SoBaoDanh { get; set; } = string.Empty;
        public string TenLop { get; set; } = string.Empty;
        public string TenTruong { get; set; } = string.Empty;
        public string TenDoi { get; set; } = string.Empty;
        public string TenGiaiThuong { get; set; } = string.Empty;
        public string ThanhTich { get; set; } = string.Empty;
        public int SoCauDung { get; set; }
        public int SoCauVuotQua { get; set; }
        public string DuongDanHinhAnh { get; set; } = string.Empty;
        public int ThuTuTrinhChieu { get; set; } = 1;
        public DateTime NgayVinhDanh { get; set; } = DateTime.Now;
        public string GhiChu { get; set; } = string.Empty;
    }
}
