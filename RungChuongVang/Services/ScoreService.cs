using System;
using System.Collections.Generic;
using System.Linq;
using RungChuongVang.DataAccess;
using RungChuongVang.Enums;
using RungChuongVang.Models;

namespace RungChuongVang.Services
{
    /// <summary>
    /// Chấm đáp án cho 3 loại câu hỏi và tính toán thứ hạng cuối cùng theo luật Rung Chuông Vàng.
    /// </summary>
    public class ScoreService
    {
        private readonly ChuongTrinhRepository _ctRepo = new();
        private readonly LichSuRepository _lichSuRepo = new();

        /// <summary>
        /// Đánh giá đáp án cho câu hỏi 1 đáp án, nhiều đáp án (khớp tập hợp tuyệt đối) và tự luận.
        /// </summary>
        public KetQuaDungSai EvaluateAnswer(CauHoi cauHoi, string? rawAnswer)
        {
            if (string.IsNullOrWhiteSpace(rawAnswer))
            {
                return KetQuaDungSai.KhongTraLoi;
            }

            string normalized = rawAnswer.Trim();

            if (cauHoi.LoaiCauHoi == LoaiCauHoi.TracNghiemMotDapAn)
            {
                string? correctKey = cauHoi.DanhSachDapAn
                    .FirstOrDefault(d => d.LaDapAnDung)?.KyHieu
                    .Trim().ToUpperInvariant();

                return string.Equals(normalized.ToUpperInvariant(), correctKey, StringComparison.Ordinal)
                    ? KetQuaDungSai.Dung
                    : KetQuaDungSai.Sai;
            }

            if (cauHoi.LoaiCauHoi == LoaiCauHoi.TracNghiemNhieuDapAn)
            {
                var correctSet = cauHoi.DanhSachDapAn
                    .Where(d => d.LaDapAnDung)
                    .Select(d => d.KyHieu.Trim().ToUpperInvariant())
                    .ToHashSet(StringComparer.Ordinal);

                var selectedSet = normalized
                    .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim().ToUpperInvariant())
                    .ToHashSet(StringComparer.Ordinal);

                // Phải chọn ĐẦY ĐỦ đáp án đúng và KHÔNG thừa đáp án sai
                return correctSet.Count > 0 && selectedSet.SetEquals(correctSet)
                    ? KetQuaDungSai.Dung
                    : KetQuaDungSai.Sai;
            }

            // Tự luận / Trả lời ngắn: so khớp gợi ý đáp án chuẩn (MC luôn xác nhận lại trước khi chốt)
            string? standardText = cauHoi.DanhSachDapAn.FirstOrDefault()?.NoiDungDapAn.Trim();
            if (!string.IsNullOrWhiteSpace(standardText) &&
                string.Equals(normalized, standardText, StringComparison.OrdinalIgnoreCase))
            {
                return KetQuaDungSai.Dung;
            }

            return KetQuaDungSai.ChoXuLy;
        }

        /// <summary>
        /// Tính toán và lưu thứ hạng cuối cùng cho toàn bộ thí sinh hoặc đội thi trong chương trình.
        /// </summary>
        public List<ChuongTrinhThiSinh> CalculateAndSaveRankings(int chuongTrinhId, PhuongThucThi mode)
        {
            var participants = _ctRepo.GetParticipants(chuongTrinhId, mode);

            // Người còn trụ lại / chiến thắng xếp cao nhất; tiếp theo xếp theo số câu đúng giảm dần và câu bị loại sâu hơn
            var ordered = participants
                .OrderByDescending(p => p.TrangThaiThi == TrangThaiThi.ChienThang ? 2 :
                                         (p.TrangThaiThi != TrangThaiThi.BiLoai && p.TrangThaiThi != TrangThaiThi.ChuaThamGia ? 1 : 0))
                .ThenByDescending(p => p.SoCauDung)
                .ThenByDescending(p => p.CauHoiBiLoai ?? int.MaxValue)
                .ThenBy(p => p.SoCauSai)
                .ThenBy(p => p.SoBaoDanh)
                .ToList();

            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                int rank = 1;
                for (int i = 0; i < ordered.Count; i++)
                {
                    if (i > 0)
                    {
                        var prev = ordered[i - 1];
                        var curr = ordered[i];
                        bool sameTie =
                            (prev.TrangThaiThi == TrangThaiThi.ChienThang) == (curr.TrangThaiThi == TrangThaiThi.ChienThang) &&
                            (prev.TrangThaiThi != TrangThaiThi.BiLoai) == (curr.TrangThaiThi != TrangThaiThi.BiLoai) &&
                            prev.SoCauDung == curr.SoCauDung &&
                            prev.CauHoiBiLoai == curr.CauHoiBiLoai;
                        if (!sameTie) rank = i + 1;
                    }

                    ordered[i].ThuHang = rank;
                    DatabaseHelper.ExecuteNonQuery(
                        conn, tx,
                        "UPDATE ChuongTrinh_ThiSinh SET ThuHang = ? WHERE ID = ?",
                        rank, ordered[i].ID);
                }
            });

            return ordered;
        }

        /// <summary>
        /// Điều chỉnh kết quả / thứ hạng theo quyết định chính thức của Ban Tổ Chức có ghi lịch sử đầy đủ.
        /// </summary>
        public void AdjustOfficialResult(
            int chuongTrinhId,
            int participantRecordId,
            TrangThaiThi newStatus,
            int newCorrectCount,
            int newRank,
            string reason,
            string actor = "Ban Tổ Chức")
        {
            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                DatabaseHelper.ExecuteNonQuery(
                    conn, tx,
                    "UPDATE ChuongTrinh_ThiSinh SET TrangThaiThi = ?, SoCauDung = ?, ThuHang = ? WHERE ID = ?",
                    (int)newStatus, newCorrectCount, newRank, participantRecordId);

                _lichSuRepo.InsertLichSuThaoTacInternal(conn, tx, new LichSuThaoTac
                {
                    ChuongTrinhID = chuongTrinhId,
                    LoaiThaoTac = "DIEU_CHINH_BTC",
                    DoiTuongID = participantRecordId,
                    DuLieuCu = string.Empty,
                    DuLieuMoi = $"Trạng thái={newStatus}; Số câu đúng={newCorrectCount}; Hạng={newRank}; Lý do={reason}",
                    DaHoanTac = false,
                    ThoiGian = DateTime.Now,
                    NguoiThucHien = actor
                });
            });
        }
    }
}
