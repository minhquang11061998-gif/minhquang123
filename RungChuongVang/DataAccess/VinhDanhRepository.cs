using System;
using System.Collections.Generic;
using System.Data;
using RungChuongVang.Models;

namespace RungChuongVang.DataAccess
{
    /// <summary>
    /// Truy cập dữ liệu bảng VinhDanh (lưu trữ nhiều giải thưởng, thứ tự trình chiếu và đường dẫn ảnh tương đối).
    /// </summary>
    public class VinhDanhRepository
    {
        public List<VinhDanh> GetByProgram(int chuongTrinhId)
        {
            DataTable dt = DatabaseHelper.ExecuteDataTable(
                "SELECT * FROM VinhDanh WHERE ChuongTrinhID = ? ORDER BY ThuTuTrinhChieu ASC, VinhDanhID ASC",
                chuongTrinhId);

            var list = new List<VinhDanh>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(MapRow(row));
            }
            return list;
        }

        public int Add(VinhDanh item)
        {
            int newId = 0;
            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                DatabaseHelper.ExecuteNonQuery(
                    conn, tx,
                    @"INSERT INTO VinhDanh (
                          ChuongTrinhID, ThiSinhID, DoiThiID, HoTen, SoBaoDanh, TenLop, TenTruong,
                          TenDoi, TenGiaiThuong, ThanhTich, SoCauDung, SoCauVuotQua,
                          DuongDanHinhAnh, ThuTuTrinhChieu, NgayVinhDanh, GhiChu)
                      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                    item.ChuongTrinhID,
                    item.ThiSinhID,
                    item.DoiThiID,
                    item.HoTen.Trim(),
                    item.SoBaoDanh.Trim(),
                    item.TenLop.Trim(),
                    item.TenTruong.Trim(),
                    item.TenDoi.Trim(),
                    item.TenGiaiThuong.Trim(),
                    item.ThanhTich.Trim(),
                    item.SoCauDung,
                    item.SoCauVuotQua,
                    item.DuongDanHinhAnh.Trim(),
                    item.ThuTuTrinhChieu,
                    item.NgayVinhDanh,
                    item.GhiChu.Trim());
                newId = DatabaseHelper.GetLastInsertedId(conn, tx);
            });
            item.VinhDanhID = newId;
            return newId;
        }

        public void Update(VinhDanh item)
        {
            DatabaseHelper.ExecuteNonQuery(
                @"UPDATE VinhDanh
                  SET ThiSinhID = ?, DoiThiID = ?, HoTen = ?, SoBaoDanh = ?, TenLop = ?, TenTruong = ?,
                      TenDoi = ?, TenGiaiThuong = ?, ThanhTich = ?, SoCauDung = ?, SoCauVuotQua = ?,
                      DuongDanHinhAnh = ?, ThuTuTrinhChieu = ?, NgayVinhDanh = ?, GhiChu = ?
                  WHERE VinhDanhID = ?",
                item.ThiSinhID,
                item.DoiThiID,
                item.HoTen.Trim(),
                item.SoBaoDanh.Trim(),
                item.TenLop.Trim(),
                item.TenTruong.Trim(),
                item.TenDoi.Trim(),
                item.TenGiaiThuong.Trim(),
                item.ThanhTich.Trim(),
                item.SoCauDung,
                item.SoCauVuotQua,
                item.DuongDanHinhAnh.Trim(),
                item.ThuTuTrinhChieu,
                item.NgayVinhDanh,
                item.GhiChu.Trim(),
                item.VinhDanhID);
        }

        public void Delete(int vinhDanhId)
        {
            DatabaseHelper.ExecuteNonQuery("DELETE FROM VinhDanh WHERE VinhDanhID = ?", vinhDanhId);
        }

        public void SwapOrder(VinhDanh first, VinhDanh second)
        {
            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                DatabaseHelper.ExecuteNonQuery(conn, tx, "UPDATE VinhDanh SET ThuTuTrinhChieu = ? WHERE VinhDanhID = ?", second.ThuTuTrinhChieu, first.VinhDanhID);
                DatabaseHelper.ExecuteNonQuery(conn, tx, "UPDATE VinhDanh SET ThuTuTrinhChieu = ? WHERE VinhDanhID = ?", first.ThuTuTrinhChieu, second.VinhDanhID);
            });
        }

        private static VinhDanh MapRow(DataRow row)
        {
            return new VinhDanh
            {
                VinhDanhID = Convert.ToInt32(row["VinhDanhID"]),
                ChuongTrinhID = Convert.ToInt32(row["ChuongTrinhID"]),
                ThiSinhID = row["ThiSinhID"] == DBNull.Value ? null : Convert.ToInt32(row["ThiSinhID"]),
                DoiThiID = row["DoiThiID"] == DBNull.Value ? null : Convert.ToInt32(row["DoiThiID"]),
                HoTen = row["HoTen"]?.ToString() ?? string.Empty,
                SoBaoDanh = row["SoBaoDanh"]?.ToString() ?? string.Empty,
                TenLop = row["TenLop"]?.ToString() ?? string.Empty,
                TenTruong = row["TenTruong"]?.ToString() ?? string.Empty,
                TenDoi = row["TenDoi"]?.ToString() ?? string.Empty,
                TenGiaiThuong = row["TenGiaiThuong"]?.ToString() ?? string.Empty,
                ThanhTich = row["ThanhTich"]?.ToString() ?? string.Empty,
                SoCauDung = row["SoCauDung"] == DBNull.Value ? 0 : Convert.ToInt32(row["SoCauDung"]),
                SoCauVuotQua = row["SoCauVuotQua"] == DBNull.Value ? 0 : Convert.ToInt32(row["SoCauVuotQua"]),
                DuongDanHinhAnh = row["DuongDanHinhAnh"]?.ToString() ?? string.Empty,
                ThuTuTrinhChieu = row["ThuTuTrinhChieu"] == DBNull.Value ? 1 : Convert.ToInt32(row["ThuTuTrinhChieu"]),
                NgayVinhDanh = row["NgayVinhDanh"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(row["NgayVinhDanh"]),
                GhiChu = row["GhiChu"]?.ToString() ?? string.Empty
            };
        }
    }
}
