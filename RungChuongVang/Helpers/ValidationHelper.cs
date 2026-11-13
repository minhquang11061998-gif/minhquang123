using System;
using System.Linq;

namespace RungChuongVang.Helpers
{
    /// <summary>
    /// Kiểm tra dữ liệu đầu vào dùng chung trên các Form và Service.
    /// </summary>
    public static class ValidationHelper
    {
        public static bool IsNullOrWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value);

        public static string NormalizeText(string? value) => value?.Trim() ?? string.Empty;

        public static bool IsValidPositiveInt(int value) => value > 0;

        public static bool IsValidFileExtension(string filePath, params string[] allowedExtensions)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            string ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            return allowedExtensions.Any(a => a.Equals(ext, StringComparison.OrdinalIgnoreCase));
        }
    }
}
