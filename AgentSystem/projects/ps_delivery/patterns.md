# PS_Delivery — Code Patterns & Conventions

## Cấu trúc Solution

```
ps_delivery/
├── Service/                          # PsWebhook service
│   ├── PsWebhook.API/                # Entry point, Controllers, Jobs
│   ├── PsWebhook.DataContext/        # EF Core DbContext, Entities
│   ├── PsCommon/                     # Shared utilities
│   └── EFCore.SqlServer.WithNoLock/  # Custom EF Core extension (WITH NOLOCK)
│
└── ps_delivery_receive/              # DeliveryReceive service
    ├── DeliveryReceive.API/          # Entry point, Controllers, Consumers
    ├── DeliveryReceive.Service/      # Business logic (Generated/ folder)
    └── DeliveryReceive.DataContext/  # EF Core, Entities, Models, Mapper
```

## Controller Structure (DeliveryReceive.API)

Controllers phân theo nhóm:
- `Controllers/Partner/` — endpoint cho từng đối tác vận chuyển gọi vào (AhamoveController, GhnController, GhtkController, VtpController...)
- `Controllers/CRM/` — forward data về CRM (CrmDataForwardController)
- `Controllers/Base/` — base controller
- `Controllers/MySystem/` — API nội bộ
- `LiveController.cs` — health check / live endpoint

## Naming Conventions

- Controllers: `{PartnerName}Controller.cs` (vd: `GhnController.cs`, `VtpController.cs`)
- Consumers: `PS_{Name}Consumer.cs` (vd: `PS_DeliveryReceiveDataPartnerConsumer.cs`)
- Services: `I{Name}Service` interface, implement trong `Generated/`
- Entities: `EntityBase` là base class chung
- RabbitMQ messages: namespace `PS_DeliveryReceive.RabbitMessage`, model trong `DeliveryReceive.DataContext.Model.RabbitMessage`

## MassTransit + RabbitMQ (DeliveryReceive)

```csharp
// Consumer pattern
public class PS_DeliveryReceiveDataPartnerConsumer : IConsumer<PS_DeliveryReceiveDataPartnerMessage>
{
    public async Task Consume(ConsumeContext<PS_DeliveryReceiveDataPartnerMessage> context)
    {
        var item = context.Message;
        // xử lý item.ShippingUnitCode và item.Data
    }
}
```

## Jobs (PsWebhook.API — Quartz / background)

- `JobClearLog` — dọn log cũ
- `JobProcessReceiveData` — xử lý data pending trong DB
- `JobSendTheFirstTime` — gửi lần đầu các data mới
- `JobFixError` — fix các record lỗi (disabled — bị Remove khỏi compile)

## Database Pattern

- PsWebhook dùng `EFCore.SqlServer.WithNoLock` — truy vấn `WITH (NOLOCK)` để tránh lock
- DeliveryReceive có `SqlServerWithNoLock/` folder tương tự trong DataContext

## Logging

PsWebhook gửi log ra external log service:
```json
{
  "LogServiceUrl": "http://10.0.63.2:50007",
  "ServiceName": "PS_Webhook",
  "ServiceKey": "...",
  "ServiceToken": "..."
}
```

DeliveryReceive dùng `ILogDataService.WriteError(source, message)`.

## Environments

DeliveryReceive có nhiều môi trường: Development, Staging, Sprint, Production, ProductionDebug, SprintDebug, StagingDebug.
Build/deploy qua Docker (`Dockerfile_*` và `docker_build_and_push-*.bat`).
