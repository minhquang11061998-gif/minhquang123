using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Threading;
using RungChuongVang.Helpers;

namespace RungChuongVang.DataAccess
{
    /// <summary>
    /// Lớp truy cập dữ liệu trung tâm sử dụng OleDb cho Microsoft Access (.accdb).
    /// Bắt buộc sử dụng truy vấn có tham số theo đúng thứ tự dấu ? và quản lý Transaction an toàn.
    /// </summary>
    public static class DatabaseHelper
    {
        private static readonly string[] CandidateProviders =
        {
            "Microsoft.ACE.OLEDB.16.0",
            "Microsoft.ACE.OLEDB.12.0"
        };

        private static string? _activeProvider;
        private static readonly object _providerLock = new();

        public static string DatabasePath => FileHelper.DatabasePath;

        /// <summary>
        /// Xác định Provider Microsoft Access OleDb khả dụng trên máy Windows hiện tại.
        /// </summary>
        public static string DetectAvailableProvider()
        {
            lock (_providerLock)
            {
                if (!string.IsNullOrEmpty(_activeProvider))
                {
                    return _activeProvider;
                }

                var installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    using var reader = OleDbEnumerator.GetRootEnumerator();
                    while (reader.Read())
                    {
                        string? name = reader["SOURCES_NAME"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            installed.Add(name);
                        }
                    }
                }
                catch
                {
                    // Fallback kiểm tra trực tiếp bằng chuỗi kết nối nếu enumerator hạn chế quyền
                }

                foreach (string candidate in CandidateProviders)
                {
                    if (installed.Contains(candidate))
                    {
                        _activeProvider = candidate;
                        return _activeProvider;
                    }
                }

                // Mặc định ưu tiên 16.0 nếu không enumerate được
                _activeProvider = CandidateProviders[0];
                return _activeProvider;
            }
        }

        public static bool IsOleDbProviderInstalled(out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                var installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using var reader = OleDbEnumerator.GetRootEnumerator();
                while (reader.Read())
                {
                    string? name = reader["SOURCES_NAME"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        installed.Add(name);
                    }
                }

                foreach (string candidate in CandidateProviders)
                {
                    if (installed.Contains(candidate))
                    {
                        _activeProvider = candidate;
                        return true;
                    }
                }

                errorMessage =
                    "Không tìm thấy trình điều khiển Microsoft Access Database Engine (ACE OLEDB 12.0 hoặc 16.0 - bản 64-bit).\n" +
                    "Vui lòng cài đặt Microsoft Access Database Engine 2016 Redistributable (x64) để phần mềm kết nối cơ sở dữ liệu .accdb.";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = $"Lỗi kiểm tra trình điều khiển OleDb: {ex.Message}";
                return false;
            }
        }

        public static string GetConnectionString(string? dbPath = null)
        {
            string target = string.IsNullOrWhiteSpace(dbPath) ? DatabasePath : dbPath;
            string provider = DetectAvailableProvider();
            return $"Provider={provider};Data Source={target};Persist Security Info=False;";
        }

        public static OleDbConnection CreateConnection(string? dbPath = null)
        {
            return new OleDbConnection(GetConnectionString(dbPath));
        }

        /// <summary>
        /// Mở kết nối với cơ chế thử lại ngắn khi file .accdb tạm thời bị khóa (.laccdb).
        /// </summary>
        public static OleDbConnection OpenConnectionWithRetry(int maxAttempts = 3, int delayMs = 200)
        {
            if (!File.Exists(DatabasePath))
            {
                throw new FileNotFoundException(
                    $"Không tìm thấy tệp cơ sở dữ liệu Access tại: {DatabasePath}", DatabasePath);
            }

            Exception? lastEx = null;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var conn = CreateConnection();
                try
                {
                    conn.Open();
                    return conn;
                }
                catch (OleDbException ex)
                {
                    conn.Dispose();
                    lastEx = ex;
                    if (attempt < maxAttempts)
                    {
                        Thread.Sleep(delayMs * attempt);
                    }
                }
                catch
                {
                    conn.Dispose();
                    throw;
                }
            }

            throw new InvalidOperationException(
                "Không thể mở cơ sở dữ liệu Access (tệp có thể đang bị khóa bởi chương trình khác).", lastEx);
        }

        /// <summary>
        /// Tạo tham số OleDb an toàn (chuẩn hóa DateTime bỏ mili-giây để tránh lỗi Data type mismatch của Access).
        /// </summary>
        public static void AddParameters(OleDbCommand cmd, params object?[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
            {
                return;
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                object? val = parameters[i];
                var param = new OleDbParameter($"@p{i}", OleDbType.Variant);

                if (val == null)
                {
                    param.Value = DBNull.Value;
                }
                else if (val is DateTime dt)
                {
                    param = new OleDbParameter($"@p{i}", OleDbType.Date)
                    {
                        Value = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second)
                    };
                }
                else if (val is bool b)
                {
                    param = new OleDbParameter($"@p{i}", OleDbType.Boolean) { Value = b };
                }
                else if (val is int n)
                {
                    param = new OleDbParameter($"@p{i}", OleDbType.Integer) { Value = n };
                }
                else if (val is double d)
                {
                    param = new OleDbParameter($"@p{i}", OleDbType.Double) { Value = d };
                }
                else
                {
                    param = new OleDbParameter($"@p{i}", OleDbType.VarWChar) { Value = val.ToString() ?? string.Empty };
                }

                cmd.Parameters.Add(param);
            }
        }

        public static DataTable ExecuteDataTable(string sql, params object?[] parameters)
        {
            using var conn = OpenConnectionWithRetry();
            using var cmd = new OleDbCommand(sql, conn);
            AddParameters(cmd, parameters);
            using var adapter = new OleDbDataAdapter(cmd);
            var table = new DataTable();
            adapter.Fill(table);
            return table;
        }

        public static int ExecuteNonQuery(string sql, params object?[] parameters)
        {
            using var conn = OpenConnectionWithRetry();
            using var cmd = new OleDbCommand(sql, conn);
            AddParameters(cmd, parameters);
            return cmd.ExecuteNonQuery();
        }

        public static int ExecuteNonQuery(OleDbConnection conn, OleDbTransaction tx, string sql, params object?[] parameters)
        {
            using var cmd = new OleDbCommand(sql, conn, tx);
            AddParameters(cmd, parameters);
            return cmd.ExecuteNonQuery();
        }

        public static object? ExecuteScalar(string sql, params object?[] parameters)
        {
            using var conn = OpenConnectionWithRetry();
            using var cmd = new OleDbCommand(sql, conn);
            AddParameters(cmd, parameters);
            object? result = cmd.ExecuteScalar();
            return result == DBNull.Value ? null : result;
        }

        public static object? ExecuteScalar(OleDbConnection conn, OleDbTransaction tx, string sql, params object?[] parameters)
        {
            using var cmd = new OleDbCommand(sql, conn, tx);
            AddParameters(cmd, parameters);
            object? result = cmd.ExecuteScalar();
            return result == DBNull.Value ? null : result;
        }

        /// <summary>
        /// Lấy AutoNumber ID vừa chèn trong cùng kết nối / transaction Access (@@IDENTITY).
        /// </summary>
        public static int GetLastInsertedId(OleDbConnection conn, OleDbTransaction? tx = null)
        {
            using var cmd = new OleDbCommand("SELECT @@IDENTITY", conn, tx);
            object? val = cmd.ExecuteScalar();
            return val != null && val != DBNull.Value ? Convert.ToInt32(val) : 0;
        }

        /// <summary>
        /// Thực thi một chuỗi thao tác dữ liệu trong Transaction để bảo đảm toàn vẹn dữ liệu.
        /// </summary>
        public static void ExecuteInTransaction(Action<OleDbConnection, OleDbTransaction> work)
        {
            using var conn = OpenConnectionWithRetry();
            using var tx = conn.BeginTransaction();
            try
            {
                work(conn, tx);
                tx.Commit();
            }
            catch
            {
                try
                {
                    tx.Rollback();
                }
                catch
                {
                    // Bỏ qua lỗi phụ khi rollback nếu kết nối đã đóng
                }
                throw;
            }
        }
    }
}
