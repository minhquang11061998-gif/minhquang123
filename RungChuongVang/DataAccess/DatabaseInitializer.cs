using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using RungChuongVang.Helpers;

namespace RungChuongVang.DataAccess
{
    /// <summary>
    /// Tự động khởi tạo tệp cơ sở dữ liệu Microsoft Access (RungChuongVang.accdb),
    /// tạo đầy đủ 19 bảng, chỉ mục, khóa ngoại và nâng cấp schema an toàn không mất dữ liệu.
    /// </summary>
    public static class DatabaseInitializer
    {
        private const int CurrentSchemaVersion = 1;

        public static void EnsureDatabaseInitialized()
        {
            FileHelper.EnsureApplicationDirectories();

            if (!File.Exists(DatabaseHelper.DatabasePath))
            {
                CreateEmptyAccessDatabase(DatabaseHelper.DatabasePath);
            }

            using var conn = DatabaseHelper.OpenConnectionWithRetry();
            var existingTables = GetExistingTableNames(conn);

            CreateAllTablesIfMissing(conn, existingTables);
            EnsureDefaultConfigs(conn);
            RunSafeSchemaMigrations(conn);
        }

        /// <summary>
        /// Tạo mới file .accdb trống thông qua ADOX.Catalog COM có sẵn trên Windows.
        /// </summary>
        private static void CreateEmptyAccessDatabase(string dbPath)
        {
            Type? catalogType = Type.GetTypeFromProgID("ADOX.Catalog");
            if (catalogType == null)
            {
                throw new InvalidOperationException(
                    "Không thể khởi tạo đối tượng ADOX.Catalog để tạo tệp Access (.accdb) mới.");
            }

            object? catalog = null;
            try
            {
                catalog = Activator.CreateInstance(catalogType);
                if (catalog == null)
                {
                    throw new InvalidOperationException("Không thể tạo instance của ADOX.Catalog.");
                }

                string connStr = DatabaseHelper.GetConnectionString(dbPath);
                catalogType.InvokeMember(
                    "Create",
                    BindingFlags.InvokeMethod,
                    null,
                    catalog,
                    new object[] { connStr });
            }
            finally
            {
                if (catalog != null && Marshal.IsComObject(catalog))
                {
                    Marshal.FinalReleaseComObject(catalog);
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private static HashSet<string> GetExistingTableNames(OleDbConnection conn)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            DataTable? schema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object?[] { null, null, null, "TABLE" });
            if (schema == null)
            {
                return result;
            }

            foreach (DataRow row in schema.Rows)
            {
                string? tableName = row["TABLE_NAME"]?.ToString();
                if (!string.IsNullOrWhiteSpace(tableName))
                {
                    result.Add(tableName);
                }
            }
            return result;
        }

        private static void ExecuteDdl(OleDbConnection conn, string sql)
        {
            using var cmd = new OleDbCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        private static void CreateAllTablesIfMissing(OleDbConnection conn, HashSet<string> existing)
        {
            // 1. LopHoc
            if (!existing.Contains("LopHoc"))
            {
                ExecuteDdl(conn, @"
                    CREATE TABLE LopHoc (
                        LopHocID COUNTER CONSTRAINT PK_LopHoc PRIMARY KEY,
                        MaLop TEXT(30) NOT NULL,
                        TenLop TEXT(100) NOT NULL,
                        KhoiLop LONG,
                        TenTruong TEXT(150),
                        GhiChu MEMO,
                        TrangThai YESNO
                    )");
                ExecuteDdl(conn, "CREATE UNIQUE INDEX UX_LopHoc_MaLop ON LopHoc (MaLop)");
            }

            // 2. MonHoc
            if (!existing.Contains("MonHoc"))
            {
                ExecuteDdl(conn, @"
                    CREATE TABLE MonHoc (
                        MonHocID COUNTER CONSTRAINT PK_MonHoc PRIMARY KEY,
                        TenMonHoc TEXT(100) NOT NULL,
                        MoTa MEMO,
                        TrangThai YESNO
                    )");
                ExecuteDdl(conn, "CREATE UNIQUE INDEX UX_MonHoc_Ten ON MonHoc (TenMonHoc)");
            }

            // 3. ChuDe
            if (!existing.Contains("ChuDe"))
            {
                ExecuteDdl(conn, @"
                    CREATE TABLE ChuDe (
                        ChuDeID COUNTER CONSTRAINT PK_ChuDe PRIMARY KEY,
                        MonHocID LONG,
                        TenChuDe TEXT(120) NOT NULL,
                        MoTa MEMO,
                        TrangThai YESNO,
                        CONSTRAINT FK_ChuDe_MonHoc FOREIGN KEY (MonHocID) REFERENCES MonHoc(MonHocID)
                    )");
            }

            // 4. CauHoi
            if (!existing.Contains("CauHoi"))
            {
                ExecuteDdl(conn, @"
                    CREATE TABLE CauHoi (
                        CauHoiID COUNTER CONSTRAINT PK_CauHoi PRIMARY KEY,
                        MaCauHoi TEXT(30) NOT NULL,
                        NoiDung MEMO NOT NULL,
                        MonHocID LONG,
                        ChuDeID LONG,
                        MucDo LONG NOT NULL,
                        LoaiCauHoi LONG NOT NULL,
                        GiaiThich MEMO,
                        ThoiGianTraLoi LONG,
                        DuongDanHinhAnh TEXT(255),
                        TrangThai YESNO,
                        NgayTao DATETIME,
                        NgayCapNhat DATETIME,
                        CONSTRAINT FK_CauHoi_MonHoc FOREIGN KEY (MonHocID) REFERENCES MonHoc(MonHocID),
                        CONSTRAINT FK_CauHoi_ChuDe FOREIGN KEY (ChuDeID) REFERENCES ChuDe(ChuDeID)
                    )");
                ExecuteDdl(conn, "CREATE UNIQUE INDEX UX_CauHoi_Ma ON CauHoi (MaCauHoi)");
                ExecuteDdl(conn, "CREATE INDEX IX_CauHoi_Filter ON CauHoi (MonHocID, ChuDeID, MucDo, TrangThai)");
            }

            // 5. CauHoi_LopHoc
            if (!existing.Contains("CauHoi_LopHoc"))
            {
                ExecuteDdl(conn, @"
                    CREATE TABLE CauHoi_LopHoc (
                        ID COUNTER CONSTRAINT PK_CauHoi_LopHoc PRIMARY KEY,
                        CauHoiID LONG NOT NULL,
                        LopHocID LONG NOT NULL,
                        CONSTRAINT FK_CHLH_CauHoi FOREIGN KEY (CauHoiID) REFERENCES CauHoi(CauHoiID) ON DELETE CASCADE,
                        CONSTRAINT FK_CHLH_LopHoc FOREIGN KEY (LopHocID) REFERENCES LopHoc(LopHocID)
                    )");
                ExecuteDdl(conn, "CREATE UNIQUE INDEX UX_CauHoi_LopHoc ON CauHoi_LopHoc (CauHoiID, LopHocID)");
            }

            // 6. DapAn
            if (!existing.Contains("DapAn"))
            {
                ExecuteDdl(conn, @"
                    CREATE TABLE DapAn (
                        DapAnID COUNTER CONSTRAINT PK_DapAn PRIMARY KEY,
                        CauHoiID LONG NOT NULL,
                        KyHieu TEXT(10) NOT NULL,
                        NoiDungDapAn MEMO NOT NULL,
                        LaDapAnDung YESNO NOT NULL,
                        ThuTu LONG NOT NULL,
                        CONSTRAINT FK_DapAn_CauHoi FOREIGN KEY (CauHoiID) REFERENCES CauHoi(CauHoiID) ON DELETE CASCADE
                    )");
                ExecuteDdl(conn, "CREATE INDEX IX_DapAn_CauHoi ON DapAn (CauHoiID, ThuTu)");
            }

            // 7. ThiSinh
            if (!existing.Contains("ThiSinh"))
            {
                ExecuteDdl(conn, @"
                    CREATE TABLE ThiSinh (
                        ThiSinhID COUNTER CONSTRAINT PK_ThiSinh PRIMARY KEY,
                        MaThiSinh TEXT(30) NOT NULL,
                        SoBaoDanh TEXT(30) NOT NULL,
                        HoTen TEXT(120) NOT NULL,
                        LopHocID LONG,
                        TenTruong TEXT(150),
                        GioiTinh TEXT(20),
                        DuongDanHinhAnh TEXT(255),
                        GhiChu MEMO,
                        TrangThai YESNO,
                        CONSTRAINT FK_ThiSinh_LopHoc FOREIGN KEY (LopHocID) REFERENCES LopHoc(LopHocID)
                    )");
                ExecuteDdl(conn, "CREATE UNIQUE INDEX UX_ThiSinh_Ma ON ThiSinh (MaThiSinh)");
                ExecuteDdl(conn, "CREATE INDEX IX_ThiSinh_SBD ON ThiSinh (SoBaoDanh)");
            }

            // 8. ChuongTrinh (Tạo trước DoiThi để DoiThi có thể tham chiếu ChuongTrinhID)
            if (!existing.Contains("ChuongTrinh"))
            {
                ExecuteDdl(conn, @"
                    CREATE TABLE ChuongTrinh (
                        ChuongTrinhID COUNTER CONSTRAINT PK_ChuongTrinh PRIMARY KEY,
                        MaChuongTrinh TEXT(30) NOT NULL,
                        TenChuongTrinh TEXT(200) NOT NULL,
                        NgayToChuc DATETIME NOT NULL,
                        DonViToChuc TEXT(200),
                        DiaDiem TEXT(200),
                        MoTa MEMO,
                        HinhThucThi TEXT(50),
                        PhuongThucThi LONG NOT NULL,
                        QuyTacTraLoiDoi LONG,
                        SoLuongCauHoi LONG,
                        ThoiGianMacDinh LONG,
                        SoLanCuuTroToiDa LONG,
                        QuyTacHetCauHoi LONG,
                        DaKhoaBoCauHoi YESNO,
                        TrangThai LONG,
                        GhiChu MEMO
                    )");
                ExecuteDdl(conn, "CREATE UNIQUE INDEX UX_ChuongTrinh_Ma ON ChuongTrinh (MaChuongTrinh)");
            }

            // 9. DoiThi
            if (!existing.Contains("DoiThi"))
            {
                ExecuteDdl(conn, @"
                    CREATE TABLE DoiThi (
                        DoiThiID COUNTER CONSTRAINT PK_DoiThi PRIMARY KEY,
                        MaDoi TEXT(30) NOT NULL,
                        TenDoi TEXT(120) NOT NULL,
                        ChuongTrinhID LONG,
                        TenLop TEXT(100),
                        TenTruong TEXT(150),
                        TenDoiTruong TEXT(120),
                        DuongDanHinhAnh TEXT(255),
                        GhiChu MEMO,
                        CONSTRAINT FK_DoiThi_ChuongTrinh FOREIGN KEY (ChuongTrinhID) REFERENCES ChuongTrinh(ChuongTrinhID)
                    )");
                ExecuteDdl(conn, "CREATE UNIQUE INDEX UX_DoiThi_Ma ON DoiThi (MaDoi)");
            }

            // 10. ThanhVienDoi
            if (!existing.Contains("ThanhVienDoi"))
            {
                ExecuteDdl(conn, @"
                    CREATE TABLE ThanhVienDoi (
                        ThanhVienDoiID COUNTER CONSTRAINT PK_ThanhVienDoi PRIMARY KEY,
                        DoiThiID LONG NOT NULL,
                        ThiSinhID LONG NOT NULL,
                        ChuongTrinhID LONG,
                        VaiTro TEXT(50),
                        TrangThai YESNO,
                        CONSTRAINT FK_TVD_DoiThi FOREIGN KEY (DoiThiID) REFERENCES DoiThi(DoiThiID) ON DELETE CASCADE,
                        CONSTRAINT FK_TVD_ThiSinh FOREIGN KEY (ThiSinhID) REFERENCES ThiSinh(ThiSinhID),
                        CONSTRAINT FK_TVD_ChuongTrinh FOREIGN KEY (ChuongTrinhID) REFERENCES ChuongTrinh(ChuongTrinhID)
                    )");
                ExecuteDdl(conn, "CREATE UNIQUE INDEX UX_TVD_Doi_TS ON ThanhVienDoi (DoiThiID, ThiSinhID)");
                ExecuteDdl(conn, "CREATE INDEX IX_TVD_CT_TS ON ThanhVienDoi (ChuongTrinhID, ThiSinhID)");
            }

            // 11. ChuongTrinh_CauHoi
            if (!existing.Contains("ChuongTrinh_CauHoi"))
            {
                ExecuteDdl(conn, @"
                    CREATE TABLE ChuongTrinh_CauHoi (
                        ID COUNTER CONSTRAINT PK_ChuongTrinh_CauHoi PRIMARY KEY,
                        ChuongTrinhID LONG NOT NULL,
                        CauHoiID LONG NOT NULL,
                        ThuTu LONG NOT NULL,
                        ThoiGianTraLoi LONG,
                        LaCauHoiPhu YESNO,
                        TrangThai LONG,
                        CONSTRAINT FK_CTCH_ChuongTrinh FOREIGN KEY (ChuongTrinhID) REFERENCES ChuongTrinh(ChuongTrinhID) ON DELETE CASCADE,
                        CONSTRAINT FK_CTCH_CauHoi FOREIGN KEY (CauHoiID) REFERENCES CauHoi(CauHoiID)
                    )");
                ExecuteDdl(conn, "CREATE UNIQUE INDEX UX_CTCH_Unique ON ChuongTrinh_CauHoi (ChuongTrinhID, CauHoiID)");
            }

            // 12. ChuongTrinh_ThiSinh
            if (!existing.Contains("ChuongTrinh_ThiSinh"))
            {
                ExecuteDdl(conn, @"
                    CREATE TABLE ChuongTrinh_ThiSinh (
                        ID COUNTER CONSTRAINT PK_ChuongTrinh_ThiSinh PRIMARY KEY,
                        ChuongTrinhID LONG NOT NULL,
                        ThiSinhID LONG,
                        DoiThiID LONG,
                        TrangThaiThi LONG NOT NULL,
                        SoCauDung LONG,
                        SoCauSai LONG,
                        SoCauKhongTraLoi LONG,
                        CauHoiBiLoai LONG,
                        ThoiDiemBiLoai DATETIME,
                        ThuHang LONG,
                        SoLanCuuTro LONG,
                        CONSTRAINT FK_CTTS_ChuongTrinh FOREIGN KEY (ChuongTrinhID) REFERENCES ChuongTrinh(ChuongTrinhID) ON DELETE CASCADE,
                        CONSTRAINT FK_CTTS_ThiSinh FOREIGN KEY (ThiSinhID) REFERENCES ThiSinh(ThiSinhID),
                        CONSTRAINT FK_CTTS_DoiThi FOREIGN KEY (DoiThiID) REFERENCES DoiThi(DoiThiID)
                    )");
                ExecuteDdl(conn, "CREATE INDEX IX_CTTS_Main ON ChuongTrinh_ThiSinh (ChuongTrinhID, TrangThaiThi)");
            }

            // 13. KetQua
            if (!existing.Contains("KetQua"))
            {
                ExecuteDdl(conn, @"
                    CREATE TABLE KetQua (
                        KetQuaID COUNTER CONSTRAINT PK_KetQua PRIMARY KEY,
                        ChuongTrinhID LONG NOT NULL,
                        CauHoiID LONG NOT NULL,
                        ThiSinhID LONG,
                        DoiThiID LONG,
                        DapAnDaChon TEXT(255),
                        KetQuaDungSai LONG NOT NULL,
                        ThoiGianTraLoi DOUBLE,
                        TrangThaiXuLy LONG,
                        Diem DOUBLE,
                        GhiChu MEMO,
                        CONSTRAINT FK_KQ_ChuongTrinh FOREIGN KEY (ChuongTrinhID) REFERENCES ChuongTrinh(ChuongTrinhID) ON DELETE CASCADE,
                        CONSTRAINT FK_KQ_CauHoi FOREIGN KEY (CauHoiID) REFERENCES CauHoi(CauHoiID)
                    )");
                ExecuteDdl(conn, "CREATE INDEX IX_KetQua_Lookup ON KetQua (ChuongTrinhID, CauHoiID, ThiSinhID, DoiThiID)");
            }

            // 14. LichSuLoai
            if (!existing.Contains("LichSuLoai"))
            {
                ExecuteDdl(conn, @"
                    CREATE TABLE LichSuLoai (
                        ID COUNTER CONSTRAINT PK_LichSuLoai PRIMARY KEY,
                        ChuongTrinhID LONG NOT NULL,
                        CauHoiID LONG NOT NULL,
                        ThiSinhID LONG,
                        DoiThiID LONG,
                        LyDoLoai TEXT(255),
                        ThoiGianLoai DATETIME NOT NULL,
                        NguoiThucHien TEXT(100),
                        GhiChu MEMO,
                        CONSTRAINT FK_LSL_ChuongTrinh FOREIGN KEY (ChuongTrinhID) REFERENCES ChuongTrinh(ChuongTrinhID) ON DELETE CASCADE
                    )");
            }

            // 15. LichSuCuuTro
            if (!existing.Contains("LichSuCuuTro"))
            {
                ExecuteDdl(conn, @"
                    CREATE TABLE LichSuCuuTro (
                        ID COUNTER CONSTRAINT PK_LichSuCuuTro PRIMARY KEY,
                        ChuongTrinhID LONG NOT NULL,
                        CauHoiIDHienTai LONG,
                        ThiSinhID LONG,
                        DoiThiID LONG,
                        LyDoCuuTro TEXT(255),
                        ThoiGianCuuTro DATETIME NOT NULL,
                        NguoiThucHien TEXT(100),
                        CONSTRAINT FK_LSCT_ChuongTrinh FOREIGN KEY (ChuongTrinhID) REFERENCES ChuongTrinh(ChuongTrinhID) ON DELETE CASCADE
                    )");
            }

            // 16. LichSuThaoTac
            if (!existing.Contains("LichSuThaoTac"))
            {
                ExecuteDdl(conn, @"
                    CREATE TABLE LichSuThaoTac (
                        ID COUNTER CONSTRAINT PK_LichSuThaoTac PRIMARY KEY,
                        ChuongTrinhID LONG NOT NULL,
                        LoaiThaoTac TEXT(50) NOT NULL,
                        DoiTuongID LONG,
                        DuLieuCu MEMO,
                        DuLieuMoi MEMO,
                        DaHoanTac YESNO,
                        ThoiGian DATETIME NOT NULL,
                        NguoiThucHien TEXT(100),
                        CONSTRAINT FK_LSTT_ChuongTrinh FOREIGN KEY (ChuongTrinhID) REFERENCES ChuongTrinh(ChuongTrinhID) ON DELETE CASCADE
                    )");
                ExecuteDdl(conn, "CREATE INDEX IX_LSTT_CT_Time ON LichSuThaoTac (ChuongTrinhID, ThoiGian)");
            }

            // 17. VinhDanh
            if (!existing.Contains("VinhDanh"))
            {
                ExecuteDdl(conn, @"
                    CREATE TABLE VinhDanh (
                        VinhDanhID COUNTER CONSTRAINT PK_VinhDanh PRIMARY KEY,
                        ChuongTrinhID LONG NOT NULL,
                        ThiSinhID LONG,
                        DoiThiID LONG,
                        HoTen TEXT(150) NOT NULL,
                        SoBaoDanh TEXT(30),
                        TenLop TEXT(100),
                        TenTruong TEXT(150),
                        TenDoi TEXT(120),
                        TenGiaiThuong TEXT(150) NOT NULL,
                        ThanhTich TEXT(255),
                        SoCauDung LONG,
                        SoCauVuotQua LONG,
                        DuongDanHinhAnh TEXT(255),
                        ThuTuTrinhChieu LONG,
                        NgayVinhDanh DATETIME,
                        GhiChu MEMO,
                        CONSTRAINT FK_VD_ChuongTrinh FOREIGN KEY (ChuongTrinhID) REFERENCES ChuongTrinh(ChuongTrinhID) ON DELETE CASCADE
                    )");
            }

            // 18. CauHinh
            if (!existing.Contains("CauHinh"))
            {
                ExecuteDdl(conn, @"
                    CREATE TABLE CauHinh (
                        CauHinhID COUNTER CONSTRAINT PK_CauHinh PRIMARY KEY,
                        TenCauHinh TEXT(100) NOT NULL,
                        GiaTri MEMO,
                        MoTa TEXT(255)
                    )");
                ExecuteDdl(conn, "CREATE UNIQUE INDEX UX_CauHinh_Ten ON CauHinh (TenCauHinh)");
            }

            // 19. TrangThaiChuongTrinh
            if (!existing.Contains("TrangThaiChuongTrinh"))
            {
                ExecuteDdl(conn, @"
                    CREATE TABLE TrangThaiChuongTrinh (
                        ID COUNTER CONSTRAINT PK_TrangThaiChuongTrinh PRIMARY KEY,
                        ChuongTrinhID LONG NOT NULL,
                        CauHoiHienTai LONG,
                        TrangThaiTrinhChieu LONG,
                        ThoiGianConLai DOUBLE,
                        TrangThaiDongHo LONG,
                        NgayCapNhat DATETIME,
                        CONSTRAINT FK_TTCT_ChuongTrinh FOREIGN KEY (ChuongTrinhID) REFERENCES ChuongTrinh(ChuongTrinhID) ON DELETE CASCADE
                    )");
                ExecuteDdl(conn, "CREATE UNIQUE INDEX UX_TTCT_CT ON TrangThaiChuongTrinh (ChuongTrinhID)");
            }
        }

        private static void EnsureDefaultConfigs(OleDbConnection conn)
        {
            var defaults = new Dictionary<string, (string Val, string Desc)>
            {
                ["DbSchemaVersion"] = (CurrentSchemaVersion.ToString(), "Phiên bản cấu trúc cơ sở dữ liệu Access"),
                ["MucDo_1_Ten"] = ("Nhận biết", "Tên hiển thị mức độ 1"),
                ["MucDo_1_Mau"] = ("#16A34A", "Màu hiển thị mức độ 1"),
                ["MucDo_2_Ten"] = ("Thông hiểu", "Tên hiển thị mức độ 2"),
                ["MucDo_2_Mau"] = ("#1E5BB8", "Màu hiển thị mức độ 2"),
                ["MucDo_3_Ten"] = ("Vận dụng", "Tên hiển thị mức độ 3"),
                ["MucDo_3_Mau"] = ("#D97706", "Màu hiển thị mức độ 3"),
                ["MucDo_4_Ten"] = ("Vận dụng cao", "Tên hiển thị mức độ 4"),
                ["MucDo_4_Mau"] = ("#B91C1C", "Màu hiển thị mức độ 4"),
                ["ThoiGianTraLoiMacDinh"] = ("15", "Thời gian đếm ngược mặc định (giây)"),
                ["AmThanhBat"] = ("True", "Bật âm thanh cảnh báo và hiệu ứng gameshow"),
                ["TuDongSaoLuu"] = ("True", "Tự động sao lưu CSDL khi khởi động/đóng chương trình"),
                ["TenDonViMacDinh"] = ("BAN TỔ CHỨC CHƯƠNG TRÌNH RUNG CHUÔNG VÀNG", "Tên đơn vị hiển thị màn hình chờ"),
                ["ManHinhTrinhChieuIndex"] = ("-1", "Chỉ số màn hình phụ trình chiếu (-1 = tự động chọn)")
            };

            foreach (var kv in defaults)
            {
                using var checkCmd = new OleDbCommand("SELECT COUNT(*) FROM CauHinh WHERE TenCauHinh = ?", conn);
                DatabaseHelper.AddParameters(checkCmd, kv.Key);
                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count == 0)
                {
                    using var insertCmd = new OleDbCommand(
                        "INSERT INTO CauHinh (TenCauHinh, GiaTri, MoTa) VALUES (?, ?, ?)", conn);
                    DatabaseHelper.AddParameters(insertCmd, kv.Key, kv.Value.Val, kv.Value.Desc);
                    insertCmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Cơ chế nâng cấp schema an toàn theo phiên bản, không bao giờ xóa dữ liệu người dùng.
        /// </summary>
        private static void RunSafeSchemaMigrations(OleDbConnection conn)
        {
            using var cmd = new OleDbCommand("SELECT GiaTri FROM CauHinh WHERE TenCauHinh = ?", conn);
            DatabaseHelper.AddParameters(cmd, "DbSchemaVersion");
            object? raw = cmd.ExecuteScalar();
            int version = 1;
            if (raw != null && int.TryParse(raw.ToString(), out int parsed))
            {
                version = parsed;
            }

            // Các bước nâng cấp trong tương lai (if version < 2 ...) sẽ được bổ sung tại đây
            if (version < CurrentSchemaVersion)
            {
                using var updateCmd = new OleDbCommand("UPDATE CauHinh SET GiaTri = ? WHERE TenCauHinh = ?", conn);
                DatabaseHelper.AddParameters(updateCmd, CurrentSchemaVersion.ToString(), "DbSchemaVersion");
                updateCmd.ExecuteNonQuery();
            }
        }
    }
}
