# Dev — Lịch sử sản xuất

> Service: **SanXuat** (`SanXuat.API` / `SanXuat.Service`) · FE module **sanxuat** (3 màn lịch sử). Chỉ dành cho dev.
> Trace registry: dùng chung node `registry/san-xuat/SaleOrderDonHangController.md`, `SaleOrderProductionController.md`, `WorkOrderController.md` (endpoint lịch sử là `TimTheoDieuKienLichSu` / `GetLichSu` của các controller này).

## Màn hình & route (FE)
| Màn hình | Route | Component / folder |
|---|---|---|
| Lịch sử đơn đặt hàng | `/sanxuat/lich-su-donhang` | `saleorder-donhang-lichsu/saleorder-donhang-lichsu.component` |
| Lịch sử sản xuất (PR / Production Order) | `/sanxuat/lich-su-sanxuat` | `saleorder-lich-su/saleorder-lich-su.component` |
| Lịch sử lệnh công việc (WO) | `/sanxuat/lich-su-congviec` | `workorder-lich-su/workorder-lich-su.component` |

- Route con: `sanxuat-routing.module.ts:31` (lich-su-donhang), `:37` (lich-su-sanxuat), `:41` (lich-su-congviec).
- Màn WO lịch sử: filter Trạng thái + Trạng thái sản xuất (dropdown từ enum), modal "Chi tiết nguyên vật liệu cần", helper màu `getTrangThaiColor`/`getTrangThaiSanXuatColor`.

## Endpoint / Controller (trace registry)
| Màn | Endpoint | Quyền |
|---|---|---|
| Lịch sử đơn đặt hàng | `POST api/SaleOrderDonHang/TimTheoDieuKienLichSu` | `[SBAuthorize()]` rỗng (key comment) |
| Lịch sử PR | `POST api/SaleOrderProduction/TimTheoDieuKienLichSu` | `[SBAuthorize()]` rỗng (key comment) |
| Lịch sử WO | `POST api/WorkOrder/TimTheoDieuKien` (+ `GetLichSu`) | `WorkOrder_TimTheoDieuKien` / `GetLichSu` `[SBAuthorize()]` |
| Xóa PR (chưa tách) | `POST api/SaleOrderProduction/Xoa/{id}` | `SaleOrderProduction_Xoa` |
| Xóa WO | `POST api/WorkOrder/Xoa/{id}` | `[SBAuthorize()]` rỗng |
| Chi tiết NVL cần (modal WO) | `POST api/WorkOrder/GetWOSanPhamCan` | `WorkOrder_GetWOSanPhamCan` |

## Entities
- Đọc `SaleOrderDonHang`/`SaleOrderDonHangChiTiet`, `SaleOrderProduction`, `WorkOrder`/`WorkOrderStep`/`WorkOrderStepProduct` (xem feature `quan-ly-san-xuat` / registry để biết schema đầy đủ).
- Trạng thái: `WOTrangThaiEnums {ChuaBatDau=0, ChuaXepLich=1, DaXepLich=2}`, `WOTrangThaiSanXuatEnums {ChuaBatDau=0, DangSanXuat=1, TamDung=2, Huy=3, HoanThanh=4}` (`sanxuat/models/sanxuat-enum.ts`).

## Luồng code
- 3 màn lịch sử = list read-only, gọi `TimTheoDieuKien(LichSu)` của controller tương ứng (phân trang theo filter + CurrentUser). Modal NVL cần dùng `GetWOSanPhamCan` (join BomSetup+BomProduct Loai=0, gọi Warehouse `GetTonKhoVaDonViTinhByListSanPham`, `SoLuongThieu=max(0, can−tonKho)`).
- Nút Xóa tái sử dụng đúng service xóa của Quản lý sản xuất.

## Bug-prone / lưu ý (ghi nhận, KHÔNG sửa — để session security)
- **Endpoint lịch sử (`TimTheoDieuKienLichSu`) để `[SBAuthorize()]` rỗng** (key thật comment) ở cả SaleOrderDonHang và SaleOrderProduction → chỉ cần đăng nhập là tra được; cần xác nhận có chủ ý (rủi ro lộ dữ liệu sản xuất giữa tenant nếu service không lọc IdDonVi/IdChiNhanh — cần kiểm thêm tầng service).
- **Xóa WO từ màn lịch sử = cùng `DeleteAsyncV2`** → KHÔNG hoàn/void phiếu kho (lỗi đã biết, xem `quan-ly-san-xuat/dev.md`).
- **Lọc TrangThaiSanXuat=null vs 0**: khi un-schedule WO set TrangThaiSanXuat=null; filter "ChuaBatDau(0)" có thể không bắt các bản null → đối chiếu cẩn thận.

## File source (path:line)
- BE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\SanXuat\SanXuat.API\Controllers\SaleOrderDonHangController.cs` (TimTheoDieuKienLichSu, L135 key comment), `SaleOrderProductionController.cs` (TimTheoDieuKienLichSu), `WorkOrderController.cs` (TimTheoDieuKien, GetLichSu, GetWOSanPhamCan, Xoa).
- BE service: `...\SanXuat\SanXuat.Service\` (`SaleOrderProductionService.cs` DeleteAsync :49, `WorkOrderService.cs` GetWOSanPhamCan :584, DeleteAsyncV2 :2220).
- FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\sanxuat\saleorder-donhang-lichsu\`, `saleorder-lich-su\`, `workorder-lich-su\` (+ `sanxuat-routing.module.ts:31/:37/:41`).
