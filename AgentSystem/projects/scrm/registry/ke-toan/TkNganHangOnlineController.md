---
controller: TkNganHangOnlineController
module: ke-toan
service: WebhookBank
man_hinh_ui: "Cấu hình ngân hàng > đăng ký/ xác minh/ hủy kết nối thanh toán online (terminal MB). (Payment.API)"
muc_dich: "Đăng ký, xác minh (OTP/verify) và hủy kết nối thanh toán online (terminal) cho tài khoản ngân hàng — để TK có thể thu QR động qua MB."
alias: ["đăng ký thanh toán online", "kết nối terminal", "xác minh ngân hàng online", "hủy kết nối online"]
auth: internal
state_machine: []
deprecated: false
entities: [TkNganHangKetNoiOnline]
stored_procs: []
depends: [TkNganHangKetNoiOnlineService]
cross_module_in: [FE cấu hình ngân hàng online]
cross_module_out: [Ngân hàng MB (đăng ký/verify terminal)]
---

# TkNganHangOnlineController — Đăng ký kết nối thanh toán online (terminal)

> 3 endpoint live, **tất cả `[SBAuthorize]`** (yêu cầu đăng nhập → `internal`): đăng ký, xác minh, hủy kết nối thanh toán online. Các bản cũ dùng `[BankAuthorizeAttribute]` và 1 endpoint `[AllowAnonymous] GetPayment/{madonhang}` đã bị **comment** (L47-79).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Đăng ký online | "Kết nối online" | `POST api/TkNganHangOnline/DangKyThanhToanOnline` | (login `[SBAuthorize]`) | internal | — | `DangKyThanhToanOnline(model)` gửi đăng ký terminal sang NH. | service `TkNganHangKetNoiOnlineService` | — |
| 2 | Xác minh online | "Xác minh" | `POST api/TkNganHangOnline/XacMinhThanhToanOnline` | (login `[SBAuthorize]`) | internal | sau bước đăng ký | `XacMinhThanhToanOnline(model)` (verify/OTP). | service | — |
| 3 | Hủy kết nối online | "Hủy" | `POST api/TkNganHangOnline/HuyThanhToanOnline` | (login `[SBAuthorize]`) | internal | đang kết nối | `HuyThanhToanOnline(model)`. | service | — |

## Quan hệ
- **Gọi sang:** `TkNganHangKetNoiOnlineService` → NH MB (đăng ký/verify terminal).
- **Dữ liệu (entities):** `TkNganHangKetNoiOnline` (TerminalId/BankAccount — bảng mà `BankSendController` & `QrcodeBank` map TerminalId).
- **Ripple / đồng bộ:** đăng ký thành công → TK có TerminalId → `QrcodeBankController` mới tạo được QR động.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WebhookBank/Payment.API/Controllers/TkNganHangOnlineController.cs:20,29,38` — 3 endpoint `[SBAuthorize]` (internal). Bản cũ `[BankAuthorizeAttribute]` + `[AllowAnonymous] GetPayment/{madonhang}` đã comment (L47-79) → không live.
