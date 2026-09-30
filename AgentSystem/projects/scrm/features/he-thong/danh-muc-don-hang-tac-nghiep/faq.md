> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Danh mục đơn hàng & tác nghiệp (gốc — cấp hệ thống)

**Q: Tôi thêm một loại đơn hàng mới ở màn quản trị nhưng dropdown loại đơn ở đơn vị vẫn chưa thấy?**
A: Danh mục đơn hàng dùng cache hệ thống. Trong code, thao tác **Thêm/Sửa không tự clear cache** (chỉ Xóa mới clear). Vì vậy mục mới có thể chưa phản ánh ngay tới dropdown cho tới khi có thao tác clear cache kế tiếp. Nếu cần, liên hệ đội kỹ thuật để làm mới cache.

**Q: Danh mục tác nghiệp ở khu quản trị hệ thống khác gì với danh mục tác nghiệp trong shop của tôi?**
A: Có hai bản: bản **hệ thống** (do admin hệ thống quản lý, route quản trị) và bản **đơn vị** (do shop tự quản lý). Bản hệ thống làm khung chung; bản đơn vị phục vụ riêng từng shop. Đừng nhầm hai màn.

**Q: "Bước khởi đầu", "khóa số" trong danh mục tác nghiệp nghĩa là gì?**
A: Mỗi bước tác nghiệp có thể được đánh dấu là bước khởi đầu (điểm bắt đầu của quy trình follow-up), bước khóa số (khóa số điện thoại trong khoảng thời gian), hoặc cho phép sửa giờ. Các cờ này điều khiển hành vi chăm sóc kho số/đơn.

**Q: Tôi cần quyền gì để vào các màn danh mục này?**
A: Danh mục tác nghiệp hệ thống cần quyền HT-AD hoặc HT-CSKH. Danh mục đơn hàng (bản quản trị) thuộc tiện ích superadmin của hệ thống. Tài khoản đơn vị thông thường không truy cập.

**Q: Vì sao route danh mục đơn hàng ghi "Sanbox" thiếu một chữ?**
A: Đó là lỗi chính tả trong route từ trước ("Sanbox" thay vì "Sandbox"). Cả backend và frontend đều dùng đúng chuỗi sai này nên hệ thống vẫn chạy bình thường — không nên sửa lẻ một bên vì sẽ làm lệch.

**Q: Xóa một loại đơn hàng có an toàn không?**
A: Sau khi xóa, hệ thống sẽ làm mới cache danh mục đơn hàng. Tuy nhiên hãy chắc loại đơn đó không còn được dùng, vì đây là master data dùng chung toàn hệ thống.
