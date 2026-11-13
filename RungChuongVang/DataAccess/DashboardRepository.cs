using System;
using System.Collections.Generic;
using System.Data;
using RungChuongVang.Enums;
using RungChuongVang.Models;

namespace RungChuongVang.DataAccess
{
    /// <summary>
    /// Truy vấn thống kê tổng quan cho Dashboard từ cơ sở dữ liệu Access thực tế.
    /// </summary>
    public class DashboardRepository
    {
        public DashboardStats GetDashboardStats()
        {
            var stats = new DashboardStats
            {
                TongCauHoi = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM CauHoi") ?? 0),
                CauHoiDangDung = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM CauHoi WHERE TrangThai = True") ?? 0),
                TongLopHoc = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM LopHoc WHERE TrangThai = True") ?? 0),
                TongMonHoc = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM MonHoc WHERE TrangThai = True") ?? 0),
                TongThiSinh = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM ThiSinh WHERE TrangThai = True") ?? 0),
                TongDoiThi = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM DoiThi") ?? 0),
                TongChuongTrinh = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM ChuongTrinh") ?? 0),
                ChuongTrinhDangDienRa = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM ChuongTrinh WHERE TrangThai = 1") ?? 0)
            };

            // Thống kê câu hỏi theo môn học và mức độ
            string sqlBySubject = @"
                SELECT 
                    IIF(m.TenMonHoc IS NULL, 'Chưa phân môn', m.TenMonHoc) AS TenMon,
                    COUNT(c.CauHoiID) AS TongSo,
                    SUM(IIF(c.MucDo = 1, 1, 0)) AS NhanBiet,
                    SUM(IIF(c.MucDo = 2, 1, 0)) AS ThongHieu,
                    SUM(IIF(c.MucDo = 3, 1, 0)) AS VanDung,
                    SUM(IIF(c.MucDo = 4, 1, 0)) AS VanDungCao
                FROM CauHoi c
                LEFT JOIN MonHoc m ON c.MonHocID = m.MonHocID
                WHERE c.TrangThai = True
                GROUP BY IIF(m.TenMonHoc IS NULL, 'Chưa phân môn', m.TenMonHoc)";

            DataTable dtSubject = DatabaseHelper.ExecuteDataTable(sqlBySubject);
            foreach (DataRow row in dtSubject.Rows)
            {
                stats.ThongKeTheoMon.Add(new CauHoiTheoMonSummary
                {
                    TenMonHoc = row["TenMon"]?.ToString() ?? string.Empty,
                    TongSo = Convert.ToInt32(row["TongSo"]),
                    NhanBiet = Convert.ToInt32(row["NhanBiet"]),
                    ThongHieu = Convert.ToInt32(row["ThongHieu"]),
                    VanDung = Convert.ToInt32(row["VanDung"]),
                    VanDungCao = Convert.ToInt32(row["VanDungCao"])
                });
            }

            // Danh sách chương trình gần đây
            string sqlRecent = @"
                SELECT TOP 10 ChuongTrinhID, MaChuongTrinh, TenChuongTrinh, NgayToChuc,
                       DonViToChuc, PhuongThucThi, SoLuongCauHoi, TrangThai
                FROM ChuongTrinh
                ORDER BY NgayToChuc DESC, ChuongTrinhID DESC";

            DataTable dtPrograms = DatabaseHelper.ExecuteDataTable(sqlRecent);
            foreach (DataRow row in dtPrograms.Rows)
            {
                stats.ChuongTrinhGanDay.Add(new ChuongTrinh
                {
                    ChuongTrinhID = Convert.ToInt32(row["ChuongTrinhID"]),
                    MaChuongTrinh = row["MaChuongTrinh"]?.ToString() ?? string.Empty,
                    TenChuongTrinh = row["TenChuongTrinh"]?.ToString() ?? string.Empty,
                    NgayToChuc = Convert.ToDateTime(row["NgayToChuc"]),
                    DonViToChuc = row["DonViToChuc"]?.ToString() ?? string.Empty,
                    PhuongThucThi = (PhuongThucThi)Convert.ToInt32(row["PhuongThucThi"]),
                    SoLuongCauHoi = row["SoLuongCauHoi"] == DBNull.Value ? 0 : Convert.ToInt32(row["SoLuongCauHoi"]),
                    TrangThai = (TrangThaiChuongTrinhEnum)Convert.ToInt32(row["TrangThai"])
                });
            }

            return stats;
        }
    }
}
