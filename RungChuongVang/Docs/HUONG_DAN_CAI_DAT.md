# HƯỚNG DẪN CÀI ĐẶT & BIÊN DỊCH PHẦN MỀM RUNG CHUÔNG VÀNG

## 1. Yêu cầu hệ thống
- **Hệ điều hành:** Windows 10 hoặc Windows 11 (phiên bản **64-bit**).
- **Trình điều khiển CSDL Access (Bắt buộc trên cả máy phát triển và máy sử dụng):**
  - Cài đặt **Microsoft Access Database Engine 2016 Redistributable (64-bit / x64)** (file `accessdatabaseengine_X64.exe`).
  - *Lưu ý:* Phần mềm được biên dịch ở chế độ `x64`. Nếu máy đã cài Microsoft Office 32-bit, hãy chạy cài đặt `accessdatabaseengine_X64.exe /quiet` hoặc sử dụng Office 64-bit để có provider `Microsoft.ACE.OLEDB.16.0` (64-bit).
- **Môi trường phát triển / biên dịch (nếu build từ mã nguồn):**
  - `.NET 8.0 SDK (x64)` hoặc **Visual Studio 2022** (với workload *.NET desktop development*).
  - Máy kết nối Internet lần đầu để `dotnet restore` tải 2 gói NuGet: `ClosedXML` (`0.104.2`) và `System.Data.OleDb` (`8.0.1`).

---

## 2. Cách biên dịch & chạy từ mã nguồn (Visual Studio hoặc CLI)

### Cách A: Mở bằng Visual Studio 2022
1. Mở tệp `RungChuongVang\RungChuongVang.sln`.
2. Trên thanh công cụ, chọn cấu hình **`Release`** (hoặc `Debug`) và nền tảng **`x64`**.
3. Nhấn **`Build -> Build Solution`** (`Ctrl + Shift + B`), sau đó nhấn `F5` để chạy thử.

### Cách B: Dùng dòng lệnh / Script đóng gói sẵn
Mở **Command Prompt** hoặc **PowerShell** tại thư mục `RungChuongVang\`:
- Chạy file **`build-and-publish.bat`** (hoặc `powershell -ExecutionPolicy Bypass -File build-and-publish.ps1`).
- Script sẽ tự động:
  1. Khôi phục NuGet packages.
  2. Biên dịch bản `Release x64`.
  3. Xuất gói chạy độc lập **offline** (`win-x64 --self-contained true`) vào thư mục `publish\RungChuongVang_Win64\`.
  4. Tạo đầy đủ cây thư mục `Data\` và sao chép thư mục `Docs\`.

---

## 3. Tạo bộ cài đặt Windows `.exe` (Tùy chọn với Inno Setup)
1. Chạy `build-and-publish.bat` để tạo thư mục `publish\RungChuongVang_Win64\`.
2. Cài đặt công cụ miễn phí **Inno Setup 6**.
3. Mở file `Installer\RungChuongVang_Setup.iss` bằng Inno Setup Compiler và nhấn **Compile**.
4. File cài đặt hoàn chỉnh `Setup_RungChuongVang_x64.exe` sẽ nằm trong thư mục `publish\Installer\`.

---

## 4. Lần khởi chạy đầu tiên trên máy tổ chức sự kiện
1. Mở `RungChuongVang.exe` (ưu tiên đặt trong thư mục có quyền ghi như `D:\RungChuongVang\` nếu chạy bản portable, hoặc cài đặt qua Inno Setup đã tự cấp quyền ghi cho thư mục `Data\`).
2. Hệ thống tự động kiểm tra trình điều khiển `Microsoft.ACE.OLEDB.16.0` / `12.0`.
3. Nếu chưa có tệp `Data\RungChuongVang.accdb`, phần mềm sẽ **tự động tạo mới CSDL Access**, khởi tạo toàn bộ **19 bảng dữ liệu, khóa ngoại, chỉ mục và thông số cấu hình mặc định**.
4. Nếu có lỗi phát sinh, kiểm tra tệp nhật ký tại `Data\Logs\error_yyyyMMdd.log`.
