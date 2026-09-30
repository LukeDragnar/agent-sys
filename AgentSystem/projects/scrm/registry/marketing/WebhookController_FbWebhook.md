---
controller: WebhookController
module: marketing
service: FacebookWebhook
man_hinh_ui: "nội bộ — không có UI (cổng webhook Facebook cũ). Route api/Webhook."
muc_dich: "Cổng webhook CŨ nhận dữ liệu Facebook (xử lý ĐỒNG BỘ): GET verify challenge + POST nhận payload rồi LƯU + XỬ LÝ NGAY qua IFBDataWebhookService (hoặc chỉ lưu nếu bật saveDataOnly). Bản mới hơn là FacebookWebhooksController (bắn queue)."
alias: ["webhook FB cũ", "Webhook", "api/Webhook", "ReceiveData old", "verify webhook"]
auth: anonymous
state_machine: []
deprecated: false
entities: [FBDataWebhook, FBDataWebhook1st, CauHinh]
stored_procs: []
depends:
  - "IFBDataWebhookService.SaveAndProcessWebhookForward — lưu + xử lý đồng bộ"
  - "IFBDataWebhook1stService.SaveWebhook — chỉ lưu (khi saveDataOnly=true)"
  - "IRepository<CauHinh> — inject nhưng không dùng trong file này"
cross_module_in:
  - "Facebook/Meta platform — GET verify + POST sự kiện"
cross_module_out:
  - "CRM (qua SaveAndProcessWebhookForward → forward sự kiện FB sang xử lý Contact/hội thoại)"
---

# WebhookController — Cổng webhook Facebook (CŨ, xử lý đồng bộ)

> Route `api/Webhook`. Bản cũ: nhận xong xử lý ngay (không qua queue). Bản thay thế là `FacebookWebhooksController` (`api/v1/Webhooks`, bắn RabbitMQ).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Verify webhook (Meta challenge) | (không UI) | GET `api/Webhook?hub.mode=&hub.verify_token=&hub.challenge=` | — | anonymous | Facebook gọi khi subscribe | `hub.mode=="subscribe"` và `hub.verify_token == "TokenSandboxVaFacebook"` → trả `hub.challenge`; ngược lại `""`. | (không EF) | **Verify token HARD-CODE** `"TokenSandboxVaFacebook"` (`WebhookController.cs:46`). |
| 2 | Nhận + xử lý payload | (không UI) | POST `api/Webhook` | — | anonymous | Facebook POST sự kiện | Đọc raw body → `JsonConvert.DeserializeObject<FacebookRequestData>` → nếu `saveDataOnly` (hard-code false) → `SaveWebhook` (chỉ lưu); ngược lại `SaveAndProcessWebhookForward` (lưu + xử lý). Trả `MethodResult` data. | EF (FBDataWebhook / FBDataWebhook1st) | **POST anonymous KHÔNG verify chữ ký** Meta. **catch nuốt lỗi**: catch chỉ set `webhookData=null` rồi `return null` → lỗi xử lý bị giấu, FB nhận 200/empty. |

## Quy tắc nghiệp vụ
- `saveDataOnly = false` hard-code → luôn đi nhánh `SaveAndProcessWebhookForward` (lưu + xử lý đồng bộ). Nhánh `SaveWebhook` (lưu trước, job xử lý sau) là dự phòng, hiện không dùng.

## Quan hệ
- **Gọi sang:** `IFBDataWebhookService` / `IFBDataWebhook1stService`.
- **Dữ liệu (entities):** ghi log webhook vào `FBDataWebhook` (lưu+xử lý) hoặc `FBDataWebhook1st` (chỉ lưu).
- **Ripple / đồng bộ:** `SaveAndProcessWebhookForward` forward sự kiện sang xử lý CRM (Contact/hội thoại). So với `FacebookWebhooksController`, ở đây xử lý ngay trong request, không qua RabbitMQ.

## Lỗi đã biết / lưu ý kỹ thuật
- `WebhookController.cs:38-49` — verify token hard-code `"TokenSandboxVaFacebook"` (config bị comment).
- `WebhookController.cs:56-57` — POST `[AllowAnonymous]`, **không validate X-Hub-Signature-256** → có thể bị bơm payload giả.
- `WebhookController.cs:89-94` — `catch {}` chỉ gán `webhookData=null` rồi `return null` (HTTP 204/empty) → **mọi exception bị nuốt**, không log, khó debug; FB không retry vì không nhận lỗi.
- `_reposCauHinh` (`IRepository<CauHinh>`) được inject nhưng không dùng → dead dependency.
- Trùng tên với `WebhookController` bản FacebookChatWebhook (node `_FbChatWebhook`).
