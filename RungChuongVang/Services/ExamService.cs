using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RungChuongVang.DataAccess;
using RungChuongVang.Enums;
using RungChuongVang.Models;

namespace RungChuongVang.Services
{
    public class ParticipantAnswerConfirmation
    {
        public int ParticipantRecordId { get; set; }
        public int? ThiSinhId { get; set; }
        public int? DoiThiId { get; set; }
        public string DapAnDaChon { get; set; } = string.Empty;
        public KetQuaDungSai KetQua { get; set; } = KetQuaDungSai.Dung;
        public double ThoiGianTraLoi { get; set; }
        public string GhiChu { get; set; } = string.Empty;
        public List<MemberAnswerItem> MemberAnswers { get; set; } = new();
    }

    public class MemberAnswerItem
    {
        public int ThiSinhId { get; set; }
        public string DapAnDaChon { get; set; } = string.Empty;
        public KetQuaDungSai KetQua { get; set; }
    }

    public enum RoundEndStatus
    {
        ContinueNormal,
        OnlyOneRemaining,
        ZeroRemaining,
        OutOfQuestionsWithMultipleRemaining
    }

    public class RoundEndCheckResult
    {
        public RoundEndStatus Status { get; set; }
        public int RemainingCount { get; set; }
        public List<ChuongTrinhThiSinh> RemainingParticipants { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Bộ máy điều hành thi Rung Chuông Vàng truyền thống (dùng chung cho thi Cá nhân và thi Theo đội).
    /// Thực hiện giao dịch dữ liệu an toàn cho: xác nhận kết quả, loại thí sinh/đội, cứu trợ, khôi phục, hoàn tác và kết thúc.
    /// </summary>
    public class ExamService
    {
        private readonly ChuongTrinhRepository _ctRepo = new();
        private readonly KetQuaRepository _kqRepo = new();
        private readonly LichSuRepository _lsRepo = new();
        private readonly ScoreService _scoreService = new();

        private sealed class UndoSnapshotPayload
        {
            public int? CauHoiId { get; set; }
            public List<ParticipantSnapshot> Records { get; set; } = new();
        }

        private sealed class ParticipantSnapshot
        {
            public int ID { get; set; }
            public TrangThaiThi TrangThaiThi { get; set; }
            public int SoCauDung { get; set; }
            public int SoCauSai { get; set; }
            public int SoCauKhongTraLoi { get; set; }
            public int? CauHoiBiLoai { get; set; }
            public DateTime? ThoiDiemBiLoai { get; set; }
            public int SoLanCuuTro { get; set; }
        }

        public void StartProgram(int chuongTrinhId, string actor = "MC")
        {
            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                DatabaseHelper.ExecuteNonQuery(
                    conn, tx,
                    "UPDATE ChuongTrinh SET TrangThai = ?, DaKhoaBoCauHoi = True WHERE ChuongTrinhID = ?",
                    (int)TrangThaiChuongTrinhEnum.DangDienRa, chuongTrinhId);

                _lsRepo.InsertLichSuThaoTacInternal(conn, tx, new LichSuThaoTac
                {
                    ChuongTrinhID = chuongTrinhId,
                    LoaiThaoTac = "BAT_DAU_CHUONG_TRINH",
                    DuLieuMoi = "Bắt đầu chương trình và cố định bộ câu hỏi",
                    ThoiGian = DateTime.Now,
                    NguoiThucHien = actor
                });
            });
        }

        public void SaveLiveState(TrangThaiChuongTrinhModel state)
        {
            _kqRepo.SaveLiveProgramState(state);
        }

        public TrangThaiChuongTrinhModel? LoadLiveState(int chuongTrinhId)
        {
            return _kqRepo.LoadLiveProgramState(chuongTrinhId);
        }

        /// <summary>
        /// Xác nhận kết quả câu hỏi cho thí sinh hoặc đội thi (hỗ trợ cả xác nhận hàng loạt và 3 quy tắc đội).
        /// Không bao giờ tự động loại trước khi MC xác nhận qua phương thức này.
        /// </summary>
        public void ConfirmParticipantResults(
            int chuongTrinhId,
            int cauHoiId,
            int questionOrder,
            List<ParticipantAnswerConfirmation> confirmations,
            QuyTacTraLoiDoi? teamRule = null,
            string actor = "MC")
        {
            if (confirmations.Count == 0) return;

            var oldSnapshots = new List<ParticipantSnapshot>();
            var newSnapshots = new List<ParticipantSnapshot>();

            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                foreach (var item in confirmations)
                {
                    var current = _kqRepo.GetParticipantRecordById(item.ParticipantRecordId);
                    if (current == null) continue;

                    // Chỉ xử lý thí sinh/đội đang thi (tránh tính trùng cho người đã bị loại trước đó)
                    if (current.TrangThaiThi == TrangThaiThi.BiLoai || current.TrangThaiThi == TrangThaiThi.ChuaThamGia)
                    {
                        continue;
                    }

                    oldSnapshots.Add(ToSnapshot(current));

                    // Nếu thi theo đội với Quy tắc 3: lưu kết quả từng thành viên, nếu bất kỳ thành viên nào sai/không trả lời -> đội bị loại
                    KetQuaDungSai finalResult = item.KetQua;
                    if (item.DoiThiId.HasValue && teamRule == QuyTacTraLoiDoi.TatCaThanhVienPhaiDung && item.MemberAnswers.Count > 0)
                    {
                        foreach (var mem in item.MemberAnswers)
                        {
                            _kqRepo.UpsertKetQuaInternal(conn, tx, new KetQua
                            {
                                ChuongTrinhID = chuongTrinhId,
                                CauHoiID = cauHoiId,
                                ThiSinhID = mem.ThiSinhId,
                                DoiThiID = item.DoiThiId,
                                DapAnDaChon = mem.DapAnDaChon,
                                KetQuaDungSai = mem.KetQua,
                                ThoiGianTraLoi = item.ThoiGianTraLoi,
                                TrangThaiXuLy = TrangThaiXuLyKetQua.DaXacNhan,
                                Diem = mem.KetQua == KetQuaDungSai.Dung ? 1 : 0,
                                GhiChu = "Thành viên đội (Quy tắc tất cả phải đúng)"
                            });
                        }

                        if (item.MemberAnswers.Any(m => m.KetQua == KetQuaDungSai.Sai || m.KetQua == KetQuaDungSai.KhongTraLoi))
                        {
                            finalResult = KetQuaDungSai.Sai;
                        }
                        else if (item.MemberAnswers.All(m => m.KetQua == KetQuaDungSai.Dung))
                        {
                            finalResult = KetQuaDungSai.Dung;
                        }
                    }

                    // Lưu kết quả chính thức của thí sinh / đội
                    _kqRepo.UpsertKetQuaInternal(conn, tx, new KetQua
                    {
                        ChuongTrinhID = chuongTrinhId,
                        CauHoiID = cauHoiId,
                        ThiSinhID = item.ThiSinhId,
                        DoiThiID = item.DoiThiId,
                        DapAnDaChon = item.DapAnDaChon,
                        KetQuaDungSai = finalResult,
                        ThoiGianTraLoi = item.ThoiGianTraLoi,
                        TrangThaiXuLy = finalResult == KetQuaDungSai.ChoXuLy
                            ? TrangThaiXuLyKetQua.Tam
                            : TrangThaiXuLyKetQua.DaXacNhan,
                        Diem = finalResult == KetQuaDungSai.Dung ? 1 : 0,
                        GhiChu = item.GhiChu
                    });

                    if (finalResult == KetQuaDungSai.Dung)
                    {
                        current.TrangThaiThi = TrangThaiThi.DangThi;
                        current.SoCauDung++;
                    }
                    else if (finalResult == KetQuaDungSai.Sai || finalResult == KetQuaDungSai.KhongTraLoi)
                    {
                        current.TrangThaiThi = TrangThaiThi.BiLoai;
                        if (finalResult == KetQuaDungSai.Sai) current.SoCauSai++;
                        else current.SoCauKhongTraLoi++;
                        current.CauHoiBiLoai = questionOrder;
                        current.ThoiDiemBiLoai = DateTime.Now;

                        _lsRepo.InsertLichSuLoaiInternal(conn, tx, new LichSuLoai
                        {
                            ChuongTrinhID = chuongTrinhId,
                            CauHoiID = cauHoiId,
                            ThiSinhID = item.ThiSinhId,
                            DoiThiID = item.DoiThiId,
                            LyDoLoai = finalResult == KetQuaDungSai.Sai ? "Trả lời sai" : "Không trả lời khi hết giờ",
                            ThoiGianLoai = DateTime.Now,
                            NguoiThucHien = actor,
                            GhiChu = item.GhiChu
                        });
                    }

                    UpdateParticipantRowInternal(conn, tx, current);
                    newSnapshots.Add(ToSnapshot(current));
                }

                if (oldSnapshots.Count > 0)
                {
                    var oldPayload = new UndoSnapshotPayload { CauHoiId = cauHoiId, Records = oldSnapshots };
                    var newPayload = new UndoSnapshotPayload { CauHoiId = cauHoiId, Records = newSnapshots };

                    _lsRepo.InsertLichSuThaoTacInternal(conn, tx, new LichSuThaoTac
                    {
                        ChuongTrinhID = chuongTrinhId,
                        LoaiThaoTac = "XAC_NHAN_KET_QUA",
                        DoiTuongID = cauHoiId,
                        DuLieuCu = JsonSerializer.Serialize(oldPayload),
                        DuLieuMoi = JsonSerializer.Serialize(newPayload),
                        DaHoanTac = false,
                        ThoiGian = DateTime.Now,
                        NguoiThucHien = actor
                    });
                }
            });
        }

        public void EliminateManually(int chuongTrinhId, int cauHoiId, int questionOrder, int participantRecordId, string reason, string actor = "MC")
        {
            var current = _kqRepo.GetParticipantRecordById(participantRecordId)
                ?? throw new InvalidOperationException("Không tìm thấy thí sinh/đội.");

            if (current.TrangThaiThi == TrangThaiThi.BiLoai) return;

            var oldSnap = new UndoSnapshotPayload { CauHoiId = cauHoiId, Records = new() { ToSnapshot(current) } };

            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                current.TrangThaiThi = TrangThaiThi.BiLoai;
                current.CauHoiBiLoai = questionOrder;
                current.ThoiDiemBiLoai = DateTime.Now;
                UpdateParticipantRowInternal(conn, tx, current);

                _lsRepo.InsertLichSuLoaiInternal(conn, tx, new LichSuLoai
                {
                    ChuongTrinhID = chuongTrinhId,
                    CauHoiID = cauHoiId,
                    ThiSinhID = current.ThiSinhID,
                    DoiThiID = current.DoiThiID,
                    LyDoLoai = string.IsNullOrWhiteSpace(reason) ? "Loại thủ công" : reason,
                    ThoiGianLoai = DateTime.Now,
                    NguoiThucHien = actor
                });

                var newSnap = new UndoSnapshotPayload { CauHoiId = cauHoiId, Records = new() { ToSnapshot(current) } };
                _lsRepo.InsertLichSuThaoTacInternal(conn, tx, new LichSuThaoTac
                {
                    ChuongTrinhID = chuongTrinhId,
                    LoaiThaoTac = "LOAI_THU_CONG",
                    DoiTuongID = participantRecordId,
                    DuLieuCu = JsonSerializer.Serialize(oldSnap),
                    DuLieuMoi = JsonSerializer.Serialize(newSnap),
                    DaHoanTac = false,
                    ThoiGian = DateTime.Now,
                    NguoiThucHien = actor
                });
            });
        }

        /// <summary>
        /// Cứu trợ hoặc Khôi phục thí sinh / đội đã bị loại (không xóa LichSuLoai trước đó).
        /// </summary>
        public int RescueParticipants(
            int chuongTrinhId,
            int? currentQuestionId,
            List<int> participantRecordIds,
            string reason,
            bool isSingleRestore = false,
            string actor = "MC")
        {
            if (participantRecordIds.Count == 0) return 0;

            var oldSnapshots = new List<ParticipantSnapshot>();
            var newSnapshots = new List<ParticipantSnapshot>();
            int rescuedCount = 0;

            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                foreach (int recId in participantRecordIds.Distinct())
                {
                    var current = _kqRepo.GetParticipantRecordById(recId);
                    if (current == null || current.TrangThaiThi != TrangThaiThi.BiLoai) continue;

                    oldSnapshots.Add(ToSnapshot(current));

                    current.TrangThaiThi = isSingleRestore ? TrangThaiThi.DuocKhoiPhuc : TrangThaiThi.DuocCuuTro;
                    current.SoLanCuuTro++;
                    UpdateParticipantRowInternal(conn, tx, current);

                    _lsRepo.InsertLichSuCuuTroInternal(conn, tx, new LichSuCuuTro
                    {
                        ChuongTrinhID = chuongTrinhId,
                        CauHoiIDHienTai = currentQuestionId,
                        ThiSinhID = current.ThiSinhID,
                        DoiThiID = current.DoiThiID,
                        LyDoCuuTro = string.IsNullOrWhiteSpace(reason)
                            ? (isSingleRestore ? "Khôi phục thí sinh/đội" : "Cứu trợ")
                            : reason,
                        ThoiGianCuuTro = DateTime.Now,
                        NguoiThucHien = actor
                    });

                    newSnapshots.Add(ToSnapshot(current));
                    rescuedCount++;
                }

                if (rescuedCount > 0)
                {
                    _lsRepo.InsertLichSuThaoTacInternal(conn, tx, new LichSuThaoTac
                    {
                        ChuongTrinhID = chuongTrinhId,
                        LoaiThaoTac = isSingleRestore ? "KHOI_PHUC" : "CUU_TRO",
                        DoiTuongID = currentQuestionId,
                        DuLieuCu = JsonSerializer.Serialize(new UndoSnapshotPayload { CauHoiId = currentQuestionId, Records = oldSnapshots }),
                        DuLieuMoi = JsonSerializer.Serialize(new UndoSnapshotPayload { CauHoiId = currentQuestionId, Records = newSnapshots }),
                        DaHoanTac = false,
                        ThoiGian = DateTime.Now,
                        NguoiThucHien = actor
                    });
                }
            });

            return rescuedCount;
        }

        public string? PreviewLastUndoDescription(int chuongTrinhId)
        {
            var latest = _lsRepo.GetLatestUndoableAction(chuongTrinhId);
            if (latest == null) return null;

            string label = latest.LoaiThaoTac switch
            {
                "XAC_NHAN_KET_QUA" => "Xác nhận kết quả câu hỏi",
                "LOAI_THU_CONG" => "Loại thủ công thí sinh/đội",
                "CUU_TRO" => "Cứu trợ thí sinh/đội",
                "KHOI_PHUC" => "Khôi phục thí sinh/đội",
                _ => latest.LoaiThaoTac
            };
            return $"{label} lúc {latest.ThoiGian:HH:mm:ss} bởi {latest.NguoiThucHien}";
        }

        /// <summary>
        /// Hoàn tác thao tác gần nhất trong LichSuThaoTac, không xóa lịch sử mà ghi nhận thêm thao tác HOAN_TAC.
        /// </summary>
        public bool UndoLastAction(int chuongTrinhId, out string message, string actor = "MC")
        {
            var latest = _lsRepo.GetLatestUndoableAction(chuongTrinhId);
            if (latest == null)
            {
                message = "Không có thao tác nào khả dụng để hoàn tác.";
                return false;
            }

            UndoSnapshotPayload? oldPayload = JsonSerializer.Deserialize<UndoSnapshotPayload>(latest.DuLieuCu);
            if (oldPayload == null || oldPayload.Records.Count == 0)
            {
                message = "Dữ liệu hoàn tác không hợp lệ.";
                return false;
            }

            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                foreach (var snap in oldPayload.Records)
                {
                    var current = _kqRepo.GetParticipantRecordById(snap.ID);
                    if (current == null) continue;

                    current.TrangThaiThi = snap.TrangThaiThi;
                    current.SoCauDung = snap.SoCauDung;
                    current.SoCauSai = snap.SoCauSai;
                    current.SoCauKhongTraLoi = snap.SoCauKhongTraLoi;
                    current.CauHoiBiLoai = snap.CauHoiBiLoai;
                    current.ThoiDiemBiLoai = snap.ThoiDiemBiLoai;
                    current.SoLanCuuTro = snap.SoLanCuuTro;

                    UpdateParticipantRowInternal(conn, tx, current);

                    if (latest.LoaiThaoTac == "XAC_NHAN_KET_QUA" && oldPayload.CauHoiId.HasValue)
                    {
                        if (current.ThiSinhID.HasValue)
                        {
                            DatabaseHelper.ExecuteNonQuery(
                                conn, tx,
                                "UPDATE KetQua SET TrangThaiXuLy = ? WHERE ChuongTrinhID = ? AND CauHoiID = ? AND ThiSinhID = ?",
                                (int)TrangThaiXuLyKetQua.DaHoanTac, chuongTrinhId, oldPayload.CauHoiId.Value, current.ThiSinhID.Value);
                        }
                        else if (current.DoiThiID.HasValue)
                        {
                            DatabaseHelper.ExecuteNonQuery(
                                conn, tx,
                                "UPDATE KetQua SET TrangThaiXuLy = ? WHERE ChuongTrinhID = ? AND CauHoiID = ? AND DoiThiID = ?",
                                (int)TrangThaiXuLyKetQua.DaHoanTac, chuongTrinhId, oldPayload.CauHoiId.Value, current.DoiThiID.Value);
                        }
                    }
                }

                // Đánh dấu bản ghi gốc đã được hoàn tác và ghi thêm dòng HOAN_TAC (không xóa lịch sử)
                DatabaseHelper.ExecuteNonQuery(conn, tx, "UPDATE LichSuThaoTac SET DaHoanTac = True WHERE ID = ?", latest.ID);

                _lsRepo.InsertLichSuThaoTacInternal(conn, tx, new LichSuThaoTac
                {
                    ChuongTrinhID = chuongTrinhId,
                    LoaiThaoTac = "HOAN_TAC",
                    DoiTuongID = latest.ID,
                    DuLieuCu = latest.DuLieuMoi,
                    DuLieuMoi = $"Đã hoàn tác thao tác #{latest.ID} ({latest.LoaiThaoTac})",
                    DaHoanTac = true,
                    ThoiGian = DateTime.Now,
                    NguoiThucHien = actor
                });
            });

            message = $"Đã hoàn tác thành công thao tác '{latest.LoaiThaoTac}'.";
            return true;
        }

        public RoundEndCheckResult CheckRoundEndCondition(int chuongTrinhId, PhuongThucThi mode, int currentQuestionIndex, int totalQuestions)
        {
            var all = _ctRepo.GetParticipants(chuongTrinhId, mode);
            var remaining = all.Where(p =>
                p.TrangThaiThi == TrangThaiThi.DangThi ||
                p.TrangThaiThi == TrangThaiThi.TraLoiDung ||
                p.TrangThaiThi == TrangThaiThi.DuocCuuTro ||
                p.TrangThaiThi == TrangThaiThi.DuocKhoiPhuc ||
                p.TrangThaiThi == TrangThaiThi.ChienThang).ToList();

            string unit = mode == PhuongThucThi.ThiCaNhan ? "thí sinh" : "đội thi";

            if (remaining.Count == 1)
            {
                return new RoundEndCheckResult
                {
                    Status = RoundEndStatus.OnlyOneRemaining,
                    RemainingCount = 1,
                    RemainingParticipants = remaining,
                    Message = $"Chỉ còn duy nhất 1 {unit} trên sàn đấu: {remaining[0].TenHienThi}. MC có thể xác nhận người chiến thắng!"
                };
            }

            if (remaining.Count == 0)
            {
                return new RoundEndCheckResult
                {
                    Status = RoundEndStatus.ZeroRemaining,
                    RemainingCount = 0,
                    RemainingParticipants = remaining,
                    Message = $"Không còn {unit} nào trên sàn đấu! Hãy tiến hành Cứu trợ hoặc Kết thúc chương trình."
                };
            }

            if (currentQuestionIndex >= totalQuestions)
            {
                return new RoundEndCheckResult
                {
                    Status = RoundEndStatus.OutOfQuestionsWithMultipleRemaining,
                    RemainingCount = remaining.Count,
                    RemainingParticipants = remaining,
                    Message = $"Đã hết bộ câu hỏi chính nhưng vẫn còn {remaining.Count} {unit}. MC có thể dùng Câu hỏi phụ hoặc xét số câu đúng để kết thúc."
                };
            }

            return new RoundEndCheckResult
            {
                Status = RoundEndStatus.ContinueNormal,
                RemainingCount = remaining.Count,
                RemainingParticipants = remaining
            };
        }

        public void ConfirmWinnersAndFinish(int chuongTrinhId, PhuongThucThi mode, List<int> winnerRecordIds, string actor = "MC")
        {
            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                foreach (int recId in winnerRecordIds)
                {
                    DatabaseHelper.ExecuteNonQuery(
                        conn, tx,
                        "UPDATE ChuongTrinh_ThiSinh SET TrangThaiThi = ? WHERE ID = ?",
                        (int)TrangThaiThi.ChienThang, recId);
                }

                DatabaseHelper.ExecuteNonQuery(
                    conn, tx,
                    "UPDATE ChuongTrinh SET TrangThai = ? WHERE ChuongTrinhID = ?",
                    (int)TrangThaiChuongTrinhEnum.DaKetThuc, chuongTrinhId);

                _lsRepo.InsertLichSuThaoTacInternal(conn, tx, new LichSuThaoTac
                {
                    ChuongTrinhID = chuongTrinhId,
                    LoaiThaoTac = "KET_THUC_CHUONG_TRINH",
                    DuLieuMoi = $"Xác nhận {winnerRecordIds.Count} đối tượng chiến thắng và kết thúc chương trình",
                    ThoiGian = DateTime.Now,
                    NguoiThucHien = actor
                });
            });

            _scoreService.CalculateAndSaveRankings(chuongTrinhId, mode);
        }

        private static void UpdateParticipantRowInternal(System.Data.OleDb.OleDbConnection conn, System.Data.OleDb.OleDbTransaction tx, ChuongTrinhThiSinh p)
        {
            DatabaseHelper.ExecuteNonQuery(
                conn, tx,
                @"UPDATE ChuongTrinh_ThiSinh
                  SET TrangThaiThi = ?, SoCauDung = ?, SoCauSai = ?, SoCauKhongTraLoi = ?,
                      CauHoiBiLoai = ?, ThoiDiemBiLoai = ?, SoLanCuuTro = ?
                  WHERE ID = ?",
                (int)p.TrangThaiThi,
                p.SoCauDung,
                p.SoCauSai,
                p.SoCauKhongTraLoi,
                p.CauHoiBiLoai,
                p.ThoiDiemBiLoai,
                p.SoLanCuuTro,
                p.ID);
        }

        private static ParticipantSnapshot ToSnapshot(ChuongTrinhThiSinh p) => new()
        {
            ID = p.ID,
            TrangThaiThi = p.TrangThaiThi,
            SoCauDung = p.SoCauDung,
            SoCauSai = p.SoCauSai,
            SoCauKhongTraLoi = p.SoCauKhongTraLoi,
            CauHoiBiLoai = p.CauHoiBiLoai,
            ThoiDiemBiLoai = p.ThoiDiemBiLoai,
            SoLanCuuTro = p.SoLanCuuTro
        };
    }
}
