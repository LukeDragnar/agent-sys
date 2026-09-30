---
controller: MyHubController
module: sales
service: Switchboard
man_hinh_ui: "nội bộ — không có UI (bản MyHub v1 ở folder Controllers, toàn bộ endpoint comment)"
muc_dich: "Bản MyHub v1 (folder Controllers, TI_Switchboard) — TOÀN BỘ endpoint (ReceiveDataHotline, ReceiveData, ClickToCall) đã comment. Controller rỗng; bản ACTIVE là MyHubController ở folder Controllers_MyHub (xem MyHubController.md)."
alias: ["MyHub v1", "tổng đài MyHub cũ"]
auth: internal
state_machine: []
deprecated: true
entities: [DataReceived]
stored_procs: []
depends: [IMyHubService, ISwitchboardService]
cross_module_in: []
cross_module_out: []
---

# MyHub (v1, Controllers) — Tổng đài MyHub v1 (deprecated)

> ⚠️ Trùng tên `MyHubController` với bản active ở `Controllers_MyHub`. Node này = bản v1 trong `Controllers\MyHubController.cs` (rỗng). Đặt hậu tố `_Switchboard` để tránh đè node bản active.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | (không còn endpoint hoạt động) | — | — | — | internal | — | DI cũng comment; tất cả action (`ReceiveDataHotline`, `ReceiveData`, `ClickToCall`) comment. | — | Dùng bản `Controllers_MyHub\MyHubController` thay thế |

## Lỗi đã biết / lưu ý kỹ thuật
- `Controllers\MyHubController.cs:16-58` — constructor + 3 endpoint đều comment → rỗng, deprecated. Giữ node để map tránh nhầm với bản active.
