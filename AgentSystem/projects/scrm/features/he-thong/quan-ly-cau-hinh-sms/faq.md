> Draft — cần bổ sung từ ticket thật.

# FAQ — Quản lý cấu hình SMS (duyệt đơn vị dùng tài khoản SMS)

**Q: Màn "Quản lý phê duyệt tài khoản cấu hình hệ thống" để làm gì?**
A: Để admin hệ thống/kế toán phê duyệt (hoặc từ chối) việc một đơn vị được dùng tài khoản SMS chung/đại lý để gửi tin. Đơn vị đăng ký xong sẽ ở trạng thái "Đang đợi duyệt"; sau khi duyệt mới được phép gửi qua tài khoản đó.

**Q: Trạng thái phê duyệt có những giá trị nào?**
A: Ba mức: **Đang đợi duyệt** (chưa quyết định), **Đã được duyệt** (đơn vị được dùng tài khoản), **Bị từ chối** (không được dùng).

**Q: Vì sao tôi chỉ thấy bản ghi của đơn vị mình?**
A: Hệ thống lọc theo đơn vị đang đăng nhập — mỗi đơn vị chỉ xem được đăng ký của chính mình để tránh lộ thông tin đơn vị khác.

**Q: Duyệt ở đây có nghĩa là gửi tin luôn không?**
A: Không. Duyệt chỉ cấp/thu **quyền dùng tài khoản** gửi tin. Việc soạn và gửi SMS/Zalo nằm ở module SMS riêng.

**Q: Tôi duyệt nhầm một đơn vị, hủy được không?**
A: Được. Trên dòng đó bấm nút **Hủy phê duyệt** (✗) để chuyển sang trạng thái Bị từ chối; có hộp xác nhận trước khi đổi.

**Q: Đơn vị đã đăng ký nhưng không gửi được SMS?**
A: Kiểm tra trạng thái phê duyệt của đơn vị tại màn này — nếu còn "Đang đợi duyệt" hoặc "Bị từ chối" thì đơn vị chưa được phép gửi qua tài khoản đó. Duyệt xong mới gửi được.

**Q: Khách hàng cuối có thấy màn này không?**
A: Không. Đây là màn quản trị nội bộ cho admin hệ thống và kế toán hệ thống.

---

### Cấu hình gateway/nhà cung cấp SMS (ConfigSMS) — *(bổ sung Pha 2 — ConfigSMS gateway)*

**Q: "Cấu hình SMS (ConfigSMS)" khác gì với phần duyệt đơn vị-dùng-tài-khoản ở trên?**
A: Hai thứ khác nhau. Phần duyệt ở trên là **cấp/thu quyền cho một đơn vị được dùng tài khoản SMS**. Còn ConfigSMS là **khai báo kết nối kỹ thuật tới nhà cung cấp SMS (gateway)**: mã cấu hình, nhà cung cấp/server, brandname, khóa kết nối, loại tin, bật/tắt... — để hệ thống biết gửi tin qua đâu và bằng tài khoản nào.

**Q: Cấu hình gateway gồm những thông tin gì?**
A: Mã cấu hình, nhà cung cấp/server, **brandname** (tên thương hiệu hiển thị khi gửi tin), khóa kết nối tới nhà cung cấp, loại tin, có dùng tài khoản gốc hay không, bật/tắt sử dụng, chế độ thử nghiệm (sandbox).

**Q: Khách hàng cuối có tự khai báo gateway SMS được không?**
A: Đây là cấu hình kỹ thuật cấp quản trị hệ thống, thường do đội vận hành đặt — *(cần product xác nhận có mở cho khách hàng cuối hay không)*.

**Q: Cấu hình gateway có chứa thông tin nhạy cảm không? Có an toàn không?**
A: Có — nó chứa **khóa kết nối tới nhà cung cấp** (khóa API/secret), nếu lộ ra ngoài người khác có thể giả mạo gửi tin bằng tài khoản của bạn. Hiện đường lấy cấu hình theo mã **không yêu cầu đăng nhập**, nên đây là điểm đội kỹ thuật cần xử lý (đã ghi chi tiết ở tài liệu dev). Người dùng nghiệp vụ không cần thao tác gì thêm.

**Q: Tôi đổi/tắt một cấu hình gateway thì có ảnh hưởng gì?**
A: Có. Khi gửi tin, hệ thống đọc đúng cấu hình theo mã để kết nối nhà cung cấp; nếu cấu hình bị tắt/sai hoặc bị xóa thì việc gửi tin qua nhà cung cấp đó có thể không hoạt động.
