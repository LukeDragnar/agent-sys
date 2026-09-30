# TiktokWebhook — Nhận Event Từ TikTok

> Nhận webhook events từ TikTok (comment, video, order...) → phân phối vào queue → tạo Contact/Lead.

---

## Vai trò

- ← TikTok platform gửi event (comment, video publish, order)
- → **Contact**: tạo lead từ comment/tin nhắn TikTok
- → **OrderLogistic**: tạo đơn từ TikTok Shop

---

## Entities chính

| Entity | Ý nghĩa |
|--------|---------|
| `TTDataWebhook` | Webhook data thô từ TikTok (JSON payload) |
| `TTCommentWebhook` | Comment webhook (PartnerCommentId, VideoId, Text, Sender) |
| `UserConnect` | Kết nối tài khoản TikTok của đơn vị |

Fields tracking: `Retry`, `IsProcessing`, `IsComplete`, `IsForwardRequest`

---

## Luồng

```
POST /api/Webhooks/ReceiveData (từ TikTok platform)
  ↓
Round-robin: queueIndex = requestCounter % 10 + 1
  ↓ (RabbitMQ, 10 queues song song)
TiktokWebhookProcess.API Consumer
  ├→ Ghi log MongoDB
  ├→ TTDataWebhookService.ProcessOrSaveWebhook()
  │     Parse event type: post.publish, comment.update...
  │     Batch insert (100 records/batch)
  └→ → Contact: tạo lead từ comment
```

---

## Checklist fix bug

- [ ] Webhook không xử lý → kiểm tra `TTDataWebhook.IsProcessing`, `Retry`, xem consumer log
- [ ] Comment không tạo lead → kiểm tra `TTCommentWebhook`, xem ContactReceive service
