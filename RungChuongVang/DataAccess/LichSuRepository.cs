using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using RungChuongVang.Models;

namespace RungChuongVang.DataAccess
{
    /// <summary>
    /// Quản lý nhật ký lịch sử loại, lịch sử cứu trợ và nhật ký thao tác phục vụ hoàn tác (không xóa lịch sử).
    /// </summary>
    public class LichSuRepository
    {
        public void InsertLichSuLoaiInternal(OleDbConnection conn, OleDbTransaction tx, LichSuLoai item)
        {
            DatabaseHelper.ExecuteNonQuery(
                conn, tx,
                @"INSERT INTO LichSuLoai (ChuongTrinhID, CauHoiID, ThiSinhID, DoiThiID, LyDoLoai, ThoiGianLoai, NguoiThucHien, GhiChu)
                  VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                item.ChuongTrinhID,
                item.CauHoiID,
                item.ThiSinhID,
                item.DoiThiID,
                item.LyDoLoai.Trim(),
                item.ThoiGianLoai,
                item.NguoiThucHien.Trim(),
                item.GhiChu.Trim());
        }

        public void InsertLichSuCuuTroInternal(OleDbConnection conn, OleDbTransaction tx, LichSuCuuTro item)
        {
            DatabaseHelper.ExecuteNonQuery(
                conn, tx,
                @"INSERT INTO LichSuCuuTro (ChuongTrinhID, CauHoiIDHienTai, ThiSinhID, DoiThiID, LyDoCuuTro, ThoiGianCuuTro, NguoiThucHien)
                  VALUES (?, ?, ?, ?, ?, ?, ?)",
                item.ChuongTrinhID,
                item.CauHoiIDHienTai,
                item.ThiSinhID,
                item.DoiThiID,
                item.LyDoCuuTro.Trim(),
                item.ThoiGianCuuTro,
                item.NguoiThucHien.Trim());
        }

        public void InsertLichSuThaoTacInternal(OleDbConnection conn, OleDbTransaction tx, LichSuThaoTac item)
        {
            DatabaseHelper.ExecuteNonQuery(
                conn, tx,
                @"INSERT INTO LichSuThaoTac (ChuongTrinhID, LoaiThaoTac, DoiTuongID, DuLieuCu, DuLieuMoi, DaHoanTac, ThoiGian, NguoiThucHien)
                  VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                item.ChuongTrinhID,
                item.LoaiThaoTac,
                item.DoiTuongID,
                item.DuLieuCu,
                item.DuLieuMoi,
                item.DaHoanTac,
                item.ThoiGian,
                item.NguoiThucHien);
        }

        public void LogSimpleAction(int chuongTrinhId, string loaiThaoTac, string moTaMoi, string nguoiThucHien = "MC")
        {
            DatabaseHelper.ExecuteNonQuery(
                @"INSERT INTO LichSuThaoTac (ChuongTrinhID, LoaiThaoTac, DoiTuongID, DuLieuCu, DuLieuMoi, DaHoanTac, ThoiGian, NguoiThucHien)
                  VALUES (?, ?, NULL, '', ?, False, ?, ?)",
                chuongTrinhId, loaiThaoTac, moTaMoi, DateTime.Now, nguoiThucHien);
        }

        public LichSuThaoTac? GetLatestUndoableAction(int chuongTrinhId)
        {
            string sql = @"
                SELECT TOP 1 * FROM LichSuThaoTac
                WHERE ChuongTrinhID = ?
                  AND DaHoanTac = False
                  AND LoaiThaoTac IN ('XAC_NHAN_KET_QUA', 'LOAI_THU_CONG', 'CUU_TRO', 'KHOI_PHUC')
                ORDER BY ID DESC";

            DataTable dt = DatabaseHelper.ExecuteDataTable(sql, chuongTrinhId);
            if (dt.Rows.Count == 0) return null;

            return MapThaoTac(dt.Rows[0]);
        }

        public List<LichSuThaoTac> GetAuditLog(int chuongTrinhId, int maxRows = 100)
        {
            string sql = $@"
                SELECT TOP {maxRows} * FROM LichSuThaoTac
                WHERE ChuongTrinhID = ?
                ORDER BY ID DESC";

            DataTable dt = DatabaseHelper.ExecuteDataTable(sql, chuongTrinhId);
            var list = new List<LichSuThaoTac>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(MapThaoTac(row));
            }
            return list;
        }

        public List<LichSuLoai> GetLichSuLoai(int chuongTrinhId)
        {
            DataTable dt = DatabaseHelper.ExecuteDataTable(
                "SELECT * FROM LichSuLoai WHERE ChuongTrinhID = ? ORDER BY ThoiGianLoai DESC",
                chuongTrinhId);
            var list = new List<LichSuLoai>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new LichSuLoai
                {
                    ID = Convert.ToInt32(row["ID"]),
                    ChuongTrinhID = Convert.ToInt32(row["ChuongTrinhID"]),
                    CauHoiID = Convert.ToInt32(row["CauHoiID"]),
                    ThiSinhID = row["ThiSinhID"] == DBNull.Value ? null : Convert.ToInt32(row["ThiSinhID"]),
                    DoiThiID = row["DoiThiID"] == DBNull.Value ? null : Convert.ToInt32(row["DoiThiID"]),
                    LyDoLoai = row["LyDoLoai"]?.ToString() ?? string.Empty,
                    ThoiGianLoai = Convert.ToDateTime(row["ThoiGianLoai"]),
                    NguoiThucHien = row["NguoiThucHien"]?.ToString() ?? string.Empty,
                    GhiChu = row["GhiChu"]?.ToString() ?? string.Empty
                });
            }
            return list;
        }

        private static LichSuThaoTac MapThaoTac(DataRow row)
        {
            return new LichSuThaoTac
            {
                ID = Convert.ToInt32(row["ID"]),
                ChuongTrinhID = Convert.ToInt32(row["ChuongTrinhID"]),
                LoaiThaoTac = row["LoaiThaoTac"]?.ToString() ?? string.Empty,
                DoiTuongID = row["DoiTuongID"] == DBNull.Value ? null : Convert.ToInt32(row["DoiTuongID"]),
                DuLieuCu = row["DuLieuCu"]?.ToString() ?? string.Empty,
                DuLieuMoi = row["DuLieuMoi"]?.ToString() ?? string.Empty,
                DaHoanTac = row["DaHoanTac"] != DBNull.Value && Convert.ToBoolean(row["DaHoanTac"]),
                ThoiGian = Convert.ToDateTime(row["ThoiGian"]),
                NguoiThucHien = row["NguoiThucHien"]?.ToString() ?? string.Empty
            };
        }
    }
}
