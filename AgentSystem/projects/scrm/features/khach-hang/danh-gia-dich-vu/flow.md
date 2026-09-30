# Luồng nghiệp vụ — Nhận xét & đánh giá dịch vụ

## Tiền điều kiện
- Có **đơn hàng** (offline `Order` hoặc online `OrderLogistic`) thuộc một đơn vị/chi nhánh; đơn có dịch vụ và nhân viên phục vụ.
- Đã sinh được **link đánh giá rút gọn** (ShortLink) gắn với đơn đó để gửi khách.
- Để xem/xử lý danh sách: người dùng đăng nhập có quyền `DV-AD-TONG / DV-AD-CN / DV-AD-MCN / DV-QLDV`.
- Để cấu hình danh mục theo đơn vị: là **admin đơn vị** (`IsAdmin`).

## Luồng dùng (khách đánh giá → shop xem/xử lý)

**A. Sinh link & khởi tạo bản ghi đánh giá**
1. Shop/Hệ thống tạo yêu cầu đánh giá cho 1 đơn → gọi sinh ShortLink (loại đối tượng = đơn offline / đơn online / mã đánh giá).
2. Khi khách (hoặc shop) mở thông tin đơn lần đầu, hệ thống **tạo bản ghi `KhachHangDanhGia`** nếu chưa có: sinh `MaDanhGia` duy nhất, gắn đơn vị/chi nhánh/đơn hàng, lấy mã hóa đơn + id khách từ Order/OrderLogistic, lấy tên chi nhánh từ Core.

**B. Khách chấm điểm**
3. Khách mở link → màn "Phản hồi dịch vụ" hiện đơn + danh sách dịch vụ/nhân viên (gộp người tư vấn nếu thiếu nhân viên).
4. Khách chọn **mức hài lòng chung** (1–3), tuỳ chọn chấm riêng từng nhân viên + nhận xét, rồi **Gửi**.
5. Hệ thống lưu: cập nhật `MucDoHaiLong` + `PhanHoiYKien` ở bản cha; tạo/cập nhật các dòng `KhachHangDanhGiaDetail` (mỗi dịch vụ–nhân viên 1 dòng). Nếu nhân viên không được chấm riêng thì lấy theo mức chung.
6. Sau khi lưu thành công (trong transaction): ghi **lịch sử tương tác "Đánh giá"** vào hồ sơ khách + **publish thông báo** về chi nhánh.

**C. Shop xem & xử lý**
7. Admin/CSKH vào "Phản hồi dịch vụ", hệ thống gọi `GetListPhanHoiDichVu`: lọc theo đơn vị (+ chi nhánh nếu không phải admin/CSKH/superuser), mức hài lòng, ngày, trạng thái xử lý, từ khoá; chỉ lấy bản ghi **đã có mức hài lòng**.
8. Trả về danh sách + thống kê (rất hài lòng/hài lòng/chưa hài lòng/tổng đánh giá) + **tổng số khách nhận link** (gọi sang Booking).
9. Shop bấm **đánh dấu đã xử lý** (`UpdatePhanHoiDichVu` → set `IsProcessed`), hoặc **Xuất Excel** (`ExportExcelPhanHoiDichVu`, dùng lại cùng filter, cap 5.000 dòng).

**D. Quản lý danh mục đánh giá (admin đơn vị)**
10. `TimTheoDieuKienDsDetailForUnit` liệt kê các nhận xét nhanh theo đơn vị (lọc kiểu loại/từ khoá).
11. Thêm/sửa (`Insert/UpdateKhDanhGiaDetailForUnit`): kiểu loại phải ∈ {1,2,3}, tên bắt buộc, tên không trùng trong đơn vị. Sửa chỉ đổi được **tên** (kiểu loại giữ nguyên — code đổi kiểu loại đã comment out).
12. **Khởi tạo** (`InitDataForUnit`): copy bộ danh mục mẫu của ngành nghề (`IdDonVi == Guid.Empty`) về đơn vị, bỏ qua tên đã có (so sánh lower-case).

## Case biên
- **Đơn không tìm thấy / Order service lỗi:** trả "Không tìm thấy thông tin đơn hàng".
- **Mã đánh giá không tồn tại** khi khách gửi: chặn lưu.
- **Khách chưa chọn mức hài lòng** (`ValueDanhGiaChung == null/<=0`): báo "Vui lòng chọn đánh giá".
- **Đánh giá lại đơn đã đánh giá:** code update lại detail theo (dịch vụ, nhân viên) đã tồn tại — không tạo trùng. (Phần chặn "đã đánh giá rồi" đang comment out → hiện cho gửi lại.)
- **Nhân viên thiếu trên dòng dịch vụ:** nếu `IdNhanVien` null nhưng có người tư vấn → lấy người tư vấn; nếu trống hẳn → thêm 1 dòng nhân viên rỗng để khách vẫn chấm được dịch vụ.
- **Lưu lỗi giữa chừng:** toàn bộ phần lưu cha + chi tiết nằm trong **transaction**, lỗi thì rollback, không ghi nửa vời.
- **Lọc trạng thái xử lý:** `IsProcessed == true` → chỉ lấy đã xử lý; `== false` → lấy cả `false` lẫn `null` (chưa xử lý).
- **Phân quyền chi nhánh:** không phải admin/CSKH/superuser → ép `IdChiNhanh = chi nhánh của user`.
- **Danh mục: tên trùng / kiểu loại sai:** chặn với thông báo tương ứng.

## Ví dụ có số
Spa "Hồng" (đơn vị D), chi nhánh Quận 1. Trong tháng gửi **120 link** đánh giá; có **80 khách** thật sự chấm điểm.
- Rất hài lòng: 50, Hài lòng: 20, Không hài lòng: 10 → **Tổng đánh giá = 80**, **Tổng link = 120**.
- Đơn HD0098: khách chọn mức chung = **Hài lòng (2)**; chấm riêng KTV An = **Rất hài lòng (1)** + nhận xét "khéo tay", còn KTV Bình không chấm riêng → hệ thống lưu detail Bình = **2** (theo mức chung).
- Quản lý lọc "Chưa hài lòng" + "Chưa xử lý" → ra 10 đơn; liên hệ xong bấm đã xử lý 6 đơn, còn 4 đơn ở trạng thái chưa xử lý.
- Bấm Xuất Excel với cùng filter → file 10 dòng + bảng tổng hợp (120 / 80 / 50 / 20 / 10).

## Hậu điều kiện
- Bản ghi `KhachHangDanhGia` có `MucDoHaiLong` + `PhanHoiYKien`; các dòng `KhachHangDanhGiaDetail` tương ứng dịch vụ–nhân viên.
- Hồ sơ khách có thêm 1 dòng **lịch sử tương tác "Đánh giá"**.
- Chi nhánh nhận **thông báo** khách vừa đánh giá.
- `IsProcessed` phản ánh trạng thái xử lý của shop.
- Danh mục đánh giá của đơn vị được cập nhật (nếu có chỉnh).
