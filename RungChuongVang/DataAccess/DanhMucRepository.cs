using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using RungChuongVang.Enums;
using RungChuongVang.Models;

namespace RungChuongVang.DataAccess
{
    /// <summary>
    /// Truy cập dữ liệu danh mục: Lớp học, Môn học, Chủ đề và Cấu hình mức độ khó.
    /// </summary>
    public class DanhMucRepository
    {
        #region Lớp học

        public List<LopHoc> GetLopHoc(string keyword = "", bool? onlyActive = null)
        {
            var list = new List<LopHoc>();
            var conditions = new List<string>();
            var args = new List<object?>();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                conditions.Add("(MaLop LIKE ? OR TenLop LIKE ? OR TenTruong LIKE ?)");
                string pattern = $"%{keyword.Trim()}%";
                args.Add(pattern);
                args.Add(pattern);
                args.Add(pattern);
            }

            if (onlyActive.HasValue)
            {
                conditions.Add("TrangThai = ?");
                args.Add(onlyActive.Value);
            }

            string where = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : string.Empty;
            string sql = $"SELECT LopHocID, MaLop, TenLop, KhoiLop, TenTruong, GhiChu, TrangThai FROM LopHoc{where} ORDER BY KhoiLop ASC, TenLop ASC";

            DataTable dt = DatabaseHelper.ExecuteDataTable(sql, args.ToArray());
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new LopHoc
                {
                    LopHocID = Convert.ToInt32(row["LopHocID"]),
                    MaLop = row["MaLop"]?.ToString() ?? string.Empty,
                    TenLop = row["TenLop"]?.ToString() ?? string.Empty,
                    KhoiLop = row["KhoiLop"] == DBNull.Value ? 0 : Convert.ToInt32(row["KhoiLop"]),
                    TenTruong = row["TenTruong"]?.ToString() ?? string.Empty,
                    GhiChu = row["GhiChu"]?.ToString() ?? string.Empty,
                    TrangThai = row["TrangThai"] != DBNull.Value && Convert.ToBoolean(row["TrangThai"])
                });
            }

            return list;
        }

        public bool ExistsMaLop(string maLop, int excludeId = 0)
        {
            object? val = DatabaseHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM LopHoc WHERE MaLop = ? AND LopHocID <> ?",
                maLop.Trim(), excludeId);
            return Convert.ToInt32(val ?? 0) > 0;
        }

        public int AddLopHoc(LopHoc item)
        {
            int newId = 0;
            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                DatabaseHelper.ExecuteNonQuery(
                    conn, tx,
                    "INSERT INTO LopHoc (MaLop, TenLop, KhoiLop, TenTruong, GhiChu, TrangThai) VALUES (?, ?, ?, ?, ?, ?)",
                    item.MaLop.Trim(),
                    item.TenLop.Trim(),
                    item.KhoiLop,
                    item.TenTruong.Trim(),
                    item.GhiChu.Trim(),
                    item.TrangThai);
                newId = DatabaseHelper.GetLastInsertedId(conn, tx);
            });
            return newId;
        }

        public void UpdateLopHoc(LopHoc item)
        {
            DatabaseHelper.ExecuteNonQuery(
                "UPDATE LopHoc SET MaLop = ?, TenLop = ?, KhoiLop = ?, TenTruong = ?, GhiChu = ?, TrangThai = ? WHERE LopHocID = ?",
                item.MaLop.Trim(),
                item.TenLop.Trim(),
                item.KhoiLop,
                item.TenTruong.Trim(),
                item.GhiChu.Trim(),
                item.TrangThai,
                item.LopHocID);
        }

        public void SetLopHocActive(int lopHocId, bool active)
        {
            DatabaseHelper.ExecuteNonQuery("UPDATE LopHoc SET TrangThai = ? WHERE LopHocID = ?", active, lopHocId);
        }

        /// <summary>
        /// Xóa an toàn: nếu lớp đã gắn câu hỏi hoặc thí sinh thì chuyển sang ngừng sử dụng.
        /// </summary>
        public bool DeleteLopHoc(int lopHocId, out string message)
        {
            int usedInQuestions = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM CauHoi_LopHoc WHERE LopHocID = ?", lopHocId) ?? 0);
            int usedInStudents = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM ThiSinh WHERE LopHocID = ?", lopHocId) ?? 0);

            if (usedInQuestions > 0 || usedInStudents > 0)
            {
                SetLopHocActive(lopHocId, false);
                message = "Lớp học đang được sử dụng trong câu hỏi hoặc thí sinh nên đã được chuyển sang trạng thái Ngừng sử dụng.";
                return false;
            }

            DatabaseHelper.ExecuteNonQuery("DELETE FROM LopHoc WHERE LopHocID = ?", lopHocId);
            message = "Đã xóa lớp học thành công.";
            return true;
        }

        #endregion

        #region Môn học

        public List<MonHoc> GetMonHoc(string keyword = "", bool? onlyActive = null)
        {
            var list = new List<MonHoc>();
            var conditions = new List<string>();
            var args = new List<object?>();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                conditions.Add("TenMonHoc LIKE ?");
                args.Add($"%{keyword.Trim()}%");
            }

            if (onlyActive.HasValue)
            {
                conditions.Add("TrangThai = ?");
                args.Add(onlyActive.Value);
            }

            string where = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : string.Empty;
            DataTable dt = DatabaseHelper.ExecuteDataTable(
                $"SELECT MonHocID, TenMonHoc, MoTa, TrangThai FROM MonHoc{where} ORDER BY TenMonHoc ASC",
                args.ToArray());

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new MonHoc
                {
                    MonHocID = Convert.ToInt32(row["MonHocID"]),
                    TenMonHoc = row["TenMonHoc"]?.ToString() ?? string.Empty,
                    MoTa = row["MoTa"]?.ToString() ?? string.Empty,
                    TrangThai = row["TrangThai"] != DBNull.Value && Convert.ToBoolean(row["TrangThai"])
                });
            }

            return list;
        }

        public bool ExistsTenMonHoc(string tenMonHoc, int excludeId = 0)
        {
            object? val = DatabaseHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM MonHoc WHERE TenMonHoc = ? AND MonHocID <> ?",
                tenMonHoc.Trim(), excludeId);
            return Convert.ToInt32(val ?? 0) > 0;
        }

        public int AddMonHoc(MonHoc item)
        {
            int newId = 0;
            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                DatabaseHelper.ExecuteNonQuery(
                    conn, tx,
                    "INSERT INTO MonHoc (TenMonHoc, MoTa, TrangThai) VALUES (?, ?, ?)",
                    item.TenMonHoc.Trim(), item.MoTa.Trim(), item.TrangThai);
                newId = DatabaseHelper.GetLastInsertedId(conn, tx);
            });
            return newId;
        }

        public void UpdateMonHoc(MonHoc item)
        {
            DatabaseHelper.ExecuteNonQuery(
                "UPDATE MonHoc SET TenMonHoc = ?, MoTa = ?, TrangThai = ? WHERE MonHocID = ?",
                item.TenMonHoc.Trim(), item.MoTa.Trim(), item.TrangThai, item.MonHocID);
        }

        public bool DeleteMonHoc(int monHocId, out string message)
        {
            int usedQuestions = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM CauHoi WHERE MonHocID = ?", monHocId) ?? 0);
            int usedTopics = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM ChuDe WHERE MonHocID = ?", monHocId) ?? 0);

            if (usedQuestions > 0 || usedTopics > 0)
            {
                DatabaseHelper.ExecuteNonQuery("UPDATE MonHoc SET TrangThai = False WHERE MonHocID = ?", monHocId);
                message = "Môn học đã có chủ đề hoặc câu hỏi liên kết nên được chuyển sang Ngừng sử dụng.";
                return false;
            }

            DatabaseHelper.ExecuteNonQuery("DELETE FROM MonHoc WHERE MonHocID = ?", monHocId);
            message = "Đã xóa môn học thành công.";
            return true;
        }

        #endregion

        #region Chủ đề

        public List<ChuDe> GetChuDe(int? monHocId = null, string keyword = "", bool? onlyActive = null)
        {
            var list = new List<ChuDe>();
            var conditions = new List<string>();
            var args = new List<object?>();

            if (monHocId.HasValue && monHocId.Value > 0)
            {
                conditions.Add("MonHocID = ?");
                args.Add(monHocId.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                conditions.Add("TenChuDe LIKE ?");
                args.Add($"%{keyword.Trim()}%");
            }

            if (onlyActive.HasValue)
            {
                conditions.Add("TrangThai = ?");
                args.Add(onlyActive.Value);
            }

            string where = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : string.Empty;
            DataTable dt = DatabaseHelper.ExecuteDataTable(
                $"SELECT ChuDeID, MonHocID, TenChuDe, MoTa, TrangThai FROM ChuDe{where} ORDER BY TenChuDe ASC",
                args.ToArray());

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new ChuDe
                {
                    ChuDeID = Convert.ToInt32(row["ChuDeID"]),
                    MonHocID = row["MonHocID"] == DBNull.Value ? null : Convert.ToInt32(row["MonHocID"]),
                    TenChuDe = row["TenChuDe"]?.ToString() ?? string.Empty,
                    MoTa = row["MoTa"]?.ToString() ?? string.Empty,
                    TrangThai = row["TrangThai"] != DBNull.Value && Convert.ToBoolean(row["TrangThai"])
                });
            }

            return list;
        }

        public int AddChuDe(ChuDe item)
        {
            int newId = 0;
            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                DatabaseHelper.ExecuteNonQuery(
                    conn, tx,
                    "INSERT INTO ChuDe (MonHocID, TenChuDe, MoTa, TrangThai) VALUES (?, ?, ?, ?)",
                    item.MonHocID, item.TenChuDe.Trim(), item.MoTa.Trim(), item.TrangThai);
                newId = DatabaseHelper.GetLastInsertedId(conn, tx);
            });
            return newId;
        }

        public void UpdateChuDe(ChuDe item)
        {
            DatabaseHelper.ExecuteNonQuery(
                "UPDATE ChuDe SET MonHocID = ?, TenChuDe = ?, MoTa = ?, TrangThai = ? WHERE ChuDeID = ?",
                item.MonHocID, item.TenChuDe.Trim(), item.MoTa.Trim(), item.TrangThai, item.ChuDeID);
        }

        public bool DeleteChuDe(int chuDeId, out string message)
        {
            int usedQuestions = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM CauHoi WHERE ChuDeID = ?", chuDeId) ?? 0);
            if (usedQuestions > 0)
            {
                DatabaseHelper.ExecuteNonQuery("UPDATE ChuDe SET TrangThai = False WHERE ChuDeID = ?", chuDeId);
                message = "Chủ đề đang có câu hỏi sử dụng nên đã được chuyển sang Ngừng sử dụng.";
                return false;
            }

            DatabaseHelper.ExecuteNonQuery("DELETE FROM ChuDe WHERE ChuDeID = ?", chuDeId);
            message = "Đã xóa chủ đề thành công.";
            return true;
        }

        #endregion

        #region Cấu hình mức độ khó

        public Dictionary<MucDoKho, (string Ten, Color Mau)> GetMucDoConfig()
        {
            var result = new Dictionary<MucDoKho, (string, Color)>
            {
                [MucDoKho.NhanBiet] = ("Nhận biết", Helpers.ThemeColors.Level1Color),
                [MucDoKho.ThongHieu] = ("Thông hiểu", Helpers.ThemeColors.Level2Color),
                [MucDoKho.VanDung] = ("Vận dụng", Helpers.ThemeColors.Level3Color),
                [MucDoKho.VanDungCao] = ("Vận dụng cao", Helpers.ThemeColors.Level4Color)
            };

            DataTable dt = DatabaseHelper.ExecuteDataTable("SELECT TenCauHinh, GiaTri FROM CauHinh WHERE TenCauHinh LIKE 'MucDo_%'");
            foreach (DataRow row in dt.Rows)
            {
                string key = row["TenCauHinh"]?.ToString() ?? string.Empty;
                string val = row["GiaTri"]?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(val)) continue;

                for (int lv = 1; lv <= 4; lv++)
                {
                    var enumVal = (MucDoKho)lv;
                    if (key == $"MucDo_{lv}_Ten")
                    {
                        result[enumVal] = (val, result[enumVal].Mau);
                    }
                    else if (key == $"MucDo_{lv}_Mau")
                    {
                        try
                        {
                            result[enumVal] = (result[enumVal].Ten, ColorTranslator.FromHtml(val));
                        }
                        catch { }
                    }
                }
            }

            return result;
        }

        #endregion
    }
}
