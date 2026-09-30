> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Phê duyệt giao dịch tổng đài (super-admin)

**Q: Đơn vị nạp tiền tổng đài xong, sao chưa dùng được ngay?**
A: Giao dịch nạp tiền sau khi tạo ở trạng thái **chờ thanh toán**, cần super-admin (HT-AD/HT-KT) **duyệt**. Sau khi duyệt, giao dịch chuyển sang **đã thanh toán** và số dư tổng đài của đơn vị mới được cộng.

**Q: Ai có quyền duyệt giao dịch tổng đài?**
A: Chỉ role hệ thống **HT-AD** (admin hệ thống) và **HT-KT** (kế toán hệ thống). Đây là khu vực super-admin của nhà cung cấp, không phải màn của khách.

**Q: Bấm Duyệt báo "Id đơn vị không được để trống" là sao?**
A: Khi duyệt phải xác định đúng đơn vị của giao dịch. Nếu thiếu Id đơn vị (chưa chọn đơn vị/giao dịch), hệ thống chặn và báo lỗi này. Chọn lại đúng giao dịch của đơn vị rồi duyệt.

**Q: Lỡ duyệt nhầm một giao dịch thì sửa thế nào?**
A: Dùng nút **Hủy duyệt** để chuyển giao dịch về trạng thái **hủy thanh toán**. Lưu ý đối chiếu lại số dư đơn vị sau khi hủy.

**Q: Sau khi duyệt, số dư tổng đài của đơn vị có tự cộng không?**
A: Có. Duyệt thành công sẽ cộng số dư tổng đài cho đơn vị tương ứng với giá trị giao dịch nạp tiền.

**Q: Tôi muốn gia hạn cho một giao dịch thì làm ở đâu?**
A: Mở chi tiết giao dịch và dùng chức năng **Cập nhật ngày hết hạn** để đặt lại ngày hết hạn của giao dịch đó.

**Q: Có xem được toàn bộ lịch sử nạp tiền tổng đài của một đơn vị không?**
A: Có. Chọn đơn vị, màn sẽ hiển thị **lịch sử giao dịch** nạp tiền tổng đài của đơn vị đó để bạn đối chiếu, theo dõi.
