---
controller: FBDataWebhookController
module: marketing
service: Facebook
man_hinh_ui: "nội bộ — không có UI. Route api/FBDataWebhook (Controllers, service Facebook). TOÀN BỘ ENDPOINT BỊ COMMENT."
muc_dich: "Controller danh mục FBDataWebhook (log webhook FB) ở Facebook.API — RỖNG về endpoint: ctor inject IFBDataWebhookService nhưng toàn bộ 5 action CRUD bị comment cứng. Scaffold danh mục sinh tự động, chưa kích hoạt."
alias: ["FBDataWebhook danh mục", "log webhook FB rỗng"]
auth: public
state_machine: []
deprecated: true
entities: [FBDataWebhook]
stored_procs: []
depends:
  - "IFBDataWebhookService (inject, không dùng — action bị comment)"
cross_module_in: []
cross_module_out: []
---

# FBDataWebhookController — FBDataWebhook (Controllers, COMMENTED OUT)

> Route `api/FBDataWebhook`. Gate `[TienIch]` cũng bị comment. **Toàn bộ action CRUD bị comment** — không expose endpoint.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (rỗng) | — | — | — | — | — | Không expose endpoint (tất cả comment). | — | TimTheoId/ThemMoi/CapNhat/Xoa/TimTheoDieuKien đều bị comment (`FBDataWebhookController.cs:26-111`). |

## Lỗi đã biết / lưu ý kỹ thuật
- `FBDataWebhookController.cs:13,26-111` — `[TienIch]` + 5 action CRUD comment toàn bộ; chỉ còn ctor inject `IFBDataWebhookService` (service này được dùng thật ở `WebhookController`).
- `deprecated: true` — scaffold danh mục chưa kích hoạt; thao tác ghi `FBDataWebhook` xảy ra qua luồng webhook, không qua controller này.
