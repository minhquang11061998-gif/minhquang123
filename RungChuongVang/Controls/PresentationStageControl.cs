using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using RungChuongVang.Enums;
using RungChuongVang.Helpers;
using RungChuongVang.Models;
using RungChuongVang.Services;

namespace RungChuongVang.Controls
{
    /// <summary>
    /// Canvas trình chiếu sân khấu dùng chung cho cả Màn hình lớn (FrmTrinhChieu) và Khung xem trước (Khu vực A).
    /// Tự động co giãn tỷ lệ sắc nét cho 9 trạng thái gameshow + Màn hình Vinh danh + Pháo giấy.
    /// </summary>
    public sealed class PresentationStageControl : Control
    {
        private ExamStateManager? _state;
        private readonly System.Windows.Forms.Timer _animTimer;
        private readonly List<ConfettiParticle> _confetti = new();
        private float _pulsePhase;

        private string? _cachedQuestionImgPath;
        private Image? _cachedQuestionImg;
        private string? _cachedWinnerImgPath;
        private Image? _cachedWinnerImg;

        public bool EnableAnimation { get; set; } = true;

        public PresentationStageControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = ThemeColors.NavyDark;

            _animTimer = new System.Windows.Forms.Timer { Interval = 33 };
            _animTimer.Tick += OnAnimTick;
        }

        public void BindState(ExamStateManager stateManager)
        {
            if (_state != null)
            {
                _state.StateChanged -= OnStateUpdated;
                _state.TimerChanged -= OnTimerUpdated;
            }

            _state = stateManager;
            _state.StateChanged += OnStateUpdated;
            _state.TimerChanged += OnTimerUpdated;

            UpdateAnimationLoop();
            Invalidate();
        }

        private void OnStateUpdated()
        {
            PresentationService.RunOnUi(this, () =>
            {
                UpdateAnimationLoop();
                Invalidate();
            });
        }

        private void OnTimerUpdated(double rem, int disp, bool lastFive)
        {
            PresentationService.RunOnUi(this, Invalidate);
        }

        private void UpdateAnimationLoop()
        {
            if (!EnableAnimation || _state == null)
            {
                _animTimer.Stop();
                return;
            }

            bool needAnim =
                _state.DisplayState == TrangThaiTrinhChieu.ManHinhCho ||
                _state.DisplayState == TrangThaiTrinhChieu.ManHinhChienThang ||
                _state.DisplayState == TrangThaiTrinhChieu.ManHinhVinhDanh ||
                _state.DisplayState == TrangThaiTrinhChieu.ManHinhCuuTro;

            if (needAnim)
            {
                if (_state.TriggerConfetti && _confetti.Count == 0)
                {
                    InitConfetti();
                }
                if (!_animTimer.Enabled) _animTimer.Start();
            }
            else
            {
                _animTimer.Stop();
                _confetti.Clear();
            }
        }

        private void InitConfetti()
        {
            _confetti.Clear();
            Color[] palette = { ThemeColors.GoldBright, Color.White, Color.FromArgb(56, 189, 248), Color.FromArgb(248, 113, 113), Color.FromArgb(74, 222, 128) };
            for (int i = 0; i < 85; i++)
            {
                _confetti.Add(new ConfettiParticle
                {
                    X = (float)Random.Shared.NextDouble(),
                    Y = (float)Random.Shared.NextDouble(),
                    SpeedY = 0.004f + (float)Random.Shared.NextDouble() * 0.008f,
                    DriftX = ((float)Random.Shared.NextDouble() - 0.5f) * 0.003f,
                    Size = 6 + Random.Shared.Next(8),
                    Color = palette[i % palette.Length]
                });
            }
        }

        private void OnAnimTick(object? sender, EventArgs e)
        {
            _pulsePhase = (_pulsePhase + 0.08f) % ((float)Math.PI * 2);
            if (_state != null && _state.TriggerConfetti)
            {
                foreach (var c in _confetti)
                {
                    c.Y += c.SpeedY;
                    c.X += c.DriftX;
                    if (c.Y > 1.05f)
                    {
                        c.Y = -0.05f;
                        c.X = (float)Random.Shared.NextDouble();
                    }
                }
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            float scale = Math.Min(Width / 1280f, Height / 720f);
            if (scale <= 0.05f) scale = 0.05f;

            DrawStageBackground(g);

            if (_state == null || _state.CurrentProgram == null)
            {
                DrawCenteredText(g, "PHẦN MỀM RUNG CHUÔNG VÀNG", new Font("Segoe UI", Math.Max(8f, 38f * scale), FontStyle.Bold), ThemeColors.GoldBright, new RectangleF(0, Height * 0.38f, Width, Height * 0.2f));
                return;
            }

            switch (_state.DisplayState)
            {
                case TrangThaiTrinhChieu.ManHinhCho:
                    DrawStandbyScreen(g, scale);
                    break;
                case TrangThaiTrinhChieu.GioiThieuCauHoi:
                case TrangThaiTrinhChieu.HienThiDapAn:
                case TrangThaiTrinhChieu.DemNguoc:
                case TrangThaiTrinhChieu.CongBoDapAn:
                    DrawQuestionFlowScreen(g, scale);
                    break;
                case TrangThaiTrinhChieu.ThongKeThiSinh:
                    DrawStatisticsScreen(g, scale);
                    break;
                case TrangThaiTrinhChieu.ManHinhCuuTro:
                    DrawRescueScreen(g, scale);
                    break;
                case TrangThaiTrinhChieu.ManHinhChungKet:
                    DrawFinalRoundScreen(g, scale);
                    break;
                case TrangThaiTrinhChieu.ManHinhChienThang:
                    DrawStageWinnerScreen(g, scale);
                    DrawConfetti(g, scale);
                    break;
                case TrangThaiTrinhChieu.ManHinhVinhDanh:
                    DrawSpotlightWinnerScreen(g, scale);
                    DrawConfetti(g, scale);
                    break;
            }
        }

        private void DrawStageBackground(Graphics g)
        {
            using var bgBrush = new LinearGradientBrush(
                ClientRectangle,
                Color.FromArgb(6, 17, 38),
                Color.FromArgb(16, 48, 96),
                LinearGradientMode.Vertical);
            g.FillRectangle(bgBrush, ClientRectangle);

            // Ánh sáng sân khấu trung tâm
            using var path = new GraphicsPath();
            path.AddEllipse(-Width * 0.1f, -Height * 0.2f, Width * 1.2f, Height * 1.1f);
            using var pgb = new PathGradientBrush(path)
            {
                CenterColor = Color.FromArgb(55, 245, 197, 66),
                SurroundColors = new[] { Color.Transparent }
            };
            g.FillEllipse(pgb, -Width * 0.1f, -Height * 0.2f, Width * 1.2f, Height * 1.1f);

            // Viền vàng kim khung sân khấu
            int pad = Math.Max(2, (int)(Width * 0.01f));
            using var goldPen = new Pen(Color.FromArgb(140, 212, 175, 55), Math.Max(1f, Width * 0.002f));
            g.DrawRectangle(goldPen, pad, pad, Width - pad * 2, Height - pad * 2);
        }

        private void DrawStandbyScreen(Graphics g, float scale)
        {
            float glow = 18 + (float)Math.Sin(_pulsePhase) * 6;
            DrawBellEmblem(g, Width / 2f, Height * 0.25f, 70 * scale, glow * scale);

            using var fOrg = new Font("Segoe UI", Math.Max(7f, 20f * scale), FontStyle.Bold);
            using var fTitle = new Font("Segoe UI", Math.Max(10f, 54f * scale), FontStyle.Bold);
            using var fProg = new Font("Segoe UI", Math.Max(8f, 28f * scale), FontStyle.Bold);

            DrawCenteredText(g, _state!.OrganizationName.ToUpperInvariant(), fOrg, Color.FromArgb(215, 228, 248), new RectangleF(Width * 0.08f, Height * 0.41f, Width * 0.84f, Height * 0.09f));
            DrawCenteredText(g, "RUNG CHUÔNG VÀNG", fTitle, ThemeColors.GoldBright, new RectangleF(Width * 0.06f, Height * 0.49f, Width * 0.88f, Height * 0.16f));
            DrawCenteredText(g, _state.CurrentProgram!.TenChuongTrinh.ToUpperInvariant(), fProg, Color.White, new RectangleF(Width * 0.08f, Height * 0.67f, Width * 0.84f, Height * 0.14f));
        }

        private void DrawQuestionFlowScreen(Graphics g, float scale)
        {
            var slot = _state!.CurrentQuestionSlot;
            if (slot?.CauHoiChiTiet == null) return;
            var q = slot.CauHoiChiTiet;

            // Header bar: CÂU HỎI X / N | Mức độ | Đồng hồ (nếu đang đếm giờ)
            float headerH = Height * 0.14f;
            string qBadge = slot.LaCauHoiPhu
                ? $"CÂU HỎI PHỤ"
                : $"CÂU HỎI {_state.CurrentQuestionIndex + 1:00} / {_state.Questions.Count(x => !x.LaCauHoiPhu):00}";

            using var fBadge = new Font("Segoe UI", Math.Max(7f, 22f * scale), FontStyle.Bold);
            using var fLevel = new Font("Segoe UI", Math.Max(6f, 16f * scale), FontStyle.Bold);

            DrawPillBox(g, new RectangleF(Width * 0.04f, Height * 0.04f, Width * 0.25f, Height * 0.085f), ThemeColors.Gold, ThemeColors.NavyDark, qBadge, fBadge);

            string levelName = _state.MucDoConfig.TryGetValue(q.MucDo, out var lv) ? lv.Ten : q.MucDo.ToString();
            Color levelColor = _state.MucDoConfig.TryGetValue(q.MucDo, out var lv2) ? lv2.Mau : ThemeColors.PrimaryBlue;
            DrawPillBox(g, new RectangleF(Width * 0.31f, Height * 0.04f, Width * 0.20f, Height * 0.085f), levelColor, Color.White, levelName.ToUpperInvariant(), fLevel);

            // Thống kê còn lại góc trên phải
            string aliveText = _state.CurrentProgram!.PhuongThucThi == PhuongThucThi.ThiCaNhan
                ? $"CÒN LẠI: {_state.RemainingParticipantsCount}/{_state.TotalParticipants} TS"
                : $"CÒN LẠI: {_state.RemainingParticipantsCount}/{_state.TotalParticipants} ĐỘI";
            DrawPillBox(g, new RectangleF(Width * 0.54f, Height * 0.04f, Width * 0.26f, Height * 0.085f), Color.FromArgb(20, 59, 115), Color.White, aliveText, fLevel);

            // Đồng hồ đếm ngược tròn nổi bật góc phải
            DrawCountdownBadge(g, new RectangleF(Width * 0.83f, Height * 0.025f, Height * 0.14f, Height * 0.14f), scale);

            // Khung nội dung câu hỏi (+ ảnh minh họa nếu có)
            Image? qImg = GetCachedQuestionImage(q.DuongDanHinhAnh);
            float contentTop = headerH + Height * 0.03f;
            float contentHeight = _state.ShowAnswersOnStage ? Height * 0.33f : Height * 0.72f;

            var qBox = new RectangleF(Width * 0.04f, contentTop, qImg != null ? Width * 0.62f : Width * 0.92f, contentHeight);
            DrawCard(g, qBox, Color.FromArgb(230, 14, 37, 74), ThemeColors.Gold, Math.Max(1f, 2.5f * scale));

            using var fQuestion = new Font("Segoe UI", Math.Max(8f, (q.NoiDung.Length > 150 ? 22f : 28f) * scale), FontStyle.Bold);
            var qTextRect = new RectangleF(qBox.X + 20 * scale, qBox.Y + 14 * scale, qBox.Width - 40 * scale, qBox.Height - 28 * scale);
            DrawCenteredText(g, q.NoiDung, fQuestion, Color.White, qTextRect);

            if (qImg != null)
            {
                var imgBox = new RectangleF(Width * 0.68f, contentTop, Width * 0.28f, contentHeight);
                DrawCard(g, imgBox, Color.FromArgb(200, 10, 24, 48), ThemeColors.BorderLight, 1.5f);
                DrawImageAspect(g, qImg, RectangleF.Inflate(imgBox, -8 * scale, -8 * scale));
            }

            // Hiển thị phương án A, B, C, D hoặc Đáp án tự luận
            if (_state.ShowAnswersOnStage)
            {
                float ansTop = contentTop + contentHeight + Height * 0.03f;
                float ansAreaH = Height - ansTop - (_state.RevealCorrectAnswer && !string.IsNullOrWhiteSpace(q.GiaiThich) ? Height * 0.13f : Height * 0.05f);

                if (q.LoaiCauHoi == LoaiCauHoi.TuLuanTraLoiNgan)
                {
                    var essayBox = new RectangleF(Width * 0.12f, ansTop + ansAreaH * 0.18f, Width * 0.76f, ansAreaH * 0.55f);
                    string text = _state.RevealCorrectAnswer
                        ? $"ĐÁP ÁN CHÍNH XÁC: {q.DanhSachDapAn.FirstOrDefault()?.NoiDungDapAn}"
                        : "CÂU HỎI TỰ LUẬN — MỜI THÍ SINH VIẾT ĐÁP ÁN";
                    Color bg = _state.RevealCorrectAnswer ? Color.FromArgb(22, 163, 74) : Color.FromArgb(20, 59, 115);
                    DrawCard(g, essayBox, bg, ThemeColors.GoldBright, 2.5f * scale);
                    using var fEssay = new Font("Segoe UI", Math.Max(8f, 26f * scale), FontStyle.Bold);
                    DrawCenteredText(g, text, fEssay, Color.White, essayBox);
                }
                else
                {
                    var answers = q.DanhSachDapAn.Take(4).ToList();
                    float gapX = Width * 0.025f;
                    float gapY = Height * 0.025f;
                    float cellW = (Width * 0.92f - gapX) / 2f;
                    float cellH = (ansAreaH - gapY) / 2f;
                    using var fOpt = new Font("Segoe UI", Math.Max(7f, 21f * scale), FontStyle.Bold);

                    for (int i = 0; i < answers.Count; i++)
                    {
                        int col = i % 2;
                        int row = i / 2;
                        var optRect = new RectangleF(
                            Width * 0.04f + col * (cellW + gapX),
                            ansTop + row * (cellH + gapY),
                            cellW,
                            cellH);

                        bool isCorrect = answers[i].LaDapAnDung;
                        Color fill = _state.RevealCorrectAnswer && isCorrect
                            ? ThemeColors.SuccessGreen
                            : (_state.RevealCorrectAnswer ? Color.FromArgb(140, 20, 44, 82) : Color.FromArgb(230, 20, 59, 115));
                        Color border = _state.RevealCorrectAnswer && isCorrect ? ThemeColors.GoldBright : Color.FromArgb(160, 212, 175, 55);

                        DrawCard(g, optRect, fill, border, (_state.RevealCorrectAnswer && isCorrect ? 4f : 2f) * scale);
                        string optText = $"{answers[i].KyHieu}.  {answers[i].NoiDungDapAn}";
                        DrawLeftMiddleText(g, optText, fOpt, Color.White, RectangleF.Inflate(optRect, -18 * scale, -6 * scale));
                    }
                }

                // Dòng giải thích đáp án khi công bố
                if (_state.RevealCorrectAnswer && !string.IsNullOrWhiteSpace(q.GiaiThich))
                {
                    var expRect = new RectangleF(Width * 0.04f, Height * 0.88f, Width * 0.92f, Height * 0.09f);
                    DrawCard(g, expRect, ThemeColors.GoldSoft, ThemeColors.Gold, 1.5f);
                    using var fExp = new Font("Segoe UI", Math.Max(6f, 16f * scale), FontStyle.Bold);
                    DrawCenteredText(g, $"Giải thích: {q.GiaiThich}", fExp, ThemeColors.NavyDark, RectangleF.Inflate(expRect, -12 * scale, -4 * scale));
                }
            }
        }

        private void DrawCountdownBadge(Graphics g, RectangleF rect, float scale)
        {
            bool warn = _state!.IsLastFiveSeconds || _state.DisplaySeconds == 0;
            Color bg = warn ? ThemeColors.DangerRed : ThemeColors.RoyalBlue;
            Color ring = warn ? ThemeColors.GoldBright : ThemeColors.Gold;

            using var brush = new SolidBrush(bg);
            g.FillEllipse(brush, rect);
            using var pen = new Pen(ring, Math.Max(2f, 5f * scale));
            g.DrawEllipse(pen, rect);

            using var fTime = new Font("Segoe UI", Math.Max(9f, 36f * scale), FontStyle.Bold);
            DrawCenteredText(g, _state.DisplaySeconds.ToString("00"), fTime, Color.White, rect);
        }

        private void DrawStatisticsScreen(Graphics g, float scale)
        {
            bool isTeam = _state!.CurrentProgram!.PhuongThucThi == PhuongThucThi.ThiTheoDoi;
            string unit = isTeam ? "ĐỘI THI" : "THÍ SINH";

            using var fHeader = new Font("Segoe UI", Math.Max(9f, 34f * scale), FontStyle.Bold);
            DrawCenteredText(g, $"THỐNG KÊ {unit}", fHeader, ThemeColors.GoldBright, new RectangleF(0, Height * 0.05f, Width, Height * 0.1f));

            float cardW = Width * 0.27f;
            float cardH = Height * 0.22f;
            DrawStatTile(g, new RectangleF(Width * 0.06f, Height * 0.18f, cardW, cardH), "TỔNG BAN ĐẦU", _state.TotalParticipants.ToString(), ThemeColors.RoyalBlue, scale);
            DrawStatTile(g, new RectangleF(Width * 0.365f, Height * 0.18f, cardW, cardH), "CÒN TRÊN SÀN", _state.RemainingParticipantsCount.ToString(), ThemeColors.SuccessGreen, scale);
            DrawStatTile(g, new RectangleF(Width * 0.67f, Height * 0.18f, cardW, cardH), "ĐÃ BỊ LOẠI", _state.EliminatedParticipantsCount.ToString(), ThemeColors.DangerRed, scale);

            var targetList = _state.StatsShowEliminatedOnly
                ? _state.Participants.Where(p => p.TrangThaiThi == TrangThaiThi.BiLoai).ToList()
                : _state.Participants.Where(ExamStateManager.IsParticipantActive).ToList();

            string listTitle = _state.StatsShowEliminatedOnly
                ? $"DANH SÁCH {unit} BỊ LOẠI ({targetList.Count})"
                : $"DANH SÁCH {unit} CÒN THI ({targetList.Count})";

            using var fSub = new Font("Segoe UI", Math.Max(7f, 20f * scale), FontStyle.Bold);
            DrawCenteredText(g, listTitle, fSub, Color.White, new RectangleF(0, Height * 0.43f, Width, Height * 0.07f));

            DrawParticipantChipGrid(g, targetList, new RectangleF(Width * 0.06f, Height * 0.51f, Width * 0.88f, Height * 0.42f), scale, _state.StatsShowEliminatedOnly ? ThemeColors.DangerRed : ThemeColors.PrimaryBlue);
        }

        private void DrawRescueScreen(Graphics g, float scale)
        {
            DrawBellEmblem(g, Width / 2f, Height * 0.19f, 54 * scale, 16 * scale);
            using var fTitle = new Font("Segoe UI", Math.Max(9f, 38f * scale), FontStyle.Bold);
            DrawCenteredText(g, _state!.RescueMessage, fTitle, ThemeColors.GoldBright, new RectangleF(Width * 0.05f, Height * 0.32f, Width * 0.90f, Height * 0.14f));

            var list = _state.RescuedNames;
            if (list.Count > 0)
            {
                using var fNames = new Font("Segoe UI", Math.Max(7f, 22f * scale), FontStyle.Bold);
                string joined = string.Join("   •   ", list.Take(24));
                var box = new RectangleF(Width * 0.08f, Height * 0.50f, Width * 0.84f, Height * 0.36f);
                DrawCard(g, box, Color.FromArgb(200, 14, 37, 74), ThemeColors.Gold, 2.5f * scale);
                DrawCenteredText(g, joined, fNames, Color.White, RectangleF.Inflate(box, -20 * scale, -16 * scale));
            }
        }

        private void DrawFinalRoundScreen(Graphics g, float scale)
        {
            var finalists = _state!.Participants.Where(ExamStateManager.IsParticipantActive).ToList();
            using var fTitle = new Font("Segoe UI", Math.Max(10f, 40f * scale), FontStyle.Bold);
            DrawCenteredText(g, "VÒNG CHUNG KẾT - RUNG CHUÔNG VÀNG", fTitle, ThemeColors.GoldBright, new RectangleF(0, Height * 0.06f, Width, Height * 0.12f));

            using var fSub = new Font("Segoe UI", Math.Max(7f, 22f * scale), FontStyle.Bold);
            DrawCenteredText(g, $"CÁC GƯƠNG MẶT XUẤT SẮC CÒN LẠI ({finalists.Count})", fSub, Color.White, new RectangleF(0, Height * 0.18f, Width, Height * 0.08f));

            DrawParticipantChipGrid(g, finalists, new RectangleF(Width * 0.08f, Height * 0.28f, Width * 0.84f, Height * 0.62f), scale, ThemeColors.RoyalBlue);
        }

        private void DrawStageWinnerScreen(Graphics g, float scale)
        {
            var winners = _state!.Participants.Where(p => p.TrangThaiThi == TrangThaiThi.ChienThang).ToList();
            if (winners.Count == 0)
            {
                winners = _state.Participants.Where(ExamStateManager.IsParticipantActive).ToList();
            }

            DrawBellEmblem(g, Width / 2f, Height * 0.22f, 68 * scale, 22 * scale);

            using var fHead = new Font("Segoe UI", Math.Max(9f, 36f * scale), FontStyle.Bold);
            using var fWinner = new Font("Segoe UI", Math.Max(11f, 48f * scale), FontStyle.Bold);
            using var fSub = new Font("Segoe UI", Math.Max(7f, 24f * scale), FontStyle.Bold);

            DrawCenteredText(g, "CHÚC MỪNG NGƯỜI CHIẾN THẮNG RUNG CHUÔNG VÀNG", fHead, ThemeColors.GoldBright, new RectangleF(Width * 0.06f, Height * 0.36f, Width * 0.88f, Height * 0.12f));

            string names = winners.Count > 0 ? string.Join(" & ", winners.Select(w => w.TenHienThi.ToUpperInvariant())) : "NHÀ VÔ ĐỊCH";
            string details = winners.Count > 0 ? string.Join(" | ", winners.Select(w => $"{w.TenLop} - {w.SoCauDung} câu đúng")) : string.Empty;

            DrawCenteredText(g, names, fWinner, Color.White, new RectangleF(Width * 0.06f, Height * 0.50f, Width * 0.88f, Height * 0.18f));
            DrawCenteredText(g, details, fSub, ThemeColors.GoldSoft, new RectangleF(Width * 0.08f, Height * 0.70f, Width * 0.84f, Height * 0.12f));
        }

        private void DrawSpotlightWinnerScreen(Graphics g, float scale)
        {
            var w = _state!.ActiveSpotlightWinner;
            if (w == null)
            {
                DrawStageWinnerScreen(g, scale);
                return;
            }

            using var fTop = new Font("Segoe UI", Math.Max(8f, 28f * scale), FontStyle.Bold);
            using var fAward = new Font("Segoe UI", Math.Max(9f, 32f * scale), FontStyle.Bold);
            using var fName = new Font("Segoe UI", Math.Max(10f, 42f * scale), FontStyle.Bold);
            using var fMeta = new Font("Segoe UI", Math.Max(7f, 21f * scale), FontStyle.Bold);
            using var fWish = new Font("Segoe UI", Math.Max(6f, 18f * scale), FontStyle.Italic);

            DrawCenteredText(g, "VINH DANH NGƯỜI CHIẾN THẮNG", fTop, ThemeColors.GoldBright, new RectangleF(0, Height * 0.04f, Width, Height * 0.08f));
            DrawCenteredText(g, w.TenGiaiThuong.ToUpperInvariant(), fAward, Color.White, new RectangleF(0, Height * 0.11f, Width, Height * 0.08f));

            // Khung ảnh vinh danh trung tâm
            Image? winImg = GetCachedWinnerImage(w.DuongDanHinhAnh);
            var imgFrame = new RectangleF(Width * 0.12f, Height * 0.22f, Width * 0.28f, Height * 0.52f);
            DrawCard(g, imgFrame, Color.FromArgb(20, 42, 82), ThemeColors.GoldBright, 3.5f * scale);
            if (winImg != null)
            {
                DrawImageAspect(g, winImg, RectangleF.Inflate(imgFrame, -8 * scale, -8 * scale));
            }
            else
            {
                DrawBellEmblem(g, imgFrame.X + imgFrame.Width / 2f, imgFrame.Y + imgFrame.Height / 2f, 58 * scale, 16 * scale);
            }

            // Thông tin bên phải khung ảnh
            var infoRect = new RectangleF(Width * 0.43f, Height * 0.23f, Width * 0.49f, Height * 0.50f);
            DrawCard(g, infoRect, Color.FromArgb(190, 12, 32, 66), ThemeColors.Gold, 2f * scale);

            DrawLeftMiddleText(g, w.HoTen.ToUpperInvariant(), fName, ThemeColors.GoldBright, new RectangleF(infoRect.X + 24 * scale, infoRect.Y + 18 * scale, infoRect.Width - 48 * scale, infoRect.Height * 0.28f));

            string line2 = !string.IsNullOrWhiteSpace(w.TenDoi) ? $"Đội: {w.TenDoi}  |  Lớp: {w.TenLop}" : $"Lớp: {w.TenLop}";
            if (!string.IsNullOrWhiteSpace(w.SoBaoDanh)) line2 = $"SBD: {w.SoBaoDanh}  |  {line2}";
            DrawLeftMiddleText(g, line2, fMeta, Color.White, new RectangleF(infoRect.X + 24 * scale, infoRect.Y + infoRect.Height * 0.34f, infoRect.Width - 48 * scale, infoRect.Height * 0.18f));
            DrawLeftMiddleText(g, w.TenTruong, fMeta, Color.FromArgb(200, 220, 245), new RectangleF(infoRect.X + 24 * scale, infoRect.Y + infoRect.Height * 0.52f, infoRect.Width - 48 * scale, infoRect.Height * 0.16f));

            string statsLine = $"Thành tích: {w.ThanhTich}  •  Đúng {w.SoCauDung} câu  •  Vượt qua {w.SoCauVuotQua} câu";
            DrawLeftMiddleText(g, statsLine, fMeta, ThemeColors.GoldSoft, new RectangleF(infoRect.X + 24 * scale, infoRect.Y + infoRect.Height * 0.72f, infoRect.Width - 48 * scale, infoRect.Height * 0.22f));

            string wish = string.IsNullOrWhiteSpace(w.GhiChu)
                ? "CHÚC MỪNG BẠN ĐÃ XUẤT SẮC VƯỢT QUA CÁC THỬ THÁCH RUNG CHUÔNG VÀNG!"
                : w.GhiChu;
            DrawCenteredText(g, wish, fWish, ThemeColors.GoldBright, new RectangleF(Width * 0.06f, Height * 0.80f, Width * 0.88f, Height * 0.12f));
        }

        private void DrawParticipantChipGrid(Graphics g, List<ChuongTrinhThiSinh> items, RectangleF area, float scale, Color chipColor)
        {
            int maxShow = Math.Min(items.Count, 24);
            if (maxShow == 0) return;

            int cols = maxShow <= 6 ? 2 : (maxShow <= 12 ? 3 : 4);
            int rows = (int)Math.Ceiling(maxShow / (double)cols);
            float gap = 10 * scale;
            float w = (area.Width - gap * (cols - 1)) / cols;
            float h = Math.Min(64 * scale, (area.Height - gap * (rows - 1)) / rows);

            using var fChip = new Font("Segoe UI", Math.Max(6f, 15f * scale), FontStyle.Bold);
            for (int i = 0; i < maxShow; i++)
            {
                int r = i / cols;
                int c = i % cols;
                var rect = new RectangleF(area.X + c * (w + gap), area.Y + r * (h + gap), w, h);
                DrawCard(g, rect, chipColor, ThemeColors.Gold, 1.5f);
                string txt = $"{items[i].SoBaoDanh} - {items[i].TenHienThi}";
                DrawCenteredText(g, txt, fChip, Color.White, RectangleF.Inflate(rect, -6 * scale, -2 * scale));
            }
        }

        private void DrawStatTile(Graphics g, RectangleF rect, string label, string value, Color fill, float scale)
        {
            DrawCard(g, rect, fill, ThemeColors.Gold, 2f * scale);
            using var fLbl = new Font("Segoe UI", Math.Max(6f, 16f * scale), FontStyle.Bold);
            using var fVal = new Font("Segoe UI", Math.Max(10f, 44f * scale), FontStyle.Bold);
            DrawCenteredText(g, label, fLbl, Color.FromArgb(225, 235, 250), new RectangleF(rect.X, rect.Y + 12 * scale, rect.Width, rect.Height * 0.30f));
            DrawCenteredText(g, value, fVal, ThemeColors.GoldBright, new RectangleF(rect.X, rect.Y + rect.Height * 0.35f, rect.Width, rect.Height * 0.58f));
        }

        private void DrawConfetti(Graphics g, float scale)
        {
            foreach (var c in _confetti)
            {
                using var b = new SolidBrush(c.Color);
                g.FillRectangle(b, c.X * Width, c.Y * Height, c.Size * scale, c.Size * 0.6f * scale);
            }
        }

        private static void DrawBellEmblem(Graphics g, float cx, float cy, float radius, float glowExpand)
        {
            using var glowBrush = new SolidBrush(Color.FromArgb(45, 245, 197, 66));
            g.FillEllipse(glowBrush, cx - radius - glowExpand, cy - radius - glowExpand, (radius + glowExpand) * 2, (radius + glowExpand) * 2);

            using var bellBrush = new LinearGradientBrush(
                new RectangleF(cx - radius, cy - radius, radius * 2, radius * 2),
                ThemeColors.GoldBright,
                ThemeColors.Gold,
                LinearGradientMode.ForwardDiagonal);
            g.FillEllipse(bellBrush, cx - radius, cy - radius, radius * 2, radius * 2);

            using var pen = new Pen(Color.White, Math.Max(1.5f, radius * 0.06f));
            g.DrawEllipse(pen, cx - radius, cy - radius, radius * 2, radius * 2);

            using var fBell = new Font("Segoe UI", Math.Max(8f, radius * 0.58f), FontStyle.Bold);
            DrawCenteredText(g, "RCV", fBell, ThemeColors.NavyDark, new RectangleF(cx - radius, cy - radius, radius * 2, radius * 2));
        }

        private static void DrawPillBox(Graphics g, RectangleF rect, Color bg, Color fg, string text, Font font)
        {
            DrawCard(g, rect, bg, ThemeColors.GoldBright, 1.5f);
            DrawCenteredText(g, text, font, fg, rect);
        }

        private static void DrawCard(Graphics g, RectangleF rect, Color fill, Color border, float borderWidth)
        {
            using var b = new SolidBrush(fill);
            g.FillRectangle(b, rect);
            using var p = new Pen(border, borderWidth);
            g.DrawRectangle(p, rect.X, rect.Y, rect.Width, rect.Height);
        }

        private static void DrawCenteredText(Graphics g, string text, Font font, Color color, RectangleF rect)
        {
            using var brush = new SolidBrush(color);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord };
            g.DrawString(text, font, brush, rect, sf);
        }

        private static void DrawLeftMiddleText(Graphics g, string text, Font font, Color color, RectangleF rect)
        {
            using var brush = new SolidBrush(color);
            using var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord };
            g.DrawString(text, font, brush, rect, sf);
        }

        private static void DrawImageAspect(Graphics g, Image img, RectangleF target)
        {
            float ratio = Math.Min(target.Width / img.Width, target.Height / img.Height);
            float w = img.Width * ratio;
            float h = img.Height * ratio;
            float x = target.X + (target.Width - w) / 2f;
            float y = target.Y + (target.Height - h) / 2f;
            g.DrawImage(img, x, y, w, h);
        }

        private Image? GetCachedQuestionImage(string? relPath)
        {
            if (string.IsNullOrWhiteSpace(relPath)) return null;
            if (_cachedQuestionImgPath == relPath) return _cachedQuestionImg;
            _cachedQuestionImg?.Dispose();
            _cachedQuestionImgPath = relPath;
            _cachedQuestionImg = ImageHelper.LoadImageSafe(relPath);
            return _cachedQuestionImg;
        }

        private Image? GetCachedWinnerImage(string? relPath)
        {
            if (string.IsNullOrWhiteSpace(relPath)) return null;
            if (_cachedWinnerImgPath == relPath) return _cachedWinnerImg;
            _cachedWinnerImg?.Dispose();
            _cachedWinnerImgPath = relPath;
            _cachedWinnerImg = ImageHelper.LoadImageSafe(relPath);
            return _cachedWinnerImg;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animTimer.Stop();
                _animTimer.Dispose();
                _cachedQuestionImg?.Dispose();
                _cachedWinnerImg?.Dispose();
                if (_state != null)
                {
                    _state.StateChanged -= OnStateUpdated;
                    _state.TimerChanged -= OnTimerUpdated;
                }
            }
            base.Dispose(disposing);
        }

        private sealed class ConfettiParticle
        {
            public float X;
            public float Y;
            public float SpeedY;
            public float DriftX;
            public float Size;
            public Color Color;
        }
    }
}
