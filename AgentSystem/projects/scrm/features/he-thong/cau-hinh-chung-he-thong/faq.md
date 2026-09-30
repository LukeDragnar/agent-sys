> Draft — cần bổ sung từ ticket vận hành thật.

# FAQ — Cấu hình chung hệ thống

**Q: Cấu hình hệ thống dùng để làm gì?**
A: Đây là nơi khai báo các tham số dùng chung cho toàn hệ thống dưới dạng cặp mã → giá trị (key tích hợp, cờ bật/tắt tính năng, thông tin mặc định…). Đổi tham số ở đây giúp thay đổi hành vi hệ thống mà không cần sửa code.

**Q: Tôi không xóa được một tham số cấu hình?**
A: Việc xóa tham số cấu hình chỉ dành cho tài khoản SuperUser. Tài khoản admin thường chỉ xem/thêm/sửa, không xóa được — đây là chủ ý để tránh xóa nhầm tham số quan trọng. Nếu cần xóa, nhờ tài khoản SuperUser thực hiện.

**Q: Domain CORS là gì, thêm/xóa để làm gì?**
A: CORS là danh sách "domain (website) được phép gọi API của hệ thống". Khi triển khai một website/đối tác mới cần gọi API, bạn thêm domain đó vào whitelist. Khi một domain không còn dùng, xóa khỏi danh sách để chặn.

**Q: Thêm/xóa domain CORS có cần khởi động lại hệ thống không?**
A: Không. Mỗi lần thêm/sửa/xóa domain, hệ thống tự cập nhật lại cấu hình CORS toàn cục ngay lập tức. Lưu ý: vì có hiệu lực ngay nên xóa nhầm domain đang dùng sẽ khiến website đó bị chặn gọi API tức thì.

**Q: Sau khi thêm domain CORS mà website vẫn bị chặn gọi API?**
A: Kiểm tra lại: (1) đã nhập đúng domain (đúng giao thức/cổng nếu cần) chưa; (2) thao tác lưu có báo thành công không. Trong trường hợp hiếm, nếu bước cập nhật CORS toàn cục lỗi sau khi đã lưu danh sách thì có thể lệch giữa danh sách đã lưu và cấu hình đang chạy — cần kỹ thuật kiểm tra.

**Q: Vì sao có endpoint config app không cần đăng nhập?**
A: Giao diện cần một số cấu hình phía client (ví dụ key bản đồ) ngay khi khởi động, trước cả màn đăng nhập, nên endpoint này để công khai. Chỉ nên trả các key dùng phía client, không trả thông tin bí mật của máy chủ.

**Q: Đổi một tham số cấu hình có rủi ro gì không?**
A: Có. Nhiều module đọc tham số từ cấu hình hệ thống, nên đổi một giá trị có thể ảnh hưởng nhiều màn hình cùng lúc. Hãy hiểu rõ tham số (mã, ý nghĩa, nơi dùng) trước khi sửa, và ưu tiên test trên môi trường thử.

**Q: Khách hàng (chủ shop) có vào được màn cấu hình hệ thống không?**
A: Không. Đây là chức năng nội bộ của super-admin SaaS. Khách hàng không thấy và không chỉnh được các tham số toàn hệ thống.
