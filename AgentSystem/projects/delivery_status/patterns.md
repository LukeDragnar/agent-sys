# EcommerceConnect — Code Patterns & Conventions

## Cấu trúc Solution

```
{ServiceName}.API/
├── Controllers/
│   ├── Base/BaseController.cs
│   ├── Crm/          — endpoint CRM gọi vào
│   ├── Shopee/       — endpoint Shopee
│   ├── TikTok/       — endpoint TikTok
│   ├── MySystem/     — API nội bộ / config
│   └── LiveController.cs
├── Consumers/        — MassTransit IConsumer<T>
├── Jobs/             — Quartz IJob
└── Program.cs
```

## Naming Conventions

- Controllers: `{Platform}{Entity}Controller.cs` (vd: `CrmOrderController.cs`, `ShopeeProductController.cs`)
- Consumers: `{Action}Consumer.cs` (vd: `CreateOrderConsumer.cs`)
- Services: `I{Name}Service` interface
- Entities: kế thừa `EntityBase`
- RabbitMQ message models: trong `Models/RabbitMessage/`

## MassTransit Pattern

```csharp
public class CreateOrderConsumer : IConsumer<MessageCreateOrder>
{
    public async Task Consume(ConsumeContext<MessageCreateOrder> context)
    {
        var message = context.Message;
        // xử lý
    }
}
```

## Logging

Dùng `ILogDataService.WriteError(source, message)` → ghi vào bảng `LogData`.

## appsettings.json Keys

```json
{
  "ServiceName": "EcommerceConnect",
  "ConnectionStrings": { "DefaultConnection": "..." },
  "RabbitMq": { "Host": "...", "Username": "...", "Password": "..." },
  "Redis": "...",
  "CrmApiUrl": "..."
}
```
