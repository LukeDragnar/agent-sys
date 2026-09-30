---
controller: SwitchboardController
module: sales
service: Switchboard
man_hinh_ui: "nội bộ — không có UI (toàn bộ endpoint đã comment)"
muc_dich: "Controller gốc đăng ký tổng đài/user (RegisterSwitchboard, RegisterUser) — TOÀN BỘ endpoint đã bị comment, hiện chỉ còn class-level [SBAuthorize], không expose API nào."
alias: ["switchboard", "đăng ký tổng đài"]
auth: internal
state_machine: []
deprecated: true
entities: [Switchboard]
stored_procs: []
depends: [ISwitchboardService]
cross_module_in: []
cross_module_out: []
---

# Switchboard — Đăng ký tổng đài/user (deprecated)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | (không còn endpoint hoạt động) | — | — | `[SBAuthorize()]` (class-level) | internal | — | Class còn DI `ISwitchboardService` nhưng `RegisterSwitchboard` + `RegisterUser` đã comment. | — | TienIch attribute cũng đã comment → controller rỗng |

## Lỗi đã biết / lưu ý kỹ thuật
- `SwitchboardController.cs:9,18-44` — `[TienIch...]`, `RegisterSwitchboard`, `RegisterUser` đều comment → deprecated. Đăng ký tổng đài hiện đi qua `GroupController` (SanboxArea) / `TongDaiIpDonViController` / `TongDaiDonViController`.
