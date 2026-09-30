---
controller: FBUserFanpagesController
module: marketing
service: Facebook
man_hinh_ui: "Marketing > Facebook > Phân quyền user theo fanpage (gán user nào quản lý page nào + quyền chi tiết: tạo/sửa/xóa post, trả lời/sửa/xóa comment, inbox); và dropdown chọn fanpage/user"
muc_dich: "Quản lý mapping user ↔ fanpage (FBUserFanpage) trong đơn vị: liệt kê fanpage của 1 user, user của 1 fanpage, lọc, CRUD bản ghi phân quyền chi tiết (IsCreatePost/IsReplyComment/IsInbox...)."
alias: ["phân quyền fanpage", "user quản lý page", "gán user vào page", "FBUserFanpage", "quyền chat page"]
auth: public
state_machine: []
deprecated: false
entities: [FBUserFanpage, FBFanpage, User]
stored_procs: []
depends:
  - "IFBUserFanpageService (Facebook.Service.Generated — kế thừa BaseService<FBUserFanpage>)"
  - "IUserService.GetAllUser — danh sách user của đơn vị"
cross_module_in:
  - "Facebook UI — màn phân quyền user-fanpage + dropdown chọn page/user"
cross_module_out: []
---

# FBUserFanpagesController (Facebook) — Phân quyền user ↔ fanpage

> CRUD + tra cứu mapping user↔fanpage. Không có `[TienIch]`. Mọi endpoint `[SBAuthorize]` key rỗng (chỉ đăng nhập + 6 role MKT/AD). **Lưu ý bảo mật:** một số truy vấn select cả `AccessToken`/`PageToken` của fanpage ra ngoài (xem known-issue).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy tất cả fanpage của user | (dropdown page) | GET `api/FBUserFanpages/LayTatCaFanpageCuaUser` | (key rỗng — 6 role) | public | — | Join User→FBUserFanpage→FBFanpage theo `IdDonVi` + `userId = CurrentUser.Id`; trả list page (gồm cả AccessToken/PageToken). | EF (3 bảng) | **Lộ AccessToken + PageToken** trong projection (FBUserFanpageService.cs:146-147). |
| 2 | Lấy tất cả user trong 1 fanpage | (danh sách user của page) | GET `api/FBUserFanpages/LayTatCaUserTrongFanpage/{IdFanPage:Guid}` | (key rỗng — 6 role) | public | — | Join User→UF theo `IdDonVi` + `IdFBFanpage`; trả user + cờ quyền (IsCreatePost...IsDeleteInbox). | EF | — |
| 3 | Lấy tất cả user (đơn vị) | (dropdown user) | GET `api/FBUserFanpages/LayTatCaUser` | (key rỗng — 6 role) | public | — | `IUserService.GetAllUser(CurrentUser)`. | EF (User) | — |
| 4 | Lọc fanpage của user (phân trang) | Tìm kiếm | POST `api/FBUserFanpages/TimTheoDieuKienAsync` | (key rỗng — 6 role) | public | — | `FilterFanpageOfUser`: join theo user hiện tại + đơn vị + chi nhánh; KeyWord theo Ten hoặc FanPageId (TryParse); phân trang, OrderByDescending NgayTao, trả total. | EF (3 bảng) | **Lộ AccessToken + PageToken** trong projection (FBUserFanpageService.cs:56-57). Hai nhánh Sorts giống nhau (vô tác dụng). |
| 5 | Lọc user quản lý 1 fanpage | Tìm kiếm | POST `api/FBUserFanpages/LocUserManagerPage?fanpageId` | (key rỗng — 6 role) | public | — | `FilterUserManagerFanpage`: join User→UF theo `IdFBFanpage == Guid.Parse(fanpageId)` + đơn vị/chi nhánh; KeyWord theo tên user; phân trang + cờ quyền. | EF | `Guid.Parse(fanpageId)` không guard → throw nếu fanpageId rỗng/sai định dạng. Hai nhánh Sorts giống nhau. |
| 6 | Xem 1 mapping | (mở chi tiết) | GET `api/FBUserFanpages/TimTheoId/{id:Guid}` | (key rỗng — 6 role) | public | — | `GetByIdAsync(id)`. | EF | Không re-check đơn vị. |
| 7 | Thêm mapping | Thêm | POST `api/FBUserFanpages/ThemMoi` | (key rỗng — 6 role) | public | ModelState valid | Gán `IdDonVi` từ CurrentUser; `InsertAsync` override: check trùng (IdFBFanpage+IdUser+IdDonVi) → "Đã tồn tại", còn lại insert. | EF | — |
| 8 | Cập nhật quyền mapping | Lưu | POST `api/FBUserFanpages/CapNhatThongTin` | (key rỗng — 6 role) | public | Bản ghi tồn tại | GetById → null thì MESS_NotFound; `UpdateAsync` override MarkDirty 9 cờ quyền (IsCreatePost/IsEditPost/IsDeletePost/IsReplyComment/IsEditComment/IsDeleteComment/IsInbox/IsEditInbox/IsDeleteInbox). | EF | Logic check trùng khi đổi IdUser/IdFBFanpage bị comment-out → cập nhật có thể tạo cặp trùng. |
| 9 | Xóa mapping | Xóa | POST `api/FBUserFanpages/Xoa/{id:Guid}` | (key rỗng — 6 role) | public | Bản ghi tồn tại | GetById → null thì MESS_NotFound; `DeleteAsync(id)`. | EF | Không re-check đơn vị trước xóa. |

## Quan hệ
- **Gọi sang:** không (EF nội bộ + IUserService).
- **Dữ liệu (entities):** `FBUserFanpage` (chính, gồm 9 cờ quyền chi tiết), `FBFanpage`, `User`.
- **Ripple / đồng bộ:** không event cross-service; cờ quyền ở đây dùng cho UI phân quyền thao tác post/comment/inbox.

## Lỗi đã biết / lưu ý kỹ thuật
- `FBUserFanpageService.cs:56-57` & `:146-147` — projection select cả `AccessToken` + `PageToken` của fanpage rồi trả ra client (lộ token nhạy cảm). Khác với FBFanpageController_Facebook đã chủ động ẩn AccessToken="".
- `FBUserFanpageService.cs:91` — `Guid.Parse(fanpageId)` trong LINQ không guard → 500 khi fanpageId sai/rỗng.
- `FilterFanpageOfUser`/`FilterUserManagerFanpage` — hai nhánh `if(Sorts)/else` cùng `OrderByDescending(NgayTao)` → Sorts vô tác dụng.
- `UpdateAsync` (FBUserFanpageService.cs:206-248) — block check trùng IdUser/IdFBFanpage comment-out → đổi mapping có thể tạo cặp (user,page) trùng dù InsertAsync có chặn.
