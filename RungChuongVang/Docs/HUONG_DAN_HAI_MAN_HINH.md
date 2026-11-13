# HƯỚNG DẪN VẬN HÀNH HAI MÀN HÌNH (MC & MÁY CHIẾU / LED)

Phần mềm hỗ trợ hai màn hình hoạt động hoàn toàn độc lập:
- **Màn hình 1 (Laptop / PC điều hành):** Hiển thị `FrmDieuKhien` với khung xem trước, bảng thí sinh, 17+ nút bấm và nhật ký điều hành.
- **Màn hình 2 (Máy chiếu / Màn hình LED hội trường):** Hiển thị `FrmTrinhChieu` toàn màn hình không viền (`FormBorderStyle = None`, `TopMost`), đồng bộ theo thời gian thực qua `ExamStateManager`.

---

## 1. Cấu hình Windows trước khi mở phần mềm
1. Kết nối dây HDMI / VGA / Wireless Display từ máy tính tới máy chiếu hoặc bộ xử lý màn hình LED.
2. Nhấn tổ hợp phím **`Windows + P`** trên bàn phím.
3. Chọn chế độ **`Extend` (Mở rộng)** — **KHÔNG** chọn `Duplicate` (Nhân bản), vì chế độ Duplicate sẽ chiếu luôn bảng điều khiển MC lên sân khấu.

---

## 2. Mở và điều chỉnh màn hình trình chiếu trong phần mềm
1. Tại màn hình **Điều khiển trình chiếu**, thanh công cụ phía trên tự động liệt kê các màn hình hiện có (`Screen.AllScreens`) kèm độ phân giải và đánh dấu `(Chính)` / `(Phụ - Máy chiếu)`.
2. Chọn màn hình phụ trong danh sách **Màn hình chiếu** (phần mềm mặc định tự chọn màn hình không phải `Primary`).
3. Bỏ tích **`Chạy thử 1 màn hình`** và nhấn **`Mở màn hình chiếu`**.
4. Nếu màn hình trình chiếu bị hiển thị ngược bên (hiện lên laptop thay vì máy chiếu), nhấn **`Đảo 2 màn hình`** để hoán đổi vị trí ngay lập tức mà không làm gián đoạn câu hỏi đang thi.

---

## 3. Chế độ chạy thử khi chỉ có 1 màn hình (Chuẩn bị trước sự kiện)
- Khi chuẩn bị kịch bản tại nhà hoặc chỉ dùng 1 laptop không gắn máy chiếu:
  - Hệ thống tự động đánh dấu **`Chạy thử 1 màn hình`**.
  - Khi nhấn **`Mở màn hình chiếu`**, cửa sổ `FrmTrinhChieu` mở ở dạng cửa sổ thu gọn (`1120x660`) để bạn kiểm tra trực tiếp hiển thị câu hỏi, ảnh minh họa, đếm ngược và màn hình vinh danh ngay trên cùng một màn hình.

---

## 4. Phím tắt & thao tác trên màn hình trình chiếu
- Nhấn phím **`F11`** khi cửa sổ trình chiếu đang được chọn để chuyển đổi nhanh giữa **Toàn màn hình** và **Cửa sổ**.
- Nhấn phím **`ESC`** để thoát chế độ toàn màn hình nếu cần kiểm tra tác vụ Windows khẩn cấp.
- Mọi thay đổi từ bảng điều khiển MC (`Hiển thị câu hỏi`, `Đếm giờ`, `Công bố đáp án`, `Cứu trợ`, `Vinh danh`) đều được đồng bộ tức thì và an toàn luồng UI (`Invoke/BeginInvoke`).
