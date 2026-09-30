---
controller: VerificationCodeController
module: marketing
service: SMS
man_hinh_ui: "Nội bộ — không có UI quản trị. Endpoint kiểm tra OTP (mã xác minh) gọi từ luồng đăng nhập/đăng ký/xác thực."
muc_dich: "Xác minh mã OTP (VerificationCode) do hệ thống gửi qua SMS: nhận OTP người dùng nhập và kiểm tra hợp lệ."
alias: ["VerificationCode", "OTP", "mã xác minh", "kiểm tra OTP", "xác thực sms"]
auth: anonymous
state_machine: []
deprecated: false
entities: [VerificationCode]
stored_procs: []
depends:
  - "IVerificationCodeService (SMS.Services.Generated)"
  - "SB.Core.SMS"
cross_module_in:
  - "[anonymous] luồng đăng nhập/đăng ký/xác thực gọi `KiemTraOTP` để verify mã OTP đã gửi qua SMS"
cross_module_out: []
---

# VerificationCodeController — Xác minh mã OTP

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Kiểm tra OTP | Xác minh | POST `api/VerificationCode/KiemTraOTP` | (không) | anonymous | — | Nhận `CheckOtpDto` (sđt + mã) → kiểm tra OTP còn hiệu lực/đúng → trả kết quả. | EF (`KiemTraOtp`) | Không auth (đúng bản chất luồng OTP trước đăng nhập). |

## Quan hệ
- **Gọi sang:** `IVerificationCodeService`.
- **Dữ liệu (entities):** `VerificationCode` (mã OTP đã phát + hạn dùng).
- **Ripple / đồng bộ:** OTP được gửi đi qua luồng SMS (SmsSend consumer); controller này chỉ verify chiều nhận lại.

## Lỗi đã biết / lưu ý kỹ thuật
- Ctor không nhận `IServiceProvider inject` (khác pattern các controller khác trong service) — `VerificationCodeController.cs:14`.
- Endpoint `[AllowAnonymous]` ngầm (không attribute auth) — hợp lý vì OTP verify chạy trước khi user đăng nhập, nhưng cần chống brute-force (rate limit) ở tầng khác.
