---
controller: FBFanpageServerController
module: marketing
service: FacebookWebhook
man_hinh_ui: "nội bộ + có gọi từ client CRM. Route api/FBFanpageServer."
muc_dich: "Quản lý ánh xạ Fanpage ↔ Server webhook: thêm fanpage gắn server (AddFanpageServer, anonymous) và tìm fanpage theo điều kiện cho client CRM (TimTheoDieuKienAsync, có login)."
alias: ["FBFanpageServer", "fanpage server", "gắn fanpage vào server webhook", "AddFanpageServer"]
auth: mixed
state_machine: []
deprecated: false
entities: [FBFanpageServer]
stored_procs: []
depends:
  - "IFBFanpageServerService.AddFanpageServer / TimTheoDieuKienAsync"
  - "ICommonService, IConfiguration (inject, không dùng trong file)"
cross_module_in:
  - "Client CRM — gọi TimTheoDieuKienAsync (isClientCall=true) để liệt kê fanpage của đơn vị"
cross_module_out: []
---

# FBFanpageServerController — Ánh xạ Fanpage ↔ Server webhook

> Route `api/FBFanpageServer`. Mix: AddFanpageServer anonymous, TimTheoDieuKien yêu cầu login.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm fanpage gắn server | (nội bộ/đăng ký) | POST `api/FBFanpageServer/AddFanpageServer` | — | anonymous | — | Nhận `FBFanpageServerAddDto`, lưu ánh xạ fanpage→server webhook. | EF (FBFanpageServer) | **Anonymous ghi dữ liệu**: cho phép tạo ánh xạ fanpage→server không cần auth → rủi ro chèn fanpage giả/đổi routing (`FBFanpageServerController.cs:36-38`). |
| 2 | Tìm fanpage theo điều kiện | (grid) | POST `api/FBFanpageServer/TimTheoDieuKienAsync` | `[SBAuthorize()]` (login bất kỳ) | public | Đã đăng nhập | Filter fanpage theo `FBFanpageServerFilterDto`, truyền `CurrentUser` + `isClientCall=true` (giới hạn theo đơn vị user). | EF | — |

## Quan hệ
- **Gọi sang:** `IFBFanpageServerService`.
- **Dữ liệu (entities):** `FBFanpageServer` — bảng map fanpage (pageId) ↔ ServerCode/đơn vị, dùng để định tuyến webhook về đúng server/queue.
- **Ripple:** ánh xạ này được `FacebookWebhooksController` đọc (`GetByPageId`) trong nhánh route-by-config (hiện tắt).

## Lỗi đã biết / lưu ý kỹ thuật
- `FBFanpageServerController.cs:36-42` — `AddFanpageServer` `[AllowAnonymous]` cho phép ghi không xác thực; nên gắn quyền hoặc xác thực nguồn gọi.
- `_commonService`, `_configuration` inject nhưng không dùng → dead dependency.
- Trùng tên với bản FbChatWebhook → node suffix `_FbWebhook`.
