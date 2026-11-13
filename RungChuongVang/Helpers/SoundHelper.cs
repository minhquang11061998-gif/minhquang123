using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using RungChuongVang.DataAccess;

namespace RungChuongVang.Helpers
{
    /// <summary>
    /// Phát âm thanh hiệu ứng gameshow bất đồng bộ (ưu tiên file WAV trong Resources/Sounds, fallback tiếng bíp hệ thống).
    /// </summary>
    public static class SoundHelper
    {
        public static bool IsSoundEnabled()
        {
            try
            {
                object? val = DatabaseHelper.ExecuteScalar("SELECT GiaTri FROM CauHinh WHERE TenCauHinh = ?", "AmThanhBat");
                if (val == null) return true;
                return !string.Equals(val.ToString(), "False", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return true;
            }
        }

        public static void PlayWarningTick() => PlayCue("tick_5s.wav", 880, 120);
        public static void PlayTimeUp() => PlayCue("timeout.wav", 440, 450);
        public static void PlayCorrect() => PlayCue("correct.wav", 1046, 220);
        public static void PlayWrong() => PlayCue("wrong.wav", 260, 300);
        public static void PlayGoldenBell() => PlayCue("bell.wav", 1318, 500);
        public static void PlayFanfare() => PlayCue("fanfare.wav", 1174, 450);

        private static void PlayCue(string wavFileName, int fallbackFreq, int fallbackDurationMs)
        {
            if (!IsSoundEnabled()) return;

            Task.Run(() =>
            {
                try
                {
                    string wavPath = Path.Combine(FileHelper.BaseDirectory, "Resources", "Sounds", wavFileName);
                    if (File.Exists(wavPath))
                    {
                        using var player = new SoundPlayer(wavPath);
                        player.Play();
                    }
                    else
                    {
                        Console.Beep(fallbackFreq, fallbackDurationMs);
                    }
                }
                catch
                {
                    // Bỏ qua nếu máy không có thiết bị âm thanh
                }
            });
        }
    }
}
