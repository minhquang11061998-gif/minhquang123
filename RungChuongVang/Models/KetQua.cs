using System;
using RungChuongVang.Enums;

namespace RungChuongVang.Models
{
    public class KetQua
    {
        public int KetQuaID { get; set; }
        public int ChuongTrinhID { get; set; }
        public int CauHoiID { get; set; }
        public int? ThiSinhID { get; set; }
        public int? DoiThiID { get; set; }
        public string DapAnDaChon { get; set; } = string.Empty;
        public KetQuaDungSai KetQuaDungSai { get; set; } = KetQuaDungSai.ChoXuLy;
        public double ThoiGianTraLoi { get; set; }
        public TrangThaiXuLyKetQua TrangThaiXuLy { get; set; } = TrangThaiXuLyKetQua.Tam;
        public double Diem { get; set; }
        public string GhiChu { get; set; } = string.Empty;
    }

    public class ChuongTrinhThiSinh
    {
        public int ID { get; set; }
        public int ChuongTrinhID { get; set; }
        public int? ThiSinhID { get; set; }
        public int? DoiThiID { get; set; }
        public TrangThaiThi TrangThaiThi { get; set; } = TrangThaiThi.DangThi;
        public int SoCauDung { get; set; }
        public int SoCauSai { get; set; }
        public int SoCauKhongTraLoi { get; set; }
        public int? CauHoiBiLoai { get; set; }
        public DateTime? ThoiDiemBiLoai { get; set; }
        public int? ThuHang { get; set; }
        public int SoLanCuuTro { get; set; }

        // Thông tin hiển thị tổng hợp
        public string SoBaoDanh { get; set; } = string.Empty;
        public string TenHienThi { get; set; } = string.Empty;
        public string TenLop { get; set; } = string.Empty;
        public string TenTruong { get; set; } = string.Empty;
        public int SoThanhVien { get; set; }
    }
}
