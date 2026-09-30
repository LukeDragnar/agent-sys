> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Quản lý Subiz

**Q: Subiz là gì và để làm gì trong hệ thống?**
A: Subiz là công cụ chat trên website. Khi khách nhắn tin qua Subiz trên website của shop, hệ thống nhận sự kiện đó và **tự tạo contact** (gán nguồn, đơn vị, chi nhánh, nhân viên sale) theo cấu hình bạn khai báo, để Sale/Marketing chăm sóc.

**Q: Tôi khai báo cấu hình Subiz ở đâu?**
A: Vào khu **Cấu hình chat nguồn ngoài (Subiz)** (cần đã bật tiện ích External Chat). Bấm Thêm, nhập account_id, secret, domain website và chọn nguồn / đơn vị / chi nhánh / nhân viên sale mặc định.

**Q: account_id và secret lấy ở đâu?**
A: Lấy từ tài khoản Subiz của bạn (bên Subiz cung cấp). account_id để hệ thống nhận diện tài khoản; secret là mã bí mật để **xác thực chữ ký** mỗi sự kiện chat — đảm bảo sự kiện thật từ Subiz.

**Q: Khách chat trên website mà hệ thống không nhận được contact?**
A: Kiểm tra: (1) account_id khai đúng tài khoản Subiz chưa; (2) secret đúng chưa (sai secret → xác thực thất bại); (3) domain website khớp chưa; (4) tiện ích External Chat còn bật không. Nếu vẫn lỗi, liên hệ hỗ trợ kiểm tra webhook.

**Q: Contact từ Subiz về sai nhân viên / sai nguồn?**
A: Contact mới được gán theo **giá trị mặc định** trong cấu hình (nguồn, đơn vị, chi nhánh, nhân viên sale). Mở cấu hình, chỉnh lại các giá trị mặc định cho đúng.

**Q: Tôi sửa được secret không, và khi nào nên đổi?**
A: Có thể sửa cấu hình (gồm secret). Nên đổi secret khi nghi ngờ bị lộ — đổi bên Subiz trước rồi cập nhật lại ở đây, vì secret là chìa khóa xác thực webhook.

**Q: Màn này có trả lời tin nhắn khách được không?**
A: Không. Đây là màn **cấu hình kết nối**. Tin nhắn khách qua Subiz được tạo thành contact, còn việc trả lời/chăm sóc diễn ra ở luồng contact/chat tương ứng.

**Q: Một shop khai báo được nhiều cấu hình Subiz không?**
A: Có thể có nhiều cấu hình (ví dụ nhiều website/domain). Mỗi cấu hình có account_id + domain riêng. *Chi tiết giới hạn cần product xác nhận.*
