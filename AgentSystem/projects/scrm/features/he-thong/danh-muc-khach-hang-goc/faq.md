> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Danh mục khách hàng (gốc — cấp hệ thống)

**Q: Danh mục nhóm/hạng khách hàng ở khu quản trị này khác gì với danh mục trong shop của tôi?**
A: Khu quản trị này là **cấp hệ thống**, dành cho quản trị viên của nhà cung cấp phần mềm. Đây là bộ danh mục gốc dùng chung. Còn trong từng shop/đơn vị có màn danh mục riêng để khai báo theo nhu cầu của shop. Người dùng shop thông thường không vào màn cấp hệ thống này.

**Q: Tôi muốn xóa một nhóm khách hàng không dùng nữa thì làm sao?**
A: Màn quản trị nhóm khách hàng hiện **không cho xóa** (chức năng xóa đã bị tắt). Cách xử lý là **tắt "Sử dụng"** cho nhóm đó — nhóm sẽ không còn được gán cho khách mới nữa.

**Q: "Tự động xét nâng nhóm khi thanh toán" nghĩa là gì?**
A: Khi bật cờ này cho một nhóm, hệ thống sẽ tự động kiểm tra và nâng nhóm cho khách hàng tại thời điểm khách thanh toán (theo điều kiện đã thiết lập). Khi tắt, hệ thống không tự xét — việc đổi nhóm phải làm thủ công.

**Q: Vì sao tôi vào được màn Hạng khách hàng nhưng đồng nghiệp lại không?**
A: Màn Hạng khách hàng (bản admin) yêu cầu quyền quản trị hệ thống (HT-AD). Nếu tài khoản chưa được cấp quyền này thì không truy cập được.

**Q: Tôi sửa lại tên một hạng khách hàng thì khách đang ở hạng đó có bị ảnh hưởng không?**
A: Đổi tên hạng chỉ thay đổi nhãn hiển thị; khách vẫn ở hạng đó. Tuy nhiên thay đổi danh mục hạng ở cấp hệ thống có thể lan tới logic xếp hạng và hiển thị ở màn khách hàng 360, nên cần cân nhắc và kiểm tra lại sau khi sửa.

**Q: Tôi đặt một nhóm là "mặc định" được không?**
A: Theo chức năng hiện có ở màn admin nhóm khách hàng, bạn có thể bật/tắt "Sử dụng" và "Tự động xét nâng nhóm khi thanh toán". Việc đặt nhóm mặc định cụ thể *cần product xác nhận* vì không nằm trong các hành động được ghi nhận ở màn này.
