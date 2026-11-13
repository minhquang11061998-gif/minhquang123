using System;
using System.Collections.Generic;
using RungChuongVang.Enums;

namespace RungChuongVang.Models
{
    public class CauHoi
    {
        public int CauHoiID { get; set; }
        public string MaCauHoi { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
        public int? MonHocID { get; set; }
        public int? ChuDeID { get; set; }
        public MucDoKho MucDo { get; set; } = MucDoKho.NhanBiet;
        public LoaiCauHoi LoaiCauHoi { get; set; } = LoaiCauHoi.TracNghiemMotDapAn;
        public string GiaiThich { get; set; } = string.Empty;
        public int ThoiGianTraLoi { get; set; } = 15;
        public string DuongDanHinhAnh { get; set; } = string.Empty;
        public bool TrangThai { get; set; } = true;
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public DateTime NgayCapNhat { get; set; } = DateTime.Now;

        // Thuộc tính hiển thị bổ sung khi join
        public string TenMonHoc { get; set; } = string.Empty;
        public string TenChuDe { get; set; } = string.Empty;
        public string DanhSachTenLop { get; set; } = string.Empty;

        public List<int> LopHocIds { get; set; } = new();
        public List<DapAn> DanhSachDapAn { get; set; } = new();
    }
}
