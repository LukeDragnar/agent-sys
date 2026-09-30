---
controller: WebhookController
module: marketing
service: FacebookChatWebhook
man_hinh_ui: "nội bộ — không có UI (cổng webhook Facebook chat cũ). Route api/Webhook."
muc_dich: "Cổng webhook CŨ của service chat (xử lý ĐỒNG BỘ): GET verify + POST nhận payload rồi LƯU + XỬ LÝ NGAY qua IFBDataWebhookService (hoặc chỉ lưu nếu saveDataOnly). Bản mới hơn là FacebookWebhooksController (bắn queue + Debug)."
alias: ["webhook FB chat cũ", "Webhook chat", "api/Webhook chat", "ReceiveData old chat"]
auth: anonymous
state_machine: []
deprecated: false
entities: [FBDataWebhook, FBDataWebhook1st, CauHinh]
stored_procs: []
depends:
  - "IFBDataWebhookService.SaveAndProcessWebhookForward"
  - "IFBDataWebhook1stService.SaveWebhook"
  - "IRepository<CauHinh> — inject nhưng không dùng"
cross_module_in:
  - "Facebook/Meta platform — GET verify + POST sự kiện chat"
cross_module_out:
  - "CRM (SaveAndProcessWebhookForward → xử lý hội thoại)"
---

# WebhookController — Cổng webhook Facebook Chat (CŨ, đồng bộ)

> Route `api/Webhook`. Bản cũ xử lý ngay; bản thay thế `FacebookWebhooksController` (`api/v1/Webhooks`). Mã giống hệt bản FacebookWebhook (chỉ khác namespace/service).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Verify webhook | (không UI) | GET `api/Webhook?hub.mode=&hub.verify_token=&hub.challenge=` | — | anonymous | FB subscribe | Token khớp `"TokenSandboxVaFacebook"` → trả challenge; ngược lại `""`. | (không EF) | Verify token hard-code (`WebhookController.cs:46`). |
| 2 | Nhận + xử lý payload | (không UI) | POST `api/Webhook` | — | anonymous | FB POST sự kiện | Deserialize `FacebookRequestData` → `saveDataOnly` (false) → `SaveAndProcessWebhookForward` (lưu+xử lý); else `SaveWebhook`. | EF (FBDataWebhook/1st) | POST anonymous không verify chữ ký; **catch nuốt lỗi** → `webhookData=null; return null`. |

## Quan hệ
- **Gọi sang:** `IFBDataWebhookService` / `IFBDataWebhook1stService`.
- **Dữ liệu (entities):** `FBDataWebhook` / `FBDataWebhook1st`.
- **Ripple:** forward sự kiện chat sang xử lý CRM (hội thoại).

## Lỗi đã biết / lưu ý kỹ thuật
- `WebhookController.cs:38-49` — verify token hard-code.
- `WebhookController.cs:56-57` — POST anonymous không validate X-Hub-Signature-256.
- `WebhookController.cs:89-94` — `catch {}` nuốt mọi exception, `return null`.
- `_reposCauHinh` inject không dùng.
- Trùng tên với bản FacebookWebhook → node suffix `_FbChatWebhook`.
