> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Đối soát đơn online (Kế toán tác nghiệp)

**Q: Màn Kế toán tác nghiệp khác gì màn Thủ kho tác nghiệp?**
A: Cùng dữ liệu đơn hàng online nhưng khác góc nhìn và quyền: Kế toán tác nghiệp dành cho vai kế toán/CSKH (ghi chú kế toán, đối chiếu, xuất Excel), còn Thủ kho tác nghiệp dành cho thủ kho (thao tác kho/đăng đơn). Luồng đăng đơn dùng chung nên các ràng buộc giống nhau.

**Q: Tôi không thấy nút "đánh dấu đã đối soát" cho đơn?**
A: Chức năng "đối soát nội bộ" riêng hiện đang bị tắt trong hệ thống. Tạm thời, việc đối soát thực hiện qua xem chi tiết đơn, ghi chú kế toán, cập trạng thái và xuất Excel để đối chiếu. Nếu cần bật lại tính năng đánh dấu, liên hệ bộ phận hỗ trợ.

**Q: Kế toán có đăng đơn (gửi giao vận) được không?**
A: Được, nếu đơn vị bật cấu hình cho phép kế toán đăng đơn (mặc định bật). Nút Đăng đơn / Hủy đăng đơn nằm ngay trên màn. Lưu ý đơn sàn TMĐT bị chặn hủy đăng.

**Q: Đăng đơn báo lỗi thiếu tồn / không đăng được?**
A: Luồng đăng đơn dùng chung với Thủ kho tác nghiệp, nên các kiểm tra (tồn kho, khóa SP, gửi đơn vị giao vận) là như nhau. Kiểm tra tồn và cấu hình kho rồi thử lại; nếu cần đăng ngoại lệ, nhờ admin dùng chức năng Admin đăng đơn.

**Q: Tôi xuất Excel kiểu 1 và kiểu 2 thì 2 file trùng tên?**
A: Đúng — kiểu 1 và kiểu 2 cùng tải ra file tên `KeToanTacNghiep.xlsx`. Bạn nên đổi tên file sau khi tải để khỏi nhầm; kiểu 3 và kiểu 4 có hậu tố phân biệt sẵn.

**Q: Vì sao đơn đã giao mà chưa thấy lên hóa đơn/kế toán?**
A: Việc tự sinh phiếu bán hàng kế toán và phát hành hóa đơn tự động phụ thuộc cấu hình mốc trạng thái giao hàng và việc bật kế toán. Kiểm tra trạng thái giao hàng của đơn đã đạt mốc cấu hình chưa (xem feature "Tạo hóa đơn tự động").

**Q: Tôi thêm số điện thoại vào blacklist thì có tác dụng gì?**
A: Số trong blacklist được đánh dấu để cảnh báo/chặn ở các luồng liên quan (đặt đơn, chăm sóc). Bạn có thể thêm/xóa blacklist ngay trên màn này.
