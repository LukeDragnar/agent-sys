# FacebookWebhook — Nhận Event Từ Facebook

> Nhận webhook events từ Facebook Page (comment, message, lead ads...) → phân phối vào queue → tạo Contact.

---

## Vai trò

- ← Facebook platform gửi event
- → **Contact/FacebookChat**: tạo lead, conversation
- → **ChiaSo**: phân phối lead cho sale

---

## Entities chính

| Entity | Ý nghĩa |
|--------|---------|
| `FBDataWebhook` | Webhook data thô (FanPageId, Data JSON) |
| `FBDataWebhook1st` | Webhook verification lần đầu |
| `FBFanpageServer` | Ánh xạ fanpage → server xử lý |
| `ServerDonViCauHinh` | Cấu hình server cho từng đơn vị |

Fields tracking: `ServerId`, `ServerCode`, `Retry`, `IsProcessing`, `IsComplete`

---

## Luồng

```
GET /api/v1/Webhooks — Verify subscription
  hub.verify_token match → return hub.challenge

POST /api/v1/Webhooks/ReceiveData
  ↓
  Mode 1 (mặc định): round-robin queueIndex = requestCounter % 10 + 1
  Mode 2 (config): Query FBFanpageServer → ServerDonViCauHinh → RabbitServerNumber
  ↓ (RabbitMQ)
Consumer
  ├→ Ghi log MongoDB
  ├→ FBDataWebhookService.ProcessOrSaveWebhook()
  │     Batch insert (100 records/batch)
  └→ → ContactReceiveFB: tạo Contact từ event
```

---

## Checklist fix bug

- [ ] Webhook không nhận → kiểm tra verify token, xem `FBDataWebhook1st`
- [ ] Lead không tạo từ FB → xem `FBDataWebhook.IsComplete`, kiểm tra consumer
- [ ] Fanpage sai server → kiểm tra `FBFanpageServer` mapping
