---
controller: PermisionController (SanboxArea / QuanTriArea)
module: he-thong
service: Core
man_hinh_ui: "Quản trị > Phân quyền hệ thống (route api/QuanTriArea/Permision): phân quyền mặc định theo role (superadmin), copy quyền giữa đơn vị, tự động phân quyền, tạo/sửa/xóa permission handmade, gán role theo key"
muc_dich: "Biến thể QUẢN TRỊ/SUPERADMIN của Permision: phân quyền mặc định toàn hệ thống theo role, copy quyền giữa đơn vị (V1/V2), tự động phân quyền, CRUD permission handmade, gán/đọc role theo keycode. Nhiều thao tác chỉ superadmin."
alias: ["phân quyền hệ thống", "copy quyền", "permission handmade", "tự động phân quyền", "Permision QuanTriArea"]
auth: mixed
state_machine: []
deprecated: false
entities: [Permision, RolePermision]
stored_procs: []
depends: ["IPermisionService", "BaseController", "ServiceSettings", "IRedisCacheService", "ServiceInfo (RunningJobCopyDonVi)"]
cross_module_in: ["FE Quản trị/Phân quyền hệ thống gọi LayDanhSachQuyenTheoRole/PhanQuyenChucNang/DonViQuyenCopy*/AutoPhanQuyenChucNang/CreateHandmadePermision...", "Job copy đơn vị trigger qua RunJobCopyDonVi"]
cross_module_out: ["Phân quyền mặc định + copy + auto-set + handmade permission → đổi quyền role toàn hệ thống/đơn vị, ảnh hưởng truy cập chức năng diện rộng"]
---

# PermisionController (QuanTriArea) — Phân quyền hệ thống / superadmin

Bản QuanTriArea, kế thừa `BaseController`, controller-level `[SBAuthorize]` + `[TienIch(TI_HeThong)]`. Tập trung thao tác phân quyền cấp hệ thống + tiện ích quản trị quyền. Một số endpoint chặn `CurrentUser.IsSuperUser`; `RunJobCopyDonVi` `[AllowAnonymous]` → auth = mixed. Controller lớn → bảng giữ action có UI, gom phần còn lại.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Xem quyền theo role (hệ thống) | (load bảng quyền) | POST `api/QuanTriArea/Permision/LayDanhSachQuyenTheoRole` | `Permision_LayDanhSachQuyenTheoRole` (HT-AD) | public | chỉ SuperUser | Nếu `IsSuperUser != true` → BadRequest; ngược lại trả ma trận quyền role mặc định hệ thống. | EF | — |
| 2 | Cập nhật quyền (hệ thống) | Lưu phân quyền | POST `api/QuanTriArea/Permision/PhanQuyenChucNang` | `Permision_PhanQuyenChucNang` | public | chỉ SuperUser | `!IsSuperUser` → BadRequest; ngược lại `UpdateRolePermision(model)`. | EF | — |
| 3 | Danh sách tiện ích | (load màn) | GET `api/QuanTriArea/Permision/LayDanhSachTienIch` | — (login) | public | — | Superadmin all / user thường theo đơn vị mua. | EF | — |
| 4 | Copy quyền | Copy | POST `api/QuanTriArea/Permision/DonViQuyenCopy`, `DonViQuyenCopyV2` | `Permision_CopyQuyen` / `Permision_CopyQuyenV2` | public | V2 chỉ SuperUser | Copy cấu hình quyền sang đơn vị (V2: `CopyPermisionToDonVi`, chặn non-superuser). | EF | — |
| 5 | Tự động phân quyền | Auto phân quyền | POST `api/QuanTriArea/Permision/AutoPhanQuyenChucNang` | `Permision_AutoPhanQuyenChucNang` (HT-AD, HT-CSKH) | public | — | `AutoSetRolePermision()` tự gán quyền mặc định toàn hệ thống. | EF | — |
| 6 | Init form copy / tìm đơn vị | (form copy) | GET `GetInitFromCopy`, POST `GetDonViTheoDieuKien` | `Permision_GetInitFromCopy` / `Permision_GetDonViTheoDieuKien` | public | — | Lấy dữ liệu init form copy quyền + tìm đơn vị đích. | EF | — |
| 7 | Permission handmade | Tạo/Sửa/Xóa permission | POST `CreateHandmadePermision`, `UpdateHandmadePermision`, `DeleteHandmadePermision/{keycode}` | `Permision_CreateHandmadePermision` / `_UpdateHandmadePermision` / `_DeleteHandmadePermision` | public | — | CRUD permission tự tạo (handmade) theo keycode. | EF | — |
| 8 | Gán role theo key | (form gán role) | GET `GetByKey?keycode=`, POST `UpdateByKey`, `UpdateByKeyDonVi`, `GetRolePermissionByKeycode/{keycode}`, `GetDetailPermissionByKeycode/{keycode}`, `UpdateDescRolePermision` | `Permision_GetByKey` / `_UpdateGetByKey` / `_UpdateByKeyDonVi` / ... | public | — | Đọc/gán danh sách role theo keycode permission (toàn hệ thống hoặc đơn vị). `UpdateByKeyDonVi` yêu cầu `DonViId != null`. | EF | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] **GET `RunJobCopyDonVi`** `[AllowAnonymous]` — bật cờ `ServiceInfo.RunningJobCopyDonVi = true` để job nền copy quyền đơn vị chạy. KHÔNG yêu cầu xác thực.

## Quan hệ
- **Gọi sang:** IPermisionService (EF), ServiceInfo (cờ job).
- **Dữ liệu (entities):** `Permision`, `RolePermision`.
- **Ripple / đồng bộ:** Phân quyền mặc định/auto/copy/handmade → thay đổi quyền role hệ thống & đơn vị diện rộng.

## Lỗi đã biết / lưu ý kỹ thuật
- `RunJobCopyDonVi` (dòng 126–133) `[AllowAnonymous]` → ai cũng bật được cờ chạy job copy đơn vị; cần cân nhắc rủi ro.
- Nhiều endpoint ghi quyền (`PhanQuyenChucNang`, copy, handmade) chỉ gắn key chung, một số chặn SuperUser trong code (LayDanhSachQuyenTheoRole, PhanQuyenChucNang, DonViQuyenCopyV2) — không đồng nhất giữa các action.
