# HƯỚNG DẪN SỬ DỤNG PHẦN MỀM RUNG CHUÔNG VÀNG

## Quy trình chuẩn tổ chức một cuộc thi

### Bước 1: Thiết lập Danh mục nền tảng
1. **Quản lý lớp học:** Khai báo các lớp tham dự (ví dụ: `5A`, `5B`, `6A1`, khối lớp, trường).
2. **Quản lý môn học & chủ đề:** Khai báo môn học (Toán, Tiếng Việt, Lịch sử, Tin học...) và các chủ đề trực thuộc.
3. **Cài đặt (tùy chọn):** Điều chỉnh tên đơn vị tổ chức mặc định, thời gian đếm ngược mặc định (giây), âm thanh, và tùy chỉnh tên/màu 4 mức độ khó (`Nhận biết`, `Thông hiểu`, `Vận dụng`, `Vận dụng cao`).

---

### Bước 2: Xây dựng Ngân hàng câu hỏi
1. Vào **Quản lý câu hỏi**:
   - Thêm câu hỏi thủ công với 3 dạng: **Trắc nghiệm một đáp án**, **Trắc nghiệm nhiều đáp án**, **Tự luận / Trả lời ngắn**.
   - Gán 1 hoặc nhiều lớp học phù hợp cho từng câu hỏi, chọn Môn học, Chủ đề, Mức độ khó, Giải thích và đính kèm Hình ảnh minh họa (ảnh được sao chép an toàn vào `Data\Images\Questions\`).
   - Có thể **Nhân bản** câu hỏi tương tự hoặc **Ngừng sử dụng / Khôi phục** câu hỏi (xóa mềm bảo vệ lịch sử thi).
2. **Nhập câu hỏi hàng loạt từ Excel:**
   - Nhấn **Nhập Excel** -> chọn **Tải file mẫu Excel** để lấy mẫu chuẩn 14 cột.
   - Chọn file `.xlsx` đã điền -> hệ thống hiển thị bản xem trước và tô đỏ các dòng lỗi chi tiết (thiếu lớp, sai mức độ, sai ký hiệu đáp án đúng, trùng nội dung...).
   - Chọn xử lý trùng lặp (**Bỏ qua** hoặc **Cập nhật đè**) và nhấn **Thực hiện nhập**.

---

### Bước 3: Nhập Thí sinh & Đội thi
1. Vào **Quản lý thí sinh**:
   - Nhập từng thí sinh hoặc tải mẫu Excel -> nhập hàng loạt danh sách thí sinh.
   - Có thể gắn ảnh chân dung thí sinh.
2. Vào **Quản lý đội thi** (nếu tổ chức thi đồng đội):
   - Tạo hoặc nhập danh sách đội từ file mẫu Excel (`Mẫu Excel` -> `Nhập Excel`).
   - Chọn đội ở cột trái -> chọn thí sinh ở cột phải để **Thêm vào đội**, **Đặt làm Đội trưởng**, **Xóa khỏi đội** hoặc **Chuyển đội**.

---

### Bước 4: Tạo Chương trình & Trộn bộ câu hỏi thi
1. Vào **Quản lý chương trình** -> **Tạo chương trình mới**:
   - Chọn phương thức thi: **Thi cá nhân** hoặc **Thi theo đội**.
   - Nếu thi theo đội, chọn 1 trong 3 quy tắc trả lời:
     1. *Đội cử 1 đại diện trả lời.*
     2. *Thống nhất 1 đáp án chung toàn đội.*
     3. *Tất cả thành viên cùng trả lời — mọi thành viên đúng đội mới được tính đúng.*
   - Chọn danh sách thí sinh hoặc đội thi tham gia (hệ thống tự chặn trường hợp 1 thí sinh thuộc 2 đội trong cùng chương trình).
2. Vào **Tạo bộ câu hỏi thi** (hoặc nút từ danh sách chương trình):
   - Thiết lập số lượng câu hỏi, chọn lọc Lớp / Môn / Chủ đề / Loại câu hỏi.
   - Điều chỉnh tỉ lệ `%` cho 4 mức độ khó (**tổng 4 mức phải đúng 100%**).
   - Nhấn **Trộn ngẫu nhiên** -> xem trước, đổi thứ tự lên/xuống, thay thế câu lẻ hoặc thêm/bớt câu hỏi, sau đó nhấn **Lưu bộ câu hỏi** hoặc **Khóa bộ câu hỏi** để sẵn sàng lên sóng.

---

### Bước 5: Điều hành trực tiếp & Luật Rung Chuông Vàng
Vào **Điều khiển trình chiếu** (hoặc mở từ chương trình đang chuẩn bị):
- **Không bao giờ tự loại nhầm:** Khi hết giờ đếm ngược, phần mềm phát tín hiệu hết giờ nhưng **chỉ loại thí sinh/đội khi MC/Giám khảo xác nhận kết quả ở Khu vực D**.
- **Luồng thao tác cho mỗi câu hỏi:**
  1. `1. Bắt đầu chương trình` -> `2. Hiển thị câu hỏi` -> `Hiện đáp án A,B,C,D`.
  2. `3. Bắt đầu đếm giờ` (hỗ trợ `4. Tạm dừng`, `5. Tiếp tục`, `Đặt lại giờ`).
  3. `6. Công bố đáp án`: Chiếu đáp án đúng + lời giải thích lên màn hình sân khấu.
  4. Kiểm tra và tích chọn thí sinh/đội ở **Khu vực D** -> bấm **`✓ Xác nhận ĐÚNG`**, **`✕ Xác nhận SAI (Loại)`** hoặc **`Không trả lời`**.
  5. Nếu có sai sót khi bấm xác nhận: nhấn **`Hoàn tác`** để phục hồi đúng trạng thái trước đó và ghi nhận `HOAN_TAC` vào lịch sử.
  6. Khi nhiều thí sinh/đội bị loại, bấm **`13. Cứu trợ`** (chọn số lượng hoặc từng đối tượng + nhập lý do) để đưa trở lại sàn thi đấu mà vẫn giữ nguyên dấu vết từng bị loại ở câu nào.
  7. Nếu kết thúc bộ câu hỏi chính mà vẫn còn nhiều người trên sàn, bấm **`Thêm câu hỏi phụ`** để chọn câu hỏi phụ chưa từng dùng.
  8. Khi còn 1 người/đội chiến thắng hoặc kết thúc vòng thi, bấm **`16. Kết thúc chương trình`**.

---

### Bước 6: Vinh danh, Xem kết quả & Báo cáo Excel
1. Nhấn **`17. VINH DANH NGƯỜI CHIẾN THẮNG`** (hoặc vào mục **Vinh danh** trên thanh menu):
   - Nhấn **Lấy tự động từ kết quả thi** hoặc tự nhập bổ sung các hạng mục giải thưởng (Quán quân, Á quân, Đội xuất sắc...).
   - Bấm **Chọn & Cắt ảnh...** để phóng to, kéo chỉnh vùng chân dung chuẩn sân khấu.
   - Bấm **Xem trước** để kiểm tra bố cục cục bộ trước khi bấm **XÁC NHẬN & TRÌNH CHIẾU LÊN MÁY CHIẾU**.
2. Vào **Kết quả thi** để xem bảng xếp hạng, chi tiết từng câu trả lời, hoặc **Điều chỉnh kết quả BTC** (yêu cầu nhập lý do).
3. Vào **Báo cáo** để xem và xuất 4 nhóm thống kê (Câu hỏi, Chương trình, Thí sinh, Đội thi) ra Excel `.xlsx`.
