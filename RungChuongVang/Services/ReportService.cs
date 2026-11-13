using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ClosedXML.Excel;
using RungChuongVang.DataAccess;
using RungChuongVang.Enums;
using RungChuongVang.Models;

namespace RungChuongVang.Services
{
    /// <summary>
    /// Tổng hợp 4 nhóm báo cáo (Câu hỏi, Chương trình, Thí sinh, Đội thi) và xuất Excel định dạng chuyên nghiệp.
    /// </summary>
    public class ReportService
    {
        private readonly ChuongTrinhRepository _ctRepo = new();
        private readonly DanhMucRepository _dmRepo = new();
        private readonly CauHoiRepository _chRepo = new();

        public DataTable GetQuestionReportTable()
        {
            var table = new DataTable();
            table.Columns.Add("Nhóm thống kê", typeof(string));
            table.Columns.Add("Chỉ mục", typeof(string));
            table.Columns.Add("Tổng số câu", typeof(int));
            table.Columns.Add("Đang sử dụng", typeof(int));
            table.Columns.Add("Ngừng sử dụng", typeof(int));

            var all = _chRepo.SearchCauHoi(trangThai: null);
            var mucDoMap = _dmRepo.GetMucDoConfig();

            table.Rows.Add("Tổng quan", "Toàn bộ ngân hàng câu hỏi", all.Count, all.Count(q => q.TrangThai), all.Count(q => !q.TrangThai));

            foreach (var grp in all.GroupBy(q => string.IsNullOrWhiteSpace(q.TenMonHoc) ? "Chưa phân môn" : q.TenMonHoc))
            {
                table.Rows.Add("Theo Môn học", grp.Key, grp.Count(), grp.Count(q => q.TrangThai), grp.Count(q => !q.TrangThai));
            }

            foreach (MucDoKho lv in Enum.GetValues<MucDoKho>())
            {
                string name = mucDoMap.TryGetValue(lv, out var info) ? info.Ten : lv.ToString();
                var sub = all.Where(q => q.MucDo == lv).ToList();
                table.Rows.Add("Theo Mức độ", name, sub.Count, sub.Count(q => q.TrangThai), sub.Count(q => !q.TrangThai));
            }

            var classes = _dmRepo.GetLopHoc();
            foreach (var cls in classes)
            {
                var sub = all.Where(q => q.LopHocIds.Contains(cls.LopHocID)).ToList();
                table.Rows.Add("Theo Lớp học", cls.TenLop, sub.Count, sub.Count(q => q.TrangThai), sub.Count(q => !q.TrangThai));
            }

            return table;
        }

        public DataTable GetProgramReportTable()
        {
            var table = new DataTable();
            table.Columns.Add("Mã CT", typeof(string));
            table.Columns.Add("Tên chương trình", typeof(string));
            table.Columns.Add("Ngày tổ chức", typeof(string));
            table.Columns.Add("Phương thức", typeof(string));
            table.Columns.Add("Số thí sinh/đội", typeof(int));
            table.Columns.Add("Tổng câu hỏi", typeof(int));
            table.Columns.Add("Câu đã thi", typeof(int));
            table.Columns.Add("Chiến thắng", typeof(string));
            table.Columns.Add("Trạng thái", typeof(string));

            var programs = _ctRepo.GetAllPrograms();
            foreach (var p in programs)
            {
                var participants = _ctRepo.GetParticipants(p.ChuongTrinhID, p.PhuongThucThi);
                int usedQuestions = Convert.ToInt32(DatabaseHelper.ExecuteScalar(
                    "SELECT COUNT(*) FROM (SELECT DISTINCT CauHoiID FROM KetQua WHERE ChuongTrinhID = ?) AS SubQ",
                    p.ChuongTrinhID) ?? 0);

                var winners = participants.Where(x => x.TrangThaiThi == TrangThaiThi.ChienThang).Select(x => x.TenHienThi).ToList();
                if (winners.Count == 0 && p.TrangThai == TrangThaiChuongTrinhEnum.DaKetThuc)
                {
                    winners = participants.Where(x => x.ThuHang == 1).Select(x => x.TenHienThi).ToList();
                }

                table.Rows.Add(
                    p.MaChuongTrinh,
                    p.TenChuongTrinh,
                    p.NgayToChuc.ToString("dd/MM/yyyy"),
                    p.PhuongThucThi == PhuongThucThi.ThiCaNhan ? "Cá nhân" : "Theo đội",
                    participants.Count,
                    p.SoLuongCauHoi,
                    usedQuestions,
                    winners.Count > 0 ? string.Join(", ", winners) : "--",
                    p.TrangThai.ToString());
            }

            return table;
        }

        public DataTable GetStudentResultReportTable(int chuongTrinhId)
        {
            var table = new DataTable();
            table.Columns.Add("Hạng", typeof(string));
            table.Columns.Add("SBD", typeof(string));
            table.Columns.Add("Họ và tên", typeof(string));
            table.Columns.Add("Lớp", typeof(string));
            table.Columns.Add("Số câu đúng", typeof(int));
            table.Columns.Add("Câu bị loại", typeof(string));
            table.Columns.Add("Thời điểm bị loại", typeof(string));
            table.Columns.Add("Số lần cứu trợ", typeof(int));
            table.Columns.Add("Trạng thái", typeof(string));

            var ct = _ctRepo.GetById(chuongTrinhId);
            if (ct == null || ct.PhuongThucThi != PhuongThucThi.ThiCaNhan) return table;

            var list = _ctRepo.GetParticipants(chuongTrinhId, PhuongThucThi.ThiCaNhan)
                .OrderBy(x => x.ThuHang ?? int.MaxValue)
                .ThenByDescending(x => x.SoCauDung);

            foreach (var p in list)
            {
                table.Rows.Add(
                    p.ThuHang?.ToString() ?? "-",
                    p.SoBaoDanh,
                    p.TenHienThi,
                    p.TenLop,
                    p.SoCauDung,
                    p.CauHoiBiLoai?.ToString() ?? "-",
                    p.ThoiDiemBiLoai?.ToString("dd/MM/yyyy HH:mm:ss") ?? "-",
                    p.SoLanCuuTro,
                    p.TrangThaiThi.ToString());
            }
            return table;
        }

        public DataTable GetTeamResultReportTable(int chuongTrinhId)
        {
            var table = new DataTable();
            table.Columns.Add("Hạng", typeof(string));
            table.Columns.Add("Mã đội", typeof(string));
            table.Columns.Add("Tên đội", typeof(string));
            table.Columns.Add("Lớp", typeof(string));
            table.Columns.Add("Thành viên", typeof(int));
            table.Columns.Add("Số câu đúng", typeof(int));
            table.Columns.Add("Câu bị loại", typeof(string));
            table.Columns.Add("Số lần cứu trợ", typeof(int));
            table.Columns.Add("Thành tích / Trạng thái", typeof(string));

            var ct = _ctRepo.GetById(chuongTrinhId);
            if (ct == null || ct.PhuongThucThi != PhuongThucThi.ThiTheoDoi) return table;

            var list = _ctRepo.GetParticipants(chuongTrinhId, PhuongThucThi.ThiTheoDoi)
                .OrderBy(x => x.ThuHang ?? int.MaxValue)
                .ThenByDescending(x => x.SoCauDung);

            foreach (var p in list)
            {
                table.Rows.Add(
                    p.ThuHang?.ToString() ?? "-",
                    p.SoBaoDanh,
                    p.TenHienThi,
                    p.TenLop,
                    p.SoThanhVien,
                    p.SoCauDung,
                    p.CauHoiBiLoai?.ToString() ?? "-",
                    p.SoLanCuuTro,
                    p.TrangThaiThi == TrangThaiThi.ChienThang ? "Đội chiến thắng" : p.TrangThaiThi.ToString());
            }
            return table;
        }

        public void ExportAllReportsToExcel(int? selectedProgramId, string filePath)
        {
            using var wb = new XLWorkbook();

            AddTableWorksheet(wb, "BaoCaoCauHoi", "BÁO CÁO NGÂN HÀNG CÂU HỎI", GetQuestionReportTable());
            AddTableWorksheet(wb, "BaoCaoChuongTrinh", "BÁO CÁO CÁC CHƯƠNG TRÌNH THI", GetProgramReportTable());

            if (selectedProgramId.HasValue && selectedProgramId.Value > 0)
            {
                var ct = _ctRepo.GetById(selectedProgramId.Value);
                string titleSuffix = ct != null ? $" - {ct.TenChuongTrinh.ToUpperInvariant()}" : string.Empty;

                DataTable dtStudents = GetStudentResultReportTable(selectedProgramId.Value);
                if (dtStudents.Rows.Count > 0)
                {
                    AddTableWorksheet(wb, "BaoCaoThiSinh", "BÁO CÁO THÍ SINH" + titleSuffix, dtStudents);
                }

                DataTable dtTeams = GetTeamResultReportTable(selectedProgramId.Value);
                if (dtTeams.Rows.Count > 0)
                {
                    AddTableWorksheet(wb, "BaoCaoDoiThi", "BÁO CÁO ĐỘI THI" + titleSuffix, dtTeams);
                }
            }

            wb.SaveAs(filePath);
        }

        public void ExportSingleTableToExcel(string sheetName, string reportTitle, DataTable table, string filePath)
        {
            using var wb = new XLWorkbook();
            AddTableWorksheet(wb, sheetName, reportTitle, table);
            wb.SaveAs(filePath);
        }

        private static void AddTableWorksheet(XLWorkbook wb, string sheetName, string title, DataTable dt)
        {
            var ws = wb.Worksheets.Add(sheetName);
            int colCount = Math.Max(1, dt.Columns.Count);

            ws.Cell(1, 1).Value = title;
            ws.Range(1, 1, 1, colCount).Merge().Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#0B1D3A");
            ws.Cell(2, 1).Value = $"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}";

            for (int c = 0; c < dt.Columns.Count; c++)
            {
                ws.Cell(4, c + 1).Value = dt.Columns[c].ColumnName;
            }
            var hdr = ws.Range(4, 1, 4, colCount);
            hdr.Style.Fill.BackgroundColor = XLColor.FromHtml("#143B73");
            hdr.Style.Font.FontColor = XLColor.White;
            hdr.Style.Font.Bold = true;
            ws.Row(4).Height = 25;

            for (int r = 0; r < dt.Rows.Count; r++)
            {
                for (int c = 0; c < dt.Columns.Count; c++)
                {
                    object val = dt.Rows[r][c];
                    ws.Cell(r + 5, c + 1).Value = val == DBNull.Value ? string.Empty : XLCellValue.FromObject(val);
                }
                if (r % 2 == 1)
                {
                    ws.Range(r + 5, 1, r + 5, colCount).Style.Fill.BackgroundColor = XLColor.FromHtml("#F4F7FB");
                }
            }

            var dataRange = ws.Range(4, 1, Math.Max(4, dt.Rows.Count + 4), colCount);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            ws.Columns().AdjustToContents();
        }
    }
}
