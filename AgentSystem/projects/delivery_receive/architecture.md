# DeliveryReceive — Architecture

## Vai trò trong hệ thống

```
[Đối tác vận chuyển]
  (GHN, GHTK, VTP, J&T, Ahamove, ...)
       │  HTTP POST callback
       ▼
ReceiveDataController
       │ SendLogAndQueueMessageAsync()
       ├──▶ LogDeliveryMessage (queue) — logging
       └──▶ DeliveryReceiveDataPartnerMessage (queue)
                   │
                   ▼
       DeliveryReceiveDataPartnerConsumer
                   │ ReceiveData()
                   ▼
       DataReceive (DB, Status=SENDING)
                   │ publish MessageOrderStatus
                   ▼
       DeliveryStatus service
                   │ xử lý xong, publish Response (TransactionId)
                   ▼
       DeliveryReceive_DeliveryStatus_SendDataPartner_ResponseConsumer
                   │ tìm DataReceive theo TransactionId
                   ▼
       DataReceive (Status=DONE, IsDeleted=true)

Retry path (DataReceiveJob mỗi 15s):
       PRC_DeliveryReceive_DataReceive_GetForResend
       → publish ResendReceiveDataPartnerMessage
       → ResendReceiveDataPartnerConsumer
       → ReceiveData() [lại từ đầu]
```

## Cấu trúc Solution

```
delivery_receive/
├── DeliveryReceive.API/
│   ├── Controllers/
│   │   ├── Base/BaseController.cs
│   │   ├── Partner/ReceiveDataController.cs
│   │   ├── CRM/CrmDataForwardController.cs
│   │   └── LiveController.cs
│   ├── Consumer/
│   │   ├── DeliveryReceiveDataPartnerConsumer.cs
│   │   ├── ResendReceiveDataPartnerConsumer.cs
│   │   └── DeliveryReceive_DeliveryStatus_SendDataPartner_ResponseConsumer.cs
│   ├── Extensions/
│   │   ├── MassTransitExtensions.cs
│   │   ├── JobExtention.cs
│   │   └── ServicesExtention.cs
│   └── Program.cs
├── DeliveryReceive.DataContext/
│   ├── Generated/DbContexts/DeliveryReceiveDataContext.cs
│   ├── Entity/
│   │   ├── Common/EntityBase.cs
│   │   ├── DataReceive.cs
│   │   └── LogData.cs
│   └── Model/
│       ├── Common/ (MethodResult, UserLogin, QueueKeys, EnumCommon)
│       └── RabbitMessage/
│           ├── DeliveryReceiveDataPartnerMessage.cs
│           ├── ResendReceiveDataPartnerMessage.cs
│           ├── DeliveryReceive_SendDataStatus_Response.cs
│           ├── LogDeliveryMessage.cs
│           └── RabbitModel.cs (MessageOrderStatus)
└── DeliveryReceive.Service/
    └── Generated/
        ├── Schedule/ (DataReceiveJob, JobDeleteDataReceive, JobDeleteLogData)
        └── Services/ (ReceiveDataService, LogDataService)
```

## RabbitMQ Queues

| Queue | Direction | Message | Mô tả |
|-------|-----------|---------|-------|
| DeliveryReceive_ReceiveData_Queue | Consume | DeliveryReceiveDataPartnerMessage | Data mới từ đối tác |
| ResendReceive_ReceiveData_Queue | Consume | ResendReceiveDataPartnerMessage | Data cần gửi lại |
| DeliveryReceive_DeliveryStatus_SendDataPartner_Response | Consume ← delivery_status | Response (TransactionId) | Xác nhận đã xử lý |
| DeliveryStatus_ReceiveStatus | Publish → delivery_status | MessageOrderStatus | Gửi data sang DeliveryStatus |
| LogDeliveryMessage | Publish | LogDeliveryMessage | Gửi log ra ngoài |

## appsettings.json Keys

```json
{
  "ServiceName": "DeliveryReceive.API",
  "ServiceKey": "...",
  "ServiceToken": "...",
  "ConnectionStrings": {
    "DeliveryReceiveConnectString": "server=...;database=DeliveryReceive;..."
  },
  "AppSettings": { "ServerId": 1 },
  "BusDelivery": {
    "RabbitMq": {
      "Host": ["...:5672"],
      "User": "...",
      "Password": "..."
    }
  }
}
```

> Production: RabbitMQ cluster 2 nodes, SQL Server pool max 300 connections.

## Môi trường

Development, Staging, Sprint, Production — Jobs chỉ chạy ở Staging/Sprint/Production.
