---
controller: FacebookWebhooksController
module: marketing
service: FacebookWebhook
man_hinh_ui: "nội bộ — không có UI (endpoint webhook Facebook gọi trực tiếp). Route api/v1/Webhooks."
muc_dich: "Cổng webhook MỚI nhận dữ liệu từ Facebook (project FacebookWebhook.API): GET verify challenge của Meta + POST nhận payload rồi BẮN VÀO QUEUE RabbitMQ (round-robin 1..10) để consumer xử lý bất đồng bộ. Không xử lý nghiệp vụ trực tiếp."
alias: ["webhook Facebook", "FacebookWebhooks", "nhận webhook FB", "Meta webhook", "api/v1/Webhooks", "ReceiveData"]
auth: anonymous
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends:
  - "MassTransit IPublishEndpoint — publish FacebookRequestDataQueue0..10 vào RabbitMQ"
  - "IFBFanpageServerService.GetByPageId — (nhánh isPublishQueueByConfig=false nên hiện KHÔNG chạy)"
  - "IServerDonViCauHinhService.GetAsync — (cùng nhánh dead-code trên)"
  - "EnumRabbitServerNumber (Models.Core.Enum.FacebookWebhook)"
cross_module_in:
  - "Facebook/Meta platform — gọi GET (verify) + POST (sự kiện message/feed) tới endpoint này"
cross_module_out:
  - "RabbitMQ → consumer FacebookWebhook (FacebookRequestDataQueue0..10) → xử lý message/feed → đẩy về CRM (Contact/Conversation)"
---

# FacebookWebhooksController — Cổng webhook Facebook (NEW, bắn queue)

> Route `api/v1/Webhooks`. Controller MỚI thay cho `WebhookController` cũ: thay vì xử lý đồng bộ, nó chỉ nhận raw body rồi publish vào 1 trong 10 queue (luân phiên) cho consumer xử lý.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Verify webhook (Meta challenge) | (không UI) | GET `api/v1/Webhooks?hub.mode=&hub.verify_token=&hub.challenge=` | — | anonymous | Facebook gọi khi subscribe | Nếu `hub.mode=="subscribe"` và `hub.verify_token == "TokenSandboxVaFacebook"` → trả `hub.challenge`; ngược lại trả `""`. | (không EF) | **Verify token HARD-CODE** `"TokenSandboxVaFacebook"` ngay trong code (`FacebookWebhooksController.cs:56`), không đọc từ config (dòng `_settingService.GetBySettingNameAsync("FB_TOKEN_WEBHOOK")` đã comment). |
| 2 | Nhận payload webhook | (không UI) | POST `api/v1/Webhooks/ReceiveData` · POST `api/v1/Webhooks` | — | anonymous | Facebook POST sự kiện | Đọc raw body → tăng `_requestCounter` (static) → `queueIndex = counter % 10 + 1` → deserialize body sang `FacebookRequestDataQueueN` tương ứng → `Publish` vào RabbitMQ → trả `Ok("Done")`. | (không EF — chỉ publish queue) | **KHÔNG xác thực chữ ký** (X-Hub-Signature-256) ở POST → bất kỳ ai biết URL đều bơm được payload giả vào queue. Lỗ hổng (xem mục Lỗi). |

## Quy tắc nghiệp vụ (định tuyến queue)
- **Round-robin (đang dùng):** `isPublishQueueByConfig = false` (hard-code) → `queueIndex = _requestCounter % 10 + 1` (luân phiên Rabbit1..Rabbit10, riêng case Default=0 chỉ rơi vào nếu queueIndex match enum Default).
- **Theo cấu hình đơn vị (DEAD CODE):** nếu `isPublishQueueByConfig=true` → lấy `pageId` từ `webhookData.Entry[0].Id` rồi `GetByPageId` → tra `ServerDonViCauHinh` → lấy `RabbitServerNumber`. **NHƯNG** `webhookData` luôn `null` tại thời điểm này (chỉ gán sau khi deserialize trong từng case) → nếu bật cờ sẽ NullReferenceException. Hiện cờ tắt nên không chạy.
- **Mapping case:** mỗi `EnumRabbitServerNumber` (Default, Rabbit1..Rabbit10) → 1 type `FacebookRequestDataQueueN` riêng (publish type khác nhau để route exchange khác nhau).

## Endpoint nội bộ (private / service-to-service)
- Không có endpoint private kiểu CRUD. Toàn bộ "đầu ra" là message RabbitMQ (`FacebookRequestDataQueue0..10`) cho consumer FacebookWebhook tiêu thụ.

## Quan hệ
- **Gọi sang:** `IPublishEndpoint.Publish(...)` (MassTransit/RabbitMQ). Nhánh config gọi `IFBFanpageServerService` + `IServerDonViCauHinhService` (hiện không chạy).
- **Dữ liệu (entities):** không đụng EF trực tiếp; chỉ chuyển raw body thành message.
- **Ripple / đồng bộ:** consumer nhận queue → parse sự kiện FB (tin nhắn/comment/feed) → tạo Contact/Conversation/Order trong CRM. Đây là cửa vào toàn bộ luồng inbound Facebook.

## Luồng xuyên module (inbound Facebook)
1. Facebook subscribe → GET verify (challenge) → controller trả challenge (nếu token khớp).
2. Có sự kiện (tin nhắn/comment) → Facebook POST `ReceiveData` → controller publish vào `FacebookRequestDataQueueN`.
3. Consumer FacebookWebhook đọc queue → xử lý → đồng bộ về CRM (Contact/hội thoại/đơn).

## Lỗi đã biết / lưu ý kỹ thuật
- `FacebookWebhooksController.cs:48-59` — **GET verify dùng token hard-code** `"TokenSandboxVaFacebook"`; nếu lộ token này thì bất kỳ ai cũng pass được bước verify. Nên đọc từ config/secret.
- `FacebookWebhooksController.cs:67-70` — **POST anonymous KHÔNG verify X-Hub-Signature-256** (chữ ký HMAC của Meta). Lỗ hổng: kẻ tấn công biết URL có thể bơm payload giả → consumer xử lý dữ liệu rác / giả mạo hội thoại. Khuyến nghị validate signature bằng app secret trước khi publish.
- `FacebookWebhooksController.cs:99-146` — nhánh `isPublishQueueByConfig` là **dead code và buggy**: dùng `webhookData.Entry` khi `webhookData` vẫn `null` → bật cờ sẽ NRE.
- `_requestCounter` là `static long` chia sẻ toàn process → cân bằng queue chỉ theo số lần gọi, không theo tải thật của consumer.
- Khác biệt với bản FacebookChatWebhook: bản này KHÔNG có nhánh Debug/Timeline/BodyRaw (xem `FacebookWebhooksController_FbChatWebhook.md`).
