# Luồng — Cấu hình chung hệ thống

## Tiền điều kiện
- Người dùng đăng nhập bằng tài khoản nội bộ super-admin có quyền hệ thống (gợi ý HT-AD). Xóa tham số cấu hình yêu cầu **SuperUser**.
- Bảng cấu hình hệ thống (ConfigSystem) và danh sách domain CORS (DomainCors) đã tồn tại trong hệ thống.

## Luồng dùng (quản lý cấu hình hệ thống)
1. Vào **Quản trị → Quản lý cấu hình** (`{domain}/quantri/quanLyCauHinh`).
2. Xem danh sách (lưới) hoặc cây theo nhóm + sort. Mở 1 dòng theo Id để sửa.
3. Thêm mới: nhập mã + giá trị + nhóm → hệ thống set người tạo/ngày tạo → lưu.
4. Sửa: chọn bản ghi → đổi giá trị → set người cập nhật/ngày cập nhật → lưu (không tồn tại → báo không tìm thấy).
5. Xóa: chỉ SuperUser; check tồn tại → xóa.
- Service/màn khác đọc 1 config theo mã (vd `IS_SHOW_MOBILE`, `HT_TK_NGANHANG`) để áp hành vi.

## Luồng dùng (quản lý domain CORS)
1. Vào **Quản trị → Quản lý domain CORS**.
2. Xem danh sách (lấy tất cả / tìm theo điều kiện) hoặc xem chi tiết theo Id.
3. Thêm/sửa/xóa domain → sau mỗi thao tác hệ thống **gọi cập nhật lại cấu hình CORS toàn cục** ngay (refresh whitelist, không cần restart).

## Luồng đọc công khai (config app)
- Giao diện gọi `GET /api/ConfigSystem/GetConfigApp` (anonymous) khi khởi động để lấy config phía client (vd Google Map key) — không cần đăng nhập.

## Các trường hợp / case biên
- **Xóa tham số cấu hình bởi tài khoản không phải SuperUser** → bị chặn (giới hạn qua phân quyền, không hardcode trong code).
- **Sửa tham số không tồn tại** → trả 404 (không tìm thấy bản ghi).
- **Thêm/sửa/xóa domain CORS** → mỗi thao tác đều kéo theo `UpdateLaiDomainGlobal` để áp ngay danh sách mới.
- **`UpdateLaiDomainGlobal` ném lỗi sau khi đã ghi DB domain** → **không có rollback** → có thể lệch giữa DB domain và cache CORS toàn cục (ghi rõ dev.md). *Cần kỹ thuật lưu ý.*
- **Xóa domain đang dùng** → website tương ứng bị chặn gọi API ngay (vì áp tức thì).
- **`GetConfigApp` anonymous** → key map lộ public (chấp nhận vì là client key); KHÔNG được trả secret server-side qua endpoint này.

## Ví dụ cụ thể có số
Đội vận hành mở CORS cho 1 website triển khai mới.
- Vào Quản lý domain CORS → danh sách hiện đang có 12 domain.
- Bấm Thêm → nhập domain mới → lưu. Hệ thống insert 1 record (giờ 13 domain) → gọi `UpdateLaiDomainGlobal` → CORS toàn cục áp 13 domain ngay → website mới gọi API được.
- Một tuần sau, gỡ 1 domain cũ → xóa → còn 12 → cập nhật CORS toàn cục → domain cũ bị chặn ngay.
- Cấu hình hệ thống: SuperUser đổi tham số `IS_SHOW_MOBILE` từ `1` → `0` → các màn đọc cờ này chuyển sang ẩn điện thoại (ảnh hưởng nhiều màn cùng lúc).

## Hậu điều kiện
- Tham số cấu hình ghi vào DB ngay khi lưu; mọi module đọc config sẽ nhận giá trị mới (tùy cache của từng nơi).
- Danh sách domain CORS ghi vào DB + **cấu hình CORS toàn cục được cập nhật lại** sau mỗi thay đổi.
- `GetConfigApp` chỉ đọc, không sinh side-effect dữ liệu.
