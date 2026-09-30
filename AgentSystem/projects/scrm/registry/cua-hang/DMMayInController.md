---
controller: DMMayInController
module: cua-hang
service: Order
man_hinh_ui: "Order / Danh mục máy in (pages/Order/dm-may-in, DmMayInComponent) — list + tìm kiếm + nút Thêm mới/Sửa (modal DmMayInEditComponent), toggle sử dụng, xóa. Dropdown máy in dùng khi cấu hình in đơn bếp/hóa đơn."
muc_dich: "Quản lý danh mục máy in của đơn vị: CRUD máy in, bật/tắt sử dụng, tìm theo điều kiện, dropdown — máy in được dùng để in đơn bếp/hóa đơn ở luồng đơn hàng POS."
alias: ["danh mục máy in", "máy in", "cấu hình máy in", "in đơn bếp", "in hóa đơn máy in"]
auth: public
state_machine: []
deprecated: false
entities: [DMMayIn]
stored_procs: []
depends: [Order]
cross_module_in: [Order/dm-may-in (DmMayInComponent) gọi toàn bộ CRUD; các màn in đơn/POS gọi DropdownDMMayIn để chọn máy in]
cross_module_out: [Máy in được tham chiếu khi in đơn bếp/hóa đơn ở DonHang/POS]
---

# DMMayInController — Danh mục máy in

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới máy in | "Thêm mới" (modal) | POST `api/DMMayIn/Create` | `DMMayInController_Create` | public | nút Thêm | ModelState valid → `Create(payload, CurrentUser)`; insert máy in | EF | - |
| 2 | Cập nhật máy in | "Sửa" (modal) | POST `api/DMMayIn/Update` | `DMMayInController_Update` | public | per-row | ModelState valid → `Update(payload, CurrentUser)` | EF | - |
| 3 | Tìm theo Id | mở chi tiết/sửa | POST `api/DMMayIn/TimTheoId/{id}` | `UMMayInController_TimTheoId` | public | - | `TimTheoId(id, CurrentUser)` — 1 máy in theo Id | EF | Permission key prefix `UMMayInController` (typo, lẽ ra `DMMayInController`) |
| 4 | Xóa máy in | "Xóa" (confirm) | POST `api/DMMayIn/Delete/{id}` | `UMMayInController_Delete` | public | per-row | `Delete(id, CurrentUser)` | EF | Permission key prefix `UMMayInController` (typo) |
| 5 | Bật/tắt sử dụng | toggle "Sử dụng" | POST `api/DMMayIn/ChangeSuDung/{id}` | `UMMayInController_ChangeSuDung` | public | per-row | `ChangeSuDung(id, CurrentUser)` — đảo trạng thái sử dụng | EF | Permission key prefix `UMMayInController` (typo) |
| 6 | Tìm theo điều kiện (list) | "Tìm kiếm" / load grid | POST `api/DMMayIn/TimTheoDieuKien` | `DMMayInController_TimTheoDieuKien` | public | màn list | `TimTheoDieuKien(payload, CurrentUser)` — list theo filter (DMMayInFilter) | EF | - |
| 7 | Dropdown máy in | (dropdown chọn máy in) | POST `api/DMMayIn/DropdownDMMayIn` | (rỗng — login) | public | nơi chọn máy in | `DropdownDMMayIn(CurrentUser)` — list rút gọn cho dropdown | EF | permission key đã comment (`...DropdownDMMayIn`) → chỉ còn login |

## Quan hệ
- **Gọi sang:** `IDMMayInService` (Create/Update/TimTheoId/Delete/ChangeSuDung/TimTheoDieuKien/DropdownDMMayIn).
- **Dữ liệu (entities):** `DMMayIn`.
- **Ripple / đồng bộ:** máy in được chọn (qua dropdown) để in đơn bếp/hóa đơn ở luồng đơn hàng POS.

## Lỗi đã biết / lưu ý kỹ thuật
- 3 endpoint (`TimTheoId`/`Delete`/`ChangeSuDung`) dùng permission key prefix sai `UMMayInController_*` thay vì `DMMayInController_*` (copy-paste typo) — vẫn hoạt động nếu key được seed đúng, nhưng không nhất quán với Create/Update/TimTheoDieuKien.
- `DropdownDMMayIn` mất permission key (đã comment) → chỉ còn login.
- Code thật: BE `Order/Order.API/Controllers/DMMayInController.cs`; FE service `services/order/dm-may-in.service.ts`, page `pages/Order/dm-may-in/` (+ `dm-may-in-edit/`).
