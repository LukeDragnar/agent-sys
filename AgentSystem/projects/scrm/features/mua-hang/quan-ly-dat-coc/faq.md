> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Quản lý đặt cọc

**Q: Tôi không thấy phiếu cọc cũ sau khi đã thay sản phẩm?**
A: Đúng. Khi bạn thay sản phẩm đã cọc, phiếu cũ được đánh dấu "bị thay thế" và **luôn bị ẩn** khỏi danh sách/tìm kiếm/xuất Excel; thay vào đó là phiếu thay thế mới. Đây là thiết kế, không phải mất dữ liệu — bạn có thể xem trong lịch sử.

**Q: Vì sao tôi không thay sản phẩm / không hoàn cọc cho phiếu này được?**
A: Hai hành động này có điều kiện. **Thay sản phẩm** chỉ khi phiếu **đã thanh toán**, chưa vào đợt trả cọc và chưa chọn đợi đợt sau. **Hoàn cọc** chỉ khi phiếu **đã thanh toán**, chưa hoàn và chưa giao hàng. Nếu phiếu đã giao thì khóa cả hai.

**Q: Hoàn cọc cho khách làm thế nào?**
A: Hai bước: (1) bấm **Hoàn cọc** để tạo yêu cầu, nhập thông tin ngân hàng và lý do; (2) sau khi đã thực sự chi tiền, bấm **Xác nhận hoàn cọc**. Khi xác nhận, hệ thống cập nhật cho tất cả phiếu cùng mã cọc và gửi SMS cho khách.

**Q: Tiền cọc trung bình hiển thị bằng 0 dù khách đã nộp?**
A: Đây là lỗi tính toán đã được ghi nhận (giá trị trung bình hiển thị sai về 0). Số tiền đã nộp của từng phiếu vẫn đúng. Đang chờ xử lý ở bản cập nhật sau.

**Q: "Đợt trả cọc" để làm gì?**
A: Là cách gom nhiều phiếu cọc cùng sản phẩm/điều kiện thành một tệp để chuẩn bị **trả cọc và gửi SMS/Zalo hàng loạt** cho nhóm khách đó. Bạn tạo đợt, gán danh sách phiếu đủ điều kiện, gắn mẫu tin nhắn, rồi khóa đợt khi chốt.

**Q: Tôi tạo đợt rồi mà không sửa/gán thêm được nữa?**
A: Có thể đợt đã bị **khóa** (Khóa đợt). Sau khi khóa, mọi thao tác sửa/gán/xóa/cấu hình SMS đều bị chặn để giữ nguyên đợt đã chốt. Nếu cần thay đổi, phải tạo đợt mới.

**Q: Tôi xóa phiếu khỏi đợt trả cọc nhưng vẫn còn vài phiếu dính lại?**
A: Mỗi lần thao tác chỉ xử lý tối đa 1.000 phiếu. Nếu đợt có hơn 1.000 phiếu, hãy thực hiện lại để gỡ phần còn lại. (Lưu ý kỹ thuật: giới hạn 1.000 này đã được ghi nhận trong dev.)

**Q: Hệ thống có tự gửi SMS không?**
A: Màn đặt cọc không trực tiếp gửi tin — nó bắn yêu cầu sang dịch vụ SMS/Zalo. Việc gửi thật do dịch vụ đó xử lý theo mẫu bạn cấu hình cho đợt.

**Q: Tôi xóa hẳn một phiếu cọc nhập nhầm được không?**
A: Không có chức năng xóa phiếu cọc. Bạn xử lý qua các thao tác trạng thái (thay sản phẩm, hoàn cọc...). Nếu cần xử lý đặc biệt, liên hệ bộ phận hỗ trợ.
