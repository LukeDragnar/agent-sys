# Switchboard — Tổng Đài Điện Thoại

> Quản lý tổng đài IP, hotline, lịch sử cuộc gọi. Tích hợp với hệ thống tổng đài bên ngoài (MyHub, IP telephony). Hỗ trợ ghi âm cuộc gọi, quản lý số dư tổng đài.

---

## Vai trò

- ← **CRM users**: gọi/nhận điện qua hotline
- → **External switchboard** (MyHub, IP telephony): quản lý cuộc gọi
- → **Core**: lấy thông tin đơn vị, chi nhánh
- → **Redis**: cache cấu hình

---

## Entities chính

| Entity | Ý nghĩa |
|--------|---------|
| `CallHistory` (MyHubCallHistory) | Lịch sử cuộc gọi: `CallId`, `Phone`, `HotlineId`, `HotlineName`, `StartTime`, `EndTime`, `Duration`, `RecordFile`, `Type`, `IdContact`, `IdKhachHang`, `IdDonHang` |
| `Hotline` | Số hotline: kích hoạt/vô hiệu hóa, lịch sử |
| `UserSwitchboard` | Mapping user → tổng đài |
| `UserSwitchboard_Number` | Số máy nhánh của user |
| `TongDaiDonVi` | Kết nối tổng đài của đơn vị: balance, cấu hình |
| `Group` | Nhóm tổng đài |

---

## API Endpoints

| Endpoint | Mô tả |
|----------|-------|
| `TongDaiDonVi.*` | Quản lý kết nối tổng đài: tạo mới, cập nhật, kết nối, đăng ký webhook |
| `TongDaiDonVi.GetBalance` | Xem số dư tổng đài |
| `TongDaiDonVi.NapTien` | Nạp tiền cho đơn vị |
| `TongDaiDonVi.DuyetSoDu` | Duyệt/từ chối nạp tiền |
| `Hotline.GetHotline` | Lấy danh sách hotline của user |
| `Hotline.ExtendTimeHotline` | Gia hạn hotline |
| `Hotline.RetrySendFail` | Retry giao dịch thất bại |
| `CallHistory.*` | Xem lịch sử cuộc gọi |

---

## Luồng

```
[Cuộc gọi đến/đi]
User nhận/gọi qua hotline
  ↓
External switchboard (MyHub) xử lý
  ↓ webhook
MyHubController nhận event
  └→ CallHistoryService.Lưu lịch sử
       Fields: CallId, Phone, Duration, RecordFile, IdContact, IdDonHang

[Quản lý số dư]
Admin nạp tiền → TongDaiDonViService.NapTienDonVi()
  ↓
SuperAdmin duyệt → TongDaiDonViService.TongDaiDuyetSoDuDonVi()

[Gán hotline cho user]
UserSwitchboardService.GanHotline() → UserSwitchboard_Number
```

---

## Đặc điểm quan trọng

- `RecordFile`: link file ghi âm cuộc gọi
- `IsChange`: cờ tracking thay đổi hotline
- Hotline có thời hạn, cần gia hạn định kỳ
- Hỗ trợ IP telephony (TongDaiIp) bên cạnh tổng đài truyền thống

---

## Checklist fix bug

- [ ] Cuộc gọi không ghi lịch sử → kiểm tra webhook MyHub, `CallHistoryService`
- [ ] Hotline hết hạn → gọi `ExtendTimeHotline`
- [ ] Số dư tổng đài hết → kiểm tra `TongDaiDonVi.Balance`, nạp tiền
- [ ] User không có số nhánh → kiểm tra `UserSwitchboard_Number`
