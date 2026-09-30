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

**SetInfoBase pattern** (service dùng `_logDataService`): `SetInfoBase` ở đầu method, `WriteLog` ở `finally`.

```csharp
public async Task<IMethodResult<Guid>> Save(ReqSaveDto request)
{
    _logDataService.SetInfoBase(nameof(Save), request.EcommerceCode, null, userLogin);
    try { /* logic */ }
    catch (Exception ex)
    {
        _logDataService._messError += LogDataService.FormatLogDataException(ex);
        return MethodResult<Guid>.ResultWithError($"Lỗi: {_logDataService._idTrans}");
    }
    finally
    {
        _logDataService._messInfo += LogDataService.FormatLogData(nameof(ReqSaveDto), request);
        await _logDataService.WriteLog(_logDataService._messInfo, _logDataService._messError);
    }
}
```

## Pattern: Persist parent TRƯỚC khi gọi SP đọc parent

Khi gọi stored procedure đọc bảng cha qua `Id` (ví dụ `PRC_*_InsertByOrderDataId(@OrderDataId)`), entity cha **phải được persist trước đó** bằng `InsertAsync` / `UpdateAsync`. Set property in-memory **không tính là persist** — SP đọc DB sẽ thấy dữ liệu cũ → child rows insert với FK null/sai.

```csharp
// ❌ SAI
orderDataDb.CrmOrderId = newId;          // chỉ set RAM
await InsertInvoiceShopee_ByOrderDataId(orderDataDb.Id);  // SP đọc DB → CrmOrderId vẫn NULL
await reposOrderData.InsertAsync(orderDataDb);  // persist sau khi SP đã chạy → quá muộn

// ✅ ĐÚNG
orderDataDb.CrmOrderId = newId;
await reposOrderData.InsertAsync(orderDataDb);   // persist trước
await InsertInvoiceShopee_ByOrderDataId(orderDataDb.Id);  // SP đọc được CrmOrderId
```

Bug này đã gây ra hàng trăm record `InvoiceShopee` lỗi với `CrmOrderId NULL`, retry vô hạn cho đến khi quá 15 ngày (May 2026).

## Pattern: Retry job re-lookup foreign keys khi null

Khi job retry gửi external API với foreign key fields (ví dụ `CrmOrderId`), **bắt buộc re-lookup** từ bảng nguồn nếu field hiện tại NULL — không dùng giá trị stale từ row.

```csharp
// Trong loop xử lý retry
if (string.IsNullOrWhiteSpace(invoice.CrmOrderId))
{
    var orderData = await reposOrderData.TableWithNolock()
        .Where(o => o.PartnerOrderCode == invoice.PartnerOrderCode
            && o.UnitConnectId == invoice.UnitConnectId
            && o.IsDeleted == false)
        .Select(o => new { o.CrmOrderId, o.CrmOrderCode })  // projection để tránh kéo NVARCHAR(MAX)
        .FirstOrDefaultAsync();

    if (orderData != null && !string.IsNullOrWhiteSpace(orderData.CrmOrderId))
    {
        invoice.CrmOrderId = orderData.CrmOrderId;
        invoice.CrmOrderCode = orderData.CrmOrderCode;
        invoice.MarkDirty(nameof(invoice.CrmOrderId));
        invoice.MarkDirty(nameof(invoice.CrmOrderCode));
    }
}
```

## Pattern: Filter external API response trước khi forward

Khi forward response từ external API (Shopee, TikTok, ...) sang service downstream (CRM), **kiểm tra field `error` / status** trong từng item của response. Không assume `httpResponse.Success == true` đồng nghĩa data có ý nghĩa.

```csharp
foreach (var item in shopeeResponse.Data.invoice_info_list)
{
    // ✅ Filter: Shopee có thể trả error nhưng wrap trong success response
    if (!string.IsNullOrWhiteSpace(item.error) || item.invoice_detail == null)
    {
        await SetDelete(invoice, $"Bỏ qua — Shopee không có data: {item.error}");
        continue;
    }
    // ... gửi CRM
}
```

Ví dụ: Shopee `GetBuyerInvoiceInfo` có thể return `error: "receipt settings not found"` cho từng order trong list, trong khi response wrapper vẫn `Success=true`. Forward thẳng sang CRM sẽ tạo "fake success" với data rỗng.

## Pattern: Webhook nội bộ ECOMM ↔ SCRM — không auth, không realtime push

ECOMM và SCRM chạy **cùng cluster K8s / VPN nội bộ** → trust ở network layer. Webhook giữa 2 service theo pattern đơn giản (mirror `OrderLogistic.API/.../DonHangController.NhanDuLieuDonHangTMDT`):

```csharp
[Route("api/[controller]")]
[ApiController]
public partial class XxxWebhookController : BaseController
{
    private readonly IXxxService _service;
    public XxxWebhookController(IXxxService service) { _service = service; }

    //[SBAuthorize()]   // commented — internal trust
    [HttpPost("NhanDuLieuXxx")]
    public async Task<IActionResult> NhanDuLieuXxx(XxxReqDto reqData)
        => Ok(await _service.NhanDuLieuXxx(reqData));
}
```

**KHÔNG dùng** (overengineer cho mạng nội bộ): HMAC/signature verify · API key header · JWT Bearer · `[AllowAnonymous]` · `EnableBuffering`+raw body · MassTransit→SignalR realtime push · ILogger riêng. Nếu cần alert push (vd deadline khiếu nại) → cron job + persist `NotificationInfo` + FE pull, KHÔNG realtime inline trong webhook. Review PR thấy raw-body/HMAC/IConfiguration inject → flag overengineered.

## Pattern: Ranh giới logic ECOMM (adapter) vs CRM/SCRM (generic)

Trước khi add field/config/logic, tự hỏi: **partner-specific adapter** hay **generic business logic**? SCRM/CRM dùng chung cho **mọi nguồn** (Shopee/TikTok/Lazada + non-sàn) → nhồi business logic vào ECOMM = phải copy khi làm sàn khác.

| Loại | Đặt ở | Ví dụ |
|---|---|---|
| Partner adapter (call API riêng sàn) | **ECOMM** | `ShopeeReturnsService.Dispute()`, push code mapping |
| Webhook receiver từ sàn | **ECOMM** | `PushNotificationService`, `OrderReturnShopeeService.ProcessSendOrderReturnToCrm` |
| Forward DTO sàn→CRM (data only, no decision) | **ECOMM** | `CrmOrderReturnModel` |
| Generic business config (auto-create HBBTL, deadline, phí…) | **CRM/SCRM** | `AUTO_CREATE_HBBTL` flag trong `Ecommerce.DonHangHoanService` |
| Generic UI / cross-source report | **CRM** | màn hình KHÔNG wording "Shopee/TikTok" |

Quy tắc: áp dụng cho **mọi nguồn + non-sàn** → CRM-side; chỉ **1 sàn** → ECOMM; nghi ngờ → hỏi user.

## Pattern: Defer persist tới sau khi call ngoài thành công (luồng thủ công)

Luồng **sync/create thủ công** có gọi service ngoài giữa chừng (vd nút "Lấy đơn hoàn mới" → build payload → push CRM): **KHÔNG `InsertAsync` record local upfront**. Build in-memory → push ngoài → **chỉ Insert khi thành công**; fail thì return sớm, không để row mồ côi `Status=null`.

```csharp
// CREATE: object in-memory (Id=NewGuid) → sync field → push ngoài → thành công MỚI InsertAsync
// UPDATE (record đã có DB): sync field → push → MarkDirty + UpdateAsync sau khi thành công
```

**Khác với webhook job** (vd `ProcessSendOrderReturnToCrm`): job CỐ Ý Insert-upfront + `catch` set `Status=Error` + SP retry cap `ResendNo` (lưới an toàn) → orphan được track & retry. Luồng thủ công KHÔNG có lưới đó nên phải defer. Bug HIGH đã gặp: row `Status=null` làm discovery idempotent bỏ qua sn đó → đơn kẹt vĩnh viễn, phải xóa tay.

## Docs Notation — Gợi ý kèm URL nguồn khi user cấp link

**Quy tắc workflow (suggestion, KHÔNG bắt buộc):**

Khi user cấp URL docs partner (Shopee Open Platform, TikTok Shop Partner, Lazada Open Platform) liên quan đến 1 thực thể trong code, **Claude confirm với user trước khi save URL vào `<summary>` của thực thể liên quan** (function, enum, const, class, model property…).

**Format:** URL nằm BÊN TRONG `<summary>` tag, không tách riêng block khác.

```csharp
/// <summary>
/// Result api GetReturnDetail
/// Docs: https://open.shopee.com/documents/v2/v2.returns.get_return_detail
/// Field order theo đúng thứ tự response trong tài liệu Shopee Open API.
/// </summary>
public class ResGetReturnDetail : ShopeeBaseModel.ResBase { ... }
```

| Loại link | Save vào `<summary>` của |
|-----------|--------------------------|
| API endpoint (vd `v2.returns.get_return_detail`) | Class/method consuming trong `.cs` (vd `ResGetReturnDetail`, `ShopeeReturnsService.GetReturnDetail()`) |
| Push code / webhook code | Const trong `EnumShopee.PushMechanismCode` (vd `ReturnUpdatesPush`) |
| Enum / Data Definition | Enum class/field liên quan trong `EnumShopee.cs` hoặc model |
| Property/field cụ thể (vd `return_refund_request_type`) | XML doc của property đó trong model class |
| Overview / developer guide (concept-level, không gắn 1 entity) | File `.md` trong `Shopee/Docs/` |

**Why:** URL gắn vào `<summary>` của entity liên quan giúp dev đọc code nhìn ngay verify-link, hover IntelliSense hiển thị URL. Đặt vào `.md` chỉ khi link là concept overview không gắn 1 entity cụ thể.

**Workflow chuẩn:**
1. User paste URL + context
2. Claude xác định entity liên quan trong code → đề xuất "save vào `<summary>` của X"
3. User confirm OK → Claude Edit thêm dòng `Docs: <url>` vào trong `<summary>` existing
4. Nếu user không confirm/không reply → skip, chỉ note vào response

**Examples existing pattern (đã có trong code):**
- [ReturnsModel.cs:15-19](D:/PROJECT/Git/_SourceForCode/ecommerce_connect/EcommerceConnect.DataContext/Model/Shopee/ReturnsModel.cs#L15) — URL trong `<summary>` của `ResGetReturnDetail`
- [OrderReturnShopeeService.cs:22-24](D:/PROJECT/Git/_SourceForCode/ecommerce_connect/EcommerceConnect.Service/Generated/MySystem/OrderReturnShopeeService.cs#L22) — URL trong `<summary>` của service class

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
