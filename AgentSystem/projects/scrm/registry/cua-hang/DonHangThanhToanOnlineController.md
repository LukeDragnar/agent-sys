---
controller: DonHangThanhToanOnlineController
module: cua-hang
service: Order
man_hinh_ui: "Cửa hàng / POS thu ngân — màn 'Danh sách thanh toán online' (route `pages/Order/thungan/danh-sach-thanh-toan-online`): tab danh sách đơn yêu cầu thanh toán online + tab lịch sử giao dịch online. FE service `donhangthanhtoanonline.service.ts`."
muc_dich: "Tra cứu (chỉ đọc) danh sách đơn hàng yêu cầu thanh toán online và lịch sử các giao dịch thanh toán online theo điều kiện lọc."
alias: ["danh sách thanh toán online", "lịch sử thanh toán online", "giao dịch vietqr", "đơn yêu cầu thanh toán online"]
auth: public
state_machine: []
deprecated: false
entities: [DonHangThanhToanOnline, DonHang]
stored_procs: []
depends: [Order DonHang]
cross_module_in: []
cross_module_out: []
---

# DonHangThanhToanOnlineController — Lịch sử thanh toán online

> Controller-level `[SBAuthorize]` (login). Cả 2 endpoint không gắn permission key riêng → chỉ cần đăng nhập.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Đơn yêu cầu thanh toán online | tab "Yêu cầu thanh toán" | POST `api/DonHangThanhToanOnline/TimTheoDieuKien` | login (controller-level) | public | màn list | List đơn đang yêu cầu thanh toán online theo bộ lọc (`TimKiemDonHangYeuCauThanhToanAsync`) | EF (service) | không có permission key |
| 2 | Lịch sử thanh toán online | tab "Lịch sử" | POST `api/DonHangThanhToanOnline/TimLichSuThanhToanTheoDieuKien` | login (controller-level) | public | màn list | List lịch sử giao dịch thanh toán online (`TimKiemLichSuThanhToanOnlineAsync`) | EF (service) | không có permission key |

## Quan hệ
- **Dữ liệu (entities):** `DonHangThanhToanOnline` (lịch sử giao dịch online — số tiền, nội dung, trạng thái khớp), `DonHang`.
- **Ripple / đồng bộ:** chỉ đọc; dữ liệu được sinh bởi luồng webhook của `PaymentController` (DataReceivce → consumer cập nhật).

## Lỗi đã biết / lưu ý kỹ thuật
- Cả 2 endpoint chỉ login-only (không key) — màn tra cứu giao dịch tiền nhưng không phân quyền theo role.
- Controller chỉ-đọc, không tạo/sửa; phát sinh dữ liệu nằm ở consumer webhook ngân hàng.
- Code thật: BE `Order/Order.API/Controllers/DonHangThanhToanOnlineController.cs` + `IDonHangThanhToanOnlineService`; FE `services/order/donhangthanhtoanonline.service.ts`, `pages/Order/thungan/danh-sach-thanh-toan-online`.
