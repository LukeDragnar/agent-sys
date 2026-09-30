---
alias: ["mẫu thông báo", "template thông báo", "quản lý thông báo", "mã thông báo", "cấu hình mẫu thông báo", "noti template", "thông báo hệ thống"]
module: he-thong
trang_thai: Core (nội bộ super-admin)
quyen: "Mẫu thông báo (NotiTemplate): hầu hết action yêu cầu DV-AD-TONG; riêng 'Danh sách nhóm thông báo' chỉ cần đăng nhập. Quản lý thông báo đẩy (admin-thongbao/hethong): admin hệ thống. Cần product xác nhận role chi tiết cho phần thông báo đẩy."
goi: cần product xác nhận
phu_thuoc: ["Cấu hình gửi thông báo — tham chiếu Mã (Code) của mẫu để chọn nội dung", "Dịch vụ gửi thông báo — dùng nội dung mẫu để dựng thông báo gửi đi"]
link_flow: ./flow.md
---

# Mẫu & quản lý thông báo

## Là gì
Đây là nơi **quản lý nội dung thông báo của hệ thống** ở cấp quản trị, gồm hai phần liên quan:
- **Mẫu thông báo (template):** các "khuôn" nội dung/tiêu đề thông báo, định danh bằng **Mã (Code)**, dùng để hệ thống sinh ra thông báo cho từng sự kiện (ví dụ: nhắc lịch hẹn, xác nhận đơn…). Mỗi mẫu gắn nhóm thông báo, danh sách vai trò nhận, độ trễ gửi (gửi sau N giây), trạng thái sử dụng.
- **Quản lý thông báo (đẩy):** các thông báo/tin tức được tạo và đẩy tới người dùng (tiêu đề, hình ảnh, link, ngày gửi), quản lý bản hệ thống và bản đẩy thường.

Phục vụ **admin hệ thống (super-admin nội bộ SaaS)**. Khách hàng cuối không quản lý phần này; họ chỉ **nhận** thông báo do mẫu/cấu hình sinh ra.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem/lọc danh sách mẫu thông báo | ✅ | DV-AD-TONG |
| Xem chi tiết mẫu theo Mã (Code) | ✅ | DV-AD-TONG |
| Sửa (cập nhật) mẫu | ✅ | DV-AD-TONG |
| Xóa mẫu (theo Mã) | ✅ | DV-AD-TONG |
| **Thêm mới mẫu** | ⚠️ | FE admin có nút "Thêm mới mẫu thông báo"; **controller NotiTemplate cấp hệ thống không có endpoint ThemMoi** — tạo mẫu qua kênh admin/sanbox khác hoặc seed DB (cần dev/product xác nhận đường tạo) |
| Lấy danh sách mã / nhóm thông báo (combo) | ✅ | mã: DV-AD-TONG; nhóm: chỉ cần đăng nhập |
| Thêm/Sửa/Xóa thông báo đẩy (tin tức/thông báo) | ✅ | admin hệ thống; chỉ thao tác khi **chưa gửi** (đã gửi thì khóa) |
| Gửi thông báo đã tạo | ✅ | qua dịch vụ gửi thông báo (đánh dấu "Đã gửi") |

## Để làm gì / cho ai
- **Admin hệ thống:** chuẩn hóa nội dung thông báo toàn hệ thống (mẫu theo Code), cấu hình ai nhận, gửi trễ bao lâu; soạn và đẩy thông báo/tin tức tới người dùng.
- **Hệ thống (tự động):** khi một sự kiện xảy ra, lấy mẫu theo Code → dựng nội dung → gửi qua kênh thông báo.

## Cách dùng
**Mẫu thông báo:**
1. Vào **Thông báo → Quản lý mẫu thông báo** (bản admin: "Cấu hình mẫu thông báo").
2. Lọc theo **Mã thông báo** / **Tên** / **Nhóm** → **Tìm kiếm**.
3. Bấm **Sửa** trên một mẫu để chỉnh nội dung/tiêu đề/nhóm/danh sách vai trò nhận/gửi sau (giây)/trạng thái sử dụng.
4. Mở menu → **Xóa** (theo Mã) khi không dùng nữa.

**Quản lý thông báo đẩy:**
1. Vào **Quản trị → Quản lý thông báo** (hoặc bản hệ thống).
2. Lọc theo tiêu đề / khoảng ngày → **Tìm kiếm**.
3. **Thêm mới** thông báo (tiêu đề, hình ảnh, link, sử dụng…), **Sửa**/**Xóa** — chỉ khi **chưa gửi**.

## Cần chú ý
- **Mẫu định danh bằng Mã (Code), không phải Id** — tra/xóa theo Code. Xóa mẫu mà cấu hình gửi (NotificationConfig) còn trỏ tới Mã đó → thông báo bị "mồ côi" (rỗng nội dung). Kiểm tra trước khi xóa.
- **Thông báo đã gửi thì khóa** — không sửa/xóa được (nút Sửa/Xóa ẩn khi `daGui = true`).
- Mô tả/Swagger của một số API mẫu thông báo bị **copy nhầm** từ controller chi nhánh ("Tìm kiếm chi nhánh"…) — chỉ là text sai, không ảnh hưởng nghiệp vụ (xem dev.md).
- Phần "Thêm mới mẫu" trên FE admin chưa map đúng endpoint ThemMoi của controller cấp hệ thống — cần dev xác nhận đường tạo mẫu.

## Ảnh hưởng tới
- **Cấu hình gửi thông báo (NotificationConfig.WebPatternCode):** trỏ tới Mã mẫu; đổi/xóa mẫu ảnh hưởng nội dung thông báo gửi đi.
- **Toàn bộ thông báo hệ thống gửi cho người dùng** — lấy nội dung từ mẫu này.
