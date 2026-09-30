---
controller: ThongBaoHeThongController
module: he-thong
service: Notification
man_hinh_ui: "QuanTri (sandbox) > Thông báo hệ thống: list + form thêm/sửa/xóa; FE người dùng cuối gọi GetForUser/KiemTraMoi để hiển thị thông báo hệ thống"
muc_dich: "CRUD thông báo do quản trị hệ thống (HT-AD) soạn để phát cho toàn bộ người dùng, và API cho FE lấy/đếm thông báo hệ thống mới của user hiện tại."
alias: ["ThongBaoHeThong", "thông báo hệ thống", "system notification", "thông báo admin"]
auth: mixed
state_machine: []
deprecated: false
entities: [ThongBaoHeThong]
stored_procs: []
depends: ["NotificationDataContext (SQL)", "IUserPrincipalService (CurrentUser)"]
cross_module_in: ["FE QuanTri/Thông báo hệ thống gọi CRUD (HT-AD)", "FE người dùng gọi GetForUser/KiemTraMoi"]
cross_module_out: []
---

# ThongBaoHeThongController — Quản lý thông báo hệ thống

Quản trị hệ thống (role HT-AD) soạn thông báo cấp hệ thống (route `api/QuanTriArea/ThongBaoHeThong`). FE người dùng cuối gọi `GetForUser` để lấy danh sách và `KiemTraMoi` để đếm/đánh dấu thông báo mới.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách | List/lọc | POST `api/QuanTriArea/ThongBaoHeThong/TimTheoDieuKien` | `ThongBaoHeThong_TimTheoDieuKien` (HT-AD) | public | — | Lấy danh sách thông báo hệ thống theo filter. | EF | — |
| 2 | Thêm mới | Lưu | POST `api/QuanTriArea/ThongBaoHeThong/ThemMoi` | `ThongBaoHeThong_ThemMoi` (HT-AD) | public | — | Validate ModelState; insert thông báo, gắn CurrentUser. | EF | — |
| 3 | Cập nhật | Lưu | POST `api/QuanTriArea/ThongBaoHeThong/CapNhatThongTin` | `ThongBaoHeThong_CapNhatThongTin` (HT-AD) | public | — | Validate ModelState; update thông báo, gắn CurrentUser. | EF | — |
| 4 | Xóa | Xóa | POST `api/QuanTriArea/ThongBaoHeThong/Xoa/{id}` | `ThongBaoHeThong_Xoa` (HT-AD) | public | — | Tìm theo id; không tồn tại → lỗi NotFound; có → xóa. | EF | — |
| 5 | Xem chi tiết | — | GET `api/QuanTriArea/ThongBaoHeThong/TimTheoId/{id}` | (SBAuthorize, không key) | public | — | Get 1 thông báo theo id. | EF | — |
| 6 | Lấy cho user | — | POST `api/QuanTriArea/ThongBaoHeThong/GetForUser` | (SBAuthorize, không key) | public | — | Lấy danh sách thông báo hệ thống dành cho CurrentUser theo filter. | EF | — |
| 7 | Kiểm tra mới | — | GET `api/QuanTriArea/ThongBaoHeThong/KiemTraMoi` | (SBAuthorize, không key) | public | — | Kiểm tra/đếm thông báo hệ thống mới cho CurrentUser. | EF | — |

## Quan hệ
- **Dữ liệu (entities):** `ThongBaoHeThong` (NotificationDataContext / SQL).
- **Gọi sang:** không (CRUD nội bộ service Notification).

## Lỗi đã biết / lưu ý kỹ thuật
- Endpoint chi tiết/GetForUser/KiemTraMoi chỉ `[SBAuthorize]` không key → bất kỳ user đăng nhập đều gọi được (đúng ý: FE người dùng cuối đọc thông báo): `Notification/Notification.API/Areas/SanboxArea/ThongBaoHeThongController.cs:70-90`.
