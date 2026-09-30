---
controller: DonHangCauHinhGiamGiaController
module: cua-hang
service: Order
man_hinh_ui: "nội bộ — không có UI (controller bị comment toàn bộ, không build endpoint nào)"
muc_dich: "(Đã vô hiệu) Cấu hình giảm giá đơn hàng theo mức giá — toàn bộ class + endpoint bị comment trong file; không còn route nào hoạt động."
alias: ["cấu hình giảm giá đơn hàng", "giảm giá theo giá tiền"]
auth: internal
state_machine: []
deprecated: true
entities: [DonHangCauHinhGiamGia]
stored_procs: []
depends: []
cross_module_in: []
cross_module_out: []
---

# DonHangCauHinhGiamGiaController — Cấu hình giảm giá đơn hàng (ĐÃ VÔ HIỆU)

> **deprecated: true.** Toàn bộ nội dung file `DonHangCauHinhGiamGiaController.cs` (kể cả khai báo class, `[Route]`, `[ApiController]`, constructor và mọi action) đều bị **comment** — file chỉ còn namespace rỗng. Không có endpoint nào được đăng ký/route. AI có thể bỏ qua nhanh khi triage.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có endpoint active) | — | — | — | — | — | Controller bị comment toàn bộ | — | xem dưới |

## Lỗi đã biết / lưu ý kỹ thuật
- Class + 6 endpoint cũ (`TimTheoDieuKien`, `ThemMoi`, `CapNhatThongTin`, `TimTheoId`, `Xoa`, `GetConfigOrderByPrice`) đã bị comment hoàn toàn → không hoạt động. Logic giảm giá đơn hàng nay nằm ở luồng khác (voucher/KM trong `DonHangController`, chiết khấu theo bill ở `CauHinhChietKhauTheoBillController`).
- Entity `DonHangCauHinhGiamGia` (Order.DataContext) có thể vẫn tồn tại nhưng không có API phục vụ.
- Code thật: BE `Order/Order.API/Controllers/DonHangCauHinhGiamGiaController.cs` (toàn bộ comment).
