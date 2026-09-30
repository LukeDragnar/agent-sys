---
controller: UserSwitchboardController
module: sales
service: Switchboard
man_hinh_ui: "nội bộ — không có UI (bản MyHub v1 ở folder Controllers, toàn bộ endpoint comment)"
muc_dich: "Bản UserSwitchboardController v1 (folder Controllers, route api/UserSwitchboard, TI_Switchboard) — endpoint TimTheoDieuKien comment, DI comment. Rỗng; bản active ở SanboxArea (xem UserSwitchboardController.md) + MyHubUserSwitchboardController."
alias: ["UserSwitchboard MyHub v1", "user tổng đài cũ"]
auth: internal
state_machine: []
deprecated: true
entities: [UserSwitchboard]
stored_procs: []
depends: [IUserSwitchboardService]
cross_module_in: []
cross_module_out: []
---

# UserSwitchboard (v1, Controllers) — User tổng đài MyHub v1 (deprecated)

> ⚠️ Trùng tên `UserSwitchboardController`. Node này = bản v1 trong `Controllers\UserSwitchboardController.cs` (rỗng) → hậu tố `_Switchboard`. Class-level `[SBAuthorize]` còn nhưng không có action.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | (không còn endpoint hoạt động) | — | — | `[SBAuthorize]` (class-level) | internal | — | DI + `TimTheoDieuKien` đều comment. | — | Dùng SanboxArea UserSwitchboardController / MyHubUserSwitchboardController thay thế |

## Lỗi đã biết / lưu ý kỹ thuật
- `Controllers\UserSwitchboardController.cs:14-37` — toàn bộ comment → rỗng, deprecated.
