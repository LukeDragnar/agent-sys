---
controller: ConfigSystemController
module: he-thong
service: ClearData
man_hinh_ui: "nội bộ — quản trị cấu hình hệ thống (SandBox/QuanTriArea), không phải màn user thường"
muc_dich: "CRUD cấu hình hệ thống (ConfigSystem) trong ClearData service: tìm theo mã/id, lấy tất cả/theo group, thêm/sửa/xóa (xóa chỉ super user)."
alias: ["config system", "cấu hình hệ thống", "ConfigSystem customer"]
auth: internal
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends: [IConfigSystemService, AutoMapper]
cross_module_in: ["FE quản trị cấu hình (QuanTriArea) gọi CRUD"]
cross_module_out: []
---

# ConfigSystemController (ClearData) — Cấu hình hệ thống

TienIch: `ConfigSystemController_Customer` "Quản lý ConfigSystem customer" (TI_HeThong, ẩn=true). Controller-level `[SBAuthorize]` + mỗi action có key riêng. Route `api/QuanTriArea/ConfigSystem`. Namespace `OrderProcess.API.Areas.SandBoxArea` (dùng lại). → internal.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy config theo mã | — | GET `api/QuanTriArea/ConfigSystem/TimTheoMa` | ConfigSystem_Customer_TimTheoMa | internal | — | Trả config theo `ma` | EF | — |
| 2 | Lấy tất cả | — | GET `api/QuanTriArea/ConfigSystem/LayTatCa` | ConfigSystem_Customer_LayTatCa | internal | — | List toàn bộ config | EF | — |
| 3 | Lấy theo group + sort | — | POST `api/QuanTriArea/ConfigSystem/LayTatCaByGroupSort` | ConfigSystem_Customer_LayTatCaTheoGroupVaSort | internal | — | Tree theo group, sort | EF | — |
| 4 | Lấy theo group | — | GET `api/QuanTriArea/ConfigSystem/LayTatCaByGroup` | ConfigSystem_Customer_LayTatCaTheoGroup | internal | — | Tree theo group | EF | — |
| 5 | Thêm mới | Thêm | POST `api/QuanTriArea/ConfigSystem/ThemMoi` | ConfigSystem_Customer_ThemMoi | internal | ModelState hợp lệ | Map DTO→entity, set NgayTao/NguoiTao/Id=NewGuid, Insert | EF | — |
| 6 | Xóa theo Id | Xóa | POST `api/QuanTriArea/ConfigSystem/Xoa/{id}` | ConfigSystem_Customer_XoaConfigSystem | internal (ONLY_SUPERUSER) | tồn tại | Check tồn tại → Delete; không thấy → lỗi NotFound | EF | Mô tả ghi "ONLY_SUPERUSER" nhưng code không thấy check super user riêng |
| 7 | Cập nhật | Lưu | POST `api/QuanTriArea/ConfigSystem/CapNhatThongTin` | ConfigSystem_Customer_CapNhatThongTin | internal | ModelState hợp lệ + tồn tại | Map DTO→entity, set NguoiCapNhat/NgayCapNhat, Update; không thấy → 404 | EF | — |
| 8 | Lấy theo Id | — | GET `api/QuanTriArea/ConfigSystem/TimTheoId/{id}` | ConfigSystem_Customer_TimTheoId | internal | — | Trả config theo id (map ngược DTO) | EF | — |
| 9 | Lấy các group | — | GET `api/QuanTriArea/ConfigSystem/LayCacNhomCauHinh` | ConfigSystem_Customer_LayCacGroupConfigSystem | internal | — | List group code | EF | — |
| 10 | Tìm theo điều kiện | Tìm | POST `api/QuanTriArea/ConfigSystem/TimTheoDieuKien` | (SBAuthorize, không key) | internal | — | Lọc config theo filter | EF | SBAuthorize không truyền key → chỉ check đăng nhập |

## Quan hệ
- **Gọi sang:** `IConfigSystemService` (CRUD), `AutoMapperConfiguration.Mapper`.
- **Dữ liệu (entities):** `ConfigSystem`.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- `ConfigSystemController.cs:76` — Xóa mô tả "ONLY_SUPERUSER" nhưng code chỉ check tồn tại rồi Delete; không thấy guard super user → mô tả lệch code.
- `ConfigSystemController.cs:125-131` — `TimTheoDieuKien` dùng `[SBAuthorize()]` không key → chỉ kiểm tra đăng nhập, khác convention các action còn lại có key.
