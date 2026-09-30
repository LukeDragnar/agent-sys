# PS_Delivery — Domain & Entities

## Luồng tổng quát

```
Đối tác vận chuyển
       │  (HTTP callback / webhook)
       ▼
  PsWebhook.API  ──── lưu raw data ────▶ DB: PSGH_Webhook
       │  (publish RabbitMQ message)
       ▼
  RabbitMQ
       │  (consume)
       ▼
  DeliveryReceive.API
       │  (chuẩn hóa → forward)
       ▼
  CRM (SCRM)
```

## Entities (DeliveryReceive.DataContext)

### DataReceive : EntityBase
| Field | Type | Mô tả |
|-------|------|-------|
| ShippingUnitCode | string? | Mã đơn vị vận chuyển (GHN, GHTK, VTP...) |
| OrderCode | string? | Mã đơn hàng nội bộ |
| OrderCodeExternal | string? | Mã đơn hàng phía đối tác |
| OrderStatusCode | string? | Mã trạng thái theo chuẩn của hệ thống |
| OrderStatusName | string? | Tên trạng thái |
| OrderStatusDate | DateTime? | Thời điểm cập nhật trạng thái |
| Amount | string? | Số tiền COD |
| Data | string? | Raw data JSON từ đối tác |
| Status | string? | Trạng thái xử lý (pending/done/error) |
| Description | string? | Ghi chú |
| ResendNo | int | Số lần gửi lại |
| ReturnOrderCode | string? | Mã đơn hoàn |
| ReturnFee | string? | Phí hoàn |
| ReturnCod | string? | COD hoàn |
| ReturnOrderStatusCode | string? | Mã trạng thái đơn hoàn |
| IsProcessing | byte | Cờ đang xử lý (tránh xử lý trùng) |
| CommissionFee | string? | Phí hoa hồng |

### LogData : EntityBase
Lưu log lỗi khi xử lý dữ liệu.

### OrderStatus : EntityBase
Mapping trạng thái từ đối tác vận chuyển sang trạng thái chuẩn của hệ thống.

## RabbitMQ Messages

### PS_DeliveryReceiveDataPartnerMessage
- `ShippingUnitCode` — mã đơn vị vận chuyển
- `Data` — raw JSON data từ đối tác

### ResendReceiveDataPartnerMessage
- Dùng để gửi lại các record lỗi / chưa xử lý

## Consumers (DeliveryReceive.API)

- **PS_DeliveryReceiveDataPartnerConsumer** — nhận data mới từ webhook, gọi `IReceiveDataService.ReceiveData()`
- **ResendReceiveDataPartnerConsumer** — xử lý lại các data lỗi

## Services

- **IReceiveDataService** — chuẩn hóa và forward data trạng thái vận chuyển
- **ILogDataService** — ghi log lỗi (`WriteError`)
