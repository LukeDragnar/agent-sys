> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Quản lý tài khoản & người dùng

**Q: Nhân viên quên mật khẩu, làm sao cấp lại?**
A: Admin vào **Quản lý nhân viên / tài khoản**, tìm tài khoản đó và bấm **Đổi mật khẩu**. Khách không tự reset cho người khác được; chỉ admin hệ thống mới đổi mật khẩu hộ.

**Q: Nhân viên nghỉ việc, nên xóa hay khóa tài khoản?**
A: Thường nên **Khóa** để chặn đăng nhập mà vẫn giữ lịch sử. **Xóa** dùng khi chắc chắn không cần tài khoản đó nữa. Cả hai thao tác đều cần quyền admin hệ thống.

**Q: Tạo tài khoản mới báo "không tìm thấy đơn vị" là sao?**
A: Khi tạo tài khoản có gắn đơn vị, đơn vị đó phải tồn tại trong hệ thống. Nếu báo lỗi này, kiểm tra lại đơn vị bạn chọn đã có chưa, hoặc tạo đơn vị trước rồi tạo tài khoản.

**Q: Tài khoản của tôi vào nhầm/không thấy dữ liệu shop, có phải lỗi?**
A: Tài khoản gắn với một đơn vị và chi nhánh. Nếu thấy sai dữ liệu, có thể tài khoản đang gắn nhầm đơn vị/chi nhánh — liên hệ CSKH để tra cứu và chuyển đúng đơn vị.

**Q: CSKH tìm user/đơn vị của khách bằng cách nào?**
A: CSKH (HT-CSKH) có công cụ tìm đơn vị/chi nhánh theo user hoặc theo mã đơn, phục vụ hỗ trợ chuyển đổi đơn vị. Chức năng này giới hạn cho super-admin và CSKH.

**Q: "Đồng bộ user theo đơn vị" để làm gì, khi nào cần?**
A: Khi dữ liệu tài khoản của một đơn vị bị lệch giữa các phần của hệ thống (vd tạo/sửa nhưng chưa lan hết), super-admin chạy đồng bộ để cập nhật lại. Có màn xem trạng thái để biết đã đồng bộ xong chưa.

**Q: Khóa tài khoản rồi thì user còn đăng nhập được không?**
A: Không. Khóa sẽ chặn đăng nhập. Khi cần dùng lại, admin bấm **Mở khóa**.

**Q: Một user có thể gán nhiều role không?**
A: Role được gán cho user khi tạo/sửa tài khoản (kết hợp với phần phân quyền). Việc gán nhiều role do thiết lập của shop/hệ thống quyết định — cần product/admin xác nhận cấu hình cụ thể.
