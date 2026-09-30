# Dev — Phân quyền & quản lý quyền

> Service: **Core** (BE .NET). Vùng quản trị nằm trong `Areas/SanboxArea` (route `api/QuanTriArea/...`). Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Khu vực | Ghi chú |
|---|---|---|
| Quản lý quyền (Role) | Hệ thống > Phân quyền | CRUD chức danh/role |
| Phân quyền chức năng (hệ thống) | Quản trị > Phân quyền hệ thống | super-admin |
| Phân quyền chức năng (đơn vị) | Đơn vị > Phân quyền chức năng | admin đơn vị |
| Quản lý quyền custom | Hệ thống > Phân quyền > Quyền Custom | chỉ xem + xóa |

> Route FE cụ thể: cần product/dev xác nhận trong `scrm_frontend` (chưa trace FE trong session này).

## Endpoint / Controller (trace registry)
**RoleController** — `registry/he-thong/RoleController.md` · route `api/QuanTriArea/Role` (Area SanboxArea):
| MaTinhNang | Route | Quyền |
|---|---|---|
| `Role_TimTheoDieuKien` | POST `.../TimTheoDieuKien` | HT-AD |
| `Role_LayTatCa` | GET `.../LayTatCa` | HT-AD, HT-CSKH, HT-MB, HT-KT |
| `Role_GetRoleBySystem` | GET `.../GetRoleBySystem` | HT-AD |
| `Role_TimTheoId` | GET `.../TimTheoId/{id}` | HT-AD |
| `Role_ThemMoi` / `_CapNhatThongTin` / `_Xoa` | POST `.../ThemMoi`, `/CapNhatThongTin`, `/Xoa/{id}` | HT-AD |
| (internal) `GetRolesByListIds` / `GetRolesByListCodes` | POST | màn gán role nhân viên |

**PermisionController (đơn vị)** — `registry/he-thong/PermisionController.md` · route `api/Permision`:
| MaTinhNang | Route |
|---|---|
| (login) | GET `.../LayDanhSachTienIch` |
| `Permision_LayDanhSachQuyenTheoRoleDonVi` | POST `.../LayDanhSachQuyenTheoRoleDonVi` |
| `Permision_PhanQuyenChucNangDonVi` | POST `.../PhanQuyenChucNangDonVi` |

**PermisionController (QuanTriArea / super-admin)** — `registry/he-thong/PermisionController_Core.md` · route `api/QuanTriArea/Permision`:
| MaTinhNang | Route | Gating |
|---|---|---|
| `Permision_LayDanhSachQuyenTheoRole` | POST `.../LayDanhSachQuyenTheoRole` | chỉ IsSuperUser, else BadRequest |
| `Permision_PhanQuyenChucNang` | POST `.../PhanQuyenChucNang` | chỉ IsSuperUser |
| `Permision_CopyQuyen` / `_CopyQuyenV2` | POST `.../DonViQuyenCopy`, `/DonViQuyenCopyV2` | V2 chỉ IsSuperUser |
| `Permision_AutoPhanQuyenChucNang` | POST `.../AutoPhanQuyenChucNang` | HT-AD, HT-CSKH |
| `Permision_CreateHandmadePermision` / `_Update...` / `_Delete...` | POST `.../CreateHandmadePermision`, `/UpdateHandmadePermision`, `/DeleteHandmadePermision/{keycode}` | — |
| `Permision_GetByKey` / `_UpdateGetByKey` / `_UpdateByKeyDonVi` ... | GET `.../GetByKey?keycode=`, POST `.../UpdateByKey`, `/UpdateByKeyDonVi` | `UpdateByKeyDonVi` cần DonViId != null |
| (internal) `RunJobCopyDonVi` | GET `.../RunJobCopyDonVi` | `[AllowAnonymous]` (⚠) |

**RolePermisionLogController (quyền custom)** — `registry/he-thong/RolePermisionLogController.md` · route `api/QuanTriArea/RolePermisionLog`:
| MaTinhNang | Route | Quyền |
|---|---|---|
| `RolePermisionLog_TimTheoDieuKien` | POST `.../TimTheoDieuKien` | HT-AD, HT-CSKH |
| `RolePermisionLog_Xoa` | POST `.../Xoa/{id}` | HT-AD |

## Entities
- `Role` — chức danh/nhóm quyền; cờ `IsRoleHeThong`, `ThuTu`, audit NguoiTao/NguoiCapNhat.
- `Permision`, `RolePermision` — ma trận quyền (role × chức năng), cấp hệ thống & đơn vị.
- `RolePermisionLog` — quyền custom override role mặc định.

## Luồng code
- **Role**: `RoleController` ủy quyền `IRoleService`; ThemMoi/CapNhat set NguoiTao/NguoiCapNhat = CurrentUser; AutoMapper `AdminRoleDto ↔ Role`.
- **Permision đơn vị**: `PermisionController` (Controllers) ủy quyền `IPermisionService` → `UpdateDonViRolePermision`.
- **Permision hệ thống**: `PermisionController` (QuanTriArea) — `UpdateRolePermision`, `CopyPermisionToDonVi` (V2), `AutoSetRolePermision`; `RunJobCopyDonVi` bật cờ `ServiceInfo.RunningJobCopyDonVi` cho job nền.
- **Quyền custom**: `RolePermisionLogController` ủy quyền `IRolePermisionLogService` — chỉ list + DeleteAsync.

## Bug-prone / lưu ý
- **`RoleController.cs:144` Xóa role KHÔNG check role còn gán cho User** → user mồ côi role (FK tùy DB). Audit chỉ set ở ThemMoi/CapNhat, không có soft-delete log.
- **`RunJobCopyDonVi` (PermisionController QuanTriArea, dòng 126-133) là `[AllowAnonymous]`** → ai cũng bật được cờ chạy job copy đơn vị. Rủi ro bảo mật — để session security xử lý.
- **Gating không đồng nhất**: một số endpoint ghi quyền chặn IsSuperUser trong code (`LayDanhSachQuyenTheoRole`, `PhanQuyenChucNang`, `DonViQuyenCopyV2`), số khác chỉ gắn key chung — dễ lệch khi audit quyền.
- **PermisionController đơn vị** inject `IRedisCacheService` nhưng không dùng trong các action của bản này.
- **RolePermisionLog Xóa** hard-delete, không log lý do thu hồi quyền → khó truy vết.
- **PermisionController là `partial`** (RolePermisionLog cũng partial) — phần sinh/cấp quyền custom nằm ở partial/luồng khác, chưa lộ hết ở file controller chính.

## File source (path:line)
- BE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Core\...` — `RoleController.cs:144` (Xoa), `PermisionController.cs` (QuanTriArea, dòng 126-133 RunJobCopyDonVi), `RolePermisionLogController.cs`.
- Registry: `projects/scrm/registry/he-thong/RoleController.md`, `PermisionController.md`, `PermisionController_Core.md`, `RolePermisionLogController.md`.
