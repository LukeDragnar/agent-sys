---
controller: ServerController
module: marketing
service: FacebookChat
man_hinh_ui: "nội bộ — không có UI (toàn bộ action CRUD danh mục Server đã bị comment; chỉ còn scaffold + DI)"
muc_dich: "Controller scaffold CRUD danh mục Server (máy chủ/cấu hình) trong FacebookChat. HIỆN TẠI rỗng: tất cả action (TimTheoId/ThemMoi/CapNhatThongTin/Xoa/TimTheoDieuKien) đã comment, không expose endpoint nào. Chỉ inject IServerService."
alias: ["server FacebookChat", "danh mục server", "ServerController chat"]
auth: internal
state_machine: []
deprecated: true
entities: [Server]
stored_procs: []
depends:
  - "IServerService (FacebookChat.Services.Generated) — inject nhưng không action nào dùng"
cross_module_in: []
cross_module_out: []
---

# ServerController — Danh mục Server (FacebookChat) — SCAFFOLD (deprecated)

> Phân biệt: trùng tên với `ServerController_FbWebhook` / `ServerController_FbChatWebhook` (các service webhook khác). Bản này là service **FacebookChat** (FacebookChat.API). Toàn bộ action bị comment → không có endpoint hoạt động.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có) | — | — | — | internal | — | Tất cả action đã comment, controller chỉ còn ctor + `[TienIch]`. Không gọi được endpoint nào. | — | Scaffold chết — các action TimTheoId/ThemMoi/CapNhat/Xoa/TimTheoDieuKien comment toàn bộ (`ServerController.cs:26-112`). |

## Lỗi đã biết / lưu ý kỹ thuật
- `ServerController.cs:26-112` — toàn bộ method CRUD comment; `[TienIch("ServerController","Quản lý danh mục", TI_ChatHub)]` còn nhưng vô nghĩa vì không endpoint nào active.
- `IServerService` được inject nhưng không sử dụng.
