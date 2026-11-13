using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using RungChuongVang.Enums;
using RungChuongVang.Models;

namespace RungChuongVang.DataAccess
{
    /// <summary>
    /// Quản lý dữ liệu KetQua, trạng thái thí sinh/đội trong chương trình và TrangThaiChuongTrinh.
    /// </summary>
    public class KetQuaRepository
    {
        public List<KetQua> GetKetQuaByProgram(int chuongTrinhId, int? cauHoiId = null)
        {
            string sql = cauHoiId.HasValue
                ? "SELECT * FROM KetQua WHERE ChuongTrinhID = ? AND CauHoiID = ? ORDER BY KetQuaID ASC"
                : "SELECT * FROM KetQua WHERE ChuongTrinhID = ? ORDER BY KetQuaID ASC";

            DataTable dt = cauHoiId.HasValue
                ? DatabaseHelper.ExecuteDataTable(sql, chuongTrinhId, cauHoiId.Value)
                : DatabaseHelper.ExecuteDataTable(sql, chuongTrinhId);

            var list = new List<KetQua>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new KetQua
                {
                    KetQuaID = Convert.ToInt32(row["KetQuaID"]),
                    ChuongTrinhID = Convert.ToInt32(row["ChuongTrinhID"]),
                    CauHoiID = Convert.ToInt32(row["CauHoiID"]),
                    ThiSinhID = row["ThiSinhID"] == DBNull.Value ? null : Convert.ToInt32(row["ThiSinhID"]),
                    DoiThiID = row["DoiThiID"] == DBNull.Value ? null : Convert.ToInt32(row["DoiThiID"]),
                    DapAnDaChon = row["DapAnDaChon"]?.ToString() ?? string.Empty,
                    KetQuaDungSai = (KetQuaDungSai)Convert.ToInt32(row["KetQuaDungSai"]),
                    ThoiGianTraLoi = row["ThoiGianTraLoi"] == DBNull.Value ? 0 : Convert.ToDouble(row["ThoiGianTraLoi"]),
                    TrangThaiXuLy = row["TrangThaiXuLy"] == DBNull.Value ? TrangThaiXuLyKetQua.Tam : (TrangThaiXuLyKetQua)Convert.ToInt32(row["TrangThaiXuLy"]),
                    Diem = row["Diem"] == DBNull.Value ? 0 : Convert.ToDouble(row["Diem"]),
                    GhiChu = row["GhiChu"]?.ToString() ?? string.Empty
                });
            }
            return list;
        }

        /// <summary>
        /// Chèn hoặc cập nhật kết quả không tạo bản ghi trùng cho cùng đối tượng và câu hỏi.
        /// </summary>
        public int UpsertKetQuaInternal(OleDbConnection conn, OleDbTransaction tx, KetQua kq)
        {
            string lookupSql;
            object?[] lookupArgs;

            if (kq.ThiSinhID.HasValue && kq.DoiThiID.HasValue)
            {
                lookupSql = "SELECT KetQuaID FROM KetQua WHERE ChuongTrinhID = ? AND CauHoiID = ? AND ThiSinhID = ? AND DoiThiID = ?";
                lookupArgs = new object?[] { kq.ChuongTrinhID, kq.CauHoiID, kq.ThiSinhID.Value, kq.DoiThiID.Value };
            }
            else if (kq.ThiSinhID.HasValue)
            {
                lookupSql = "SELECT KetQuaID FROM KetQua WHERE ChuongTrinhID = ? AND CauHoiID = ? AND ThiSinhID = ? AND DoiThiID IS NULL";
                lookupArgs = new object?[] { kq.ChuongTrinhID, kq.CauHoiID, kq.ThiSinhID.Value };
            }
            else
            {
                lookupSql = "SELECT KetQuaID FROM KetQua WHERE ChuongTrinhID = ? AND CauHoiID = ? AND ThiSinhID IS NULL AND DoiThiID = ?";
                lookupArgs = new object?[] { kq.ChuongTrinhID, kq.CauHoiID, kq.DoiThiID };
            }

            object? existing = DatabaseHelper.ExecuteScalar(conn, tx, lookupSql, lookupArgs);
            if (existing != null)
            {
                int existingId = Convert.ToInt32(existing);
                DatabaseHelper.ExecuteNonQuery(
                    conn, tx,
                    @"UPDATE KetQua
                      SET DapAnDaChon = ?, KetQuaDungSai = ?, ThoiGianTraLoi = ?,
                          TrangThaiXuLy = ?, Diem = ?, GhiChu = ?
                      WHERE KetQuaID = ?",
                    kq.DapAnDaChon.Trim(),
                    (int)kq.KetQuaDungSai,
                    kq.ThoiGianTraLoi,
                    (int)kq.TrangThaiXuLy,
                    kq.Diem,
                    kq.GhiChu.Trim(),
                    existingId);
                return existingId;
            }

            DatabaseHelper.ExecuteNonQuery(
                conn, tx,
                @"INSERT INTO KetQua (ChuongTrinhID, CauHoiID, ThiSinhID, DoiThiID, DapAnDaChon,
                                      KetQuaDungSai, ThoiGianTraLoi, TrangThaiXuLy, Diem, GhiChu)
                  VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                kq.ChuongTrinhID,
                kq.CauHoiID,
                kq.ThiSinhID,
                kq.DoiThiID,
                kq.DapAnDaChon.Trim(),
                (int)kq.KetQuaDungSai,
                kq.ThoiGianTraLoi,
                (int)kq.TrangThaiXuLy,
                kq.Diem,
                kq.GhiChu.Trim());
            return DatabaseHelper.GetLastInsertedId(conn, tx);
        }

        public ChuongTrinhThiSinh? GetParticipantRecordById(int recordId)
        {
            DataTable dt = DatabaseHelper.ExecuteDataTable(
                "SELECT * FROM ChuongTrinh_ThiSinh WHERE ID = ?", recordId);
            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new ChuongTrinhThiSinh
            {
                ID = Convert.ToInt32(row["ID"]),
                ChuongTrinhID = Convert.ToInt32(row["ChuongTrinhID"]),
                ThiSinhID = row["ThiSinhID"] == DBNull.Value ? null : Convert.ToInt32(row["ThiSinhID"]),
                DoiThiID = row["DoiThiID"] == DBNull.Value ? null : Convert.ToInt32(row["DoiThiID"]),
                TrangThaiThi = (TrangThaiThi)Convert.ToInt32(row["TrangThaiThi"]),
                SoCauDung = row["SoCauDung"] == DBNull.Value ? 0 : Convert.ToInt32(row["SoCauDung"]),
                SoCauSai = row["SoCauSai"] == DBNull.Value ? 0 : Convert.ToInt32(row["SoCauSai"]),
                SoCauKhongTraLoi = row["SoCauKhongTraLoi"] == DBNull.Value ? 0 : Convert.ToInt32(row["SoCauKhongTraLoi"]),
                CauHoiBiLoai = row["CauHoiBiLoai"] == DBNull.Value ? null : Convert.ToInt32(row["CauHoiBiLoai"]),
                ThoiDiemBiLoai = row["ThoiDiemBiLoai"] == DBNull.Value ? null : Convert.ToDateTime(row["ThoiDiemBiLoai"]),
                ThuHang = row["ThuHang"] == DBNull.Value ? null : Convert.ToInt32(row["ThuHang"]),
                SoLanCuuTro = row["SoLanCuuTro"] == DBNull.Value ? 0 : Convert.ToInt32(row["SoLanCuuTro"])
            };
        }

        public void SaveLiveProgramState(TrangThaiChuongTrinhModel state)
        {
            object? existing = DatabaseHelper.ExecuteScalar(
                "SELECT ID FROM TrangThaiChuongTrinh WHERE ChuongTrinhID = ?",
                state.ChuongTrinhID);

            if (existing != null)
            {
                DatabaseHelper.ExecuteNonQuery(
                    @"UPDATE TrangThaiChuongTrinh
                      SET CauHoiHienTai = ?, TrangThaiTrinhChieu = ?, ThoiGianConLai = ?,
                          TrangThaiDongHo = ?, NgayCapNhat = ?
                      WHERE ChuongTrinhID = ?",
                    state.CauHoiHienTai,
                    (int)state.TrangThaiTrinhChieu,
                    state.ThoiGianConLai,
                    (int)state.TrangThaiDongHo,
                    DateTime.Now,
                    state.ChuongTrinhID);
            }
            else
            {
                DatabaseHelper.ExecuteNonQuery(
                    @"INSERT INTO TrangThaiChuongTrinh
                          (ChuongTrinhID, CauHoiHienTai, TrangThaiTrinhChieu, ThoiGianConLai, TrangThaiDongHo, NgayCapNhat)
                      VALUES (?, ?, ?, ?, ?, ?)",
                    state.ChuongTrinhID,
                    state.CauHoiHienTai,
                    (int)state.TrangThaiTrinhChieu,
                    state.ThoiGianConLai,
                    (int)state.TrangThaiDongHo,
                    DateTime.Now);
            }
        }

        public TrangThaiChuongTrinhModel? LoadLiveProgramState(int chuongTrinhId)
        {
            DataTable dt = DatabaseHelper.ExecuteDataTable(
                "SELECT * FROM TrangThaiChuongTrinh WHERE ChuongTrinhID = ?", chuongTrinhId);
            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new TrangThaiChuongTrinhModel
            {
                ID = Convert.ToInt32(row["ID"]),
                ChuongTrinhID = Convert.ToInt32(row["ChuongTrinhID"]),
                CauHoiHienTai = Convert.ToInt32(row["CauHoiHienTai"]),
                TrangThaiTrinhChieu = (TrangThaiTrinhChieu)Convert.ToInt32(row["TrangThaiTrinhChieu"]),
                ThoiGianConLai = Convert.ToDouble(row["ThoiGianConLai"]),
                TrangThaiDongHo = (TrangThaiDongHo)Convert.ToInt32(row["TrangThaiDongHo"]),
                NgayCapNhat = Convert.ToDateTime(row["NgayCapNhat"])
            };
        }
    }
}
