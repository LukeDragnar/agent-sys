# LogDelivery — Ghi Log Giao Hàng

> Ghi nhận log webhook từ các đơn vị vận chuyển (GHN, GHTK...) vào MongoDB. Hỗ trợ tra cứu theo đơn vị vận chuyển và resend khi lỗi.

---

## Vai trò

- ← **DeliveryReceive**: gửi message `LogDeliveryMessage` qua MassTransit
- → **MongoDB**: lưu log vào collection `LogDelivery`

---

## Entities chính

| Entity | Ý nghĩa |
|--------|---------|
| `LogDeliveryMongo` | MongoDB document: `ShippingUnitCode`, `Data`, `NgayTao`, `IsXuLy` |
| `DonViElasticSearchLog` | Cấu hình ES cho đơn vị (ESNameDonHangLogistic) |

---

## API Endpoints

| Endpoint | Mô tả |
|----------|-------|
| `POST /api/LogDelivery/TimTheoDieuKien` | Tìm log theo ngày + ShippingUnitCode |
| `POST /api/LogDelivery/ResendDataErrorToDelivery` | Gửi lại data lỗi cho delivery |
| `GET /api/LogDelivery/TestJob` | Test job xóa dữ liệu cũ |

---

## Luồng

```
DeliveryReceive nhận webhook từ đơn vị vận chuyển
  ↓ publish LogDeliveryMessage
LogDeliveryConsumer
  └→ Lưu vào MongoDB (collection: LogDelivery)
       Fields: ShippingUnitCode, Data, NgayTao, IsXuLy = false

[Tra cứu]
TimTheoDieuKien → filter theo ngày + ShippingUnitCode
  └→ Count + query song song (parallel) để tối ưu performance
```

---

## Checklist fix bug

- [ ] Log không ghi → kiểm tra `LogDeliveryConsumer`, kết nối MongoDB
- [ ] Tìm log theo đơn vị vận chuyển → filter `ShippingUnitCode`
- [ ] Data lỗi cần tái xử lý → gọi `ResendDataErrorToDelivery`
