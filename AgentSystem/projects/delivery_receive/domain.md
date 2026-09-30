# DeliveryReceive — Domain & Entities

## EntityBase (base class chung)

| Field | Type | Mô tả |
|-------|------|-------|
| Id | Guid | Primary Key |
| IsDeleted | bool | Soft delete |
| CreatedById/Name | string | Người tạo |
| CreatedDate | DateTime | Mặc định DateTime.Now |
| UpdatedById/Name | string? | Người cập nhật |
| UpdatedDate | DateTime? | |
| DeletedById/Name/Date | string?/DateTime? | |
| CreatedAtUnixMs | long? | Unix timestamp UTC0 (ms) |

## DB: DeliveryReceiveDataContext

### DataReceive
| Field | Type | Mô tả |
|-------|------|-------|
| ShippingUnitCode | VARCHAR(20) | Mã đơn vị vận chuyển |
| Data | NVARCHAR(MAX) | Raw JSON từ đối tác |
| Status | NVARCHAR(20) | RECEIVED / SENDING / SENDED / SENDFAIL / DONE / RESENDING |
| Description | NVARCHAR(4000) | Ghi chú / lỗi |
| ResendNo | int | Số lần gửi lại |
| InProcess | bool | Cờ đang xử lý (tránh trùng) |

### LogData
| Field | Type | Mô tả |
|-------|------|-------|
| Type | VARCHAR(10) | INFO / ERROR |
| ProcessName | VARCHAR(128) | Tên process |
| DataInfo | NVARCHAR(MAX) | Dữ liệu thông tin (JSON) |
| DataError | NVARCHAR(MAX) | Chi tiết lỗi / stack trace |
| ServerCode | VARCHAR(128) | Server identifier |
| ShippingUnitCode | VARCHAR(20) | Mã đối tác |
| CrmUnitId | VARCHAR(128) | CRM unit |
| CrmUnitChildId | VARCHAR(128) | CRM unit con |
| OrderCode | VARCHAR(128) | Mã đơn hàng |
| IdTransaction | Guid? | FK sang DataReceive.Id |

## Endpoints

### ReceiveDataController
| Method | Route | Mô tả |
|--------|-------|-------|
| POST | `{shippingUnitCode:regex(^[[a-zA-Z_]]+$)}` | Nhận data JSON từ đối tác. Response J&T theo format `{status: true/false}` |
| POST | `WithParam/{shippingUnitCode:alpha}` | Nhận data qua query string |
| GET | `ReceiveCode` | Hiển thị query params dạng HTML |

### CrmDataForwardController
| Method | Route | Mô tả |
|--------|-------|-------|
| POST | `SendDataToServiceAsync?shippingUnitCode=` | Resend message cho delivery lỗi |

### LiveController
| Method | Route | Mô tả |
|--------|-------|-------|
| GET | `/api/Live/Test` | Health check, trigger ResendData job |

## Consumers

| Consumer | Queue | Message | Mô tả |
|----------|-------|---------|-------|
| DeliveryReceiveDataPartnerConsumer | `DeliveryReceive_ReceiveData_Queue` | DeliveryReceiveDataPartnerMessage | Nhận data mới từ đối tác, gọi ReceiveData() |
| ResendReceiveDataPartnerConsumer | `ResendReceive_ReceiveData_Queue` | ResendReceiveDataPartnerMessage | Xử lý lại data lỗi |
| DeliveryReceive_DeliveryStatus_SendDataPartner_ResponseConsumer | `DeliveryReceive_DeliveryStatus_SendDataPartner_Response` | DeliveryReceive_DeliveryStatus_SendDataPartner_Response | Nhận phản hồi từ DeliveryStatus, đánh dấu DONE |

> Config mỗi consumer: PrefetchCount=10, Retry=2x3000ms

## RabbitMQ Messages

### DeliveryReceiveDataPartnerMessage
```csharp
string? ShippingUnitCode
object? Data   // raw JSON từ đối tác
```

### ResendReceiveDataPartnerMessage
```csharp
string? ShippingUnitCode
object? Data
```

### DeliveryReceive_DeliveryStatus_SendDataPartner_Response
```csharp
Guid? TransactionId   // = DataReceive.Id
string? Data          // phản hồi từ DeliveryStatus
```

### LogDeliveryMessage
```csharp
string? ShippingUnitCode
object? Data   // gửi lên external logging service
```

### RabbitModel.MessageOrderStatus (publish sang DeliveryStatus)
```csharp
Guid? TransactionId        // = DataReceive.Id
string? ShippingUnitCode
string? Message
string? Data               // JSON order status
```

## Services

### IReceiveDataService
| Method | Mô tả |
|--------|-------|
| ReceiveData(shippingUnitCode, reqData) | Tạo DataReceive, publish sang DeliveryStatus |
| DeliveryReceive_DeliveryStatus_SendDataPartner_Response(message) | Tìm DataReceive theo TransactionId, đánh dấu DONE + soft delete |
| ResendData() | Query PRC_DeliveryReceive_DataReceive_GetForResend, publish lại các record lỗi |
| SendLogAndQueueMessageAsync(shippingUnitCode, reqData) | Publish LogDeliveryMessage + DeliveryReceiveDataPartnerMessage |
| ReSendQueueMessageAsync(shippingUnitCode, reqData) | Publish ResendReceiveDataPartnerMessage |
| DeleteDataReceive() | Xóa DataReceive cũ (giữ 5 phút gần nhất) |
| DeleteLogData() | Xóa LogData cũ (giữ 30 ngày) |

### ILogDataService
| Method | Mô tả |
|--------|-------|
| WriteLog(processName, data, strError, ...) | Ghi log info hoặc error |
| WriteInfo(processName, data, ...) | Ghi log info |
| WriteError(processName, strError, ...) | Ghi log error |

## Quartz Jobs

| Job | Lịch | Môi trường | Mô tả |
|-----|------|------------|-------|
| DataReceiveJob | 15s | Tất cả | ResendData() — gửi lại data lỗi |
| JobDeleteDataReceive | 3:00 AM hàng ngày | Staging/Sprint/Production | PRC_DeliveryReceive_DataReceive_Delete |
| JobDeleteLogData | 2:00 AM hàng ngày | Staging/Sprint/Production | PRC_DeliveryReceive_LogData_Delete |

## Stored Procedures

| SP | Mô tả |
|----|-------|
| PRC_DeliveryReceive_DataReceive_GetForResend | Lấy các record cần gửi lại |
| PRC_DeliveryReceive_DataReceive_Delete | Xóa DataReceive cũ (giữ 5 phút) |
| PRC_DeliveryReceive_LogData_Delete | Xóa LogData cũ (giữ 30 ngày) |

## Luồng xử lý

```
Happy Path:
  Partner POST → ReceiveDataController
  → SendLogAndQueueMessageAsync() → publish DeliveryReceiveDataPartnerMessage
  → Consumer → ReceiveData() → tạo DataReceive (SENDING)
  → publish MessageOrderStatus → DeliveryStatus
  → DeliveryStatus xử lý xong → publish Response
  → ResponseConsumer → DeliveryReceive_DeliveryStatus_SendDataPartner_Response()
  → DataReceive Status=DONE, IsDeleted=true

Retry Path:
  DataReceiveJob (15s) → ResendData()
  → query PRC_DeliveryReceive_DataReceive_GetForResend
  → publish ResendReceiveDataPartnerMessage
  → ResendConsumer → ReceiveData() [như happy path]
```
