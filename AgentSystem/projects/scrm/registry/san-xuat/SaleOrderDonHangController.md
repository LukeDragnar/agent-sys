---
controller: SaleOrderDonHangController
module: san-xuat
service: SanXuat
man_hinh_ui: "Sản xuất > Đơn đặt hàng sản xuất (Sale Order) — danh sách & nhập đơn đặt hàng làm đầu vào cho yêu cầu sản xuất; lock (giữ) số lượng sản phẩm để dành sản xuất"
muc_dich: "Quản lý đơn đặt hàng sản xuất (header SaleOrderDonHang + chi tiết SaleOrderDonHangChiTiet): thêm/sửa/xóa đơn, tổng hợp số lượng, lock sản phẩm (giữ chỗ tồn kho qua SanPhamLock) và là nguồn để tạo Production Request."
alias: ["đơn đặt hàng sản xuất", "sale order", "đơn hàng sản xuất", "SaleOrderDonHang", "lock sản phẩm", "giữ chỗ sản phẩm", "đơn đặt hàng"]
auth: public                                   # controller [SBAuthorize]; phần lớn endpoint có key riêng, 1 endpoint [SBAuthorize()] rỗng
state_machine: [SaleOrderDonHang.TrangThaiPR(0=chưa tạo PR, 1=đã tạo PR), ChiTiet.TrangThai(0=khởi tạo, 2=đang sản xuất)]
deprecated: false
entities: [SaleOrderDonHang, SaleOrderDonHangChiTiet, SanPhamLock]
stored_procs: []
depends: [SanXuat, IGenCodeService, IWarehouseProductService (HTTP — GetTonKhoByListSanPham, DropdownSanPhamSanXuat)]
cross_module_in: [SaleOrderProductionController (PR đọc chi tiết đơn để tạo Production Request, set ChiTiet.TrangThai=2)]
cross_module_out: [WarehouseProduct/Kho (chỉ ĐỌC tồn kho khi tìm/lock — không ghi)]
---

# SaleOrderDonHangController — Đơn đặt hàng sản xuất (Sale Order)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm đơn | Thêm mới | POST `api/SaleOrderDonHang/ThemMoi` | `SaleOrderDonHang_ThemMoi` (AD-TONG/AD-CN/SANXUAT) | public | màn nhập đơn | Sinh Ma; tạo header + chi tiết, tính `TongSoLuong`; transaction | EF | InsertAsync (SaleOrderDonHangService.cs:273) |
| 2 | Sửa đơn | Lưu | POST `api/SaleOrderDonHang/CapNhatThongTin` | `SaleOrderDonHang_CapNhatThongTin` | public | đơn tồn tại | Update header; **xóa hết chi tiết rồi insert lại** (delete-all-reinsert) | EF | UpdateAsync (:186) — reinsert nên IdChiTiet đổi |
| 3 | Xóa (chi tiết / đơn) | Xóa | POST `api/SaleOrderDonHang/Xoa/{id}` | `SaleOrderDonHang_Xoa` | public | - | Xóa 1 chi tiết, tính lại `TongSoLuong`; nếu hết chi tiết → xóa luôn đơn | EF | DeleteAsync (:126) — id là chi tiết, không phải header |
| 4 | Lock sản phẩm | Lock sản phẩm | POST `api/SaleOrderDonHang/LockSanPham` | `SaleOrderDonHang_LockSanPham` | public | màn đơn | Giữ chỗ tồn kho: set `IsLock=true` + `SoLuongLock` trên chi tiết; tổng hợp số lock theo SP qua mọi đơn → upsert `SanPhamLock` | EF | LockSanPham (:500) |
| 5 | Danh sách đơn | Danh sách đơn đặt hàng | POST `api/SaleOrderDonHang/DanhSach` | `SaleOrderDonHang_DanhSach` | public | màn list | Phân trang đơn + chi tiết lồng | EF | GetDanhSachDonHangAsync (:70) |
| 6 | Tìm theo điều kiện | grid lọc | POST `api/SaleOrderDonHang/TimTheoDieuKien` | `SaleOrderDonHang_TimTheoDieuKien` | public | màn list | Tìm; join chi tiết `TrangThai==0`; gọi Warehouse `GetTonKhoByListSanPham` + `DropdownSanPhamSanXuat` (đọc tồn) | EF + HTTP Warehouse | TimTheoDieuKienAsync (:339) |
| 7 | Xem chi tiết (để sửa) | mở đơn | GET `api/SaleOrderDonHang/TimTheoId/{id}` | `SaleOrderDonHang_TimTheoId` | public | - | Trả 1 đơn + toàn bộ chi tiết (GetByIdForEditAsync) | EF | :578 |
| 8 | Lịch sử theo điều kiện | lịch sử | POST `api/SaleOrderDonHang/TimTheoDieuKienLichSu` | `SBAuthorize()` (chỉ đăng nhập) | public | - | Tìm lịch sử đơn theo filter | EF | key bị comment, để rỗng |

## Quy tắc nghiệp vụ (state — blueprint để trace)
1. **TrangThaiPR (header):** `0` chưa tạo Production Request → `1` đã tạo PR (set bởi SaleOrderProductionController).
2. **ChiTiet.TrangThai:** `0` khởi tạo → `2` đang sản xuất (khi PR được tạo); xóa PR → reset về `0`. Tìm/lock chỉ xét chi tiết `TrangThai==0`.
3. **Lock:** `IsLock=true` + `SoLuongLock` trên chi tiết; `SanPhamLock` = tổng số lock theo SP toàn hệ thống → là cơ sở giữ chỗ tồn kho trước khi sản xuất.
4. **Update = delete-all-reinsert** chi tiết → mọi reference theo IdChiTiet cũ sẽ mất.

## Quan hệ
- **Gọi sang:** Warehouse/Kho (chỉ đọc tồn kho khi tìm/lock); IGenCodeService (sinh Ma đơn).
- **Dữ liệu (entities):** `SaleOrderDonHang {Ma, TongSoLuong, TrangThaiPR}`, `SaleOrderDonHangChiTiet {TrangThai, IsLock, SoLuongLock}`, `SanPhamLock`.
- **Ripple / đồng bộ:** không ghi sang Warehouse; lock chỉ ghi nội bộ (`SanPhamLock`). Không RabbitMQ.

## Lỗi đã biết / lưu ý kỹ thuật
- `Xoa/{id}` nhận id **chi tiết** (không phải header) — xóa chi tiết cuối mới xóa đơn (SaleOrderDonHangService.cs:126).
- `CapNhatThongTin` xóa-hết-insert-lại chi tiết (:186) → IdChiTiet thay đổi, cẩn trọng nếu nơi khác giữ id cũ.
- `TimTheoDieuKienLichSu` để `[SBAuthorize()]` rỗng (controller L135), key thật comment → chỉ cần đăng nhập.
