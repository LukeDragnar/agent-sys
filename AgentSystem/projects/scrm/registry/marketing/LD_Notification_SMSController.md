---
controller: LD_Notification_SMSController
module: marketing
service: SMS
man_hinh_ui: "nội bộ — không có UI. Controller scaffold, hiện KHÔNG có endpoint active."
muc_dich: "Scaffold cho log/notification SMS (LD_Notification_SMS). Hiện toàn bộ endpoint bị comment → controller RỖNG, không expose API nào."
alias: ["LD_Notification_SMS", "log notification sms"]
auth: internal
state_machine: []
deprecated: true
entities: [LD_Notification_SMS]
stored_procs: []
depends:
  - "ILD_Notification_SMSService (SMS.Services.Generated)"
cross_module_in: []
cross_module_out: []
---

# LD_Notification_SMSController — Log notification SMS (scaffold rỗng)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có endpoint active) | — | — | — | — | — | Controller chỉ inject service, không expose method nào. | — | `TimTheoDieuKien` đã bị comment (`cs:26-31`). |

## Quan hệ
- **Gọi sang:** `ILD_Notification_SMSService` (inject nhưng chưa dùng).
- **Dữ liệu (entities):** `LD_Notification_SMS` (log notification — chưa có CRUD).

## Lỗi đã biết / lưu ý kỹ thuật
- **DEAD/scaffold:** không có endpoint nào hoạt động — toàn bộ method bị comment (`LD_Notification_SMSController.cs:26-31`). Đánh dấu `deprecated: true` để AI bỏ qua nhanh.
