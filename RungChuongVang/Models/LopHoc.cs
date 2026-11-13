namespace RungChuongVang.Models
{
    public class LopHoc
    {
        public int LopHocID { get; set; }
        public string MaLop { get; set; } = string.Empty;
        public string TenLop { get; set; } = string.Empty;
        public int KhoiLop { get; set; }
        public string TenTruong { get; set; } = string.Empty;
        public string GhiChu { get; set; } = string.Empty;
        public bool TrangThai { get; set; } = true;
    }
}
