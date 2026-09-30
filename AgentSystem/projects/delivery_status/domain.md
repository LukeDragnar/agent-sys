# DeliveryStatus — Domain & Entities

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

## DB: DeliveryStatusDataContext

### DataReceive
| Field | Type | Mô tả |
|-------|------|-------|
| ShippingUnitCode | VARCHAR(20) | Mã đơn vị vận chuyển |
| OrderCode | NVARCHAR(50)? | Mã đơn nội bộ |
| OrderCodeExternal | NVARCHAR(50)? | Mã đơn phía đối tác |
| OrderStatusCode | NVARCHAR(50)? | Mã trạng thái |
| OrderStatusName | NVARCHAR(50)? | Tên trạng thái |
| OrderStatusDate | DateTime? | Thời điểm cập nhật |
| Amount | NVARCHAR(20)? | COD |
| Data | NVARCHAR(MAX)? | Raw JSON từ đối tác |
| Status | NVARCHAR(20)? | received / success / sendfail |
| Description | NVARCHAR(4000)? | Ghi chú / lỗi |
| ResendNo | int | Số lần gửi lại |
| ReturnOrderCode | NVARCHAR(60)? | Mã đơn hoàn |
| ReturnFee | NVARCHAR(20)? | Phí hoàn |
| ReturnCod | NVARCHAR(20)? | COD hoàn |
| ReturnOrderStatusCode | NVARCHAR(50)? | Trạng thái đơn hoàn |
| InProcess | bool | Cờ đang xử lý (tránh trùng) |
| CommissionFee | VARCHAR(50)? | Phí hoa hồng |
| CreatedAtUnixMs | BIGINT? | Unix timestamp (ms) |
| OrderStatusId | Guid? | FK sang OrderStatus |

### OrderStatus
| Field | Type | Mô tả |
|-------|------|-------|
| ServerCode | VARCHAR(50)? | |
| ShippingUnitCode | VARCHAR(20)? | Mã đơn vị vận chuyển |
| CrmUnitId/Name | VARCHAR(64)?/NVARCHAR(256)? | Đơn vị CRM |
| CrmUnitChildId/Name | VARCHAR(64)?/NVARCHAR(256)? | Đơn vị con CRM |
| CrmCallBackUrl | VARCHAR(256) | URL callback về CRM |
| OrderCode | VARCHAR(100)? | Mã đơn nội bộ |
| OrderCodeExternal | VARCHAR(100)? | Mã đơn đối tác |
| OrderStatusCode | VARCHAR(200)? | Mã trạng thái |
| OrderStatusName | NVARCHAR(200)? | Tên trạng thái |
| OrderStatusDate | DateTime? | |
| Amount | VARCHAR(50)? | COD |
| ReturnOrderCode | VARCHAR(100)? | |
| ReturnFee/Cod | VARCHAR(50)? | |
| ReturnOrderStatusCode | VARCHAR(200)? | |
| ProcessName | NVARCHAR(200)? | Tên process đã xử lý |
| ProcessStatus | VARCHAR(200)? | success/error/doing/fail/prepare |
| Description | NVARCHAR(1000)? | |
| ResendNo | int | Số lần gửi lại |
| Logs | NVARCHAR(4000)? | Log xử lý |
| ReqCreateOrder | NVARCHAR(max)? | Request tạo đơn ban đầu |
| PropName1-5 | VARCHAR(128)? | Custom properties |
| PropValue1-5 | NVARCHAR(512)? | Custom property values |
| ShippingUnitCodeCustom | VARCHAR(20)? | |
| CreatedAtUnixMs | BIGINT? | |

### OrderStatusExt
| Field | Type | Mô tả |
|-------|------|-------|
| ReqCreateOrder | NVARCHAR(max)? | Extended request data |
| CreatedAtUnixMs | BIGINT? | |

### MyStatusOrder
| Field | Type | Mô tả |
|-------|------|-------|
| StatusId | int? | |
| StatusCode | VARCHAR(200) | Mã trạng thái nội bộ |
| StatusName | NVARCHAR(200) | Tên hiển thị |
| NumberOrder | int | Thứ tự hiển thị |
| Color | VARCHAR(50) | Màu UI |
| Description | NVARCHAR(1000) | |

### PartnerStatusOrder
| Field | Type | Mô tả |
|-------|------|-------|
| ShippingUnitCode | VARCHAR(20) | Mã đối tác |
| PartnerStatusCode | VARCHAR(200)? | Mã trạng thái của đối tác |
| PartnerStatusName | NVARCHAR(200)? | Tên trạng thái đối tác |
| MyStatusId | int? | ID trạng thái nội bộ |
| MyStatusCode | VARCHAR(200)? | Mã trạng thái nội bộ |
| MyStatusName | NVARCHAR(200)? | Tên trạng thái nội bộ |
| Description | NVARCHAR(1000)? | Mô tả mapping |

### ConfigSystem
| Field | Type |
|-------|------|
| Type | VARCHAR(50) |
| ConfigKey | VARCHAR(100) |
| ValueInitial | NVARCHAR(4000) |
| ValueCurrent | NVARCHAR(4000) |
| Description | NVARCHAR(1000) |

## Endpoints

### OrderController
| Endpoint | Tham số | Mô tả |
|----------|---------|-------|
| POST /api/Order/CreateOrder | ReqCreateOrder | Tạo đơn tracking |
| POST /api/Order/RegistReceiveStatusFromPS | Dictionary<string,string> | Đăng ký nhận trạng thái |

### MyStatusOrderController
| Endpoint | Mô tả |
|----------|-------|
| GET /api/MyStatusOrder/Search | Lấy tất cả trạng thái nội bộ (có cache) |
| POST /api/MyStatusOrder/Save | Lưu/cập nhật danh sách trạng thái |
| POST /api/MyStatusOrder/Refresh | Refresh cache |

### PartnerStatusOrderController
| Endpoint | Mô tả |
|----------|-------|
| GET /api/PartnerStatusOrder/search?shippingUnitCode=&partnerStatusCode= | Tìm kiếm mapping |
| POST /api/PartnerStatusOrder/Save | Lưu mapping |
| POST /api/PartnerStatusOrder/Refresh?shippingUnitCode= | Refresh cache |

### LiveController
| Endpoint | Mô tả |
|----------|-------|
| GET /api/Live/Test | Health check |
| GET /api/Live/TestJob?pass= | Test DataReceive processing (pass: "sb!34gg464") |

## Consumers

### DeliveryStatusReceiveStatusConsumer
- Message: `RabbitModel.MessageOrderStatus` { TransactionId, ShippingUnitCode, Data, Message }
- Queue: `DeliveryStatus_ReceiveStatus`
- Gọi: `IDataReceiveService.SaveDataReceiveFromPartner()`
- Route xử lý theo `ShippingUnitCode` đến service của từng đối tác

## Services

| Interface | Methods chính |
|-----------|--------------|
| IOrderService | CreateOrder, RegistReceiveStatusFromPS |
| IDataReceiveService | SaveDataReceiveFromPartner, ProcessSendDataReceiveToCrm |
| IDataReceiveErrorService | SaveDataReceiveFromPartner, HandleDataReceive |
| IMyStatusOrderService | Search, Save, Refresh |
| IPartnerStatusOrderService | Search, Save, Refresh, GetByShippingUnitCode, Get |
| ILogDataService | WriteLog, WriteInfo, WriteError |
| ICleanDataService | ProcessPRC_DeliveryStatus_DataReceive_Delete_WithBackup, ProcessPRC_DeliveryStatus_BakDataReceive_Delete, ProcessPRC_DeliveryStatus_LogData_Delete |
| IClientHelper | ApiPost, ApiGet, ApiPostStatusOrderToCrm |
| IRedisCacheService | Get, Set, GetOrSet, Remove, RemoveByPrefix, SetNotExistsNew |

## Quartz Jobs

### JobDataReceive
| Timer | Interval | Mô tả |
|-------|----------|-------|
| DoWork | 15s (debug: 150s) | ProcessSendDataReceiveToCrm() — gửi data về CRM |
| ProcessHandleDataReceive | 60s (debug: 600s) | HandleDataReceive() — xử lý lại data lỗi |

### JobCleanData
| Timer | Interval | Mô tả |
|-------|----------|-------|
| DataReceive_Delete_WithBackup | 1 giờ | Xóa + backup DataReceive cũ (giữ 5 phút gần nhất) |
| BakDataReceive_Delete | 3:00 AM hàng ngày | Xóa bản backup cũ |
| LogData_Delete | 3:00 AM hàng ngày | Xóa log cũ |

## RabbitMQ Message

```csharp
// MessageOrderStatus — nhận từ delivery_receive
{ TransactionId: Guid?, ShippingUnitCode: string?, Message: string?, Data: string? }

// Response — gửi lại delivery_receive sau khi xử lý
{ TransactionId: Guid?, Data: string? }
```

## Đối tác vận chuyển được hỗ trợ

GHN, GHTK, VTP, JNT, EMS, SuperShip, Best, BoxMe, HolaShip, AhaMove, NJV, SPX, CUST_SSC (custom)

## Models

### ReqCreateOrder
```csharp
UserLogin? UserLogin
string? ShippingUnitCode
string? CrmCallBackUrl
string? OrderCode
string? OrderCodeExternal
string? OrderData
string? ShippingUnitCodeCustom
```

### ReqPostStatusOrderToCrm
```csharp
string? ShippingUnitCode, CrmUnitId/Name, CrmUnitChildId/Name
string? OrderCode, OrderCodeExternal
int? OrderStatusId
string? OrderStatusCode/Name/CodeExternal/NameExternal
DateTime? OrderStatusDate
bool IsSignPart
string? ReturnCod, ReturnFee, ReturnOrderCodeExternal, CodSignPart
string? Cod, TotalFee, ReasonUpdate
```
