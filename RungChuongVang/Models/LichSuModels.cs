using System;
using RungChuongVang.Enums;

namespace RungChuongVang.Models
{
    public class LichSuLoai
    {
        public int ID { get; set; }
        public int ChuongTrinhID { get; set; }
        public int CauHoiID { get; set; }
        public int? ThiSinhID { get; set; }
        public int? DoiThiID { get; set; }
        public string LyDoLoai { get; set; } = string.Empty;
        public DateTime ThoiGianLoai { get; set; } = DateTime.Now;
        public string NguoiThucHien { get; set; } = string.Empty;
        public string GhiChu { get; set; } = string.Empty;
    }

    public class LichSuCuuTro
    {
        public int ID { get; set; }
        public int ChuongTrinhID { get; set; }
        public int? CauHoiIDHienTai { get; set; }
        public int? ThiSinhID { get; set; }
        public int? DoiThiID { get; set; }
        public string LyDoCuuTro { get; set; } = string.Empty;
        public DateTime ThoiGianCuuTro { get; set; } = DateTime.Now;
        public string NguoiThucHien { get; set; } = string.Empty;
    }

    public class LichSuThaoTac
    {
        public int ID { get; set; }
        public int ChuongTrinhID { get; set; }
        public string LoaiThaoTac { get; set; } = string.Empty;
        public int? DoiTuongID { get; set; }
        public string DuLieuCu { get; set; } = string.Empty;
        public string DuLieuMoi { get; set; } = string.Empty;
        public bool DaHoanTac { get; set; }
        public DateTime ThoiGian { get; set; } = DateTime.Now;
        public string NguoiThucHien { get; set; } = string.Empty;
    }

    public class TrangThaiChuongTrinhModel
    {
        public int ID { get; set; }
        public int ChuongTrinhID { get; set; }
        public int CauHoiHienTai { get; set; } = 1;
        public TrangThaiTrinhChieu TrangThaiTrinhChieu { get; set; } = TrangThaiTrinhChieu.ManHinhCho;
        public double ThoiGianConLai { get; set; } = 15;
        public TrangThaiDongHo TrangThaiDongHo { get; set; } = TrangThaiDongHo.ChuaChay;
        public DateTime NgayCapNhat { get; set; } = DateTime.Now;
    }
}
