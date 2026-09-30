# Luồng — Cấu hình Elasticsearch

## Tiền điều kiện
- Người dùng đăng nhập bằng tài khoản **nội bộ SaaS** có quyền hệ thống (HT-AD / HT-CSKH). Một số lệnh (xóa index, reindex full) siết quyền hơn (HT-AD / chỉ SuperUser).
- Hạ tầng sẵn sàng: cluster **Elasticsearch** chạy, **RabbitMQ** chạy (vì việc đánh index được đẩy qua hàng đợi để xử lý nền).
- Đơn vị cần đánh index đã được khai báo cấu hình ES (tên index từng bảng: contact, kho số, đơn logistic, đơn hàng, sản phẩm).

## Luồng dùng (vận hành — đánh lại index)
1. Vào **Hệ thống → Quản lý Elastic search**.
2. Chọn đơn vị (1 hoặc nhiều) — hoặc để trống nếu muốn áp tất cả đơn vị.
3. Tick loại dữ liệu muốn đánh lại: contact / kho số / đơn logistic / đơn hàng / sản phẩm.
4. Bấm **Reindex**.
5. Hệ thống **không tự đánh tại chỗ**: nó đẩy một "lệnh đánh index" (event) vào RabbitMQ cho từng đơn vị + từng loại.
6. Tiến trình nền (consumer ESDocument) nhận lệnh → đọc dữ liệu gốc từ DB → ghi lên Elasticsearch.
7. Kết quả mong đợi: sau khi tiến trình nền chạy xong, các màn tìm kiếm nhanh trả kết quả khớp dữ liệu gốc.

## Luồng cấu hình (admin — khai báo đơn vị dùng ES)
1. Thêm/sửa đơn vị dùng ES (lưu vào DB cấu hình): khai báo đơn vị nào bật ES, tên index từng loại.
2. Có thể đổi **phương thức tìm kiếm** của đơn vị (chọn dùng ES hay phương thức khác).
3. Xem danh sách đơn vị dùng ES qua bộ lọc.

## Các trường hợp / case biên
- **Danh sách đơn vị để trống** → coi như **áp cho tất cả đơn vị** (reindex toàn bộ shop của loại dữ liệu đó). Lệnh có chọn đơn vị → chỉ publish event cho các đơn vị được chọn.
- **Reindex sản phẩm** → publish 2 lệnh: index `sanpham` + `sanphamcha` trong cùng thao tác.
- **Reindex full** (`ReindexFullDonVi`) → **chặn nếu tài khoản không phải SuperUser** (`CurrentUser.IsSuperUser != true`). Nếu là SuperUser → đánh lại orderlogistic + contact + khoso + sanpham + sanphamcha cho mọi đơn vị.
- **Loại "order" trong lệnh hàng loạt** → đã bị tắt (comment) ở `ReindexAllByDonVi` / `ReindexFullDonVi`: tick "đơn hàng" trong các lệnh gói có thể **không** đánh index order như mong đợi → *cần product/kỹ thuật xác nhận hành vi đúng*.
- **Xóa index** → publish event xóa theo cờ từng loại; không xóa dữ liệu gốc, chỉ xóa bản index tìm kiếm.
- **Bấm reindex liên tục** → mỗi lần đẩy thêm event vào hàng đợi; không có khử trùng tại controller → tốn tài nguyên, không phá dữ liệu nhưng nên tránh.

## Ví dụ cụ thể có số
Đội vận hành xử lý ticket: shop X (1 đơn vị) báo "tìm contact không ra" dù có 3.500 contact.
- Admin hệ thống vào Quản lý Elastic search → chọn đúng đơn vị X → tick "contact" → bấm Reindex.
- Hệ thống publish **1 event** (EventIndexDocument, action=index, indexName=ES contact của X) vào RabbitMQ.
- Consumer nền đọc 3.500 contact của X từ DB → ghi 3.500 document lên Elasticsearch. Sau ~30 giây, tìm contact của X trả đủ kết quả.
- So sánh: nếu admin **để trống** danh sách đơn vị và tick "contact" → hệ thống publish event cho **toàn bộ N đơn vị** (vd 200 đơn vị) → nặng hơn rất nhiều, chỉ nên dùng khi cần.
- Reindex sản phẩm cho X: chọn X + tick "sản phẩm" → publish **2 event** (sanpham + sanphamcha), không phải 1.

## Hậu điều kiện
- **Không thay đổi dữ liệu gốc** (contact/đơn/sản phẩm trong DB nghiệp vụ giữ nguyên).
- Dữ liệu tìm kiếm trên Elasticsearch được **dựng lại/xóa** sau khi tiến trình nền xử lý xong (eventual — không tức thì).
- Cấu hình đơn vị dùng ES (thêm/sửa/đổi phương thức) ghi vào DB cấu hình ngay khi lưu.
