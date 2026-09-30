---
controller: UserResetController
module: he-thong
service: Core
man_hinh_ui: "Màn Quên mật khẩu (đăng nhập): nhập SĐT → nhận OTP → xác minh OTP → đặt mật khẩu mới"
muc_dich: "Luồng quên/lấy lại mật khẩu cho nhân viên: gửi OTP theo SĐT, xác minh OTP (cấp token tạm), đặt lại mật khẩu — có chống brute-force (lock IP)."
alias: ["UserReset", "quên mật khẩu", "lấy lại mật khẩu", "reset password", "OTP quên mật khẩu"]
auth: anonymous
state_machine: [GuiOTP, XacMinhOTP, DatLaiMatKhau]
deprecated: false
entities: [User, VerificationCode]
stored_procs: []
depends: ["IUserService", "IVerificationCodeService", "IQuenMatKhauService", "IRedisCacheService", "BaseController"]
cross_module_in: ["FE Quên mật khẩu gọi GetUser (gửi OTP) → VeriOtp (xác minh) → CapNhatMatKhau (đặt MK mới)"]
cross_module_out: ["Đặt lại mật khẩu user → ảnh hưởng đăng nhập; lock IP chống brute-force (IQuenMatKhauService) toàn service"]
---

# UserResetController — Quên / lấy lại mật khẩu

Controller route `api/UserReset`, **KHÔNG có `[SBAuthorize]`** (anonymous — màn quên mật khẩu trước đăng nhập). Luồng 3 bước: `GetUser` (gửi OTP theo SĐT) → `VeriOtp` (xác minh OTP, cấp token tạm lưu Redis 5 phút) → `CapNhatMatKhau` (đặt MK mới bằng token). Có **chống brute-force** qua `IQuenMatKhauService` (CheckLockIp/CountRequest/XoaLockIp). Domain lấy từ `GetDomain()`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Gửi OTP theo SĐT | Gửi mã | GET `api/UserReset/GetUser?soDienThoai=` | — (anonymous) | anonymous | Chưa bị lock IP | CheckLockIp → nếu bị lock: "Bạn gọi quá nhiều lần..."; CountRequest; SĐT trống/user không tồn tại → lỗi; ngược lại `TaoMaOtpQMK` type=LayLaiMatKhau gửi OTP. | EF + Redis | Trả "Người dùng không tồn tại" lộ thông tin user tồn tại hay không (user enumeration) |
| 2 | Xác minh OTP | Xác nhận | POST `api/UserReset/VeriOtp` | — (anonymous) | anonymous | Có OTP + phone | CheckLockIp; validate otp/phone; user tồn tại; `CheckOTP` đúng → tạo token GUID lưu Redis `UserForgetPass_{token}` (5 phút) → trả token. OTP sai → CountRequest. | EF + Redis | — |
| 3 | Đặt lại mật khẩu | Lưu MK mới | POST `api/UserReset/CapNhatMatKhau` | — (anonymous) | anonymous | Có token hợp lệ (chưa hết hạn) | Lấy user từ Redis theo token; token hết hạn → "Phiên làm việc hết hạn..."; `QuenMatKhau(Id, NewPassword, ConfirmPassword)`; thành công → XoaLockIp + xóa token Redis. | EF + Redis | Không re-validate độ mạnh/khớp confirm tại controller (giao service QuenMatKhau) |

## Quy tắc nghiệp vụ (anti brute-force / state)
1. GetUser & VeriOtp: `CheckLockIp()` true → chặn "Bạn gọi quá nhiều lần, tạm thời quay lại sau". (dòng 57, 94)
2. GetUser: `CountRequest()` mỗi lần gọi → tích lũy đếm; quá ngưỡng → lock IP. (dòng 61)
3. VeriOtp: OTP sai → `CountRequest()` (tăng đếm brute-force) rồi trả lỗi. (dòng 115)
4. VeriOtp thành công → token GUID lưu Redis 5 phút (`UserForgetPass_{token}`). (dòng 120-121)
5. CapNhatMatKhau thành công → `XoaLockIp()` + xóa token Redis (one-time use). (dòng 152-153)

## Quan hệ
- **Gọi sang:** IUserService (GetUserByPhone, QuenMatKhau), IVerificationCodeService (TaoMaOtpQMK, CheckOTP), IQuenMatKhauService (CheckLockIp/CountRequest/XoaLockIp), IRedisCacheService (token tạm).
- **Dữ liệu (entities):** `User` (đổi mật khẩu), `VerificationCode` (OTP type LayLaiMatKhau).
- **Ripple / đồng bộ:** đặt lại MK → user đăng nhập bằng MK mới; lock IP toàn service.

## Luồng xuyên module
- GetUser → OTP gửi qua SMS/notify (IVerificationCodeService) → VeriOtp cấp token → CapNhatMatKhau đổi MK + gỡ lock IP.

## Lỗi đã biết / lưu ý kỹ thuật
- `UserResetController.cs:65,71` thông báo "Người dùng không tồn tại" khác với trường hợp tồn tại → **user enumeration** (lộ SĐT có/không trong hệ thống).
- Anonymous endpoint — bảo vệ chỉ dựa vào lock IP; không có captcha ở controller.
- `record VeriOTP`/`MatKhau` khai báo ngay trong controller (dòng 132-133).
