# Dev — Báo cáo & thống kê (cửa hàng)

> 2 controller (service **Report**): **OrderReportController** (báo cáo đơn hàng/doanh thu — hub lớn nhất) + **BranchReportController** (báo cáo chi nhánh/nhân viên, gồm hoa hồng NV). Đều THUẦN ĐỌC, 100% đi stored proc trên `_dbDapper.CopyNew()` (DB SCRM_Report). Chỉ dành cho dev.

## Endpoint/Controller (trace registry)

### OrderReportController
Node: `registry/bao-cao/OrderReportController.md`. `[TienIchAttribute(..., TI_Report_Chung)]`, không `[SBAuthorize]` controller-level; đa số method có key `OrderReport_*`. Gate chung mỗi method: validate khoảng ngày (`CheckCauHinhKhoangThoiGianChoPhepTimKiem` ≠ -1 → chặn) + ép chi nhánh + log query chậm.

| Quyền (key) | Route | SP |
|---|---|---|
| `OrderReport_OrderReportDaily` | POST `api/OrderReport/OrderReportDaily` | `PRC_SCRM_OrderReport_Daily` (+ NhanSanPham) |
| `OrderReport_OrderReportChiNhanh` | `…/OrderReportChiNhanh` | `PRC_SCRM_OrderReport_ChiNhanh` |
| `OrderReport_OrderReportCustomer` | `…/OrderReportCustomer` | `PRC_SCRM_OrderReport_Customers` |
| `OrderReport_OrderReportCashiers` | `…/OrderReportCashiers` | `PRC_SCRM_OrderReport_ThuNgan` |
| `OrderReport_ReportDonHangSanPhamBanHang(_Group)` | `…/ReportDonHangSanPhamBanHang(Group)` | `sp_ReportDonHangSanPham_BanHang(_Group)` |
| `OrderReport_ReportOrderReportProduct` | `…/ReportOrderReportProduct` | `PRC_SCRM_OrderReport_Products` |
| `OrderReport_ReportOrderReportOperator` | `…/ReportOrderReportOperator` | `PRC_SCRM_OrderReport_Operators` |
| `OrderReport_CustomerReportProduct(_Detail)` | `…/CustomerReportProduct(Detail)` | `PRC_SCRM_CustomerReport_Products(_Detail)` |
| `OrderReport_OrderCustomerReportVisitors` | `…/OrderCustomerReportVisitors` | `PRC_SCRM_Order_CustomerReport_Visitors` |
| `OrderReport_CustomerOrderReportUsedCard` | `…/CustomerOrderReportUsedCard` | `PRC_SCRM_OrderReport_UsedCard` |
| `OrderReport_Dashboard*` (đếm/summary/doanh số/thu-chi/SP/combo/nạp thẻ/tỉ lệ lấp đầy) | `…/Dashboard*` | `sp_Dashboard*`, `sp_OrderReport*`, `PRC_SCRM_Order_BaoCaoTiLeLapDay` |
| `[PrivateSBAuthorize]` | `…/HistoriesUsingServiceCustomer` | `PRC_SCRM_Order_HistoriesUsingServiceCustomer` |

Method `*V2` (CEO dashboard) dùng `OpenNewConnectionOrDefault` — không expose qua controller này.

### BranchReportController (trace BC_HoaHong)
Node: `registry/bao-cao/BranchReportController.md`. `[TienIchAttribute(..., TI_Branch)]` + `[SBAuthorize]` controller-level (cần login). Báo cáo chấm công enrich Booking (khung giờ) + Core (nhóm nhân viên).

| # | Quyền (key) | Route | SP |
|---|---|---|---|
| 1 | `DashboardReportDoanhSoThuChi` | POST `api/BranchReport/DashboardReportDoanhSoThuChi` | `sp_BranchDashboardReportDoanhSoThuChi` |
| 2 | (key bị comment → chỉ login) | `…/DashboardReportHoaHongNhanVienApDung` | `PRC_SCRM_MajorReport_HoaHongNhanVienApDung` |
| 3 | `BranchReport_BaoCaoDoanhSoNhanVien` | `…/BaoCaoDoanhSoNhanVien` | `PRC_SCRM_MajorReport_BaoCaoDoanhSoNhanVien` |
| 4 | `SBAuthorize()` (login) | `…/BaoCaoDoanhSoNhanVien_ChiTiet` | `PRC_SCRM_MajorReport_BaoCaoDoanhSoNhanVien_ChiTiet` |
| 5 | `GioLamViecNhanVien_BaoCaoChamCongTongQuat` | `…/BaoCaoChamCongTongQuat` | `Report_BaoCaoChamCongTongQuat` (4 table) |
| 6 | `SBAuthorize()` (login) | `…/BaoCaoChamCongTongQuatUser` | `Report_BaoCaoChamCongTongQuat` |

## Entities / nguồn
- Không đọc/ghi entity nội bộ — 100% qua stored procedure DB SCRM_Report. Read-only, không bắn message/event.
- Phụ thuộc: Report `CommonService` (CheckCauHinhKhoangThoiGianChoPhepTimKiem, GetStatusShowQueryReport); Booking `KhungGio/LayTatCaTheoDonVi`; Core `NhomNhanVien/GetListByDonVi`.

## Luồng code
- Service: `OrderReportService`, `BranchReportService` (Report.Service/Generated). Thân chính `db.Ado.UseStoredProcedure().GetDataSetAll(...)` (call SYNC của SqlSugar), chỉ call phụ là await.
- Doanh số NV chi tiết (#4): build param cố định `DonHangTrangThai=DaThanhToan, LoaiDoiTuong=DichVu, LoaiHoaHong=HoaHongThucHien`, `@PageIndex = Page - 1` (0-based), total từ `@TotalRecord` OUTPUT.
- Chấm công (#5): resolve chi nhánh theo cơ cấu phòng ban → gọi Booking + Core → build 16 param → SP trả 4 table (total/data/ca được giao/ca nâng cao) → `FillDataBaoCaoChamCongTongQuat` map song song.

## Bug-prone / lưu ý (path:line) — KHÔNG sửa ở session này
- **BUG SP placeholder** — `OrderReportService.cs:2104` `DashboardOrderReportDichVu` exec `GetDataSetAll("PROC", ...)` + log `"PRC"` (L2102): tên SP chưa thay → endpoint biểu đồ dịch vụ **luôn lỗi runtime**. Cần sửa thành `sp_OrderReportDichVu`.
- **BranchReport hoa hồng NV mất permission key** — `BranchReportController.cs:35` `[SBAuthorize("DashboardReportHoaHongNhanVienApDung", ...)]` **bị comment** → endpoint #2 chỉ còn login controller-level (không key riêng).
- **2 method `[SBAuthorize()]` trống** — `BranchReportController.cs:57` (#4 chi tiết doanh số NV), `:76` (#6 chấm công user) → không kiểm permission key.
- **Log luôn chạy** — `BranchReportService.cs:611` `if (IsSuperUser || IsCSKH || true)` → vế `true` thừa, objectLog luôn ghi.
- **Hiệu năng** — SP nặng + connection `CopyNew` per request; `GetDataSetAll` call SYNC. Lưu ý khi tối ưu.
- `ReportOrderReportProductGiaoCa` (OrderReportController.cs:83-89) đã comment toàn bộ.

## File source (path:line)
- BE OrderReport: `…\scrm\Report\Report.API\Controllers\OrderReportController.cs`, service `…\Report\Report.Service\Generated\OrderReportService.cs` (L2102/2104 bug; L3480 OpenNewConnectionOrDefault).
- BE BranchReport: `…\scrm\Report\Report.API\Controllers\BranchReportController.cs` (L35/57/76), service `…\Report\Report.Service\Generated\BranchReportService.cs` (L477 PageIndex; L611 log).
- Stored procedures: nằm trong DB SCRM_Report (không trong repo source) — xem danh sách ở frontmatter node registry.
