---
controller: ContactSendSMSController
module: marketing
service: MarketingCampaign
man_hinh_ui: "Marketing > Landing campaign > tab 'Log SMS' — list log gửi SMS từ landing + thống kê"
muc_dich: "Tra cứu & thống kê log SMS gửi từ Landing campaign (ContactWebhookSendSMSLog): contact điền form landing → webhook gửi SMS → ghi log; màn này list + thống kê."
alias: ["log sms landing", "log gửi sms", "thống kê sms landing", "contact send sms", "landing campaign sms"]
auth: public
state_machine: []
deprecated: false
entities: [ContactWebhookSendSMSLog]
stored_procs: []
depends: [MongoDB, SMS gateway, Landing webhook]
cross_module_in: [Landing/Webhook (EventContactWebhookSendSMSConsumer ghi log)]
cross_module_out: []
---

# ContactSendSMSController — Log SMS Landing campaign

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy danh sách log SMS | "Tìm kiếm" | POST `api/ContactSendSMS/TimKiemTheoDieuKien` | `ContactSendSMSController_TimKiemTheoDieuKien` | public | tab log | List log `ContactWebhookSendSMSLog` theo điều kiện/phân trang | Mongo | - |
| 2 | Thống kê log SMS | "Thống kê" | POST `api/ContactSendSMS/ThongKe` | `ContactSendSMSController_ThongKe` | public | tab thống kê | Thống kê log SMS landing (đếm theo trạng thái/thời gian) | Mongo aggregation | - |

## Quan hệ
- **Gọi sang:** đọc Mongo `ContactWebhookSendSMSLog`.
- **Dữ liệu (entities):** `ContactWebhookSendSMSLog`.
- **Ripple / đồng bộ:** Log do consumer `EventContactWebhookSendSMSConsumer` ghi khi contact landing trigger gửi SMS; cập nhật trạng thái qua `EventUpdateStatusSendSMSConsumer`. Controller chỉ đọc (read-only view).

## Lỗi đã biết / lưu ý kỹ thuật
- Controller thuần read-only (list + thống kê) — không có ghi/sửa. Việc ghi log nằm ở consumer.
