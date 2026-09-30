---
controller: BankSendController
module: ke-toan
service: WebhookBank
man_hinh_ui: "nội bộ — không có UI. Webhook nhận callback thanh toán từ Ngân hàng MB (BankReceive.API)."
muc_dich: "Nhận webhook báo thanh toán QR từ NH MB → cập nhật trạng thái LogRequestQrcode (Đã thanh toán) + POST callback BankOnlineReceive về UrlCallback của session (token hệ thống) để Order cập nhật đơn hàng. CHỈ chạy ở môi trường dev/test (chặn bằng IsProduction)."
alias: ["webhook MB", "PaymentUpdate", "callback thanh toán bank", "báo có (QR MB)"]
auth: anonymous
state_machine: [TrangThai=0 (ChuaThanhToan), TrangThai=1 (DaThanhToan)]
deprecated: false
entities: [LogRequestQrcode, TkNganHangKetNoiOnline]
stored_procs: []
depends: [Order (UrlCallback nhận BankOnlineReceive), RestSharp, ServiceInfo.Token]
cross_module_in: [Ngân hàng MB (webhook gọi vào)]
cross_module_out: [Order/DataReceive (BankOnlineReceive cập nhật thanh toán đơn hàng)]
---

# BankSendController — Webhook nhận thanh toán QR từ Ngân hàng MB (dev/test)

> **2 webhook GET anonymous** `PaymentUpdate/{ma}` và `PaymentUpdate2/{ma}` (logic GẦN NHƯ y hệt nhau). Cả hai: tìm `LogRequestQrcode` theo `TransactionPurpose==ma` (mới nhất), nếu `TrangThai==0` → set `=1` (Đã thanh toán) + ngày NH gửi/thanh toán, rồi POST callback `BankOnlineReceive` về `session.UrlCallback` kèm header `SystemAuthorization: Bearer {ServiceInfo.Token}` để Order cập nhật đơn. **CẢ HAI ĐỀU GATED bởi IsProduction**: ở Production/ProductionDebug trả `Ok("Bạn không có quyền")` và KHÔNG làm gì → đây là webhook chỉ dùng cho dev/test (Production dùng `MerchantOrderController/PaymentReceive`).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Webhook báo thanh toán | (không UI — NH MB gọi) | `GET api/BankSend/PaymentUpdate/{ma}` · `GET api/BankSend/PaymentUpdate2/{ma}` | (không) | anonymous | chỉ chạy khi KHÔNG phải Production | Tìm `LogRequestQrcode` theo `ma`; nếu `TrangThai==0` → set `=1` + ngày; POST `BankOnlineReceive` về `UrlCallback` (token hệ thống). Production → `Ok("Bạn không có quyền")`. Mã không có → `Ok("Mã không tồn tại")`. | **EF** `LogRequestQrcode` + `TkNganHangKetNoiOnline` (lấy STK/chủ TK theo TerminalId) | Anonymous + nhận tiền (mutate payment state) — chỉ an toàn nhờ chặn Production. Lỗi callback nuốt im (`catch{}` rỗng) → Order có thể không nhận được mà NH coi là OK. |

## Endpoint nội bộ (private / service-to-service)
- [outbound] POST callback `session.UrlCallback` (thường là Order/DataReceive) body `BankOnlineReceive{MaHoaDon, TongTien, NgayThanhToan, TerminalId, MaThanhToanBank, SoTaiKhoan, ChuTaiKhoan}` + header `SystemAuthorization` = token hệ thống. Có retry 1 lần (`SendRequestWithRetry`).

## Quan hệ
- **Gọi sang:** Order (qua `UrlCallback` lưu trong session `LogRequestQrcode`).
- **Dữ liệu (entities):** `LogRequestQrcode` (session QR + trạng thái thanh toán), `TkNganHangKetNoiOnline` (map TerminalId → STK/chủ TK).
- **Ripple / đồng bộ:** webhook → cập nhật trạng thái QR → đẩy thanh toán sang Order. Production dùng `MerchantOrderController/PaymentReceive` thay cho controller này.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WebhookBank/BankReceive.API/Controllers/BankSendController.cs:28,102` — 2 GET webhook KHÔNG có attribute auth (controller cũng không) → **anonymous; nhận tiền/mutate payment state**. Chỉ không thành lỗ hổng vì IsProduction chặn (L33-36, L107-110).
- `BankSendController.cs:79,153` — gọi `SendRequestWithRetry` nhưng bọc trong `try{}catch(Exception){}` rỗng (L82-85, L156-159) → lỗi gửi callback bị nuốt; Order có thể miss thanh toán mà không log.
- PaymentUpdate vs PaymentUpdate2: code trùng lặp gần như 100% (chỉ tách route) — sửa logic phải sửa cả 2.
- `(double)session.TransactionAmount.Value` (L61,135) — NPE nếu `TransactionAmount` null.
