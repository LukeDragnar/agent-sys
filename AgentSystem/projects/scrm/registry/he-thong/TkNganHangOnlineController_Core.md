---
controller: TkNganHangOnlineController
module: he-thong
service: Core
man_hinh_ui: "Cấu hình ngân hàng > Kết nối thanh toán online (VietQR/terminal): khởi tạo kết nối, đăng ký, xác minh OTP, hủy kết nối thanh toán online"
muc_dich: "Đăng ký / xác minh / hủy kết nối thanh toán online (VietQR/terminal) cho tài khoản ngân hàng — để TK thu QR động."
alias: ["TkNganHangOnline", "thanh toán online", "VietQR", "kết nối thanh toán online", "đăng ký QR online"]
auth: internal
state_machine: [KhoiTao, DangKy, XacMinh, Huy]
deprecated: false
entities: [TkNganHangKetNoiOnline]
stored_procs: []
depends: ["ITkNganHangKetNoiOnlineService", "BaseController"]
cross_module_in: ["FE Cấu hình ngân hàng/Kết nối online gọi KetNoiInit/DangKyThanhToanOnline/XacMinhThanhToanOnline/HuyThanhToanOnline"]
cross_module_out: ["Kết nối thành công → TK ngân hàng thu được QR động online; trùng tên file với TkNganHangOnlineController (service WebhookBank, module ke-toan) — KHÁC controller"]
---

# TkNganHangOnlineController — Kết nối thanh toán online (Core)

Controller service **Core** (route `api/TkNganHangOnline`), kế thừa `BaseController`, controller-level `[SBAuthorize]` rỗng (login bắt buộc), từng method KHÔNG gắn key chi tiết → auth = internal (login). KHÁC với `TkNganHangOnlineController` service WebhookBank ở `registry/ke-toan/TkNganHangOnlineController.md` → node này suffix `_Core`.

Luồng kết nối: KhoiTao → DangKy → XacMinh (OTP/verify) → (có thể) Huy.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Khởi tạo kết nối | (mở form kết nối) | GET `api/TkNganHangOnline/KetNoiInit/{id:Guid}` | — (login) | internal | TK chưa kết nối | `KetNoiInit(id, CurrentUser)` — chuẩn bị dữ liệu đăng ký cho TK `id`. | EF/external | — |
| 2 | Đăng ký thanh toán online | Đăng ký | POST `api/TkNganHangOnline/DangKyThanhToanOnline` | — (login) | internal | Sau KhoiTao | `DangKyThanhToanOnline(model, CurrentUser)` — gửi yêu cầu đăng ký kết nối (gọi MB/cổng). | external | — |
| 3 | Xác minh thanh toán online | Xác minh OTP | POST `api/TkNganHangOnline/XacMinhThanhToanOnline` | — (login) | internal | Sau DangKy (có mã OTP) | `XacMinhThanhToanOnline(model, CurrentUser)` — verify OTP → kích hoạt kết nối. | external | — |
| 4 | Hủy kết nối | Hủy | POST `api/TkNganHangOnline/HuyThanhToanOnline/{id:Guid}` | — (login) | internal | TK đã kết nối | `HuyThanhToanOnline(id, CurrentUser)` — hủy kết nối thanh toán online. | external | — |

## Quy tắc nghiệp vụ (state machine)
1. KetNoiInit → tạo phiên/khởi tạo dữ liệu cho TK `id`.
2. DangKyThanhToanOnline → gửi đăng ký tới cổng (MB) → trả về cần xác minh OTP.
3. XacMinhThanhToanOnline → verify OTP đúng → kết nối kích hoạt (TK thu QR online được).
4. HuyThanhToanOnline → hủy kết nối đang hoạt động.

## Quan hệ
- **Gọi sang:** ITkNganHangKetNoiOnlineService (gọi cổng thanh toán MB/external).
- **Dữ liệu (entities):** trạng thái kết nối online của tài khoản ngân hàng.
- **Ripple / đồng bộ:** kết nối thành công → bật thu QR động online cho TK; tác động luồng thanh toán đơn hàng.

## Lỗi đã biết / lưu ý kỹ thuật
- `[TienIch("TkNganHangOnlineController", ..., TI_PaymentOnline)]` bị comment (dòng 10) → không gating theo tiện ích PaymentOnline ở controller-level Core này.
- Trùng tên file với controller WebhookBank ở ke-toan → node suffix `_Core`.
