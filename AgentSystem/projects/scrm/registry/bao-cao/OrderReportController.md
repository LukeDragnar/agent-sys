---
controller: OrderReportController
module: bao-cao
service: Report
man_hinh_ui: "Báo cáo > Doanh thu / Dashboard đơn hàng: nhóm báo cáo doanh thu (theo ngày, chi nhánh, khách hàng, thu ngân, sản phẩm/dịch vụ, nhân viên), báo cáo bán sản phẩm (chi tiết + group), thống kê KH dùng sản phẩm/thẻ dịch vụ, và loạt widget dashboard (đếm đặt lịch thanh toán, tổng doanh thu, doanh số, tài chính thu-chi, biểu đồ dịch vụ/sản phẩm/combo/nạp thẻ, tỉ lệ lấp đầy, lượng khách). Bộ lọc: chi nhánh, Từ/Đến ngày, sản phẩm/dịch vụ, nhân viên, paging."
muc_dich: "Hub báo cáo đơn hàng/doanh thu lớn nhất của Report-service: ~22 endpoint công khai, TẤT CẢ đọc qua stored proc (lưới báo cáo + widget dashboard). Một số báo cáo còn gọi phụ SP NhanSanPham để enrich tên sản phẩm."
alias: ["báo cáo doanh thu", "bao cao doanh thu", "báo cáo đơn hàng", "order report", "dashboard doanh số", "báo cáo bán sản phẩm", "báo cáo thu ngân", "tỉ lệ lấp đầy", "lịch sử dùng thẻ"]
auth: mixed
state_machine: []
deprecated: false
entities: []
stored_procs: [PRC_SCRM_OrderReport_Daily, PRC_SCRM_OrderReport_ChiNhanh, PRC_SCRM_OrderReport_Customers, PRC_SCRM_OrderReport_ThuNgan, sp_ReportDonHangSanPham_BanHang, sp_ReportDonHangSanPham_BanHang_Group, PRC_SCRM_OrderReport_Products, PRC_SCRM_OrderReport_Operators, PRC_SCRM_Order_CustomerReport_Visitors, PRC_SCRM_CustomerReport_Products, PRC_SCRM_CustomerReport_Products_Detail, PRC_SCRM_OrderReport_UsedCard, sp_DashboardGetCountDatLichThanhToan, sp_DashboardGetSummaryOrder, sp_DashboardDoanhSo, sp_DashboardReportDoanhSoThuChi, sp_OrderReportSanPham, sp_OrderReportCombo, sp_OrderReportNapThe, sp_OrderReportDetail, PRC_SCRM_Order_BaoCaoTiLeLapDay, PRC_SCRM_Order_HistoriesUsingServiceCustomer, PRC_SCRM_WarehouseProduct_NhanSanPham]
depends: [CommonService/CheckCauHinhKhoangThoiGianChoPhepTimKiem (giới hạn khoảng ngày), CommonService/GetStatusShowQueryReport (bật log query), SqlSugar (CopyNew + GetDataSetAll stored proc), Report DB SCRM_Report]
cross_module_in: []
cross_module_out: []
---

# OrderReportController — Báo cáo đơn hàng / doanh thu (hub báo cáo lớn nhất)

> Controller view/report THUẦN ĐỌC, **lớn nhất** trong Report-service (~22 endpoint public + nhiều method `*V2` nội bộ cho CEO dashboard). **TẤT CẢ đi stored proc** (`db.Ado.UseStoredProcedure().GetDataSetAll` trên `_dbDapper.CopyNew()`, DB SCRM_Report) — không có truy vấn EF/LINQ trên entity. `[TienIchAttribute(..., TI_Report_Chung)]`; không `[SBAuthorize]` controller-level. Hầu hết method có permission key riêng (`OrderReport_*`); `HistoriesUsingServiceCustomer` là `[PrivateSBAuthorize]` (internal). Gate chung mỗi method: validate khoảng ngày (`CheckCauHinhKhoangThoiGianChoPhepTimKiem` ≠ -1 → chặn) + ép chi nhánh user khi không phải admin/super/test/CSKH và không chọn chi nhánh; log query chậm khi super/CSKH/showQuery. Vài báo cáo doanh thu (Daily, ChiNhanh, Products, ThongKeTheoKhungGio) còn gọi phụ SP `PRC_SCRM_WarehouseProduct_NhanSanPham` để enrich tên sản phẩm.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo cáo doanh thu theo ngày | Lưới doanh thu daily | `POST api/OrderReport/OrderReportDaily` | `OrderReport_OrderReportDaily` | public | màn doanh thu | Validate ngày + ép chi nhánh. Gọi phụ SP NhanSanPham (enrich SP). Chạy SP chính, trả jsonData + total. | **SP** `PRC_SCRM_OrderReport_Daily` (+ phụ `PRC_SCRM_WarehouseProduct_NhanSanPham`) | — |
| 2 | Báo cáo doanh thu chi nhánh | Lưới doanh thu theo chi nhánh | `POST api/OrderReport/OrderReportChiNhanh` | `OrderReport_OrderReportChiNhanh` | public | màn doanh thu chi nhánh | Như #1; chạy SP ChiNhanh (+ phụ NhanSanPham). | **SP** `PRC_SCRM_OrderReport_ChiNhanh` (+ NhanSanPham) | — |
| 3 | Báo cáo doanh thu theo khách hàng | Lưới doanh thu/KH | `POST api/OrderReport/OrderReportCustomer` | `OrderReport_OrderReportCustomer` | public | màn doanh thu KH | Chạy SP Customers, trả jsonData + total. | **SP** `PRC_SCRM_OrderReport_Customers` | — |
| 4 | Báo cáo doanh thu thu ngân | Lưới doanh thu/thu ngân | `POST api/OrderReport/OrderReportCashiers` | `OrderReport_OrderReportCashiers` | public | màn thu ngân | Chạy SP ThuNgan. | **SP** `PRC_SCRM_OrderReport_ThuNgan` | — |
| 5 | Báo cáo bán sản phẩm (chi tiết / group) | Lưới bán sản phẩm | `POST api/OrderReport/ReportDonHangSanPhamBanHang`, `.../ReportDonHangSanPhamBanHangGroup` | `OrderReport_ReportDonHangSanPhamBanHang`, `..._Group` | public | màn bán sản phẩm | 2 biến thể (chi tiết / gom nhóm). Chạy SP tương ứng. | **SP** `sp_ReportDonHangSanPham_BanHang`, `sp_ReportDonHangSanPham_BanHang_Group` | — |
| 6 | Báo cáo doanh thu theo sản phẩm | Lưới doanh thu/SP | `POST api/OrderReport/ReportOrderReportProduct` | `OrderReport_ReportOrderReportProduct` | public | màn doanh thu SP | Chạy SP Products (+ phụ NhanSanPham). | **SP** `PRC_SCRM_OrderReport_Products` (+ NhanSanPham) | — |
| 7 | Báo cáo doanh thu theo nhân viên | Lưới doanh thu/NV | `POST api/OrderReport/ReportOrderReportOperator` | `OrderReport_ReportOrderReportOperator` | public | màn doanh thu NV | Chạy SP Operators. | **SP** `PRC_SCRM_OrderReport_Operators` | — |
| 8 | Thống kê KH dùng sản phẩm (+ chi tiết) | Lưới KH dùng SP | `POST api/OrderReport/CustomerReportProduct`, `.../CustomerReportProductDetail` | `OrderReport_CustomerReportProduct`, `..._Detail` | public | màn KH dùng SP | 2 biến thể tổng/chi tiết. Chạy SP tương ứng. | **SP** `PRC_SCRM_CustomerReport_Products`, `..._Detail` | — |
| 9 | Lượng khách hàng (visitors) | Biểu đồ lượng KH | `POST api/OrderReport/OrderCustomerReportVisitors` | `OrderReport_OrderCustomerReportVisitors` | public | màn lượng khách | Chạy SP visitors. | **SP** `PRC_SCRM_Order_CustomerReport_Visitors` | — |
| 10 | Lịch sử dùng thẻ dịch vụ | Lưới lịch sử dùng thẻ | `POST api/OrderReport/CustomerOrderReportUsedCard` | `OrderReport_CustomerOrderReportUsedCard` | public | màn lịch sử thẻ | Chạy SP UsedCard. | **SP** `PRC_SCRM_OrderReport_UsedCard` | — |
| 11 | Dashboard đếm đặt lịch thanh toán | Widget đếm | `POST api/OrderReport/DashboardGetCountDatLichThanhToan` | `OrderReport_DashboardGetCountDatLichThanhToan` | public | dashboard | Chạy SP đếm. | **SP** `sp_DashboardGetCountDatLichThanhToan` | — |
| 12 | Dashboard tổng doanh thu / doanh số / tài chính | Widget tổng | `POST api/OrderReport/DashboardGetSummaryOrder`, `.../DashboardDoanhSo`, `.../DashboardReportDoanhSoThuChi` | `OrderReport_DashboardGetSummaryOrder`, `..._DashboardDoanhSo`, `..._DashboardReportDoanhSoThuChi` | public | dashboard | 3 widget tổng hợp. Mỗi cái 1 SP. | **SP** `sp_DashboardGetSummaryOrder`, `sp_DashboardDoanhSo`, `sp_DashboardReportDoanhSoThuChi` | — |
| 13 | Dashboard biểu đồ dịch vụ | Biểu đồ số lượng dịch vụ | `POST api/OrderReport/DashboardOrderReportDichVu` | `OrderReport_DashboardOrderReportDichVu` | public | dashboard | Build param; chạy SP. | **SP** ⚠️ exec `"PROC"` (placeholder — đáng lẽ `sp_OrderReportDichVu`) | **BUG**: SP name là `"PROC"`/log `"PRC"` (L2104/L2102) → endpoint này luôn lỗi runtime. |
| 14 | Dashboard biểu đồ SP/combo/nạp thẻ/chi tiết | Biểu đồ số lượng | `POST api/OrderReport/DashboardOrderReportSanPham`, `.../DashboardOrderReportCombo`, `.../DashboardOrderReportNapThe`, `.../DashboardOrderReportDetail` | `OrderReport_DashboardOrderReportSanPham` ... `..._Detail` | public | dashboard | 4 biểu đồ. Mỗi cái 1 SP. | **SP** `sp_OrderReportSanPham`, `sp_OrderReportCombo`, `sp_OrderReportNapThe`, `sp_OrderReportDetail` | — |
| 15 | Báo cáo tỉ lệ lấp đầy | Widget tỉ lệ lấp đầy | `POST api/OrderReport/DashboardBaoCaoTiLeLapDay` | `OrderReport_DashboardBaoCaoTiLeLapDay` | public | dashboard | Chạy SP. | **SP** `PRC_SCRM_Order_BaoCaoTiLeLapDay` | — |
| 16 | Lịch sử dùng dịch vụ (internal) | (service-to-service) | `POST api/OrderReport/HistoriesUsingServiceCustomer` | `[PrivateSBAuthorize]` | internal | gọi nội bộ | Chạy SP, không gate khoảng ngày. | **SP** `PRC_SCRM_Order_HistoriesUsingServiceCustomer` | endpoint nội bộ (signature không nhận UserLogin). |

## Endpoint nội bộ (private / service-to-service)
- [internal] `HistoriesUsingServiceCustomer` (`[PrivateSBAuthorize]`) → SP `PRC_SCRM_Order_HistoriesUsingServiceCustomer` — phục vụ service khác lấy lịch sử dùng dịch vụ của KH (không có UserLogin).
- Service còn nhiều method **không expose qua controller này**: bộ `*V2` (OrderReportDailyV2, OrderReportCustomerV2, OrderReportCashiersV2, CustomerReportProductV2, DashboardGetCountDatLichThanhToanV2, DashboardGetSummaryOrderV2, DashboardDoanhSoV2, DashboardReportDoanhSoThuChiV2, DashboardOrderReportDetailV2, BookingReportBookingsV2) dùng `OpenNewConnectionOrDefault` (mở connection riêng) cho CEO dashboard; `ReportChuyenDoiLichHenDoanhThu` (SP `PRC_SCRM_OrderReport_LichHenDoanhThu`), `ReportThongKeTheoKhungGio` (SP `PRC_SCRM_OrderReport_ThongKeTheoKhungGio` + NhanSanPham), `ReportThongKeDoanhThuTheoNguon` (SP `PRC_SCRM_OrderReport_NguonDoanhThu_CuaHang`), `ReportTacNghiep` (SP `SCRM_Report_ReportCongViec_Offline`), `ReportTheoNhan` (SP `SCRM_Report_ReportNhanContact_Offline`).

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Giới hạn khoảng ngày:** mọi báo cáo gate `CheckCauHinhKhoangThoiGianChoPhepTimKiem` ≠ -1 → chặn "Chỉ được xem dữ liệu trong khoảng 'N ngày'".
- **Ép chi nhánh:** user thường (không admin/super/test/CSKH) + `IdChiNhanh==Empty` → mặc định chi nhánh user.
- **Log query chậm:** `objectLog = LogCommandV2(...)` khi `IsSuperUser || IsCSKH || GetStatusShowQueryReport(DonViId)==true`.
- **Enrich tên sản phẩm:** một số báo cáo gọi phụ SP `PRC_SCRM_WarehouseProduct_NhanSanPham` rồi map vào kết quả.

## Quan hệ
- **Gọi sang:** Report `CommonService` (CheckCauHinhKhoangThoiGianChoPhepTimKiem, GetStatusShowQueryReport).
- **Dữ liệu (entities):** không đọc/ghi entity nội bộ — 100% qua stored proc DB SCRM_Report (22 SP chính + 1 SP phụ NhanSanPham). Read-only.
- **Ripple / đồng bộ:** không bắn message/event.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.Service/Generated/OrderReportService.cs`
  - **L2104 — BUG SP placeholder:** `DashboardOrderReportDichVu` exec `GetDataSetAll("PROC", ...)` và log `LogCommandV2(db, "PRC", ...)` (L2102) — tên SP placeholder chưa thay → endpoint #13 luôn lỗi runtime. Cần sửa thành `sp_OrderReportDichVu`.
  - Nhiều method async nhưng thân chính `GetDataSetAll` là call SYNC của SqlSugar; chỉ các call phụ (CommonService, SP NhanSanPham) là await — lưu ý hiệu năng SP nặng + connection `CopyNew` per request.
  - `OpenNewConnectionOrDefault` (L3480) mở `SqlSugarClient` mới bằng connection string `SCRM_ReportStr` cho các biến thể `*V2` (CEO dashboard) — không qua controller này.
- BE `Report/Report.API/Controllers/OrderReportController.cs`
  - `ReportOrderReportProductGiaoCa` (L83-89) đã comment toàn bộ. `HistoriesUsingServiceCustomer` (L200) `[PrivateSBAuthorize]` → internal.
