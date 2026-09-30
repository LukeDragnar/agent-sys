---
controller: RoleController
module: he-thong
service: Core
man_hinh_ui: "Hệ thống > Phân quyền > Quản lý quyền (Role): lưới chức danh/role + Thêm/Sửa/Xóa; nguồn dropdown role cho màn gán quyền nhân viên"
muc_dich: "CRUD danh mục Role (chức danh/nhóm quyền hệ thống) + cung cấp danh sách role (theo Id/Code/hệ thống) cho các màn phân quyền và gán role nhân viên."
alias: ["Role", "quản lý quyền", "chức danh", "nhóm quyền", "role hệ thống", "phân quyền - role"]
auth: mixed
state_machine: []
deprecated: false
entities: [Role]
stored_procs: []
depends: ["IRoleService", "BaseController"]
cross_module_in: ["FE Phân quyền/Quản lý quyền gọi TimTheoDieuKien/ThemMoi/CapNhatThongTin/Xoa", "Màn gán role nhân viên (UserController) gọi LayTatCa/GetRolesByListIds/GetRolesByListCodes", "Màn quản trị hệ thống gọi GetRoleBySystem"]
cross_module_out: ["Role là nền phân quyền — User/Permision/Page tham chiếu RoleId; xóa role ảnh hưởng phân quyền user đang gán"]
---

# RoleController — Quản lý Role (chức danh / nhóm quyền)

CRUD Role kế thừa `BaseController` (route `api/QuanTriArea/Role`, Area SanboxArea). Mỗi method tự gắn key `Role_*`. Có thêm 3 endpoint đọc role theo bộ lọc (hệ thống / list Id / list Code) phục vụ màn gán quyền. Controller-level `[SBAuthorize]` rỗng + `[TienIch("RoleController", ..., true)]` TI_HeThong → auth = mixed (key chi tiết phần lớn HT-AD).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | (load lưới) | POST `api/QuanTriArea/Role/TimTheoDieuKien` | `Role_TimTheoDieuKien` (HT-AD) | mixed | — | Lọc role theo `RoleFilterDto`. | EF | — |
| 2 | Lấy tất cả | (dropdown role) | GET `api/QuanTriArea/Role/LayTatCa` | `Role_LayTatCa` (HT-AD, HT-CSKH, HT-MB, HT-KT) | mixed | — | Trả toàn bộ role, sort theo `ThuTu`. | EF | — |
| 3 | Lấy role hệ thống | (dropdown role QT hệ thống) | GET `api/QuanTriArea/Role/GetRoleBySystem` | `Role_GetRoleBySystem` (HT-AD) | mixed | — | Lọc `IsRoleHeThong == true`, sort `ThuTu`. | EF | — |
| 4 | Xem chi tiết | (mở form) | GET `api/QuanTriArea/Role/TimTheoId/{id:Guid}` | `Role_TimTheoId` (HT-AD) | mixed | — | Map `Role`→`AdminRoleDto`, trả null nếu không có. | EF | — |
| 5 | Thêm mới | Thêm | POST `api/QuanTriArea/Role/ThemMoi` | `Role_ThemMoi` (HT-AD) | mixed | — | ModelState valid → map `AdminRoleDto`→`Role`, set NguoiTaoUserId/UserName = CurrentUser → Insert. | EF | — |
| 6 | Cập nhật | Sửa | POST `api/QuanTriArea/Role/CapNhatThongTin` | `Role_CapNhatThongTin` (HT-AD) | mixed | — | ModelState valid → map → kiểm tra tồn tại → set NguoiCapNhatUserId/UserName → Update. | EF | — |
| 7 | Xóa | Xóa | POST `api/QuanTriArea/Role/Xoa/{id:Guid}` | `Role_Xoa` (HT-AD) | mixed | — | Kiểm tra tồn tại → `DeleteAsync` → trả MESS_Delete_Success. | EF | Không kiểm tra role đang được gán cho User trước khi xóa → user mồ côi role |

## Endpoint nội bộ (service-to-service / cross-module)
- [internal] **POST `GetRolesByListIds`** (`[SBAuthorize]` từ controller) — màn gán quyền nhân viên truyền `List<Guid>` lấy role tương ứng.
- [internal] **POST `GetRolesByListCodes`** — như trên nhưng theo `List<string>` mã role.

## Quan hệ
- **Gọi sang:** IRoleService (EF), AutoMapper AdminRoleDto↔Role.
- **Dữ liệu (entities):** `Role` (cờ `IsRoleHeThong`, `ThuTu`, audit NguoiTao/NguoiCapNhat).
- **Ripple / đồng bộ:** Role là nền phân quyền — User/UserRole/Permision tham chiếu; không bắn event.

## Lỗi đã biết / lưu ý kỹ thuật
- `RoleController.cs:144` Xóa không check role còn được gán cho User → có thể tạo user mồ côi role (FK tùy DB).
- Audit chỉ set ở ThemMoi/CapNhatThongTin (CurrentUser), không có soft-delete log.
