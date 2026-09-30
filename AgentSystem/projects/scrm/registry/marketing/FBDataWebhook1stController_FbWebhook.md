---
controller: FBDataWebhook1stController
module: marketing
service: FacebookWebhook
man_hinh_ui: "nội bộ — không có UI. Route api/FBDataWebhook1st."
muc_dich: "Controller scaffold cho entity FBDataWebhook1st (log webhook 'chỉ lưu, chờ job xử lý'). HIỆN KHÔNG CÓ ENDPOINT NÀO HOẠT ĐỘNG — mọi action bị comment."
alias: ["FBDataWebhook1st", "log webhook FB lưu trước"]
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

# FBDataWebhook1stController — Log webhook FB lưu-trước (scaffold, chưa có endpoint)

> Route `api/FBDataWebhook1st`. Giống `FBDataWebhookController`: inject service nhưng **mọi action comment**.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có endpoint active) | — | — | — | — | — | Controller rỗng về API; `IFBDataWebhook1stService` dùng thật ở `WebhookController` (nhánh `saveDataOnly`) và `FacebookWebhooksController` deps. | EF (FBDataWebhook1st) | Toàn bộ CRUD comment (`FBDataWebhook1stController.cs:26-111`). |

## Quan hệ
- **Dữ liệu (entities):** `FBDataWebhook1st` — log webhook "chỉ lưu, chờ job" (`SaveWebhook`).

## Lỗi đã biết / lưu ý kỹ thuật
- Scaffold thuần; chưa có API tra cứu.
- Trùng tên với bản FbChatWebhook → node suffix `_FbWebhook`.
