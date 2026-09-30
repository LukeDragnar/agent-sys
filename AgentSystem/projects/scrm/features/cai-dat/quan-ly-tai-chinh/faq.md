> Draft — cần bổ sung từ ticket khách thật.

# FAQ — Quản lý tài chính (đối tác trả góp & domain)

**Q: Khách muốn thanh toán trả góp nhưng form không có đối tác nào để chọn?**
A: Đơn vị cần khai báo trước trong **Cấu hình đối tác trả góp**: tích chọn (map) các đối tác trả góp chấp nhận và khai báo tài khoản trả góp. Sau khi cấu hình, form thanh toán mới có đối tác để chọn.

**Q: Tôi thêm cấu hình trả góp mà báo "Id chi nhánh là bắt buộc"?**
A: Nếu bạn không phải admin tổng của đơn vị, hệ thống yêu cầu chọn **chi nhánh** cho cấu hình trả góp. Hãy chọn chi nhánh rồi lưu lại. Admin tổng có thể tạo cấu hình cấp đơn vị (không cần chi nhánh).

**Q: Tôi lưu cấu hình trả góp mà báo "Đối tượng đã tồn tại trong hệ thống"?**
A: Đã có một cấu hình trùng theo cùng đơn vị + chi nhánh + đối tác trả góp + mã tài khoản. Hãy sửa cấu hình hiện có thay vì thêm mới, hoặc đổi mã tài khoản.

**Q: Map nhanh đối tác và thêm cấu hình chi tiết khác nhau thế nào?**
A: "Map nhanh" chỉ tích chọn đối tác đơn vị chấp nhận (lưu tối thiểu). "Thêm cấu hình" khai báo đầy đủ tài khoản trả góp (gắn nhà cung cấp, mã/tên tài khoản, chi nhánh) để dùng cho thu tiền. Hãy dùng cấu hình đầy đủ nếu cần thu tiền theo đối tác.

**Q: Tôi muốn đổi tên miền (domain) của hệ thống đơn vị thì làm sao?**
A: Vào màn **Domain đơn vị**, bấm **Tạo yêu cầu** đổi domain. Yêu cầu sẽ ở trạng thái chờ duyệt; admin hệ thống duyệt xong mới áp domain mới. Bạn có thể hủy yêu cầu trước khi nó được xử lý.

**Q: Yêu cầu đổi domain của tôi bị từ chối / tôi tự duyệt được không?**
A: Việc duyệt hoặc từ chối yêu cầu đổi domain là của **admin hệ thống** (HT-AD/HT-KT), không phải đơn vị. Nếu bị từ chối, domain cũ được giữ nguyên; bạn có thể tạo lại yêu cầu hoặc liên hệ hỗ trợ để biết lý do.

**Q: Tôi xem được các lần đã đổi domain ở đâu?**
A: Trong màn **Lịch sử thay đổi domain** (phía đơn vị), bạn lọc và xem các yêu cầu cùng trạng thái (chờ duyệt / đã duyệt / từ chối / hủy).

**Q: Xóa một đối tác trả góp đã cấu hình có ảnh hưởng gì?**
A: Xóa cấu hình sẽ bỏ đối tác/tài khoản đó khỏi form thanh toán trả góp của đơn vị. Các giao dịch đã phát sinh trước đó không bị xóa, nhưng từ sau sẽ không chọn được đối tác này nữa.
