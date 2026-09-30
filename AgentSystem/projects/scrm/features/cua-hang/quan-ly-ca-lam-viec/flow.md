# Luồng — Quản lý ca làm việc (Biên bản bàn giao ca)

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị (DonViId).
- Có role được phép thao tác biên bản (DV-AD-TONG / DV-AD-CN / HT-KT / DV-KT / DV-KT-TONG / DV-QLDV — *cần product xác nhận* danh sách gói).
- Có sẵn danh sách nhân viên để chọn người nhận đầu/cuối ca.

## Luồng dùng (user)
1. Người dùng mở màn **Biên bản bàn giao ca** → hệ thống liệt kê biên bản theo filter + theo người dùng hiện tại.
2. Bấm **Thêm mới** → tạo biên bản. Hệ thống ghi lại **người tạo** (dùng cho việc chặn xóa sau này).
3. **Đầu ca:** nhập người nhận đầu ca + tổng sản phẩm/tiền đầu ca (qua các dòng chi tiết) → bấm **Xác nhận đầu ca**. Hệ thống đặt cờ "đã xác nhận đầu ca" → biên bản chuyển trạng thái và khóa phần đầu ca.
4. **Cuối ca:** mở lại biên bản, nhập người nhận cuối ca + **tiền thiếu cuối ca** → bấm **Xác nhận cuối ca**. Hệ thống đặt cờ "đã xác nhận cuối ca" → đóng ca.
5. Xem chi tiết biên bản bất kỳ lúc nào (header + danh sách dòng chi tiết).

## Luồng cấu hình (admin)
- **Quản trị dòng chi tiết:** super user (HT-AD) có màn riêng để Thêm/Xóa từng dòng chi tiết của biên bản. Thao tác Thêm/Xóa **chặn cứng** nếu không phải super user; thao tác Cập nhật dòng thì cả admin đơn vị (DV-AD-TONG) cũng làm được.
- Người dùng cửa hàng bình thường KHÔNG dùng màn quản trị này — họ nhập chi tiết ngay trong biên bản.

## Các trường hợp / case biên
- **Xóa khi đã xác nhận** (đầu ca HOẶC cuối ca = true) → chặn: "Không thể xóa biên bản đã được xác nhận!".
- **Xóa bởi người không phải người tạo** (và không super user) → chặn: "Người tạo phiếu mới có thể xóa!".
- **Xóa biên bản của đơn vị khác** → coi như không tìm thấy (guard theo IdDonVi của người dùng).
- **Xác nhận cuối ca khi chưa xác nhận đầu ca** → trạng thái không hợp lệ; thứ tự bắt buộc Nháp → đầu ca → cuối ca.
- **Thêm/Xóa dòng chi tiết qua màn quản trị bởi tài khoản không phải super user** → "Bạn không có quyền thêm mới" / "Bạn không có quyền xóa".

## Ví dụ cụ thể có số
Cửa hàng B, ca sáng do anh Hùng trực:
- Anh Hùng (DV-KT) lập biên bản, nhập đầu ca: nhận **2.000.000đ** tiền mặt + 50 sản phẩm trưng bày → **Xác nhận đầu ca**. Biên bản chuyển trạng thái "Đã xác nhận đầu ca", khóa phần đầu ca.
- Cuối ca, đối chiếu quỹ còn **1.950.000đ** so với kỳ vọng 2.000.000đ → nhập **tiền thiếu cuối ca = 50.000đ**, chọn người nhận cuối ca là chị Mai → **Xác nhận cuối ca**. Ca đóng.
- Sau đó anh Hùng thử **Xóa** biên bản này → hệ thống chặn "Không thể xóa biên bản đã được xác nhận!".
- Một biên bản ca chiều mới lập (chưa xác nhận) do chị Mai tạo; anh Hùng bấm xóa → bị chặn "Người tạo phiếu mới có thể xóa!" (vì không phải người tạo).

## Hậu điều kiện
- Sau **Xác nhận đầu ca / cuối ca**: cờ trạng thái được set, phần tương ứng bị khóa sửa/xóa.
- Biên bản đã xác nhận trở thành chứng từ đối chiếu quỹ ca, không xóa được.
- *Cần product xác nhận:* hệ thống có sinh bút toán/đối chiếu doanh thu trong ca từ biên bản này không (không thấy ripple sang đơn hàng/kế toán trong code đọc được).
