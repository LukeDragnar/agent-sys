---
controller: GioLamViecController
module: he-thong
service: Core
man_hinh_ui: "Cấu hình hệ thống > Giờ làm việc (khung giờ làm việc của đơn vị) — danh sách + thêm/sửa/xóa; lọc theo điều kiện"
muc_dich: "CRUD khung giờ làm việc (GioLamViec) của đơn vị, kèm 1 endpoint tìm theo điều kiện (lọc/phân trang). Kế thừa CRUD chuẩn từ SBBaseController."
alias: ["giờ làm việc", "khung giờ làm việc", "GioLamViec", "ca làm việc"]
auth: public
state_machine: []
deprecated: false
entities: [GioLamViec]
stored_procs: []
depends: ["IGioLamViecService"]
cross_module_in: ["FE Cấu hình hệ thống/Giờ làm việc gọi CRUD + tìm theo điều kiện"]
cross_module_out: []
---

# GioLamViecController — Quản lý giờ làm việc

Controller mỏng kế thừa `SBBaseController<GioLamViec, GioLamViecDto>` (route `api/GioLamViec/...`), cung cấp CRUD chuẩn + 1 endpoint riêng `TimTheoDieuKien` (lọc danh sách giờ làm việc theo `GioLamViecFilterDto`). Toàn bộ ủy quyền cho `IGioLamViecService`. Class **không** đặt `[SBAuthorize]` ở mức controller (chỉ kế thừa attribute từ base class `SBBaseController` vốn có `[SBAuthorize]`); endpoint `TimTheoDieuKien` riêng KHÔNG có permission key.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | CRUD chuẩn (kế thừa SBBaseController) | Thêm / Sửa / Xóa / Xem | GET `api/GioLamViec/TimTheoId/{id}`, GET `api/GioLamViec/LayTatCa`, POST `api/GioLamViec/ThemMoi`, POST `api/GioLamViec/CapNhatThongTin`, POST `api/GioLamViec/Xoa/{id}` | — (chỉ `[SBAuthorize]` mức base, không permission key riêng) | public | — | CRUD GioLamViec qua `IBaseService` (map Dto↔Entity, GetById/GetAll/Insert/Update/Delete). | EF `GioLamViec` | — |
| 2 | Tìm theo điều kiện | Grid / Lọc | POST `api/GioLamViec/TimTheoDieuKien` | — (không có permission key) | public (kế thừa `[SBAuthorize]` base) | — | `TimTheoDieuKienAsync(GioLamViecFilterDto)` → danh sách lọc theo điều kiện. | EF `GioLamViec` | — |

## Quan hệ
- **Gọi sang:** `IGioLamViecService` (TimTheoDieuKienAsync) + `IBaseService<GioLamViec>` (CRUD nền).
- **Dữ liệu (entities):** `GioLamViec`. DTO: `GioLamViecDto`, `GioLamViecFilterDto`.
- **Ripple / đồng bộ:** không có cross-module rõ rệt (cấu hình nội bộ đơn vị).

## Lỗi đã biết / lưu ý kỹ thuật
- `Controllers/GioLamViecController.cs:36` — `TimTheoDieuKien` không gắn permission key riêng (chỉ dựa `[SBAuthorize]` kế thừa từ base) → mọi user đã xác thực gọi được, không phân quyền chi tiết.
