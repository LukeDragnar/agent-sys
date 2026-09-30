> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Hóa đơn điện tử

**Q: Tôi bấm xuất hóa đơn nhưng không thấy hóa đơn ra ngay, có phải lỗi không?**
A: Không. Việc phát hành hóa đơn được xử lý ngầm: hệ thống chỉ báo "đã nhận yêu cầu xuất hóa đơn điện tử", sau đó mới gửi sang nhà cung cấp (MISA/Viettel) để ký và phát hành. Bạn đợi vài giây rồi mở lại popup "Danh sách hóa đơn điện tử" của đơn để xem trạng thái. Nếu sau một lúc vẫn báo thất bại, hãy kiểm tra kết nối tài khoản và mẫu mặc định.

**Q: Bấm xuất hóa đơn thì báo "Bạn không có quyền sử dụng tiện ích này"?**
A: Đơn vị của bạn chưa bật tiện ích hóa đơn điện tử (MISA hoặc Viettel). Liên hệ admin/đơn vị cung cấp dịch vụ để bật tiện ích tương ứng.

**Q: Shop tôi dùng cả MISA và Viettel được không?**
A: Được. Hệ thống hỗ trợ cả 2 nhà cung cấp; mỗi nhà cung cấp khai báo tài khoản và mẫu riêng (2 tab MeInvoice và Viettel). Khi phát hành, hệ thống tự gửi đúng nhà cung cấp theo loại hóa đơn của đơn.

**Q: Tôi không phát hành được hóa đơn từ phiếu xuất kho qua Viettel?**
A: Phiếu xuất kho hiện chỉ hỗ trợ phát hành hóa đơn qua MeInvoice (MISA). Nếu cần dùng Viettel, hãy xuất hóa đơn từ đơn hàng thông thường.

**Q: Hóa đơn nháp là gì, dùng khi nào?**
A: Hóa đơn nháp cho phép kế toán tạo và soát thông tin trước khi phát hành thật. Sau khi soát xong, bạn đồng bộ nháp → phát hành để xuất hóa đơn chính thức. Lưu ý: nháp chỉ hỗ trợ MISA và Viettel.

**Q: Vừa tạo tài khoản ở tab Viettel mà sao nó hiện thành tài khoản MISA?**
A: Đây là tình huống đã biết: nếu khi lưu tài khoản thiếu thông tin loại nhà cung cấp, hệ thống có thể gán nhầm. Bạn xóa tài khoản đó và tạo lại, đảm bảo chọn đúng tab/nhà cung cấp; nếu vẫn lệch, báo bộ phận hỗ trợ.

**Q: Tải file hóa đơn PDF ở đâu?**
A: Sau khi đơn đã phát hành thành công (có số hóa đơn), mở popup hóa đơn điện tử của đơn và bấm nút Tải/Download. File PDF lấy trực tiếp từ nhà cung cấp.

**Q: Tra cứu danh sách hóa đơn ở popup theo đơn và ở màn tra cứu tổng lại lệch số?**
A: Hai màn lấy dữ liệu từ 2 kho khác nhau (lịch sử gửi nhà cung cấp và bảng phát hành), nên trong lúc dữ liệu chưa đồng bộ kịp có thể thấy lệch. Đợi đồng bộ hoặc đối chiếu lại theo mã đơn/số hóa đơn.

**Q: Khách có được báo khi đã có hóa đơn không?**
A: Có thể — sau khi phát hành thành công, hệ thống có thể đẩy thông báo qua SMS/Zalo (ZNS) tùy cấu hình của đơn vị.
