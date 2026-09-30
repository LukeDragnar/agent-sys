---
controller: ServerController
module: marketing
service: FacebookWebhook
man_hinh_ui: "nội bộ — không có UI. Route api/Server (Controllers, KHÁC bản Area QuanTri)."
muc_dich: "Controller Server ở thư mục Controllers — scaffold, MỌI ACTION COMMENT (không endpoint active). Bản có endpoint thật là ServerController trong Area SanboxArea (route api/QuanTriArea/Server)."
alias: ["Server FB Webhook", "server webhook scaffold"]
auth: public
state_machine: []
deprecated: false
entities: [Server]
stored_procs: []
depends:
  - "IServerService (inject, chưa expose endpoint)"
cross_module_in: []
cross_module_out: []
---

# ServerController — Server webhook (Controllers, scaffold)

> Route `api/Server`. Inject `IServerService` nhưng **mọi CRUD comment**. Bản thật ở Area QuanTri (node `ServerController_FbWebhook_Area.md`).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có endpoint active) | — | — | — | — | — | Không expose endpoint. | EF (Server) | Toàn bộ CRUD comment (`ServerController.cs:20-105`). |

## Quan hệ
- **Dữ liệu (entities):** `Server` (định nghĩa server/cụm RabbitMQ phục vụ webhook).

## Lỗi đã biết / lưu ý kỹ thuật
- Scaffold; nghiệp vụ Server thật nằm ở `Areas/SanboxArea/ServerController` (`api/QuanTriArea/Server`).
- Trùng tên 3 phía (Area cùng service + bản FbChatWebhook) → node suffix `_FbWebhook`.
