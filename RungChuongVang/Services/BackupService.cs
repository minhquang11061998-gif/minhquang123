using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using RungChuongVang.DataAccess;
using RungChuongVang.Helpers;

namespace RungChuongVang.Services
{
    public class BackupManifest
    {
        public DateTime CreatedAt { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public string SchemaVersion { get; set; } = "1";
        public long DatabaseSizeBytes { get; set; }
        public int ImageFileCount { get; set; }
    }

    /// <summary>
    /// Sao lưu và phục hồi toàn bộ CSDL Access, thư mục hình ảnh và cấu hình chương trình.
    /// Tự động sao lưu bản hiện tại trước khi phục hồi và kiểm tra tính hợp lệ của bản sao lưu.
    /// </summary>
    public class BackupService
    {
        public string CreateBackup(string? targetFolder = null, string prefix = "RCV_Backup")
        {
            FileHelper.EnsureApplicationDirectories();
            string destFolder = string.IsNullOrWhiteSpace(targetFolder) ? FileHelper.BackupDirectory : targetFolder;
            if (!Directory.Exists(destFolder))
            {
                Directory.CreateDirectory(destFolder);
            }

            if (!File.Exists(FileHelper.DatabasePath))
            {
                throw new FileNotFoundException("Không tìm thấy tệp cơ sở dữ liệu Access để sao lưu.", FileHelper.DatabasePath);
            }

            // Giải phóng pool OleDb trước khi đọc copy file .accdb
            OleDbConnection.ReleaseObjectPool();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            string zipName = $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
            string zipPath = Path.Combine(destFolder, zipName);
            string tempDir = Path.Combine(Path.GetTempPath(), "RCV_Backup_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                // 1. Copy CSDL Access
                string tempDb = Path.Combine(tempDir, "RungChuongVang.accdb");
                File.Copy(FileHelper.DatabasePath, tempDb, overwrite: true);

                // 2. Copy toàn bộ thư mục Images
                string tempImgRoot = Path.Combine(tempDir, "Images");
                int imgCount = CopyDirectoryRecursive(FileHelper.ImagesRootDirectory, tempImgRoot);

                // 3. Tạo manifest kiểm chứng bản sao lưu
                object? schemaVer = null;
                try { schemaVer = DatabaseHelper.ExecuteScalar("SELECT GiaTri FROM CauHinh WHERE TenCauHinh = ?", "DbSchemaVersion"); } catch { }

                var manifest = new BackupManifest
                {
                    CreatedAt = DateTime.Now,
                    MachineName = Environment.MachineName,
                    SchemaVersion = schemaVer?.ToString() ?? "1",
                    DatabaseSizeBytes = new FileInfo(tempDb).Length,
                    ImageFileCount = imgCount
                };
                File.WriteAllText(Path.Combine(tempDir, "backup_manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

                if (File.Exists(zipPath)) File.Delete(zipPath);
                ZipFile.CreateFromDirectory(tempDir, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
                return zipPath;
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
        }

        public bool ValidateBackupArchive(string zipPath, out string report)
        {
            if (string.IsNullOrWhiteSpace(zipPath) || !File.Exists(zipPath))
            {
                report = "Không tìm thấy tệp sao lưu.";
                return false;
            }

            try
            {
                using var archive = ZipFile.OpenRead(zipPath);
                var dbEntry = archive.GetEntry("RungChuongVang.accdb");
                if (dbEntry == null || dbEntry.Length == 0)
                {
                    report = "Tệp sao lưu không hợp lệ: thiếu cơ sở dữ liệu RungChuongVang.accdb.";
                    return false;
                }

                var manifestEntry = archive.GetEntry("backup_manifest.json");
                if (manifestEntry != null)
                {
                    using var stream = manifestEntry.Open();
                    using var reader = new StreamReader(stream);
                    var manifest = JsonSerializer.Deserialize<BackupManifest>(reader.ReadToEnd());
                    if (manifest != null)
                    {
                        report = $"Bản sao lưu hợp lệ.\n- Thời gian tạo: {manifest.CreatedAt:dd/MM/yyyy HH:mm:ss}\n" +
                                 $"- Phiên bản CSDL: {manifest.SchemaVersion}\n" +
                                 $"- Dung lượng Access: {manifest.DatabaseSizeBytes / 1024.0:N1} KB\n" +
                                 $"- Số ảnh đính kèm: {manifest.ImageFileCount}";
                        return true;
                    }
                }

                report = $"Bản sao lưu hợp lệ (CSDL {dbEntry.Length / 1024.0:N1} KB).";
                return true;
            }
            catch (Exception ex)
            {
                report = $"Tệp sao lưu bị lỗi hoặc hỏng cấu trúc ZIP: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Phục hồi dữ liệu từ file ZIP sao lưu. Bắt buộc xác nhận và tự động sao lưu dữ liệu hiện tại trước khi ghi đè.
        /// </summary>
        public string RestoreFromBackup(string zipPath, bool userConfirmed)
        {
            if (!userConfirmed)
            {
                throw new InvalidOperationException("Không được phép ghi đè dữ liệu khi chưa có xác nhận từ người dùng.");
            }

            if (!ValidateBackupArchive(zipPath, out string validationMsg))
            {
                throw new InvalidOperationException(validationMsg);
            }

            // Tự động sao lưu bản hiện tại trước khi phục hồi
            string safetyBackupPath = File.Exists(FileHelper.DatabasePath)
                ? CreateBackup(FileHelper.BackupDirectory, "AutoPreRestore")
                : string.Empty;

            string tempExtract = Path.Combine(Path.GetTempPath(), "RCV_Restore_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempExtract);

            try
            {
                ZipFile.ExtractToDirectory(zipPath, tempExtract, overwriteFiles: true);
                string extractedDb = Path.Combine(tempExtract, "RungChuongVang.accdb");
                if (!File.Exists(extractedDb))
                {
                    throw new InvalidOperationException("Không tìm thấy RungChuongVang.accdb sau khi giải nén.");
                }

                OleDbConnection.ReleaseObjectPool();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                FileHelper.EnsureApplicationDirectories();
                File.Copy(extractedDb, FileHelper.DatabasePath, overwrite: true);

                string extractedImages = Path.Combine(tempExtract, "Images");
                if (Directory.Exists(extractedImages))
                {
                    CopyDirectoryRecursive(extractedImages, FileHelper.ImagesRootDirectory);
                }

                // Kiểm tra lại schema an toàn sau khi phục hồi
                DatabaseInitializer.EnsureDatabaseInitialized();
                return safetyBackupPath;
            }
            finally
            {
                try { Directory.Delete(tempExtract, recursive: true); } catch { }
            }
        }

        public List<FileInfo> GetAvailableBackups()
        {
            FileHelper.EnsureApplicationDirectories();
            return new DirectoryInfo(FileHelper.BackupDirectory)
                .GetFiles("*.zip")
                .OrderByDescending(f => f.CreationTime)
                .ToList();
        }

        public void TryAutoBackupIfEnabled()
        {
            try
            {
                object? enabled = DatabaseHelper.ExecuteScalar("SELECT GiaTri FROM CauHinh WHERE TenCauHinh = ?", "TuDongSaoLuu");
                if (enabled == null || !string.Equals(enabled.ToString(), "True", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // Tối đa 1 bản auto-backup mỗi ngày để tránh trùng lặp không cần thiết
                string todayPrefix = $"AutoBackup_{DateTime.Now:yyyyMMdd}";
                var existingToday = Directory.GetFiles(FileHelper.BackupDirectory, $"{todayPrefix}*.zip");
                if (existingToday.Length > 0) return;

                CreateBackup(FileHelper.BackupDirectory, "AutoBackup");

                // Giữ tối đa 10 bản AutoBackup gần nhất
                var autoFiles = new DirectoryInfo(FileHelper.BackupDirectory)
                    .GetFiles("AutoBackup_*.zip")
                    .OrderByDescending(f => f.CreationTime)
                    .Skip(10);
                foreach (var old in autoFiles)
                {
                    try { old.Delete(); } catch { }
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteErrorLog("BackupService.TryAutoBackupIfEnabled", ex);
            }
        }

        private static int CopyDirectoryRecursive(string sourceDir, string targetDir)
        {
            if (!Directory.Exists(sourceDir)) return 0;
            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

            int count = 0;
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
                count++;
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                count += CopyDirectoryRecursive(subDir, Path.Combine(targetDir, Path.GetFileName(subDir)));
            }

            return count;
        }
    }
}
