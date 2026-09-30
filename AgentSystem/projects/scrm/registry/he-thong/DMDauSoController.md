---
controller: DMDauSoController
module: he-thong
service: Core
man_hinh_ui: "Quản trị (QuanTriArea) > Hệ thống > danh mục đầu số hệ thống (tiện ích TI_HeThong)"
muc_dich: "Quản lý danh mục đầu số hệ thống (DMDauSo): thêm, cập nhật, tìm theo id, xóa, tìm theo điều kiện."
alias: ["Đầu số hệ thống", "DM đầu số", "DMDauSo", "danh mục đầu số"]
auth: public
state_machine: []
deprecated: false
entities: [DMDauSo]
stored_procs: []
depends: ["IDMDauSoService"]
cross_module_in: ["[UI] màn quản trị đầu số hệ thống gọi Create/Update/TimTheoId/Delete/TimTheoDieuKien"]
cross_module_out: []
---

# DMDauSoController — Danh mục đầu số hệ thống

Controller trong namespace `Core.API.Areas.SanboxArea` nhưng **route thật là `api/QuanTriArea/[controller]`** (KHÔNG có `[Area]` attribute) → đường dẫn `api/QuanTriArea/DMDauSo`. Kế thừa `BaseController` (không phải SBBaseController), tự khai báo đủ CRUD + tìm kiếm, mỗi action gắn `[SBAuthorize(key)]` riêng. Khai báo `[TienIch(..., MaTienIchSCRM.TI_HeThong, true)]`. Mọi nghiệp vụ ủy quyền `IDMDauSoService`, truyền `CurrentUser`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới đầu số | Thêm mới | POST `api/QuanTriArea/DMDauSo/Create` | `DMDauSoController_Create` | public | — | Validate ModelState; gọi `service.Create(payload, CurrentUser)`. | EF qua service | — |
| 2 | Cập nhật đầu số | Cập nhật | POST `api/QuanTriArea/DMDauSo/Update` | `DMDauSoController_Update` | public | — | Validate ModelState; gọi `service.Update(payload, CurrentUser)`. | EF qua service | — |
| 3 | Tìm theo Id | (chi tiết) | POST `api/QuanTriArea/DMDauSo/TimTheoId/{id:Guid}` | `UMMayInController_TimTheoId` | public | — | Lấy 1 đầu số theo `id`. **Dùng POST cho thao tác lấy theo id.** | EF qua service | Permission key mượn từ `UMMayInController_*` (copy-paste) |
| 4 | Xóa đầu số | Xóa | POST `api/QuanTriArea/DMDauSo/Delete/{id:Guid}` | `UMMayInController_Delete` | public | — | Xóa theo `id`. | EF qua service | Permission key mượn từ `UMMayInController_*` |
| 5 | Tìm theo điều kiện | (lưới/tìm kiếm) | POST `api/QuanTriArea/DMDauSo/TimTheoDieuKien` | `DMDauSoController_TimTheoDieuKien` | public | — | Nhận `DMDauSoFilter`, trả danh sách theo điều kiện. | EF qua service | Mô tả permission ghi "DM máy in" (sai mô tả, đúng phải là đầu số) |

## Quan hệ
- **Gọi sang:** `IDMDauSoService`.
- **Dữ liệu (entities):** `DMDauSo` ↔ `DMDauSoDto` / `DMDauSoFilter`.
- **Ripple / đồng bộ:** không thấy ở tầng controller.

## Lỗi đã biết / lưu ý kỹ thuật
- `DMDauSoController.cs:11` — namespace `SanboxArea` nhưng route `api/QuanTriArea/[controller]` và KHÔNG có `[Area]` → đường dẫn thật `api/QuanTriArea/DMDauSo` (đừng giả định theo namespace).
- `DMDauSoController.cs:43,55` — permission key mượn từ controller khác: `UMMayInController_TimTheoId`, `UMMayInController_Delete` (copy-paste, sai key) → phân quyền đầu số bị trộn với quyền máy in.
- `DMDauSoController.cs:66` — mô tả `[SBAuthorize]` của `TimTheoDieuKien` ghi "Tìm kiếm DM máy in theo điều kiện" (sai, phải là đầu số).
- `DMDauSoController.cs:44` — `TimTheoId` dùng `[HttpPost]` (không phải GET) → lệch convention lấy-theo-id.
