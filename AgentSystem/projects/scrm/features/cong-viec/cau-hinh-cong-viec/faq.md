> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Cấu hình công việc

**Q: Cấu hình công việc khác Quản lý công việc ở chỗ nào?**
A: Quản lý công việc là nơi làm việc hằng ngày (tạo/giao/theo dõi task). Cấu hình công việc là nơi thiết lập sẵn khuôn mẫu: mẫu công việc, trường thông tin động, và quy trình tự sinh việc. Thiết lập một lần, dùng nhiều lần.

**Q: Tôi bấm Khởi chạy quy trình nhưng báo "Quy trình chưa có phiên bản"?**
A: Quy trình phải được lưu ít nhất một lần (để tạo phiên bản) thì mới khởi chạy được. Hãy mở quy trình, lưu lại, rồi khởi chạy.

**Q: Khởi chạy rồi sao chưa thấy công việc con?**
A: Khởi chạy chỉ tạo công việc gốc (trạng thái chưa bắt đầu). Bạn cần bấm **Bắt đầu** lượt — khi đó hệ thống mới sinh cây công việc con theo quy trình.

**Q: Tôi sửa quy trình thì các lượt đang chạy có bị ảnh hưởng không?**
A: Không. Lượt đã khởi chạy giữ nguyên phiên bản quy trình tại thời điểm khởi chạy. Sửa quy trình chỉ áp dụng cho các lượt khởi chạy sau này.

**Q: Vì sao lịch sử phiên bản quy trình nhiều bản đến vậy?**
A: Mỗi lần lưu quy trình đều tạo một phiên bản mới, kể cả khi chỉ sửa nhỏ. Đây là thiết kế để giữ vết thay đổi; lịch sử có thể dài.

**Q: Tôi xóa một mẫu công việc, các công việc cũ dùng mẫu đó thì sao?**
A: Hệ thống không kiểm tra mẫu có đang được dùng khi xóa. Công việc cũ vẫn giữ liên kết tới mẫu đã xóa (liên kết "treo"). Nên cân nhắc trước khi xóa mẫu đang dùng.

**Q: Lượt thực thi sinh ra hàng loạt công việc, lỡ sai thì xóa thế nào?**
A: Xóa lượt thực thi là xóa mềm và kéo theo công việc gốc + con của lượt. Cụm này vào Lịch sử xóa và có thể khôi phục lại.

**Q: Bắt đầu lượt mà báo "Lượt đã được bắt đầu"?**
A: Mỗi lượt chỉ được bắt đầu một lần. Nếu đã bấm Bắt đầu trước đó (đã có ngày bắt đầu) thì không bắt đầu lại được.
