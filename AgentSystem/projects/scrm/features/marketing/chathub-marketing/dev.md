# Dev — ChatHub Marketing (Phân quyền User ↔ Fanpage)

> Service: **FacebookChat** (BE .NET) · gate tiện ích `TI_ChatHub`. Lọc fanpage chạy qua stored proc. Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Khu vực | Ghi chú |
|---|---|---|
| Phân quyền Fanpage | ChatHub | gán user vào fanpage + cấu hình quyền tương tác; bộ lọc fanpage theo user/role |

## Endpoint / Controller (trace registry)

**FBUserFanpagesController** (service FacebookChat, auth **mixed/internal**, gate `TI_ChatHub`) — registry `registry/marketing/FBUserFanpagesController.md`:
| # | Hành động | Endpoint | Quyền (key) |
|---|---|---|---|
| 1 | Page của tôi | `GET api/FBUserFanpages/LayTatCaFanpageCuaUser` | `FBUserFanpages_FacebookChat_LayTatCaFanpageCuaUser` |
| 2 | User trong 1 fanpage | `GET .../LayTatCaUserTrongFanpage/{IdFanPage:Guid}` | `FBUserFanpages_FacebookChat_LayTatCaUserTrongFanpage` |
| 3 | Tất cả user | `GET .../LayTatCaUser` | `FBUserFanpages_FacebookChat_LayTatCaUser` |
| 4 | Lọc fanpage (V2) | `POST .../TimTheoDieuKienAsync` | `FBUserFanpages_FacebookChat_TimTheoDieuKienAsync` |
| 5 | Lọc user quản lý 1 page | `POST .../LocUserManagerPage?fanpageId=` | `FBUserFanpages_FacebookChat_LocUserManagerPage` |
| 6 | Thêm (phân quyền) | `POST .../ThemMoi` | `FBUserFanpages_FacebookChat_ThemMoi` |
| 7 | Tìm theo Id | `GET .../TimTheoId/{id:Guid}` | `FBUserFanpages_FacebookChat_TimTheoId` |
| 8 | Cập nhật quyền | `POST .../CapNhatThongTin` | `FBUserFanpages_FacebookChat_CapNhatThongTin` |
| 9 | Xóa fanpage / rời page | `POST .../Xoa/{id}` | `FBUserFanpages_FacebookChat_Xoa` |
| 10 | Xóa 1 bản ghi phân quyền | `POST .../XoaUF/{id:Guid}` | `[SBAuthorize]` (**KHÔNG có key** — lỗ hổng) |

## Quy tắc nghiệp vụ (phân quyền lọc fanpage)
- **Gate tiện ích:** `[TienIch("FBUserFanpagesController", ..., MaTienIchSCRM.TI_ChatHub)]`.
- **`TimTheoDieuKienAsync` rẽ nhánh role** → 4 stored proc: Admin / Manager / Leader / User (xem flow). Tham số chung: `@IdDonVi, @IdChiNhanh, @KeyWord, @PreferredPageIds, @PageIndex, @PageSize` (+ `@IdUser` cho manager/leader/user). Không khớp vai → "bạn không có quyền xem".
- **`ThemMoi`:** chỉ `CheckUserLoginIsRoleAdminChiNhanh` hoặc `CheckUserLoginIsRoleAdmin`; chặn trùng (IdFBFanpage + IdUser + DonVi); tự suy ra PhongBan/Nhom của user.
- **`Xoa`:** admin/super/admin-CN bị chặn (hướng dẫn liên hệ kỹ thuật); user thường tự gỡ quyền của chính mình.

## Entities / Stored procs
- **FBUserFanpage** — 9 cờ quyền + IdUser/IdFBFanpage/FanpageId/IdPhongBanFanpage/IdNhomNhanVienFanpage.
- **FBFanpage** — FanPageId, Ten, AccessToken, PageToken. **User**.
- Stored procs lọc: `PRC_SCRM_Facebook_Chat_FilterFanpage_Admin` / `_Manager` / `_Leader` / `_User`.

## Luồng code
- `FBUserFanpagesController` → `IFBUserFanpageService` (EF join + SqlSugar stored proc), `IUserService.GetAllUser`, `ICommonCacheService` (cache fanpage + `GetCache_CoCauToChuc` phòng/nhóm).
- Lọc fanpage: toàn bộ LINQ cũ đã comment → chạy 100% qua 4 stored proc theo vai.

## Bug-prone / lưu ý (lỗ hổng — chuyển đội security)
- **`FBUserFanpagesController.cs:130-143` (`XoaUF`)** — `[SBAuthorize]` **không permission key** + `DeleteAsync(id)` không verify chủ sở hữu/đơn vị → ai có TI_ChatHub đều xóa được bản ghi phân quyền bất kỳ. **Lỗ hổng phân quyền.**
- **`TimTheoId` (`GetByIdAsync` base)** — không lọc đơn vị/chi nhánh → **rò rỉ ngang tenant** (xem chi tiết bản ghi của đơn vị khác).
- **`FBUserFanpagesController.cs:118-122` (`Xoa`)** — nhánh admin chỉ trả message, `AdminXoaFanpage` thật bị comment (`:121`) → admin không xóa được fanpage qua UI. Lưu ý: `id` ở đây là **FanPageId (long)**, không phải Guid record.
- **`FBUserFanpageService.cs:428` (`FilterUserManagerFanpage`)** — `Guid.Parse(fanpageId)` không try-parse → **500** khi fanpageId không phải Guid.
- **`FBUserFanpageService.cs:527-553` (`PhanQuyenTuongTacFanpage`)** — `userRes` (`FirstOrDefault`) không null-check trước khi đọc `IdPhongBan/IdNhomNhanVien` → **NullRef** nếu IdUser không tồn tại.
- **`UpdateAsync` (`:554-567`)** — chỉ mark dirty 9 cờ quyền → đổi field khác (IdUser, IdFBFanpage) không lưu.
- **`FBUserFanpageService.cs:146`** — `!strIdFanpageUuTiens.IsNullOrEmpty()` gọi trên biến có thể null (gán null ở nhánh else `:138`) → an toàn (extension), nhưng logic gán-null-rồi-check dư thừa.
- Đổi logic lọc → phải sửa **stored proc** (LINQ cũ đã comment `:154-191`, `:236-281`, `:303-350`, `:372-415`), không sửa C#.

## File source (path:line)
- Registry: `projects/scrm/registry/marketing/FBUserFanpagesController.md`.
- BE (service FacebookChat): `D:\PROJECT\Git\_SourceForCode\Group2\scrm\...\FBUserFanpagesController.cs` (`:118-122`, `:121`, `:130-143`), `...\FBUserFanpageService.cs` (`:146`, `:428`, `:527-553`, `:554-567`).
