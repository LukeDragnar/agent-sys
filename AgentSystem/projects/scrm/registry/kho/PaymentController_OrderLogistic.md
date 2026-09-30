---
controller: PaymentController
module: kho
service: OrderLogistic
man_hinh_ui: "Giao vận / Đơn hàng logistic — bước thanh toán online: hiển thị/ xem lại mã QR thanh toán (ViewQrCode) cho đơn logistic; `DataReceivce` là webhook ngân hàng (không UI). [Lưu ý: file đặt _OrderLogistic do trùng tên với PaymentController service Order ở module cua-hang]."
muc_dich: "Thanh toán online cho đơn hàng logistic: xem mã QR thanh toán (ViewQrCode) và nhận webhook báo có tiền (DataReceivce) từ ngân hàng → publish message DonHang để consumer xử lý."
alias: ["thanh toán online logistic", "qr thanh toán đơn logistic", "viewqrcode", "webhook ngân hàng logistic", "payment logistic"]
auth: mixed
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends:
  - "IPaymentService (ViewQrCode)"
  - "IPublishEndpointDonHang (publish BankOnlineLogisticReceive lên bus)"
cross_module_in:
  - "Ngân hàng / cổng thanh toán (webhook DataReceivce — BankOnlineLogisticReceive)"
cross_module_out:
  - "Consumer DonHang (nhận BankOnlineLogisticReceive đã publish để cập nhật thanh toán đơn logistic)"
---

# PaymentController (OrderLogistic) — Thanh toán online đơn hàng logistic

> Trùng tên `PaymentController` với node `cua-hang/PaymentController.md` (service Order). Đây là bản của **service OrderLogistic** → file suffix `_OrderLogistic`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Webhook nhận có tiền | (không UI — webhook ngân hàng) | POST `api/Payment/DataReceivce` | (không) | anonymous (`[AllowAnonymous]`) | gọi từ ngân hàng | Nhận `BankOnlineLogisticReceive` → `_publishEndpointDonHang.Publish(reqBody)` → trả `true`. KHÔNG xử lý đồng bộ tại controller, đẩy sang consumer | publish bus (DonHang) | Tên action sai chính tả `DataReceivce` (PaymentController.cs:32) |
| 2 | Xem mã QR thanh toán | (bước thanh toán đơn logistic) | POST `api/Payment/ViewQrCode` | `[SBAuthorize()]` (controller dùng TienIch `TI_PaymentOnline`) | public | khi thanh toán online | Nhận `ViewQrCodeDto` → `_service.ViewQrCode(request, CurrentUser)` → trả thông tin QR | IPaymentService | - |

## Endpoint nội bộ (private / service-to-service)
- [internal/webhook] `DataReceivce` không phải service-to-service nội bộ mà là webhook ngân hàng → publish `BankOnlineLogisticReceive` lên bus cho consumer DonHang xử lý cập nhật thanh toán.

## Quan hệ
- **Gọi sang:** `IPaymentService.ViewQrCode`; bus qua `IPublishEndpointDonHang.Publish`.
- **Dữ liệu (entities):** không thao tác EF trực tiếp ở controller (DTO `ViewQrCodeDto`, `BankOnlineLogisticReceive`).
- **Ripple / đồng bộ:** webhook → publish message → consumer DonHang cập nhật trạng thái thanh toán đơn logistic.

## Luồng xuyên module
- Ngân hàng báo có → `DataReceivce` → publish `BankOnlineLogisticReceive` → consumer DonHang (OrderLogistic) cập nhật đơn đã thanh toán.

## Lỗi đã biết / lưu ý kỹ thuật
- `PaymentController.cs:14` `[TienIch("QrcodeBankController", "Quản lý thanh toán online vietqr", MaTienIchSCRM.TI_PaymentOnline)]` — tên TienIch là "QrcodeBankController" (lệch tên class).
- `PaymentController.cs:32` action typo `DataReceivce` (thiếu sửa, ảnh hưởng nếu webhook config theo đúng path — giữ nguyên path khi tích hợp).
- Khác biệt với `cua-hang/PaymentController` (service Order, có CreateQrcode/KiemTraThanhToan/DataReceivce, dùng VietQR/MBBank): bản này chỉ 2 endpoint, phục vụ riêng đơn logistic.
