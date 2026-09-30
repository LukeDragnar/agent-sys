# LogContact — Ghi Log Tương Tác Contact

> Ghi nhận và lưu trữ toàn bộ log data contact nhận từ các nguồn (Ladi, webhook...) vào MongoDB. Hỗ trợ tra cứu và resend khi lỗi.

---

## Vai trò

- ← **ESDocument / các service khác**: gửi message `LogDataReceive` qua MassTransit
- → **MongoDB**: lưu log vào collection `LogDataContactReceviceCollection`

---

## Entities chính

| Entity | Ý nghĩa |
|--------|---------|
| `DonViElasticSearchLog` | Cấu hình đơn vị có dùng ES (ESNameContact, NgayIndexContact, TongSoIndexContact) |
| `LogDataContactMongo` | Document MongoDB: data thô từ contact webhook |

---

## API Endpoints

| Endpoint | Mô tả |
|----------|-------|
| `POST /api/LogDataContact/TimTheoDieuKien` | Tìm kiếm log contact theo điều kiện + phân trang |
| `POST /api/LogDataContact/ResendDataContact` | Gửi lại dữ liệu contact bị lỗi |

---

## Luồng

```
Service khác publish LogDataReceive message
  ↓ (MassTransit/RabbitMQ)
GetLogContactConsumer
  └→ Lưu vào MongoDB (LogDataContactReceviceCollection)

[Resend khi lỗi]
Admin gọi ResendDataContact
  └→ LogDataContactService.ResendDataContact()
       → Gọi lại API helper để tái xử lý
```

---

## Checklist fix bug

- [ ] Log không ghi → kiểm tra consumer `GetLogContactConsumer`, kết nối MongoDB
- [ ] Không tìm thấy log → kiểm tra filter ngày (TuNgay/DenNgay)
- [ ] Cần resend → gọi endpoint `ResendDataContact`
