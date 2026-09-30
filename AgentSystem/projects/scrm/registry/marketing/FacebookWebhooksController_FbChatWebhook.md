---
controller: FacebookWebhooksController
module: marketing
service: FacebookChatWebhook
man_hinh_ui: "nội bộ — không có UI (endpoint webhook Facebook gọi trực tiếp). Route api/v1/Webhooks."
muc_dich: "Cổng webhook MỚI nhận dữ liệu Facebook cho service CHAT (FacebookChatWebhook.API): GET verify challenge + POST nhận payload rồi BẮN VÀO QUEUE RabbitMQ. Khác bản FacebookWebhook: có nhánh DEBUG (queue riêng theo ASPNETCORE_ENVIRONMENT) + đính kèm Timeline (đo thời gian) và BodyRaw vào message."
alias: ["webhook Facebook Chat", "FacebookWebhooks Chat", "nhận webhook FB chat", "Meta webhook chat", "api/v1/Webhooks chat"]
auth: anonymous
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends:
  - "MassTransit IPublishEndpoint — publish FacebookChatRequestDataQueue0..10 + QueueDebug"
  - "IFBFanpageServerService.GetByPageId — (nhánh isPublishQueueByConfig=false → dead-code)"
  - "IServerDonViCauHinhService.GetAsync — (cùng nhánh dead-code)"
  - "EnumRabbitServerNumber (gồm Debug), TimeCheck (Timeline)"
cross_module_in:
  - "Facebook/Meta platform — GET verify + POST sự kiện chat (tin nhắn/messenger)"
cross_module_out:
  - "RabbitMQ → consumer FacebookChatWebhook (FacebookChatRequestDataQueue0..10/Debug) → xử lý hội thoại → CRM"
---

# FacebookWebhooksController — Cổng webhook Facebook Chat (NEW, bắn queue + Debug)

> Route `api/v1/Webhooks`. Giống bản FacebookWebhook nhưng cho luồng CHAT và bổ sung Debug routing + Timeline.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Verify webhook (Meta challenge) | (không UI) | GET `api/v1/Webhooks?hub.mode=&hub.verify_token=&hub.challenge=` | — | anonymous | Facebook gọi khi subscribe | `hub.mode=="subscribe"` và `hub.verify_token == "TokenSandboxVaFacebook"` → trả `hub.challenge`; ngược lại `""`. | (không EF) | **Verify token HARD-CODE** `"TokenSandboxVaFacebook"` (`FacebookWebhooksController.cs:58`). |
| 2 | Nhận payload webhook | (không UI) | POST `api/v1/Webhooks/ReceiveData` · POST `api/v1/Webhooks` | — | anonymous | Facebook POST sự kiện | Ghi `timeNow` → đọc raw body → `queueIndex = counter % 10 + 1` → nếu môi trường *Debug → queueIndex=Debug → tạo `FacebookChatRequestDataQueueN` set `Timeline`/`QueueNumber`/`BodyRaw=bodyData` → `Publish` → `Ok("Done")`. | (không EF — publish queue) | **KHÔNG verify chữ ký** X-Hub-Signature-256 (anonymous) → bơm payload giả vào queue. |

## Quy tắc nghiệp vụ (định tuyến queue)
- **Debug override:** đọc `ASPNETCORE_ENVIRONMENT`; nếu `*Debug` (StagingDebug/ProductionDebug/SprintDebug) → `queueIndex = EnumRabbitServerNumber.Debug` (bỏ qua round-robin) để gom về queue debug.
- **Round-robin (mặc định):** `isPublishQueueByConfig=false` → `queueIndex = _requestCounter % 10 + 1`.
- **Theo cấu hình đơn vị (DEAD CODE + buggy):** như bản FacebookWebhook — dùng `webhookData.Entry` khi `webhookData` còn `null` → bật cờ sẽ NRE.
- **Message khác bản kia:** mỗi case `new FacebookChatRequestDataQueueN { Timeline=lstTimeline, QueueNumber=..., BodyRaw=bodyData }` (đính raw + mốc thời gian để consumer đo latency), thay vì deserialize sẵn như FacebookWebhook.

## Endpoint nội bộ (private / service-to-service)
- Không có CRUD; đầu ra là message RabbitMQ `FacebookChatRequestDataQueue0..10` / `QueueDebug`.

## Quan hệ
- **Gọi sang:** `IPublishEndpoint.Publish`. Nhánh config (tắt): `IFBFanpageServerService` + `IServerDonViCauHinhService`.
- **Ripple:** consumer FacebookChatWebhook đọc queue → parse tin nhắn Messenger → đồng bộ hội thoại/Contact vào CRM.

## Luồng xuyên module (inbound Facebook chat)
1. FB subscribe → GET verify → trả challenge (nếu token khớp).
2. Có tin nhắn → POST `ReceiveData` → publish `FacebookChatRequestDataQueueN` (kèm Timeline/BodyRaw).
3. Consumer xử lý hội thoại → CRM.

## Lỗi đã biết / lưu ý kỹ thuật
- `FacebookWebhooksController.cs:50-61` — GET verify token hard-code `"TokenSandboxVaFacebook"`.
- `FacebookWebhooksController.cs:69-72` — POST anonymous **không verify chữ ký Meta** → lỗ hổng bơm payload giả.
- `FacebookWebhooksController.cs:107-155` — nhánh `isPublishQueueByConfig` dead code + NRE (`webhookData.Entry` khi null).
- `_requestCounter` static toàn process → cân bằng queue theo số lần gọi, không theo tải consumer.
- Khác bản FacebookWebhook: có Debug queue + Timeline/BodyRaw (node `FacebookWebhooksController_FbWebhook.md`).
