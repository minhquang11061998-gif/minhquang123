using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using RungChuongVang.Enums;
using RungChuongVang.Models;

namespace RungChuongVang.DataAccess
{
    /// <summary>
    /// Truy cập dữ liệu Ngân hàng câu hỏi (CauHoi, DapAn, CauHoi_LopHoc).
    /// Hỗ trợ 3 loại câu hỏi, đa lớp học, sao chép, ngừng sử dụng/khôi phục và tìm kiếm lọc đa tiêu chí.
    /// </summary>
    public class CauHoiRepository
    {
        public List<CauHoi> SearchCauHoi(
            string keyword = "",
            int? lopHocId = null,
            int? monHocId = null,
            int? chuDeId = null,
            MucDoKho? mucDo = null,
            LoaiCauHoi? loaiCauHoi = null,
            bool? trangThai = null)
        {
            var conditions = new List<string>();
            var args = new List<object?>();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                conditions.Add("(c.MaCauHoi LIKE ? OR c.NoiDung LIKE ?)");
                string pattern = $"%{keyword.Trim()}%";
                args.Add(pattern);
                args.Add(pattern);
            }

            if (monHocId.HasValue && monHocId.Value > 0)
            {
                conditions.Add("c.MonHocID = ?");
                args.Add(monHocId.Value);
            }

            if (chuDeId.HasValue && chuDeId.Value > 0)
            {
                conditions.Add("c.ChuDeID = ?");
                args.Add(chuDeId.Value);
            }

            if (mucDo.HasValue)
            {
                conditions.Add("c.MucDo = ?");
                args.Add((int)mucDo.Value);
            }

            if (loaiCauHoi.HasValue)
            {
                conditions.Add("c.LoaiCauHoi = ?");
                args.Add((int)loaiCauHoi.Value);
            }

            if (trangThai.HasValue)
            {
                conditions.Add("c.TrangThai = ?");
                args.Add(trangThai.Value);
            }

            if (lopHocId.HasValue && lopHocId.Value > 0)
            {
                conditions.Add("EXISTS (SELECT 1 FROM CauHoi_LopHoc chl WHERE chl.CauHoiID = c.CauHoiID AND chl.LopHocID = ?)");
                args.Add(lopHocId.Value);
            }

            string where = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : string.Empty;
            string sql = $@"
                SELECT c.CauHoiID, c.MaCauHoi, c.NoiDung, c.MonHocID, c.ChuDeID, c.MucDo,
                       c.LoaiCauHoi, c.GiaiThich, c.ThoiGianTraLoi, c.DuongDanHinhAnh,
                       c.TrangThai, c.NgayTao, c.NgayCapNhat,
                       m.TenMonHoc, cd.TenChuDe
                FROM (CauHoi c
                LEFT JOIN MonHoc m ON c.MonHocID = m.MonHocID)
                LEFT JOIN ChuDe cd ON c.ChuDeID = cd.ChuDeID
                {where}
                ORDER BY c.CauHoiID DESC";

            DataTable dt = DatabaseHelper.ExecuteDataTable(sql, args.ToArray());
            var result = new List<CauHoi>();
            foreach (DataRow row in dt.Rows)
            {
                result.Add(MapRow(row));
            }

            // Nạp tên lớp áp dụng cho danh sách câu hỏi
            if (result.Count > 0)
            {
                var classLookup = GetClassNamesByQuestionMap();
                foreach (var q in result)
                {
                    if (classLookup.TryGetValue(q.CauHoiID, out var info))
                    {
                        q.LopHocIds = info.Ids;
                        q.DanhSachTenLop = string.Join(", ", info.Names);
                    }
                }
            }

            return result;
        }

        public CauHoi? GetById(int cauHoiId)
        {
            string sql = @"
                SELECT c.CauHoiID, c.MaCauHoi, c.NoiDung, c.MonHocID, c.ChuDeID, c.MucDo,
                       c.LoaiCauHoi, c.GiaiThich, c.ThoiGianTraLoi, c.DuongDanHinhAnh,
                       c.TrangThai, c.NgayTao, c.NgayCapNhat,
                       m.TenMonHoc, cd.TenChuDe
                FROM (CauHoi c
                LEFT JOIN MonHoc m ON c.MonHocID = m.MonHocID)
                LEFT JOIN ChuDe cd ON c.ChuDeID = cd.ChuDeID
                WHERE c.CauHoiID = ?";

            DataTable dt = DatabaseHelper.ExecuteDataTable(sql, cauHoiId);
            if (dt.Rows.Count == 0)
            {
                return null;
            }

            CauHoi q = MapRow(dt.Rows[0]);
            q.LopHocIds = GetLopHocIds(cauHoiId);
            q.DanhSachDapAn = GetDapAnByCauHoi(cauHoiId);
            return q;
        }

        public List<DapAn> GetDapAnByCauHoi(int cauHoiId)
        {
            DataTable dt = DatabaseHelper.ExecuteDataTable(
                "SELECT DapAnID, CauHoiID, KyHieu, NoiDungDapAn, LaDapAnDung, ThuTu FROM DapAn WHERE CauHoiID = ? ORDER BY ThuTu ASC, DapAnID ASC",
                cauHoiId);

            var list = new List<DapAn>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new DapAn
                {
                    DapAnID = Convert.ToInt32(row["DapAnID"]),
                    CauHoiID = Convert.ToInt32(row["CauHoiID"]),
                    KyHieu = row["KyHieu"]?.ToString() ?? string.Empty,
                    NoiDungDapAn = row["NoiDungDapAn"]?.ToString() ?? string.Empty,
                    LaDapAnDung = row["LaDapAnDung"] != DBNull.Value && Convert.ToBoolean(row["LaDapAnDung"]),
                    ThuTu = Convert.ToInt32(row["ThuTu"])
                });
            }
            return list;
        }

        public List<int> GetLopHocIds(int cauHoiId)
        {
            DataTable dt = DatabaseHelper.ExecuteDataTable(
                "SELECT LopHocID FROM CauHoi_LopHoc WHERE CauHoiID = ?", cauHoiId);
            var ids = new List<int>();
            foreach (DataRow row in dt.Rows)
            {
                ids.Add(Convert.ToInt32(row["LopHocID"]));
            }
            return ids;
        }

        public bool ExistsMaCauHoi(string maCauHoi, int excludeId = 0)
        {
            object? val = DatabaseHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM CauHoi WHERE MaCauHoi = ? AND CauHoiID <> ?",
                maCauHoi.Trim(), excludeId);
            return Convert.ToInt32(val ?? 0) > 0;
        }

        public int? FindExistingIdByNoiDung(string noiDung)
        {
            // Access MEMO không hỗ trợ = trực tiếp trong một số ngữ cảnh phức tạp nhưng hỗ trợ với tham số hoặc StrComp
            DataTable dt = DatabaseHelper.ExecuteDataTable("SELECT CauHoiID, NoiDung FROM CauHoi");
            string normalized = noiDung.Trim();
            foreach (DataRow row in dt.Rows)
            {
                string existing = (row["NoiDung"]?.ToString() ?? string.Empty).Trim();
                if (string.Equals(existing, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return Convert.ToInt32(row["CauHoiID"]);
                }
            }
            return null;
        }

        public string GenerateNextMaCauHoi()
        {
            object? val = DatabaseHelper.ExecuteScalar("SELECT MAX(CauHoiID) FROM CauHoi");
            int next = (val == null ? 0 : Convert.ToInt32(val)) + 1;
            string code = $"CH{next:0000}";
            while (ExistsMaCauHoi(code))
            {
                next++;
                code = $"CH{next:0000}";
            }
            return code;
        }

        public int AddCauHoi(CauHoi q)
        {
            if (string.IsNullOrWhiteSpace(q.MaCauHoi))
            {
                q.MaCauHoi = GenerateNextMaCauHoi();
            }

            int newId = 0;
            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                newId = InsertQuestionInternal(conn, tx, q);
            });
            return newId;
        }

        public void UpdateCauHoi(CauHoi q)
        {
            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                UpdateQuestionInternal(conn, tx, q);
            });
        }

        public int InsertQuestionInternal(OleDbConnection conn, OleDbTransaction tx, CauHoi q)
        {
            DateTime now = DateTime.Now;
            DatabaseHelper.ExecuteNonQuery(
                conn, tx,
                @"INSERT INTO CauHoi (MaCauHoi, NoiDung, MonHocID, ChuDeID, MucDo, LoaiCauHoi,
                                      GiaiThich, ThoiGianTraLoi, DuongDanHinhAnh, TrangThai, NgayTao, NgayCapNhat)
                  VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                q.MaCauHoi.Trim(),
                q.NoiDung.Trim(),
                q.MonHocID,
                q.ChuDeID,
                (int)q.MucDo,
                (int)q.LoaiCauHoi,
                q.GiaiThich.Trim(),
                q.ThoiGianTraLoi > 0 ? q.ThoiGianTraLoi : 15,
                q.DuongDanHinhAnh.Trim(),
                q.TrangThai,
                now,
                now);

            int newId = DatabaseHelper.GetLastInsertedId(conn, tx);
            q.CauHoiID = newId;

            SaveClassesAndAnswersInternal(conn, tx, newId, q.LopHocIds, q.DanhSachDapAn);
            return newId;
        }

        public void UpdateQuestionInternal(OleDbConnection conn, OleDbTransaction tx, CauHoi q)
        {
            DatabaseHelper.ExecuteNonQuery(
                conn, tx,
                @"UPDATE CauHoi
                  SET MaCauHoi = ?, NoiDung = ?, MonHocID = ?, ChuDeID = ?, MucDo = ?, LoaiCauHoi = ?,
                      GiaiThich = ?, ThoiGianTraLoi = ?, DuongDanHinhAnh = ?, TrangThai = ?, NgayCapNhat = ?
                  WHERE CauHoiID = ?",
                q.MaCauHoi.Trim(),
                q.NoiDung.Trim(),
                q.MonHocID,
                q.ChuDeID,
                (int)q.MucDo,
                (int)q.LoaiCauHoi,
                q.GiaiThich.Trim(),
                q.ThoiGianTraLoi > 0 ? q.ThoiGianTraLoi : 15,
                q.DuongDanHinhAnh.Trim(),
                q.TrangThai,
                DateTime.Now,
                q.CauHoiID);

            DatabaseHelper.ExecuteNonQuery(conn, tx, "DELETE FROM CauHoi_LopHoc WHERE CauHoiID = ?", q.CauHoiID);
            DatabaseHelper.ExecuteNonQuery(conn, tx, "DELETE FROM DapAn WHERE CauHoiID = ?", q.CauHoiID);
            SaveClassesAndAnswersInternal(conn, tx, q.CauHoiID, q.LopHocIds, q.DanhSachDapAn);
        }

        private static void SaveClassesAndAnswersInternal(
            OleDbConnection conn,
            OleDbTransaction tx,
            int cauHoiId,
            List<int> lopHocIds,
            List<DapAn> answers)
        {
            foreach (int lopId in lopHocIds.Distinct())
            {
                DatabaseHelper.ExecuteNonQuery(
                    conn, tx,
                    "INSERT INTO CauHoi_LopHoc (CauHoiID, LopHocID) VALUES (?, ?)",
                    cauHoiId, lopId);
            }

            int order = 1;
            foreach (var da in answers)
            {
                DatabaseHelper.ExecuteNonQuery(
                    conn, tx,
                    "INSERT INTO DapAn (CauHoiID, KyHieu, NoiDungDapAn, LaDapAnDung, ThuTu) VALUES (?, ?, ?, ?, ?)",
                    cauHoiId,
                    da.KyHieu.Trim(),
                    da.NoiDungDapAn.Trim(),
                    da.LaDapAnDung,
                    da.ThuTu > 0 ? da.ThuTu : order);
                order++;
            }
        }

        public int CloneQuestion(int sourceCauHoiId)
        {
            CauHoi? source = GetById(sourceCauHoiId);
            if (source == null)
            {
                throw new InvalidOperationException("Không tìm thấy câu hỏi gốc để sao chép.");
            }

            source.CauHoiID = 0;
            source.MaCauHoi = GenerateNextMaCauHoi();
            source.NoiDung = source.NoiDung + " (Bản sao)";
            source.TrangThai = true;
            return AddCauHoi(source);
        }

        public void SetActiveStatus(int cauHoiId, bool active)
        {
            DatabaseHelper.ExecuteNonQuery(
                "UPDATE CauHoi SET TrangThai = ?, NgayCapNhat = ? WHERE CauHoiID = ?",
                active, DateTime.Now, cauHoiId);
        }

        public bool DeleteCauHoi(int cauHoiId, out string message)
        {
            int usedInPrograms = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM ChuongTrinh_CauHoi WHERE CauHoiID = ?", cauHoiId) ?? 0);
            int usedInResults = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM KetQua WHERE CauHoiID = ?", cauHoiId) ?? 0);

            if (usedInPrograms > 0 || usedInResults > 0)
            {
                SetActiveStatus(cauHoiId, false);
                message = "Câu hỏi đã được sử dụng trong chương trình thi nên được chuyển sang Ngừng sử dụng để bảo toàn lịch sử.";
                return false;
            }

            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                DatabaseHelper.ExecuteNonQuery(conn, tx, "DELETE FROM DapAn WHERE CauHoiID = ?", cauHoiId);
                DatabaseHelper.ExecuteNonQuery(conn, tx, "DELETE FROM CauHoi_LopHoc WHERE CauHoiID = ?", cauHoiId);
                DatabaseHelper.ExecuteNonQuery(conn, tx, "DELETE FROM CauHoi WHERE CauHoiID = ?", cauHoiId);
            });
            message = "Đã xóa câu hỏi khỏi ngân hàng câu hỏi.";
            return true;
        }

        private Dictionary<int, (List<int> Ids, List<string> Names)> GetClassNamesByQuestionMap()
        {
            var map = new Dictionary<int, (List<int> Ids, List<string> Names)>();
            DataTable dt = DatabaseHelper.ExecuteDataTable(@"
                SELECT chl.CauHoiID, l.LopHocID, l.TenLop
                FROM CauHoi_LopHoc chl
                INNER JOIN LopHoc l ON chl.LopHocID = l.LopHocID
                ORDER BY l.KhoiLop ASC, l.TenLop ASC");

            foreach (DataRow row in dt.Rows)
            {
                int qId = Convert.ToInt32(row["CauHoiID"]);
                int lId = Convert.ToInt32(row["LopHocID"]);
                string lName = row["TenLop"]?.ToString() ?? string.Empty;

                if (!map.TryGetValue(qId, out var entry))
                {
                    entry = (new List<int>(), new List<string>());
                    map[qId] = entry;
                }
                entry.Ids.Add(lId);
                entry.Names.Add(lName);
            }
            return map;
        }

        private static CauHoi MapRow(DataRow row)
        {
            return new CauHoi
            {
                CauHoiID = Convert.ToInt32(row["CauHoiID"]),
                MaCauHoi = row["MaCauHoi"]?.ToString() ?? string.Empty,
                NoiDung = row["NoiDung"]?.ToString() ?? string.Empty,
                MonHocID = row["MonHocID"] == DBNull.Value ? null : Convert.ToInt32(row["MonHocID"]),
                ChuDeID = row["ChuDeID"] == DBNull.Value ? null : Convert.ToInt32(row["ChuDeID"]),
                MucDo = (MucDoKho)Convert.ToInt32(row["MucDo"]),
                LoaiCauHoi = (LoaiCauHoi)Convert.ToInt32(row["LoaiCauHoi"]),
                GiaiThich = row["GiaiThich"]?.ToString() ?? string.Empty,
                ThoiGianTraLoi = row["ThoiGianTraLoi"] == DBNull.Value ? 15 : Convert.ToInt32(row["ThoiGianTraLoi"]),
                DuongDanHinhAnh = row["DuongDanHinhAnh"]?.ToString() ?? string.Empty,
                TrangThai = row["TrangThai"] != DBNull.Value && Convert.ToBoolean(row["TrangThai"]),
                NgayTao = row["NgayTao"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(row["NgayTao"]),
                NgayCapNhat = row["NgayCapNhat"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(row["NgayCapNhat"]),
                TenMonHoc = row["TenMonHoc"]?.ToString() ?? string.Empty,
                TenChuDe = row["TenChuDe"]?.ToString() ?? string.Empty
            };
        }
    }
}
