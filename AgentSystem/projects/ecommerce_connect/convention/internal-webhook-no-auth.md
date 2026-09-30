---
name: ECOMM↔SCRM webhook — internal cluster, no auth no realtime push
description: Webhook giữa ECOMM và SCRM services chạy nội bộ cluster/VPN — không HMAC/JWT, không MassTransit signalr realtime push, theo pattern existing NhanDuLieuDonHangTMDT
metadata:
  type: feedback
---

ECOMM và SCRM services chạy cùng cluster K8s / VPN nội bộ. Khi thiết kế webhook giữa 2 service:

**Pattern bắt buộc theo (mirror `OrderLogistic.API/Controllers/Ecommerce/DonHangController.NhanDuLieuDonHangTMDT` + `DonHangHoanEcommController.NhanDuLieuDonHangHoanTMDT`):**

```csharp
[TienIchAttribute("...", "Webhook ...", MaTienIchSCRM.TI_OrderLogistic)]
[Route("api/[controller]")]
[ApiController]
public partial class XxxWebhookController : BaseController
{
    private readonly IXxxService _service;

    public XxxWebhookController(IXxxService service)
    {
        _service = service;
    }

    //[SBAuthorize()]  // commented — internal trust
    [HttpPost("NhanDuLieuXxx")]
    public async Task<IActionResult> NhanDuLieuXxx(XxxReqDto reqData)
    {
        var rs = await _service.NhanDuLieuXxx(reqData);
        return Ok(rs);
    }
}
```

**KHÔNG dùng:**
- HMAC / signature verify (mạng nội bộ trust nhau)
- API Key tĩnh trong header
- JWT Bearer token
- `[AllowAnonymous]` annotation (vì không nằm sau JWT middleware require)
- `EnableBuffering` + raw body read
- `[FromBody]` attribute explicit (mặc định ASP.NET bind đủ)
- MassTransit publish event chain → consumer push SignalR realtime cho FE
- ILogger inject riêng cho webhook (service tự log)
- Custom HMAC helper

**Lý do:**
- ECOMM ↔ SCRM nằm cùng cluster, trust ở network layer
- Realtime SignalR push qua webhook là overengineer — FE refresh thủ công đủ cho 99% case
- Nếu cần alert push (vd deadline khiếu nại) → dùng cron job + persist NotificationInfo + FE pull notifications (existing pattern), KHÔNG cần realtime

**How to apply:**
- Mới làm webhook ECOMM→SCRM (vd return update, refund status) → copy template trên, không add HMAC/MassTransit
- Reviewing PR có webhook: thấy raw body read / HMAC / IConfiguration inject → flag overengineered
- Nếu use case THỰC SỰ cần realtime push → tạo riêng cron + consumer, KHÔNG inline vào webhook controller

Related: [[scrm-always-nolock]] (BE convention chung), [[ecomm-vs-crm-logic-boundary]] (adapter vs generic boundary).
