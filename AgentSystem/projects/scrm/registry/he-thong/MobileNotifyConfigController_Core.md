---
controller: MobileNotifyConfigController (SanboxArea / QuanTriArea)
module: he-thong
service: Core
man_hinh_ui: "Quản trị > Cấu hình thông báo mobile (route api/QuanTriArea/MobileNotifyConfig): grid lọc + form Thêm/Sửa/Xóa + nút Duplicate (nhân bản cấu hình)"
muc_dich: "Biến thể QUẢN TRỊ của MobileNotifyConfig: CRUD đầy đủ + Duplicate cấu hình thông báo mobile. Có gating quyền đầy đủ (HT-AD, HT-MB). Khác bản Controllers (chỉ READ cho app)."
alias: ["cấu hình thông báo mobile", "mobile notify config", "duplicate notify config", "MobileNotifyConfig QuanTriArea"]
auth: public
state_machine: []
deprecated: false
entities: [MobileNotifyConfig]
stored_procs: []
depends: ["IMobileNotifyConfigService", "AutoMapper", "Core.DataContext (SQL)"]
cross_module_in: ["Trang quản trị Cấu hình thông báo mobile gọi CRUD + Duplicate qua route QuanTriArea"]
cross_module_out: ["Thay đổi cấu hình → app mobile đọc qua MobileNotifyConfigController (Controllers) để gửi/hiển thị thông báo"]
---

# MobileNotifyConfigController (QuanTriArea) — CRUD cấu hình thông báo mobile

Biến thể **SanboxArea** (route `api/QuanTriArea/MobileNotifyConfig`), TienIch nhóm `TI_HeThong` (tham số 4 = `true`). CRUD đầy đủ + **Duplicate** (nhân bản 1 cấu hình theo id). **Mỗi endpoint đều có `[SBAuthorize]` key + role HT-AD/HT-MB** (gating đầy đủ). Đây là nơi WRITE cấu hình; bản Controllers chỉ READ cho app.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới | Thêm | POST `api/QuanTriArea/MobileNotifyConfig/ThemMoi` | `MobileNotifyConfigController_ThemMoi` (HT-AD, HT-MB) | public | ModelState valid | Map dto→entity → `InsertAsync`. | EF `MobileNotifyConfig` | — |
| 2 | Sửa | Lưu | POST `api/QuanTriArea/MobileNotifyConfig/CapNhatThongTin` | `MobileNotifyConfigController_CapNhatThongTin` (HT-AD, HT-MB) | public | ModelState valid + tồn tại Id | Map → `GetByIdAsync`; null → `MESS_NotFound` (404); có → `UpdateAsync`. | EF | ModelState lỗi trả `CODE_Error` (khác ThemMoi dùng `CODE_Model_UnValidate`) — lệch convention |
| 3 | Xóa | Xóa | POST `api/QuanTriArea/MobileNotifyConfig/Xoa/{id:Guid}` | `MobileNotifyConfigController_Xoa` (HT-AD, HT-MB) | public | Tồn tại Id | `GetByIdAsync` null → `MESS_NotFound`; có → `DeleteAsync` → `MESS_Delete_Success`. | EF | — |
| 4 | Nhân bản | Duplicate | POST `api/QuanTriArea/MobileNotifyConfig/Duplicate/{id:Guid}` | `MobileNotifyConfigController_Duplicate` (HT-AD, HT-MB) | public | — | `Duplicate(id)` — clone cấu hình. | EF | Trả kèm message `MESS_Delete_Success` (sai message: duplicate nhưng báo "xóa thành công") |
| 5 | Lọc theo điều kiện | Grid lọc | POST `api/QuanTriArea/MobileNotifyConfig/TimTheoDieuKien` | `MobileNotifyConfigController_TimTheoDieuKien` (HT-AD, HT-MB) | public | — | `TimTheoDieuKienAsync(gridInfo)`. | EF | — |
| 6 | Chi tiết theo Id | Mở chi tiết | GET `api/QuanTriArea/MobileNotifyConfig/TimTheoId/{id:Guid}` | `MobileNotifyConfigController_TimTheoId` (HT-AD, HT-MB) | public | — | `GetByIdAsync`; có → map `MobileNotifyConfigDto`. | EF | Nhánh null trả thẳng `data` thay vì NotFound |

## Quan hệ
- **Gọi sang:** `IMobileNotifyConfigService` (CRUD + `Duplicate`), AutoMapper.
- **Dữ liệu (entities):** `MobileNotifyConfig`.
- **Ripple / đồng bộ:** WRITE cấu hình ở đây → app mobile đọc qua `MobileNotifyConfigController` (Controllers) → ảnh hưởng gửi/hiển thị thông báo.

## Lỗi đã biết / lưu ý kỹ thuật
- `Areas/SanboxArea/MobileNotifyConfigController.cs:53` `Duplicate` trả `Contants.MESS_Delete_Success` ("Xóa thành công") cho thao tác nhân bản → message sai ngữ nghĩa.
- `Areas/SanboxArea/MobileNotifyConfigController.cs:62` `CapNhatThongTin` trả `CODE_Error` khi ModelState invalid, khác `ThemMoi` (`CODE_Model_UnValidate`) → mã lỗi không nhất quán.
- `Areas/SanboxArea/MobileNotifyConfigController.cs:83-87` `TimTheoId` nhánh null bọc cả `data` (MethodResult) thay vì trả NotFound rõ ràng.
- Khác bản Controllers: bản này **có gating quyền đầy đủ** (HT-AD/HT-MB) và là nơi WRITE; bản Controllers chỉ READ (`LayTatCa` / `LayTatCa_Encrypt`).
