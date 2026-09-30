---
controller: HotlineController
module: sales
service: Switchboard
man_hinh_ui: "nội bộ — không có UI (bản MyHub v1 ở folder Controllers, toàn bộ endpoint comment)"
muc_dich: "Bản HotlineController v1 (folder Controllers, route api/Hotline, TI_Switchboard) — TOÀN BỘ endpoint comment (GetHotline, GetHotlineByIdUserPartner). Rỗng; bản active ở SanboxArea (xem HotlineController.md) + MyHubHotlineController."
alias: ["Hotline MyHub v1", "hotline cũ"]
auth: internal
state_machine: []
deprecated: true
entities: [Hotline]
stored_procs: []
depends: [IHotlineService]
cross_module_in: []
cross_module_out: []
---

# Hotline (v1, Controllers) — Hotline tổng đài MyHub v1 (deprecated)

> ⚠️ Trùng tên `HotlineController`. Node này = bản v1 trong `Controllers\HotlineController.cs` (rỗng) → hậu tố `_Switchboard`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | (không còn endpoint hoạt động) | — | — | — | internal | — | DI + `GetHotline`, `GetHotlineByIdUserPartner` đều comment. | — | Dùng SanboxArea HotlineController / MyHubHotlineController thay thế |

## Lỗi đã biết / lưu ý kỹ thuật
- `Controllers\HotlineController.cs:14-41` — toàn bộ comment → rỗng, deprecated.
