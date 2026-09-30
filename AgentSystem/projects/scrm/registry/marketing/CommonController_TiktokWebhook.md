---
controller: CommonController
module: marketing
service: TiktokWebhook
man_hinh_ui: "nội bộ — không có UI. Controller khung (chưa có action)."
muc_dich: "Controller Common cho TiktokWebhookProcess — hiện chỉ inject ICommonService, KHÔNG có endpoint nào (khung trống chờ bổ sung)."
alias: ["CommonController Tiktok", "Common TiktokWebhook"]
auth: internal
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends:
  - "ICommonService (TiktokWebhookebhook.Services.Generated.Common)"
cross_module_in: []
cross_module_out: []
---

# CommonController (TiktokWebhookProcess) — Khung trống (chưa có action)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có action) | — | `api/Common/*` (chưa endpoint nào) | — | **internal** (`[SBAuthorize()]` controller-level) | — | Controller chỉ có ctor inject `ICommonService`; không khai báo method action nào. | — | Khung trống — chưa implement. |

## Quan hệ
- **Gọi sang:** `ICommonService` (inject sẵn, chưa dùng).
- **Dữ liệu (entities):** không có.
- **Ripple / đồng bộ:** không có.

## Lỗi đã biết / lưu ý kỹ thuật
- `Common\CommonController.cs:11-19` — controller có `[SBAuthorize()]` + inject `ICommonService` nhưng **0 action** → node placeholder; cập nhật khi có endpoint.
- Namespace service `TiktokWebhookebhook.Services.Generated.Common` có typo ("ebhook") — lưu ý khi search.
