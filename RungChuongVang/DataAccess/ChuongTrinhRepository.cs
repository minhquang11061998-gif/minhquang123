using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using RungChuongVang.Enums;
using RungChuongVang.Models;

namespace RungChuongVang.DataAccess
{
    /// <summary>
    /// Truy cập dữ liệu Chương trình thi, Thí sinh/Đội tham gia và Bộ câu hỏi của chương trình.
    /// </summary>
    public class ChuongTrinhRepository
    {
        private readonly CauHoiRepository _cauHoiRepo = new();
        private readonly DoiThiRepository _doiThiRepo = new();

        public List<ChuongTrinh> GetAllPrograms(string keyword = "", TrangThaiChuongTrinhEnum? status = null)
        {
            var conditions = new List<string>();
            var args = new List<object?>();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                conditions.Add("(MaChuongTrinh LIKE ? OR TenChuongTrinh LIKE ? OR DonViToChuc LIKE ?)");
                string pattern = $"%{keyword.Trim()}%";
                args.Add(pattern);
                args.Add(pattern);
                args.Add(pattern);
            }

            if (status.HasValue)
            {
                conditions.Add("TrangThai = ?");
                args.Add((int)status.Value);
            }

            string where = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : string.Empty;
            string sql = $@"
                SELECT ChuongTrinhID, MaChuongTrinh, TenChuongTrinh, NgayToChuc, DonViToChuc,
                       DiaDiem, MoTa, HinhThucThi, PhuongThucThi, QuyTacTraLoiDoi,
                       SoLuongCauHoi, ThoiGianMacDinh, SoLanCuuTroToiDa, QuyTacHetCauHoi,
                       DaKhoaBoCauHoi, TrangThai, GhiChu
                FROM ChuongTrinh
                {where}
                ORDER BY NgayToChuc DESC, ChuongTrinhID DESC";

            DataTable dt = DatabaseHelper.ExecuteDataTable(sql, args.ToArray());
            var list = new List<ChuongTrinh>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(MapRow(row));
            }
            return list;
        }

        public ChuongTrinh? GetById(int chuongTrinhId)
        {
            DataTable dt = DatabaseHelper.ExecuteDataTable(@"
                SELECT ChuongTrinhID, MaChuongTrinh, TenChuongTrinh, NgayToChuc, DonViToChuc,
                       DiaDiem, MoTa, HinhThucThi, PhuongThucThi, QuyTacTraLoiDoi,
                       SoLuongCauHoi, ThoiGianMacDinh, SoLanCuuTroToiDa, QuyTacHetCauHoi,
                       DaKhoaBoCauHoi, TrangThai, GhiChu
                FROM ChuongTrinh
                WHERE ChuongTrinhID = ?", chuongTrinhId);

            return dt.Rows.Count > 0 ? MapRow(dt.Rows[0]) : null;
        }

        public bool ExistsMaChuongTrinh(string ma, int excludeId = 0)
        {
            object? val = DatabaseHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM ChuongTrinh WHERE MaChuongTrinh = ? AND ChuongTrinhID <> ?",
                ma.Trim(), excludeId);
            return Convert.ToInt32(val ?? 0) > 0;
        }

        public string GenerateNextMaChuongTrinh()
        {
            object? val = DatabaseHelper.ExecuteScalar("SELECT MAX(ChuongTrinhID) FROM ChuongTrinh");
            int next = (val == null ? 0 : Convert.ToInt32(val)) + 1;
            string code = $"RCV{DateTime.Now:yy}{next:000}";
            while (ExistsMaChuongTrinh(code))
            {
                next++;
                code = $"RCV{DateTime.Now:yy}{next:000}";
            }
            return code;
        }

        public int AddProgram(ChuongTrinh ct)
        {
            if (string.IsNullOrWhiteSpace(ct.MaChuongTrinh))
            {
                ct.MaChuongTrinh = GenerateNextMaChuongTrinh();
            }

            int newId = 0;
            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                DatabaseHelper.ExecuteNonQuery(
                    conn, tx,
                    @"INSERT INTO ChuongTrinh (
                          MaChuongTrinh, TenChuongTrinh, NgayToChuc, DonViToChuc, DiaDiem, MoTa,
                          HinhThucThi, PhuongThucThi, QuyTacTraLoiDoi, SoLuongCauHoi,
                          ThoiGianMacDinh, SoLanCuuTroToiDa, QuyTacHetCauHoi, DaKhoaBoCauHoi, TrangThai, GhiChu)
                      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                    ct.MaChuongTrinh.Trim(),
                    ct.TenChuongTrinh.Trim(),
                    ct.NgayToChuc,
                    ct.DonViToChuc.Trim(),
                    ct.DiaDiem.Trim(),
                    ct.MoTa.Trim(),
                    "RungChuongVangTruyenThong",
                    (int)ct.PhuongThucThi,
                    (int)ct.QuyTacTraLoiDoi,
                    ct.SoLuongCauHoi,
                    ct.ThoiGianMacDinh > 0 ? ct.ThoiGianMacDinh : 15,
                    ct.SoLanCuuTroToiDa >= 0 ? ct.SoLanCuuTroToiDa : 1,
                    ct.QuyTacHetCauHoi,
                    ct.DaKhoaBoCauHoi,
                    (int)ct.TrangThai,
                    ct.GhiChu.Trim());
                newId = DatabaseHelper.GetLastInsertedId(conn, tx);
            });

            ct.ChuongTrinhID = newId;
            return newId;
        }

        public void UpdateProgram(ChuongTrinh ct)
        {
            DatabaseHelper.ExecuteNonQuery(
                @"UPDATE ChuongTrinh
                  SET MaChuongTrinh = ?, TenChuongTrinh = ?, NgayToChuc = ?, DonViToChuc = ?, DiaDiem = ?,
                      MoTa = ?, PhuongThucThi = ?, QuyTacTraLoiDoi = ?, SoLuongCauHoi = ?,
                      ThoiGianMacDinh = ?, SoLanCuuTroToiDa = ?, QuyTacHetCauHoi = ?,
                      DaKhoaBoCauHoi = ?, TrangThai = ?, GhiChu = ?
                  WHERE ChuongTrinhID = ?",
                ct.MaChuongTrinh.Trim(),
                ct.TenChuongTrinh.Trim(),
                ct.NgayToChuc,
                ct.DonViToChuc.Trim(),
                ct.DiaDiem.Trim(),
                ct.MoTa.Trim(),
                (int)ct.PhuongThucThi,
                (int)ct.QuyTacTraLoiDoi,
                ct.SoLuongCauHoi,
                ct.ThoiGianMacDinh > 0 ? ct.ThoiGianMacDinh : 15,
                ct.SoLanCuuTroToiDa >= 0 ? ct.SoLanCuuTroToiDa : 1,
                ct.QuyTacHetCauHoi,
                ct.DaKhoaBoCauHoi,
                (int)ct.TrangThai,
                ct.GhiChu.Trim(),
                ct.ChuongTrinhID);
        }

        public bool DeleteProgram(int chuongTrinhId, out string message)
        {
            var ct = GetById(chuongTrinhId);
            if (ct == null)
            {
                message = "Không tìm thấy chương trình.";
                return false;
            }

            int resultCount = Convert.ToInt32(DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM KetQua WHERE ChuongTrinhID = ?", chuongTrinhId) ?? 0);
            if (ct.TrangThai != TrangThaiChuongTrinhEnum.ChuaBatDau || resultCount > 0)
            {
                message = "Chương trình đã bắt đầu hoặc đã có dữ liệu kết quả thi nên không thể xóa.";
                return false;
            }

            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                DatabaseHelper.ExecuteNonQuery(conn, tx, "DELETE FROM TrangThaiChuongTrinh WHERE ChuongTrinhID = ?", chuongTrinhId);
                DatabaseHelper.ExecuteNonQuery(conn, tx, "DELETE FROM ChuongTrinh_CauHoi WHERE ChuongTrinhID = ?", chuongTrinhId);
                DatabaseHelper.ExecuteNonQuery(conn, tx, "DELETE FROM ChuongTrinh_ThiSinh WHERE ChuongTrinhID = ?", chuongTrinhId);
                DatabaseHelper.ExecuteNonQuery(conn, tx, "DELETE FROM ChuongTrinh WHERE ChuongTrinhID = ?", chuongTrinhId);
            });

            message = "Đã xóa chương trình thi thành công.";
            return true;
        }

        #region Thí sinh / Đội tham gia chương trình

        public List<ChuongTrinhThiSinh> GetParticipants(int chuongTrinhId, PhuongThucThi mode)
        {
            var list = new List<ChuongTrinhThiSinh>();

            if (mode == PhuongThucThi.ThiCaNhan)
            {
                string sql = @"
                    SELECT ctts.ID, ctts.ChuongTrinhID, ctts.ThiSinhID, ctts.DoiThiID, ctts.TrangThaiThi,
                           ctts.SoCauDung, ctts.SoCauSai, ctts.SoCauKhongTraLoi, ctts.CauHoiBiLoai,
                           ctts.ThoiDiemBiLoai, ctts.ThuHang, ctts.SoLanCuuTro,
                           ts.SoBaoDanh, ts.HoTen, l.TenLop, ts.TenTruong
                    FROM (ChuongTrinh_ThiSinh ctts
                    INNER JOIN ThiSinh ts ON ctts.ThiSinhID = ts.ThiSinhID)
                    LEFT JOIN LopHoc l ON ts.LopHocID = l.LopHocID
                    WHERE ctts.ChuongTrinhID = ? AND ctts.ThiSinhID IS NOT NULL
                    ORDER BY ts.SoBaoDanh ASC, ts.HoTen ASC";

                DataTable dt = DatabaseHelper.ExecuteDataTable(sql, chuongTrinhId);
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(MapParticipantRow(row, false));
                }
            }
            else
            {
                string sql = @"
                    SELECT ctts.ID, ctts.ChuongTrinhID, ctts.ThiSinhID, ctts.DoiThiID, ctts.TrangThaiThi,
                           ctts.SoCauDung, ctts.SoCauSai, ctts.SoCauKhongTraLoi, ctts.CauHoiBiLoai,
                           ctts.ThoiDiemBiLoai, ctts.ThuHang, ctts.SoLanCuuTro,
                           d.MaDoi, d.TenDoi, d.TenLop, d.TenTruong,
                           (SELECT COUNT(*) FROM ThanhVienDoi tv WHERE tv.DoiThiID = d.DoiThiID AND tv.TrangThai = True) AS SoTV
                    FROM ChuongTrinh_ThiSinh ctts
                    INNER JOIN DoiThi d ON ctts.DoiThiID = d.DoiThiID
                    WHERE ctts.ChuongTrinhID = ? AND ctts.DoiThiID IS NOT NULL
                    ORDER BY d.TenDoi ASC";

                DataTable dt = DatabaseHelper.ExecuteDataTable(sql, chuongTrinhId);
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(MapParticipantRow(row, true));
                }
            }

            return list;
        }

        public void SaveParticipants(int chuongTrinhId, PhuongThucThi mode, List<int> selectedIds)
        {
            var distinctIds = selectedIds.Distinct().ToList();

            // Nếu thi theo đội, kiểm tra không có thí sinh nào trùng giữa các đội được chọn
            if (mode == PhuongThucThi.ThiTheoDoi && distinctIds.Count > 1)
            {
                var seenStudentIds = new HashSet<int>();
                foreach (int doiId in distinctIds)
                {
                    var members = _doiThiRepo.GetThanhVienByDoi(doiId);
                    foreach (var m in members)
                    {
                        if (!seenStudentIds.Add(m.ThiSinhID))
                        {
                            throw new InvalidOperationException(
                                $"Thí sinh '{m.HoTenThiSinh}' (SBD: {m.SoBaoDanh}) đang thuộc hơn một đội được chọn. Một thí sinh không được thuộc nhiều đội trong cùng một chương trình.");
                        }
                    }
                }
            }

            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                DatabaseHelper.ExecuteNonQuery(conn, tx, "DELETE FROM ChuongTrinh_ThiSinh WHERE ChuongTrinhID = ?", chuongTrinhId);

                foreach (int id in distinctIds)
                {
                    int? tsId = mode == PhuongThucThi.ThiCaNhan ? id : null;
                    int? doiId = mode == PhuongThucThi.ThiTheoDoi ? id : null;

                    DatabaseHelper.ExecuteNonQuery(
                        conn, tx,
                        @"INSERT INTO ChuongTrinh_ThiSinh
                              (ChuongTrinhID, ThiSinhID, DoiThiID, TrangThaiThi, SoCauDung, SoCauSai, SoCauKhongTraLoi, SoLanCuuTro)
                          VALUES (?, ?, ?, ?, 0, 0, 0, 0)",
                        chuongTrinhId, tsId, doiId, (int)TrangThaiThi.DangThi);
                }
            });
        }

        #endregion

        #region Bộ câu hỏi chương trình

        public List<ChuongTrinhCauHoi> GetProgramQuestions(int chuongTrinhId, bool? laCauHoiPhu = null)
        {
            string filterExtra = laCauHoiPhu.HasValue ? " AND ctc.LaCauHoiPhu = ?" : string.Empty;
            var args = new List<object?> { chuongTrinhId };
            if (laCauHoiPhu.HasValue) args.Add(laCauHoiPhu.Value);

            string sql = $@"
                SELECT ctc.ID, ctc.ChuongTrinhID, ctc.CauHoiID, ctc.ThuTu,
                       ctc.ThoiGianTraLoi, ctc.LaCauHoiPhu, ctc.TrangThai
                FROM ChuongTrinh_CauHoi ctc
                WHERE ctc.ChuongTrinhID = ?{filterExtra}
                ORDER BY ctc.LaCauHoiPhu ASC, ctc.ThuTu ASC";

            DataTable dt = DatabaseHelper.ExecuteDataTable(sql, args.ToArray());
            var result = new List<ChuongTrinhCauHoi>();
            foreach (DataRow row in dt.Rows)
            {
                int cauHoiId = Convert.ToInt32(row["CauHoiID"]);
                result.Add(new ChuongTrinhCauHoi
                {
                    ID = Convert.ToInt32(row["ID"]),
                    ChuongTrinhID = Convert.ToInt32(row["ChuongTrinhID"]),
                    CauHoiID = cauHoiId,
                    ThuTu = Convert.ToInt32(row["ThuTu"]),
                    ThoiGianTraLoi = row["ThoiGianTraLoi"] == DBNull.Value ? 15 : Convert.ToInt32(row["ThoiGianTraLoi"]),
                    LaCauHoiPhu = row["LaCauHoiPhu"] != DBNull.Value && Convert.ToBoolean(row["LaCauHoiPhu"]),
                    TrangThai = row["TrangThai"] == DBNull.Value ? 0 : Convert.ToInt32(row["TrangThai"]),
                    CauHoiChiTiet = _cauHoiRepo.GetById(cauHoiId)
                });
            }
            return result;
        }

        public void SaveProgramQuestions(int chuongTrinhId, List<ChuongTrinhCauHoi> items, bool lockSet)
        {
            var ct = GetById(chuongTrinhId) ?? throw new InvalidOperationException("Không tìm thấy chương trình.");
            if (ct.DaKhoaBoCauHoi || ct.TrangThai != TrangThaiChuongTrinhEnum.ChuaBatDau)
            {
                throw new InvalidOperationException("Bộ câu hỏi đã bị khóa hoặc chương trình đã bắt đầu nên không thể thay đổi thứ tự/danh sách câu hỏi chính.");
            }

            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                DatabaseHelper.ExecuteNonQuery(
                    conn, tx,
                    "DELETE FROM ChuongTrinh_CauHoi WHERE ChuongTrinhID = ? AND LaCauHoiPhu = False",
                    chuongTrinhId);

                int order = 1;
                foreach (var item in items)
                {
                    DatabaseHelper.ExecuteNonQuery(
                        conn, tx,
                        @"INSERT INTO ChuongTrinh_CauHoi (ChuongTrinhID, CauHoiID, ThuTu, ThoiGianTraLoi, LaCauHoiPhu, TrangThai)
                          VALUES (?, ?, ?, ?, False, 0)",
                        chuongTrinhId,
                        item.CauHoiID,
                        order++,
                        item.ThoiGianTraLoi > 0 ? item.ThoiGianTraLoi : ct.ThoiGianMacDinh);
                }

                DatabaseHelper.ExecuteNonQuery(
                    conn, tx,
                    "UPDATE ChuongTrinh SET SoLuongCauHoi = ?, DaKhoaBoCauHoi = ? WHERE ChuongTrinhID = ?",
                    items.Count, lockSet, chuongTrinhId);
            });
        }

        public void LockProgramQuestions(int chuongTrinhId)
        {
            DatabaseHelper.ExecuteNonQuery("UPDATE ChuongTrinh SET DaKhoaBoCauHoi = True WHERE ChuongTrinhID = ?", chuongTrinhId);
        }

        public int AddSupplementaryQuestion(int chuongTrinhId, int cauHoiId, int thoiGianTraLoi)
        {
            int nextOrder = Convert.ToInt32(DatabaseHelper.ExecuteScalar(
                "SELECT MAX(ThuTu) FROM ChuongTrinh_CauHoi WHERE ChuongTrinhID = ?", chuongTrinhId) ?? 0) + 1;

            int newId = 0;
            DatabaseHelper.ExecuteInTransaction((conn, tx) =>
            {
                DatabaseHelper.ExecuteNonQuery(
                    conn, tx,
                    @"INSERT INTO ChuongTrinh_CauHoi (ChuongTrinhID, CauHoiID, ThuTu, ThoiGianTraLoi, LaCauHoiPhu, TrangThai)
                      VALUES (?, ?, ?, ?, True, 0)",
                    chuongTrinhId, cauHoiId, nextOrder, thoiGianTraLoi > 0 ? thoiGianTraLoi : 15);
                newId = DatabaseHelper.GetLastInsertedId(conn, tx);
            });
            return newId;
        }

        #endregion

        private static ChuongTrinh MapRow(DataRow row)
        {
            return new ChuongTrinh
            {
                ChuongTrinhID = Convert.ToInt32(row["ChuongTrinhID"]),
                MaChuongTrinh = row["MaChuongTrinh"]?.ToString() ?? string.Empty,
                TenChuongTrinh = row["TenChuongTrinh"]?.ToString() ?? string.Empty,
                NgayToChuc = Convert.ToDateTime(row["NgayToChuc"]),
                DonViToChuc = row["DonViToChuc"]?.ToString() ?? string.Empty,
                DiaDiem = row["DiaDiem"]?.ToString() ?? string.Empty,
                MoTa = row["MoTa"]?.ToString() ?? string.Empty,
                HinhThucThi = row["HinhThucThi"]?.ToString() ?? "RungChuongVangTruyenThong",
                PhuongThucThi = (PhuongThucThi)Convert.ToInt32(row["PhuongThucThi"]),
                QuyTacTraLoiDoi = row["QuyTacTraLoiDoi"] == DBNull.Value ? QuyTacTraLoiDoi.DoiTruongQuyetDinh : (QuyTacTraLoiDoi)Convert.ToInt32(row["QuyTacTraLoiDoi"]),
                SoLuongCauHoi = row["SoLuongCauHoi"] == DBNull.Value ? 0 : Convert.ToInt32(row["SoLuongCauHoi"]),
                ThoiGianMacDinh = row["ThoiGianMacDinh"] == DBNull.Value ? 15 : Convert.ToInt32(row["ThoiGianMacDinh"]),
                SoLanCuuTroToiDa = row["SoLanCuuTroToiDa"] == DBNull.Value ? 1 : Convert.ToInt32(row["SoLanCuuTroToiDa"]),
                QuyTacHetCauHoi = row["QuyTacHetCauHoi"] == DBNull.Value ? 1 : Convert.ToInt32(row["QuyTacHetCauHoi"]),
                DaKhoaBoCauHoi = row["DaKhoaBoCauHoi"] != DBNull.Value && Convert.ToBoolean(row["DaKhoaBoCauHoi"]),
                TrangThai = (TrangThaiChuongTrinhEnum)Convert.ToInt32(row["TrangThai"]),
                GhiChu = row["GhiChu"]?.ToString() ?? string.Empty
            };
        }

        private static ChuongTrinhThiSinh MapParticipantRow(DataRow row, bool isTeam)
        {
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
                SoLanCuuTro = row["SoLanCuuTro"] == DBNull.Value ? 0 : Convert.ToInt32(row["SoLanCuuTro"]),
                SoBaoDanh = isTeam ? (row["MaDoi"]?.ToString() ?? string.Empty) : (row["SoBaoDanh"]?.ToString() ?? string.Empty),
                TenHienThi = isTeam ? (row["TenDoi"]?.ToString() ?? string.Empty) : (row["HoTen"]?.ToString() ?? string.Empty),
                TenLop = row["TenLop"]?.ToString() ?? string.Empty,
                TenTruong = row["TenTruong"]?.ToString() ?? string.Empty,
                SoThanhVien = isTeam && row.Table.Columns.Contains("SoTV") && row["SoTV"] != DBNull.Value ? Convert.ToInt32(row["SoTV"]) : 1
            };
        }
    }
}
