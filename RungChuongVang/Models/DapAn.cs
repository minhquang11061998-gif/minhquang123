namespace RungChuongVang.Models
{
    public class DapAn
    {
        public int DapAnID { get; set; }
        public int CauHoiID { get; set; }
        public string KyHieu { get; set; } = string.Empty;
        public string NoiDungDapAn { get; set; } = string.Empty;
        public bool LaDapAnDung { get; set; }
        public int ThuTu { get; set; }
    }
}
