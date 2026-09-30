# Authen — Xác Thực Nhân Viên Nội Bộ

> JWT authentication cho staff. AuthenCustomer và AuthenPartner là các service riêng cho khách hàng/đối tác.

---

## Vai trò trong hệ thống

- Cấp JWT token cho toàn bộ service nội bộ
- ← FE staff gọi login → nhận JWT + RefreshToken
- → Mọi API service verify JWT trước khi xử lý

---

## Entities chính

### RefreshToken
| Field | Ý nghĩa |
|-------|---------|
| `Token` | Refresh token string |
| `NgayHetHan` | Thời hạn |
| `DeviceId/DeviceKey` | Thiết bị đăng nhập |
| `IpAddress` | IP client |

### UserSession
| Field | Ý nghĩa |
|-------|---------|
| `IsLockedOut` | Tài khoản bị khóa |
| `LockoutEndTime` | Kết thúc khóa |
| `AccessFailed` | Số lần sai mật khẩu |
| `IsUseCaptchaLogin` | Bắt nhập captcha |
| `IsUseOtpLogin` | Xác thực OTP |
| `IsUseGoogleAuthenticator` | Google Authenticator |

### UserDeviceLog
Ghi lịch sử đăng nhập: `UserAgent`, `IpAddress`, `DevicesName`, `IdDonVi`

---

## API Endpoints

| Endpoint | Mô tả |
|----------|-------|
| `POST /api/Authen/login-encrypt` | Đăng nhập (AES-ECB-128) |
| `POST /api/Authen/getuserusecaptcha` | Kiểm tra có cần captcha không |

---

## Luồng xác thực

```
FE: POST /login-encrypt (username + password mã hóa AES-ECB-128)
  ↓
Sai N lần → IsUseCaptchaLogin = true
Quá giới hạn → IsLockedOut = true
Đúng → JWT (short-lived) + RefreshToken (long-lived, lưu DB)
  ↓
Ghi UserDeviceLog
  ↓
Nếu IsLoginOneDevice → invalidate RefreshToken thiết bị cũ
```

---

## Checklist fix bug

- [ ] Không đăng nhập được → `UserSession.IsLockedOut`, `AccessFailed`
- [ ] Token expired quá nhanh → kiểm tra cấu hình TTL JWT
- [ ] Bị kick thiết bị → `IsLoginOneDevice = true`, kiểm tra `DeviceId`
