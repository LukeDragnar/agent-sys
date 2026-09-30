---
controller: FBDataWebhookController
module: marketing
service: FacebookWebhook
man_hinh_ui: "nội bộ — không có UI. Route api/FBDataWebhook."
muc_dich: "Controller scaffold cho entity FBDataWebhook (log webhook đã lưu+xử lý). HIỆN KHÔNG CÓ ENDPOINT NÀO HOẠT ĐỘNG — toàn bộ action (TimTheoId/ThemMoi/CapNhat/Xoa/TimTheoDieuKien) đang bị comment."
alias: ["FBDataWebhook", "log webhook FB"]
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

# FBDataWebhookController — Log webhook FB (scaffold, chưa có endpoint)

> Route `api/FBDataWebhook`. Inject `IFBDataWebhookService` nhưng **mọi action đều comment** → không expose endpoint runtime nào.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có endpoint active) | — | — | — | — | — | Controller rỗng về API; service `IFBDataWebhookService` được dùng thật ở `WebhookController.ReceiveData` (lưu+xử lý webhook). | EF (FBDataWebhook) | Toàn bộ CRUD comment (`FBDataWebhookController.cs:26-111`). |

## Quan hệ
- **Dữ liệu (entities):** `FBDataWebhook` — log webhook đã lưu+xử lý (ghi bởi `WebhookController` qua service).

## Lỗi đã biết / lưu ý kỹ thuật
- Controller chỉ là scaffold; nếu cần tra cứu/xóa log webhook thì phải bỏ comment + gắn `[SBAuthorize]`.
- Trùng tên với bản FbChatWebhook → node suffix `_FbWebhook`.
