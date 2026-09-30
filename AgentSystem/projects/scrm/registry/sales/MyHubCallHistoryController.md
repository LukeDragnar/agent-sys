---
controller: MyHubCallHistoryController
module: sales
service: Switchboard
man_hinh_ui: "Tổng đài MyHub (sim số) > Lịch sử cuộc gọi (theo khách hàng, tìm theo điều kiện)"
muc_dich: "Tra cứu lịch sử cuộc gọi tổng đài MyHub (sim số): lấy lịch sử cuộc gọi theo khách hàng và tìm lịch sử theo điều kiện (auto-scope theo chi nhánh nếu không phải admin)."
alias: ["lịch sử cuộc gọi MyHub", "call history sim số", "MyHub call history"]
auth: public
state_machine: []
deprecated: false
entities: [CallHistory]
stored_procs: []
depends: [ICallHistoryService]
cross_module_in: []
cross_module_out: []
---

# MyHubCallHistory — Lịch sử cuộc gọi tổng đài MyHub (sim số)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy lịch sử cuộc gọi theo khách hàng | (popup KH) | POST `GetHistoryByCustomer` | `MyHubCallHistory_GetHistoryByCustomer` (nhiều role DV) | public | mở chi tiết KH | ModelState invalid → trả lỗi; hợp lệ → trả lịch sử cuộc gọi theo khách hàng. | EF | — |
| 2 | Tìm lịch sử cuộc gọi theo điều kiện | Tìm | POST `TimTheoDieuKien` | `MyHubCallHistory_TimTheoDieuKien` (DV-AD-TONG, DV-AD-CN, DV-AD-MCN) | public | màn lịch sử cuộc gọi | Nếu **không phải admin** → ép `IdChiNhanh = CurrentUser.IdChiNhanh` (scope chi nhánh); trả danh sách theo điều kiện. | EF | — |

## Quan hệ
- **Gọi sang:** `ICallHistoryService` (đọc lịch sử cuộc gọi).
- **Dữ liệu (entities):** bản ghi lịch sử cuộc gọi MyHub.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- `MyHubCallHistoryController.cs:47-72` — 2 endpoint `GetCallHistoryWithConditions` và `..._Manager` đã bị comment → không còn nghe ghi âm/quản lý qua controller này.
- `TimTheoDieuKien` (`:34-44`) auto-scope chi nhánh khi `IsAdmin == false` → user thường chỉ thấy cuộc gọi chi nhánh mình.
