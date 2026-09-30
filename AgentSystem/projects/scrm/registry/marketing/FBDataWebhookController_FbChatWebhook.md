---
controller: FBDataWebhookController
module: marketing
service: FacebookChatWebhook
man_hinh_ui: "nội bộ — không có UI. Route api/FBDataWebhook."
muc_dich: "Controller scaffold cho entity FBDataWebhook (log webhook đã lưu+xử lý) của service chat. HIỆN KHÔNG CÓ ENDPOINT — mọi action comment."
alias: ["FBDataWebhook chat", "log webhook FB chat"]
auth: public
state_machine: []
deprecated: false
entities: [FBDataWebhook]
stored_procs: []
depends:
  - "IFBDataWebhookService (inject, chưa expose endpoint)"
cross_module_in: []
cross_module_out: []
---

# FBDataWebhookController — Log webhook FB chat (scaffold, chưa có endpoint)

> Route `api/FBDataWebhook`. Mọi action comment. Giống bản FacebookWebhook.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có endpoint active) | — | — | — | — | — | Service `IFBDataWebhookService` dùng thật ở `WebhookController.ReceiveData`. | EF (FBDataWebhook) | Toàn bộ CRUD comment (`FBDataWebhookController.cs:26-111`). |

## Quan hệ
- **Dữ liệu (entities):** `FBDataWebhook`.

## Lỗi đã biết / lưu ý kỹ thuật
- Scaffold thuần; chưa có API.
- Trùng tên với bản FbWebhook → node suffix `_FbChatWebhook`.
