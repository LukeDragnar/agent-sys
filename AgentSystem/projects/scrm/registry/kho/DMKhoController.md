---
controller: DMKhoController
module: kho
service: WarehouseProduct
man_hinh_ui: "Hệ thống (SUPER_ADMIN) > Danh mục kho — CRUD danh mục kho cấp hệ thống. CRUD thật nằm ở Areas/SandBoxArea (route api/SanboxArea/DMKho), KHÔNG ở controller cùng tên trong Controllers/ (file đó rỗng)."
muc_dich: "CRUD danh mục kho (DMKho) cấp hệ thống cho SUPER_ADMIN: tìm theo điều kiện, thêm/sửa/xóa, lấy chi tiết theo Id."
alias: ["danh mục kho", "DM kho", "quản lý danh mục kho"]
auth: public               # mọi action có [SBAuthorize(key, "HT-AD")]
state_machine: []
deprecated: false
entities: [DMKho]
stored_procs: []
depends: [WarehouseProduct]
cross_module_in: []
cross_module_out: []
---

# DMKhoController — Danh mục kho (cấp hệ thống / SUPER_ADMIN)

> ⚠️ **Có HAI file cùng tên `DMKhoController`:**
> - `Controllers/DMKhoController.cs` (route `api/DMKho`) — **RỖNG**: chỉ inject `IDMKhoService`, `[TienIch]` bị comment (L12), KHÔNG có endpoint nào. Không phục vụ request.
> - `Areas/SandBoxArea/DMKhoController.cs` (route `api/SanboxArea/DMKho`) — **CRUD thật**, `[TienIch("DMKhoController", ..., MaTienIchSCRM.TI_HeThong, true)]`, mọi action quyền `HT-AD`. ← node này mô tả file này.
> (Trước đây node ghi nhầm controller là "stub chưa có endpoint" vì chỉ thấy file rỗng trong `Controllers/`.)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid) | "Tìm kiếm" | POST `api/SanboxArea/DMKho/TimTheoDieuKien` | `DMKhoController_TimTheoDieuKien` (role HT-AD) | public | màn list | List DMKho theo `DMKhoFilterDto` | EF | - |
| 2 | Thêm mới DM kho | "Thêm mới" | POST `api/SanboxArea/DMKho/ThemMoi` | `DMKhoController_ThemMoi` (role HT-AD) | public | popup Thêm | Map `DMKhoDto`→`DMKho`, insert | EF | - |
| 3 | Lấy chi tiết theo Id | mở popup Sửa | GET `api/SanboxArea/DMKho/TimTheoId/{id:Guid}` | `DMKhoController_TimTheoId` (role HT-AD) | public | per-row | `GetByIdAsync`, map entity→`DMKhoDto` | EF | - |
| 4 | Cập nhật DM kho | "Lưu" (popup Sửa) | POST `api/SanboxArea/DMKho/CapNhat` | `DMKhoController_CapNhat` (role HT-AD) | public | popup Sửa | Update theo `DMKho` model | EF | - |
| 5 | Xóa DM kho | "Xóa" (per-row) | GET `api/SanboxArea/DMKho/Xoa/{id:Guid}` | `DMKhoController_Xoa` (role HT-AD) | public | per-row | Xóa theo id | EF | Dùng HTTP **GET** cho thao tác xóa (lệch convention POST) — khớp FE, đừng tự đổi |

## Quan hệ
- **Gọi sang:** `IDMKhoService` (toàn bộ nghiệp vụ CRUD).
- **Dữ liệu (entities):** `DMKho`.
- **Ripple / đồng bộ:** danh mục kho dùng để phân loại/cấu hình kho thực tế (`KhoController` — node riêng). Quản lý kho vận hành ≠ danh mục kho.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Areas/SandBoxArea/DMKhoController.cs` — controller CRUD thật.
  - L60-65 (`XoaDM`): dùng `[HttpGet("Xoa/{id:Guid}")]` cho thao tác xóa — lệch convention POST nhưng khớp FE.
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/DMKhoController.cs` (23 dòng) — file RỖNG, `[TienIch]` comment (L12), không endpoint. Là scaffold bỏ dở/giữ chỗ; KHÔNG phải nơi xử lý request. Đừng nhầm với bản SandBoxArea ở trên.
