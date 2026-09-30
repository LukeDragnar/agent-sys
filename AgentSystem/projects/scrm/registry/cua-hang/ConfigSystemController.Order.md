---
controller: ConfigSystemController
module: cua-hang
service: Order
man_hinh_ui: "Quản trị hệ thống (QuanTriArea / superadmin) — cấu hình ConfigSystem của service Order. FE: pages/quantri/config/config-order/ (config-order.component + config-order-edit). Vị trí menu chính xác chưa xác nhận"
muc_dich: "CRUD ConfigSystem (key-value cấu hình hệ thống cấp thấp) cho service Order — superadmin quản lý theo nhóm/group."
alias: ["config system order", "cấu hình hệ thống order", "ConfigSystem Order", "config key-value Order"]
auth: internal
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends: [Order]
cross_module_in: [FE pages/quantri/config/config-order — ConfigSystemOrderService]
cross_module_out: []
---

# ConfigSystemController (Order, QuanTriArea) — Cấu hình hệ thống ConfigSystem (superadmin)

> Controller `Order.API/Areas/SandBoxArea/ConfigSystemController.cs`, route **`api/QuanTriArea/ConfigSystem`**. `[SBAuthorize]` controller-level + `[TienIch("ConfigSystemController_Order", ..., MaTienIchSCRM.TI_HeThong, true)]` (cờ `true` = **superadmin/hệ thống**). Quản lý key-value ConfigSystem cấp thấp của service **Order**. (Lưu ý: có node trùng tên cho service **PreOrder** ở `ConfigSystemController.md` — đây là bản service Order, file đặt tên `-Order` để tránh đè.)

## Hành động (UI / công khai)
| # | Hành động | Endpoint (HTTP path) | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn |
|---|---|---|---|---|---|---|
| 1 | Lấy config theo mã | GET `.../TimTheoMa?ma=` | `ConfigSystem_Order_TimTheoMa` | internal | `TimTheoMaAsync(ma)` | EF |
| 2 | Lấy tất cả | GET `.../LayTatCa` | `ConfigSystem_Order_LayTatCa` | internal | `GetAllAsync()` | EF |
| 3 | Lấy tất cả theo group + sort | POST `.../LayTatCaByGroupSort` | `ConfigSystem_Order_LayTatCaTheoGroupVaSort` | internal | `GetLikeTreeSort(filter)` (FilterCustomDto) — cây theo group + sort | EF |
| 4 | Lấy tất cả theo group (tree) | GET `.../LayTatCaByGroup` | `ConfigSystem_Order_LayTatCaTheoGroup` | internal | `GetLikeTree()` | EF |
| 5 | Thêm mới | POST `.../ThemMoi` | `ConfigSystem_Order_ThemMoi` | internal | Validate ModelState; map DTO→entity; set NgayTao/NguoiTao=CurrentUser, Id=NewGuid; `Insert` | EF |
| 6 | Xóa theo Id | POST `.../Xoa/{id:Guid}` | `ConfigSystem_Order_XoaConfigSystem` | internal | Check tồn tại (GetById) null→"Không tìm thấy" → `Delete(id)`; key mô tả "ONLY_SUPERUSER" | EF |
| 7 | Cập nhật | POST `.../CapNhatThongTin` | `ConfigSystem_Order_CapNhatThongTin` | internal | Validate; check tồn tại null→"Không tìm thấy ConfigSystem !"; set NguoiCapNhat=CurrentUser, NgayCapNhat=Now; `Update` | EF |
| 8 | Lấy 1 theo Id | GET `.../TimTheoId/{id:Guid}` | `ConfigSystem_Order_TimTheoId` | internal | `GetByIdAsync(id)` → có thì map ConfigSystemDto; null → trả raw result | EF |
| 9 | Lấy các nhóm cấu hình | GET `.../LayCacNhomCauHinh` | `ConfigSystem_Order_LayCacGroupConfigSystem` | internal | `GetGroupCode()` | EF |

## Quan hệ
- **Gọi sang:** `IConfigSystemService` (TimTheoMaAsync / GetAllAsync / GetLikeTreeSort / GetLikeTree / Insert / Delete / Update / GetByIdAsync / GetGroupCode).
- **Dữ liệu (entities):** `ConfigSystem` (Ma, Value, Group, Sort; audit NgayTao/NguoiTao + NgayCapNhat/NguoiCapNhat).

## Lỗi đã biết / lưu ý kỹ thuật
- Method `GetConfigSystemByCodeAsync` (`GetConfigSystem/{code}`) đã **comment** (dòng 128–134) — không active.
- **"ONLY_SUPERUSER" chỉ ghi trong mô tả permission Xóa** — không thấy guard role riêng trong code, thực tế chỉ check permission key + cờ superadmin của TienIch.
- Nhánh `TimTheoId` khi `data.Data == null` trả `ResultWithData(data)` (bọc nguyên MethodResult) — hơi lạ.
- Tất cả method `virtual`; không clear cache trong controller.
- FE: `services/order/configsystem-order.service.ts` (`ConfigSystemOrderService`) chỉ wrap LayTatCaByGroup / LayTatCaByGroupSort / LayCacNhomCauHinh / TimTheoMa; màn `pages/quantri/config/config-order/`.
