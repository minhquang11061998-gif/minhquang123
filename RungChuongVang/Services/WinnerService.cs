using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using RungChuongVang.DataAccess;
using RungChuongVang.Enums;
using RungChuongVang.Models;

namespace RungChuongVang.Services
{
    /// <summary>
    /// Nghiệp vụ Vinh danh người chiến thắng: tự động lấy dữ liệu từ kết quả thi cá nhân/đội và xuất danh sách vinh danh.
    /// </summary>
    public class WinnerService
    {
        private readonly ChuongTrinhRepository _ctRepo = new();
        private readonly ThiSinhRepository _tsRepo = new();
        private readonly DoiThiRepository _doiRepo = new();
        private readonly ScoreService _scoreService = new();
        private readonly VinhDanhRepository _vdRepo = new();

        public static readonly string[] DefaultAwardTitles =
        {
            "Giải Nhất - Rung Chuông Vàng",
            "Giải Nhì",
            "Giải Ba",
            "Giải Khuyến khích",
            "Giải Người chơi xuất sắc nhất",
            "Giải Đội thi xuất sắc nhất"
        };

        /// <summary>
        /// Tự động tạo danh sách đề xuất vinh danh từ kết quả thi thực tế (cá nhân hoặc đội).
        /// </summary>
        public List<VinhDanh> BuildAutoCandidatesFromResults(int chuongTrinhId)
        {
            var ct = _ctRepo.GetById(chuongTrinhId);
            if (ct == null) return new List<VinhDanh>();

            var ranked = _scoreService.CalculateAndSaveRankings(chuongTrinhId, ct.PhuongThucThi);
            var existing = _vdRepo.GetByProgram(chuongTrinhId);
            int nextOrder = existing.Count > 0 ? existing.Max(x => x.ThuTuTrinhChieu) + 1 : 1;

            var candidates = new List<VinhDanh>();
            foreach (var p in ranked.Take(5))
            {
                string defaultAward = p.ThuHang switch
                {
                    1 => ct.PhuongThucThi == PhuongThucThi.ThiTheoDoi ? "Giải Nhất - Đội xuất sắc nhất" : "Giải Nhất - Rung Chuông Vàng",
                    2 => "Giải Nhì",
                    3 => "Giải Ba",
                    _ => "Giải Khuyến khích"
                };

                int passed = p.CauHoiBiLoai.HasValue ? Math.Max(0, p.CauHoiBiLoai.Value - 1) : ct.SoLuongCauHoi;

                if (ct.PhuongThucThi == PhuongThucThi.ThiCaNhan && p.ThiSinhID.HasValue)
                {
                    var ts = _tsRepo.GetById(p.ThiSinhID.Value);
                    candidates.Add(new VinhDanh
                    {
                        ChuongTrinhID = chuongTrinhId,
                        ThiSinhID = p.ThiSinhID,
                        HoTen = ts?.HoTen ?? p.TenHienThi,
                        SoBaoDanh = ts?.SoBaoDanh ?? p.SoBaoDanh,
                        TenLop = ts?.TenLop ?? p.TenLop,
                        TenTruong = ts?.TenTruong ?? p.TenTruong,
                        TenGiaiThuong = defaultAward,
                        ThanhTich = p.TrangThaiThi == TrangThaiThi.ChienThang ? "Quán quân Rung Chuông Vàng" : $"Xếp hạng {p.ThuHang}",
                        SoCauDung = p.SoCauDung,
                        SoCauVuotQua = passed,
                        DuongDanHinhAnh = ts?.DuongDanHinhAnh ?? string.Empty,
                        ThuTuTrinhChieu = nextOrder++,
                        NgayVinhDanh = DateTime.Now,
                        GhiChu = "CHÚC MỪNG BẠN ĐÃ XUẤT SẮC VƯỢT QUA CÁC THỬ THÁCH!"
                    });
                }
                else if (ct.PhuongThucThi == PhuongThucThi.ThiTheoDoi && p.DoiThiID.HasValue)
                {
                    var members = _doiRepo.GetThanhVienByDoi(p.DoiThiID.Value);
                    var team = _doiRepo.SearchDoiThi().FirstOrDefault(d => d.DoiThiID == p.DoiThiID.Value);
                    string memberSummary = members.Count > 0
                        ? $"Thành viên: {string.Join(", ", members.Select(m => m.HoTenThiSinh))}"
                        : "Tập thể đội thi";

                    candidates.Add(new VinhDanh
                    {
                        ChuongTrinhID = chuongTrinhId,
                        DoiThiID = p.DoiThiID,
                        HoTen = team?.TenDoi ?? p.TenHienThi,
                        SoBaoDanh = team?.MaDoi ?? p.SoBaoDanh,
                        TenDoi = team?.TenDoi ?? p.TenHienThi,
                        TenLop = team?.TenLop ?? p.TenLop,
                        TenTruong = team?.TenTruong ?? p.TenTruong,
                        TenGiaiThuong = defaultAward,
                        ThanhTich = p.TrangThaiThi == TrangThaiThi.ChienThang ? "Đội vô địch Rung Chuông Vàng" : $"Xếp hạng {p.ThuHang}",
                        SoCauDung = p.SoCauDung,
                        SoCauVuotQua = passed,
                        DuongDanHinhAnh = team?.DuongDanHinhAnh ?? string.Empty,
                        ThuTuTrinhChieu = nextOrder++,
                        NgayVinhDanh = DateTime.Now,
                        GhiChu = memberSummary
                    });
                }
            }

            return candidates;
        }

        public void ExportWinnersToExcel(ChuongTrinh program, List<VinhDanh> winners, string filePath)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("DanhSachVinhDanh");

            ws.Cell(1, 1).Value = $"DANH SÁCH VINH DANH - {program.TenChuongTrinh.ToUpperInvariant()}";
            ws.Range(1, 1, 1, 11).Merge().Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(2, 1).Value = $"Ngày tổ chức: {program.NgayToChuc:dd/MM/yyyy} | Đơn vị: {program.DonViToChuc}";

            string[] headers =
            {
                "STT", "Giải thưởng", "Họ tên / Tên đội", "SBD / Mã đội", "Lớp", "Trường",
                "Đội thi", "Thành tích", "Số câu đúng", "Câu vượt qua", "Ghi chú / Thành viên"
            };
            for (int c = 0; c < headers.Length; c++)
            {
                ws.Cell(4, c + 1).Value = headers[c];
            }

            var hdrRange = ws.Range(4, 1, 4, headers.Length);
            hdrRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#143B73");
            hdrRange.Style.Font.FontColor = XLColor.White;
            hdrRange.Style.Font.Bold = true;

            int r = 5;
            for (int i = 0; i < winners.Count; i++)
            {
                var w = winners[i];
                string extra = w.GhiChu;
                if (w.DoiThiID.HasValue)
                {
                    var members = _doiRepo.GetThanhVienByDoi(w.DoiThiID.Value);
                    if (members.Count > 0)
                    {
                        extra = $"{w.GhiChu} | DS thành viên: {string.Join(", ", members.Select(m => $"{m.HoTenThiSinh} ({m.SoBaoDanh})"))}";
                    }
                }

                ws.Cell(r, 1).Value = i + 1;
                ws.Cell(r, 2).Value = w.TenGiaiThuong;
                ws.Cell(r, 3).Value = w.HoTen;
                ws.Cell(r, 4).Value = w.SoBaoDanh;
                ws.Cell(r, 5).Value = w.TenLop;
                ws.Cell(r, 6).Value = w.TenTruong;
                ws.Cell(r, 7).Value = w.TenDoi;
                ws.Cell(r, 8).Value = w.ThanhTich;
                ws.Cell(r, 9).Value = w.SoCauDung;
                ws.Cell(r, 10).Value = w.SoCauVuotQua;
                ws.Cell(r, 11).Value = extra;
                r++;
            }

            ws.Columns().AdjustToContents();
            wb.SaveAs(filePath);
        }
    }
}
