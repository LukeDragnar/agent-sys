---
controller: RechargeHistoryController
module: marketing
service: SMS
man_hinh_ui: "Marketing/Tài khoản SMS > Lịch sử nạp (bảng lịch sử nạp tiền của tài khoản SMS) — thường nhúng trong màn chi tiết tài khoản."
muc_dich: "Tra cứu lịch sử nạp tiền (RechargeHistory) của tài khoản SMS theo điều kiện lọc."
alias: ["RechargeHistory", "lịch sử nạp", "lịch sử nạp tiền tài khoản sms"]
auth: anonymous
state_machine: []
deprecated: false
entities: [RechargeHistory]
stored_procs: []
depends:
  - "IRechargeHistoryService (SMS.Services.Generated)"
cross_module_in: []
cross_module_out: []
---

# RechargeHistoryController — Lịch sử nạp tiền tài khoản SMS

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lịch sử nạp | Bảng lịch sử | POST `api/RechargeHistory/TimTheoDieuKien` | (không) | anonymous | — | Lọc danh sách `RechargeHistory` theo filter. | EF | KHÔNG có `[SBAuthorize]` → endpoint mở (`cs:26`). |

## Quan hệ
- **Gọi sang:** `IRechargeHistoryService`.
- **Dữ liệu (entities):** `RechargeHistory` (log nạp tiền tài khoản SMS).
- **Ripple / đồng bộ:** đọc dữ liệu sinh ra bởi luồng nạp tiền (AccountSms.DepositMoneyIntoAccount / duyệt SMSLichSuNapTien).

## Lỗi đã biết / lưu ý kỹ thuật
- Endpoint không có attribute auth nào (`RechargeHistoryController.cs:26-31`) → ai cũng query được lịch sử nạp; cần rà phân quyền.
- Trùng dữ liệu với `AccountSms.RechargeHistoryGetByAccountSmsId` (bản theo accountSmsId, có quyền).
