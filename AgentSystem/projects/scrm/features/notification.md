# Notification — Thông Báo Push

> Gửi push notification đến thiết bị di động qua Firebase Cloud Messaging (FCM). Quản lý device token, topic subscription, queue thông báo.

---

## Vai trò

- ← **Các service khác**: publish `NotificationInfo` qua MassTransit khi cần gửi thông báo
- → **Firebase Cloud Messaging**: push notification đến iOS/Android
- → **MongoDB**: lưu queue thông báo, device tokens, user topics

---

## Entities chính

| Entity | Ý nghĩa |
|--------|---------|
| `ThongBao` | Thông báo: `TieuDe`, `NoiDung`, `Images`, `Url`, `NgayGui`, `GuiNgay`, `ThongBaoTatCa`, `DaGui`, `IsHeThong` |
| `ThongBaoNguoiNhan` | Người nhận: `IdDonVis` (list), `ChiNhanhds` (list), `Roles` |
| `NotiTemplate` | Mẫu thông báo: `Ma`, `Ten`, `MauThongBao`, `ListRolesSend`, `GuiSau` (phút), `IsUseSound`, `SoundTemplate` |
| `Notification` (MongoDB) | Queue thông báo |
| `UserToken` (MongoDB) | Firebase device token theo user |
| `UserTopic` (MongoDB) | Topic đăng ký của user |
| `UserNotification` (MongoDB) | Thông báo đã nhận của user |

---

## API Endpoints

| Endpoint | Mô tả |
|----------|-------|
| `POST /api/ThongBao/TimTheoDieuKien` | Tìm kiếm thông báo |
| `POST /api/ThongBao/ThemMoi` | Tạo thông báo mới |
| `POST /api/ThongBao/CapNhatThongTin` | Cập nhật |
| `POST /api/ThongBao/send-test` [AllowAnonymous] | Test gửi thông báo |
| Register/Remove Token | Đăng ký / xóa device token FCM |
| Subscribe/Unsubscribe Topic | Đăng ký / hủy topic |

---

## Luồng

```
[Gửi thông báo từ service khác]
Service publish NotificationInfo (MassTransit)
  ↓
SendNotificationConsumer
  ↓
INotificationService.SendMessage()
  ├→ Lấy UserToken từ MongoDB
  ├→ Gọi Firebase Cloud Messaging API
  └→ Nếu token lỗi (unregistered/sender_id_mismatch) → xóa token

[Job gửi thông báo có hẹn giờ]
JobSendNotification (scheduler)
  └→ Quét queue MongoDB → gửi các thông báo đến giờ

[Đăng ký thiết bị]
App đăng nhập → gọi RegisterToken(userId, deviceToken)
  └→ Lưu UserToken MongoDB

[Gửi theo topic]
DangKyTopicNotificationConsumer → subscribe/unsubscribe topic FCM
XuLyGuiThongBaoConsumer → gửi theo template
```

---

## Đặc điểm quan trọng

- **Sound**: iOS dùng `.caf`, Android dùng file âm thanh riêng (`IsUseSound`, `SoundTemplate`)
- **Topic**: nhóm gửi hàng loạt (broadcast theo đơn vị/chi nhánh)
- **GuiSau**: template có thể cấu hình gửi trễ N phút sau sự kiện
- **JobRemoveToken**: dọn dẹp token hết hạn định kỳ

---

## Checklist fix bug

- [ ] Push không đến máy → kiểm tra `UserToken` trong MongoDB, token còn hợp lệ không
- [ ] Token lỗi liên tục → `SendNotificationConsumer` tự xóa token lỗi, user cần đăng nhập lại để re-register
- [ ] Thông báo hẹn giờ không gửi → kiểm tra `JobSendNotification`, xem queue MongoDB
- [ ] Template không áp → kiểm tra `NotiTemplate.Ma` và `ListRolesSend`
