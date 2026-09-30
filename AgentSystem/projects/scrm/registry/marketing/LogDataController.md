---
controller: LogDataController
module: marketing
service: SMS
man_hinh_ui: "nội bộ — không có UI. Controller scaffold, hiện KHÔNG có endpoint active."
muc_dich: "Scaffold cho tra cứu log dữ liệu SMS (LogData). Hiện toàn bộ endpoint + `[TienIch]` bị comment → controller RỖNG, không expose API."
alias: ["LogData", "log dữ liệu sms"]
auth: internal
state_machine: []
deprecated: true
entities: [LogData]
stored_procs: []
depends:
  - "ILogDataService (SMS.Services.Generated)"
cross_module_in: []
cross_module_out: []
---

# LogDataController — Log dữ liệu SMS (scaffold rỗng)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có endpoint active) | — | — | — | — | — | Controller chỉ inject service, không expose method nào. | — | `TimTheoDieuKien` (key `LogData_TimTheoDieuKien`) đã bị comment (`cs:29-35`); `[TienIch]` cũng comment (`cs:10`). |

## Quan hệ
- **Gọi sang:** `ILogDataService` (inject nhưng chưa dùng).
- **Dữ liệu (entities):** `LogData` (log dữ liệu SMS — chưa có CRUD).

## Lỗi đã biết / lưu ý kỹ thuật
- **DEAD/scaffold:** không có endpoint active — method + attribute bị comment (`LogDataController.cs:10,29-35`). `deprecated: true`.
