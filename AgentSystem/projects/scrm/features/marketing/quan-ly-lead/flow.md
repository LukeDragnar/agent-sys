# Luồng — Quản lý Lead

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị và chi nhánh.
- Có vai phù hợp: màn Hồ sơ khách hàng (lead) cần vai admin/marketing (AD/MKT/QL-MKT/CHIASO); Dashboard MKT cần admin tổng/chi nhánh hoặc quản lý/lead/nhân viên MKT; Danh sách Seeding cần admin hoặc quản lý/lead Sale & MKT.
- Đã có dữ liệu lead đổ về (contact tiếp nhận từ landing/quảng cáo) để hiển thị.
- (Tùy chọn) Đã khai báo nguồn dữ liệu/landing để lead gắn nguồn chính xác.

## Luồng dùng (xem danh sách lead)
1. Người dùng mở màn **Hồ sơ khách hàng** (lead MKT).
2. Nhập điều kiện lọc → hệ thống gọi `GetHoSoKhachHang_LeadMKT_Search` với scope vai Sale+MKT của user hiện tại → trả lưới lead theo phạm vi được phép xem.
3. Hệ thống đồng thời gọi `GetHoSoKhachHang_LeadMKT_TrungSo_Count` → hiển thị badge **số lead trùng số điện thoại** (theo vai MKT).
4. Người dùng bấm **Xuất Excel** (kiểu 1 hoặc kiểu 2) → nếu thành công, tải file `DanhSachContact.xlsx`; nếu service báo lỗi, trả thông báo lỗi (BadRequest).

## Luồng dùng (Dashboard Marketing — lịch sử thao tác)
1. Người dùng mở **Marketing → Dashboard**.
2. Chọn loại hoạt động (load từ danh mục `GetDMActivityLog`) + khoảng ngày.
3. Hệ thống gọi `GetUserActivitiesLog` → trả danh sách lịch sử thao tác dữ liệu của nhân viên theo đơn vị / khoảng ngày / loại hoạt động.

## Luồng cấu hình (Danh sách số Seeding)
1. Mở **Danh sách số Seeding**.
2. **Thêm mới** số → insert số seeding theo đơn vị.
3. **Tìm theo điều kiện** → lưới các số seeding đã khai báo.
4. **Xóa một** (`DeleteOneAsync`) hoặc **xóa nhiều** (`DeleteMutipleAsync`) theo id(s).
5. Khi có số mới đổ về luồng tạo contact / chia số → hệ thống gọi `CheckPhoneNumberSeeding/{phoneNumber}` trả true/false để loại số seeding khỏi lead thật.

## Luồng nhận lead tự động (lead intake — bổ sung Pha 2 — lead intake tự động)
> Đây là cách lead **đổ vào hệ thống** trước khi hiển thị ở màn Hồ sơ khách hàng. Cổng nhận **công khai (anonymous)**, không qua đăng nhập.
1. Khách điền form trên **landing page (LadiPage)** hoặc **website (WooCommerce)** → form bắn HTTP request tới cổng nhận, kèm mã định danh landing (`lid`) và mã nguồn (`sc`):
   - LadiPage: `POST`/`GET ReceiveData/ld-api/lid/{lid}/sc/{sc}`.
   - WooCommerce: `POST ReceiveDataWoocommerce/ld-api/lid/{lid}/sc/{sc}`.
2. Cổng nhận **ghi log toàn bộ request** rồi **parse theo content-type** (json / multipart / x-www-form-urlencoded / query string).
3. Dữ liệu hợp lệ → đẩy vào **hàng đợi (RabbitMQ)** để **service log-contact** xử lý → service này **tạo/ghép contact** trong SCRM (đây chính là lead xuất hiện ở màn Hồ sơ khách hàng).
4. Trong quá trình tạo contact / chia số, hệ thống gọi `CheckPhoneNumberSeeding` để **loại số seeding** khỏi lead thật; lead trùng số được badge trùng số ghi nhận.
5. Nếu service log cần **bắn lại** một request đã log (retry) → gọi ngược `ResendDataLogAsync` để dựng lại data và đẩy lại vào luồng.

## Case biên
- **Lead trùng số:** badge đếm trùng dựa trên truy vấn riêng (vai DV-MKT). Endpoint **lấy danh sách** trùng số gốc đã bị vô hiệu (chỉ còn Count) — UI lấy danh sách trùng số qua màn Contact (`GetDanhSachTrungSo`), không qua màn lead này.
- **Phạm vi xem theo vai:** user thuần MKT chỉ thấy lead trong phạm vi mình; admin thấy rộng hơn. Tập lead trả về phụ thuộc role gán cứng `SALE_AND_MKT` trong code.
- **Xuất Excel lỗi:** khi service xuất trả `Success=false` → trả về thông báo lỗi (BadRequest), không tải được file. (Khác convention ResponseResult của các màn khác.)
- **Số rỗng / không tồn tại khi check Seeding:** `CheckPhoneNumberSeeding` trả false → số được coi là số bình thường, vẫn vào luồng lead.
- **Xóa nhiều số Seeding với danh sách rỗng:** không có id nào → không xóa gì (no-op).
- **Dashboard — biểu đồ thống kê:** một số method chart cũ (`TimTheoDieuKien`, `GetStatisticsLast7DayOfLanding`) ở controller MKT-dashboard (service Contact) đã bị vô hiệu → biểu đồ landing/thống kê hiện lấy ở phần **Báo cáo Marketing** (service Report), không ở Dashboard này.

### Case biên — nhận lead tự động (bổ sung Pha 2 — lead intake tự động)
- **Request rỗng / không có giá trị:** cổng nhận ghi log lỗi rồi trả **400** với message đúng theo từng content-type (query/json/multipart/form-url-encoded) → không tạo lead.
- **Exception khi xử lý POST:** action POST ghi log DB rồi **nuốt lỗi và trả Ok** → request không lỗi về phía landing, nhưng nếu xử lý hỏng thì **mất dấu vết** (chỉ còn log DB) → lead có thể không vào mà landing vẫn báo thành công.
- **Cổng nhận công khai, không xác thực nguồn:** mọi endpoint nhận lead là **anonymous, không verify chữ ký/token** → bất kỳ ai biết `lid`/`sc` đều bắn được **lead giả/rác** vào hệ thống (spam contact, nhiễu báo cáo). Đây là lỗ hổng đã biết — xem dev.
- **Số seeding lọt vào landing:** nếu khách (hoặc bot) điền đúng một số đã khai báo Seeding → khi tạo contact/chia số, `CheckPhoneNumberSeeding` trả true → **loại khỏi lead thật**.
- **Retry/bắn lại:** khi service log gọi `ResendDataLogAsync`, mẫu mới có cờ `isWoocommerce` quyết định đi nhánh Woo hay Ladi; nếu data dựng lại sai cấu trúc → có thể tạo lead thiếu trường.

## Ví dụ cụ thể có số
Đơn vị X chạy quảng cáo, trong ngày đổ về **520 lead**.
- Nhân viên MKT Hằng (vai DV-MKT) mở Hồ sơ khách hàng: hệ thống chỉ trả lead trong phạm vi của Hằng, ví dụ **180 lead**; badge trùng số hiển thị **24** (24 bản ghi cùng số đã có trước đó).
- Trong 520 lead có **15 số** trùng với Danh sách Seeding (số test nội bộ) → các luồng tạo contact/chia số gọi `CheckPhoneNumberSeeding` trả true cho 15 số này → loại ra, chỉ còn **505 lead thật** vào chia số.
- Quản lý MKT lọc khoảng ngày = hôm nay, xuất Excel kiểu 1 → tải `DanhSachContact.xlsx` chứa 180 dòng theo phạm vi của user xuất.
- Trên Dashboard, chọn loại hoạt động "Cập nhật contact", từ ngày 01–15 → trả ví dụ **62 dòng** lịch sử thao tác của nhân viên.

**Ví dụ nhận lead tự động (bổ sung Pha 2):** Landing "Khuyến mãi tháng 6" (`lid=LD-0612`, `sc=FB-AD`) trong 1 giờ có **530 form gửi về**.
- **8 form** gửi rỗng/thiếu (bot bấm submit không điền) → cổng nhận trả 400, **không tạo lead** → còn 522.
- **522 request** parse hợp lệ → đẩy RabbitMQ → service log-contact tạo contact → **522 lead** gắn nguồn `FB-AD` / landing `LD-0612`.
- Trong 522 có **2 số** trùng Danh sách Seeding (nhân viên test landing) → `CheckPhoneNumberSeeding` trả true → loại → còn **520 lead thật** vào chia số.
- 24 lead trùng số với khách cũ → vẫn tạo nhưng badge trùng số ở màn Hồ sơ khách hàng cộng thêm **24**.

## Hậu điều kiện
- Xem/đếm/Dashboard: **không thay đổi dữ liệu** lead (read-only).
- Xuất Excel: tải file, không sửa lead.
- Thêm/Xóa số Seeding: ghi/xóa bản ghi `SeedingInfo` theo đơn vị; ảnh hưởng các luồng tạo contact/chia số sau đó (số seeding bị loại).
- Nhận lead tự động _(bổ sung Pha 2)_: ghi **log request** (LogContact) + đẩy message RabbitMQ; sau đó service log-contact **tạo/ghép contact** → lead mới xuất hiện ở màn Hồ sơ khách hàng và vào luồng chia số (đã loại seeding).
