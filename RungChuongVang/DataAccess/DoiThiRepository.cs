using System;
using System.Collections.Generic;
using System.Data;
using RungChuongVang.Models;

namespace RungChuongVang.DataAccess
{
    /// <summary>
    /// Truy cập dữ liệu Đội thi và Thành viên đội.
    /// Đảm bảo quy tắc: 1 thí sinh có thể tham gia nhiều chương trình với đội khác nhau,
    /// nhưng không được thuộc 2 đội trong cùng 1 chương trình thi.
    /// </summary>
    public class DoiThiRepository
    {
        public List<DoiThi> SearchDoiThi(string keyword = "", int? chuongTrinhId = null)
        {
            var conditions = new List<string>();
            var args = new List<object?>();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                conditions.Add("(d.MaDoi LIKE ? OR d.TenDoi LIKE ? OR d.TenLop LIKE ? OR d.TenTruong LIKE ?)");
                string pattern = $"%{keyword.Trim()}%";
                args.Add(pattern);
                args.Add(pattern);
                args.Add(pattern);
                args.Add(pattern);
            }

            if (chuongTrinhId.HasValue && chuongTrinhId.Value > 0)
            {
                conditions.Add("d.ChuongTrinhID = ?");
                args.Add(chuongTrinhId.Value);
            }

            string where = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : string.Empty;
            string sql = $@"
                SELECT d.DoiThiID, d.MaDoi, d.TenDoi, d.ChuongTrinhID, d.TenLop,
                       d.TenTruong, d.TenDoiTruong, d.DuongDanHinhAnh, d.GhiChu
                FROM DoiThi d
                {where}
                ORDER BY d.TenDoi ASC";

            DataTable dt = DatabaseHelper.ExecuteDataTable(sql, args.ToArray());
            var list = new List<DoiThi>();
            foreach (DataRow row in dt.Rows)
            {
                var team = new DoiThi
                {
                    DoiThiID = Convert.ToInt32(row["DoiThiID"]),
                    MaDoi = row["MaDoi"]?.ToString() ?? string.Empty,
                    TenDoi = row["TenDoi"]?.ToString() ?? string.Empty,
                    ChuongTrinhID = row["ChuongTrinhID"] == DBNull.Value ? null : Convert.ToInt32(row["ChuongTrinhID"]),
                    TenLop = row["TenLop"]?.ToString() ?? string.Empty,
                    TenTruong = row["TenTruong"]?.ToString() ?? string.Empty,
                    TenDoiTruong = row["TenDoiTruong"]?.ToString() ?? string.Empty,
                    DuongDanHinhAnh = row["DuongDanHinhAnh"]?.ToString() ?? string.Empty,
                    GhiChu = row["GhiChu"]?.ToString() ?? string.Empty
                };
                team.ThanhVien = GetThanhVienByDoi(team.DoiThiID);
                list.Add(team);
            }

            return list;
        }

        public List<ThanhVienDoi> GetThanhVienByDoi(int doiThiId)
        {
            string sql = @"
                SELECT tv.ThanhVienDoiID, tv.DoiThiID, tv.ThiSinhID, tv.ChuongTrinhID, tv.VaiTro, tv.TrangThai,
                       ts.HoTen, ts.SoBaoDanh, l.TenLop
                FROM (ThanhVienDoi tv
                INNER JOIN ThiSinh ts ON tv.ThiSinhID = ts.ThiSinhID)
                LEFT JOIN LopHoc l ON ts.LopHocID = l.LopHocID
                WHERE tv.DoiThiID = ? AND tv.TrangThai = True
                ORDER BY IIF(tv.VaiTro = 'DoiTruong', 0, 1), ts.HoTen ASC";

            DataTable dt = DatabaseHelper.ExecuteDataTable(sql, doiThiId);
            var list = new List<ThanhVienDoi>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new ThanhVienDoi
                {
                    ThanhVienDoiID = Convert.ToInt32(row["ThanhVienDoiID"]),
                    DoiThiID = Convert.ToInt32(row["DoiThiID"]),
                    ThiSinhID = Convert.ToInt32(row["ThiSinhID"]),
                    ChuongTrinhID = row["ChuongTrinhID"] == DBNull.Value ? null : Convert.ToInt32(row["ChuongTrinhID"]),
                    VaiTro = row["VaiTro"]?.ToString() ?? "ThanhVien",
                    TrangThai = row["TrangThai"] != DBNull.Value && Convert.ToBoolean(row["TrangThai"]),
                    HoTenThiSinh = row["HoTen"]?.ToString() ?? string.Empty,
                    SoBaoDanh = row["SoBaoDanh"]?.ToString() ?? string.Empty,
                    TenLop = row["TenLop"]?.ToString() ?? string.Empty
                });
            }
            return list;
        }

        public bool ExistsMaDoi(string maDoi, int excludeId = 0)
        {
            object? val = DatabaseHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM DoiThi WHERE MaDoi = ? AND DoiThiID <> ?",
                maDoi.Trim(), excludeId);
            return Convert.ToInt32(val ?? 0) > 0;
        }

        public string GenerateNextMaDoi()
        {
            object? val = DatabaseHelper.ExecuteScalar("SELECT MAX(DoiThiID) FROM DoiThi");
            int next = (val == null ? 0 : Convert.ToInt32(val)) + 1;
            string code = $"DOI{next:000}";
            while (ExistsMaDoi(code))
            {
                next++;
                code = $"DOI{next:000}";
            }
            return code;
        }

        public int AddDoiThi(DoiThi team)
        {
            if (string.IsNullOrWhiteSpace(team.MaDoi))
            {
                team.MaDoi = GenerateNextMaDoi();
            }

            int newId = 0;
            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                DatabaseHelper.ExecuteNonQuery(
                    conn, tx,
                    @"INSERT INTO DoiThi (MaDoi, TenDoi, ChuongTrinhID, TenLop, TenTruong, TenDoiTruong, DuongDanHinhAnh, GhiChu)
                      VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                    team.MaDoi.Trim(),
                    team.TenDoi.Trim(),
                    team.ChuongTrinhID,
                    team.TenLop.Trim(),
                    team.TenTruong.Trim(),
                    team.TenDoiTruong.Trim(),
                    team.DuongDanHinhAnh.Trim(),
                    team.GhiChu.Trim());
                newId = DatabaseHelper.GetLastInsertedId(conn, tx);
            });
            return newId;
        }

        public void UpdateDoiThi(DoiThi team)
        {
            DatabaseHelper.ExecuteNonQuery(
                @"UPDATE DoiThi
                  SET MaDoi = ?, TenDoi = ?, ChuongTrinhID = ?, TenLop = ?, TenTruong = ?,
                      TenDoiTruong = ?, DuongDanHinhAnh = ?, GhiChu = ?
                  WHERE DoiThiID = ?",
                team.MaDoi.Trim(),
                team.TenDoi.Trim(),
                team.ChuongTrinhID,
                team.TenLop.Trim(),
                team.TenTruong.Trim(),
                team.TenDoiTruong.Trim(),
                team.DuongDanHinhAnh.Trim(),
                team.GhiChu.Trim(),
                team.DoiThiID);
        }

        public void AddMemberToTeam(int doiThiId, int thiSinhId, string vaiTro = "ThanhVien")
        {
            // Kiểm tra ChuongTrinhID của đội đích
            object? ctObj = DatabaseHelper.ExecuteScalar("SELECT ChuongTrinhID FROM DoiThi WHERE DoiThiID = ?", doiThiId);
            int? chuongTrinhId = ctObj == null ? null : Convert.ToInt32(ctObj);

            // Kiểm tra đã tồn tại trong chính đội này chưa
            int sameTeam = Convert.ToInt32(DatabaseHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM ThanhVienDoi WHERE DoiThiID = ? AND ThiSinhID = ? AND TrangThai = True",
                doiThiId, thiSinhId) ?? 0);
            if (sameTeam > 0)
            {
                throw new InvalidOperationException("Thí sinh này đã là thành viên của đội.");
            }

            // Kiểm tra quy tắc không thuộc 2 đội trong cùng 1 chương trình thi
            if (chuongTrinhId.HasValue)
            {
                int otherTeamInSameProgram = Convert.ToInt32(DatabaseHelper.ExecuteScalar(
                    @"SELECT COUNT(*) FROM ThanhVienDoi
                      WHERE ChuongTrinhID = ? AND ThiSinhID = ? AND DoiThiID <> ? AND TrangThai = True",
                    chuongTrinhId.Value, thiSinhId, doiThiId) ?? 0);
                if (otherTeamInSameProgram > 0)
                {
                    throw new InvalidOperationException(
                        "Thí sinh này đã thuộc một đội khác trong cùng chương trình thi. Không thể tham gia hai đội trong cùng chương trình.");
                }
            }

            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                if (vaiTro == "DoiTruong")
                {
                    DatabaseHelper.ExecuteNonQuery(
                        conn, tx,
                        "UPDATE ThanhVienDoi SET VaiTro = 'ThanhVien' WHERE DoiThiID = ?",
                        doiThiId);
                }

                // Nếu từng bị gỡ mềm trước đó trong cùng đội thì kích hoạt lại
                int softRemoved = Convert.ToInt32(DatabaseHelper.ExecuteScalar(
                    conn, tx,
                    "SELECT COUNT(*) FROM ThanhVienDoi WHERE DoiThiID = ? AND ThiSinhID = ?",
                    doiThiId, thiSinhId) ?? 0);

                if (softRemoved > 0)
                {
                    DatabaseHelper.ExecuteNonQuery(
                        conn, tx,
                        "UPDATE ThanhVienDoi SET TrangThai = True, VaiTro = ?, ChuongTrinhID = ? WHERE DoiThiID = ? AND ThiSinhID = ?",
                        vaiTro, chuongTrinhId, doiThiId, thiSinhId);
                }
                else
                {
                    DatabaseHelper.ExecuteNonQuery(
                        conn, tx,
                        "INSERT INTO ThanhVienDoi (DoiThiID, ThiSinhID, ChuongTrinhID, VaiTro, TrangThai) VALUES (?, ?, ?, ?, True)",
                        doiThiId, thiSinhId, chuongTrinhId, vaiTro);
                }

                if (vaiTro == "DoiTruong")
                {
                    SyncCaptainName(conn, tx, doiThiId, thiSinhId);
                }
            });
        }

        public void RemoveMemberFromTeam(int doiThiId, int thiSinhId)
        {
            DatabaseHelper.ExecuteNonQuery(
                "DELETE FROM ThanhVienDoi WHERE DoiThiID = ? AND ThiSinhID = ?",
                doiThiId, thiSinhId);
        }

        public void SetTeamCaptain(int doiThiId, int thiSinhId)
        {
            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                DatabaseHelper.ExecuteNonQuery(conn, tx, "UPDATE ThanhVienDoi SET VaiTro = 'ThanhVien' WHERE DoiThiID = ?", doiThiId);
                DatabaseHelper.ExecuteNonQuery(conn, tx, "UPDATE ThanhVienDoi SET VaiTro = 'DoiTruong' WHERE DoiThiID = ? AND ThiSinhID = ?", doiThiId, thiSinhId);
                SyncCaptainName(conn, tx, doiThiId, thiSinhId);
            });
        }

        public void TransferMember(int thiSinhId, int fromDoiId, int toDoiId)
        {
            if (fromDoiId == toDoiId)
            {
                throw new InvalidOperationException("Đội đích phải khác đội hiện tại.");
            }

            object? ctObj = DatabaseHelper.ExecuteScalar("SELECT ChuongTrinhID FROM DoiThi WHERE DoiThiID = ?", toDoiId);
            int? toProgramId = ctObj == null ? null : Convert.ToInt32(ctObj);

            if (toProgramId.HasValue)
            {
                int conflict = Convert.ToInt32(DatabaseHelper.ExecuteScalar(
                    @"SELECT COUNT(*) FROM ThanhVienDoi
                      WHERE ChuongTrinhID = ? AND ThiSinhID = ? AND DoiThiID <> ? AND DoiThiID <> ? AND TrangThai = True",
                    toProgramId.Value, thiSinhId, fromDoiId, toDoiId) ?? 0);
                if (conflict > 0)
                {
                    throw new InvalidOperationException("Thí sinh đang thuộc một đội khác trong chương trình đích.");
                }
            }

            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                DatabaseHelper.ExecuteNonQuery(conn, tx, "DELETE FROM ThanhVienDoi WHERE DoiThiID = ? AND ThiSinhID = ?", fromDoiId, thiSinhId);
                DatabaseHelper.ExecuteNonQuery(conn, tx, "DELETE FROM ThanhVienDoi WHERE DoiThiID = ? AND ThiSinhID = ?", toDoiId, thiSinhId);
                DatabaseHelper.ExecuteNonQuery(
                    conn, tx,
                    "INSERT INTO ThanhVienDoi (DoiThiID, ThiSinhID, ChuongTrinhID, VaiTro, TrangThai) VALUES (?, ?, ?, 'ThanhVien', True)",
                    toDoiId, thiSinhId, toProgramId);
            });
        }

        public bool DeleteDoiThi(int doiThiId, out string message)
        {
            int usedPrograms = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM ChuongTrinh_ThiSinh WHERE DoiThiID = ?", doiThiId) ?? 0);
            int usedResults = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM KetQua WHERE DoiThiID = ?", doiThiId) ?? 0);

            if (usedPrograms > 0 || usedResults > 0)
            {
                message = "Đội thi đã có dữ liệu thi đấu trong chương trình nên không thể xóa để bảo toàn lịch sử.";
                return false;
            }

            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                DatabaseHelper.ExecuteNonQuery(conn, tx, "DELETE FROM ThanhVienDoi WHERE DoiThiID = ?", doiThiId);
                DatabaseHelper.ExecuteNonQuery(conn, tx, "DELETE FROM DoiThi WHERE DoiThiID = ?", doiThiId);
            });
            message = "Đã xóa đội thi thành công.";
            return true;
        }

        private static void SyncCaptainName(System.Data.OleDb.OleDbConnection conn, System.Data.OleDb.OleDbTransaction tx, int doiThiId, int thiSinhId)
        {
            object? nameObj = DatabaseHelper.ExecuteScalar(conn, tx, "SELECT HoTen FROM ThiSinh WHERE ThiSinhID = ?", thiSinhId);
            string captainName = nameObj?.ToString() ?? string.Empty;
            DatabaseHelper.ExecuteNonQuery(conn, tx, "UPDATE DoiThi SET TenDoiTruong = ? WHERE DoiThiID = ?", captainName, doiThiId);
        }
    }
}
