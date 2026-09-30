# Recipe — Gửi SMS / Zalo / Notification

> **Trạng thái: ✅ Verify từ source SCRM** (quét 2026-06-19).

## Khi nào dùng
- **SMS:** tin text qua nhà mạng (cần `AccountSms` cấu hình).
- **Zalo ZNS:** OA notification (cần `ZaloTempID`, `ZaloParams`, `OAIDZalo`).
- **Push:** thông báo in-app/Firebase real-time.

Tất cả đi qua **MassTransit publish → consumer → service** (async, không block request). Xem thêm [masstransit.md](masstransit.md).

## Gửi SMS
1. Publish `SendSmsConsumerDto` → `SendSMSConsumer.Consume`
2. `ISmsSendService.SaveSendMessageV2()` xử lý
3. Gửi partner (VNPay/Bravo/TingTong) qua `SendSmsToPartnerDto`

Cấu hình: `AccountSms` (Username/Password/IsUse), `PatternSmsV2.Contents` (template + placeholder `{TenKhachHang}`...), `NotificationConfig` (bật/tắt theo nghiệp vụ).

## Gửi Zalo ZNS
1. Publish `SendZaloConsumerDto` → `SendZaloConsumer.Consume`
2. Routing theo `AccountSmsV2.MessageType` (`SMS` vs `ZALO`)
3. `PatternSmsV2`: `ZaloTempID`, `ZaloParams` (JSON), `OAIDZalo`, `ZaloTemplate`
```csharp
if (account.MessageType == EnumTypeAccountSms.ZALO) { /* gửi ZNS qua TingTong */ }
```

## Push notification (Firebase)
1. Tạo `NotificationInfo` → queue → `JobSendNotification` (Quartz) chạy định kỳ
2. Lấy token theo `Topic` (All / user / đơn vị / role) từ MongoDB
3. Publish → `SendNotificationConsumer` → `FirebaseMessaging.DefaultInstance.SendAsync()`
4. `RegisterToken()` lưu token + topic + hạn; job cleanup token hết hạn

## Bẫy thường gặp
- **Template placeholder** `{...}` thay runtime; thiếu data → fallback nội dung mặc định pattern.
- **Fallback default account:** không có cấu hình riêng → dùng `AccountSmsV2` mặc định đơn vị (`IdDonVi == Guid.Empty`); Redis lỗi → `NeedFallback=true` query DB. (xem memory `fallback_default_pattern_zalo_sms`)
- **Async qua queue** tránh block; Firebase multicast tối đa ~100 token/lần.
- Gửi thất bại → lưu `SmsSendFail` → retry (`SendSmsFailConsumer`).
- `NotificationConfig.IsUse == false` → không gửi.

## File source mẫu
- SMS: `SMS/SMS.Service/Generated/SmsSendV2Service.cs:685` · `SMS.API/Consumer/SendSMSConsumer.cs`
- Zalo: `SmsSendV2Service.cs` (MessageType check) · `Consumer/SendZaloConsumer.cs`
- Push: `Notification/Notification.API/Job/JobSendNotification.cs` · `Consumer/SendNotificationConsumer.cs` · `Services/NotificationService.cs`
- Cấu hình: `SMS.DataContext/Entity/PatternSmsV2.cs`, `AccountSms.cs` · fallback `Libs/SB.Core/SMS/SMSService.cs`
