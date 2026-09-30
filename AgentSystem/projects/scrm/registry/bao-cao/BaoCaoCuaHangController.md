---
controller: BaoCaoCuaHangController
module: bao-cao
service: Report
man_hinh_ui: "Báo cáo > Cửa hàng: báo cáo doanh thu (theo ngày, theo chi nhánh, theo khách hàng, theo sản phẩm), thống kê KH dùng sản phẩm, báo cáo tỉ lệ chuyển đổi lịch hẹn/doanh thu, thống kê doanh thu theo khung giờ, theo nguồn, báo cáo tác nghiệp và báo cáo theo nhãn. Bộ lọc: chi nhánh, từ/đến ngày, sản phẩm/dịch vụ, nhân viên, nguồn, paging."
muc_dich: "Mặt tiền (controller) báo cáo cửa hàng — 10 endpoint public, tất cả đọc qua stored proc qua IOrderReportService (cùng service backing OrderReportController). Gồm doanh thu/khách hàng/sản phẩm + báo cáo online (chuyển đổi lịch hẹn, khung giờ, nguồn, tác nghiệp, nhãn)."
alias: ["báo cáo cửa hàng", "bao cao cua hang", "báo cáo doanh thu cửa hàng", "báo cáo tác nghiệp", "báo cáo theo nhãn", "tỉ lệ chuyển đổi lịch hẹn", "thống kê theo khung giờ", "thống kê theo nguồn"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: [PRC_SCRM_OrderReport_ChiNhanh, PRC_SCRM_OrderReport_Daily, PRC_SCRM_OrderReport_Customers, PRC_SCRM_CustomerReport_Products, PRC_SCRM_OrderReport_Products, PRC_SCRM_OrderReport_LichHenDoanhThu, PRC_SCRM_OrderReport_ThongKeTheoKhungGio, PRC_SCRM_OrderReport_NguonDoanhThu_CuaHang, SCRM_Report_ReportCongViec_Offline, SCRM_Report_ReportNhanContact_Offline, PRC_SCRM_WarehouseProduct_NhanSanPham]
depends: [Report/CommonService (CheckCauHinhKhoangThoiGianChoPhepTimKiem, GetStatusShowQueryReport, GetTimeToLog), SqlSugar (CopyNew + GetDataSetAll stored proc), DB SCRM_Report]
cross_module_in: []
cross_module_out: []
---

# BaoCaoCuaHangController — Báo cáo cửa hàng (doanh thu + báo cáo online)

> Controller view/report THUẦN ĐỌC (10 endpoint public). Dùng chung `IOrderReportService` với `OrderReportController` — gọi tập con method báo cáo doanh thu + nhóm báo cáo "online" (chuyển đổi lịch hẹn, khung giờ, nguồn, tác nghiệp, nhãn). **TẤT CẢ đi stored proc** (`db.Ado.UseStoredProcedure().GetDataSetAll(...)`, DB SCRM_Report). `[TienIchAttribute(..., TI_Report_CuaHang)]`; mỗi method có `[SBAuthorize("Key", ...)]` riêng (public). Lưu ý: một số method dùng permission key tiền tố `BaoCaoOnlineController_*` (ReportChuyenDoiLichHenDoanhThu, ReportThongKeTheoKhungGio, ReportThongKeDoanhThuTheoNguon) — key thuộc nhóm "online" cũ. Gate chung mỗi method: validate khoảng ngày + ép chi nhánh user khi không admin + log query chậm khi super/CSKH/showQuery. Vài báo cáo (Daily, ChiNhanh, Products, ThongKeTheoKhungGio) gọi phụ SP `PRC_SCRM_WarehouseProduct_NhanSanPham` để enrich tên sản phẩm.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo cáo doanh thu chi nhánh | Lưới doanh thu/chi nhánh | `POST api/BaoCaoCuaHang/OrderReportChiNhanh` | `BaoCaoCuaHangController_OrderReportChiNhanh` | public | màn doanh thu chi nhánh | Validate ngày + ép chi nhánh. Chạy SP ChiNhanh (+ phụ NhanSanPham). | **SP** `PRC_SCRM_OrderReport_ChiNhanh` (+ NhanSanPham) | — |
| 2 | Báo cáo doanh thu theo ngày | Lưới doanh thu daily | `POST api/BaoCaoCuaHang/OrderReportDaily` | `BaoCaoCuaHangController_OrderReportDaily` | public | màn doanh thu | Như #1; chạy SP Daily (+ phụ NhanSanPham). | **SP** `PRC_SCRM_OrderReport_Daily` (+ NhanSanPham) | — |
| 3 | Báo cáo doanh thu theo khách hàng | Lưới doanh thu/KH | `POST api/BaoCaoCuaHang/OrderReportCustomer` | `BaoCaoCuaHangController_OrderReportCustomer` | public | màn doanh thu KH | Chạy SP Customers. | **SP** `PRC_SCRM_OrderReport_Customers` | — |
| 4 | Thống kê KH dùng sản phẩm | Lưới KH dùng SP | `POST api/BaoCaoCuaHang/CustomerReportProduct` | `BaoCaoCuaHangController_CustomerReportProduct` | public | màn KH dùng SP | Chạy SP CustomerReport_Products. | **SP** `PRC_SCRM_CustomerReport_Products` | — |
| 5 | Báo cáo doanh thu theo sản phẩm | Lưới doanh thu/SP | `POST api/BaoCaoCuaHang/ReportOrderReportProduct` | `BaoCaoCuaHangController_ReportOrderReportProduct` | public | màn doanh thu SP | Chạy SP Products (+ phụ NhanSanPham). | **SP** `PRC_SCRM_OrderReport_Products` (+ NhanSanPham) | — |
| 6 | Tỉ lệ chuyển đổi lịch hẹn/doanh thu | Lưới chuyển đổi | `POST api/BaoCaoCuaHang/ReportChuyenDoiLichHenDoanhThu` | `BaoCaoOnlineController_ReportChuyenDoiLichHenDoanhThu` | public | màn chuyển đổi | Báo cáo tỉ lệ chuyển đổi lịch hẹn → lead → doanh thu. Chạy SP. | **SP** `PRC_SCRM_OrderReport_LichHenDoanhThu` | key tiền tố `BaoCaoOnlineController_*`. |
| 7 | Thống kê doanh thu theo khung giờ | Lưới khung giờ | `POST api/BaoCaoCuaHang/ReportThongKeTheoKhungGio` | `BaoCaoOnlineController_ReportThongKeTheoKhungGio` | public | màn khung giờ | Chạy SP ThongKeTheoKhungGio (+ phụ NhanSanPham). | **SP** `PRC_SCRM_OrderReport_ThongKeTheoKhungGio` (+ NhanSanPham) | key tiền tố `BaoCaoOnlineController_*`. |
| 8 | Thống kê doanh thu theo nguồn | Lưới theo nguồn | `POST api/BaoCaoCuaHang/ReportThongKeDoanhThuTheoNguon` | `BaoCaoOnlineController_ReportThongKeDoanhThuTheoNguon` | public | màn theo nguồn | Chạy SP NguonDoanhThu. | **SP** `PRC_SCRM_OrderReport_NguonDoanhThu_CuaHang` | key tiền tố `BaoCaoOnlineController_*`. |
| 9 | Báo cáo tác nghiệp | Lưới tác nghiệp | `POST api/BaoCaoCuaHang/ReportTacNghiep` | `BaoCaoCuaHangController_ReportTacNghiep` | public | màn tác nghiệp | Báo cáo công việc/tác nghiệp offline. Chạy SP. | **SP** `SCRM_Report_ReportCongViec_Offline` | — |
| 10 | Báo cáo theo nhãn | Lưới theo nhãn | `POST api/BaoCaoCuaHang/ReportTheoNhan` | `BaoCaoCuaHangController_ReportNhan` | public | màn theo nhãn | Báo cáo nhãn contact offline. Chạy SP. | **SP** `SCRM_Report_ReportNhanContact_Offline` | key là `..._ReportNhan` (endpoint là `ReportTheoNhan`). |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Giới hạn khoảng ngày:** mọi báo cáo gate `CheckCauHinhKhoangThoiGianChoPhepTimKiem` ≠ -1 → chặn "Chỉ được xem dữ liệu trong khoảng 'N ngày'".
- **Ép chi nhánh:** user thường (không admin/super/test/CSKH) + `IdChiNhanh==Empty` → mặc định chi nhánh user.
- **Log query chậm:** `LogCommandV2(...)` khi `IsSuperUser || IsCSKH || GetStatusShowQueryReport==true`.
- **Enrich tên sản phẩm:** Daily/ChiNhanh/Products/ThongKeTheoKhungGio gọi phụ SP `PRC_SCRM_WarehouseProduct_NhanSanPham` rồi map vào kết quả.

## Quan hệ
- **Gọi sang:** Report `CommonService` (CheckCauHinhKhoangThoiGianChoPhepTimKiem, GetStatusShowQueryReport, GetTimeToLog).
- **Dữ liệu (entities):** không đọc/ghi entity nội bộ — qua stored proc DB SCRM_Report. Read-only.
- **Ripple / đồng bộ:** không bắn message/event.
- **Lưu ý:** trùng backing service `IOrderReportService` với `OrderReportController` (xem `OrderReportController.md`); các method ở đây là tập con expose qua menu Cửa hàng.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.Service/Generated/OrderReportService.cs` — các SP map theo dòng: ChiNhanh L473, Daily L260, Customers L571, CustomerReport_Products L1352, Products L1058, LichHenDoanhThu L2576, ThongKeTheoKhungGio L2704, NguonDoanhThu L2851, ReportCongViec_Offline L3162, ReportNhanContact_Offline L3391; SP phụ NhanSanPham L198/L415/L991/L2668.
- BE `Report/Report.API/Controllers/BaoCaoCuaHangController.cs`
  - 3 method (ReportChuyenDoiLichHenDoanhThu, ReportThongKeTheoKhungGio, ReportThongKeDoanhThuTheoNguon) dùng permission key tiền tố `BaoCaoOnlineController_*` thay vì `BaoCaoCuaHangController_*` (L63/L72/L80) — di sản từ controller online cũ.
  - L95-102 — endpoint `ReportTheoNhan` nhưng permission key là `BaoCaoCuaHangController_ReportNhan` (lệch tên endpoint/key).
  - L67 — `gridInfo.LoaiNhanVien = 1` đã comment trong ReportChuyenDoiLichHenDoanhThu.
