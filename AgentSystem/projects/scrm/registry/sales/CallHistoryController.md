---
controller: CallHistoryController
module: sales
service: Switchboard
man_hinh_ui: "nội bộ — không có UI (toàn bộ endpoint đã bị comment; bản MyHub v1 cũ)"
muc_dich: "Bản cũ quản lý lịch sử cuộc gọi tổng đài MyHub v1 — TOÀN BỘ endpoint đã bị comment, controller hiện rỗng (không expose API nào)."
alias: ["call history MyHub v1", "lịch sử cuộc gọi MyHub cũ"]
auth: internal
state_machine: []
deprecated: true
entities: [CallHistory]
stored_procs: []
depends: [ICallHistoryService]
cross_module_in: []
cross_module_out: []
---

# CallHistory — Lịch sử cuộc gọi tổng đài MyHub v1 (deprecated)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | (không còn endpoint hoạt động) | — | — | — | internal | — | Controller chỉ còn class-level `[SBAuthorize]`; tất cả action đã comment. | — | Đã thay bằng `MyHubCallHistoryController` (MyHub) + `TongDaiLichSuCuocGoiController` (tổng đài IP) |

## Lỗi đã biết / lưu ý kỹ thuật
- `CallHistoryController.cs:19-58` — 3 endpoint (`GetHistoryByCustomer`, `TimTheoDieuKien`, `GetCallHistoryWithConditions`) đều bị comment toàn bộ → controller rỗng, deprecated. Giữ node để map alias "MyHub v1" tránh nhầm với bản mới.
