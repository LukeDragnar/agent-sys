---
controller: GroupController
module: sales
service: Switchboard
man_hinh_ui: "nội bộ — không có UI (bản MyHub v1 ở folder Controllers, toàn bộ endpoint comment)"
muc_dich: "Bản GroupController v1 (folder Controllers, route api/Group, TI_Switchboard) — TOÀN BỘ endpoint comment (GetTongDaiActive, TimTheoDieuKien, ChangeActiveTongDai). Rỗng; bản active là Areas\\SandBoxArea\\GroupController (xem GroupController.md)."
alias: ["Group MyHub v1", "tổng đài group cũ"]
auth: internal
state_machine: []
deprecated: true
entities: [Group]
stored_procs: []
depends: [IGroupService]
cross_module_in: []
cross_module_out: []
---

# Group (v1, Controllers) — Tổng đài đơn vị MyHub v1 (deprecated)

> ⚠️ Trùng tên `GroupController`. Node này = bản v1 trong `Controllers\GroupController.cs` (rỗng) → hậu tố `_Switchboard`. Bản active ở SanboxArea xem `GroupController.md`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | (không còn endpoint hoạt động) | — | — | — | internal | — | DI + 3 action (`GetTongDaiActive`, `TimTheoDieuKien`, `ChangeActiveTongDai`) đều comment. | — | Dùng SanboxArea GroupController hoặc MyHubGroupController thay thế |

## Lỗi đã biết / lưu ý kỹ thuật
- `Controllers\GroupController.cs:15-50` — toàn bộ comment → rỗng, deprecated.
