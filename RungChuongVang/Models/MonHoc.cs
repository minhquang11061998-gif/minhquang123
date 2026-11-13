namespace RungChuongVang.Models
{
    public class MonHoc
    {
        public int MonHocID { get; set; }
        public string TenMonHoc { get; set; } = string.Empty;
        public string MoTa { get; set; } = string.Empty;
        public bool TrangThai { get; set; } = true;
    }

    public class ChuDe
    {
        public int ChuDeID { get; set; }
        public int? MonHocID { get; set; }
        public string TenChuDe { get; set; } = string.Empty;
        public string MoTa { get; set; } = string.Empty;
        public bool TrangThai { get; set; } = true;
    }
}
