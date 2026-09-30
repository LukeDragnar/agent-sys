# Báo cáo doanh số — Kỹ thuật

> Service: **Report** (BE .NET) · FE module **Report** (Angular 15 + ng-zorro). Chỉ dành cho dev. Tất cả báo cáo **THUẦN ĐỌC** qua stored proc DB `SCRM_Report` (`db.Ado.UseStoredProcedure().GetDataSetAll(...)` trên `_dbDapper.CopyNew()` hoặc `OpenNewConnectionOrDefault` cho báo cáo nặng). Không EF/LINQ trên entity.

## Màn hình & route (FE)
Prefix module: `app-routing.module.ts:212` (`path: 'report'` → lazy `ReportModule`). Route con khai báo ở `Report/report-routing.module.ts`.

| Màn hình | Route | Component |
|---|---|---|
| Doanh thu theo ngày | `/report/daily` | `ReportDailyComponent` |
| Doanh thu theo chi nhánh | `/report/chi-nhanh` | `ReportChiNhanhComponent` |
| Doanh thu theo khách hàng | `/report/customers` | `ReportCustomerComponent` |
| Doanh thu thu ngân | `/report/cashiers` | `ReportCashiersComponent` |
| Doanh thu theo sản phẩm | `/report/product` | `ReportProductComponent` |
| Báo cáo bán SP theo ngày | `/report/report-san-pham-ban-theo-ngay` | `ReportSanPhamBanTheoNgayComponent` |
| Doanh thu theo nhân viên | `/report/operator` | `ReportOperatorComponent` |
| Doanh số tổng quan (online) | `/report/report-doanh-so-tong-quat` | `ReportDoanhSoTongQuanComponent` |
| Doanh số theo giao vận | `/report/report-doanh-so-theo-giao-van` | `ReportDoanhSoTheoGiaoVanComponent` |
| Tổng hợp bán hàng | `/report/report-tong-hop-ban-hang` | `ReportTongHopBanHangComponent` |
| Báo cáo bán sản phẩm | `/report/san-pham-ban-hang` | `ReportBaoCaoSanPhamBanHangComponent` |
| Báo cáo doanh thu cửa hàng (tổng thể) | `/report/report-tong-the-cua-hang` | `ReportTongTheCuaHangComponent` |

## Endpoint / Controller (trace registry)
Trace registry: `registry/bao-cao/OrderReportController.md`, `registry/bao-cao/BaoCaoCuaHangController.md`, `registry/bao-cao/BaoCaoOnlineController.md`.

**OrderReportController** (`TI_Report_Chung`, key `OrderReport_*`, public):
| Endpoint | Quyền (key) | SP |
|---|---|---|
| `POST api/OrderReport/OrderReportDaily` | `OrderReport_OrderReportDaily` | `PRC_SCRM_OrderReport_Daily` (+ phụ `PRC_SCRM_WarehouseProduct_NhanSanPham`) |
| `POST api/OrderReport/OrderReportChiNhanh` | `OrderReport_OrderReportChiNhanh` | `PRC_SCRM_OrderReport_ChiNhanh` (+ NhanSanPham) |
| `POST api/OrderReport/OrderReportCustomer` | `OrderReport_OrderReportCustomer` | `PRC_SCRM_OrderReport_Customers` |
| `POST api/OrderReport/OrderReportCashiers` | `OrderReport_OrderReportCashiers` | `PRC_SCRM_OrderReport_ThuNgan` |
| `POST api/OrderReport/ReportDonHangSanPhamBanHang(+Group)` | `OrderReport_ReportDonHangSanPhamBanHang(+_Group)` | `sp_ReportDonHangSanPham_BanHang(+_Group)` |
| `POST api/OrderReport/ReportOrderReportProduct` | `OrderReport_ReportOrderReportProduct` | `PRC_SCRM_OrderReport_Products` (+ NhanSanPham) |
| `POST api/OrderReport/ReportOrderReportOperator` | `OrderReport_ReportOrderReportOperator` | `PRC_SCRM_OrderReport_Operators` |

**BaoCaoCuaHangController** (`TI_Report_CuaHang`, public — dùng chung `IOrderReportService`):
| Endpoint | Quyền (key) | SP |
|---|---|---|
| `POST api/BaoCaoCuaHang/OrderReportChiNhanh` | `BaoCaoCuaHangController_OrderReportChiNhanh` | `PRC_SCRM_OrderReport_ChiNhanh` |
| `POST api/BaoCaoCuaHang/OrderReportDaily` | `BaoCaoCuaHangController_OrderReportDaily` | `PRC_SCRM_OrderReport_Daily` |
| `POST api/BaoCaoCuaHang/OrderReportCustomer` | `BaoCaoCuaHangController_OrderReportCustomer` | `PRC_SCRM_OrderReport_Customers` |
| `POST api/BaoCaoCuaHang/ReportOrderReportProduct` | `BaoCaoCuaHangController_ReportOrderReportProduct` | `PRC_SCRM_OrderReport_Products` |

**BaoCaoOnlineController** (`TI_Report_Online`, public — báo cáo doanh số online):
| Endpoint | Quyền (key) | SP |
|---|---|---|
| `POST api/BaoCaoOnline/ReportDoanhSoTongQuan` (+ `ExportExcelReportDoanhSoTongQuan`) | `BaoCaoOnlineController_ReportDoanhSoTongQuan` (+ `_ExportExcel...`) | `SCRM_Report_ReportDoanhSoTongQuan` |
| `POST api/BaoCaoOnline/ReportDoanhSoTheoNgay` | **(chỉ [SBAuthorize], không key)** | `SCRM_Report_ActionDashBoard_BaoCaoTheoNgay` |
| `POST api/BaoCaoOnline/ReportDoanhSoTongQuan_SoLieuGiaoVanSale` | `BaoCaoOnlineController_..._SoLieuGiaoVanSale` | `SCRM_Report_ReportDoanhSoTongQuan_SoLieuGiaoVan` |
| `POST api/BaoCaoOnline/ReportDoanhSoTheoGiaoVan` (+ Export) | `BaoCaoOnlineController_ReportDoanhSoTheoGiaoVan` (dùng chung cho cả Export) | `SCRM_Report_ReportDoanhSoTheoGiaoVan` |
| `POST api/BaoCaoOnline/ReportTongHopBanHang` (+ Export) | `BaoCaoOnlineController_ReportTongHopBanHang` (+ Export) | `PRC_SCRM_Contact_BangTongHopBanHang` |

## Entities
- Không đọc/ghi entity nội bộ. 100% qua stored proc DB `SCRM_Report` (read-only).
- Nguồn dữ liệu gốc: đơn hàng (OrderLogistic) đã được tổng hợp sẵn vào DB báo cáo.

## Luồng code
Mỗi method báo cáo theo cùng khuôn:
1. Validate khoảng ngày: `CommonService.CheckCauHinhKhoangThoiGianChoPhepTimKiem` ≠ -1 → chặn nếu vượt.
2. Ép chi nhánh: user không phải admin/super/test/CSKH và `IdChiNhanh==Empty` → gán chi nhánh user.
3. Build `SugarParameter` → `db.Ado.UseStoredProcedure().GetDataSetAll(<SP>, ...)` trên `_dbDapper.CopyNew()`.
4. Báo cáo doanh thu (Daily/ChiNhanh/Products) gọi phụ SP `PRC_SCRM_WarehouseProduct_NhanSanPham` enrich tên SP.
5. Log query chậm `LogCommandV2(...)` khi `IsSuperUser || IsCSKH || GetStatusShowQueryReport(DonViId)==true`.
6. Export (online): build lại cùng SP rồi xuất `.xlsx` bằng GemBox; trả 503 nếu `Service_Unavailable`.

## Bug-prone / lưu ý (path:line)
- **`ReportDoanhSoTheoNgay` (BaoCaoOnlineController.cs:34)** dùng `[SBAuthorize]` không key (key cũ comment L33) → quyền chỉ theo auth chung, không có permission key riêng.
- **`ExportExcelReportDoanhSoTheoGiaoVan` (BaoCaoOnlineController.cs:127)** dùng CHUNG permission key với báo cáo `ReportDoanhSoTheoGiaoVan` — không tách quyền xuất riêng.
- **`ReportTongHopBanHang` (ReportService.cs:14948)** dùng `if (true || userLogin.IsSuperUser ...)` → LUÔN gọi `LogCommandV2` (điều kiện vô hiệu hóa) → ghi log mỗi request, overhead.
- **GetDataSetAll là call SYNC** của SqlSugar dù method async; SP nặng + `CopyNew` per request → cân nhắc hiệu năng với khoảng ngày dài.
- **Phong thần bảng / giao vận** dùng `OpenNewConnectionOrDefault` mở connection riêng (ReportService.cs:8286/8413/8556) cho store nặng.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.API\Controllers\OrderReportController.cs`, `...\BaoCaoCuaHangController.cs`, `...\BaoCaoOnlineController.cs`.
- BE service: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.Service\Generated\OrderReportService.cs` (ChiNhanh L473, Daily L260, Customers L571, Products L1058, Operators; NhanSanPham phụ L198/L415/L991/L2668), `...\ReportService.cs` (ReportDoanhSoTongQuan, ReportTongHopBanHang L14948).
- FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\Report\report-routing.module.ts` (các route ở bảng trên), thư mục component tương ứng dưới `pages\Report\`.
- SP (DB, không trong repo): `PRC_SCRM_OrderReport_*`, `sp_ReportDonHangSanPham_BanHang*`, `SCRM_Report_ReportDoanhSoTongQuan`, `SCRM_Report_ReportDoanhSoTheoGiaoVan`, `PRC_SCRM_Contact_BangTongHopBanHang`.
