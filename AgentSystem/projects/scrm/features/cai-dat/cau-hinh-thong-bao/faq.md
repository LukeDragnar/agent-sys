> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Cấu hình thông báo

**Q: Tôi đã cấu hình rồi mà nhân viên vẫn không nhận được thông báo?**
A: Kiểm tra theo thứ tự: (1) dòng sự kiện đó đã tích **Sử dụng** chưa; (2) đã chọn **Mẫu tin nhắn** chưa (bỏ trống thì không có nội dung để gửi); (3) đã tích đúng **đối tượng nhận** (sale/MKT/thu ngân/admin/user) chưa; (4) đã bấm **Lưu** chưa. Ngoài ra thông báo chỉ phát sinh khi sự kiện thật xảy ra (đơn đổi trạng thái, hoàn tất tác nghiệp, lịch hẹn đổi trạng thái).

**Q: Cấu hình gửi thông báo và Mẫu thông báo khác gì nhau?**
A: Mẫu thông báo là **nội dung** (viết sẵn, có mã). Cấu hình gửi là **luật**: khi sự kiện X xảy ra thì gửi mẫu nào, cho ai. Muốn dùng được một mẫu trong cấu hình thì mẫu đó phải tồn tại ở màn Mẫu thông báo trước.

**Q: "Khởi tạo lại" làm gì? Bấm có mất dữ liệu không?**
A: "Khởi tạo lại" sẽ **xóa toàn bộ cấu hình của kiểu thông báo đang chọn** (ví dụ đang ở tab Trạng thái đơn hàng thì xóa hết cấu hình của tab đó). Dùng khi muốn làm lại từ đầu. Hãy cân nhắc kỹ vì đây là xóa hàng loạt.

**Q: Có mấy kiểu thông báo để cấu hình?**
A: Có 4 kiểu chính: **Hành động hệ thống**, **Tác nghiệp** (luồng chăm sóc khách), **Trạng thái đơn hàng/giao hàng**, **Trạng thái lịch hẹn**. Mỗi kiểu có danh sách sự kiện riêng để bạn tích chọn người nhận và mẫu.

**Q: Tôi muốn gửi thông báo cho một vài nhân viên cụ thể, không phải cả nhóm?**
A: Trong dòng cấu hình có ô **Gửi user** — chọn đúng những nhân viên bạn muốn nhận. Bạn cũng có thể vừa tích nhóm (sale/MKT/admin…) vừa chọn thêm user cụ thể.

**Q: Tôi muốn thêm một mẫu thông báo mới nhưng không thấy nút Thêm mới?**
A: Màn Mẫu thông báo hiện chỉ cho **Sửa** và **Xóa** mẫu có sẵn, không có nút Thêm mới. Việc tạo mẫu mới do hệ thống cấp/khởi tạo — vui lòng liên hệ bộ phận hỗ trợ (cần product xác nhận quy trình chuẩn).

**Q: Tôi lỡ xóa một mẫu, giờ thông báo bị trống nội dung?**
A: Nếu một cấu hình gửi đang trỏ tới mẫu đã bị xóa thì thông báo sinh ra có thể rỗng. Hãy vào lại Cấu hình gửi, chọn lại một mẫu còn tồn tại cho dòng sự kiện đó.

**Q: Gửi trước / sau bao nhiêu phút là gì?**
A: Với một số cấu hình (đặc biệt luồng tác nghiệp tiếp theo), bạn có thể đặt gửi thông báo **trước** hoặc **sau** sự kiện một khoảng phút, để nhắc đúng thời điểm thay vì gửi ngay lập tức.
