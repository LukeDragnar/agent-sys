---
controller: FBDataWebhook1stController
module: marketing
service: FacebookChatWebhook
man_hinh_ui: "nội bộ — không có UI. Route api/FBDataWebhook1st."
muc_dich: "Controller scaffold cho entity FBDataWebhook1st (log webhook 'chỉ lưu, chờ job') của service chat. HIỆN KHÔNG CÓ ENDPOINT — mọi action comment."
alias: ["FBDataWebhook1st chat", "log webhook FB chat lưu trước"]
auth: public
state_machine: []
deprecated: false
entities: [FBDataWebhook1st]
stored_procs: []
depends:
  - "IFBDataWebhook1stService (inject, chưa expose endpoint)"
cross_module_in: []
cross_module_out: []
---

# FBDataWebhook1stController — Log webhook FB chat lưu-trước (scaffold)

> Route `api/FBDataWebhook1st`. Mọi action comment. Giống bản FacebookWebhook.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có endpoint active) | — | — | — | — | — | `IFBDataWebhook1stService` dùng ở `WebhookController` (nhánh saveDataOnly) + `FacebookWebhooksController`. | EF (FBDataWebhook1st) | Toàn bộ CRUD comment (`FBDataWebhook1stController.cs:26-111`). |

## Quan hệ
- **Dữ liệu (entities):** `FBDataWebhook1st`.

## Lỗi đã biết / lưu ý kỹ thuật
- Scaffold; chưa có API tra cứu.
- Trùng tên với bản FbWebhook → node suffix `_FbChatWebhook`.
