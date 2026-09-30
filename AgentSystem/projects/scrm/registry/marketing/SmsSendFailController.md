---
controller: SmsSendFailController
module: marketing
service: SMS
man_hinh_ui: "Marketing > SMS/Zalo > Tin gửi lỗi (bảng các tin gửi thất bại để theo dõi/gửi lại)."
muc_dich: "Tra cứu danh sách tin gửi thất bại (SmsSendFail) theo điều kiện lọc. Job gửi lại (JobSendFail) hiện đã bị comment."
alias: ["SmsSendFail", "tin gửi lỗi", "sms thất bại", "gửi lại sms"]
auth: anonymous
state_machine: []
deprecated: false
entities: [SmsSendFail]
stored_procs: []
depends:
  - "ISmsSendFailService (SMS.Services.Generated)"
cross_module_in: []
cross_module_out: []
---

# SmsSendFailController — Tin SMS/Zalo gửi lỗi

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách tin lỗi | Tìm kiếm | POST `api/SmsSendFail/TimTheoDieuKien` | (không) | anonymous | — | Lọc danh sách `SmsSendFail` theo filter. | EF | KHÔNG có `[SBAuthorize]` (`cs:26`). |

## Quan hệ
- **Gọi sang:** `ISmsSendFailService`.
- **Dữ liệu (entities):** `SmsSendFail` (tin gửi thất bại, sinh từ consumer khi gửi lỗi).
- **Ripple / đồng bộ:** dữ liệu đến từ consumer gửi SMS khi gửi thất bại.

## Lỗi đã biết / lưu ý kỹ thuật
- Endpoint không có auth (`SmsSendFailController.cs:26-31`).
- `JobSendFail` (gửi lại tin lỗi) đã bị comment (`cs:33-38`) → không còn cơ chế retry tự động qua controller.
