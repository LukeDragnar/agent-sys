> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Quản lý đơn vị & hợp đồng

**Q: "Đơn vị" là gì?**
A: Mỗi khách hàng/shop dùng SCRM là một **đơn vị (tenant)**. Mọi dữ liệu (khách, đơn, nhân viên…) đều thuộc về một đơn vị. Quản trị đơn vị là việc của nhà cung cấp; khách chỉ làm việc trong đơn vị của mình.

**Q: Làm sao tạo đơn vị/shop mới?**
A: Có thể đăng ký đơn vị mới (bản app cho phép đăng ký), hoặc admin hệ thống tạo từ màn quản trị. Khi tạo, hệ thống khởi tạo dữ liệu nền cho đơn vị đó.

**Q: Sửa thông tin đơn vị nhưng vẫn thấy dữ liệu cũ?**
A: Màn chi tiết để **sửa** lấy bản mới nhất (không qua cache), còn thông tin hiển thị thường có thể qua cache. Nếu thấy chưa cập nhật, mở lại màn chi tiết/đăng nhập lại.

**Q: "Mã đơn vị" sinh và duyệt thế nào?**
A: Admin bấm **Sinh mã** để tạo mã cho đơn vị, sau đó **Duyệt mã** để xác nhận. Hai bước này tách nhau để kiểm soát.

**Q: Tôi muốn nâng/đổi gói cho shop của mình thì làm gì?**
A: Gửi **yêu cầu đổi gói sử dụng** từ phần đơn vị. Yêu cầu sẽ được phía nhà cung cấp (admin/kế toán hệ thống) duyệt; sau khi duyệt, gói áp dụng mới có hiệu lực.

**Q: Khác nhau giữa "hợp đồng" và "hợp đồng tiện ích"?**
A: **Hợp đồng** là chứng từ hợp đồng dịch vụ của đơn vị. **Hợp đồng tiện ích** là **yêu cầu mua một tiện ích** cụ thể — khi được duyệt thì tiện ích đó được bật cho đơn vị.

**Q: Duyệt yêu cầu mua tiện ích rồi mà khách vẫn chưa dùng được tính năng?**
A: Khi **Duyệt** hợp đồng tiện ích, tiện ích sẽ được kích hoạt cho đơn vị. Nếu khách vẫn chưa dùng được, kiểm tra thêm: user đã có **quyền** (phân quyền chức năng) cho chức năng đó chưa — tiện ích bật mới là lớp một, còn cần đúng quyền role.

**Q: Xem được đơn vị nào đang dùng tiện ích nào không?**
A: Có, dùng màn **Đơn vị tiện ích** để tra cứu danh sách tiện ích đang gắn cho từng đơn vị (dành cho HT-AD/HT-KT).

**Q: Đăng ký đơn vị mới có cần đăng nhập không?**
A: Hiện luồng đăng ký đơn vị (bản app) cho phép tạo mà không cần đăng nhập. Vì vậy nhà cung cấp cần có biện pháp xác minh để tránh đăng ký rác — đây là điểm đang lưu ý về an toàn.
