---
controller: CommonController
module: marketing
service: FacebookChatWebhook
man_hinh_ui: "nội bộ — không có UI riêng (cấu hình chung service FacebookChatWebhook). Route api/Common."
muc_dich: "Lấy cấu hình dùng chung cho service FacebookChatWebhook: GetConfig (theo mã) và GetConfigFacebook (config app Facebook theo domain/đơn vị)."
alias: ["Common FB Chat Webhook", "GetConfig chat", "GetConfigFacebook chat"]
auth: mixed
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends:
  - "ICommonService.GetConfig / GetConfigFacebook"
cross_module_in: []
cross_module_out: []
---

# CommonController — Cấu hình chung service FacebookChatWebhook

> Route `api/Common`. Controller-level `[SBAuthorize()]`; `GetConfigFacebook` override `[AllowAnonymous]`. Mã giống bản FacebookWebhook.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy config theo mã | (nội bộ) | GET `api/Common/GetConfig?ma=` | `[SBAuthorize()]` (login) | public | Đã đăng nhập | Trả `ConfigSystemDto` theo mã. | EF (ConfigSystem) | — |
| 2 | Lấy config app Facebook | (nội bộ) | GET `api/Common/GetConfigFacebook` | — | anonymous | — | Trả `ConfigSystemFacebookDto` (appid theo domain→đơn vị), `isClientCall=false`. | EF | **Anonymous trả config app Facebook** (`CommonController.cs:38-39`) → cân nhắc siết quyền. |

## Quan hệ
- **Gọi sang:** `ICommonService`.
- **Dữ liệu (entities):** `ConfigSystem`.

## Lỗi đã biết / lưu ý kỹ thuật
- `GetConfigFacebook` `[AllowAnonymous]` trả cấu hình app FB không cần login.
- Trùng tên với các service khác + bản FbWebhook → node suffix `_FbChatWebhook`.
