> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Thông báo & realtime nền (chuông thông báo)

**Q: Sao tôi không nhận được thông báo realtime (không thấy popup đẩy)?**
A: Thường do trình duyệt/điện thoại đang **chặn thông báo**. Kiểm tra: (1) lần đầu đăng nhập bạn đã bấm **Cho phép** khi được hỏi "cho phép gửi thông báo?" chưa — nếu lỡ bấm "Chặn" thì vào cài đặt trang để bật lại; (2) app/trình duyệt có đang được phép chạy nền không; (3) thử **đăng xuất rồi đăng nhập lại** để hệ thống đăng ký lại thiết bị. Lưu ý chuông trên web vẫn cập nhật khi bạn mở web, kể cả khi đẩy bị chặn.

**Q: Chuông không tăng số / số chưa đọc không đúng?**
A: Badge đếm số thông báo **chưa đọc** của riêng bạn. Nếu thấy lệch, thử tải lại trang. Nếu hoàn toàn không tăng dù chắc chắn có sự kiện (ví dụ vừa có đơn mới), có thể do cấu hình "sự kiện này có gửi cho vai trò của bạn không" — nhờ admin kiểm tra ở **Cấu hình thông báo**, hoặc báo kỹ thuật nếu cả nhà cùng không nhận.

**Q: Tôi vừa được đổi vai trò/chi nhánh, giờ không nhận được các thông báo theo vai trò mới?**
A: Thông báo gửi theo **nhóm** bạn thuộc về (đơn vị, chi nhánh, vai trò). Khi đổi vai trò, cần đăng ký lại nhóm — cách đơn giản là **đăng xuất và đăng nhập lại**, hệ thống sẽ cập nhật đúng các nhóm mới của bạn.

**Q: Tại sao đồng nghiệp nhận được thông báo còn tôi thì không (cùng một sự kiện)?**
A: Vì cấu hình quyết định **gửi cho ai** theo vai trò/đối tượng (Sale, MKT, thu ngân, admin chi nhánh…). Một sự kiện có thể chỉ gửi cho Sale phụ trách chứ không gửi cho tất cả. Đây là thiết lập ở **Cấu hình thông báo**, không phải lỗi.

**Q: Thông báo tới chậm, có khi vài chục giây mới thấy — có bình thường không?**
A: Có. Thông báo được đưa vào **hàng đợi** rồi một tiến trình nền gửi đi theo từng đợt. Bình thường chỉ vài giây; lúc hệ thống tải nặng hoặc gửi số lượng lớn có thể trễ hơn. Nếu trễ kéo dài bất thường (hàng phút) thì báo kỹ thuật.

**Q: Tôi đăng nhập trên cả máy tính và điện thoại — thông báo về đâu?**
A: Về **tất cả** thiết bị bạn đang đăng nhập. Khi bạn đăng xuất khỏi một thiết bị, thiết bị đó sẽ ngừng nhận thông báo đẩy.

**Q: Tôi có thể tự tắt từng loại thông báo cho riêng mình không?**
A: Hiện việc bật/tắt theo từng loại sự kiện nằm ở **Cấu hình thông báo** cấp đơn vị (admin quản lý), chứ chưa có công tắc riêng cho từng cá nhân. (cần product xác nhận nếu sau này có.) Bạn có thể tắt **đẩy** bằng cách bỏ quyền thông báo của trình duyệt, nhưng chuông trong web vẫn hiện.

**Q: Tôi xóa nhầm một thông báo trên chuông, lấy lại được không?**
A: Xóa thông báo trên chuông là thao tác cá nhân và **không khôi phục lại được** từ giao diện. Nếu nội dung quan trọng (ví dụ đơn mới), bạn vẫn tra được ở module gốc tương ứng (đơn hàng, công việc, chat…).

**Q: Đôi khi tôi thấy một thông báo bị trùng hai lần?**
A: Có thể do bạn thuộc nhiều nhóm cùng được cấu hình nhận một sự kiện (ví dụ vừa là Sale vừa là trưởng nhóm), hoặc đang mở nhiều thiết bị. Nếu trùng nhiều và gây khó chịu, nhờ admin rà lại **Cấu hình thông báo** để tránh chọn chồng nhiều đối tượng nhận cho cùng một sự kiện.
