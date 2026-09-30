---
controller: AuthenController
module: he-thong
service: Authen
man_hinh_ui: "Màn đăng nhập back-office/portal nhân viên (web app + mobile app nhân viên): login mã hóa AES, login Google Authenticator (2FA), login OTP, refresh token, logout. Không có màn quản trị riêng."
muc_dich: "Xác thực NHÂN VIÊN / back-office: đăng nhập (AES + captcha + khóa thiết bị + OTP/2FA), làm mới token, đăng xuất; login đối tác (Partner Login/RefreshToken trả token thẳng)."
alias: ["đăng nhập nhân viên", "login back-office", "login web app", "đăng nhập 2FA", "google authenticator login", "login otp", "refresh token nhân viên", "auth staff"]
auth: mixed
state_machine: []
deprecated: false
entities: [User, UserSession, UserDeviceSession, RefreshToken, UserDeviceLog, UserDevice, DonVi, HopDong]
stored_procs: [PRC_SCRM_Core_UserSession_Login]
depends: ["SQL Server (AuthenConnectStr) — EF Core (User/UserSession/RefreshToken) + SqlSugar (UserDeviceSession/UserDevice/DonVi/HopDong)", "Redis (RefreshToken cache + session GoogleAuthenticator)", "HybridCache (DonViCauHinh + DeviceSession)", "BCrypt (verify password)", "JWT (SB.Core.Token, AppSettings.Secret)", "AES_ECB_128 (mã hóa payload login/refresh)", "OtpNet (TOTP Google Authenticator)", "IOtpService (OTP SMS đăng nhập)", "LoginAttemptService (chặn IP/UA + captcha)", "ImageCaptchaService", "Notification service /api/Notifications/removetoken (logout)"]
cross_module_in: ["Web app / mobile app nhân viên → login-encrypt, login-google-authenticator, loginotp-encrypt, tokenrefresh-encrypt, logout", "Đối tác (Partner) → Login, RefreshToken (token trả thẳng)"]
cross_module_out: ["Notification service (xóa token Firebase khi logout)", "Redis/HybridCache (cache + revoke refresh token, device session)", "UserDeviceLog (ghi log đăng nhập async)"]
---

# AuthenController — Xác thực nhân viên / back-office (login + 2FA + OTP)

Đây là controller xác thực **nhân viên (back-office/portal)** của service **Authen** — KHÁC với `AuthenController` của service AuthenCustomer (khách hàng cuối), nên file có hậu tố `_Authen`. Toàn bộ logic nằm trong `AuthenService` (`Authen.API/Service/AuthenService.cs`). User được định danh theo **(UserName, Domain)** chứ không theo IdDonVi; `Domain` resolve từ header `SB-Origin`/`Origin`/`Referer` (hàm `GetDomain`). Mobile bọc payload login/refresh bằng **AES_ECB_128**; desktop set cookie HttpOnly (token/refresh/uid/exp/errCount). Controller chỉ `[ApiController]` — chỉ `logout` có `[SBAuthorize]`; login/refresh là public và tự verify password/token bên trong.

JWT (V2): claim `id`, `thoigian` (=timestamp mốc tạo token / `NgayTao` của session, dùng vô hiệu token cũ khi đổi mật khẩu), `domain`, `device` (=sessionId). Hết hạn `AppSettings.ExpireTime ?? 1440` phút (~1 ngày). **Có HAI cơ chế khóa song song**: (a) `LoginV2`/`KiemTraOtp` dùng `UserSession.AccessFailed` + `LoginAttemptService` (chặn IP/UA, khóa 15 phút), (b) hàm `Login`/`IsLockedOutAsync` cũ dùng `User.AccessFailed >= 100` khóa 1 giờ — `login-encrypt` đi qua `LoginV2` nên dùng cơ chế (a).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Đăng nhập | "Đăng nhập" | POST `api/Authen/login-encrypt` | — | public (tự verify) | Màn login | AES-decrypt UserName/Password (lỗi → "Tài khoản và mật khẩu không hợp lệ."); header `SB-Device`=mobile → mã hóa AES kết quả, desktop → set cookie + đếm errCount. `LoginV2`: chặn IP/UA (15') → captcha nếu vượt ngưỡng → tìm User theo (UserName, Domain) → BCrypt.Verify → check hợp đồng đơn vị → duyệt thiết bị (nếu đơn vị bật) → OTP/2FA nếu UserSession bật → sinh JWT V2 + UserDeviceSession, ghi log async. | EF (User/UserSession) + SqlSugar + BCrypt + JWT + Redis | `appletest` trên desktop bị chặn cứng (AuthenController.cs:103). Thành công desktop set cookie token/refresh **plaintext-trong-cookie** dù response mã hóa |
| 2 | Đăng nhập 2FA (Google Authenticator) | "Nhập mã Google Authenticator" | POST `api/Authen/login-google-authenticator` | — | public (tự verify) | Sau login bước 1 trả mã "05" + IdGoogleAuthenticator | `KiemTraGoogleAuthenticator`: lấy session login đã cache Redis theo `IdGoogleAuthenticator` (TTL 3') → check UserSession `IsUseGoogleAuthenticator` + có SecretKey → verify TOTP (`OtpNet.Totp`, window RFC) → trả token đã cache, xóa key Redis. | Redis + UserSession + OtpNet | Không nhận `model` từ controller cho mobile branch sai-mã (vẫn trả Encrypt rỗng). Window xác thực = RfcSpecifiedNetworkDelay (±1 step) |
| 3 | Đăng nhập OTP (SMS) | (ngầm khi đơn vị bật OTP) | POST `api/Authen/loginotp-encrypt` | — | public (tự verify) | Login trả "04" yêu cầu OTP | AES-decrypt user/pass → `KiemTraOtp`: verify pass → check thiết bị → nếu `IsUseOtpLogin` thì gọi `IOtpService.KiemTraOtp` (KieuLoai=DangNhap) → sinh token. | EF + IOtpService + JWT | Cùng pattern khóa IP/UA như login |
| 4 | Làm mới token | (ngầm khi token hết hạn) | POST `api/Authen/tokenrefresh-encrypt` | — | public (tự verify) | Token sắp/đã hết hạn | Mobile: AES-decrypt `RefreshToken`; desktop: lấy từ cookie `<prefix>refreshToken`. Rỗng → "RefreshToken không hợp lệ". Tra Redis `RefreshToken:<token>` → DB `UserDeviceSession` → check NgayHetHan + đơn vị + duyệt thiết bị; nếu đơn vị 1-thiết-bị thì revoke session khác → sinh JWT mới (giữ refresh token cũ). Desktop set lại cookie token+exp. | SqlSugar (UserDeviceSession) + JWT + Redis | try/catch bao toàn hàm **nuốt mọi exception** → mọi lỗi đều trả "RefreshToken không hợp lệ" (AuthenController.cs:479) |
| 5 | Đăng xuất | "Đăng xuất" | POST `api/Authen/logout` | — | login `[SBAuthorize]` | Đã login | Lấy refresh token từ cookie hoặc body → `LogOut`: xóa các RefreshToken (hết hạn HOẶC == token này) khỏi DB + Redis → gọi Notification `removetoken` (async) → clear cookie token/exp/refresh/uid/error. | EF (RefreshToken) + HTTP Notification | RefreshToken rỗng → trả Logout=true (no-op), không revoke server-side. `LogOut` thao tác bảng **RefreshToken** (cũ) trong khi login V2 ghi **UserDeviceSession** → logout không xóa được session V2 |
| 6 | Kiểm tra cần OTP | (ngầm) | POST `api/Authen/check-need-otp` | — | public | — | **Luôn trả `false`** (thân hàm đã comment, hardcode `ResultWithData(false)`). | (không query) | Endpoint vô hiệu hóa logic, chỉ giữ chữ ký |
| 7 | Hiển thị captcha? | (ngầm khi gõ username) | POST `api/Authen/getuserusecaptcha` | — | public | Desktop | AES-decrypt UserName → `GetUserIsUseCaptchaLogin`: tra User theo (UserName, Domain) → trả `UserSession.IsUseCaptchaLogin`. | EF (User/UserSession) | — |
| 8 | Login đối tác | (API partner) | POST `api/Authen/Login` · POST `api/Authen/RefreshToken` | — | public | Tích hợp đối tác | `Login` gọi `LoginV2` "website" rồi **trả token thẳng** (không mã hóa AES, không cookie); `RefreshToken` gọi service trả thẳng. | EF + JWT | Trả token plaintext trong body — dành cho server-to-server đối tác |

## Quy tắc nghiệp vụ (gating đăng nhập — LoginV2)
1. **Chặn brute-force IP/UA:** `LoginAttemptService.IsBlocked(ip, ua)` → "...thử lại sau 15 phút" (code "05"). `ShouldShowCaptcha` → bắt nhập captcha (code "03"), sai captcha → `RegisterFailedAttempt`.
2. **Tài khoản:** không có (UserName, Domain) → "Thông tin đăng nhập không chính xác" + đếm sai. Tạo `UserSession` mới nếu chưa có.
3. **Khóa session:** `UserSession.IsLockedOut && LockoutEndTime > now` → báo còn N phút.
4. **Verify BCrypt sai:** `UserSession.AccessFailed+1` + `RegisterFailedAttempt`; nếu lúc này `IsBlocked` → set `IsLockedOut=true`, `LockoutEndTime=now+15'`.
5. **Captcha theo user:** `UserSession.IsUseCaptchaLogin==true` → bắt captcha riêng.
6. **Hợp đồng đơn vị:** nếu account KHÔNG phải IsTest/IsSuperUser/IsCSKH → tra `HopDong` theo IdDonVi; null hoặc `NgayKetThuc < hôm nay` → chặn ("...hết hạn sử dụng").
7. **Duyệt thiết bị:** đơn vị `IsPheDuyetThietBi==true` và user không thuộc SuperUser/CSKH/Test/Admin → `UserDevice` phải `IsApproved` (lần đầu auto-insert `IsApproved=false`) → "Thiết bị chưa được duyệt".
8. **OTP đăng nhập:** `UserSession.IsUseOtpLogin==true` & chưa có `otp` → gửi OTP SMS, trả code "04".
9. **2FA Google Authenticator:** `UserSession.IsUseGoogleAuthenticator==true` → cache response token vào Redis (TTL 3'), trả code "05" + IdGoogleAuthenticator để client gọi `login-google-authenticator`.
10. **1 thiết bị:** đơn vị `IsLoginOneDevice==true` → chạy SP `PRC_SCRM_Core_UserSession_Login` revoke session cũ; ngược lại `removeOldRefreshTokens` + insert session.

## Quan hệ
- **Gọi sang:** Notification (`removetoken`), IOtpService (gửi/kiểm tra OTP SMS), SP `PRC_SCRM_Core_UserSession_Login`.
- **Dữ liệu (entities):** `User` (UserName/Domain/PasswordHash BCrypt, AccessFailed/LockoutEndDate cũ, IsSuperUser/IsCSKH/IsTest/IsAdmin), `UserSession` (AccessFailed/IsLockedOut/LockoutEndTime + IsUseCaptchaLogin/IsUseOtpLogin/IsUseGoogleAuthenticator/SecretKey), `UserDeviceSession` (TokenRefresh/DeviceType/NgayHetHan +1 tháng), `UserDevice` (duyệt thiết bị), `RefreshToken` (bảng cũ, dùng ở Login()/LogOut), `DonVi` (cấu hình duyệt/1-thiết-bị), `HopDong` (hạn dùng).
- **Ripple / đồng bộ:** logout xóa token Firebase bên Notification; login revoke refresh token cũ trong Redis/HybridCache.

## Lỗi đã biết / lưu ý kỹ thuật
- `AuthenController.cs:479-482` — `TokenRefresh_Encrypt` bọc try/catch toàn hàm nuốt mọi exception → luôn trả "RefreshToken không hợp lệ", khó debug.
- `AuthenController.cs:507-514` — `check-need-otp` hardcode trả `false` (thân logic đã comment) → endpoint không còn tác dụng.
- `AuthenService.cs:70` — ngưỡng `MaxFailedAccessAttemptsBeforeLockout=100` (cơ chế khóa CŨ `Login()/AccessFailedAsync/IsLockedOutAsync`); `login-encrypt` thực tế đi `LoginV2` dùng cơ chế khóa 15' của `LoginAttemptService` → tồn tại 2 hệ khóa song song dễ nhầm.
- `AuthenService.cs:956-958` — `LogOut` thao tác bảng `RefreshToken` (cũ), còn login V2 lưu `UserDeviceSession` → logout V2 **không xóa được session đang dùng** (chỉ clear cookie client + Firebase).
- `AuthenService.cs:707` — JWT vẫn ghi claim `domain` (bind theo domain) ở generateJwtToken/V2 — khác với AuthenCustomer (đã bỏ claim domain).
- `AuthenService.cs:1074-1098` — `GetUserDeviceAsync`: thiết bị lạ tự insert record `IsApproved=false` (mỗi DeviceId mới tạo 1 dòng UserDevice).
- `AuthenController.cs:154-157` — desktop set cookie `COOKIE_TOKEN`/`COOKIE_REFRESH` giá trị JWT/refresh **không mã hóa thêm** (chỉ HttpOnly), dù response body mã hóa AES.
- `AuthenService.cs:864` (RefreshToken): khi đọc UserDeviceSession trực tiếp từ DB không qua cache, dùng `Get_Session_Login` bọc try/catch trả null khi lỗi → lỗi DB cũng thành "RefreshToken không hợp lệ".
