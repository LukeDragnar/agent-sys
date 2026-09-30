---
controller: LogDeliveryController
module: he-thong
service: LogDelivery
man_hinh_ui: "Hệ thống > Log Delivery (HT-AD) — tra cứu nhật ký data giao vận + nút gửi lại data lỗi"
muc_dich: "Ghi/đọc log nhật ký data giao vận (delivery): tra cứu log theo điều kiện và gửi lại (resend) data bị lỗi vào luồng delivery."
alias: ["log delivery", "nhật ký giao vận", "gửi lại data delivery", "resend delivery"]
auth: internal
state_machine: []
deprecated: false
entities: [LogDelivery, ReqResendDataError]
stored_procs: []
depends: [ILogDeliveryService]
cross_module_in: ["FE màn Log Delivery (HT-AD) gọi tra cứu/resend"]
cross_module_out: ["ResendDataErrorToDelivery đẩy lại data lỗi vào luồng xử lý Delivery"]
---

# LogDeliveryController — Log nhật ký data Delivery

Route `api/LogDelivery`, dùng `[SBAuthorize]` key (HT-AD) → **internal**.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tra cứu log Delivery | Tìm | POST `api/LogDelivery/TimTheoDieuKien` | LogDeliveryController_TimTheoDieuKienAsync | internal (HT-AD) | — | Lọc log theo `LogDeliveryFilter` | Mongo (log store) | — |
| 2 | Gửi lại data lỗi | Gửi lại | POST `api/LogDelivery/ResendDataErrorToDelivery` | LogDeliveryController_ResendDataErrorToDelivery | internal (HT-AD) | Data ở trạng thái lỗi | Đẩy lại bản ghi lỗi (`ReqResendDataError`) vào luồng xử lý Delivery | service | Mô tả quyền ghi "Gửi lại data Landing To Contact" (sai context) |

## Endpoint nội bộ (private / service-to-service)
- [public] GET `api/LogDelivery/TestJob` (không `[SBAuthorize]`) → gọi `JobClearDataLogMongo` (dọn log Mongo); endpoint test, trả Ok/BadRequest theo exception.

## Quan hệ
- **Gọi sang:** `ILogDeliveryService` (TimTheoDieuKien/ResendDataErrorToDelivery/JobClearDataLogMongo). Log lưu MongoDB.
- **Dữ liệu (entities):** log data Delivery, `ReqResendDataError`.
- **Ripple / đồng bộ:** ResendDataErrorToDelivery đẩy lại data → luồng xử lý Delivery xử lý lần nữa.

## Lỗi đã biết / lưu ý kỹ thuật
- `LogDeliveryController.cs:34` — `TestJob` public (không phân quyền) gọi clear data Mongo; nên giới hạn hoặc xóa.
- `LogDeliveryController.cs:27` — text mô tả ResendDataErrorToDelivery ghi "Gửi lại data Landing To Contact" (sai context).
