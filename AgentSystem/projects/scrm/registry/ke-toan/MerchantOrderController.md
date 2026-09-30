---
controller: MerchantOrderController
module: ke-toan
service: WebhookBank
man_hinh_ui: "nội bộ — không có UI. Webhook PRODUCTION nhận báo có thanh toán từ Ngân hàng (BankReceive.API)."
muc_dich: "Endpoint webhook chính (Production) để Ngân hàng/đối tác bắn báo có thanh toán QR vào → service xử lý cập nhật đơn/giao dịch qua MerchantOrderService.GetDataReceive."
alias: ["webhook báo có", "PaymentReceive", "nhận thanh toán bank production", "callback ngân hàng"]
auth: anonymous
state_machine: []
deprecated: false
entities: [LogRequestQrcode, MerchantOrder]
stored_procs: []
depends: [BankReceive.Service/MerchantOrderService]
cross_module_in: [Ngân hàng / đối tác thanh toán (webhook gọi vào)]
cross_module_out: [Order (cập nhật thanh toán đơn hàng qua service)]
---

# MerchantOrderController — Webhook nhận báo có thanh toán (Production)

> Controller **1 endpoint** `POST api/MerchantOrder/PaymentReceive`. Nhận `BankReceiveDto` từ ngân hàng → ủy thác toàn bộ cho `IMerchantOrderService.GetDataReceive(model)` (logic match hóa đơn + cập nhật thanh toán nằm trong service). **KHÔNG có attribute auth nào** trên method lẫn controller → anonymous. Đây là webhook nhận TIỀN thật trên Production (khác `BankSendController` chỉ chạy dev/test).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Nhận webhook báo có | (không UI — NH gọi) | `POST api/MerchantOrder/PaymentReceive` | (không) | anonymous | webhook (mọi env) | Nhận `BankReceiveDto` → `MerchantOrderService.GetDataReceive(model)` xử lý báo có/match đơn → trả `Ok(rs)`. | service `MerchantOrderService.GetDataReceive` (logic EF/match trong service) | **Anonymous + nhận tiền thật trên Production**: lỗ hổng nếu service không tự kiểm chữ ký/whitelist IP/secret bên trong `GetDataReceive`. Cần xác minh service có validate nguồn gọi không. |

## Endpoint nội bộ (private / service-to-service)
- Không có. Toàn bộ side-effect nằm trong `MerchantOrderService.GetDataReceive` (cập nhật trạng thái thanh toán, có thể callback Order).

## Quan hệ
- **Gọi sang:** `MerchantOrderService` → (gián tiếp) Order để cập nhật đơn.
- **Dữ liệu (entities):** xử lý qua service trên `LogRequestQrcode`/giao dịch merchant.
- **Ripple / đồng bộ:** webhook báo có → cập nhật thanh toán đơn hàng (ripple sang Order).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WebhookBank/BankReceive.API/Controllers/MerchantOrderController.cs:22-28` — `PaymentReceive` KHÔNG có auth (controller cũng không) → **anonymous webhook nhận tiền trên Production**. Bảo mật phụ thuộc HOÀN TOÀN vào việc `GetDataReceive` tự verify chữ ký/nguồn — phải kiểm tra trong service, controller không chặn gì.
- Comment chết L26 (`BankReceiveResponseDto.ResultWithSuccess()`) — bỏ qua.
