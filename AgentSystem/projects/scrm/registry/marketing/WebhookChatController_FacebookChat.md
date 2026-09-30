---
controller: WebhookChatController
module: marketing
service: FacebookChat
man_hinh_ui: "nội bộ — không có UI (endpoint webhook nhận dữ liệu Facebook; toàn bộ file đã comment)"
muc_dich: "Endpoint webhook ChatHub nhận sự kiện Facebook (verify + nhận payload đẩy sang RabbitMQ xử lý hội thoại). HIỆN TẠI: TOÀN BỘ file bị comment (kể cả namespace) → không endpoint nào hoạt động; luồng webhook chat thực tế chạy ở service FacebookReceive (node ...FbChatWebhook)."
alias: ["webhook chat FB", "nhận webhook ChatHub", "WebhookChat", "verify webhook facebook chat"]
auth: anonymous
state_machine: []
deprecated: true
entities: [FBDataWebhook, Conversation]
stored_procs: []
depends:
  - "IWebhookService / IFBWebhookService / IPublishEndpoint — (đã comment, không hoạt động)"
cross_module_in: []
cross_module_out: []
---

# WebhookChatController — Webhook nhận dữ liệu Facebook (FacebookChat) — SCAFFOLD (deprecated)

> Toàn bộ file (`WebhookChatController.cs:1-108`) bị comment, kể cả `namespace`. KHÔNG có endpoint hoạt động trong FacebookChat.API. Luồng webhook chat thật nằm ở service FacebookReceive — xem `WebhookChatController` / `FacebookWebhooksController_FbChatWebhook`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có) | — | — | — | anonymous | — | File comment toàn bộ → không endpoint active. (Logic gốc đã định: GET VerifyWebhook xác thực `hub.verify_token=="TokenSandboxVaFacebook"`; POST `[AllowAnonymous]` ReceiveWebhook đọc body, deserialize `FacebookRequestData`, Publish `FacebookRequestRabbit` lên RabbitMQ.) | — | Dead controller — toàn bộ + namespace comment. |

## Lỗi đã biết / lưu ý kỹ thuật
- `WebhookChatController.cs:1-108` — toàn bộ file comment (kể cả `namespace`) → không build thành controller hoạt động; chỉ là di tích, dùng node `_FbChatWebhook` (FacebookReceive) cho luồng webhook thật.
- Nếu khôi phục: `VerifyToken` hardcode `"TokenSandboxVaFacebook"` (`:51`); `ReceiveWebhook` `[AllowAnonymous]`, catch nuốt lỗi trả `null` (`:94-98`).
