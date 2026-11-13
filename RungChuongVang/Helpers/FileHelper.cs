using System;
using System.IO;

namespace RungChuongVang.Helpers
{
    /// <summary>
    /// Quản lý thư mục dữ liệu, đường dẫn tương đối và thao tác tập tin an toàn.
    /// </summary>
    public static class FileHelper
    {
        public static string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;

        public static string DataDirectory => Path.Combine(BaseDirectory, "Data");
        public static string DatabasePath => Path.Combine(DataDirectory, "RungChuongVang.accdb");

        public static string ImagesRootDirectory => Path.Combine(DataDirectory, "Images");
        public static string QuestionImagesDirectory => Path.Combine(ImagesRootDirectory, "Questions");
        public static string WinnerImagesDirectory => Path.Combine(ImagesRootDirectory, "Winners");
        public static string TeamImagesDirectory => Path.Combine(ImagesRootDirectory, "Teams");
        public static string ContestantImagesDirectory => Path.Combine(ImagesRootDirectory, "Contestants");
        public static string LogoImagesDirectory => Path.Combine(ImagesRootDirectory, "Logo");

        public static string BackupDirectory => Path.Combine(DataDirectory, "Backup");
        public static string ExportsDirectory => Path.Combine(DataDirectory, "Exports");
        public static string LogsDirectory => Path.Combine(DataDirectory, "Logs");

        /// <summary>
        /// Tạo toàn bộ cây thư mục dữ liệu bắt buộc nếu chưa tồn tại.
        /// </summary>
        public static void EnsureApplicationDirectories()
        {
            string[] directories =
            {
                DataDirectory,
                ImagesRootDirectory,
                QuestionImagesDirectory,
                WinnerImagesDirectory,
                TeamImagesDirectory,
                ContestantImagesDirectory,
                LogoImagesDirectory,
                BackupDirectory,
                ExportsDirectory,
                LogsDirectory
            };

            foreach (string dir in directories)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
        }

        /// <summary>
        /// Chuyển đường dẫn tương đối lưu trong Access thành đường dẫn tuyệt đối trên máy.
        /// </summary>
        public static string? ResolveDataPath(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            if (Path.IsPathRooted(relativePath))
            {
                return relativePath;
            }

            string normalized = relativePath.TrimStart('\\', '/');
            return Path.Combine(DataDirectory, normalized);
        }

        /// <summary>
        /// Chuyển đường dẫn tuyệt đối trong thư mục Data thành đường dẫn tương đối để lưu Access.
        /// </summary>
        public static string ToRelativeDataPath(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return string.Empty;
            }

            return Path.GetRelativePath(DataDirectory, fullPath);
        }

        /// <summary>
        /// Ghi log lỗi kỹ thuật vào file nhật ký ngày để hỗ trợ chẩn đoán sự cố thực tế.
        /// </summary>
        public static void WriteErrorLog(string context, Exception ex)
        {
            try
            {
                EnsureApplicationDirectories();
                string logFile = Path.Combine(LogsDirectory, $"error_{DateTime.Now:yyyyMMdd}.log");
                string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{context}] {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}----------------------------------------{Environment.NewLine}";
                File.AppendAllText(logFile, entry);
            }
            catch
            {
                // Không để việc ghi log gây lỗi dây chuyền
            }
        }
    }
}
