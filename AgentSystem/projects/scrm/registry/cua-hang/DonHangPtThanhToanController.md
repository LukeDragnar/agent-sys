---
controller: DonHangPtThanhToanController
module: cua-hang
service: Order
man_hinh_ui: "Cửa hàng — chỉnh sửa phương thức thanh toán của đơn ĐÃ thanh toán (kế toán): màn chi tiết hóa đơn / chỉnh sửa dữ liệu đơn hàng (`pages/Order/thungan/ds-thungan/chinh-sua-du-lieu-donhang`) — đổi PTTT + xem lịch sử thay đổi. FE service `donhang-phuongthucthanhtoan.ts`."
muc_dich: "Quản lý phương thức thanh toán (PTTT) của đơn: tạo bản ghi thanh toán không gắn đơn, cập nhật PTTT cho đơn đã thanh toán (nghiệp vụ kế toán sửa sai PTTT), và xem lịch sử thay đổi PTTT."
alias: ["phương thức thanh toán", "sửa phương thức thanh toán", "đổi pttt", "lịch sử thay đổi pttt", "cập nhật pttt đơn đã thanh toán"]
auth: public
state_machine: []
deprecated: false
entities: [DonHangPtThanhToan, DonHang, DonHangPtThanhToanHistory]
stored_procs: []
depends: [Order DonHang]
cross_module_in: []
cross_module_out: []
---

# DonHangPtThanhToanController — Phương thức thanh toán đơn hàng

> Controller-level `[SBAuthorize()]` (login). Methods `virtual`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo PTTT không có đơn | (nội bộ/đặc thù) | POST `api/DonHangPtThanhToan/CreateWithNoOrder` | `DonHangPtThanhToan_CreateWithNoOrder` | public | - | Tạo danh sách bản ghi `DonHangPtThanhToan` không gắn đơn hàng (case đặc biệt — thanh toán độc lập) | EF | - |
| 2 | Cập nhật PTTT đơn đã thanh toán | "Đổi phương thức thanh toán" | POST `api/DonHangPtThanhToan/UpdatePtttDonHangDaThanhToan` | `DonHangPtThanhToan_UpdatePtttDonHangDaThanhToan` | public | đơn DaThanhToan | Kế toán sửa PTTT cho đơn đã chốt (DV-AD-TONG/DV-KT/DV-KT-TONG); ghi lịch sử thay đổi | EF + history | - |
| 3 | Lịch sử thay đổi PTTT | "Lịch sử thay đổi" | POST `api/DonHangPtThanhToan/GetLichSuThayDoiPTTTDonHang` | `DonHangPtThanhToan_GetLichSuThayDoiPTTTDonHang` | public | - | List lịch sử các lần đổi PTTT của đơn (ai/khi nào/từ→đến) | EF | - |

## Quan hệ
- **Dữ liệu (entities):** `DonHangPtThanhToan` (PTTT của đơn: loại, số tiền), bảng lịch sử thay đổi PTTT, `DonHang`.
- **Ripple / đồng bộ:** đổi PTTT đơn đã thanh toán ảnh hưởng báo cáo doanh thu theo PTTT (kế toán) — nghiệp vụ giới hạn role DV-KT.

## Lỗi đã biết / lưu ý kỹ thuật
- `UpdatePtttDonHangDaThanhToan` sửa dữ liệu tài chính của đơn ĐÃ chốt → chỉ mở cho role kế toán; cần đảm bảo ghi history đầy đủ (audit).
- Code thật: BE `Order/Order.API/Controllers/DonHangPtThanhToanController.cs` + `IDonHangPtThanhToanService`; rollback liên quan ở `Order.Service/Generated/DonHangPttt/DonHangPtttRollback.cs`; FE `services/order/donhang-phuongthucthanhtoan.ts`.
