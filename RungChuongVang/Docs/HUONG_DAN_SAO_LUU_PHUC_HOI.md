# HƯỚNG DẪN SAO LƯU & PHỤC HỒI DỮ LIỆU

Toàn bộ dữ liệu cuộc thi của phần mềm nằm trong:
- Tệp cơ sở dữ liệu Access: `Data\RungChuongVang.accdb`
- Thư mục hình ảnh đính kèm: `Data\Images\` (`Questions`, `Contestants`, `Teams`, `Winners`, `Logo`)

---

## 1. Sao lưu tự động (Auto-Backup)
- Mặc định tính năng **Tự động sao lưu mỗi ngày khi khởi động phần mềm** được bật trong mục **Cài đặt**.
- Mỗi ngày lần đầu khởi động, phần mềm tự tạo một gói nén `AutoBackup_yyyyMMdd_HHmmss.zip` trong `Data\Backup\` và tự động giữ lại **10 bản tự động gần nhất** để tối ưu dung lượng ổ đĩa.

---

## 2. Sao lưu thủ công trước và sau mỗi cuộc thi
1. Vào phân hệ **Cài đặt** ở thanh menu bên trái.
2. Kiểm tra hoặc thay đổi **Thư mục lưu bản sao lưu** (khuyến nghị sao lưu thêm 1 bản ra USB hoặc ổ đĩa khác).
3. Nhấn **`Sao lưu ngay`**.
4. Phần mềm đóng gói toàn bộ file `.accdb`, toàn bộ hình ảnh trong `Data\Images\` và tệp kiểm chứng `backup_manifest.json` thành một file `RCV_Backup_yyyyMMdd_HHmmss.zip`.

---

## 3. Kiểm tra & Phục hồi dữ liệu an toàn
1. Tại bảng danh sách bản sao lưu trong mục **Cài đặt**, chọn một file `.zip` cần phục hồi (hoặc nhấn **`Chọn file .zip...`** nếu lấy từ USB / máy khác).
2. Có thể nhấn **`Kiểm tra ZIP`** trước để xác minh tệp còn nguyên vẹn, xem ngày tạo, phiên bản schema Access, dung lượng và số ảnh đính kèm.
3. Nhấn **`Phục hồi`** -> đọc kỹ thông tin kiểm chứng và xác nhận **`Yes`**.
4. **Cơ chế bảo vệ chống mất dữ liệu:** Trước khi giải nén ghi đè, phần mềm **luôn tự động tạo một bản sao lưu khẩn cấp `AutoPreRestore_yyyyMMdd_HHmmss.zip`** từ dữ liệu hiện tại, đóng kết nối Access đang mở, phục hồi dữ liệu và hình ảnh, rồi chạy kiểm tra nâng cấp schema an toàn.
