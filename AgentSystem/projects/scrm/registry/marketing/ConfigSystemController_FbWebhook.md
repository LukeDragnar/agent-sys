---
controller: ConfigSystemController
module: marketing
service: FacebookWebhook
man_hinh_ui: "nội bộ — không có UI. Route api/ConfigSystem (Controllers, KHÁC bản Area QuanTri)."
muc_dich: "Controller ConfigSystem ở thư mục Controllers của FacebookWebhookProcess.API — HIỆN RỖNG: chỉ inject IConfigSystemService, KHÔNG có endpoint nào. Bản có endpoint thật là ConfigSystemController trong Area SanboxArea (route api/QuanTriArea/ConfigSystem)."
alias: ["ConfigSystem FB Webhook", "config rỗng"]
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

> Route `api/ConfigSystem`. **Không có action nào** — chỉ ctor inject `IConfigSystemService`. Đừng nhầm với bản Area QuanTri (node `ConfigSystemController_FbWebhook_Area.md`).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (rỗng) | — | — | — | — | — | Không expose endpoint. | — | Controller rỗng (`ConfigSystemController.cs:14-24`) — chỉ placeholder. |

## Lỗi đã biết / lưu ý kỹ thuật
- Controller placeholder; nghiệp vụ ConfigSystem thật nằm ở `Areas/SanboxArea/ConfigSystemController` (route `api/QuanTriArea/ConfigSystem`).
- Trùng tên 3 phía: bản Area cùng service, bản SMS, bản FbChatWebhook → node suffix `_FbWebhook`.
