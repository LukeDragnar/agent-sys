---
controller: CustomerInfoCampaignController
module: marketing
service: MarketingCampaign
man_hinh_ui: "Marketing > Chiến dịch > tab 'Lịch sử khách hàng gửi Job' — list khách đã được chiến dịch gửi SMS/Zalo + nút gửi lại"
muc_dich: "Tra cứu lịch sử khách hàng đã được Job chiến dịch gửi (CustomerInfoCampaign): trạng thái gửi từng khách; cho phép gửi lại SMS/Zalo cho các bản ghi đã chọn."
alias: ["lịch sử khách hàng gửi job", "lịch sử gửi chiến dịch", "khách hàng đã gửi", "gửi lại SMS/Zalo", "resend sms zalo", "customer info campaign"]
auth: public
state_machine: [LoiJobCampaign, ChuaGui, DangGui, GuiThanhCong, GuiThatBai]   # EnumStatusSendSMS (-1,0,1,2,3)
deprecated: false
entities: [CustomerInfoCampaign, CustomerInfoCampaignJob]
stored_procs: []
depends: [MongoDB, SMS/Zalo gateway, Customer (sync)]
cross_module_in: [Campaign Job (sinh bản ghi gửi), SMS/Zalo gateway (cập nhật trạng thái qua consumer)]
cross_module_out: [SMS/Zalo gateway (gửi lại)]
---

# CustomerInfoCampaignController — Lịch sử khách hàng gửi Job chiến dịch

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy lịch sử khách gửi | "Tìm kiếm" | POST `api/CustomerInfoCampaign/TimKiemTheoDieuKien` | `CustomerInfoCampaignController_TimKiemTheoDieuKien` | public | tab lịch sử | List khách CustomerInfoCampaign theo điều kiện (chiến dịch/trạng thái gửi/ngày), phân trang | Mongo `CustomerInfoCampaign` | - |
| 2 | Gửi lại SMS/Zalo | "Gửi lại" | POST `api/CustomerInfoCampaign/ReSendSMS_ZALO` | `CustomerInfoCampaignController_ReSendSMS_ZALO` | public | bản ghi đã chọn (thường GuiThatBai) | Gửi lại tin nhắn SMS/Zalo cho các bản ghi trong payload | Mongo + gateway | - |

## Quy tắc nghiệp vụ
- **Trạng thái gửi** (EnumStatusSendSMS): `-1` LoiJobCampaign, `0` ChuaGui, `1` DangGui, `2` GuiThanhCong, `3` GuiThatBai. Gửi lại thường áp cho ChuaGui/GuiThatBai/Lỗi.
- Scope dữ liệu theo currentUser (đơn vị/chi nhánh) như các màn marketing khác.

## Quan hệ
- **Gọi sang:** SMS/Zalo gateway khi gửi lại; đọc CustomerInfoCampaign (Mongo).
- **Dữ liệu (entities):** `CustomerInfoCampaign` (mỗi khách × chiến dịch × lần gửi), `CustomerInfoCampaignJob`.
- **Ripple / đồng bộ:** Bản ghi do Campaign Job sinh ra; trạng thái cập nhật qua consumer `EventUpdateStatusSendSMSConsumer` / `EventLandingCampaignUpdateStatusSMSSendConsumer`.

## Lỗi đã biết / lưu ý kỹ thuật
- Controller inject `ISyncCustomerService` + `IConfiguration` chỉ phục vụ endpoint `Test` (đã comment, CustomerInfoCampaignController.cs:42-62) — 2 dependency này hiện không dùng cho 2 endpoint active. Dọn được nếu refactor.
- Endpoint `Test` (sync 1 khách từ DB Customer qua SqlSugar) bị comment toàn bộ — chỉ là tool debug, không phải API thật.
