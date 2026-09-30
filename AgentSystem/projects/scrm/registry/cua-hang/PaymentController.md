---
controller: PaymentController
module: cua-hang
service: Order
man_hinh_ui: "Cửa hàng / POS thu ngân — bước thanh toán online: khi chọn PTTT chuyển khoản/VietQR, hệ thống tạo mã QR (CreateQrcode) và poll kiểm tra đã nhận tiền (KiemTraThanhToan). `DataReceivce` là webhook ngân hàng (không có UI)."
muc_dich: "Xử lý thanh toán online qua VietQR/MBBank cho đơn hàng tại quầy: tạo mã QR thanh toán, kiểm tra trạng thái đã nhận tiền, và nhận webhook báo có (DataReceive) từ ngân hàng đẩy vào hàng đợi xử lý."
alias: ["thanh toán online", "vietqr", "qr code thanh toán", "mbbank", "chuyển khoản qr", "quét mã trả tiền"]
auth: mixed
state_machine: []
deprecated: false
entities: [DonHang, DonHangThanhToanOnline, BankOnlineReceive]
stored_procs: []
depends: [BankReceive (webhook ngân hàng), RabbitMQ (IPublishEndpointDonHang), VietQR/MBBank API]
cross_module_in: [Ngân hàng/BankReceive — POST DataReceivce (webhook AllowAnonymous đẩy biến động số dư)]
cross_module_out: [RabbitMQ publish BankOnlineReceive → consumer đối chiếu + cập nhật DonHangThanhToanOnline → đánh dấu đơn đã nhận tiền online]
---

# PaymentController — Thanh toán online VietQR

> TienIch khai báo là `QrcodeBankController` / `Quản lý thanh toán online vietqr` (`MaTienIchSCRM.TI_PaymentOnline`) — tên class là `PaymentController`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo mã QR thanh toán | (auto khi chọn PTTT QR) | POST `api/Payment/CreateQrcode` | login (`[SBAuthorize()]`) | public | bước thanh toán | Sinh chuỗi VietQR/MBBank theo số tiền + nội dung đơn để khách quét | service | key `Payment_CreateQrcode` bị comment → chỉ login |
| 2 | Kiểm tra đã thanh toán | (poll/nút "Đã chuyển khoản") | POST `api/Payment/KiemTraThanhToan` | `Payment_KiemTraThanhToan` | public | sau khi tạo QR | Kiểm tra biến động số dư đã khớp đơn chưa → trả trạng thái nhận tiền | service | - |
| 3 | Webhook nhận tiền | (không có UI) | POST `api/Payment/DataReceivce` | — | anonymous | server-to-server | Nhận `BankOnlineReceive` từ ngân hàng → `Publish` vào RabbitMQ (consumer đối chiếu sau) | RabbitMQ publish | `[AllowAnonymous]` webhook — cần whitelist/secret để chống giả mạo |

## Quan hệ
- **Gọi sang:** API VietQR/MBBank (tạo QR), `IPaymentService` (đối chiếu số dư).
- **Dữ liệu (entities):** `DonHang` (số tiền/nội dung), `DonHangThanhToanOnline` (lịch sử/đối chiếu), `BankOnlineReceive` (payload webhook).
- **Ripple / đồng bộ:** `DataReceivce` → `IPublishEndpointDonHang.Publish` → consumer xử lý bất đồng bộ → cập nhật trạng thái thanh toán online của đơn.

## Luồng xuyên module (thu tiền online tại POS)
```
Thu ngân chọn PTTT VietQR → CreateQrcode (sinh QR theo tiền+nội dung đơn)
  → khách quét + chuyển khoản
  → Ngân hàng gọi webhook DataReceivce (AllowAnonymous) → Publish BankOnlineReceive (RabbitMQ)
       → consumer đối chiếu số tiền/nội dung ↔ đơn → cập nhật DonHangThanhToanOnline
  → POS poll KiemTraThanhToan → khớp → cho phép hoàn tất ThanhToan đơn
```

## Lỗi đã biết / lưu ý kỹ thuật
- **`DataReceivce` `[AllowAnonymous]`** (PaymentController:57) — webhook công khai, chỉ `Publish` rồi trả `true`; không thấy xác thực chữ ký/secret tại controller → rủi ro giả mạo biến động số dư, cần bảo vệ ở tầng consumer/đối chiếu.
- **Tên route `DataReceivce`** sai chính tả (thiếu/đảo) — copy verbatim, FE/bank đã cấu hình theo string này, không "sửa" 1 phía.
- **`CreateQrcode` mất permission key** (`Payment_CreateQrcode` comment) → chỉ login.
- Code thật: BE `Order/Order.API/Controllers/PaymentController.cs` + `IPaymentService`; webhook payload `Models.Core.DTO.BankReceive.Client.BankOnlineReceive`.
