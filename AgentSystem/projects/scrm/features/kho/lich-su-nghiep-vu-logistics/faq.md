# FAQ — Lịch sử nghiệp vụ logistics

> Draft — cần bổ sung từ ticket khách thật.

### 1. Màn hình này dùng để làm gì?
Để **tra cứu lịch sử xử lý tự động của một đơn giao vận**: sau khi đơn đổi trạng thái giao hàng, hệ thống chạy các nghiệp vụ kèm theo (cộng/trừ chi tiêu, điểm, công nợ của khách). Màn hình cho biết các nghiệp vụ đó đang **chờ chạy, thành công hay thất bại** theo từng đơn.

### 2. "Pending" hiển thị số khác 0 thì có phải đơn bị lỗi không?
Không. **Pending = đang chờ tới giờ chạy**. Hệ thống cố tình hẹn xử lý trễ một khoảng để chờ trạng thái đơn ổn định. Đơn vừa đổi trạng thái thấy Pending là bình thường, chỉ cần đợi. Cái cần để ý là cột **Thất bại**.

### 3. Khách báo "đơn đã giao thành công mà chưa thấy cộng điểm / chưa trừ nợ", tra ở đâu?
Vào màn hình này, gõ **mã đơn**, bấm **Tìm kiếm**, rồi bấm **Xem**. Trong chi tiết sẽ thấy nghiệp vụ tương ứng đang ở trạng thái nào: còn **Pending** (chờ chạy), **Thất bại** (có ghi chú lỗi), hay **Bỏ qua**.

### 4. "Thành công / Thất bại / Bỏ qua" khác nhau thế nào?
- **Thành công:** nghiệp vụ đã chạy xong và áp dụng vào dữ liệu khách.
- **Thất bại:** đã thử nhiều lần nhưng vẫn lỗi — xem ghi chú lỗi để biết nguyên nhân.
- **Bỏ qua:** bị thay thế bởi một bản ghi mới hơn cùng nghiệp vụ cho đơn đó (ví dụ đơn đổi trạng thái lần nữa), nên bản cũ không cần chạy.

### 5. "Hướng Tăng / Giảm" nghĩa là gì?
Là chiều tác động của nghiệp vụ lên giá trị của khách: **Tăng** (ví dụ cộng chi tiêu/điểm khi đơn giao thành công) hoặc **Giảm** (ví dụ trừ lại khi đơn bị hoàn). Khi một đơn có cả tăng lẫn giảm bằng nhau, hệ thống có thể triệt tiêu để không xử lý thừa.

### 6. Vì sao tìm theo ngày mà không thấy đơn cũ?
Bộ lọc ngày lọc theo **ngày tạo đơn**, không phải ngày chạy nghiệp vụ. Nới rộng khoảng ngày ra để bao trùm ngày đơn được tạo.

### 7. Tôi có sửa được trạng thái nghiệp vụ ở đây không?
Với người dùng thường, đây là màn hình **tra cứu**. Việc chạy lại một nghiệp vụ đang lỗi là thao tác **dành cho người có quyền vận hành/CSKH** — *cần product xác nhận về quyền và cách hiển thị nút thao tác.*

### 8. Một đơn hiển thị Pending mãi không chuyển sang Thành công thì sao?
Báo cho bộ phận kỹ thuật kèm **mã đơn** và **tên nghiệp vụ**. Có thể nghiệp vụ đang chờ điều kiện chưa thỏa, hoặc đang bị treo cần xử lý lại. *Quy trình eskalation cụ thể cần product xác nhận.*
