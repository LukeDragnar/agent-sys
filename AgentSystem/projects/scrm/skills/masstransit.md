# Recipe — MassTransit publish/consume

> **Trạng thái: ✅ Verify từ source SCRM** (quét 2026-06-19).

## Khi nào dùng
Gửi message giữa các service (loosely-coupled) qua RabbitMQ.

## Định nghĩa message
Class/DTO đơn giản trong `Libs/Models.Core/DTO/.../*.cs`:
```csharp
public class BankOnlineReceive {
    public string MaHoaDon { get; set; }
    public double TongTien { get; set; }
    public string MaThanhToanBank { get; set; }
}
```

## Publish
```csharp
private readonly IPublishEndpointDonHang _publishEndpointDonHang;
await _publishEndpointDonHang.Publish(reqBody);
// hoặc generic: await _publishEndpoint.Publish<MessageLog>(new MessageLog { ... });
```

## Consumer
```csharp
public class BankOnlineReceiveConsumer : IConsumer<BankOnlineReceive>
{
    public async Task Consume(ConsumeContext<BankOnlineReceive> context)
    {
        var item = context.Message;
        var donHang = await _donHangRepos.TableWithNolock()
            .Where(x => x.MaHoaDon == item.MaHoaDon).FirstOrDefaultAsync();
        // ... xử lý
    }
}
```

## Đăng ký ReceiveEndpoint
Trong `MassTransitExtensions.cs`, **consumer production đặt trong `if (!IsDebug)`**:
```csharp
if (!IsDebug) {
    cfg.ReceiveEndpoint(string.Format(QueueKeys.Queue_Name, ServiceName), e => {
        e.PrefetchCount = 10;
        e.UseMessageRetry(r => r.Interval(2, 3000));   // retry 2 lần × 3s (bỏ nếu idempotency lo bởi job)
        e.ConfigureConsumer<BankOnlineReceiveConsumer>(context);
        e.DiscardFaultedMessages(); e.DiscardSkippedMessages();
    });
}
```

## Bẫy thường gặp
- **Consumer production nằm trong `if (!IsDebug)`** — debug local không nhận message (set `ASPNETCORE_ENVIRONMENT=Production` hoặc tạm bỏ block để test).
- **Idempotency:** message có thể retry → check đã xử lý chưa (`AnyAsync(x => x.MaThanhToanBank == ...)`) trước khi ghi.
- `UseMessageRetry(...)` bỏ đi nếu idempotency đảm bảo bởi scan job (tránh xử lý kép).
- Không trùng tên queue/endpoint giữa các consumer.

## File source mẫu
- Message: `Libs/Models.Core/DTO/BankReceive.Client/BankOnlineReceive.cs`
- Consumer: `Order/Order.API/Consumer/BankOnlineReceiveConsumer.cs`
- Publish: `Order/Order.API/Controllers/PaymentController.cs:61`
- Config: `OrderLogistic/OrderLogisticConsumer.API/Service/MassTransitExtensions.cs`
