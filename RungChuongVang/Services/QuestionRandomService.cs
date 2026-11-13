using System;
using System.Collections.Generic;
using System.Linq;
using RungChuongVang.DataAccess;
using RungChuongVang.Enums;
using RungChuongVang.Models;

namespace RungChuongVang.Services
{
    public class QuestionRandomRequest
    {
        public List<int> LopHocIds { get; set; } = new();
        public List<int> MonHocIds { get; set; } = new();
        public List<int> ChuDeIds { get; set; } = new();
        public int TotalQuestions { get; set; } = 20;
        public Dictionary<MucDoKho, int> PercentageByLevel { get; set; } = new()
        {
            [MucDoKho.NhanBiet] = 20,
            [MucDoKho.ThongHieu] = 30,
            [MucDoKho.VanDung] = 30,
            [MucDoKho.VanDungCao] = 20
        };
        public bool AutoFillIfShortage { get; set; }
    }

    public class QuestionRandomResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Warnings { get; set; } = new();
        public List<CauHoi> SelectedQuestions { get; set; } = new();
        public Dictionary<MucDoKho, (int Needed, int Available, int Selected)> LevelBreakdown { get; set; } = new();
    }

    /// <summary>
    /// Dịch vụ trộn câu hỏi ngẫu nhiên từ ngân hàng câu hỏi theo Lớp, Môn, Chủ đề và Tỷ lệ mức độ khó.
    /// Đảm bảo: không lấy câu ngoài lớp chọn, không trùng lặp, không lấy câu ngừng sử dụng, đúng tổng số câu.
    /// </summary>
    public class QuestionRandomService
    {
        private readonly CauHoiRepository _cauHoiRepo = new();
        private readonly DanhMucRepository _danhMucRepo = new();

        public QuestionRandomResult GenerateRandomQuestions(QuestionRandomRequest request)
        {
            var result = new QuestionRandomResult();
            var mucDoConfig = _danhMucRepo.GetMucDoConfig();

            if (request.TotalQuestions <= 0)
            {
                result.Message = "Tổng số câu hỏi cần trộn phải lớn hơn 0.";
                return result;
            }

            int totalPercent = request.PercentageByLevel.Values.Sum();
            if (totalPercent != 100)
            {
                result.Message = $"Tổng tỷ lệ các mức độ phải đúng bằng 100% (hiện tại là {totalPercent}%).";
                return result;
            }

            // Nạp tất cả câu hỏi ĐANG SỬ DỤNG và lọc chính xác theo phạm vi lớp, môn, chủ đề
            var allActive = _cauHoiRepo.SearchCauHoi(trangThai: true);
            var filteredPool = allActive.Where(q =>
            {
                if (request.LopHocIds.Count > 0 && !q.LopHocIds.Any(id => request.LopHocIds.Contains(id)))
                {
                    return false;
                }
                if (request.MonHocIds.Count > 0 && (!q.MonHocID.HasValue || !request.MonHocIds.Contains(q.MonHocID.Value)))
                {
                    return false;
                }
                if (request.ChuDeIds.Count > 0 && (!q.ChuDeID.HasValue || !request.ChuDeIds.Contains(q.ChuDeID.Value)))
                {
                    return false;
                }
                return true;
            }).ToList();

            if (filteredPool.Count < request.TotalQuestions)
            {
                result.Message = $"Ngân hàng câu hỏi phù hợp bộ lọc chỉ có {filteredPool.Count} câu, không đủ {request.TotalQuestions} câu yêu cầu.";
                return result;
            }

            // Phân bổ số lượng từng mức độ theo phương pháp số dư lớn nhất (đảm bảo tổng chính xác tuyệt đối)
            Dictionary<MucDoKho, int> quotas = AllocateExactQuotas(request.TotalQuestions, request.PercentageByLevel);

            var selected = new List<CauHoi>();
            var usedIds = new HashSet<int>();
            var shortages = new List<string>();

            foreach (MucDoKho level in Enum.GetValues<MucDoKho>())
            {
                int needed = quotas[level];
                var poolForLevel = Shuffle(filteredPool.Where(q => q.MucDo == level).ToList());
                int available = poolForLevel.Count;

                int takeCount = Math.Min(needed, available);
                for (int i = 0; i < takeCount; i++)
                {
                    selected.Add(poolForLevel[i]);
                    usedIds.Add(poolForLevel[i].CauHoiID);
                }

                result.LevelBreakdown[level] = (needed, available, takeCount);

                if (available < needed)
                {
                    string levelName = mucDoConfig.TryGetValue(level, out var info) ? info.Ten : level.ToString();
                    shortages.Add($"- Mức độ '{levelName}': cần {needed} câu nhưng chỉ có {available} câu.");
                }
            }

            if (shortages.Count > 0 && !request.AutoFillIfShortage)
            {
                result.IsSuccess = false;
                result.Warnings = shortages;
                result.Message = "Không đủ câu hỏi ở một số mức độ khó:\n" + string.Join("\n", shortages) +
                                 "\n\nHãy điều chỉnh tỷ lệ hoặc bật tùy chọn 'Tự động bù từ mức độ khác'.";
                return result;
            }

            // Bù phần còn thiếu nếu người dùng cho phép bù từ mức độ khác trong cùng phạm vi lớp/môn/chủ đề
            if (selected.Count < request.TotalQuestions && request.AutoFillIfShortage)
            {
                int remaining = request.TotalQuestions - selected.Count;
                var fallbackPool = Shuffle(filteredPool.Where(q => !usedIds.Contains(q.CauHoiID)).ToList());
                foreach (var q in fallbackPool.Take(remaining))
                {
                    selected.Add(q);
                    usedIds.Add(q.CauHoiID);
                }
                result.Warnings = shortages;
            }

            // Trộn ngẫu nhiên thứ tự cuối cùng (giữ thứ tự tăng dần độ khó nhẹ hoặc trộn đều toàn bộ)
            result.SelectedQuestions = Shuffle(selected);
            result.IsSuccess = result.SelectedQuestions.Count == request.TotalQuestions;
            result.Message = result.Warnings.Count > 0
                ? $"Đã trộn {result.SelectedQuestions.Count} câu hỏi (có tự động bù từ mức độ khác)."
                : $"Đã trộn thành công {result.SelectedQuestions.Count} câu hỏi theo đúng tỷ lệ.";
            return result;
        }

        private static Dictionary<MucDoKho, int> AllocateExactQuotas(int total, Dictionary<MucDoKho, int> percentages)
        {
            var quotas = new Dictionary<MucDoKho, int>();
            var remainders = new List<(MucDoKho Level, double Remainder)>();

            int allocated = 0;
            foreach (MucDoKho lv in Enum.GetValues<MucDoKho>())
            {
                int pct = percentages.TryGetValue(lv, out int v) ? v : 0;
                double exact = total * (pct / 100.0);
                int floor = (int)Math.Floor(exact);
                quotas[lv] = floor;
                allocated += floor;
                remainders.Add((lv, exact - floor));
            }

            int diff = total - allocated;
            foreach (var item in remainders.OrderByDescending(r => r.Remainder))
            {
                if (diff <= 0) break;
                quotas[item.Level]++;
                diff--;
            }

            return quotas;
        }

        private static List<T> Shuffle<T>(List<T> source)
        {
            var copy = new List<T>(source);
            for (int i = copy.Count - 1; i > 0; i--)
            {
                int j = Random.Shared.Next(i + 1);
                (copy[i], copy[j]) = (copy[j], copy[i]);
            }
            return copy;
        }
    }
}
