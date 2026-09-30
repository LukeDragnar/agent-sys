---
controller: ServerController
module: marketing
service: Facebook
man_hinh_ui: "nội bộ — không có UI. Route api/Server (Controllers, service Facebook). TOÀN BỘ ENDPOINT BỊ COMMENT."
muc_dich: "Controller Server ở thư mục Controllers của Facebook.API — RỖNG về endpoint: ctor inject IServerService nhưng toàn bộ 5 action (TimTheoId/ThemMoi/CapNhat/Xoa/TimTheoDieuKien) bị comment cứng. Bản có endpoint thật là ServerController trong Area SanboxArea (route api/QuanTriArea/Server) — node _Facebook_Area."
alias: ["Server Facebook", "server webhook FB rỗng"]
auth: public
state_machine: []
deprecated: true
entities: [Server]
stored_procs: []
depends:
  - "IServerService (inject, không dùng — action bị comment)"
cross_module_in: []
cross_module_out: []
---

# ServerController — Server Facebook (Controllers, COMMENTED OUT)

> Route `api/Server`. Gate `[TienIch("ServerController","Quản lý danh mục", TI_Facebook)]`. **Toàn bộ action bị comment** — không expose endpoint. Bản dùng được nằm ở Area QuanTri (node `ServerController_Facebook_Area.md`).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (rỗng) | — | — | — | — | — | Không expose endpoint (tất cả comment). | — | TimTheoId/ThemMoi/CapNhat/Xoa/TimTheoDieuKien đều bị comment (`ServerController.cs:26-111`). |

## Lỗi đã biết / lưu ý kỹ thuật
- `ServerController.cs:26-111` — 5 action CRUD comment toàn bộ; chỉ còn ctor inject `IServerService`.
- `deprecated: true` — bản này chết, dùng `Areas/SanboxArea/ServerController` (route `api/QuanTriArea/Server`).
