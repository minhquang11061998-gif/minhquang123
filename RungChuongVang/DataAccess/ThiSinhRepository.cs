using System;
using System.Collections.Generic;
using System.Data;
using RungChuongVang.Models;

namespace RungChuongVang.DataAccess
{
    /// <summary>
    /// Truy cập dữ liệu Thí sinh (ThiSinh).
    /// </summary>
    public class ThiSinhRepository
    {
        public List<ThiSinh> SearchThiSinh(string keyword = "", int? lopHocId = null, bool? onlyActive = true)
        {
            var conditions = new List<string>();
            var args = new List<object?>();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                conditions.Add("(t.MaThiSinh LIKE ? OR t.SoBaoDanh LIKE ? OR t.HoTen LIKE ? OR t.TenTruong LIKE ?)");
                string pattern = $"%{keyword.Trim()}%";
                args.Add(pattern);
                args.Add(pattern);
                args.Add(pattern);
                args.Add(pattern);
            }

            if (lopHocId.HasValue && lopHocId.Value > 0)
            {
                conditions.Add("t.LopHocID = ?");
                args.Add(lopHocId.Value);
            }

            if (onlyActive.HasValue)
            {
                conditions.Add("t.TrangThai = ?");
                args.Add(onlyActive.Value);
            }

            string where = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : string.Empty;
            string sql = $@"
                SELECT t.ThiSinhID, t.MaThiSinh, t.SoBaoDanh, t.HoTen, t.LopHocID,
                       l.TenLop, t.TenTruong, t.GioiTinh, t.DuongDanHinhAnh, t.GhiChu, t.TrangThai
                FROM ThiSinh t
                LEFT JOIN LopHoc l ON t.LopHocID = l.LopHocID
                {where}
                ORDER BY t.SoBaoDanh ASC, t.HoTen ASC";

            DataTable dt = DatabaseHelper.ExecuteDataTable(sql, args.ToArray());
            var list = new List<ThiSinh>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(MapRow(row));
            }
            return list;
        }

        public ThiSinh? GetById(int thiSinhId)
        {
            DataTable dt = DatabaseHelper.ExecuteDataTable(@"
                SELECT t.ThiSinhID, t.MaThiSinh, t.SoBaoDanh, t.HoTen, t.LopHocID,
                       l.TenLop, t.TenTruong, t.GioiTinh, t.DuongDanHinhAnh, t.GhiChu, t.TrangThai
                FROM ThiSinh t
                LEFT JOIN LopHoc l ON t.LopHocID = l.LopHocID
                WHERE t.ThiSinhID = ?", thiSinhId);

            return dt.Rows.Count > 0 ? MapRow(dt.Rows[0]) : null;
        }

        public bool ExistsMaThiSinh(string maThiSinh, int excludeId = 0)
        {
            object? val = DatabaseHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM ThiSinh WHERE MaThiSinh = ? AND ThiSinhID <> ?",
                maThiSinh.Trim(), excludeId);
            return Convert.ToInt32(val ?? 0) > 0;
        }

        public int? FindIdByMaOrSbd(string maOrSbd)
        {
            object? val = DatabaseHelper.ExecuteScalar(
                "SELECT TOP 1 ThiSinhID FROM ThiSinh WHERE MaThiSinh = ? OR SoBaoDanh = ?",
                maOrSbd.Trim(), maOrSbd.Trim());
            return val == null ? null : Convert.ToInt32(val);
        }

        public string GenerateNextMaThiSinh()
        {
            object? val = DatabaseHelper.ExecuteScalar("SELECT MAX(ThiSinhID) FROM ThiSinh");
            int next = (val == null ? 0 : Convert.ToInt32(val)) + 1;
            string code = $"TS{next:0000}";
            while (ExistsMaThiSinh(code))
            {
                next++;
                code = $"TS{next:0000}";
            }
            return code;
        }

        public int AddThiSinh(ThiSinh item)
        {
            if (string.IsNullOrWhiteSpace(item.MaThiSinh))
            {
                item.MaThiSinh = GenerateNextMaThiSinh();
            }

            int newId = 0;
            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                DatabaseHelper.ExecuteNonQuery(
                    conn, tx,
                    @"INSERT INTO ThiSinh (MaThiSinh, SoBaoDanh, HoTen, LopHocID, TenTruong, GioiTinh, DuongDanHinhAnh, GhiChu, TrangThai)
                      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)",
                    item.MaThiSinh.Trim(),
                    item.SoBaoDanh.Trim(),
                    item.HoTen.Trim(),
                    item.LopHocID,
                    item.TenTruong.Trim(),
                    item.GioiTinh.Trim(),
                    item.DuongDanHinhAnh.Trim(),
                    item.GhiChu.Trim(),
                    item.TrangThai);
                newId = DatabaseHelper.GetLastInsertedId(conn, tx);
            });
            return newId;
        }

        public void UpdateThiSinh(ThiSinh item)
        {
            DatabaseHelper.ExecuteNonQuery(
                @"UPDATE ThiSinh
                  SET MaThiSinh = ?, SoBaoDanh = ?, HoTen = ?, LopHocID = ?, TenTruong = ?,
                      GioiTinh = ?, DuongDanHinhAnh = ?, GhiChu = ?, TrangThai = ?
                  WHERE ThiSinhID = ?",
                item.MaThiSinh.Trim(),
                item.SoBaoDanh.Trim(),
                item.HoTen.Trim(),
                item.LopHocID,
                item.TenTruong.Trim(),
                item.GioiTinh.Trim(),
                item.DuongDanHinhAnh.Trim(),
                item.GhiChu.Trim(),
                item.TrangThai,
                item.ThiSinhID);
        }

        public bool DeleteThiSinh(int thiSinhId, out string message)
        {
            int usedPrograms = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM ChuongTrinh_ThiSinh WHERE ThiSinhID = ?", thiSinhId) ?? 0);
            int usedTeams = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM ThanhVienDoi WHERE ThiSinhID = ?", thiSinhId) ?? 0);
            int usedResults = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM KetQua WHERE ThiSinhID = ?", thiSinhId) ?? 0);

            if (usedPrograms > 0 || usedTeams > 0 || usedResults > 0)
            {
                DatabaseHelper.ExecuteNonQuery("UPDATE ThiSinh SET TrangThai = False WHERE ThiSinhID = ?", thiSinhId);
                message = "Thí sinh đã tham gia đội hoặc chương trình thi nên được chuyển sang Ngừng sử dụng.";
                return false;
            }

            DatabaseHelper.ExecuteNonQuery("DELETE FROM ThiSinh WHERE ThiSinhID = ?", thiSinhId);
            message = "Đã xóa thí sinh thành công.";
            return true;
        }

        private static ThiSinh MapRow(DataRow row)
        {
            return new ThiSinh
            {
                ThiSinhID = Convert.ToInt32(row["ThiSinhID"]),
                MaThiSinh = row["MaThiSinh"]?.ToString() ?? string.Empty,
                SoBaoDanh = row["SoBaoDanh"]?.ToString() ?? string.Empty,
                HoTen = row["HoTen"]?.ToString() ?? string.Empty,
                LopHocID = row["LopHocID"] == DBNull.Value ? null : Convert.ToInt32(row["LopHocID"]),
                TenLop = row["TenLop"]?.ToString() ?? string.Empty,
                TenTruong = row["TenTruong"]?.ToString() ?? string.Empty,
                GioiTinh = row["GioiTinh"]?.ToString() ?? string.Empty,
                DuongDanHinhAnh = row["DuongDanHinhAnh"]?.ToString() ?? string.Empty,
                GhiChu = row["GhiChu"]?.ToString() ?? string.Empty,
                TrangThai = row["TrangThai"] != DBNull.Value && Convert.ToBoolean(row["TrangThai"])
            };
        }
    }
}
