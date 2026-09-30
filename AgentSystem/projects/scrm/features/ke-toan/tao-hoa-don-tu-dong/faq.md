> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Tạo hóa đơn tự động

**Q: Làm sao để đơn tự ra hóa đơn mà không cần bấm tay?**
A: Cần 3 điều kiện: (1) đã bật tiện ích hóa đơn điện tử + cấu hình tài khoản và mẫu mặc định; (2) đánh dấu trạng thái giao hàng nào được tự phát hành (cờ tự phát hành); (3) chọn loại hóa đơn tự động (phát hành thật hay tạo nháp) và số phút trễ. Sau đó, đơn nào chuyển sang trạng thái đã đánh dấu sẽ tự xử lý sau độ trễ.

**Q: Đơn giao xong rồi mà mãi chưa thấy hóa đơn?**
A: Kiểm tra: (1) trạng thái giao hàng của đơn đã đạt đúng mốc cấu hình chưa; (2) đơn đã có hóa đơn trước đó chưa (nếu có thì hệ thống bỏ qua); (3) đợi qua khoảng trễ cấu hình (mặc định 5 phút với phát hành, 2 phút với nháp); (4) xem log hóa đơn tự động để biết lý do bỏ qua/thất bại.

**Q: Tôi chỉ muốn tự tạo nháp để soát rồi mới phát hành, có được không?**
A: Được. Đặt loại hóa đơn tự động sang chế độ "nháp". Khi đó hệ thống chỉ tự tạo hóa đơn nháp; kế toán soát rồi mới đồng bộ nháp → phát hành thủ công.

**Q: Hệ thống có tự phát hành trùng 2 hóa đơn cho 1 đơn không?**
A: Không. Hệ thống bỏ qua đơn đã có số/ngày hóa đơn, và kiểm tra hàng đợi (cả bảng gửi lại) để tránh xếp trùng một đơn.

**Q: Tự phát hành có chọn nhà cung cấp được không?**
A: Tự phát hành dùng đúng nhà cung cấp/mẫu mặc định đã cấu hình cho đơn (MISA hoặc Viettel). Chỉ hỗ trợ MISA và Viettel.

**Q: Đặt độ trễ để làm gì?**
A: Độ trễ (vài phút) cho phép có khoảng đệm để hủy/sửa nếu cần trước khi hóa đơn được phát hành tự động, tránh phát hành ngay khi vừa đổi trạng thái.

**Q: Có hủy được hóa đơn đang chờ tự phát hành không?**
A: Có luồng hủy phát hành tự động cho đơn đang chờ trong hàng đợi — thao tác cụ thể cần product xác nhận. Sau khi đã phát hành thật thì xử lý theo quy trình hủy hóa đơn thông thường.
