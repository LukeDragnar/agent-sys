---
controller: UserSessionController
module: he-thong
service: Authen
man_hinh_ui: "Hệ thống/Đơn vị > Thiết lập bảo mật user: bật/tắt captcha & OTP đăng nhập theo user; đăng ký/hủy/xem Google Authenticator (2FA) cho chính mình, cho user trong đơn vị (Admin DV), và cho toàn hệ thống (HT-AD/HT-CSKH ở QuanTriArea)."
muc_dich: "Quản lý cấu hình bảo mật đăng nhập của user nhân viên: captcha, OTP SMS, và xác thực 2 lớp Google Authenticator (đăng ký/lấy QR/hủy) ở 3 cấp quyền: bản thân, admin đơn vị, hệ thống."
alias: ["thiết lập bảo mật user", "bật captcha đăng nhập", "bật otp đăng nhập", "google authenticator", "2fa", "đăng ký 2fa", "hủy 2fa", "secret key authenticator"]
auth: mixed
state_machine: []
deprecated: false
entities: [UserSession, User]
stored_procs: []
depends: ["SQL Server (AuthenConnectStr) — EF Core (UserSession/User) + EFCore.BulkExtensions", "OtpNet (Base32 + KeyGeneration sinh secret key TOTP)", "IUserPrincipalService (UserLogin để gating + lọc theo đơn vị)", "ILogActivityHelper (ghi lịch sử tác động khi đổi thiết lập)"]
cross_module_in: ["Màn Thiết lập bảo mật user (web) → ThietLapBaoMatUser, DangKy/LayThongTin/HuySuDungGoogleAuthenticator (self), AdminDV_* (admin đơn vị)", "Khu QuanTriArea (CSKH/Admin hệ thống) → System_* GoogleAuthenticator", "Service nội bộ → TimTheoDieuKien, ThietLapBaoMatUser (PrivateSBAuthorize)"]
cross_module_out: ["AuthenService.LoginV2 (đọc UserSession.IsUseCaptchaLogin/IsUseOtpLogin/IsUseGoogleAuthenticator để rẽ nhánh login)"]
---

# UserSessionController (Authen) — Thiết lập bảo mật user (captcha / OTP / 2FA Google Authenticator)

Node này **gộp 2 file cùng tên class** `UserSessionController` trong service Authen:
- **`Authen.API/Controllers/UserSessionController.cs`** — route `api/UserSession`: tìm kiếm + thiết lập captcha/OTP (PrivateSBAuthorize), và đăng ký/lấy/hủy Google Authenticator cho **bản thân** (`[SBAuthorize()]`) + cho **user trong đơn vị** (`AdminDV_*`, role DV-AD-TONG).
- **`Authen.API/Areas/SanboxArea/UserSessionController.cs`** — route `api/QuanTriArea/UserSession`: các endpoint `System_*` Google Authenticator cho **CSKH/Admin hệ thống** (role HT-AD, HT-CSKH), có chặn thêm `CurrentUser.IsSuperUser != true && IsCSKH != true → 403`.

Logic chung ở `IUserSessionService` (`Authen.Service/Generated/UserSessionService.cs`). Tham số `IsSystem` (true = QuanTriArea, false = đơn vị/self) quyết định phạm vi tìm user. `UserSession` 1-1 với `User` (cùng `Id`). Các cờ `IsUseCaptchaLogin`/`IsUseOtpLogin`/`IsUseGoogleAuthenticator` được `AuthenService.LoginV2` đọc để bắt thêm bước xác thực.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm danh sách bảo mật user | "Tìm kiếm" (grid) | POST `api/UserSession/TimTheoDieuKien` | — | internal `[PrivateSBAuthorize]` | Màn thiết lập | Join User ⟕ UserSession theo Id, lọc theo `userLogin.DonViId`, KeyWord theo Ten/UserName; trả cờ IsLockedOut/AccessFailed/IsUseCaptchaLogin/IsUseOtpLogin + role; phân trang. | EF (User join UserSession) | Khối Sorts if/else giống hệt nhau (luôn sort theo IsAdmin/IsAdChiNhanh/IsNhanVien) → tham số Sorts client bị bỏ qua |
| 2 | Thiết lập captcha/OTP | "Lưu thiết lập" | POST `api/UserSession/ThietLapBaoMatUser` | — | internal `[PrivateSBAuthorize]` | Màn thiết lập | `IsAllUser` → set cho mọi user của đơn vị; hoặc theo `UserId` đơn lẻ. Set `IsUseCaptchaLogin`/`IsUseOtpLogin` (chỉ field khác null), BulkInsertOrUpdate trong transaction + ghi LogActivity (so old/new). | EF + BulkExtensions + LogActivity | — |
| 3 | Đăng ký 2FA (self) | "Đăng ký Google Authenticator" | POST `api/UserSession/DangKySuDungGoogleAuthenticator` | — (`[SBAuthorize()]`) | login | User tự bật 2FA | `DangKySuDungGoogleAuthenticator(IsSystem=false, CurrentUser.Id, ...)`: tìm user theo (Id, DonViId) → sinh secret key Base32 (20 bytes) → set `IsUseGoogleAuthenticator=true` + `GoogleAuthenticatorSecretKey` → trả URL `otpauth://totp/SandboxVN:<user>?secret=...`. | EF (UserSession) + OtpNet | Secret key lưu **plaintext** trong DB (không mã hóa) |
| 4 | Lấy thông tin 2FA (self) | "Xem mã QR" | POST `api/UserSession/LayThongTinGoogleAuthenticator` | — (`[SBAuthorize()]`) | login | Đã/đang đăng ký | Trả `UserSession`; nếu có SecretKey thì bọc lại thành otpauth URL để hiện QR. | EF (UserSession) | Trả nguyên object UserSession (lộ cờ nội bộ); SecretKey để render QR |
| 5 | Hủy 2FA (self) | "Hủy Google Authenticator" | POST `api/UserSession/HuySuDungGoogleAuthenticator` | — (`[SBAuthorize()]`) | login | Đang bật 2FA | Set `IsUseGoogleAuthenticator=false` + xóa SecretKey. | EF (UserSession) | — |
| 6 | Đăng ký/Lấy/Hủy 2FA cho user trong đơn vị (Admin DV) | "(Admin) Bật/Xem/Hủy 2FA" | POST `api/UserSession/AdminDV_DangKySuDungGoogleAuthenticator` · `AdminDV_LayThongTin...` · `AdminDV_HuySuDung...` | `UserSession_AdminDV_DangKySuDungGoogleAuthenticator` / `..._LayThongTinGoogleAuthenticator` / `..._HuySuDungGoogleAuthenticator` | login (role DV-AD-TONG) | Admin đơn vị thao tác cho NV | Gọi service với `IsSystem=false`, `payload.IdUser/UserName`. Service gate: tìm user theo (Id, DonViId của admin); chặn thao tác lên SuperUser/CSKH khác mình. | EF (UserSession) | Truyền `IsSystem=false` → service tìm user theo `userLogin.DonViId` (đúng phạm vi đơn vị) |
| 7 | Đăng ký/Lấy/Hủy 2FA cấp hệ thống (CSKH/Admin) | "(Hệ thống) Bật/Xem/Hủy 2FA" | POST `api/QuanTriArea/UserSession/System_DangKySuDungGoogleAuthenticator` · `System_LayThongTin...` · `System_HuySuDung...` | `UserSession_System_DangKySuDungGoogleAuthenticator` / `..._LayThongTinGoogleAuthenticator` / `..._HuySuDungGoogleAuthenticator` | login (role HT-AD, HT-CSKH) | QuanTriArea | Chặn thêm `IsSuperUser!=true && IsCSKH!=true → 403`. Gọi service `IsSystem=true` → tìm user toàn hệ thống (không giới hạn đơn vị); gate: CSKH không thao tác được SuperUser/CSKH khác, SuperUser không thao tác SuperUser khác mình. | EF (UserSession) | File ở SanboxArea, `[TienIch(..., isHidden=true)]` |

## Quy tắc nghiệp vụ (gating quản trị 2FA — UserSessionService)
1. **Phạm vi tìm user:** `IsSystem==true && (caller SuperUser||CSKH)` → tìm theo Id bất kỳ đơn vị; ngược lại → tìm theo (Id, `userLogin.DonViId`) (chỉ trong đơn vị mình). Không thấy → "Không tìm thấy người dùng".
2. **Caller là CSKH:** cấm thao tác nếu target `IsSuperUser==true` HOẶC (target `IsCSKH==true` và khác chính mình) → "Không thể thực hiện với người dùng quản trị".
3. **Caller là SuperUser:** cấm thao tác nếu target `IsSuperUser==true` và khác chính mình.
4. **Đăng ký:** chưa có UserSession → tạo mới (insert); đã có → update. Sinh secret key mới mỗi lần đăng ký (đăng ký lại sẽ thay secret → mã 2FA cũ vô hiệu).
5. **System_* (QuanTriArea):** thêm chặn cứng ở controller: `CurrentUser.IsSuperUser != true && IsCSKH != true` → 403, trước khi vào service.

## Quan hệ
- **Gọi sang:** OtpNet (`KeyGeneration.GenerateRandomKey`, `Base32Encoding`), `ILogActivityHelper` (log thiết lập captcha/OTP).
- **Dữ liệu (entities):** `UserSession` (1-1 User: AccessFailed/IsLockedOut/LockoutEndTime + IsUseCaptchaLogin/IsUseOtpLogin/IsUseGoogleAuthenticator/GoogleAuthenticatorSecretKey), `User` (role để gating).
- **Ripple / đồng bộ:** thay đổi các cờ ở đây → `AuthenService.LoginV2`/`KiemTraOtp`/`KiemTraGoogleAuthenticator` thay đổi luồng login (bắt captcha/OTP/2FA).

## Lỗi đã biết / lưu ý kỹ thuật
- `UserSessionService.cs:101-102` — `GoogleAuthenticatorSecretKey` lưu **plaintext** trong DB (không mã hóa) → rủi ro bảo mật nếu rò DB (kẻ tấn công sinh được TOTP).
- `UserSessionService.cs:260-267` — `TimTheoDieuKienAsync`: nhánh `if(Sorts.Any())` và `else` y hệt nhau → Sorts từ client bị bỏ qua.
- `UserSessionService.cs:120-168` — `LayThongTinGoogleAuthenticator` trả nguyên `UserSession` (lộ cờ nội bộ); khi chưa đăng ký vẫn trả object tạm (Id user) chứ không null.
- `UserSessionController.cs (SanboxArea):28-31,44-47,60-63` — `System_*` chặn 403 bằng `IsSuperUser != true && IsCSKH != true` ở controller, nhưng service vẫn nhận `IsSystem=true` và tự gate lại theo role → 2 lớp kiểm tra (an toàn nhưng trùng lặp).
- `KiemTraGoogleAuthenticator` (luồng login 2FA) nằm ở `AuthenService.cs:460-500`, KHÔNG ở controller này — node `AuthenController_Authen` mô tả bước verify TOTP lúc đăng nhập; controller này chỉ lo đăng ký/hủy/xem.
- Đăng ký lại 2FA (`DangKy...`) sinh secret key mới mỗi lần (UserSessionService.cs:95) → người dùng phải quét lại QR; mã trên app cũ ngừng hợp lệ.
