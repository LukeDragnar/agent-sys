# Dev — ChatHub Sale (Phân quyền Fanpage)

> Service: **FacebookChat** (BE .NET) · FE module ChatHub. Chỉ dành cho dev.
> Trace registry: `registry/marketing/FBUserFanpagesController.md` (controller `FBUserFanpagesController`). Xem thêm `registry/marketing/FBUserFanpagesController_Facebook.md` cho phần chat thật.
> Phạm vi node này = **phân quyền user ↔ fanpage** (gán quyền, lọc fanpage theo cấp), KHÔNG phải toàn bộ chat KH.

## Màn hình & route (FE)
| Màn hình | Ghi chú |
|---|---|
| ChatHub > Phân quyền Fanpage | gán user vào fanpage + cấu hình quyền tương tác (đăng/sửa/xóa bài, comment, inbox) + bộ lọc fanpage theo user/role |

## Endpoint / Controller (trace registry)
`FBUserFanpagesController` (Service `FacebookChat`) — tiện ích gate `[TienIch("FBUserFanpagesController", "Quản lý phân quyền user và fanpage", MaTienIchSCRM.TI_ChatHub)]`:
| # | Hành động | Endpoint | Quyền (key) |
|---|---|---|---|
| 1 | Fanpage của user hiện tại | `GET LayTatCaFanpageCuaUser` | `FBUserFanpages_FacebookChat_LayTatCaFanpageCuaUser` |
| 2 | User trong 1 fanpage | `GET LayTatCaUserTrongFanpage/{IdFanPage}` | `FBUserFanpages_FacebookChat_LayTatCaUserTrongFanpage` |
| 3 | Tất cả user (chọn để gán) | `GET LayTatCaUser` | `FBUserFanpages_FacebookChat_LayTatCaUser` |
| 4 | Lọc fanpage theo điều kiện (V2) | `POST TimTheoDieuKienAsync` | `FBUserFanpages_FacebookChat_TimTheoDieuKienAsync` |
| 5 | Lọc user quản lý 1 page | `POST LocUserManagerPage?fanpageId=` | `FBUserFanpages_FacebookChat_LocUserManagerPage` |
| 6 | Thêm mới (phân quyền) | `POST ThemMoi` | `FBUserFanpages_FacebookChat_ThemMoi` (service chặn chỉ admin/admin-CN) |
| 7 | Tìm theo Id | `GET TimTheoId/{id}` | `FBUserFanpages_FacebookChat_TimTheoId` |
| 8 | Cập nhật quyền | `POST CapNhatThongTin` | `FBUserFanpages_FacebookChat_CapNhatThongTin` |
| 9 | Xóa fanpage (rời page) | `POST Xoa/{id}` | `FBUserFanpages_FacebookChat_Xoa` (admin bị chặn) |
| 10 | Xóa 1 bản ghi phân quyền | `POST XoaUF/{id}` | `[SBAuthorize]` (KHÔNG có permission key) |

`TimTheoDieuKienAsync` rẽ nhánh role → gọi 4 stored proc: `PRC_SCRM_Facebook_Chat_FilterFanpage_Admin/Manager/Leader/User`. LINQ cũ đã comment, chạy hoàn toàn qua SP.

## Entities
- **`FBUserFanpage`** — bản ghi phân quyền: 9 cờ quyền (IsCreatePost, IsEditPost, IsDeletePost, IsReplyComment, IsEditComment, IsDeleteComment, IsInbox, IsEditInbox, IsDeleteInbox) + IdUser / IdFBFanpage / FanpageId / IdPhongBanFanpage / IdNhomNhanVienFanpage.
- **`FBFanpage`** — FanPageId, Ten, AccessToken, PageToken.
- **`User`**.
- Stored procs: 4 `PRC_SCRM_Facebook_Chat_FilterFanpage_*`.

## Luồng code
- **Phân quyền (`PhanQuyenTuongTacFanpage`):** chặn trùng → `CheckUserLoginIsRoleAdminChiNhanh` hoặc `CheckUserLoginIsRoleAdmin` → set DonVi/ChiNhanh + lấy PhongBan/Nhom của user từ bảng User → insert.
- **Lọc fanpage theo cấp:** rẽ nhánh role (Admin → Manager → Leader → User → từ chối) như mô tả flow; tham số chung `@IdDonVi, @IdChiNhanh, @KeyWord, @PreferredPageIds, @PageIndex, @PageSize` (+ `@IdUser`).
- **Cập nhật quyền (`UpdateAsync`):** mark dirty đúng 9 cờ.
- **Xóa:** admin → trả message (AdminXoaFanpage comment); user thường → `XoaFanPage` xóa bản ghi của chính user cho page.

## Bug-prone / lưu ý (path:line)
> Chỉ ghi nhận — KHÔNG sửa code ở session này. Để session security xử lý.
- **`FBUserFanpagesController.cs:130-143` (`XoaUF`)** — `[SBAuthorize]` không permission key → mất phân quyền chi tiết; `DeleteAsync(id)` không verify chủ sở hữu/đơn vị → ai có TI_ChatHub đều xóa được bản ghi phân quyền bất kỳ.
- **`FBUserFanpagesController.cs:118-122` (`Xoa`)** — nhánh admin chỉ trả message, `AdminXoaFanpage` thật bị comment (`:121`) → admin không xóa được fanpage qua UI. `id` ở đây là **FanPageId (long)**, không phải Guid record.
- **`FBUserFanpageService.cs:428` (`FilterUserManagerFanpage`)** — `Guid.Parse(fanpageId)` không try-parse → 500 khi fanpageId không phải Guid.
- **`FBUserFanpageService.cs:527-553` (`PhanQuyenTuongTacFanpage`)** — `userRes` (`dbUser...FirstOrDefault`) không null-check trước khi đọc `IdPhongBan/IdNhomNhanVien` → NullRef nếu IdUser không tồn tại.
- **`TimTheoId` / `GetByIdAsync`** — không lọc đơn vị/chi nhánh → rò rỉ ngang tenant.
- **`UpdateAsync` (`:554-567`)** chỉ mark dirty 9 cờ quyền; đổi field khác (IdUser, IdFBFanpage) không lưu.
- **`FBUserFanpageService.cs:146`** — `!strIdFanpageUuTiens.IsNullOrEmpty()` trên biến có thể null (đã gán null ở nhánh else `:138`) → an toàn nhờ extension nhưng logic dư thừa.
- **Filter chạy hoàn toàn qua 4 stored proc;** LINQ cũ đã comment (`:154-191`, `:236-281`, `:303-350`, `:372-415`) — đổi logic phải sửa SP, không sửa C#.

## File source
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\FacebookChat\...\Controllers\FBUserFanpagesController.cs`.
- BE service: `FacebookChat\...\FBUserFanpageService.cs`.
- Registry node: `projects/scrm/registry/marketing/FBUserFanpagesController.md` (chat thật: `FBUserFanpagesController_Facebook.md`).
