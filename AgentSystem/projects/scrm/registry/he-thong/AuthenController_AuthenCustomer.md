---
controller: AuthenController
module: he-thong
service: AuthenCustomer
man_hinh_ui: "App mobile khách hàng / mini-app Zalo: màn đăng nhập, đăng ký, đổi mật khẩu, cập nhật hồ sơ cá nhân; web portal khách (login-encrypt). Không có màn quản trị nội bộ."
muc_dich: "Xác thực end-user (khách hàng) cho app mobile / mini-app Zalo: đăng ký, login (mã hóa AES cho mobile), refresh token, logout, đổi mật khẩu, cập nhật thông tin cá nhân, login bằng Zalo OAuth."
alias: ["đăng nhập khách hàng", "login app mobile", "đăng nhập mini app", "login zalo", "auth customer", "đăng ký tài khoản khách", "đổi mật khẩu app"]
auth: mixed
state_machine: []
deprecated: false
entities: [User, RefreshToken, DonVi, UserDeviceLog, KhachHang]
stored_procs: []
depends: ["SQL Server (AuthenConnectStr) — EF Core (User/RefreshToken/DonVi)", "Redis (HybridCache: thông tin đơn vị + customer login + refresh token)", "BCrypt (verify/hash password)", "JWT (SB.Core.Token, AppSettings.Secret)", "AES_ECB_128 (mã hóa payload login/refresh cho mobile)", "Google reCaptcha (chỉ desktop, ngưỡng errCount>100)", "Customer service /api/KhachHang/FindOrCreatedPrivate + CapNhatThongTinKhachPrivate", "Core service /api/ChiNhanh/LayListChiNhanhPrivate", "Notification service /api/Notifications/removetoken (khi logout)", "Zalo Graph API graph.zalo.me/v2.0/me/info"]
cross_module_in: ["App mobile / mini-app Zalo / web portal khách → login-encrypt, register, tokenrefresh-encrypt, login-zalo", "Service nội bộ (Customer/Core) → CreateOrUpdatePrivate, CreateMultiPrivateAccount, GetByListIdKhachPrivate (PrivateSBAuthorize service-to-service)"]
cross_module_out: ["Customer service (tạo/cập nhật KhachHang khi register/cập nhật hồ sơ)", "Notification service (xóa token Firebase khi logout)", "Redis (cache refresh token + customer info)"]
---

# AuthenController — Xác thực khách hàng (app mobile / mini-app Zalo)

Đây là controller xác thực **end-user (khách hàng cuối)** của hệ thống, KHÁC với `AuthenController` của service Authen (nhân viên/back-office) — vì vậy file có hậu tố `_AuthenCustomer`. Login dựa trên **username (số điện thoại) + mật khẩu BCrypt**; KHÔNG có luồng OTP. Social login duy nhất là **Zalo OAuth** (lấy số điện thoại từ Zalo Graph). Mobile bọc payload login/refresh bằng **AES_ECB_128**; desktop trả thẳng. Controller-level chỉ `[ApiController]` (không khóa toàn bộ) — mỗi method tự gắn auth: anonymous (register), public-không-attribute (login/refresh — tự verify password/token bên trong), `[SBAuthorizeCustomer]` (cần JWT khách), hoặc `[PrivateSBAuthorize]` (service-to-service).

JWT khách: claim `id`, `thoigian` (timestamp lúc tạo = mốc so với NgayDoiMatKhau để vô hiệu token cũ khi đổi mk), `madonvi`, `device`. Hết hạn mặc định `ExpireTime ?? 1440` phút. Khóa tài khoản: sai mật khẩu ≥ **100 lần** (`MaxFailedAccessAttemptsBeforeLockout`) → khóa 1 giờ.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Đăng ký tài khoản | "Đăng ký" | POST `api/Authen/register` | — | anonymous | Màn đăng ký app | UserName phải match regex SĐT VN (`(84|0[3578 9])...8 số`); Password==PasswordConfirm; chưa tồn tại theo (IdDonVi từ header `MaDonVi`, UserName). Lấy IdChiNhanh default từ CauHinh đơn vị (hoặc chi nhánh đầu tiên qua Core). Gọi Customer `FindOrCreatedPrivate` tạo KhachHang → insert User (PasswordHash BCrypt). | EF (User/DonVi) + HTTP Customer/Core | DonVi lấy theo header `MaDonVi`; thiếu/sai → "Đơn vị không tồn tại". Không check NgayHetHanSuDung khi đăng ký |
| 2 | Đăng nhập | "Đăng nhập" | POST `api/Authen/login-encrypt` | — | public (tự verify) | Màn login | Header `SB-Device`=mobile → mobile flow (mã hóa AES kết quả); desktop → trả thẳng + reCaptcha khi `errCount>100` (cookie). Verify đơn vị (tồn tại + IsDuyetMaDonVi + chưa hết hạn) → tìm User theo (IdDonVi, UserName) → BCrypt.Verify. Sai pass → tăng AccessFailed (khóa khi ≥100). Thành công → sinh JWT + RefreshToken, lưu Redis + DB, log device async. | EF + BCrypt + JWT + Redis | `appletest` trên desktop bị chặn cứng. AES decrypt username/pass đã comment (nhận plaintext). Khóa ở mốc 100 lần rất cao |
| 3 | Làm mới token | (ngầm khi token hết hạn) | POST `api/Authen/tokenrefresh-encrypt` | — | public (tự verify) | Token sắp/đã hết hạn | Mobile: AES-decrypt `RefreshToken` từ body; desktop: lấy từ cookie `<prefix>refreshToken`. Rỗng → "RefreshToken không hợp lệ". Tra Redis→DB, check NgayHetHan + đơn vị + account → sinh JWT mới (giữ nguyên refresh token cũ). Mobile mã hóa lại kết quả. | EF + JWT + Redis | Bọc try/catch nuốt mọi exception → luôn trả "RefreshToken không hợp lệ" |
| 4 | Đăng xuất | "Đăng xuất" | POST `api/Authen/logout` | — | public (login) `[SBAuthorizeCustomer]` | Đã login | Lấy refresh token từ cookie hoặc body → xóa các refresh token (hết hạn HOẶC == token này) khỏi DB + Redis → gọi Notification `removetoken` (xóa token Firebase, async) → clear cookie token/exp/refresh/uid/error. | EF + HTTP Notification | RefreshToken rỗng → trả Logout=true (no-op), không clear server-side |
| 5 | Lấy user hiện tại | (load app) | POST `api/Authen/GetCurrentUser` | — | public (login) `[SBAuthorizeCustomer]` | Đã login | Trả thẳng `CurrentCustomer` (đã resolve trong authorize filter từ JWT + cache). | (từ filter, không query thêm) | — |
| 6 | Đổi mật khẩu | "Đổi mật khẩu" | POST `api/Authen/DoiMatKhau` | — | public (login) `[SBAuthorizeCustomer]` | Đã login | Verify OldPassword (BCrypt); chặn mật khẩu mới trùng cũ; NewPassword==Confirm. Cập nhật PasswordHash + NgayDoiMatKhau (=now) → token cũ (thoigian<NgayDoiMatKhau) tự vô hiệu. Xóa cache user. | EF + BCrypt | So sánh "trùng mật khẩu cũ" bằng `HashPassword(new)==hash` — BCrypt salt ngẫu nhiên nên check này gần như không bao giờ true |
| 7 | Cập nhật hồ sơ | "Lưu thông tin" | POST `api/Authen/CapNhatThongTinCaNhan` | — | public (login) `[SBAuthorizeCustomer]` | Đã login | Chặn SĐT trùng trong cùng đơn vị → gọi Customer `CapNhatThongTinKhachPrivate` → cập nhật User (tên/SĐT/email/giới tính/ngày sinh/địa chỉ). Set cache customer info. | EF + HTTP Customer | Set `MaTinh` 2 lần, thiếu set `MaHuyen` value (chỉ MarkDirty) — địa chỉ huyện có thể không lưu |
| 8 | Đăng nhập Zalo | "Đăng nhập bằng Zalo" | POST `api/Authen/login-zalo` | — | anonymous | Mini-app Zalo | Gọi Zalo Graph `me/info` (header access_token/secret_key/code) lấy số điện thoại → verify đơn vị → nếu account tồn tại: sinh JWT/refresh; nếu chưa: auto-tạo KhachHang + User (pass default `Spa123456@a`) rồi cấp token. | HTTP Zalo + EF + JWT | **BUG: nhánh account tồn tại KHÔNG `return` response → fall-through ra "Đăng nhập thất bại"** (xem lỗi). Mật khẩu default hardcode |

## Endpoint nội bộ (private / service-to-service) — `[PrivateSBAuthorize]`
- **POST `CreateOrUpdatePrivate`** — Customer/admin service tạo-hoặc-update tài khoản khách theo (IdDonVi, UserName); set lại PasswordHash. Dùng khi đồng bộ tài khoản từ hệ back-office.
- **POST `CreateMultiPrivateAccount`** — batch nhiều `AccountCreateReqDto` (loop gọi CreateOrUpdate, bỏ qua item lỗi).
- **POST `GetByListIdKhachPrivate`** — lấy danh sách User theo list IdKhachHang (map cross-service: KhachHang ↔ tài khoản đăng nhập).
- **POST `GetByIdKhach/{idKhachHang}`** — `[SBAuthorize]` (token nhân viên, không phải khách): lấy tài khoản theo IdKhachHang.

## Quy tắc nghiệp vụ (gating đăng nhập)
1. DonVi (theo header `MaDonVi`): không tồn tại → "Đơn vị không tồn tại"; `IsDuyetMaDonVi==false` → "Đơn vị chưa duyệt mã đơn vị"; `NgayHetHanSuDung<=now` (và account không `IsTest`) → "Tài khoản đã hết hạn sử dụng".
2. Account: không có (IdDonVi, UserName) → "Tài khoản hoặc mật khẩu không đúng".
3. Khóa: `IsLockedOut==true` HOẶC `AccessFailed>=100 && LockoutEndDate>now` → báo còn bao lâu mở khóa.
4. Verify BCrypt sai → `AccessFailed+1`; chạm 100 lần lần đầu → `LockoutEndDate=now+1h`. Thành công → reset AccessFailed=0.
5. reCaptcha chỉ áp cho **desktop & errCount>100** (mobile bỏ qua hoàn toàn).

## Quan hệ
- **Gọi sang:** Customer (`FindOrCreatedPrivate`, `CapNhatThongTinKhachPrivate`), Core (`ChiNhanh/LayListChiNhanhPrivate`), Notification (`removetoken`), Zalo Graph API.
- **Dữ liệu (entities):** `User` (tài khoản khách, PasswordHash BCrypt, AccessFailed/LockoutEndDate, NgayDoiMatKhau), `RefreshToken` (Token/NgayHetHan +1 tháng/DeviceId), `DonVi` (mã đơn vị, duyệt, hạn dùng), `UserDeviceLog` (log đăng nhập async).
- **Ripple / đồng bộ:** register/Zalo-login tạo `KhachHang` bên Customer; logout xóa token Firebase bên Notification; đổi mật khẩu/cập nhật hồ sơ làm mới Redis cache customer.

## Luồng xuyên module (đăng ký khách mới qua app)
1. App POST `register` (header `MaDonVi`) → verify đơn vị → resolve IdChiNhanh default (CauHinh hoặc Core ChiNhanh).
2. Gọi Customer `FindOrCreatedPrivate` → tạo/khớp `KhachHang` → insert `User` (BCrypt).
3. App login-encrypt → JWT + RefreshToken; các request sau gắn `Authorization: Bearer` → filter `SBAuthorizeCustomer` resolve `CustomerLogin` (cache Redis 60').

## Lỗi đã biết / lưu ý kỹ thuật
- `AuthenService.cs:758` — `LoginZalo`: nhánh **account đã tồn tại** gọi `MethodResult<object>.ResultWithData(response)` nhưng **thiếu `return`** → rơi xuống cuối hàm trả "Đăng nhập thất bại" (khách Zalo đã có tài khoản không login được; chỉ tài khoản tạo-mới mới ra token).
- `AuthenService.cs:603` — `DoiMatKhau`: check "mật khẩu mới trùng cũ" bằng `BC.HashPassword(new)==user.PasswordHash`; BCrypt salt ngẫu nhiên → so sánh hash gần như luôn false (nên dùng `BC.Verify`).
- `AuthenService.cs:653-666` — `IndividualChangeAsync`: gán `user.MaTinh` 2 lần (dòng 653-654), không gán giá trị `MaHuyen` (chỉ `MarkDirty(MaHuyen)`) → mã huyện có thể không được lưu.
- `AuthenController.cs:152-206` — `TokenRefresh_Encrypt` bọc try/catch nuốt exception → mọi lỗi (decrypt/parse) đều trả chung "RefreshToken không hợp lệ", khó debug.
- `AuthenController.cs:104-105` — AES decrypt UserName/Password đã comment → login nhận username/password **plaintext** (payload chỉ mã hóa ở response cho mobile, không ở request).
- `AuthenService.cs:792,801` — `LoginZalo` đặt mật khẩu mặc định hardcode `"Spa123456@a"` cho tài khoản tạo qua Zalo.
- `AuthenService.cs:61` — ngưỡng khóa `MaxFailedAccessAttemptsBeforeLockout=100` rất cao (brute-force chậm) — lưu ý bảo mật.
- `AuthenService.cs:300-304` — claim `domain` đã comment (không bind token theo domain) → token không ràng buộc origin.
