using System;
using RungChuongVang.Enums;

namespace RungChuongVang.Models
{
    public class ChuongTrinh
    {
        public int ChuongTrinhID { get; set; }
        public string MaChuongTrinh { get; set; } = string.Empty;
        public string TenChuongTrinh { get; set; } = string.Empty;
        public DateTime NgayToChuc { get; set; } = DateTime.Today;
        public string DonViToChuc { get; set; } = string.Empty;
        public string DiaDiem { get; set; } = string.Empty;
        public string MoTa { get; set; } = string.Empty;
        public string HinhThucThi { get; set; } = "RungChuongVangTruyenThong";
        public PhuongThucThi PhuongThucThi { get; set; } = PhuongThucThi.ThiCaNhan;
        public QuyTacTraLoiDoi QuyTacTraLoiDoi { get; set; } = QuyTacTraLoiDoi.DoiTruongQuyetDinh;
        public int SoLuongCauHoi { get; set; }
        public int ThoiGianMacDinh { get; set; } = 15;
        public int SoLanCuuTroToiDa { get; set; } = 1;
        public int QuyTacHetCauHoi { get; set; } = 1;
        public bool DaKhoaBoCauHoi { get; set; }
        public TrangThaiChuongTrinhEnum TrangThai { get; set; } = TrangThaiChuongTrinhEnum.ChuaBatDau;
        public string GhiChu { get; set; } = string.Empty;
    }
}
