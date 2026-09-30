> Draft — cần bổ sung từ ticket thật.

# FAQ — Mẫu & quản lý thông báo

**Q: "Mẫu thông báo" khác "Thông báo" như thế nào?**
A: **Mẫu thông báo** là khuôn nội dung/tiêu đề (định danh bằng Mã) để hệ thống tự sinh thông báo cho từng sự kiện. **Thông báo (đẩy)** là tin tức/thông báo cụ thể admin soạn và gửi tới người dùng. Mẫu là "khuôn", thông báo là "bản gửi đi".

**Q: Vì sao mẫu thông báo tra theo Mã chứ không theo tên?**
A: Mỗi mẫu được định danh bằng **Mã (Code)** cố định để cấu hình gửi và các sự kiện trỏ tới đúng mẫu. Khi xem chi tiết/xóa, hệ thống làm việc theo Code.

**Q: Tôi xóa một mẫu thì có sao không?**
A: Cần kiểm tra trước: nếu cấu hình gửi thông báo còn trỏ tới Mã của mẫu đó, sau khi xóa thông báo sẽ "mồ côi" (rỗng nội dung). Hãy chắc không còn cấu hình nào dùng mẫu trước khi xóa.

**Q: Tôi muốn thêm một mẫu thông báo mới?**
A: Trên giao diện admin có nút "Thêm mới mẫu thông báo", nhưng đường tạo mẫu ở cấp hệ thống cần dev/quản trị xác nhận (một số mẫu được seed sẵn hoặc tạo qua kênh quản trị khác). Nếu không tạo được, liên hệ kỹ thuật.

**Q: Vì sao thông báo đã gửi không sửa/xóa được?**
A: Thông báo đã gửi (`Đã gửi`) bị khóa để bảo toàn lịch sử — nút Sửa/Xóa chỉ hiện với thông báo chưa gửi. Muốn thay đổi, tạo thông báo mới.

**Q: "Gửi sau (giây)" trong mẫu nghĩa là gì?**
A: Là độ trễ gửi của mẫu — khi sự kiện kích hoạt, hệ thống đợi số giây cấu hình rồi mới gửi thông báo theo mẫu đó.

**Q: "Danh sách gửi" (vai trò nhận) để làm gì?**
A: Xác định những vai trò (role) sẽ nhận thông báo sinh từ mẫu. Hệ thống dựa vào danh sách này để đẩy thông báo tới đúng nhóm người dùng.

**Q: Khách hàng cuối có chỉnh được mẫu/thông báo không?**
A: Không. Đây là phần quản trị nội bộ cho admin hệ thống; khách chỉ nhận thông báo.
