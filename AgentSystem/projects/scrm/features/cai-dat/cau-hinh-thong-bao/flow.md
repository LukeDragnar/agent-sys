# Luồng — Cấu hình thông báo

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị (DonViId).
- Có quyền vào màn cấu hình: HT-AD / DV-AD-TONG / HT-CSKH (cấu hình gửi); DV-AD-TONG (mẫu thông báo). Riêng tab **Trạng thái lịch hẹn** chỉ cần đăng nhập (quyền lỏng hơn — xem dev.md).
- Đã có **mẫu thông báo** trong kho (NotiTemplate) để chọn khi cấu hình gửi.
- Với kiểu **Tác nghiệp**: đơn vị đã khai báo thiết lập tác nghiệp CSKH (danh sách tác nghiệp / kết quả) — hệ thống gọi sang service tác nghiệp để dựng danh sách sự kiện.

## Luồng dùng (admin cấu hình gửi)
1. Admin mở **Thông báo → Cấu hình gửi thông báo web**.
2. Chọn **Kiểu thông báo** (Hành động=0, Tác nghiệp=1, Trạng thái đơn hàng=2, Trạng thái lịch hẹn=3). Hệ thống tải cấu hình hiện có của đơn vị theo kiểu; với kiểu Tác nghiệp còn merge danh sách tác nghiệp từ service ngoài để hiển thị đủ sự kiện.
3. Với mỗi dòng sự kiện: tích **Sử dụng**, chọn **Mẫu tin nhắn** (webPatternCode), tích các ô đối tượng nhận (sale / trưởng nhóm sale / trưởng phòng sale / MKT / trưởng nhóm MKT / trưởng phòng MKT / thu ngân / admin chi nhánh / admin tổng), chọn thêm **Gửi user** nếu cần.
4. (Kiểu Tác nghiệp) chọn mẫu gửi sau khi chuyển tác nghiệp tiếp (webPatternCodeNext) + số phút gửi sau / gửi trước.
5. Bấm **Lưu** → upsert toàn bộ list cấu hình của đơn vị theo kiểu đó.
6. Muốn làm lại: bấm **Khởi tạo lại** → xóa toàn bộ cấu hình của kiểu đang chọn.

## Luồng dùng (admin quản lý mẫu)
1. Admin mở **Thông báo → Mẫu thông báo**.
2. Lọc theo tên / nhóm / kiểu sử dụng → bấm Tìm.
3. Bấm **Sửa** một mẫu → chỉnh nội dung/tiêu đề → Lưu. Hoặc **Xóa** mẫu (theo Mã). Có thể **Xuất Excel**.

## Luồng hệ thống (lúc nghiệp vụ chạy — không do người dùng bấm)
1. Một sự kiện xảy ra: nhân viên hoàn tất 1 tác nghiệp CSKH / đơn đổi trạng thái giao hàng / lịch hẹn đổi trạng thái.
2. Service nghiệp vụ gọi cấu hình tương ứng (theo IdTacNghiep+IdKetQua, hoặc mã trạng thái giao hàng, hoặc trạng thái lịch hẹn) để biết: gửi mẫu nào + cho ai.
3. Hệ thống dựng nội dung từ mẫu rồi phát thông báo cho các đối tượng đã cấu hình.

## Case biên
- **Dòng sự kiện không tích Sử dụng** → không gửi, dù đã chọn mẫu và người nhận.
- **Chọn người nhận nhưng quên chọn mẫu** → có người nhận nhưng nội dung rỗng (mẫu trống) → thông báo có thể trống.
- **Bấm Khởi tạo lại** → xóa **toàn bộ** cấu hình của kiểu đang chọn (không idempotent-safe — bấm nhầm là mất). Phải tích chọn và Lưu lại từ đầu.
- **Xóa mẫu đang được cấu hình tham chiếu** → cấu hình trỏ tới mã mẫu đã xóa → "mồ côi" → khi sự kiện chạy, nội dung rỗng.
- **Tab Trạng thái lịch hẹn**: Lưu được chỉ với đăng nhập (quyền lỏng hơn 3 tab kia) — rủi ro người không đủ quyền vẫn lưu được tab này.
- **Thêm field cấu hình mới ở phía code mà quên đánh dấu cập nhật** → field đó có thể không được lưu (lỗi âm thầm — xem dev.md).

## Ví dụ cụ thể có số
Spa B muốn: khi đơn chuyển sang trạng thái "Giao thành công" thì báo cho Sale phụ trách và Admin tổng.
1. Admin vào Cấu hình gửi → chọn Kiểu = **Trạng thái đơn hàng**.
2. Tại dòng "Giao thành công": tích **Sử dụng**, chọn mẫu `ORDER_DELIVERED`, tích **Gửi sale** + **Gửi admin tổng** → Lưu.
3. Hôm sau có 12 đơn chuyển "Giao thành công" → mỗi đơn sinh 1 thông báo gửi tới Sale phụ trách đơn đó + Admin tổng (tổng ~24 thông báo, tùy số người admin tổng).
4. Admin đổi ý, muốn làm lại tab này → bấm **Khởi tạo lại** → toàn bộ cấu hình tab Trạng thái đơn hàng bị xóa, phải tích lại từ đầu.

## Hậu điều kiện
- Lưu cấu hình gửi → ghi vào DB (NotificationConfig theo đơn vị + kiểu). Không tự gửi thông báo ngay.
- Khởi tạo lại → xóa hết bản ghi cấu hình của kiểu đó cho đơn vị.
- Sửa/Xóa mẫu → cập nhật/xóa NotiTemplate; cấu hình gửi trỏ tới mẫu vừa xóa sẽ mồ côi.
