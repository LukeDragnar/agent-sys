---
controller: QrcodeBankController
module: ke-toan
service: WebhookBank
man_hinh_ui: "nội bộ — service-to-service. Sinh mã QR thanh toán cho Order/POS (Payment.API)."
muc_dich: "Tạo mã QR thanh toán (qua NH MB hoặc QR tĩnh tự sinh) cho đơn hàng, và kiểm tra trạng thái thanh toán đơn (CheckOrder). Dùng BankAuthorizeAttribute (token hệ thống service-to-service)."
alias: ["tạo qr", "qrcode thanh toán", "CreateQr", "CreateQrV2", "check order thanh toán"]
auth: internal
state_machine: []
deprecated: false
entities: [LogRequestQrcode, TkNganHangHeThong, TkMerchant]
stored_procs: []
depends: [MBBankService (TaoQrCodeMB/V2, GetTokenMB), TkMerchantService, TkNganHangHeThongService, QrcodeBankService (CheckOrder), QRCodeGenerator]
cross_module_in: [Order / POS (gọi tạo QR + check thanh toán)]
cross_module_out: [Ngân hàng MB (đăng ký QR terminal)]
---

# QrcodeBankController — Sinh mã QR thanh toán & kiểm tra đơn

> 4 endpoint, **tất cả có `[BankAuthorizeAttribute]`** (auth token hệ thống service-to-service → `internal`). Tạo QR theo 2 đường: nếu NH `IsKetNoiTerminal==true` → gọi MB tạo QR động theo TerminalId; ngược lại tự sinh QR tĩnh bằng `QRCodeGenerator`. Có guard `IsProduction`: khi KHÔNG Production thì **fake `TerminalId="PSALE18"`** (staging). `CheckOrder` tra trạng thái thanh toán đơn.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo QR theo Merchant | (service) | `POST api/QrcodeBank/CreateQr` | (`[BankAuthorizeAttribute]`) | internal | — | Lấy `userMerchant` theo `Merchant` → `GetTokenMB` → `TaoQrCodeMB` (QR động MB). | service `MBBankService.TaoQrCodeMB` + `TkMerchantService` | — |
| 2 | Tạo QR theo ngân hàng (thu mặc định) | (service) | `POST api/QrcodeBank/CreateQrV2` · `POST api/QrcodeBank/CreateQrNhanTien` | (`[BankAuthorizeAttribute]`) | internal | — | `GetTaiKhoanNganHang(ThuMacDinh)`; chưa cấu hình → lỗi. Nếu `IsKetNoiTerminal!=true` → QR tĩnh `QRCodeGenerator`. Nếu có → MB `TaoQrCodeMBV2` (V2) / `TaoQrCodeMB` (NhanTien). Non-Production fake `TerminalId="PSALE18"`. | service `MBBankService` + `TkNganHangHeThongService.GetTaiKhoanNganHang` | V2 & NhanTien gần trùng (khác mỗi gọi `TaoQrCodeMBV2` vs `TaoQrCodeMB`). |
| 3 | Kiểm tra thanh toán đơn | (service) | `POST api/QrcodeBank/CheckOrder` | (`[BankAuthorizeAttribute]`) | internal | — | `QrcodeBankService.CheckOrder(gridInfo)` trả trạng thái thanh toán theo đơn. | service `QrcodeBankService.CheckOrder` | — |

## Quan hệ
- **Gọi sang:** NH MB (`MBBankService` lấy token + tạo QR), `TkMerchantService` (user merchant), `TkNganHangHeThongService` (TK ngân hàng + TerminalId).
- **Dữ liệu (entities):** `TkNganHangHeThong` (BankCode/SoTaiKhoan/TerminalId/IsKetNoiTerminal), `TkMerchant` (UserName/SecretKey), `LogRequestQrcode` (session QR — sinh ở luồng MB).
- **Ripple / đồng bộ:** QR động MB → khi NH báo có sẽ callback về `MerchantOrderController`/`BankSendController`.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WebhookBank/Payment.API/Controllers/QrcodeBankController.cs:40,56,108,152` — mọi endpoint có `[BankAuthorizeAttribute]` → internal, KHÔNG anonymous (đúng chuẩn cho controller mutate luồng tiền).
- `IsProduction` đọc từ env trong constructor (L36-37); non-Production ép `TerminalId="PSALE18"` (L87-90, 136-139) → QR staging không khớp NH thật → đừng dùng QR sinh ở staging để thu tiền thật.
- `CreateQrV2` vs `CreateQrNhanTien` (L57-150) gần như trùng hoàn toàn → cân nhắc gộp; sửa logic phải sửa cả 2.
