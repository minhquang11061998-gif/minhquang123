using System;
using System.Diagnostics;
using System.Timers;
using RungChuongVang.Enums;

namespace RungChuongVang.Services
{
    /// <summary>
    /// Đồng hồ đếm ngược sử dụng System.Diagnostics.Stopwatch để bảo đảm thời gian thực chính xác tuyệt đối,
    /// không bị sai số do Timer giao diện.
    /// </summary>
    public sealed class TimerService : IDisposable
    {
        private readonly Stopwatch _stopwatch = new();
        private readonly System.Timers.Timer _pollTimer;
        private readonly object _sync = new();

        private double _totalDurationSeconds = 15;
        private double _accumulatedElapsedBeforePause;
        private int _lastBeepSecond = -1;
        private bool _expiredFired;

        public TrangThaiDongHo State { get; private set; } = TrangThaiDongHo.ChuaChay;
        public double TotalDurationSeconds => _totalDurationSeconds;

        public event Action<double, int, bool>? Tick;
        public event Action? TimeExpired;
        public event Action<int>? WarningSecondBeep;

        public TimerService()
        {
            _pollTimer = new System.Timers.Timer(50);
            _pollTimer.AutoReset = true;
            _pollTimer.Elapsed += OnPollElapsed;
        }

        public double RemainingSeconds
        {
            get
            {
                lock (_sync)
                {
                    double elapsed = _accumulatedElapsedBeforePause + (_stopwatch.IsRunning ? _stopwatch.Elapsed.TotalSeconds : 0);
                    return Math.Max(0, _totalDurationSeconds - elapsed);
                }
            }
        }

        public int DisplaySeconds => (int)Math.Ceiling(RemainingSeconds);

        public void Configure(int totalSeconds)
        {
            lock (_sync)
            {
                _pollTimer.Stop();
                _stopwatch.Reset();
                _totalDurationSeconds = Math.Max(1, totalSeconds);
                _accumulatedElapsedBeforePause = 0;
                _lastBeepSecond = -1;
                _expiredFired = false;
                State = TrangThaiDongHo.ChuaChay;
            }
            EmitTick();
        }

        public void RestoreRemaining(int totalSeconds, double remainingSeconds, TrangThaiDongHo state)
        {
            lock (_sync)
            {
                _pollTimer.Stop();
                _stopwatch.Reset();
                _totalDurationSeconds = Math.Max(1, totalSeconds);
                double clampedRem = Math.Clamp(remainingSeconds, 0, _totalDurationSeconds);
                _accumulatedElapsedBeforePause = _totalDurationSeconds - clampedRem;
                _lastBeepSecond = -1;
                _expiredFired = clampedRem <= 0;
                State = state == TrangThaiDongHo.DangChay ? TrangThaiDongHo.TamDung : state;
            }
            EmitTick();
        }

        public void Start()
        {
            lock (_sync)
            {
                _stopwatch.Reset();
                _accumulatedElapsedBeforePause = 0;
                _lastBeepSecond = -1;
                _expiredFired = false;
                State = TrangThaiDongHo.DangChay;
                _stopwatch.Start();
                _pollTimer.Start();
            }
            EmitTick();
        }

        public void Pause()
        {
            lock (_sync)
            {
                if (State != TrangThaiDongHo.DangChay) return;
                _stopwatch.Stop();
                _accumulatedElapsedBeforePause += _stopwatch.Elapsed.TotalSeconds;
                _stopwatch.Reset();
                _pollTimer.Stop();
                State = TrangThaiDongHo.TamDung;
            }
            EmitTick();
        }

        public void Resume()
        {
            lock (_sync)
            {
                if (State != TrangThaiDongHo.TamDung || RemainingSeconds <= 0) return;
                State = TrangThaiDongHo.DangChay;
                _stopwatch.Restart();
                _pollTimer.Start();
            }
            EmitTick();
        }

        public void Reset()
        {
            Configure((int)Math.Round(_totalDurationSeconds));
        }

        private void OnPollElapsed(object? sender, ElapsedEventArgs e)
        {
            double rem;
            int disp;
            bool shouldBeep = false;
            bool shouldExpire = false;

            lock (_sync)
            {
                if (State != TrangThaiDongHo.DangChay) return;

                rem = RemainingSeconds;
                disp = (int)Math.Ceiling(rem);

                if (disp <= 5 && disp >= 1 && disp != _lastBeepSecond)
                {
                    _lastBeepSecond = disp;
                    shouldBeep = true;
                }

                if (rem <= 0 && !_expiredFired)
                {
                    _expiredFired = true;
                    _stopwatch.Stop();
                    _pollTimer.Stop();
                    State = TrangThaiDongHo.HetGio;
                    shouldExpire = true;
                }
            }

            Tick?.Invoke(rem, disp, disp <= 5 && disp > 0);
            if (shouldBeep) WarningSecondBeep?.Invoke(disp);
            if (shouldExpire) TimeExpired?.Invoke();
        }

        private void EmitTick()
        {
            double rem = RemainingSeconds;
            int disp = (int)Math.Ceiling(rem);
            Tick?.Invoke(rem, disp, disp <= 5 && disp > 0);
        }

        public void Dispose()
        {
            _pollTimer.Stop();
            _pollTimer.Dispose();
        }
    }
}
