> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Quản lý gói & tiện ích

**Q: "Gói sử dụng" và "tiện ích" khác nhau thế nào?**
A: **Gói** là gói cước nền (vd Basic/Pro) quyết định phạm vi sử dụng chung. **Tiện ích** là tính năng cộng thêm bán riêng (vd ChatHub, tổng đài). Đơn vị có một gói và có thể mua thêm nhiều tiện ích.

**Q: Tôi mua tiện ích rồi nhưng nhân viên vẫn không dùng được tính năng đó?**
A: Mua tiện ích chỉ là **bật tính năng** ở cấp đơn vị (lớp 1). Nhân viên còn cần được **phân quyền (role)** cho chức năng đó (lớp 2). Kiểm tra phân quyền chức năng cho role của nhân viên.

**Q: Mua tiện ích ở đâu, ai mua được?**
A: Vào **Chợ tiện ích / Mua tiện ích** trong phần đơn vị, bấm **Mua ngay**. Thường chỉ **admin tổng đơn vị (DV-AD-TONG)** (và kế toán hệ thống) mới mua được.

**Q: Mua xong xem lại ở đâu? Có lịch sử không?**
A: Có. Xem tab **Tiện ích đã mua** (lọc còn hạn / tất cả) và **Lịch sử giao dịch** để biết đã mua/giao dịch những gì.

**Q: Hủy/xóa tiện ích đã mua thì chuyện gì xảy ra?**
A: Xóa tiện ích đã đăng ký sẽ **thu hồi** quyền dùng tính năng tương ứng của đơn vị. Sau đó các màn thuộc tiện ích đó sẽ không truy cập được nữa cho tới khi mua lại.

**Q: Tôi là admin hệ thống, muốn xóa một tiện ích khỏi danh mục nhưng không được?**
A: Nếu tiện ích đó **đang có đơn vị sử dụng**, hệ thống sẽ chặn xóa ("Tiện ích đang có đơn vị sử dụng nên không thể xóa") để tránh làm gãy các đơn vị đang dùng. Cần gỡ hết đơn vị dùng trước.

**Q: Đơn vị xin nâng cấp gói thì ai duyệt?**
A: Yêu cầu nâng cấp gói chỉ được **super-admin, kế toán hệ thống hoặc admin hệ thống** duyệt/hủy. Người không thuộc các nhóm này sẽ bị báo không có quyền dù thấy nút.

**Q: Duyệt nâng cấp gói xong khi nào có hiệu lực?**
A: Sau khi duyệt, gói mới được cập nhật áp dụng cho đơn vị. Nếu khách vẫn thấy gói cũ, kiểm tra lại trạng thái yêu cầu (đã duyệt chưa) và đăng nhập lại.

**Q: Vì sao xóa tiện ích đã đăng ký lại giống "mở một đường link"?**
A: Về kỹ thuật, thao tác hủy tiện ích đã đăng ký hiện dùng phương thức GET — đây là điểm lưu ý nội bộ, không ảnh hưởng việc bạn bấm nút Xóa trên giao diện.
