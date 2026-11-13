namespace RungChuongVang.Enums
{
    /// <summary>
    /// Phân loại câu hỏi trong ngân hàng câu hỏi.
    /// </summary>
    public enum LoaiCauHoi
    {
        TracNghiemMotDapAn = 1,
        TracNghiemNhieuDapAn = 2,
        TuLuanTraLoiNgan = 3
    }

    /// <summary>
    /// 4 mức độ khó mặc định của câu hỏi.
    /// </summary>
    public enum MucDoKho
    {
        NhanBiet = 1,
        ThongHieu = 2,
        VanDung = 3,
        VanDungCao = 4
    }

    /// <summary>
    /// Phương thức tổ chức của một chương trình thi Rung Chuông Vàng.
    /// </summary>
    public enum PhuongThucThi
    {
        ThiCaNhan = 1,
        ThiTheoDoi = 2
    }

    /// <summary>
    /// Quy tắc xác nhận đáp án khi tổ chức thi theo đội/lớp.
    /// </summary>
    public enum QuyTacTraLoiDoi
    {
        DoiTruongQuyetDinh = 1,
        DoiThongNhatDapAn = 2,
        TatCaThanhVienPhaiDung = 3
    }

    /// <summary>
    /// Trạng thái vòng đời của chương trình thi.
    /// </summary>
    public enum TrangThaiChuongTrinhEnum
    {
        ChuaBatDau = 0,
        DangDienRa = 1,
        TamDung = 2,
        DaKetThuc = 3
    }

    /// <summary>
    /// Trạng thái thi đấu của thí sinh hoặc đội thi trong chương trình.
    /// </summary>
    public enum TrangThaiThi
    {
        ChuaThamGia = 0,
        DangThi = 1,
        TraLoiDung = 2,
        BiLoai = 3,
        DuocCuuTro = 4,
        DuocKhoiPhuc = 5,
        ChienThang = 6
    }

    /// <summary>
    /// Kết quả chấm một câu hỏi của thí sinh hoặc đội thi.
    /// </summary>
    public enum KetQuaDungSai
    {
        Sai = 0,
        Dung = 1,
        KhongTraLoi = 2,
        ChoXuLy = 3
    }

    /// <summary>
    /// Trạng thái xử lý bản ghi kết quả.
    /// </summary>
    public enum TrangThaiXuLyKetQua
    {
        Tam = 0,
        DaXacNhan = 1,
        DaHoanTac = 2
    }

    /// <summary>
    /// 9 trạng thái trình chiếu sân khấu + màn hình vinh danh.
    /// </summary>
    public enum TrangThaiTrinhChieu
    {
        ManHinhCho = 1,
        GioiThieuCauHoi = 2,
        HienThiDapAn = 3,
        DemNguoc = 4,
        CongBoDapAn = 5,
        ThongKeThiSinh = 6,
        ManHinhCuuTro = 7,
        ManHinhChungKet = 8,
        ManHinhChienThang = 9,
        ManHinhVinhDanh = 10
    }

    /// <summary>
    /// Trạng thái đồng hồ đếm ngược.
    /// </summary>
    public enum TrangThaiDongHo
    {
        ChuaChay = 0,
        DangChay = 1,
        TamDung = 2,
        HetGio = 3
    }
}
