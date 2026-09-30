---
controller: LogController
module: he-thong
service: LogService
man_hinh_ui: "Mixed: ghi log lỗi/usage (nội bộ, mọi service gọi) + màn tra cứu log hệ thống (HT-AD)"
muc_dich: "Nhận log lỗi/usage từ các service (publish RabbitMQ → consumer ghi Elasticsearch) và cho HT-AD tra cứu log theo điều kiện."
alias: ["log lỗi", "log hệ thống", "tra cứu log", "LogError", "LogUsage"]
auth: mixed
state_machine: []
deprecated: false
entities: [LogDetail, EventLogError, EventLogUsage]
stored_procs: []
depends: [ILogViewService, IElasticSearch, IPublishEndpoint (RabbitMQ)]
cross_module_in: ["Mọi service POST LogError/LogUsage (AuthenLog)", "FE màn tra cứu log (HT-AD) gọi TimTheoDieuKien"]
cross_module_out: ["Publish EventLogError/EventLogUsage → consumer ghi Elasticsearch"]
---

# LogController — Log lỗi / usage hệ thống

Route `api/Log` (không `[TienIch]`). Ghi log dùng `[AuthenLog]`; tra cứu dùng `[SBAuthorize]` key (HT-AD) → **mixed**.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tra cứu log | Tìm | POST `api/Log/TimTheoDieuKien` | Log_TimTheoDieuKien | internal (HT-AD) | — | Trim+ToLower các field filter (Message/LogName/Service/GroupName/UserName/Source) rồi query | Elasticsearch | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] POST `api/Log/LogError` (`[AuthenLog]`) → map LogDetail→EventLogError, publish RabbitMQ, trả `"queued"`; consumer ghi Elasticsearch background.
- [internal] POST `api/Log/LogUsage` (`[AuthenLog]`) → map LogDetail→EventLogUsage, publish RabbitMQ, trả `"queued"`.

## Quan hệ
- **Gọi sang:** `ILogViewService.TimTheoDieuKien` (đọc Elasticsearch), `IPublishEndpoint` (RabbitMQ).
- **Dữ liệu (entities):** `LogDetail` (DTO), `EventLogError`, `EventLogUsage` (event).
- **Ripple / đồng bộ:** LogError/LogUsage publish event → consumer ghi Elasticsearch (background, eventual).

## Lỗi đã biết / lưu ý kỹ thuật
- `LogController.cs:88-114` — `ToErrorEvent`/`ToUsageEvent` map y hệt nhau (trùng field), có thể gộp.
