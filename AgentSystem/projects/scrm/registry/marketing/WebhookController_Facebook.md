---
controller: WebhookController
module: marketing
service: Facebook
man_hinh_ui: "nội bộ — không có UI (cổng webhook Facebook của service Facebook). Route api/Webhook."
muc_dich: "Cổng webhook Facebook của service Facebook (bản BẮN QUEUE): GET verify challenge + POST nhận raw payload rồi PHÂN TÁN ngẫu nhiên vào 1 trong 11 queue RabbitMQ (luân phiên theo counter % 10) cho consumer ContactReceiveFB xử lý. Khác bản FacebookWebhook (xử lý đồng bộ) — node WebhookController_FbWebhook."
alias: ["webhook FB queue", "Webhook", "api/Webhook", "ReceiveData", "verify webhook", "bắn queue webhook"]
auth: anonymous
state_machine: []
deprecated: false
entities: [FBDataWebhook, CauHinh]
stored_procs: []
depends:
  - "IPublishEndpointContactReceiveFB.Publish — bắn FacebookEnviromentRequestDataQueue0..10 vào RabbitMQ (MassTransit)"
  - "IPublishEndpoint / IFBDataWebhookService (x2) / IRepository<CauHinh> — inject nhưng KHÔNG dùng trong file này"
cross_module_in:
  - "Facebook/Meta platform — GET verify + POST sự kiện"
cross_module_out:
  - "RabbitMQ (11 queue luân phiên) → consumer ContactReceiveFB xử lý sự kiện FB sang Contact/hội thoại"
---

# WebhookController — Cổng webhook Facebook (bản bắn queue, service Facebook)

> Route `api/Webhook`. Nhận payload → publish ngẫu nhiên luân phiên vào `FacebookEnviromentRequestDataQueue0..10` (chia tải 11 queue) qua MassTransit. Bản khác (xử lý đồng bộ) là `WebhookController` của service FacebookWebhook (node `_FbWebhook`).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Verify webhook (Meta challenge) | (không UI) | GET `api/Webhook?hub.mode=&hub.verify_token=&hub.challenge=` | — | anonymous | Facebook gọi khi subscribe | `hub.mode.ToLower()=="subscribe"` và `hub.verify_token == "TokenSandboxVaFacebook"` → trả `hub.challenge`; ngược lại `""`. | (không EF) | **Verify token HARD-CODE** `"TokenSandboxVaFacebook"` (`WebhookController.cs:61`); config thật bị comment (dòng 60). |
| 2 | Nhận + bắn queue payload | (không UI) | POST `api/Webhook` | — | anonymous | Facebook POST sự kiện | Đọc raw body → tăng `_requestCounter`, `queueIndex = counter%10+1` → switch publish 1 trong `FacebookEnviromentRequestDataQueue0..10` (default → Queue0). `isDebug=false` (nhánh debug bỏ). Trả `MethodResult<FBDataWebhook>` (objLog thường null). | (queue MassTransit) | Xem mục lỗi: catch nuốt lỗi, không verify chữ ký, objLog luôn null. |

## Quy tắc nghiệp vụ (phân tải queue)
- `_requestCounter` static tăng `Interlocked.Increment`; nếu <0 reset về 0. `queueIndex = _requestCounter % 10 + 1` → giá trị 1..10, ánh xạ `EnumRabbitServerNumber.Rabbit1..Rabbit10`; case không khớp/`Default(0)` → `Queue0`. **Lưu ý: vì `+1` nên index không bao giờ = 0**, nhánh `case Default` (=0) gần như không bao giờ trúng → tải dồn vào Rabbit1..10 + default fallback.
- `isDebug = false` hard-code → bỏ nhánh `FacebookEnviromentRequestDataQueueDebug`.
- Deserialize body theo từng loại `FacebookEnviromentRequestDataQueueN` rồi `Publish` — KHÔNG xử lý/đọc nội dung tại controller.

## Quan hệ
- **Gọi sang:** `IPublishEndpointContactReceiveFB.Publish` (MassTransit → RabbitMQ).
- **Dữ liệu (entities):** không ghi DB tại controller (objLog dựng tạm nhưng `webhookData==null` → bỏ qua); consumer ghi `FBDataWebhook`.
- **Ripple / đồng bộ:** payload FB → 1 trong 11 queue → consumer ContactReceiveFB forward sang xử lý Contact/hội thoại (async, chia tải).

## Luồng xuyên module
- Meta POST `api/Webhook` → controller chỉ relay body vào RabbitMQ (luân phiên 11 queue) → consumer ContactReceiveFB xử lý tạo/cập nhật Contact + hội thoại. So với bản FacebookWebhook đồng bộ, ở đây hoàn toàn async qua queue để chịu tải cao.

## Lỗi đã biết / lưu ý kỹ thuật
- `WebhookController.cs:60-62` — verify token hard-code `"TokenSandboxVaFacebook"` (config bị comment).
- `WebhookController.cs:71-72` — POST `[AllowAnonymous]`, **không validate X-Hub-Signature-256** → có thể bị bơm payload giả thẳng vào queue.
- `WebhookController.cs:215-218` — `catch { webhookData=null; }` nuốt mọi exception (kể cả deserialize lỗi); chỉ trả "Lỗi đọc body", không log → khó debug, FB không retry.
- `WebhookController.cs:194-213` — khối dựng `objLog` chỉ chạy khi `webhookData != null`, nhưng `webhookData` không bao giờ được gán (dòng deserialize đã comment) → `objLog` luôn rỗng, response data vô nghĩa.
- `WebhookController.cs:117-184` — `queueIndex = counter%10 + 1` nên `case Default(=0)` không bao giờ trúng (dead branch); chỉ `default:` fallback hoạt động.
- `_publishEndpoint`, 2 `IFBDataWebhookService`, `_reposCauHinh` inject nhưng không dùng → dead dependency.
- Trùng tên với `WebhookController` service FacebookWebhook (node `_FbWebhook`) và bản FacebookChatWebhook → node suffix `_Facebook`.
