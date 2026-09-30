---
controller: CommonController
module: marketing
service: FacebookWebhook
man_hinh_ui: "nội bộ — không có UI riêng (cấu hình chung cho service FacebookWebhook). Route api/Common."
muc_dich: "Lấy cấu hình dùng chung cho service FacebookWebhook: GetConfig (config theo mã) và GetConfigFacebook (config app Facebook theo domain/đơn vị)."
alias: ["Common FB Webhook", "GetConfig", "GetConfigFacebook", "config app facebook"]
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

# CommonController — Cấu hình chung service FacebookWebhook

> Route `api/Common`. Controller-level `[SBAuthorize()]` nhưng `GetConfigFacebook` override bằng `[AllowAnonymous]`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy config theo mã | (nội bộ) | GET `api/Common/GetConfig?ma=` | `[SBAuthorize()]` (login bất kỳ) | public | Đã đăng nhập | Trả `ConfigSystemDto` theo mã. | EF (ConfigSystem) | — |
| 2 | Lấy config app Facebook | (nội bộ) | GET `api/Common/GetConfigFacebook` | — | anonymous | — | Trả `ConfigSystemFacebookDto` (appid/secret app FB theo domain→đơn vị), gọi với `isClientCall=false`. | EF (ConfigSystem) | **Anonymous trả config app Facebook** (có thể chứa appid/cấu hình nhạy cảm) → cân nhắc siết quyền (`CommonController.cs:38-39`). TODO trong code: map domain→đơn vị để lấy appid riêng. |

## Quan hệ
- **Gọi sang:** `ICommonService.GetConfig` / `GetConfigFacebook`.
- **Dữ liệu (entities):** `ConfigSystem` (cặp mã-giá trị + config Facebook).

## Lỗi đã biết / lưu ý kỹ thuật
- `CommonController.cs:38-46` — `GetConfigFacebook` `[AllowAnonymous]` trả cấu hình app Facebook không cần đăng nhập; review xem có rò appid/secret không.
- Trùng tên với `CommonController` các service khác (SMS/MarketingCampaign + bản FbChatWebhook) → node suffix `_FbWebhook`.
