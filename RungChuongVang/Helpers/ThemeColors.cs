using System.Drawing;

namespace RungChuongVang.Helpers
{
    /// <summary>
    /// Bảng màu và font chữ chuẩn cho giao diện quản trị và trình chiếu gameshow Rung Chuông Vàng.
    /// </summary>
    public static class ThemeColors
    {
        // Tông màu chính: Xanh dương đậm - Vàng kim - Trắng
        public static readonly Color NavyDark = Color.FromArgb(11, 29, 58);        // #0B1D3A - Sidebar / Sân khấu tối
        public static readonly Color RoyalBlue = Color.FromArgb(20, 59, 115);      // #143B73 - Header / Bảng
        public static readonly Color PrimaryBlue = Color.FromArgb(30, 91, 184);    // #1E5BB8 - Nút chính
        public static readonly Color PrimaryBlueHover = Color.FromArgb(23, 73, 150);
        public static readonly Color SoftBlueBg = Color.FromArgb(239, 246, 255);   // #EFF6FF

        public static readonly Color Gold = Color.FromArgb(212, 175, 55);          // #D4AF37 - Vàng kim chủ đạo
        public static readonly Color GoldBright = Color.FromArgb(245, 197, 66);    // #F5C542 - Nhấn nổi bật
        public static readonly Color GoldSoft = Color.FromArgb(254, 249, 231);     // #FEF9E7

        // Nền & chữ
        public static readonly Color SurfaceWhite = Color.White;
        public static readonly Color AppBackground = Color.FromArgb(244, 247, 251); // #F4F7FB
        public static readonly Color BorderLight = Color.FromArgb(221, 229, 240);   // #DDE5F0
        public static readonly Color TextPrimary = Color.FromArgb(24, 38, 61);      // #18263D
        public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);     // #64748B

        // Trạng thái nghiệp vụ
        public static readonly Color SuccessGreen = Color.FromArgb(22, 163, 74);    // #16A34A
        public static readonly Color DangerRed = Color.FromArgb(220, 38, 38);       // #DC2626
        public static readonly Color WarningOrange = Color.FromArgb(217, 119, 6);   // #D97706
        public static readonly Color InfoCyan = Color.FromArgb(8, 145, 178);        // #0891B2

        // Màu mặc định 4 mức độ khó
        public static readonly Color Level1Color = Color.FromArgb(22, 163, 74);     // Nhận biết
        public static readonly Color Level2Color = Color.FromArgb(30, 91, 184);     // Thông hiểu
        public static readonly Color Level3Color = Color.FromArgb(217, 119, 6);     // Vận dụng
        public static readonly Color Level4Color = Color.FromArgb(185, 28, 28);     // Vận dụng cao

        // Font chuẩn tiếng Việt Unicode
        public static readonly Font FontTitle = new("Segoe UI", 16F, FontStyle.Bold);
        public static readonly Font FontSubTitle = new("Segoe UI", 12F, FontStyle.Bold);
        public static readonly Font FontBody = new("Segoe UI", 10F, FontStyle.Regular);
        public static readonly Font FontBodyBold = new("Segoe UI", 10F, FontStyle.Bold);
        public static readonly Font FontSmall = new("Segoe UI", 9F, FontStyle.Regular);
    }
}
