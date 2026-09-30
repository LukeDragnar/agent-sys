# Luồng — Quản lý đa phương tiện

## Tiền điều kiện
- Người dùng đã đăng nhập (admin hệ thống).
- Thêm/Sửa cần quyền `HT_Luu_DaPhuongTien`; Xóa cần `HT_Xoa_DaPhuongTien`.
- (Tùy chọn) đã có sẵn nhóm để chọn khi thêm; nếu chưa, có thể gõ nhóm mới ngay trong form.

## Luồng dùng (user — Đa phương tiện)
1. Mở **Quản trị → Đa phương tiện** (route `quantri/daphuongtien`).
2. Chọn **Nhóm** và/hoặc nhập **Tên** vào ô tìm kiếm.
3. Bấm **Tìm kiếm** → gọi `TimTheoDieuKien`, trả bảng (tên, nhóm, thứ tự hiển thị, đường dẫn, sử dụng, ngày cập nhật + người cập nhật) có phân trang.
4. Bấm **Thêm mới** → mở popup form → điền Tên/Nhóm/Thứ tự/Đường dẫn (bắt buộc) + Mô tả + Sử dụng → **Lưu** (`Luu`) → đóng popup, tải lại bảng.
5. Bấm **Sửa** trên một dòng → popup nạp sẵn dữ liệu (`TimTheoId`) → sửa → **Lưu**.
6. Mở menu (▾) → **Xóa** → hộp xác nhận "Bạn có chắc chắn muốn xóa?" → Đồng ý → gọi `Xoa(id)` → tải lại bảng.

## Luồng cấu hình (admin)
- Không có luồng cấu hình riêng tách biệt — toàn bộ là CRUD danh mục như trên.

## Các trường hợp / case biên
- **Input rỗng / thiếu trường bắt buộc:** form validate `Validators.required` cho Tên/Nhóm/Thứ tự/Đường dẫn → bấm Lưu khi invalid thì dừng, không gọi API.
- **Tên/Mô tả có khoảng trắng đầu-cuối:** `checkSpace` chặn, báo lỗi "Tên cấu hình không được có khoảng trắng…" / "Mô tả không được có khoảng trắng…".
- **Sửa (có id):** body gắn `id`, hiển thị thông báo "Cập nhật thành công"; **Thêm (id null/undefined):** hiển thị "Thêm thành công".
- **Sử dụng:** form lưu `'1'`/`'0'` (string) rồi convert sang `true`/`false` trước khi gửi.
- **Xóa qua GET:** backend `Xoa` dùng HTTP GET (`api/DaPhuongTien/Xoa/{id}`) — không idempotent-safe; rủi ro nếu bị prefetch/crawler (xem dev.md), nhưng UI luôn có bước xác nhận.
- **Endpoint không phân quyền:** Tìm/Xem/Lấy nhóm không gắn `[SBAuthorize]` key → bất kỳ user đăng nhập nào gọi API đều đọc được dữ liệu.

## Ví dụ cụ thể có số
Admin khai báo 3 mục cho nhóm "Banner trang chủ":
- Thêm "Banner Tết" — nhóm `Banner trang chủ` — thứ tự `1` — đường dẫn `https://cdn.x/tet.png` — Sử dụng `Có` → Lưu OK ("Thêm thành công").
- Thêm "Banner KM" với Tên = ` Banner KM ` (có khoảng trắng đầu/cuối) → báo lỗi khoảng trắng, không lưu.
- Sửa "Banner Tết" đổi thứ tự `1` → `3` → Lưu OK ("Cập nhật thành công").
- Lọc nhóm = `Banner trang chủ` → bảng trả 1 dòng (Banner Tết, sử dụng ✔, thứ tự 3).
- Mở menu → Xóa "Banner Tết" → xác nhận → bảng còn 0 dòng nhóm này.

## Hậu điều kiện
- Bản ghi đa phương tiện được thêm/sửa/xóa trong DB; ngày cập nhật + người cập nhật được ghi lại (hiển thị ở cột Ngày cập nhật).
- Danh sách nhóm (dropdown) cập nhật theo các nhóm còn tồn tại.
