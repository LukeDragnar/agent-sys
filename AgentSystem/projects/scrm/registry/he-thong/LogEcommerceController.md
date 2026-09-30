---
controller: LogEcommerceController
module: he-thong
service: LogEcommerce
man_hinh_ui: "Hệ thống > Log Ecommerce (HT-AD) — tra cứu nhật ký data sàn TMĐT + nút gửi lại data lỗi"
muc_dich: "Ghi/đọc log nhật ký data nhận từ sàn TMĐT (ecommerce): tra cứu log theo điều kiện và gửi lại (resend) data bị lỗi vào luồng ecommerce."
alias: ["log ecommerce", "nhật ký sàn", "gửi lại data ecom", "resend ecommerce"]
auth: internal
state_machine: []
deprecated: false
entities: [LogEcommerce, ReqResendDataError]
stored_procs: []
depends: [ILogEcommerceService]
cross_module_in: ["FE màn Log Ecommerce (HT-AD) gọi tra cứu/resend"]
cross_module_out: ["ResendDataErrorToEcom đẩy lại data lỗi vào luồng xử lý Ecommerce"]
---

# LogEcommerceController — Log nhật ký data Ecommerce

Route `api/LogEcommerce`, dùng `[SBAuthorize]` key (HT-AD) → **internal**. `[TienIch]` đã comment.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tra cứu log Ecommerce | Tìm | POST `api/LogEcommerce/TimTheoDieuKien` | LogEcommerceController_TimTheoDieuKien | internal (HT-AD) | — | Lọc log theo `LogEcommerceFilter` | Mongo (log store) | — |
| 2 | Gửi lại data lỗi | Gửi lại | POST `api/LogEcommerce/ResendDataErrorToEcom` | LogContactReceive_ResendToContact | internal (HT-AD) | Data ở trạng thái lỗi | Đẩy lại bản ghi lỗi (`ReqResendDataError`) vào luồng xử lý Ecommerce | service | Quyền key dùng nhầm `LogContactReceive_ResendToContact` + mô tả "Gửi lại data Landing To Contact" |

## Quan hệ
- **Gọi sang:** `ILogEcommerceService` (TimTheoDieuKien/ResendDataErrorToEcom). Log lưu MongoDB.
- **Dữ liệu (entities):** log data Ecommerce, `ReqResendDataError`.
- **Ripple / đồng bộ:** ResendDataErrorToEcom đẩy lại data → luồng xử lý Ecommerce xử lý lần nữa.

## Lỗi đã biết / lưu ý kỹ thuật
- `LogEcommerceController.cs:30` — ResendDataErrorToEcom dùng nhầm permission key `LogContactReceive_ResendToContact` (của LogContact) thay vì key riêng của Ecommerce → phân quyền sai module.
