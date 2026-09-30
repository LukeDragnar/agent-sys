# Dev — Quản lý tài khoản & người dùng

> Service: **Core** (BE .NET), Area SanboxArea, route `api/QuanTriArea/User`. ⚠️ KHÁC `UserController` bản AuthenCustomer (API app khách). Node registry: `UserController_Core` (suffix `_Core` vì trùng tên). Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Khu vực | Ghi chú |
|---|---|---|
| Quản lý nhân viên / tài khoản | Hệ thống (QuanTriArea) | lưới + CRUD + đổi MK + khóa |
| Tìm đơn vị/chi nhánh (CSKH) | nội bộ CSKH | hỗ trợ chuyển đơn vị |

> Route FE cụ thể: cần product/dev xác nhận trong `scrm_frontend` (chưa trace FE trong session này).

## Endpoint / Controller (trace registry)
**UserController_Core** — `registry/he-thong/UserController_Core.md` · route `api/QuanTriArea/User`:
| MaTinhNang | Route | Quyền |
|---|---|---|
| `User_TimTheoDieuKien` | POST `.../TimTheoDieuKien` | HT-AD |
| `User_LayDanhSachUserDonViTheoDieuKien` | POST `.../LayDanhSachUserDonViTheoDieuKien` | HT-AD, HT-CSKH |
| `User_LayTatCaTheoIdDonVi` | POST `.../LayTatCaTheoIdDonVi/{idDonVi}` | HT-AD, HT-CSKH |
| `User_TimTheoId` | GET `.../TimTheoId/{id}` | HT-AD |
| `User_ThemMoi` | POST `.../ThemMoi` | HT-AD |
| `User_CapNhatThongTin` | POST `.../CapNhatThongTin` | HT-AD |
| `User_XoaTaiKhoan` | POST `.../XoaTaiKhoan` | HT-AD |
| `User_DoiMatKhau` | POST `.../DoiMatKhau` | HT-AD |
| `User_KhoaTaiKhoan` | POST `.../KhoaTaiKhoan` | HT-AD; state HoatDong↔Khoa |
| (internal, `[SBAuthorize]` rỗng) | GET `.../GetDonViChiNhanhByUser?u=`, POST `.../GetDonViByMaDon`, POST `.../DongBoUserByDonVi`, GET `.../GetStatusDongBoUserByDonVi`, POST `.../AdminSearchByKeyword_DV` | quyền check trong service (super/HT-CSKH) |

> **UserRoleController** (`registry/he-thong/UserRoleController.md`) **đã vô hiệu** (toàn class comment, `deprecated: true`) — KHÔNG còn endpoint. Việc map User–Role xử lý qua UserController_Core (ThemMoi/CapNhat) + RoleController.

## Entities
- `User` — gắn `IdDonVi`/`IdChiNhanh`/`IdNganhNghe`/`Domain`; state {HoatDong, Khoa}.
- `Role`, `DonVi` — tham chiếu khi gán role / resolve đơn vị.

## Luồng code
- `ThemMoi`: `IdDonVi` khác rỗng → `ICommonService.GetOrgByIdAsync` (cross-service) lấy ngành nghề/domain; đơn vị null → NotFound. Không đơn vị → Guid.Empty + domain theo môi trường → `ThemMoiTaiKhoanAsync`.
- `CapNhat`: chỉ resolve khi `IdDonVi.HasValue` & đơn vị tồn tại → `CapNhapTaiKhoanAsync`.
- `DongBoUserByDonVi`: qua `IUserSyncService` re-sync user toàn đơn vị; `GetStatusDongBoUserByDonVi` trả tiến trình.
- Depends: `IUserService`, `IUserSyncService`, `IRoleService`, `IPermisionService`, `IPageService`, `ICommonService`, `IRedisCacheService`, `IVerificationCodeService`, `AppCoreDataContext`.

## Bug-prone / lưu ý
- **Nhiều endpoint nội bộ chỉ `[SBAuthorize]` rỗng, dựa vào service tự check quyền** (super-admin/HT-CSKH) — không gating ở controller (`UserController.cs:200-243`). Rủi ro nếu service check lỏng — để session security xử lý.
- **Domain hardcode theo môi trường** trong controller (`UserController.cs:118-125`) — đổi domain phải sửa code, không cấu hình.
- **Trùng tên `UserController`** với bản AuthenCustomer (API app khách) → khi grep theo tên class dễ nhầm; phân biệt bằng route (`api/QuanTriArea/User` vs API khách) và node `_Core`.
- **`ThemMoi` resolve đơn vị cross-service** (`GetOrgByIdAsync`) — nếu service Common chậm/lỗi thì tạo user fail; chú ý timeout.

## File source (path:line)
- BE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Core\...\Areas\SanboxArea\UserController.cs` — ThemMoi `:101-126`, CapNhat `:140-147`, endpoint nội bộ `:200-243`.
- Registry: `projects/scrm/registry/he-thong/UserController_Core.md`, `UserRoleController.md` (deprecated).
