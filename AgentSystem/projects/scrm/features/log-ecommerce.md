# LogEcommerce — Ghi Log Sàn Thương Mại Điện Tử

> Ghi nhận log webhook từ các sàn TMĐT (Shopee, Lazada, TikTok Shop...) vào MongoDB. Hỗ trợ tra cứu và resend.

---

## Vai trò

- ← **EcommerceReceive**: gửi message `LogEcommerceMessage` qua MassTransit
- → **MongoDB**: lưu log vào collection `LogDataEcommerce`

---

## Entities chính

| Entity | Ý nghĩa |
|--------|---------|
| `LogEcommerceModel` | MongoDB document: `EcomCode`, `Data`, `Headers`, `Params`, `NgayTao`, `IsXuLy` |

---

## API Endpoints

| Endpoint | Mô tả |
|----------|-------|
| `POST /api/LogEcommerce/TimTheoDieuKien` | Tìm log ecommerce theo điều kiện |
| `POST /api/LogEcommerce/ResendDataErrorToEcom` | Gửi lại data lỗi cho ecommerce |

---

## Luồng

```
EcommerceReceive nhận webhook từ sàn TMĐT
  ↓ publish LogEcommerceMessage
DataEcomReceiveConsumer
  ├→ Parse JSON data
  ├→ Extract EcomCode (Shopee/Lazada/TikTok...)
  └→ Lưu vào MongoDB (collection: LogDataEcommerce)
       Fields: EcomCode, Data, Headers, Params, NgayTao, IsXuLy = false
```

---

## Checklist fix bug

- [ ] Log không ghi → kiểm tra `DataEcomReceiveConsumer`, kết nối MongoDB
- [ ] Lọc theo sàn → dùng filter `EcomCode`
- [ ] Data lỗi → gọi `ResendDataErrorToEcom`
