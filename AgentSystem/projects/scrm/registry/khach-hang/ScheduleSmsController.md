---
controller: ScheduleSmsController
module: khach-hang
service: Customer
man_hinh_ui: "Customer/sms-khachhang (gửi SMS khách hàng): danh sách lịch SMS + popup soạn/gửi hàng loạt (send-sms-khachhang)"
muc_dich: "Lập lịch & gửi SMS/Zalo hàng loạt cho khách hàng: tìm lịch SMS, gửi hàng loạt, đẩy hàng đợi, và worker xử lý gửi (ProcessSendSms) theo trạng thái hàng đợi"
alias: [ScheduleSms, "Gửi SMS", "SMS khách hàng", "Gửi hàng loạt", SmsSend]
auth: public
state_machine: [Waiting, Pending, Done, Error]
deprecated: false
entities: [ScheduleSms]
stored_procs: []
depends: [IScheduleSmsService, BaseController, "SMS service (AccountSms / gửi SMS, Zalo pattern)"]
cross_module_in: ["Worker/scheduler gọi ProcessSendSms định kỳ để đẩy hàng đợi"]
cross_module_out: ["SMS service (gửi SMS qua SmsType=2, Zalo qua SmsType=4)"]
---

# ScheduleSms — Lập lịch & gửi SMS hàng loạt khách hàng

> Controller có **job/worker gating** rõ: `ProcessSendSms` là worker quét hàng đợi và đổi state SMS qua máy trạng thái Waiting → Pending → Done/Error.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm lịch SMS theo điều kiện | Lưới "SMS khách hàng" | `POST TimTheoDieuKien` | — (`[SBAuthorize]` mức class) | public | — | `TimTheoDieuKienAsync(gridInfo)` → list lịch SMS theo filter | EF | — |
| 2 | Gửi SMS hàng loạt | Nút "Gửi hàng loạt" | `POST SendSms` | ScheduleSms_SendSms (HT-AD, DV-AD-TONG, HT-CSKH, DV-AD-CN, DV-MKT) | public | — | `SendSms(input, CurrentUser)` — gửi SMS cho danh sách KH | EF + SMS service | — |
| 3 | Thêm hàng loạt vào hàng đợi | (đặt lịch gửi) | `POST ScheduleSmsAddRange` | — (`[SBAuthorize]` mức class) | public | — | `ScheduleSmsAddRange(input, CurrentUser)` — tạo loạt bản ghi `ScheduleSms` trạng thái chờ | EF | — |
| 4 | Worker xử lý gửi hàng đợi | (gọi định kỳ, không phải nút) | `GET ProcessSendSms` | — (`[SBAuthorize]` mức class) | public | — | Quét tối đa 100 bản ghi đến hạn → gửi → cập nhật state | EF + SMS service | Endpoint worker để mở `[SBAuthorize]` (chỉ cần đăng nhập) — ai gọi cũng kích job gửi (cần xác nhận chủ ý) |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)

> `ProcessSendSms` là **worker hàng đợi** với máy trạng thái. Blueprint cho trace-agent.

- **Lọc bản ghi đến hạn:** lấy SMS có `Status = Waiting` HOẶC (`Status = Pending` VÀ `NgayCapNhat ≤ now − 10 phút`) → tránh kẹt bản ghi Pending quá 10 phút; sắp xếp theo `NgayTao`, lấy **tối đa 100** bản/lần.
- **Không có bản ghi nào** → trả lỗi mã `"HET_TIN_NHAN"` (hết tin nhắn chờ).
- **Đánh dấu đang xử lý:** set toàn bộ batch → `Pending`, cập nhật `NgayCapNhat`, lưu trước khi gửi.
- **Gửi theo loại:** `SmsType == 2` → `SendSmsByIdPatternSms` (SMS); `SmsType == 4` → `SendZaloByIdPatternSms` (Zalo). Loại khác không gửi.
- **Cập nhật kết quả:** gửi thành công → `Done`; thất bại → `Error`; cập nhật `NgayCapNhat`.
- (Liên quan) `ScheduleSendSmsBirthDayCustomer` trong service: lấy đơn vị có cấu hình dùng SMS rồi tạo lịch gửi SMS sinh nhật KH (job riêng — cần xác nhận trigger).

## Quan hệ
- **Gọi sang:** `IScheduleSmsService` (lập lịch + gửi) → service gọi tiếp **SMS service** (gửi SMS/Zalo theo pattern).
- **Dữ liệu (entities):** `ScheduleSms` (Status: Waiting/Pending/Done/Error, SmsType 2=SMS / 4=Zalo, DataSend = JSON `SaveSendSmsDto`).
- **Ripple / đồng bộ:** mỗi lần `ProcessSendSms` đẩy SMS/Zalo thật sang SMS service; thay đổi trạng thái bản ghi hàng đợi.

## Luồng xuyên module
1. **Đặt lịch → gửi:** FE `SendSms`/`ScheduleSmsAddRange` tạo bản ghi `ScheduleSms` (Waiting) → worker `ProcessSendSms` (gọi định kỳ) quét hàng đợi → gửi qua SMS service → cập nhật Done/Error.

## Lỗi đã biết / lưu ý kỹ thuật (path:line)
- `ScheduleSmsController.cs:31-36,44-56` — `TimTheoDieuKien`, `ScheduleSmsAddRange`, `ProcessSendSms` **không gắn key quyền riêng**, chỉ dựa `[SBAuthorize]` mức class → bất kỳ user đăng nhập gọi được; riêng `SendSms` mới siết role.
- `ScheduleSmsService.cs:564-572` — ngưỡng "kẹt Pending" cố định **10 phút** và batch **100**; đổi 2 hằng này ảnh hưởng throughput/độ trễ gửi.
- `ScheduleSmsService.cs:590-597` — chỉ xử lý `SmsType` 2 (SMS) và 4 (Zalo); loại khác bị bỏ qua âm thầm (không gửi, không báo lỗi).
- `ScheduleSmsController.cs:18-24` — constructor nhận `IServiceProvider inject` nhưng không gán field nào ngoài `_service` (tham số thừa).
