---
controller: CauHinhDauSoController
module: sales
service: Switchboard
man_hinh_ui: "Tổng đài > Cấu hình đầu số (CRUD đầu số, tìm theo điều kiện, kiểm tra quyền nhận cuộc gọi)"
muc_dich: "CRUD cấu hình đầu số tổng đài của đơn vị (tạo/sửa/xóa/chi tiết/tìm kiếm), kiểm tra quyền nhận cuộc gọi theo đầu số, và expose API private cache đầu số cho service khác."
alias: ["cấu hình đầu số", "đầu số tổng đài", "quyền nhận cuộc gọi"]
auth: mixed
state_machine: []
deprecated: false
entities: [CauHinhDauSo]
stored_procs: []
depends: [ICauHinhDauSoService]
cross_module_in: ["service nội bộ (GetAndSetCauHinhDauSoCache/{idDonVi} — set cache cấu hình đầu số theo đơn vị)"]
cross_module_out: []
---

# CauHinhDauSo — Cấu hình đầu số tổng đài

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo mới đầu số | Thêm | POST `Create` | `CauHinhDauSoController_Create` (DV-AD-TONG) | public | màn cấu hình đầu số | ModelState invalid → trả lỗi; hợp lệ → tạo đầu số cho `CurrentUser`. | EF | — |
| 2 | Cập nhật đầu số | Lưu | POST `Update` | `CauHinhDauSoController_Update` (DV-AD-TONG) | public | dòng đầu số | ModelState invalid → lỗi; hợp lệ → cập nhật. | EF | — |
| 3 | Xóa đầu số | Xóa | POST `Delete/{id}` | `CauHinhDauSoController_Delete` (DV-AD-TONG) | public | dòng đầu số | Xóa đầu số theo id. | EF | — |
| 4 | Chi tiết đầu số | (load) | POST `GetById/{id}` | `CauHinhDauSoController_GetById` (DV-AD-TONG) | public | mở chi tiết | Trả đầu số theo id. | EF | — |
| 5 | Tìm đầu số theo điều kiện | Tìm | POST `TimTheoDieuKien` | `CauHinhDauSoController_TimTheoDieuKien` (DV-AD-TONG) | public | màn danh sách | ModelState invalid → lỗi; hợp lệ → trả danh sách đầu số theo filter. | EF | — |
| 6 | Kiểm tra quyền nhận cuộc gọi | (ngầm) | POST `CheckCallReceivingPermissions` | `[SBAuthorize]` (login) | public | khi có cuộc gọi đến | Trả cờ quyền nhận cuộc gọi theo đầu số. | EF | — |

## Endpoint nội bộ (private / service-to-service)
- `[PrivateSBAuthorize]` `GetAndSetCauHinhDauSoCache/{idDonVi}` — service khác gọi để đọc + set cache cấu hình đầu số theo đơn vị.

## Quan hệ
- **Gọi sang:** `ICauHinhDauSoService` (CRUD đầu số + cache + check quyền).
- **Dữ liệu (entities):** cấu hình đầu số của đơn vị.
- **Ripple / đồng bộ:** cache đầu số được set qua API private (`GetAndSetCauHinhDauSoCache`).

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhDauSoController.cs` — các action có `//[SBAuthorize]` comment phía trên rồi mới key thật → không ảnh hưởng, key thật vẫn active.
