---
controller: ConfigSystemController
module: marketing
service: Facebook
man_hinh_ui: "nội bộ — không có UI. Route api/ConfigSystem (Controllers, service Facebook). KHÁC bản Area QuanTri."
muc_dich: "Controller ConfigSystem ở thư mục Controllers của Facebook.API — HIỆN RỖNG: chỉ inject IConfigSystemService, KHÔNG có endpoint nào. Bản có endpoint thật là ConfigSystemController trong Area SanboxArea (route api/QuanTriArea/ConfigSystem) — node _Facebook_Area."
alias: ["ConfigSystem Facebook", "config rỗng FB"]
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

# ConfigSystemController — ConfigSystem Facebook (Controllers, RỖNG)

> Route `api/ConfigSystem`. **Không có action nào** — chỉ ctor inject `IConfigSystemService`. Đừng nhầm với bản Area QuanTri (node `ConfigSystemController_Facebook_Area.md`).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (rỗng) | — | — | — | — | — | Không expose endpoint. | — | Controller rỗng (`ConfigSystemController.cs:9-17`) — chỉ placeholder. |

## Lỗi đã biết / lưu ý kỹ thuật
- Controller placeholder; nghiệp vụ ConfigSystem thật nằm ở `Areas/SanboxArea/ConfigSystemController` (route `api/QuanTriArea/ConfigSystem`).
- Trùng tên với bản Area cùng service (node `_Facebook_Area`) và các bản ConfigSystem service khác (SMS, FbWebhook) → node suffix `_Facebook`.
