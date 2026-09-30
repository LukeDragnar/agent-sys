---
controller: SaleOrderDonHangChiTietController
module: san-xuat
service: SanXuat
man_hinh_ui: "nội bộ / phụ trợ — thao tác chi tiết dòng đơn đặt hàng sản xuất (thường gọi trong màn Đơn đặt hàng sản xuất, không phải menu riêng)"
muc_dich: "CRUD trực tiếp dòng chi tiết đơn đặt hàng sản xuất (SaleOrderDonHangChiTiet) — thao tác cấp dòng tách khỏi header SaleOrderDonHang."
alias: ["chi tiết đơn đặt hàng sản xuất", "dòng đơn hàng SX", "SaleOrderDonHangChiTiet", "sale order detail"]
auth: public                                   # controller [ApiController]; các endpoint key đều bị comment → chỉ cần đăng nhập (controller-level mặc định)
state_machine: [TrangThai(0=khởi tạo, 2=đang sản xuất)]
deprecated: false
entities: [SaleOrderDonHangChiTiet]
stored_procs: []
depends: [SanXuat]
cross_module_in: []
cross_module_out: []
---

# SaleOrderDonHangChiTietController — Chi tiết đơn đặt hàng sản xuất (CRUD dòng)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm dòng | (nội bộ) | POST `api/SaleOrderDonHangChiTiet/ThemMoi` | key comment (`SaleOrderDonHangChiTiet_ThemMoi`) | public | - | ModelState lỗi → trả validate; map DTO→entity; InsertAsync | EF | key bị comment |
| 2 | Sửa dòng | (nội bộ) | POST `api/SaleOrderDonHangChiTiet/CapNhatThongTin` | key comment | public | dòng tồn tại | Map DTO→entity; GetById null → "Không tìm thấy"; UpdateAsync | EF | key bị comment |
| 3 | Xóa dòng | (nội bộ) | POST `api/SaleOrderDonHangChiTiet/Xoa/{id}` | key comment | public | - | GetById null → "Không tìm thấy"; DeleteAsync → trả true | EF | key bị comment |
| 4 | Xem chi tiết | (nội bộ) | GET `api/SaleOrderDonHangChiTiet/TimTheoId/{id}` | key comment | public | - | Trả 1 dòng theo id | EF | key bị comment |
| 5 | Tìm theo điều kiện | grid lọc | POST `api/SaleOrderDonHangChiTiet/TimTheoDieuKien` | (không có key) | public | - | Phân trang chi tiết theo filter | EF | - |

## Quan hệ
- **Gọi sang:** không (CRUD thuần qua repository).
- **Dữ liệu (entities):** `SaleOrderDonHangChiTiet {TrangThai(0/2), IsLock, SoLuongLock, IdSanPham, SoLuong}` — dòng con của `SaleOrderDonHang`.
- **Ripple / đồng bộ:** không. Thao tác header/lock/tổng hợp được làm ở `SaleOrderDonHangController`.

## Lỗi đã biết / lưu ý kỹ thuật
- Toàn bộ permission key bị comment (controller L31/43/61/87) → chỉ cần đăng nhập là thao tác được; thiếu kiểm tra IdDonVi/IdChiNhanh (khác với BomSetupController) → cần xác nhận có ràng buộc tenant ở tầng service hay không.
