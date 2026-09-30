# Luồng — Mẫu & quản lý thông báo

## Tiền điều kiện
- Admin hệ thống đăng nhập; thao tác mẫu yêu cầu role DV-AD-TONG (riêng "Danh sách nhóm thông báo" chỉ cần login).
- Đã có nhóm thông báo (`NotiGroupCode`) và danh sách mã thông báo để chọn khi cấu hình.

## Luồng dùng (admin — Mẫu thông báo)
1. Mở **Thông báo → Quản lý mẫu thông báo** (bản admin: route `notification/ad-mau-thongbao`, "Cấu hình mẫu thông báo").
2. Lọc theo **Mã** (chức năng), **Tên**, **Nhóm** → **Tìm kiếm** → `TimTheoDieuKien` trả danh sách mẫu (mã, tên, mẫu thông báo, nhóm, danh sách vai trò gửi, gửi sau (giây), sử dụng, ngày cập nhật).
3. Xem chi tiết: `TimTheoMa?code=` → trả mẫu theo Code; null → báo `MESS_NotFound`.
4. **Sửa**: `onEdit(code)` mở popup `AdminNotiTemplateEditComponent` → `CapNhatThongTin` (validate ModelState).
5. **Xóa**: `onDelete(code)` confirm → `XoaTheoMa(code)` / `Xoa/{code}`.
6. Combo dữ liệu: `DanhSachMaThongBao` (mã), `DanhSachNhomThongBao` (nhóm).

## Luồng dùng (admin — Thông báo đẩy)
1. Mở **Quản trị → Quản lý thông báo** (`admin-thongbao`) hoặc **bản hệ thống** (`admin-thongbao-hethong`, route `quantri/thongbao-hethong`).
2. Lọc theo tiêu đề / từ ngày / đến ngày / kiểu sử dụng → **Tìm kiếm**.
3. **Thêm mới** (`onAddNew`): popup soạn tiêu đề, hình ảnh, link, sử dụng → lưu.
4. **Sửa**/**Xóa**: chỉ hiển thị khi `daGui = false` (chưa gửi).

## Luồng hệ thống (dùng mẫu để gửi)
1. Sự kiện nghiệp vụ xảy ra → `NotificationConfig.WebPatternCode`/`WebPatternCodeNext` trỏ tới Code mẫu.
2. Service dựng `NotificationInfo` từ nội dung mẫu (theo Code) → gửi qua `NotificationsController.Create`.

## Các trường hợp / case biên
- **Xóa mẫu còn được tham chiếu:** `Xoa/{code}` không kiểm tra ràng buộc với `NotificationConfig.WebPatternCode` → cấu hình trỏ tới mẫu đã xóa → thông báo "mồ côi" (rỗng nội dung). Cần kiểm tra trước khi xóa.
- **Tra mã không tồn tại:** `TimTheoMa` trả `MESS_NotFound`.
- **Thông báo đã gửi:** `daGui = true` → ẩn nút Sửa/Xóa, không thao tác được.
- **Thêm mới mẫu trên FE:** nút "Thêm mới mẫu thông báo" (Action THEMMOI) trên admin, nhưng controller NotiTemplate cấp hệ thống **không có endpoint ThemMoi** (chỉ Update/Xóa) → đường tạo thực tế đi qua bản admin/sanbox khác hoặc seed (cần xác nhận).
- **Khoảng ngày sai (từ ngày > đến ngày):** màn quản lý thông báo báo lỗi `DATE_ERROR`.
- **Nhóm thông báo combo:** `DanhSachNhomThongBao` chỉ cần login (không gắn role) → khác các action mẫu khác (DV-AD-TONG).

## Ví dụ cụ thể có số
Mẫu `NOTI_NHAC_LICH_HEN`: nội dung "Bạn có lịch hẹn lúc {gio} ngày {ngay}", nhóm "Lịch hẹn", gửi sau 0 giây, danh sách gửi = [DV-SALE, DV-AD-CN], sử dụng = Có.
- Admin mở "Cấu hình mẫu thông báo", lọc Mã = `NOTI_NHAC_LICH_HEN` → thấy 1 mẫu.
- Sửa: đổi "gửi sau" 0 → 300 (5 phút) → Lưu OK.
- Một khách đặt lịch hẹn lúc 14:00 → hệ thống match `NotificationConfig` trỏ Code `NOTI_NHAC_LICH_HEN` → dựng nội dung từ mẫu → gửi cho role trong danh sách (sau độ trễ cấu hình).
- Sau đó admin xóa mẫu `NOTI_NHAC_LICH_HEN` (theo Code) nhưng quên gỡ NotificationConfig → lần đặt lịch tiếp theo, thông báo bị rỗng nội dung (mồ côi).

## Hậu điều kiện
- Mẫu được cập nhật/xóa trong DB (theo Code).
- Thông báo đẩy được tạo/sửa/xóa (khi chưa gửi) hoặc đánh dấu "Đã gửi" sau khi gửi.
- Cấu hình gửi tham chiếu Code mẫu — thay đổi mẫu ảnh hưởng nội dung thông báo sinh ra.
