---
controller: TiktokWebhooksController
module: marketing
service: TiktokWebhook
man_hinh_ui: "nội bộ — không có UI. Endpoint webhook public do TikTok Shop gọi vào để đẩy event (video/livestream/feed)."
muc_dich: "Cổng nhận webhook event từ TikTok Shop: đọc raw body, random load-balance ra 1 trong 10 queue (RabbitN) và publish cho consumer xử lý nền."
alias: ["webhook Tiktok", "nhận webhook TikTok Shop", "TiktokWebhooks", "ReceiveData Tiktok", "callback Tiktok"]
auth: anonymous
state_machine: []
deprecated: false
entities: [TTVideo]
stored_procs: []
depends:
  - "MassTransit IPublishEndpoint (publish TiktokRequestDataQueue0..10)"
  - "TiktokWebhookProcess.API Consumer (WebhookConsumer0..10) — xử lý message nền"
  - "Models.Core.DTO.Tiktok.Client.TiktokWebhook (TiktokRequestData*)"
cross_module_in:
  - "TikTok Shop (external) — gọi POST api/Webhooks/ReceiveData khi có event"
cross_module_out:
  - "RabbitMQ queue Rabbit1..Rabbit10 → WebhookConsumer0..10 (TiktokWebhookProcess) đồng bộ video/đơn TikTok về SCRM"
---

# TiktokWebhooksController — Cổng nhận webhook TikTok Shop

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Nhận webhook event TikTok | (không có UI — TikTok gọi) | POST `api/Webhooks/ReceiveData` | — (không) | **anonymous** (`[AllowAnonymous]`) | luôn mở | Đọc raw body UTF-8 → tăng `_requestCounter` (Interlocked) → `queueIndex = counter % 10 + 1` → deserialize body sang `TiktokRequestDataQueueN` đúng index → `Publish` lên queue tương ứng → trả `Ok("Done")`. Lỗi JSON → 400; lỗi khác → 500. | EF: chỉ inject `IRepository<TTVideo>` (hiện không dùng trong action). Publish MassTransit. | **Anonymous + KHÔNG xác thực chữ ký** — xem mục Bảo mật. |

## Quy tắc nghiệp vụ (load-balance queue)
- Đếm request toàn cục `_requestCounter` (static, `Interlocked.Increment`); khi tràn < 0 thì reset 0.
- `queueIndex = _requestCounter % 10 + 1` → luân phiên đều 10 queue (Rabbit1..Rabbit10), `default` rơi vào `TiktokRequestDataQueue0`.
- Mục đích: phân tải webhook lưu lượng cao ra nhiều consumer song song.

## Quan hệ
- **Gọi sang:** MassTransit `IPublishEndpoint.Publish` (10 DTO queue khác nhau).
- **Dữ liệu (entities):** `TTVideo` (repo inject sẵn, action hiện chưa ghi DB — ghi DB nằm ở consumer).
- **Ripple / đồng bộ:** message → `WebhookConsumer0..10` (project `TiktokWebhookProcess.API`) xử lý nền, đồng bộ video/đơn/feed TikTok về SCRM.

## Luồng xuyên module
TikTok Shop bắn event → `POST api/Webhooks/ReceiveData` → publish RabbitN → `WebhookConsumerN` consume → ghi `TTVideo`/đẩy tiếp sang core SCRM.

## Bảo mật / lỗ hổng (anonymous nhận data)
- Endpoint `[AllowAnonymous]`, **không verify signature TikTok** (không thấy check `Authorization`/HMAC/timestamp trong action) → bất kỳ ai biết URL đều có thể POST payload giả, bơm message rác vào toàn bộ 10 queue → consumer xử lý data giả mạo (đơn/video ảo). Cần thêm xác thực chữ ký webhook TikTok (app secret) trước khi publish.
- Counter `static` toàn process: không phải vấn đề bảo mật nhưng là điểm chia tải, mất khi restart.

## Lỗi đã biết / lưu ý kỹ thuật
- `TiktokWebhooksController.cs:34-140` — `ReceiveData` không xác thực nguồn gọi (anonymous, no signature).
- `TiktokWebhooksController.cs:19,31` — inject `IRepository<TTVideo>` nhưng action không dùng → dư hoặc dành cho mở rộng.
- `TiktokWebhooksController.cs:62-123` — switch 10 nhánh DTO gần như copy-paste; drift cao nếu sửa 1 nhánh quên nhánh khác.
