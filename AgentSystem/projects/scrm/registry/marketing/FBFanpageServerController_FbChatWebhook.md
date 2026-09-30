---
controller: FBFanpageServerController
module: marketing
service: FacebookChatWebhook
man_hinh_ui: "nội bộ + có gọi từ client CRM. Route api/FBFanpageServer."
muc_dich: "Quản lý ánh xạ Fanpage ↔ Server webhook cho service chat: AddFanpageServer (anonymous) + TimTheoDieuKienAsync (login)."
alias: ["FBFanpageServer chat", "fanpage server chat", "AddFanpageServer chat"]
auth: mixed
state_machine: []
deprecated: false
entities: [FBFanpageServer]
stored_procs: []
depends:
  - "IFBFanpageServerService.AddFanpageServer / TimTheoDieuKienAsync"
  - "ICommonService, IConfiguration (inject, không dùng)"
cross_module_in:
  - "Client CRM — TimTheoDieuKienAsync (isClientCall=true) liệt kê fanpage của đơn vị"
cross_module_out: []
---

# FBFanpageServerController — Ánh xạ Fanpage ↔ Server webhook (chat)

> Route `api/FBFanpageServer`. AddFanpageServer anonymous, TimTheoDieuKien login. Mã giống bản FacebookWebhook.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm fanpage gắn server | (đăng ký) | POST `api/FBFanpageServer/AddFanpageServer` | — | anonymous | — | Lưu ánh xạ fanpage→server webhook (`FBFanpageServerAddDto`). | EF (FBFanpageServer) | **Anonymous ghi** ánh xạ fanpage→server (`cs:36-38`). |
| 2 | Tìm fanpage theo điều kiện | (grid) | POST `api/FBFanpageServer/TimTheoDieuKienAsync` | `[SBAuthorize()]` (login) | public | Đã đăng nhập | Filter theo `FBFanpageServerFilterDto`, `CurrentUser` + `isClientCall=true`. | EF | — |

## Quan hệ
- **Gọi sang:** `IFBFanpageServerService`.
- **Dữ liệu (entities):** `FBFanpageServer` — map pageId ↔ ServerCode/đơn vị.
- **Ripple:** `FacebookWebhooksController` đọc qua `GetByPageId` (nhánh route-by-config, hiện tắt).

## Lỗi đã biết / lưu ý kỹ thuật
- `FBFanpageServerController.cs:36-42` — `AddFanpageServer` anonymous ghi không xác thực.
- `_commonService`, `_configuration` inject không dùng.
- Trùng tên với bản FbWebhook → node suffix `_FbChatWebhook`.
