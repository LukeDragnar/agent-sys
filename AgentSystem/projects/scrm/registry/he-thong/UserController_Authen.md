---
controller: UserController
module: he-thong
service: Authen
man_hinh_ui: "nội bộ — không có UI trực tiếp. Hầu hết là endpoint service-to-service (Core ↔ Authen) đồng bộ tài khoản nhân viên; vài action gắn quyền HT/DV cho màn quản trị tài khoản bên Core."
muc_dich: "Quản lý bản ghi tài khoản User của nhân viên TRONG service Authen: đồng bộ từ Core (UserSync), cập nhật/xóa/khóa tài khoản, đổi mật khẩu, đổi trạng thái nghỉ việc, bật/tắt nhận dữ liệu số chia."
alias: ["đồng bộ user authen", "user sync", "khóa tài khoản nhân viên", "đổi mật khẩu nhân viên (private)", "cập nhật tài khoản authen", "bật tắt nhận dữ liệu"]
auth: internal
state_machine: []
deprecated: false
entities: [User, RefreshToken]
stored_procs: []
depends: ["SQL Server (AuthenConnectStr) — EF Core (User/RefreshToken)", "MassTransit IPublishEndpoint (UserDangKyTopicDto publish khi khởi tạo)", "ICommonSyncDataService (EventUserSync/EventListUserSync bắn ngược về Core)", "HybridCache + Redis (xóa cache UserInfo/UserProfile khi thay đổi)", "BCrypt (import nhưng hash thực hiện ở Core)"]
cross_module_in: ["Core service → UserSync, UserSyncMultiObject, DoiMatKhau, KhoaTaiKhoan, ChangeTrangThaiNghiViec, DonViKhoiTaoUserMacDinh, BatTatNhanDuLieu* (PrivateSBAuthorize service-to-service)", "Màn quản trị tài khoản (qua Core proxy) → CapNhapTaiKhoan, CapNhapListTaiKhoan, XoaTaiKhoan (SBAuthorize)"]
cross_module_out: ["Core service (event đồng bộ ngược qua ICommonSyncDataService)", "Redis/HybridCache (revoke cache user + RefreshToken khi đổi chi nhánh/role)", "MassTransit (đăng ký topic user)"]
---

# UserController (Authen) — Quản lý & đồng bộ tài khoản nhân viên trong Authen

Controller này thuộc service **Authen** (hậu tố `_Authen` để tránh trùng `UserController` của AuthenCustomer). Đây là **kho bản sao tài khoản** để Authen tự xác thực: Core là nguồn chân lý (tạo/sửa user, hash mật khẩu), Authen nhận qua các endpoint đồng bộ. Phần lớn endpoint là `[PrivateSBAuthorize]` (service-to-service), chỉ vài action gắn `[SBAuthorize]`/permission key. Logic ở `IUserService` (`Authen.Service/Generated/UserService.cs`). Lưu ý `User.PasswordHash` được nhận thẳng từ req (đã hash ở Core), Authen **không tự hash** ở đây.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Cập nhật tài khoản | "Lưu" (màn QT tài khoản) | POST `api/User/CapNhapTaiKhoan` | — (`[SBAuthorize()]`) | login | Có token nhân viên | `ValidateReq(UPDATE)` → cập nhật thông tin user; nếu currentUser role `HT-CSKH` cho phép đổi IdDonVi/IdChiNhanh; nếu `IsChangeChiNhanh`/`IsChangeRoles` → xóa RefreshToken + cache → revoke phiên. Xóa cache UserInfo. | EF (User/RefreshToken) | — |
| 2 | Cập nhật nhiều tài khoản | "Lưu hàng loạt" | POST `api/User/CapNhapListTaiKhoan` | `User_Authen_CapNhapListTaiKhoan` | login | role HT-AD | Loop ValidateReq(UPDATE) từng item (1 item lỗi → fail cả lô), bulk update, xóa cache từng user. | EF (User) | Fail-fast: 1 bản ghi sai → cả danh sách không lưu |
| 3 | Xóa tài khoản | "Xóa" | POST `api/User/XoaTaiKhoan` | — (`[SBAuthorize()]`) | login | — | Soft-delete list user theo `UserIds` (DeleteAsync) → xóa cache. | EF (User) | — |
| 4 | Đổi mật khẩu (private) | (ngầm từ Core) | POST `api/User/DoiMatKhau` | — | internal `[PrivateSBAuthorize]` | Core gọi | Nhận `PasswordHash` (đã hash) → set PasswordHash + `NgayDoiMatKhau=now` + `ForceChangePassword=false` → token cũ (thoigian<NgayDoiMatKhau) tự vô hiệu. Xóa cache. | EF (User) | Đoạn xóa RefreshToken theo user đã comment → đổi mật khẩu KHÔNG revoke phiên đang mở (chỉ dựa vào claim thoigian) |
| 5 | Khóa / mở khóa tài khoản | "Khóa tài khoản" | POST `api/User/KhoaTaiKhoan` | — | internal `[PrivateSBAuthorize]` | — | Set `User.IsLockedOut = req.IsLock`, xóa cache. | EF (User) | Phần revoke RefreshToken khi khóa đã comment → khóa không cắt phiên đang đăng nhập |
| 6 | Đổi trạng thái nghỉ việc | (ngầm) | POST `api/User/ChangeTrangThaiNghiViec` | — | internal `[PrivateSBAuthorize]` | — | Set `IsDaNghiViec`; nếu nghỉ việc → ép `IsLockedOut=true`. Xóa cache. | EF (User) | — |
| 7 | Khởi tạo user mặc định cho đơn vị | (ngầm khi tạo đơn vị) | POST `api/User/DonViKhoiTaoUserMacDinh` | — | internal `[PrivateSBAuthorize]` | Core tạo đơn vị mới | BulkInsert `ListUser`; nếu có `ListUserRole` → publish `UserDangKyTopicDto` từng user (đăng ký topic quyền). | EF (User) + MassTransit | Danh sách rỗng → "Danh sách user không được bỏ trống" |
| 8 | Đồng bộ user từ Core | (ngầm) | POST `api/User/UserSync` · `api/User/UserSyncMultiObject` | — | internal `[PrivateSBAuthorize]` | Core thay đổi user | Upsert toàn bộ field user (đã tồn tại → update ~70 field, chưa có → add). Xóa cache. Multi: tách tồn-tại/chưa, bulk update + add. | EF (User) | Khối revoke RefreshToken + event sync đã comment → chỉ ghi DB + clear cache |
| 9 | Bật/tắt nhận dữ liệu số chia | "Bật/tắt nhận data" | POST `api/User/BatTatNhanDuLieuChoTaiKhoan` · `...ChoNhieuTaiKhoan` · `...CareDon` | — | internal `[PrivateSBAuthorize]` | Màn phân bổ data | Set `IsNhanDuLieu` (hoặc `IsCareDon`) → bắn event `EventUserSync`/`EventListUserSync` về Core (lỗi event → rollback) → xóa cache UserInfo. | EF (User) + ICommonSyncDataService | `BatTat*` luôn `return ResultWithData(true)` ở cuối kể cả khi `isSuccess=false` (rollback) → caller có thể tưởng thành công (UserService.cs:1162,1222,1274) |

## Endpoint nội bộ (private / service-to-service)
- Hầu hết action #4-#9 là `[PrivateSBAuthorize]` — Core là caller chính (đồng bộ tài khoản, khóa/đổi mật khẩu, phân bổ data).
- `ThemMoiTaiKhoan` đã bị comment (không còn tạo mới qua Authen — tạo ở Core rồi sync sang).

## Quan hệ
- **Gọi sang:** MassTransit publish `UserDangKyTopicDto`; `ICommonSyncDataService.EventUserSyncById/EventListUserSyncByIds` (bắn về Core).
- **Dữ liệu (entities):** `User` (bản sao tài khoản nhân viên trong Authen, ~70 field), `RefreshToken` (revoke khi đổi chi nhánh/role).
- **Ripple / đồng bộ:** đổi chi nhánh/role → xóa RefreshToken + cache (cắt phiên); bật/tắt nhận data → event đồng bộ ngược Core.

## Lỗi đã biết / lưu ý kỹ thuật
- `UserService.cs:1162,1222,1274` — `BatTatNhanDuLieu*Async` luôn `return MethodResult<bool>.ResultWithData(true)` ở cuối, kể cả nhánh `isSuccess=false`/đã rollback → trả "thành công" sai.
- `UserService.cs:673-726` — `DoiMatKhau`: toàn bộ phần xóa RefreshToken (revoke phiên) đã bị comment → đổi mật khẩu không log out phiên đang mở; chỉ dựa vào claim `thoigian < NgayDoiMatKhau` để vô hiệu token.
- `UserService.cs:755-762` — `KhoaTaiKhoanAsync`: phần revoke RefreshToken khi khóa đã comment → khóa tài khoản không cắt được phiên đang đăng nhập (token còn hạn vẫn dùng được tới khi hết hạn).
- `UserController.cs:13` — import `BC = BCrypt.Net.BCrypt` nhưng không dùng (Authen nhận PasswordHash đã hash từ Core).
- `UserService.cs:530-544` — `CapNhapListTaiKhoan` fail-fast: 1 item ValidateReq lỗi → return ngay, cả lô không cập nhật.
