# PHẦN MỀM RUNG CHUÔNG VÀNG (C# WinForms .NET 8 + Microsoft Access)

Ứng dụng desktop chuyên nghiệp phục vụ tổ chức, quản lý, điều hành và trình chiếu cuộc thi **Rung Chuông Vàng** theo hình thức thi **Cá nhân** và **Theo đội** trên hệ điều hành Windows 10/11 (64-bit), hoạt động **100% offline**, hỗ trợ đầy đủ tiếng Việt Unicode và chế độ **2 màn hình độc lập** (Màn hình MC + Màn hình Máy chiếu/LED).

## Kiến trúc công nghệ
- **Ngôn ngữ & Framework:** C# 12, Windows Forms trên `.NET 8.0 (net8.0-windows)`, nền tảng `x64`.
- **Cơ sở dữ liệu:** Microsoft Access (`.accdb`) kết nối qua `System.Data.OleDb` (`Microsoft.ACE.OLEDB.16.0` / `12.0`). Tự động khởi tạo file `Data\RungChuongVang.accdb` và toàn bộ **19 bảng + chỉ mục + cấu hình mặc định** ngay lần chạy đầu tiên bằng `ADOX.Catalog`.
- **Nhập / Xuất Excel:** `ClosedXML` hỗ trợ file `.xlsx` không cần cài đặt Microsoft Excel trên máy chạy chương trình.
- **Lưu trữ tập tin:** Hình ảnh câu hỏi, thí sinh, đội thi, ảnh cắt chân dung vinh danh và file sao lưu ZIP được lưu cục bộ trong thư mục `Data\`.

## Cấu trúc thư mục chính
```text
RungChuongVang\
├── RungChuongVang.sln                 # Solution Visual Studio 2022
├── RungChuongVang.csproj              # Cấu hình dự án .NET 8 WinForms x64
├── Program.cs                         # Điểm khởi động, kiểm tra OleDb, auto-backup
├── Enums\                             # Kiểu liệt kê luật thi, trạng thái, loại câu hỏi
├── Models\                            # Các lớp thực thể ánh xạ 19 bảng Access
├── Helpers\                           # ThemeColors, FileHelper, ImageHelper, SoundHelper, ValidationHelper
├── DataAccess\                        # DatabaseHelper, DatabaseInitializer & các Repository
├── Services\                          # ExamService, TimerService, ScoreService, ExcelService, ReportService, BackupService...
├── Controls\                          # Custom controls & PresentationStageControl (GDI+)
├── Forms\                             # 13 phân hệ giao diện & các hộp thoại chuyên dụng
├── Properties\PublishProfiles\        # Cấu hình Publish x64 (Self-contained & Framework-dependent)
├── Installer\                         # Kịch bản Inno Setup tạo bộ cài đặt .exe
├── build-and-publish.bat / .ps1       # Script đóng gói phát hành nhanh
└── Docs\                              # Bộ tài liệu hướng dẫn tiếng Việt
```

## Tài liệu hướng dẫn chi tiết
- [Hướng dẫn cài đặt & biên dịch](Docs/HUONG_DAN_CAI_DAT.md)
- [Hướng dẫn sử dụng toàn bộ phân hệ](Docs/HUONG_DAN_SU_DUNG.md)
- [Hướng dẫn vận hành 2 màn hình (MC & Máy chiếu)](Docs/HUONG_DAN_HAI_MAN_HINH.md)
- [Hướng dẫn sao lưu & phục hồi dữ liệu](Docs/HUONG_DAN_SAO_LUU_PHUC_HOI.md)
