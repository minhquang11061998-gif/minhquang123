using System.Collections.Generic;

namespace RungChuongVang.Models
{
    public class DoiThi
    {
        public int DoiThiID { get; set; }
        public string MaDoi { get; set; } = string.Empty;
        public string TenDoi { get; set; } = string.Empty;
        public int? ChuongTrinhID { get; set; }
        public string TenLop { get; set; } = string.Empty;
        public string TenTruong { get; set; } = string.Empty;
        public string TenDoiTruong { get; set; } = string.Empty;
        public string DuongDanHinhAnh { get; set; } = string.Empty;
        public string GhiChu { get; set; } = string.Empty;

        public List<ThanhVienDoi> ThanhVien { get; set; } = new();
    }

    public class ThanhVienDoi
    {
        public int ThanhVienDoiID { get; set; }
        public int DoiThiID { get; set; }
        public int ThiSinhID { get; set; }
        public int? ChuongTrinhID { get; set; }
        public string VaiTro { get; set; } = "ThanhVien";
        public bool TrangThai { get; set; } = true;

        // Thông tin hiển thị
        public string HoTenThiSinh { get; set; } = string.Empty;
        public string SoBaoDanh { get; set; } = string.Empty;
        public string TenLop { get; set; } = string.Empty;
    }
}
