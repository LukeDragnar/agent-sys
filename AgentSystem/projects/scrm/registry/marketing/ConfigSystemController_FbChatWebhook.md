---
controller: ConfigSystemController
module: marketing
service: FacebookChatWebhook
man_hinh_ui: "nội bộ — không có UI. Route api/ConfigSystem (Controllers, KHÁC bản Area QuanTri)."
muc_dich: "Controller ConfigSystem ở thư mục Controllers của FacebookChatWebhookProcess.API — RỖNG: chỉ inject IConfigSystemService, không endpoint. Bản có endpoint thật là ConfigSystemController trong Area SanboxArea (api/QuanTriArea/ConfigSystem)."
alias: ["ConfigSystem FB Chat Webhook", "config rỗng chat"]
auth: public
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends:
  - "IConfigSystemService (inject, không dùng)"
cross_module_in: []
cross_module_out: []
---

# ConfigSystemController — ConfigSystem (Controllers, RỖNG)

> Route `api/ConfigSystem`. **Không có action** — chỉ ctor inject service. Bản thật ở Area QuanTri (node `ConfigSystemController_FbChatWebhook_Area.md`).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (rỗng) | — | — | — | — | — | Không expose endpoint. | — | Controller rỗng (`ConfigSystemController.cs:14-24`). |

## Lỗi đã biết / lưu ý kỹ thuật
- Placeholder; nghiệp vụ thật ở `Areas/SanboxArea/ConfigSystemController` (`api/QuanTriArea/ConfigSystem`).
- Trùng tên nhiều phía → node suffix `_FbChatWebhook`.
