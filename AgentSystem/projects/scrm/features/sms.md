# SMS — Gửi SMS và Zalo

> Gửi SMS và Zalo OA đến khách hàng. Hỗ trợ nhiều SMS provider, nhiều Zalo OA, template, pattern, OTP, retry thất bại, và callback từ provider.

---

## Vai trò

- ← **Các service** (Order, Customer, Deposit, Campaign...): publish message vào MassTransit
- → **SMS providers**: gửi SMS brandname qua API
- → **Zalo OA API**: gửi Zalo ZNS
- → **MongoDB**: log lỗi
- → **Redis**: quản lý balance, lock

---

## Entities chính

| Entity | Ý nghĩa |
|--------|---------|
| `AccountSms` | Tài khoản SMS: `BrandName`, `Username`, `Password`, `ApiKey`, `SecretKey`, `UrlServer`, `IsUseZalo`, `OAIDZalo`, `IsSanbox` |
| `AccountSms` (chi phí) | `TotalMoneySendBySystem`, `TotalMoneySend` |
| `NotificationConfigV2` | Cấu hình trigger: `ActionCode`, `MessageType` (SMS/ZALO), `SendBeforeMinute`, `SendLaterMinute`, `LuaChonMauKhiNhieuMau` |
| `PatternSms` | Pattern SMS: nội dung mẫu |
| `SmsSend` | Lịch sử gửi SMS thành công |
| `SmsSendFail` | SMS thất bại: `ResendNo` (đếm retry) |
| `TemplateSMS` | Template đăng ký với provider: `TemplateRecordId`, `Status`, `Message` |
| `VerificationCode` | OTP code |
| `SMSBalance` | Số dư SMS theo đơn vị |

---

## API Endpoints

| Endpoint | Mô tả |
|----------|-------|
| `POST /api/SmsSend/TimTheoDieuKien` | Lịch sử gửi SMS |
| `POST /api/SmsSend/SaveSendSms` | Gửi SMS (V1 hoặc V2 tùy cấu hình) |
| `POST /api/SmsSend/ReceiveData_Query` | Callback từ SMS provider |
| `POST /api/SmsSend/ReceiveData_Query_Zalo` | Callback từ Zalo |
| CRUD `/api/AccountSms/...` | Quản lý tài khoản SMS |
| CRUD `/api/NotificationConfigV2/...` | Cấu hình trigger gửi |
| CRUD `/api/PatternSmsV2/...` | Quản lý pattern |

---

## Consumers (MassTransit)

| Consumer | Xử lý |
|----------|-------|
| `SendSMSConsumer` | Gửi SMS đơn lẻ |
| `SendZaloConsumer` | Gửi Zalo đơn lẻ |
| `SendMessageV2Consumer` | Gửi V2 (cấu hình nâng cao) |
| `SendZaloSystemConsumer` | Gửi Zalo từ hệ thống |
| `SendOTPConsumer` | Gửi OTP/verification code |
| `ProcessPaymentSMSConsumer` | SMS xác nhận thanh toán |
| `SendSmsFailConsumer` | Retry SMS thất bại |
| `SendSmsByIdPatternConsumer` | Gửi theo pattern ID |
| `SendSmsToPartnerConsumer` | Gửi SMS cho đối tác |

---

## Luồng

```
[Gửi SMS thông thường]
Service publish SendSmsConsumerDto
  ↓
SendSMSConsumer → ISmsSendService.SaveSendSms()
  ├→ Chọn AccountSms phù hợp (BrandName, IsUseDefault)
  ├→ Gọi SMS provider API
  ├→ Lưu SmsSend (success) hoặc SmsSendFail (fail)
  └→ Update TotalMoneySend

[Retry SMS thất bại]
Job_JobSendFail (scheduler)
  └→ Quét SmsSendFail → retry, tăng ResendNo

[Cấu hình trigger tự động]
Sự kiện nghiệp vụ xảy ra (đặt lịch, thanh toán...)
  ↓
NotificationConfigV2 → match ActionCode
  ├→ SendBeforeMinute / SendLaterMinute: gửi trước/sau N phút
  └→ LuaChonMauKhiNhieuMau: 1=1 mẫu, 2=tất cả mẫu

[OTP]
Service publish SendOTPConsumer
  └→ Tạo VerificationCode → gửi SMS OTP
```

---

## Đặc điểm quan trọng

- **V1 vs V2**: `SuDungCauHinhNangCao` quyết định dùng cấu hình nào
- **Multi-provider**: nhiều SMS provider, chọn theo BrandName/IsUseDefault
- **Zalo ZNS**: gửi qua OAIDZalo (template phải được Zalo duyệt)
- **Sandbox**: `IsSanbox=true` để test không gửi thật

---

## Checklist fix bug

- [ ] SMS không gửi → kiểm tra `AccountSms.IsUse`, `IsUseDefault`, số dư `SMSBalance`
- [ ] Zalo không gửi → kiểm tra `AccountSms.IsUseZalo`, `OAIDZalo`, template đã duyệt chưa
- [ ] SMS thất bại → xem `SmsSendFail.ResendNo`, kiểm tra `Job_JobSendFail`
- [ ] OTP không đến → kiểm tra `VerificationCode`, `SendOTPConsumer`
- [ ] Trigger tự động không gửi → kiểm tra `NotificationConfigV2.IsUse`, `ActionCode`
