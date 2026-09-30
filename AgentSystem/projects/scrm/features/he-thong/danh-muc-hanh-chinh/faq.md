> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Danh mục hành chính (địa giới — cấp hệ thống)

**Q: Tôi muốn thêm một tỉnh/thành mới nhưng không lưu được?**
A: Khi thêm Tỉnh/TP bắt buộc phải **chọn Quốc gia**; thêm Quận/Huyện phải chọn **Tỉnh**; thêm Phường/Xã phải chọn **Tỉnh + Quận/Huyện**. Thiếu cấp cha hệ thống sẽ báo lỗi và không lưu. Ngoài ra Mã/Tên không được trùng trong cùng cấp cha.

**Q: Ở màn nhập địa chỉ khách hàng, dropdown Tỉnh/Quận/Xã lấy dữ liệu từ đâu?**
A: Lấy từ danh mục hành chính cấp hệ thống này. Nếu một tỉnh/quận/xã chưa có trong danh mục thì sẽ không xuất hiện ở dropdown. Quản trị viên hệ thống cần thêm vào danh mục trước.

**Q: Tôi xóa một huyện nhưng huyện đó vẫn đang có nhiều xã, có sao không?**
A: Hệ thống hiện **không chặn** xóa khi còn cấp con. Nếu DB không ràng buộc khóa ngoại thì các xã con có thể trở thành dữ liệu mồ côi. Hãy kiểm tra và xóa cấp con trước, hoặc xác nhận với đội kỹ thuật trước khi xóa.

**Q: Tôi sửa tên một tỉnh thành trùng với tỉnh khác mà hệ thống không báo lỗi?**
A: Đúng, khi **sửa** hệ thống hiện không kiểm tra trùng Mã/Tên (chỉ kiểm tra lúc thêm mới). Vì vậy cần cẩn thận khi sửa để tránh tạo ra hai mục trùng nhau.

**Q: Cần quyền gì để vào màn danh mục địa giới?**
A: Cần quyền quản trị hệ thống (HT-AD) hoặc CSKH hệ thống (HT-CSKH). Tài khoản đơn vị thông thường không có quyền này.

**Q: Tôi thêm một quốc gia mà quên nhập Tên thì sao?**
A: Theo logic hiện tại, kiểm tra "trống" ở màn Quốc gia bị viết nhầm nên có thể **không bắt buộc nhập Tên** (chỉ thực sự kiểm tra Mã). Đây là lỗi đã biết — nên nhập đầy đủ cả Mã và Tên để tránh dữ liệu thiếu.
