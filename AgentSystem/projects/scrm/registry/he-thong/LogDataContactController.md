---
controller: LogDataContactController
module: he-thong
service: LogContact
man_hinh_ui: "Hệ thống > Log Contact (HT-AD) — tra cứu nhật ký data đẩy vào Contact + nút gửi lại data lỗi"
muc_dich: "Ghi/đọc log nhật ký data nhận vào module Contact: tra cứu log theo điều kiện và gửi lại (resend) data bị lỗi."
alias: ["log contact", "nhật ký contact", "gửi lại data contact", "resend data contact"]
auth: internal
state_machine: []
deprecated: false
entities: [LogDataContact, ReqResendDataError]
stored_procs: []
depends: [ILogDataContactService]
cross_module_in: ["FE màn Log Contact (HT-AD) gọi tra cứu/resend"]
cross_module_out: ["ResendDataContact đẩy lại data lỗi vào luồng xử lý Contact"]
---

# LogDataContactController — Log nhật ký data Contact

Route `api/LogDataContact`, dùng `[SBAuthorize]` key (HT-AD) → **internal**.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tra cứu log Contact | Tìm | POST `api/LogDataContact/TimTheoDieuKien` | LogDataContactController_TimTheoDieuKienAsync | internal (HT-AD) | — | Lọc log theo `LogContactFilter` | Mongo (log store) | Mô tả quyền ghi nhầm "Tìm kiếm Log Delivery" |
| 2 | Gửi lại data lỗi | Gửi lại | POST `api/LogDataContact/ResendDataContact` | LogDataContactController_ResendDataContact | internal (HT-AD) | Data ở trạng thái lỗi | Đẩy lại bản ghi lỗi (`ReqResendDataError`) vào luồng xử lý Contact | service | Mô tả quyền ghi nhầm "Tìm kiếm Log Delivery" |

## Quan hệ
- **Gọi sang:** `ILogDataContactService` (TimTheoDieuKien/ResendDataContact). Có job `JobClearDataLogMongo` (đã comment) → log lưu MongoDB.
- **Dữ liệu (entities):** log data Contact, `ReqResendDataError`.
- **Ripple / đồng bộ:** ResendDataContact đẩy lại data → luồng xử lý Contact xử lý lần nữa.

## Lỗi đã biết / lưu ý kỹ thuật
- `LogDataContactController.cs:20,27` — text mô tả 2 SBAuthorize đều ghi "Tìm kiếm Log Delivery" (copy-paste sai, đúng phải là Log Contact).
