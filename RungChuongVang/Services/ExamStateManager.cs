using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using RungChuongVang.DataAccess;
using RungChuongVang.Enums;
using RungChuongVang.Helpers;
using RungChuongVang.Models;

namespace RungChuongVang.Services
{
    /// <summary>
    /// Đối tượng quản lý trạng thái chương trình dùng chung giữa Control Screen và Presentation Screen.
    /// Đảm bảo nguồn dữ liệu duy nhất và đồng bộ tức thời qua Event.
    /// </summary>
    public sealed class ExamStateManager : IDisposable
    {
        private readonly ChuongTrinhRepository _ctRepo = new();
        private readonly DanhMucRepository _dmRepo = new();
        private readonly LichSuRepository _lsRepo = new();
        private readonly ExamService _examService = new();

        public TimerService Timer { get; } = new();

        public ChuongTrinh? CurrentProgram { get; private set; }
        public List<ChuongTrinhCauHoi> Questions { get; private set; } = new();
        public List<ChuongTrinhThiSinh> Participants { get; private set; } = new();
        public Dictionary<MucDoKho, (string Ten, Color Mau)> MucDoConfig { get; private set; } = new();

        public int CurrentQuestionIndex { get; private set; }
        public TrangThaiTrinhChieu DisplayState { get; private set; } = TrangThaiTrinhChieu.ManHinhCho;
        public bool ShowAnswersOnStage { get; private set; }
        public bool RevealCorrectAnswer { get; private set; }
        public bool StatsShowEliminatedOnly { get; private set; }

        public double RemainingSeconds { get; private set; } = 15;
        public int DisplaySeconds { get; private set; } = 15;
        public bool IsLastFiveSeconds { get; private set; }

        public string OrganizationName { get; private set; } = "BAN TỔ CHỨC RUNG CHUÔNG VÀNG";
        public string RescueMessage { get; private set; } = string.Empty;
        public List<string> RescuedNames { get; private set; } = new();
        public VinhDanh? ActiveSpotlightWinner { get; private set; }
        public bool TriggerConfetti { get; private set; }

        public event Action? StateChanged;
        public event Action<double, int, bool>? TimerChanged;
        public event Action? AuditLogUpdated;

        public ExamStateManager()
        {
            Timer.Tick += OnTimerTick;
            Timer.WarningSecondBeep += _ => SoundHelper.PlayWarningTick();
            Timer.TimeExpired += OnTimerExpired;
        }

        public ChuongTrinhCauHoi? CurrentQuestionSlot =>
            CurrentQuestionIndex >= 0 && CurrentQuestionIndex < Questions.Count
                ? Questions[CurrentQuestionIndex]
                : null;

        public int TotalParticipants => Participants.Count;
        public int RemainingParticipantsCount => Participants.Count(IsParticipantActive);
        public int EliminatedParticipantsCount => Participants.Count(p => p.TrangThaiThi == TrangThaiThi.BiLoai);

        public static bool IsParticipantActive(ChuongTrinhThiSinh p) =>
            p.TrangThaiThi == TrangThaiThi.DangThi ||
            p.TrangThaiThi == TrangThaiThi.TraLoiDung ||
            p.TrangThaiThi == TrangThaiThi.DuocCuuTro ||
            p.TrangThaiThi == TrangThaiThi.DuocKhoiPhuc ||
            p.TrangThaiThi == TrangThaiThi.ChienThang;

        public void LoadProgram(int chuongTrinhId)
        {
            CurrentProgram = _ctRepo.GetById(chuongTrinhId)
                ?? throw new InvalidOperationException("Không tìm thấy chương trình thi.");

            MucDoConfig = _dmRepo.GetMucDoConfig();
            Questions = _ctRepo.GetProgramQuestions(chuongTrinhId);
            RefreshParticipants();

            object? orgVal = DatabaseHelper.ExecuteScalar("SELECT GiaTri FROM CauHinh WHERE TenCauHinh = ?", "TenDonViMacDinh");
            OrganizationName = string.IsNullOrWhiteSpace(CurrentProgram.DonViToChuc)
                ? (orgVal?.ToString() ?? "BAN TỔ CHỨC RUNG CHUÔNG VÀNG")
                : CurrentProgram.DonViToChuc;

            // Khôi phục trạng thái nếu chương trình từng chạy trước đó
            var saved = _examService.LoadLiveState(chuongTrinhId);
            if (saved != null && Questions.Count > 0)
            {
                CurrentQuestionIndex = Math.Clamp(saved.CauHoiHienTai - 1, 0, Questions.Count - 1);
                DisplayState = saved.TrangThaiTrinhChieu;
                int qSec = CurrentQuestionSlot?.ThoiGianTraLoi ?? CurrentProgram.ThoiGianMacDinh;
                Timer.RestoreRemaining(qSec, saved.ThoiGianConLai, saved.TrangThaiDongHo);
                RemainingSeconds = Timer.RemainingSeconds;
                DisplaySeconds = Timer.DisplaySeconds;
            }
            else
            {
                CurrentQuestionIndex = 0;
                DisplayState = TrangThaiTrinhChieu.ManHinhCho;
                int qSec = CurrentQuestionSlot?.ThoiGianTraLoi ?? CurrentProgram.ThoiGianMacDinh;
                Timer.Configure(qSec);
                RemainingSeconds = qSec;
                DisplaySeconds = qSec;
            }

            ShowAnswersOnStage = DisplayState == TrangThaiTrinhChieu.HienThiDapAn ||
                                 DisplayState == TrangThaiTrinhChieu.DemNguoc ||
                                 DisplayState == TrangThaiTrinhChieu.CongBoDapAn;
            RevealCorrectAnswer = DisplayState == TrangThaiTrinhChieu.CongBoDapAn;
            TriggerConfetti = false;

            RaiseStateChanged();
        }

        public void RefreshParticipants()
        {
            if (CurrentProgram == null) return;
            Participants = _ctRepo.GetParticipants(CurrentProgram.ChuongTrinhID, CurrentProgram.PhuongThucThi);
        }

        public void ReloadQuestions(bool jumpToLast = false)
        {
            if (CurrentProgram == null) return;
            Questions = _ctRepo.GetProgramQuestions(CurrentProgram.ChuongTrinhID);
            if (jumpToLast && Questions.Count > 0)
            {
                CurrentQuestionIndex = Questions.Count - 1;
            }
            RaiseStateChanged();
        }

        public void StartProgram()
        {
            if (CurrentProgram == null) return;
            _examService.StartProgram(CurrentProgram.ChuongTrinhID);
            CurrentProgram.TrangThai = TrangThaiChuongTrinhEnum.DangDienRa;
            CurrentProgram.DaKhoaBoCauHoi = true;
            DisplayState = TrangThaiTrinhChieu.ManHinhCho;
            PersistLiveSnapshot();
            RaiseStateAndLog();
        }

        public void ShowQuestion()
        {
            if (CurrentQuestionSlot == null) return;
            Timer.Configure(CurrentQuestionSlot.ThoiGianTraLoi);
            ShowAnswersOnStage = false;
            RevealCorrectAnswer = false;
            DisplayState = TrangThaiTrinhChieu.GioiThieuCauHoi;
            LogAction("HIEN_CAU_HOI", $"Hiển thị Câu {CurrentQuestionIndex + 1}");
            PersistLiveSnapshot();
            RaiseStateAndLog();
        }

        public void ShowOptions()
        {
            if (CurrentQuestionSlot == null) return;
            ShowAnswersOnStage = true;
            RevealCorrectAnswer = false;
            DisplayState = TrangThaiTrinhChieu.HienThiDapAn;
            PersistLiveSnapshot();
            RaiseStateChanged();
        }

        public void StartCountdown()
        {
            if (CurrentQuestionSlot == null) return;
            ShowAnswersOnStage = true;
            RevealCorrectAnswer = false;
            DisplayState = TrangThaiTrinhChieu.DemNguoc;
            Timer.Start();
            LogAction("DEM_GIO", $"Bắt đầu đếm giờ Câu {CurrentQuestionIndex + 1} ({CurrentQuestionSlot.ThoiGianTraLoi}s)");
            PersistLiveSnapshot();
            RaiseStateAndLog();
        }

        public void PauseCountdown()
        {
            Timer.Pause();
            PersistLiveSnapshot();
            RaiseStateChanged();
        }

        public void ResumeCountdown()
        {
            Timer.Resume();
            PersistLiveSnapshot();
            RaiseStateChanged();
        }

        public void ResetCountdown()
        {
            Timer.Reset();
            PersistLiveSnapshot();
            RaiseStateChanged();
        }

        public void EndQuestionAndRevealAnswer()
        {
            Timer.Pause();
            ShowAnswersOnStage = true;
            RevealCorrectAnswer = true;
            DisplayState = TrangThaiTrinhChieu.CongBoDapAn;
            LogAction("CONG_BO_DAP_AN", $"Công bố đáp án Câu {CurrentQuestionIndex + 1}");
            PersistLiveSnapshot();
            RaiseStateAndLog();
        }

        public void NextQuestion()
        {
            if (CurrentQuestionIndex < Questions.Count - 1)
            {
                CurrentQuestionIndex++;
                ShowQuestion();
            }
        }

        public void PreviousQuestion()
        {
            if (CurrentQuestionIndex > 0)
            {
                CurrentQuestionIndex--;
                ShowQuestion();
            }
        }

        public void ShowParticipantsStats(bool eliminatedOnly)
        {
            RefreshParticipants();
            StatsShowEliminatedOnly = eliminatedOnly;
            DisplayState = TrangThaiTrinhChieu.ThongKeThiSinh;
            PersistLiveSnapshot();
            RaiseStateChanged();
        }

        public void ShowRescueScreen(int count, List<string> names, string customReason = "")
        {
            RefreshParticipants();
            RescuedNames = names;
            RescueMessage = string.IsNullOrWhiteSpace(customReason)
                ? $"CỨU TRỢ THÀNH CÔNG {count} {(CurrentProgram?.PhuongThucThi == PhuongThucThi.ThiTheoDoi ? "ĐỘI THI" : "THÍ SINH")}!"
                : $"{customReason.ToUpperInvariant()} ({count})";
            DisplayState = TrangThaiTrinhChieu.ManHinhCuuTro;
            SoundHelper.PlayGoldenBell();
            PersistLiveSnapshot();
            RaiseStateAndLog();
        }

        public void ShowFinalRoundScreen()
        {
            RefreshParticipants();
            DisplayState = TrangThaiTrinhChieu.ManHinhChungKet;
            PersistLiveSnapshot();
            RaiseStateChanged();
        }

        public void ShowWinnerStageScreen()
        {
            RefreshParticipants();
            TriggerConfetti = true;
            DisplayState = TrangThaiTrinhChieu.ManHinhChienThang;
            SoundHelper.PlayFanfare();
            PersistLiveSnapshot();
            RaiseStateAndLog();
        }

        public void ShowSpotlightWinner(VinhDanh winner)
        {
            ActiveSpotlightWinner = winner;
            TriggerConfetti = true;
            DisplayState = TrangThaiTrinhChieu.ManHinhVinhDanh;
            SoundHelper.PlayFanfare();
            RaiseStateChanged();
        }

        public void ReturnToStandby()
        {
            Timer.Pause();
            TriggerConfetti = false;
            DisplayState = TrangThaiTrinhChieu.ManHinhCho;
            PersistLiveSnapshot();
            RaiseStateChanged();
        }

        public void NotifyParticipantsUpdated()
        {
            RefreshParticipants();
            RaiseStateAndLog();
        }

        private void OnTimerTick(double remaining, int displaySec, bool isLastFive)
        {
            RemainingSeconds = remaining;
            DisplaySeconds = displaySec;
            IsLastFiveSeconds = isLastFive;
            TimerChanged?.Invoke(remaining, displaySec, isLastFive);
        }

        private void OnTimerExpired()
        {
            SoundHelper.PlayTimeUp();
            PersistLiveSnapshot();
            RaiseStateChanged();
        }

        private void PersistLiveSnapshot()
        {
            if (CurrentProgram == null) return;
            try
            {
                _examService.SaveLiveState(new TrangThaiChuongTrinhModel
                {
                    ChuongTrinhID = CurrentProgram.ChuongTrinhID,
                    CauHoiHienTai = CurrentQuestionIndex + 1,
                    TrangThaiTrinhChieu = DisplayState,
                    ThoiGianConLai = Timer.RemainingSeconds,
                    TrangThaiDongHo = Timer.State
                });
            }
            catch
            {
                // Không làm gián đoạn trình chiếu nếu lưu snapshot nền tạm bận
            }
        }

        private void LogAction(string type, string desc)
        {
            if (CurrentProgram == null) return;
            try
            {
                _lsRepo.LogSimpleAction(CurrentProgram.ChuongTrinhID, type, desc);
            }
            catch { }
        }

        private void RaiseStateChanged() => StateChanged?.Invoke();

        private void RaiseStateAndLog()
        {
            StateChanged?.Invoke();
            AuditLogUpdated?.Invoke();
        }

        public void Dispose()
        {
            Timer.Dispose();
        }
    }
}
