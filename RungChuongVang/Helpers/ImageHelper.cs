using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace RungChuongVang.Helpers
{
    /// <summary>
    /// Xử lý hình ảnh an toàn: nạp không khóa file trên đĩa, giữ tỷ lệ, hỗ trợ JPG/JPEG/PNG/WEBP.
    /// </summary>
    public static class ImageHelper
    {
        public static readonly string[] SupportedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        public static bool IsSupportedImage(string filePath)
        {
            return ValidationHelper.IsValidFileExtension(filePath, SupportedExtensions);
        }

        /// <summary>
        /// Sao chép ảnh vào thư mục con trong Data/Images/ và trả về đường dẫn tương đối lưu Access.
        /// </summary>
        public static string CopyImageToDataFolder(string sourceFilePath, string targetFolder, string prefix)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException("Không tìm thấy tập tin hình ảnh nguồn.", sourceFilePath);
            }

            if (!IsSupportedImage(sourceFilePath))
            {
                throw new InvalidOperationException("Định dạng ảnh không được hỗ trợ. Chỉ chấp nhận JPG, JPEG, PNG, WEBP.");
            }

            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            string ext = Path.GetExtension(sourceFilePath).ToLowerInvariant();
            string fileName = $"{prefix}_{DateTime.Now:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}"[..36] + ext;
            string destPath = Path.Combine(targetFolder, fileName);

            File.Copy(sourceFilePath, destPath, overwrite: true);
            return FileHelper.ToRelativeDataPath(destPath);
        }

        /// <summary>
        /// Nạp ảnh từ đường dẫn tương đối/tuyệt đối bằng MemoryStream để không khóa file nguồn.
        /// Trả về null nếu ảnh bị mất hoặc lỗi đọc.
        /// </summary>
        public static Image? LoadImageSafe(string? pathOrRelative)
        {
            try
            {
                string? resolved = FileHelper.ResolveDataPath(pathOrRelative);
                if (string.IsNullOrWhiteSpace(resolved) || !File.Exists(resolved))
                {
                    return null;
                }

                byte[] bytes = File.ReadAllBytes(resolved);
                using var ms = new MemoryStream(bytes);
                using var original = Image.FromStream(ms);
                return new Bitmap(original);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Thay đổi kích thước ảnh giữ nguyên tỷ lệ gốc (không làm méo ảnh).
        /// </summary>
        public static Bitmap ResizeKeepAspect(Image source, int maxWidth, int maxHeight)
        {
            if (maxWidth <= 0 || maxHeight <= 0)
            {
                return new Bitmap(source);
            }

            double ratioX = (double)maxWidth / source.Width;
            double ratioY = (double)maxHeight / source.Height;
            double ratio = Math.Min(ratioX, ratioY);

            int newWidth = Math.Max(1, (int)Math.Round(source.Width * ratio));
            int newHeight = Math.Max(1, (int)Math.Round(source.Height * ratio));

            var dest = new Bitmap(newWidth, newHeight);
            using var g = Graphics.FromImage(dest);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(source, 0, 0, newWidth, newHeight);
            return dest;
        }
    }
}
