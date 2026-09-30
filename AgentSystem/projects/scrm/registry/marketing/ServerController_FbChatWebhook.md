---
controller: ServerController
module: marketing
service: FacebookChatWebhook
man_hinh_ui: "nội bộ — không có UI. Route api/Server (Controllers, KHÁC bản Area QuanTri)."
muc_dich: "Controller Server ở thư mục Controllers của service chat — scaffold, MỌI ACTION COMMENT. Bản thật là ServerController trong Area SanboxArea (api/QuanTriArea/Server)."
alias: ["Server FB Chat Webhook", "server webhook scaffold chat"]
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

> Route `api/Server`. Mọi CRUD comment. Bản thật ở Area QuanTri (node `ServerController_FbChatWebhook_Area.md`).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có endpoint active) | — | — | — | — | — | Không expose endpoint. | EF (Server) | Toàn bộ CRUD comment (`ServerController.cs:20-105`). |

## Quan hệ
- **Dữ liệu (entities):** `Server`.

## Lỗi đã biết / lưu ý kỹ thuật
- Scaffold; nghiệp vụ thật ở `Areas/SanboxArea/ServerController`.
- Trùng tên nhiều phía → node suffix `_FbChatWebhook`.
