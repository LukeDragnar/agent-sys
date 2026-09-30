---
controller: FBUserFanpagesController
module: marketing
service: FacebookChat
man_hinh_ui: "ChatHub > Phân quyền Fanpage (gán user vào fanpage + cấu hình quyền tương tác: đăng/sửa/xóa bài, comment, inbox); và bộ lọc fanpage theo user/role"
muc_dich: "Quản lý phân quyền tương tác giữa User và Fanpage trong ChatHub: gán quyền (post/comment/inbox), lọc fanpage theo role (admin/manager/leader/user qua stored proc), lọc user quản lý 1 page, và xóa quyền fanpage."
alias: ["phân quyền fanpage", "user fanpage", "gán quyền page", "quyền tương tác page", "FBUserFanpage", "lọc fanpage"]
auth: mixed
state_machine: []
deprecated: false
entities: [FBUserFanpage, FBFanpage, User]
stored_procs:
  - "PRC_SCRM_Facebook_Chat_FilterFanpage_Admin"
  - "PRC_SCRM_Facebook_Chat_FilterFanpage_Manager"
  - "PRC_SCRM_Facebook_Chat_FilterFanpage_Leader"
  - "PRC_SCRM_Facebook_Chat_FilterFanpage_User"
depends:
  - "IFBUserFanpageService (FBUserFanpageService — EF + SqlSugar stored procs)"
  - "IUserService.GetAllUser"
  - "ICommonCacheService (ClearCache_FbFanpage_FanPageId / ClearCache_FbFanpage_DonVi)"
  - "ICommonCacheService.GetCache_CoCauToChuc (phòng ban/nhóm để xác định phạm vi quản lý)"
  - "Core/TienIch (MaTienIchSCRM.TI_ChatHub)"
cross_module_in: []
cross_module_out:
  - "Cache FbFanpage (clear theo FanPageId + DonVi) khi admin xóa fanpage"
  - "Phụ thuộc cơ cấu tổ chức (CoCauToChuc: phòng ban/nhóm) để phân quyền lọc fanpage theo cấp quản lý"
---

# FBUserFanpagesController — Phân quyền User ↔ Fanpage (ChatHub)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy fanpage của user hiện tại | (danh sách page của tôi) | GET `api/FBUserFanpages/LayTatCaFanpageCuaUser` | `FBUserFanpages_FacebookChat_LayTatCaFanpageCuaUser` | internal | Có TI_ChatHub | Join User→FBUserFanpage→FBFanpage theo `IdUser=CurrentUser.Id, IdDonVi` → trả danh sách page (Id, FanPageId, Ten, token...). | EF (3 bảng join) | — |
| 2 | Lấy user trong 1 fanpage | (danh sách user của page) | GET `api/FBUserFanpages/LayTatCaUserTrongFanpage/{IdFanPage:Guid}` | `FBUserFanpages_FacebookChat_LayTatCaUserTrongFanpage` | internal | Có TI_ChatHub | Join User→FBUserFanpage theo `IdFBFanpage=IdFanPage, IdDonVi` → trả user + cờ quyền (post/comment/inbox). | EF | — |
| 3 | Lấy tất cả user | (chọn user để gán) | GET `api/FBUserFanpages/LayTatCaUser` | `FBUserFanpages_FacebookChat_LayTatCaUser` | internal | Có TI_ChatHub | `IUserService.GetAllUser(CurrentUser)` → list user của đơn vị. | EF `User` | — |
| 4 | Lọc fanpage theo điều kiện (V2) | Bộ lọc fanpage | POST `api/FBUserFanpages/TimTheoDieuKienAsync` | `FBUserFanpages_FacebookChat_TimTheoDieuKienAsync` | internal | Có TI_ChatHub | Rẽ nhánh theo role → gọi stored proc tương ứng (Admin/Manager/Leader/User), keyword + PreferredPageIds + phân trang; trả list + total. Role không khớp → "bạn không có quyền xem". | **SP** (4 PRC, xem `stored_procs`) | LINQ cũ đã comment, giờ chạy hoàn toàn qua SP. |
| 5 | Lọc user quản lý 1 page | Bộ lọc user của page | POST `api/FBUserFanpages/LocUserManagerPage?fanpageId=` | `FBUserFanpages_FacebookChat_LocUserManagerPage` | internal | Có TI_ChatHub | Join User→FBUserFanpage theo `IdFBFanpage=Guid.Parse(fanpageId)`, lọc keyword theo tên, phân trang → trả user + cờ quyền. | EF | `Guid.Parse(fanpageId)` không try-parse → 500 nếu fanpageId không phải Guid. |
| 6 | Thêm mới (phân quyền) | Phân quyền tương tác | POST `api/FBUserFanpages/ThemMoi` | `FBUserFanpages_FacebookChat_ThemMoi` | internal | Có TI_ChatHub; caller là admin/admin chi nhánh | `PhanQuyenTuongTacFanpage`: chặn trùng (user đã phân quyền page) → check role admin (chỉ admin/admin chi nhánh) → set DonVi/ChiNhanh + lấy PhongBan/Nhom của user → insert. | EF `FBUserFanpage` | Chỉ admin/admin-chi-nhánh phân quyền được dù SBAuthorize cho nhiều role → role thường gọi sẽ bị từ chối ở service. |
| 7 | Tìm theo Id | (chi tiết phân quyền) | GET `api/FBUserFanpages/TimTheoId/{id:Guid}` | `FBUserFanpages_FacebookChat_TimTheoId` | internal | Có TI_ChatHub | Trả 1 FBUserFanpage theo Id (`GetByIdAsync`). | EF | Không filter đơn vị/chi nhánh → rò rỉ ngang tenant. |
| 8 | Cập nhật quyền | Lưu quyền | POST `api/FBUserFanpages/CapNhatThongTin` | `FBUserFanpages_FacebookChat_CapNhatThongTin` | internal | Có TI_ChatHub | Validate → check tồn tại → `UpdateAsync` mark dirty 9 cờ quyền (IsCreatePost, IsEditPost, IsDeletePost, IsReplyComment, IsEditComment, IsDeleteComment, IsInbox, IsEditInbox, IsDeleteInbox). | EF | Update chỉ lưu 9 cờ quyền; field khác bỏ. |
| 9 | Xóa fanpage (rời page) | Xóa page khỏi tôi | POST `api/FBUserFanpages/Xoa/{id}` | `FBUserFanpages_FacebookChat_Xoa` | mixed | Có TI_ChatHub | Nếu caller là admin/super/admin-chi-nhánh → trả lỗi "Liên hệ kỹ thuật..." (nhánh AdminXoaFanpage đã comment); ngược lại `XoaFanPage`: xóa các FBUserFanpage của chính user cho page đó (`IdUser=CurrentUser.Id`). | EF `FBUserFanpage` | `AdminXoaFanpage` bị vô hiệu hóa (trả message, code thật comment `:121`). `id` ở đây là **FanPageId (long)** chứ không phải Guid record. |
| 10 | Xóa 1 bản ghi phân quyền | (xóa quyền cụ thể) | POST `api/FBUserFanpages/XoaUF/{id:Guid}` | `[SBAuthorize]` (chỉ yêu cầu đăng nhập, KHÔNG có permission key) | internal | Có TI_ChatHub | Check tồn tại theo Id → `DeleteAsync(id)` → true. | EF `FBUserFanpage` | **Thiếu permission key**: `[SBAuthorize]` trần → ai đăng nhập + có TI_ChatHub đều xóa được 1 bản ghi phân quyền bất kỳ (không check chủ sở hữu/đơn vị). |

## Quy tắc nghiệp vụ (gating / phân quyền lọc fanpage theo cấp)
- **Tiện ích gate:** `[TienIch("FBUserFanpagesController", "Quản lý phân quyền user và fanpage", MaTienIchSCRM.TI_ChatHub)]`.
- **`TimTheoDieuKienAsync` rẽ nhánh role (thứ tự ưu tiên):**
  1. Admin tổng / admin chi nhánh / admin nhiều CN / DV-TRUC-PAGE / SuperUser / CSKH → `..._FilterFanpage_Admin` (toàn bộ page đơn vị).
  2. QL-SALE hoặc QL-MKT → `..._FilterFanpage_Manager` (kèm `StrIdPhongNV`, `StrIdNhomNV` từ cơ cấu tổ chức).
  3. SALE-LEAD (không QL-SALE) hoặc MKT-LEAD (không QL-MKT) → `..._FilterFanpage_Leader` (kèm `StrIdNhomNV`).
  4. SALE thuần (không lead/QL) hoặc MKT thuần → `..._FilterFanpage_User` (chỉ page được gán).
  5. Không khớp role nào → "bạn không có quyền xem".
  - Tham số chung mọi nhánh: `@IdDonVi, @IdChiNhanh, @KeyWord, @PreferredPageIds, @PageIndex, @PageSize` (+ `@IdUser` cho manager/leader/user).
- **`ThemMoi` (phân quyền):** chỉ `CheckUserLoginIsRoleAdminChiNhanh` hoặc `CheckUserLoginIsRoleAdmin` được phép; chặn trùng (cùng IdFBFanpage + IdUser + DonVi); tự suy ra PhongBan/Nhom của user từ bảng User.
- **`Xoa`:** admin/super/admin-CN bị chặn (hướng dẫn liên hệ kỹ thuật); user thường chỉ tự gỡ quyền của chính mình trên page.

## Quan hệ
- **Gọi sang:** `IFBUserFanpageService` (EF join + SqlSugar stored proc), `IUserService.GetAllUser`, `ICommonCacheService` (cache fanpage + cơ cấu tổ chức).
- **Dữ liệu (entities):** `FBUserFanpage` (9 cờ quyền + IdUser/IdFBFanpage/FanpageId/IdPhongBanFanpage/IdNhomNhanVienFanpage), `FBFanpage` (FanPageId, Ten, AccessToken, PageToken), `User`.
- **Ripple / đồng bộ:** `AdminXoaFanpage` (hiện disabled) clear cache fanpage theo FanPageId + DonVi; lọc fanpage phụ thuộc dữ liệu cơ cấu tổ chức (phòng ban/nhóm) qua cache.

## Lỗi đã biết / lưu ý kỹ thuật
- `FBUserFanpagesController.cs:130-143` (`XoaUF`) — `[SBAuthorize]` không permission key → mất phân quyền chi tiết; `DeleteAsync(id)` không verify chủ sở hữu/đơn vị → ai có TI_ChatHub đều xóa được bản ghi bất kỳ.
- `FBUserFanpagesController.cs:118-122` (`Xoa`) — nhánh admin chỉ trả message, `AdminXoaFanpage` thật bị comment (`:121`) → admin không xóa được fanpage qua UI.
- `FBUserFanpageService.cs:428` (`FilterUserManagerFanpage`) & service `Guid.Parse(fanpageId)` — không try-parse → 500 khi fanpageId không phải Guid.
- `FBUserFanpageService.cs:146` — `!strIdFanpageUuTiens.IsNullOrEmpty()` gọi trên biến có thể là `null` (đã gán null ở nhánh else `:138`) → `IsNullOrEmpty()` là extension nên an toàn với null, nhưng logic gán null rồi check lại dư thừa.
- `FBUserFanpageService.cs:527-553` (`PhanQuyenTuongTacFanpage`) — `userRes` (`dbUser...FirstOrDefault`) không null-check trước khi đọc `IdPhongBan/IdNhomNhanVien` → NullRef nếu IdUser không tồn tại.
- `GetByIdAsync` (base) ở `TimTheoId` không lọc đơn vị/chi nhánh → rò rỉ ngang tenant.
- `UpdateAsync` (`:554-567`) chỉ mark dirty 9 cờ quyền; thay đổi field khác (vd IdUser, IdFBFanpage) sẽ không lưu.
- Filter chạy hoàn toàn qua 4 stored proc; toàn bộ LINQ cũ đã comment (`:154-191`, `:236-281`, `:303-350`, `:372-415`) — khi đổi logic phải sửa SP, không sửa C#.
