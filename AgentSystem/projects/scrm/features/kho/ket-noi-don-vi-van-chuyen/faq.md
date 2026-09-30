# FAQ — Kết nối đơn vị vận chuyển

> Draft — cần bổ sung từ ticket khách thật.

### 1. Tôi kết nối GHN (hoặc Viettel Post, J&T...) như thế nào?
Vào **Kho → Kết nối đơn vị vận chuyển**, chọn hãng (vd GHN). Màn hình hiện form thông tin hãng yêu cầu — thường là **Token / API key** hoặc **tài khoản + mật khẩu** do hãng cấp. Token lấy trong trang quản trị của hãng (mỗi hãng một chỗ; trên màn hình có phần "Hướng dẫn kết nối" cho từng hãng). Nhập xong chọn **kho lấy hàng**, rồi bấm **Kết nối**. Kết nối báo thành công là dùng được.

### 2. Lấy token / API key của hãng ở đâu?
Token nằm trong **trang quản trị của chính hãng vận chuyển** (vd GHN, Viettel Post bạn đăng nhập web của họ để lấy), không phải SCRM cấp. SCRM chỉ lưu token bạn dán vào để gọi sang hãng. Mỗi hãng có vị trí token khác nhau — xem mục Hướng dẫn kết nối hiển thị ngay trên màn hình kết nối của hãng đó.

### 3. Nhập sai token thì sao?
Khi bấm **Kết nối**, SCRM gọi thử sang hãng. Nếu token sai/không hợp lệ, hệ thống báo "Kết nối tài khoản thất bại" kèm lý do. Bạn nhập lại token đúng rồi kết nối lại. Token sai cũng làm **đăng đơn thất bại**, nên nên kết nối thành công trước khi gửi đơn.

### 4. Token bị hết hạn / hãng đổi token thì làm gì?
Vào lại màn hình kết nối của hãng, nhập **token mới** và bấm Kết nối/Lưu lại. Không cần xóa kết nối cũ. Khi đăng đơn báo lỗi xác thực mà trước đó vẫn chạy được thì khả năng cao là token hết hạn.

### 5. Phí ship lấy ở đâu, có chính xác không?
Trước khi đăng đơn bạn bấm **Tính phí** — SCRM hỏi hãng và trả về phí dự kiến theo người gửi/người nhận, khối lượng, dịch vụ. Sau khi đăng đơn, **hãng có thể trả lại phí thật khác** với phí dự tính; SCRM tự cập nhật phần chênh lệch vào **hỗ trợ ship**, **không thay đổi tiền thu khách** đã chốt. Một số hãng (vd GHTK) ẩn phí nên có thể không hiển thị.

### 6. Đổi hãng vận chuyển cho đơn được không?
Được, **với đơn chưa đăng đơn**: chỉ cần chọn hãng/tài khoản khác rồi đăng. Đơn **đã đăng đơn** thì phải **Hủy đăng đơn** trước, sau đó mới chọn hãng khác và đăng lại.

### 7. Một hãng kết nối được nhiều tài khoản / nhiều cửa hàng không?
Có. Một hãng có thể có **nhiều tài khoản giao vận**, mỗi tài khoản gắn một **kho/cửa hàng lấy hàng** riêng. Khi đăng đơn nhớ chọn đúng tài khoản ứng với đúng kho lấy hàng.

### 8. Đăng đơn xong lấy mã vận đơn ở đâu?
Sau khi đăng đơn thành công, hãng trả về **mã vận đơn (mã đơn giao vận)** và hệ thống gắn vào đơn hàng, đồng thời chuyển trạng thái sang **Đã đăng**. Bạn xem mã này ở chi tiết đơn / màn hình quản lý vận đơn, và có thể in phiếu gửi hàng.

### 9. Vì sao đăng đơn báo lỗi "Tiền thu hộ không chính xác"?
Hệ thống kiểm tra **COD = tiền cuối − đặt cọc**. Nếu số tiền thu hộ trên đơn không khớp công thức này, SCRM **chặn đăng đơn** để tránh thu sai tiền khách. Kiểm tra lại tiền hàng, giảm giá, đặt cọc của đơn rồi đăng lại.

### 10. Hủy đăng đơn rồi thì hàng/kho có được trả lại không?
Có. Khi **hủy đăng đơn** hoặc **đăng đơn thất bại**, hệ thống tự **cộng/trừ lại tồn kho** và đưa đơn về trạng thái chờ vận đơn. Phiếu bán hàng liên quan cũng được rollback tương ứng.

### 11. "Giao hàng tùy chỉnh" là gì, khác gì các hãng kia?
Là lựa chọn để bạn **tự khai báo đơn vị giao hàng riêng** (shipper nội bộ, hãng chưa có sẵn). Nó không gọi API hãng để lấy mã vận đơn tự động như GHN/Viettel; bạn quản lý thông tin giao hàng thủ công.
