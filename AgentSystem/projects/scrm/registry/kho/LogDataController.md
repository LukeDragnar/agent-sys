---
controller: LogDataController
module: kho
service: OrderLogistic
man_hinh_ui: "nội bộ — không có UI (màn tra cứu log kỹ thuật / ghi log từ service)"
muc_dich: "Lưu và tra cứu bản ghi log kỹ thuật (LogData) của service OrderLogistic: tìm log theo điều kiện và insert 1 bản ghi log."
alias: ["log data", "LogData", "ghi log kỹ thuật", "tra cứu log OrderLogistic"]
auth: mixed
state_machine: []
deprecated: false
entities: [LogData]
stored_procs: []
depends:
  - "ILogDataService (LogDataService.cs — TimTheoDieuKien + InsertAsync)"
cross_module_in: []
cross_module_out: []
---

# LogDataController — Lưu / tra cứu log kỹ thuật

> Controller `partial` (1 file), kế thừa BaseController. Không gắn `[TienIch]`. Tra cứu log mở (`TimTheoDieuKien` không có `[SBAuthorize]` → chỉ qua filter mặc định), insert có `[SBAuthorize]` rỗng.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm log theo điều kiện | "Tìm kiếm" (màn log) | POST `api/LogData/TimTheoDieuKien` | (không attribute) | mixed | - | List bản ghi `LogData` theo bộ lọc | EF | Endpoint KHÔNG gắn `[SBAuthorize]` — cần xác nhận mức bảo vệ |
| 2 | Insert 1 bản ghi log | (service ghi log) | POST `api/LogData/InsertAsync` | `[SBAuthorize]` (mở) | public | - | Lưu 1 bản ghi `LogData` | EF | Quyền rỗng |

## Quan hệ
- **Gọi sang:** `ILogDataService`.
- **Dữ liệu (entities):** `LogData`.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- `TimTheoDieuKien` không có attribute SBAuthorize (LogDataController.cs:28) → khác hẳn convention các controller khác; cần xác nhận có chủ ý.
- Controller tiện ích ghi/đọc log, không có nghiệp vụ phức tạp.
