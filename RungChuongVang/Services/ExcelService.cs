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
    public class QuestionImportRow
    {
        public int ExcelRowNumber { get; set; }
        public string NoiDung { get; set; } = string.Empty;
        public string LopText { get; set; } = string.Empty;
        public string MonHocText { get; set; } = string.Empty;
        public string ChuDeText { get; set; } = string.Empty;
        public string MucDoText { get; set; } = string.Empty;
        public string LoaiCauHoiText { get; set; } = string.Empty;
        public string DapAnA { get; set; } = string.Empty;
        public string DapAnB { get; set; } = string.Empty;
        public string DapAnC { get; set; } = string.Empty;
        public string DapAnD { get; set; } = string.Empty;
        public string DapAnDung { get; set; } = string.Empty;
        public string GiaiThich { get; set; } = string.Empty;
        public int ThoiGianTraLoi { get; set; } = 15;

        public bool IsValid => Errors.Count == 0;
        public bool IsDuplicate { get; set; }
        public int? ExistingCauHoiId { get; set; }
        public List<string> Errors { get; set; } = new();

        public CauHoi? ParsedQuestion { get; set; }

        public string TrangThaiHienThi =>
            !IsValid ? $"Lỗi: {string.Join("; ", Errors)}"
            : IsDuplicate ? "Trùng câu hỏi đã có"
            : "Hợp lệ";
    }

    public class ExcelImportSummary
    {
        public int Inserted { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public int Failed { get; set; }
        public List<string> RowMessages { get; set; } = new();
    }

    /// <summary>
    /// Xử lý tạo file Excel mẫu, đọc/kiểm tra dữ liệu và nhập/xuất Câu hỏi, Thí sinh, Đội thi bằng ClosedXML.
    /// </summary>
    public class ExcelService
    {
        private readonly DanhMucRepository _danhMucRepo = new();
        private readonly CauHoiRepository _cauHoiRepo = new();
        private readonly ThiSinhRepository _thiSinhRepo = new();
        private readonly DoiThiRepository _doiThiRepo = new();

        #region Ngân hàng câu hỏi (14 cột chuẩn Mục VI.2)

        public void CreateQuestionTemplate(string filePath)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("CauHoiMau");

            string[] headers =
            {
                "STT", "Nội dung câu hỏi", "Lớp", "Môn học", "Chủ đề",
                "Mức độ", "Loại câu hỏi", "Đáp án A", "Đáp án B",
                "Đáp án C", "Đáp án D", "Đáp án đúng", "Giải thích", "Thời gian trả lời"
            };

            for (int c = 0; c < headers.Length; c++)
            {
                ws.Cell(1, c + 1).Value = headers[c];
            }

            StyleHeaderRow(ws, 1, headers.Length);

            // 3 dòng hướng dẫn định dạng (người dùng có thể sửa/thay thế)
            ws.Cell(2, 1).Value = 1;
            ws.Cell(2, 2).Value = "Thủ đô của Việt Nam là thành phố nào?";
            ws.Cell(2, 3).Value = "5A";
            ws.Cell(2, 4).Value = "Địa lý";
            ws.Cell(2, 5).Value = "Địa lý Việt Nam";
            ws.Cell(2, 6).Value = "Nhận biết";
            ws.Cell(2, 7).Value = "Trắc nghiệm một đáp án";
            ws.Cell(2, 8).Value = "Hà Nội";
            ws.Cell(2, 9).Value = "Huế";
            ws.Cell(2, 10).Value = "Đà Nẵng";
            ws.Cell(2, 11).Value = "TP. Hồ Chí Minh";
            ws.Cell(2, 12).Value = "A";
            ws.Cell(2, 13).Value = "Hà Nội là thủ đô của nước CHXHCN Việt Nam.";
            ws.Cell(2, 14).Value = 15;

            ws.Cell(3, 1).Value = 2;
            ws.Cell(3, 2).Value = "Những số nào sau đây là số nguyên tố?";
            ws.Cell(3, 3).Value = "6A1";
            ws.Cell(3, 4).Value = "Toán";
            ws.Cell(3, 5).Value = "Số học";
            ws.Cell(3, 6).Value = "Thông hiểu";
            ws.Cell(3, 7).Value = "Trắc nghiệm nhiều đáp án";
            ws.Cell(3, 8).Value = "2";
            ws.Cell(3, 9).Value = "4";
            ws.Cell(3, 10).Value = "5";
            ws.Cell(3, 11).Value = "9";
            ws.Cell(3, 12).Value = "A,C";
            ws.Cell(3, 13).Value = "2 và 5 chỉ chia hết cho 1 và chính nó.";
            ws.Cell(3, 14).Value = 20;

            ws.Cell(4, 1).Value = 3;
            ws.Cell(4, 2).Value = "Chiến thắng Điện Biên Phủ diễn ra vào năm nào?";
            ws.Cell(4, 3).Value = "5A";
            ws.Cell(4, 4).Value = "Lịch sử";
            ws.Cell(4, 5).Value = "Lịch sử Việt Nam";
            ws.Cell(4, 6).Value = "Nhận biết";
            ws.Cell(4, 7).Value = "Tự luận";
            ws.Cell(4, 12).Value = "1954";
            ws.Cell(4, 13).Value = "Ngày 7/5/1954.";
            ws.Cell(4, 14).Value = 15;

            ws.Columns().AdjustToContents();

            var guide = wb.Worksheets.Add("HuongDan");
            guide.Cell(1, 1).Value = "HƯỚNG DẪN NHẬP FILE EXCEL CÂU HỎI";
            guide.Cell(1, 1).Style.Font.Bold = true;
            guide.Cell(2, 1).Value = "- Cột Lớp: Nhập đúng tên lớp đã tạo trong hệ thống (có thể nhập nhiều lớp cách nhau bởi dấu phẩy, ví dụ: 5A, 5B).";
            guide.Cell(3, 1).Value = "- Cột Mức độ: Nhập đúng tên cấu hình (mặc định: Nhận biết / Thông hiểu / Vận dụng / Vận dụng cao).";
            guide.Cell(4, 1).Value = "- Cột Loại câu hỏi: 'Trắc nghiệm một đáp án', 'Trắc nghiệm nhiều đáp án', hoặc 'Tự luận'.";
            guide.Cell(5, 1).Value = "- Cột Đáp án đúng: Loại 1 đáp án ghi A/B/C/D; Loại nhiều đáp án ghi A,C; Loại tự luận nhập đáp án chuẩn.";
            guide.Columns().AdjustToContents();

            wb.SaveAs(filePath);
        }

        public List<QuestionImportRow> ReadAndValidateQuestionsExcel(string filePath)
        {
            var rows = new List<QuestionImportRow>();
            var classes = _danhMucRepo.GetLopHoc(onlyActive: true);
            var subjects = _danhMucRepo.GetMonHoc(onlyActive: true);
            var topics = _danhMucRepo.GetChuDe(onlyActive: true);
            var mucDoConfig = _danhMucRepo.GetMucDoConfig();

            using var wb = new XLWorkbook(filePath);
            var ws = wb.Worksheet(1);
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            var seenContentInExcel = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int r = 2; r <= lastRow; r++)
            {
                var item = new QuestionImportRow
                {
                    ExcelRowNumber = r,
                    NoiDung = ws.Cell(r, 2).GetString().Trim(),
                    LopText = ws.Cell(r, 3).GetString().Trim(),
                    MonHocText = ws.Cell(r, 4).GetString().Trim(),
                    ChuDeText = ws.Cell(r, 5).GetString().Trim(),
                    MucDoText = ws.Cell(r, 6).GetString().Trim(),
                    LoaiCauHoiText = ws.Cell(r, 7).GetString().Trim(),
                    DapAnA = ws.Cell(r, 8).GetString().Trim(),
                    DapAnB = ws.Cell(r, 9).GetString().Trim(),
                    DapAnC = ws.Cell(r, 10).GetString().Trim(),
                    DapAnD = ws.Cell(r, 11).GetString().Trim(),
                    DapAnDung = ws.Cell(r, 12).GetString().Trim(),
                    GiaiThich = ws.Cell(r, 13).GetString().Trim()
                };

                string timeRaw = ws.Cell(r, 14).GetString().Trim();
                if (int.TryParse(timeRaw, out int sec) && sec > 0)
                {
                    item.ThoiGianTraLoi = sec;
                }
                else
                {
                    item.ThoiGianTraLoi = 15;
                }

                // Bỏ qua dòng hoàn toàn trống ở cuối file
                if (string.IsNullOrWhiteSpace(item.NoiDung) &&
                    string.IsNullOrWhiteSpace(item.LopText) &&
                    string.IsNullOrWhiteSpace(item.MonHocText) &&
                    string.IsNullOrWhiteSpace(item.DapAnDung))
                {
                    continue;
                }

                // Kiểm tra các trường bắt buộc
                if (string.IsNullOrWhiteSpace(item.NoiDung))
                {
                    item.Errors.Add($"Dòng {r}: Chưa nhập nội dung câu hỏi.");
                }

                // Kiểm tra lớp học (cho phép nhập nhiều lớp cách nhau dấu phẩy)
                var lopIds = new List<int>();
                if (string.IsNullOrWhiteSpace(item.LopText))
                {
                    item.Errors.Add($"Dòng {r}: Chưa nhập lớp học.");
                }
                else
                {
                    var classTokens = item.LopText.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string tk in classTokens)
                    {
                        string t = tk.Trim();
                        var matchedClass = classes.FirstOrDefault(c =>
                            string.Equals(c.TenLop, t, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(c.MaLop, t, StringComparison.OrdinalIgnoreCase));
                        if (matchedClass == null)
                        {
                            item.Errors.Add($"Dòng {r}: Lớp '{t}' không tồn tại hoặc đang ngừng sử dụng.");
                        }
                        else
                        {
                            lopIds.Add(matchedClass.LopHocID);
                        }
                    }
                }

                // Kiểm tra Môn học
                int? monHocId = null;
                if (string.IsNullOrWhiteSpace(item.MonHocText))
                {
                    item.Errors.Add($"Dòng {r}: Chưa nhập môn học.");
                }
                else
                {
                    var matchedSub = subjects.FirstOrDefault(s =>
                        string.Equals(s.TenMonHoc, item.MonHocText, StringComparison.OrdinalIgnoreCase));
                    if (matchedSub == null)
                    {
                        item.Errors.Add($"Dòng {r}: Môn học '{item.MonHocText}' không tồn tại.");
                    }
                    else
                    {
                        monHocId = matchedSub.MonHocID;
                    }
                }

                // Kiểm tra Chủ đề (nếu có nhập)
                int? chuDeId = null;
                if (!string.IsNullOrWhiteSpace(item.ChuDeText))
                {
                    var matchedTopic = topics.FirstOrDefault(t =>
                        string.Equals(t.TenChuDe, item.ChuDeText, StringComparison.OrdinalIgnoreCase) &&
                        (!monHocId.HasValue || t.MonHocID == null || t.MonHocID == monHocId.Value));
                    if (matchedTopic == null)
                    {
                        item.Errors.Add($"Dòng {r}: Chủ đề '{item.ChuDeText}' chưa được khai báo.");
                    }
                    else
                    {
                        chuDeId = matchedTopic.ChuDeID;
                    }
                }

                // Kiểm tra Mức độ khó theo danh sách cấu hình
                MucDoKho? mucDo = ParseMucDo(item.MucDoText, mucDoConfig);
                if (!mucDo.HasValue)
                {
                    item.Errors.Add($"Dòng {r}: Mức độ khó '{item.MucDoText}' không tồn tại.");
                }

                // Kiểm tra Loại câu hỏi
                LoaiCauHoi? loai = ParseLoaiCauHoi(item.LoaiCauHoiText);
                if (!loai.HasValue)
                {
                    item.Errors.Add($"Dòng {r}: Loại câu hỏi '{item.LoaiCauHoiText}' không hợp lệ.");
                }

                // Kiểm tra Đáp án và Đáp án đúng theo từng loại
                var dapAnList = new List<DapAn>();
                if (loai.HasValue)
                {
                    if (loai.Value == LoaiCauHoi.TuLuanTraLoiNgan)
                    {
                        if (string.IsNullOrWhiteSpace(item.DapAnDung))
                        {
                            item.Errors.Add($"Dòng {r}: Chưa nhập đáp án chuẩn cho câu tự luận.");
                        }
                        else
                        {
                            dapAnList.Add(new DapAn
                            {
                                KyHieu = "DA",
                                NoiDungDapAn = item.DapAnDung,
                                LaDapAnDung = true,
                                ThuTu = 1
                            });
                        }
                    }
                    else
                    {
                        var options = new List<(string Key, string Text)>
                        {
                            ("A", item.DapAnA),
                            ("B", item.DapAnB),
                            ("C", item.DapAnC),
                            ("D", item.DapAnD)
                        }.Where(o => !string.IsNullOrWhiteSpace(o.Text)).ToList();

                        if (options.Count < 2)
                        {
                            item.Errors.Add($"Dòng {r}: Câu hỏi trắc nghiệm phải có ít nhất 2 phương án trả lời.");
                        }

                        var correctKeys = item.DapAnDung
                            .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(k => k.Trim().ToUpperInvariant())
                            .Distinct()
                            .ToList();

                        if (correctKeys.Count == 0)
                        {
                            item.Errors.Add($"Dòng {r}: Đáp án đúng không hợp lệ (chưa nhập).");
                        }
                        else if (loai.Value == LoaiCauHoi.TracNghiemMotDapAn && correctKeys.Count != 1)
                        {
                            item.Errors.Add($"Dòng {r}: Câu một đáp án chỉ được chọn đúng 1 ký hiệu (ví dụ: A).");
                        }
                        else
                        {
                            foreach (string ck in correctKeys)
                            {
                                if (!options.Any(o => o.Key == ck))
                                {
                                    item.Errors.Add($"Dòng {r}: Đáp án đúng '{ck}' không khớp với các phương án đã nhập.");
                                }
                            }
                        }

                        int thuTu = 1;
                        foreach (var opt in options)
                        {
                            dapAnList.Add(new DapAn
                            {
                                KyHieu = opt.Key,
                                NoiDungDapAn = opt.Text,
                                LaDapAnDung = correctKeys.Contains(opt.Key),
                                ThuTu = thuTu++
                            });
                        }
                    }
                }

                // Kiểm tra trùng lặp trong file Excel và trong Access
                if (!string.IsNullOrWhiteSpace(item.NoiDung))
                {
                    if (!seenContentInExcel.Add(item.NoiDung))
                    {
                        item.Errors.Add($"Dòng {r}: Nội dung câu hỏi bị trùng ngay trong file Excel.");
                    }
                    else
                    {
                        int? existingId = _cauHoiRepo.FindExistingIdByNoiDung(item.NoiDung);
                        if (existingId.HasValue)
                        {
                            item.IsDuplicate = true;
                            item.ExistingCauHoiId = existingId.Value;
                        }
                    }
                }

                if (item.IsValid && mucDo.HasValue && loai.HasValue)
                {
                    item.ParsedQuestion = new CauHoi
                    {
                        CauHoiID = item.ExistingCauHoiId ?? 0,
                        NoiDung = item.NoiDung,
                        MonHocID = monHocId,
                        ChuDeID = chuDeId,
                        MucDo = mucDo.Value,
                        LoaiCauHoi = loai.Value,
                        GiaiThich = item.GiaiThich,
                        ThoiGianTraLoi = item.ThoiGianTraLoi,
                        TrangThai = true,
                        LopHocIds = lopIds,
                        DanhSachDapAn = dapAnList
                    };
                }

                rows.Add(item);
            }

            return rows;
        }

        public ExcelImportSummary ImportValidatedQuestions(List<QuestionImportRow> rows, bool updateDuplicates)
        {
            var summary = new ExcelImportSummary();

            foreach (var row in rows)
            {
                if (!row.IsValid || row.ParsedQuestion == null)
                {
                    summary.Failed++;
                    summary.RowMessages.Add(row.TrangThaiHienThi);
                    continue;
                }

                try
                {
                    if (row.IsDuplicate && row.ExistingCauHoiId.HasValue)
                    {
                        if (!updateDuplicates)
                        {
                            summary.Skipped++;
                            continue;
                        }

                        var existing = _cauHoiRepo.GetById(row.ExistingCauHoiId.Value);
                        if (existing != null)
                        {
                            row.ParsedQuestion.CauHoiID = existing.CauHoiID;
                            row.ParsedQuestion.MaCauHoi = existing.MaCauHoi;
                            _cauHoiRepo.UpdateCauHoi(row.ParsedQuestion);
                            summary.Updated++;
                        }
                    }
                    else
                    {
                        row.ParsedQuestion.MaCauHoi = _cauHoiRepo.GenerateNextMaCauHoi();
                        _cauHoiRepo.AddCauHoi(row.ParsedQuestion);
                        summary.Inserted++;
                    }
                }
                catch (Exception ex)
                {
                    summary.Failed++;
                    summary.RowMessages.Add($"Dòng {row.ExcelRowNumber}: Lỗi lưu CSDL ({ex.Message})");
                }
            }

            return summary;
        }

        public void ExportQuestionsToExcel(List<CauHoi> questions, string filePath)
        {
            var mucDoMap = _danhMucRepo.GetMucDoConfig();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("NganHangCauHoi");

            string[] headers =
            {
                "STT", "Mã câu hỏi", "Nội dung câu hỏi", "Lớp", "Môn học", "Chủ đề",
                "Mức độ", "Loại câu hỏi", "Đáp án A", "Đáp án B", "Đáp án C", "Đáp án D",
                "Đáp án đúng", "Giải thích", "Thời gian (s)", "Trạng thái"
            };

            for (int c = 0; c < headers.Length; c++)
            {
                ws.Cell(1, c + 1).Value = headers[c];
            }
            StyleHeaderRow(ws, 1, headers.Length);

            int rowIdx = 2;
            for (int i = 0; i < questions.Count; i++)
            {
                CauHoi full = _cauHoiRepo.GetById(questions[i].CauHoiID) ?? questions[i];
                ws.Cell(rowIdx, 1).Value = i + 1;
                ws.Cell(rowIdx, 2).Value = full.MaCauHoi;
                ws.Cell(rowIdx, 3).Value = full.NoiDung;
                ws.Cell(rowIdx, 4).Value = questions[i].DanhSachTenLop;
                ws.Cell(rowIdx, 5).Value = full.TenMonHoc;
                ws.Cell(rowIdx, 6).Value = full.TenChuDe;
                ws.Cell(rowIdx, 7).Value = mucDoMap.TryGetValue(full.MucDo, out var md) ? md.Ten : full.MucDo.ToString();
                ws.Cell(rowIdx, 8).Value = FormatLoaiCauHoi(full.LoaiCauHoi);

                string a = full.DanhSachDapAn.FirstOrDefault(d => d.KyHieu == "A")?.NoiDungDapAn ?? string.Empty;
                string b = full.DanhSachDapAn.FirstOrDefault(d => d.KyHieu == "B")?.NoiDungDapAn ?? string.Empty;
                string c = full.DanhSachDapAn.FirstOrDefault(d => d.KyHieu == "C")?.NoiDungDapAn ?? string.Empty;
                string d = full.DanhSachDapAn.FirstOrDefault(d => d.KyHieu == "D")?.NoiDungDapAn ?? string.Empty;
                string dung = full.LoaiCauHoi == LoaiCauHoi.TuLuanTraLoiNgan
                    ? (full.DanhSachDapAn.FirstOrDefault()?.NoiDungDapAn ?? string.Empty)
                    : string.Join(",", full.DanhSachDapAn.Where(x => x.LaDapAnDung).Select(x => x.KyHieu));

                ws.Cell(rowIdx, 9).Value = a;
                ws.Cell(rowIdx, 10).Value = b;
                ws.Cell(rowIdx, 11).Value = c;
                ws.Cell(rowIdx, 12).Value = d;
                ws.Cell(rowIdx, 13).Value = dung;
                ws.Cell(rowIdx, 14).Value = full.GiaiThich;
                ws.Cell(rowIdx, 15).Value = full.ThoiGianTraLoi;
                ws.Cell(rowIdx, 16).Value = full.TrangThai ? "Đang sử dụng" : "Ngừng sử dụng";

                if (rowIdx % 2 == 1)
                {
                    ws.Range(rowIdx, 1, rowIdx, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#F4F7FB");
                }
                rowIdx++;
            }

            ws.Columns().AdjustToContents();
            ws.Column(3).Width = 45;
            ws.Column(3).Style.Alignment.WrapText = true;
            wb.SaveAs(filePath);
        }

        #endregion

        #region Thí sinh & Đội thi Excel

        public void CreateContestantTemplate(string filePath)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("ThiSinhMau");
            string[] headers = { "STT", "Mã thí sinh", "Số báo danh", "Họ và tên", "Lớp", "Trường", "Giới tính", "Ghi chú" };
            for (int c = 0; c < headers.Length; c++) ws.Cell(1, c + 1).Value = headers[c];
            StyleHeaderRow(ws, 1, headers.Length);

            ws.Cell(2, 1).Value = 1;
            ws.Cell(2, 2).Value = "TS0001";
            ws.Cell(2, 3).Value = "001";
            ws.Cell(2, 4).Value = "Nguyễn Văn An";
            ws.Cell(2, 5).Value = "5A";
            ws.Cell(2, 6).Value = "Trường Tiểu học Chu Văn An";
            ws.Cell(2, 7).Value = "Nam";
            ws.Cell(2, 8).Value = "";

            ws.Columns().AdjustToContents();
            wb.SaveAs(filePath);
        }

        public ExcelImportSummary ImportContestantsFromExcel(string filePath)
        {
            var summary = new ExcelImportSummary();
            var classes = _danhMucRepo.GetLopHoc(onlyActive: true);

            using var wb = new XLWorkbook(filePath);
            var ws = wb.Worksheet(1);
            int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            for (int r = 2; r <= lastRow; r++)
            {
                string ma = ws.Cell(r, 2).GetString().Trim();
                string sbd = ws.Cell(r, 3).GetString().Trim();
                string hoTen = ws.Cell(r, 4).GetString().Trim();
                string tenLop = ws.Cell(r, 5).GetString().Trim();
                string truong = ws.Cell(r, 6).GetString().Trim();
                string gioiTinh = ws.Cell(r, 7).GetString().Trim();
                string ghiChu = ws.Cell(r, 8).GetString().Trim();

                if (string.IsNullOrWhiteSpace(hoTen) && string.IsNullOrWhiteSpace(sbd)) continue;

                if (string.IsNullOrWhiteSpace(hoTen) || string.IsNullOrWhiteSpace(sbd))
                {
                    summary.Failed++;
                    summary.RowMessages.Add($"Dòng {r}: Thiếu Số báo danh hoặc Họ tên.");
                    continue;
                }

                int? lopId = classes.FirstOrDefault(l =>
                    string.Equals(l.TenLop, tenLop, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(l.MaLop, tenLop, StringComparison.OrdinalIgnoreCase))?.LopHocID;

                if (!string.IsNullOrWhiteSpace(tenLop) && !lopId.HasValue)
                {
                    summary.Failed++;
                    summary.RowMessages.Add($"Dòng {r}: Lớp '{tenLop}' chưa tồn tại trong danh mục.");
                    continue;
                }

                try
                {
                    if (!string.IsNullOrWhiteSpace(ma) && _thiSinhRepo.ExistsMaThiSinh(ma))
                    {
                        summary.Skipped++;
                        summary.RowMessages.Add($"Dòng {r}: Mã thí sinh '{ma}' đã tồn tại (bỏ qua).");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(ma))
                    {
                        ma = _thiSinhRepo.GenerateNextMaThiSinh();
                    }

                    _thiSinhRepo.AddThiSinh(new ThiSinh
                    {
                        MaThiSinh = ma,
                        SoBaoDanh = sbd,
                        HoTen = hoTen,
                        LopHocID = lopId,
                        TenTruong = truong,
                        GioiTinh = gioiTinh,
                        GhiChu = ghiChu,
                        TrangThai = true
                    });
                    summary.Inserted++;
                }
                catch (Exception ex)
                {
                    summary.Failed++;
                    summary.RowMessages.Add($"Dòng {r}: {ex.Message}");
                }
            }

            return summary;
        }

        public void ExportContestantsToExcel(List<ThiSinh> list, string filePath)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("DanhSachThiSinh");
            string[] headers = { "STT", "Mã thí sinh", "Số báo danh", "Họ và tên", "Lớp", "Trường", "Giới tính", "Ghi chú", "Trạng thái" };
            for (int c = 0; c < headers.Length; c++) ws.Cell(1, c + 1).Value = headers[c];
            StyleHeaderRow(ws, 1, headers.Length);

            int r = 2;
            for (int i = 0; i < list.Count; i++)
            {
                var t = list[i];
                ws.Cell(r, 1).Value = i + 1;
                ws.Cell(r, 2).Value = t.MaThiSinh;
                ws.Cell(r, 3).Value = t.SoBaoDanh;
                ws.Cell(r, 4).Value = t.HoTen;
                ws.Cell(r, 5).Value = t.TenLop;
                ws.Cell(r, 6).Value = t.TenTruong;
                ws.Cell(r, 7).Value = t.GioiTinh;
                ws.Cell(r, 8).Value = t.GhiChu;
                ws.Cell(r, 9).Value = t.TrangThai ? "Đang hoạt động" : "Ngừng sử dụng";
                r++;
            }

            ws.Columns().AdjustToContents();
            wb.SaveAs(filePath);
        }

        public void ExportTeamsToExcel(List<DoiThi> teams, string filePath)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("DanhSachDoiThi");
            string[] headers = { "STT", "Mã đội", "Tên đội", "Lớp", "Trường", "Đội trưởng", "Số thành viên", "Danh sách thành viên", "Ghi chú" };
            for (int c = 0; c < headers.Length; c++) ws.Cell(1, c + 1).Value = headers[c];
            StyleHeaderRow(ws, 1, headers.Length);

            int r = 2;
            for (int i = 0; i < teams.Count; i++)
            {
                var d = teams[i];
                ws.Cell(r, 1).Value = i + 1;
                ws.Cell(r, 2).Value = d.MaDoi;
                ws.Cell(r, 3).Value = d.TenDoi;
                ws.Cell(r, 4).Value = d.TenLop;
                ws.Cell(r, 5).Value = d.TenTruong;
                ws.Cell(r, 6).Value = d.TenDoiTruong;
                ws.Cell(r, 7).Value = d.ThanhVien.Count;
                ws.Cell(r, 8).Value = string.Join("; ", d.ThanhVien.Select(m => $"{m.HoTenThiSinh} ({m.SoBaoDanh})"));
                ws.Cell(r, 9).Value = d.GhiChu;
                r++;
            }

            ws.Columns().AdjustToContents();
            wb.SaveAs(filePath);
        }

        public void CreateTeamTemplate(string filePath)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("DoiThiMau");
            string[] headers = { "STT", "Mã đội", "Tên đội", "Lớp", "Trường", "Ghi chú" };
            for (int c = 0; c < headers.Length; c++) ws.Cell(1, c + 1).Value = headers[c];
            StyleHeaderRow(ws, 1, headers.Length);

            ws.Cell(2, 1).Value = 1;
            ws.Cell(2, 2).Value = "DOI001";
            ws.Cell(2, 3).Value = "Đội Sao Vàng";
            ws.Cell(2, 4).Value = "5A";
            ws.Cell(2, 5).Value = "Trường Tiểu học Chu Văn An";
            ws.Cell(2, 6).Value = "";

            ws.Columns().AdjustToContents();
            wb.SaveAs(filePath);
        }

        public ExcelImportSummary ImportTeamsFromExcel(string filePath)
        {
            var summary = new ExcelImportSummary();
            using var wb = new XLWorkbook(filePath);
            var ws = wb.Worksheet(1);
            int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            for (int r = 2; r <= lastRow; r++)
            {
                string maDoi = ws.Cell(r, 2).GetString().Trim();
                string tenDoi = ws.Cell(r, 3).GetString().Trim();
                string tenLop = ws.Cell(r, 4).GetString().Trim();
                string tenTruong = ws.Cell(r, 5).GetString().Trim();
                string ghiChu = ws.Cell(r, 6).GetString().Trim();

                if (string.IsNullOrWhiteSpace(tenDoi)) continue;

                try
                {
                    if (!string.IsNullOrWhiteSpace(maDoi) && _doiThiRepo.ExistsMaDoi(maDoi))
                    {
                        summary.Skipped++;
                        summary.RowMessages.Add($"Dòng {r}: Mã đội '{maDoi}' đã tồn tại (bỏ qua).");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(maDoi))
                    {
                        maDoi = _doiThiRepo.GenerateNextMaDoi();
                    }

                    _doiThiRepo.AddDoiThi(new DoiThi
                    {
                        MaDoi = maDoi,
                        TenDoi = tenDoi,
                        TenLop = tenLop,
                        TenTruong = tenTruong,
                        GhiChu = ghiChu
                    });
                    summary.Inserted++;
                }
                catch (Exception ex)
                {
                    summary.Failed++;
                    summary.RowMessages.Add($"Dòng {r}: {ex.Message}");
                }
            }
            return summary;
        }

        #endregion

        #region Helpers

        private static void StyleHeaderRow(IXLWorksheet ws, int row, int columnCount)
        {
            var headerRange = ws.Range(row, 1, row, columnCount);
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#143B73");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(row).Height = 26;
        }

        private static MucDoKho? ParseMucDo(string text, Dictionary<MucDoKho, (string Ten, System.Drawing.Color Mau)> config)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            string t = text.Trim();

            foreach (var kv in config)
            {
                if (string.Equals(kv.Value.Ten, t, StringComparison.OrdinalIgnoreCase))
                {
                    return kv.Key;
                }
            }

            if (t.Equals("1") || t.Equals("Nhận biết", StringComparison.OrdinalIgnoreCase)) return MucDoKho.NhanBiet;
            if (t.Equals("2") || t.Equals("Thông hiểu", StringComparison.OrdinalIgnoreCase)) return MucDoKho.ThongHieu;
            if (t.Equals("3") || t.Equals("Vận dụng", StringComparison.OrdinalIgnoreCase)) return MucDoKho.VanDung;
            if (t.Equals("4") || t.Equals("Vận dụng cao", StringComparison.OrdinalIgnoreCase)) return MucDoKho.VanDungCao;
            return null;
        }

        private static LoaiCauHoi? ParseLoaiCauHoi(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            string t = text.Trim().ToLowerInvariant();
            if (t == "1" || t.Contains("một đáp án") || t.Contains("1 đáp án")) return LoaiCauHoi.TracNghiemMotDapAn;
            if (t == "2" || t.Contains("nhiều đáp án")) return LoaiCauHoi.TracNghiemNhieuDapAn;
            if (t == "3" || t.Contains("tự luận") || t.Contains("trả lời ngắn")) return LoaiCauHoi.TuLuanTraLoiNgan;
            return null;
        }

        public static string FormatLoaiCauHoi(LoaiCauHoi loai) => loai switch
        {
            LoaiCauHoi.TracNghiemMotDapAn => "Một đáp án đúng",
            LoaiCauHoi.TracNghiemNhieuDapAn => "Nhiều đáp án đúng",
            LoaiCauHoi.TuLuanTraLoiNgan => "Tự luận / Trả lời ngắn",
            _ => "Khác"
        };

        #endregion
    }
}
