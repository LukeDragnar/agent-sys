# DeliveryStatus — Architecture

## Vai trò trong hệ thống

```
delivery_receive
       │ RabbitMQ: DeliveryStatus_ReceiveStatus (MessageOrderStatus)
       ▼
DeliveryStatusReceiveStatusConsumer
       │ route theo ShippingUnitCode → partner-specific service
       ▼
DataReceive (DB) — lưu raw
       │
       │ JobDataReceive (mỗi 15s)
       ▼
ProcessSendDataReceiveToCrm()
       │ HTTP POST đến CrmCallBackUrl
       ▼
[CRM (SCRM)]
       │ sau khi xử lý, publish response
       ▼
DeliveryReceive_DeliveryStatus_SendDataPartner_Response → delivery_receive
```

## Cấu trúc Solution

```
delivery_status/
├── DeliveryStatus.API/
│   ├── Controllers/
│   │   ├── Base/BaseController.cs
│   │   ├── MySystem/OrderController.cs
│   │   ├── MySystem/MyStatusOrderController.cs
│   │   ├── MySystem/PartnerStatusOrderController.cs
│   │   ├── MySystem/ConfigDeliveryController.cs   (disabled)
│   │   ├── MySystem/ConfigSystemController.cs     (disabled)
│   │   └── LiveController.cs
│   ├── Consumer/
│   │   └── DeliveryStatusReceiveStatusConsumer.cs
│   ├── Service/Generated/Schedule/
│   │   ├── JobDataReceive.cs
│   │   └── JobCleanData.cs
│   └── Program.cs
├── DeliveryStatus.DataContext/
│   ├── Generated/DbContexts/DeliveryStatusDataContext.cs
│   ├── Entity/
│   │   ├── EntityBase.cs
│   │   ├── DataReceive.cs
│   │   ├── OrderStatus.cs
│   │   ├── OrderStatusExt.cs
│   │   ├── MyStatusOrder.cs
│   │   ├── PartnerStatusOrder.cs
│   │   ├── ConfigSystem.cs
│   │   └── LogData.cs
│   └── Model/
│       ├── Common/ (MethodResult, ResponseBase, UserLogin, EnumCommon)
│       ├── Order/ (ReqCreateOrder, ReqPostStatusOrderToCrm)
│       └── RabbitMessage/ (MessageOrderStatus, QueueKeys)
└── DeliveryStatus.Service/
    └── Generated/
        ├── Schedule/ (JobDataReceive, JobCleanData)
        └── Services/ (implementations)
```

## RabbitMQ

| Queue | Direction | Message | Mô tả |
|-------|-----------|---------|-------|
| DeliveryStatus_ReceiveStatus | Consume ← delivery_receive | MessageOrderStatus | Nhận trạng thái từ đối tác |
| DeliveryReceive_DeliveryStatus_SendDataPartner_Response | Publish → delivery_receive | Response với TransactionId | Phản hồi sau xử lý |

## Stored Procedures

- `PRC_DeliveryStatus_CreateOrder_OSGetId` — tạo OrderStatus, lấy Id
- `PRC_DeliveryStatus_DataReceive_Delete_WithBackup` — xóa + backup DataReceive cũ
- `PRC_DeliveryStatus_BakDataReceive_Delete` — xóa backup cũ
- `PRC_DeliveryStatus_LogData_Delete` — xóa log cũ

## appsettings.json Keys

```json
{
  "ServiceName": "DeliveryStatus.API",
  "ServiceKey": "...",
  "ServiceToken": "...",
  "ConnectionStrings": {
    "SwitchBoardConnectStr": "server=...;database=DeliveryStatus;..."
  },
  "AppSettings": {
    "ServerId": 1,
    "Secret": "...",
    "RefreshTokenTTL": 2,
    "ExpireTime": 10080
  },
  "Redis": {
    "UrlConnect": "...:6379",
    "Pass": "...",
    "InstanceName": "DeliveryStatus_Development",
    "Database": 10
  },
  "BusDelivery": {
    "RabbitMq": {
      "Host": ["...:5672"],
      "User": "...",
      "Password": "..."
    }
  },
  "ServiceSettings": {
    "LogServiceUrl": "http://...:50007"
  }
}
```

## Luồng xử lý chi tiết

```
1. Nhận message (Consumer)
   DeliveryStatusReceiveStatusConsumer.Consume()
   → IDataReceiveService.SaveDataReceiveFromPartner(message)
   → Route theo ShippingUnitCode → IGhnService / IVtpService / ...
   → Lưu DataReceive với Status = "received"

2. Gửi về CRM (Job mỗi 15s)
   JobDataReceive.DoWork()
   → IDataReceiveService.ProcessSendDataReceiveToCrm()
   → Lấy DataReceive Status="received", InProcess=false
   → Map sang ReqPostStatusOrderToCrm
   → IClientHelper.ApiPostStatusOrderToCrm(orderDb, dataSend)
   → HTTP POST đến OrderStatus.CrmCallBackUrl
   → Cập nhật Status = "success" hoặc "sendfail"

3. Xử lý lỗi (Job mỗi 60s)
   JobDataReceive.ProcessHandleDataReceive()
   → IDataReceiveErrorService.HandleDataReceive()
   → Retry các record sendfail

4. Cleanup (Job hàng giờ / hàng ngày)
   JobCleanData → stored procedures dọn dẹp data cũ
```
