namespace RungChuongVang.Models
{
    public class ThiSinh
    {
        public int ThiSinhID { get; set; }
        public string MaThiSinh { get; set; } = string.Empty;
        public string SoBaoDanh { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public int? LopHocID { get; set; }
        public string TenLop { get; set; } = string.Empty;
        public string TenTruong { get; set; } = string.Empty;
        public string GioiTinh { get; set; } = string.Empty;
        public string DuongDanHinhAnh { get; set; } = string.Empty;
        public string GhiChu { get; set; } = string.Empty;
        public bool TrangThai { get; set; } = true;
    }
}
