namespace RungChuongVang.Models
{
    public class ChuongTrinhCauHoi
    {
        public int ID { get; set; }
        public int ChuongTrinhID { get; set; }
        public int CauHoiID { get; set; }
        public int ThuTu { get; set; }
        public int ThoiGianTraLoi { get; set; } = 15;
        public bool LaCauHoiPhu { get; set; }
        public int TrangThai { get; set; } // 0=Chưa thi, 1=Đang thi, 2=Đã thi

        public CauHoi? CauHoiChiTiet { get; set; }
    }
}
