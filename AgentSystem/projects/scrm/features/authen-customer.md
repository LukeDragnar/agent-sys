# AuthenCustomer — Xác Thực Khách Hàng (App/Web)

> Xác thực và quản lý tài khoản khách hàng cuối (B2C app/web). Cổng dữ liệu cho KH truy cập combo, thẻ dịch vụ, lịch hẹn, đơn hàng, điểm thưởng.

---

## Vai trò

- ← **Khách hàng** (mobile app / website B2C): đăng ký, đăng nhập
- → **WarehouseProduct**: lấy combo, thẻ dịch vụ, sản phẩm
- → **Booking**: đặt/hủy lịch hẹn
- → **Order**: tạo đơn, xem lịch sử
- → **Customer**: lấy thông tin điểm thưởng, công nợ
- → **Promotion**: xem khuyến mãi

---

## Entities chính

| Entity | Ý nghĩa |
|--------|---------|
| `User` | Tài khoản KH: `IdKhachHang`, `MaKhachHang`, `TenKhachHang`, `UserName`, `PasswordHash`, `SoDienThoai`, `Email`, `DiaChi`, `GioiTinh`, `NgaySinh`, `AccessFailed`, `IsLockedOut` |
| `RefreshToken` | `UserId`, `Token`, `NgayHetHan`, `IpAddress`, `DeviceId` |
| `DonVi` | Thông tin đơn vị cho app |
| `CauHinh` | Cấu hình app: lưu JSON |
| `MobilePageConfig` | Cấu hình trang mobile |
| `UserDeviceLog` | Log thiết bị đăng nhập |

---

## API Endpoints

| Endpoint | Mô tả |
|----------|-------|
| `POST /api/authen/Register` [AllowAnonymous] | Đăng ký tài khoản |
| `POST /api/authen/Login_Encrypt` | Đăng nhập (AES-ECB-128) |
| `POST /api/authen/TokenRefresh_Encrypt` | Refresh token |
| `POST /api/authen/Logout` [SBAuthorizeCustomer] | Đăng xuất |
| `POST /api/authen/DoiMatKhau` | Đổi mật khẩu |
| `POST /api/authen/CapNhatThongTinCaNhan` | Cập nhật hồ sơ |
| `POST /api/authen/Login_Zalo` | Đăng nhập Zalo |
| `GET /api/authen/GetCurrentUser` [SBAuthorizeCustomer] | Lấy thông tin user hiện tại |
| `POST /api/authen/CreateOrUpdatePrivate` | Tạo/cập nhật tài khoản (internal) |
| `POST /api/authen/CreateMultiPrivateAccount` | Tạo nhiều tài khoản hàng loạt |
| User.GetCombo/The/LichHen/DonHang/... | Lấy dữ liệu KH (delegate sang service khác) |

---

## Luồng

```
[Đăng nhập mobile]
KH POST Login_Encrypt (username+password mã hóa AES-ECB-128)
  ↓
Giải mã → validate credential
  ├→ AccessFailed++ nếu sai, IsLockedOut nếu quá giới hạn
  └→ Sinh JWT token + RefreshToken
       └→ Lưu RefreshToken (DeviceId, IpAddress, NgayHetHan)

[Refresh token]
KH gọi TokenRefresh_Encrypt
  └→ Validate RefreshToken → sinh token mới

[Lấy dữ liệu KH]
KH gọi API (SBAuthorizeCustomer token)
  └→ UserService delegate sang service tương ứng:
       GetListComboDaMua() → gọi WarehouseProduct API
       GetLichSuLichHen()  → gọi Booking API
       GetDonHangByKhachHang() → gọi Order API
       GetLichSuDiemThuong() → gọi Customer API
```

---

## Đặc điểm quan trọng

- **Mã hóa**: credentials AES-ECB-128 (giống Authen nhân viên)
- **Device detection**: header `SB-Device` → khác format response mobile/desktop
- **Captcha**: trigger sau 100 lần lỗi (desktop only)
- **Zalo login**: tích hợp OAuth Zalo
- AuthenCustomer là **gateway** — không lưu dữ liệu nghiệp vụ, chỉ proxy sang service chuyên biệt

---

## Checklist fix bug

- [ ] KH không đăng nhập được → kiểm tra `User.IsLockedOut`, `AccessFailed`
- [ ] Token hết hạn → kiểm tra `RefreshToken.NgayHetHan`
- [ ] KH không thấy combo/lịch hẹn → kiểm tra gọi đến WarehouseProduct/Booking có lỗi không
- [ ] Đăng ký Zalo lỗi → kiểm tra config Zalo OAuth trong `CauHinh`
