---
controller: BaoCaoCeoController
module: bao-cao
service: Report
man_hinh_ui: "Báo cáo > Dashboard CEO: nhóm báo cáo cấp lãnh đạo gồm báo cáo offline (doanh thu chi nhánh/cửa hàng/sản phẩm, lịch hẹn, tỉ lệ lấp đầy, lượng khách), báo cáo online (phễu online, số liệu chung, bảng xếp hạng sale/MKT, số liệu giao vận, công việc sale), báo cáo chung (chấm công tổng quát/top, tổng đài), và loạt báo cáo doanh số/nhãn-trạng thái contact theo nguồn/ngày/sản phẩm/chia lead. Bộ lọc chung qua ReportCeoFilter (chi nhánh, Từ/Đến ngày, cơ cấu tổ chức)."
muc_dich: "Hub Dashboard CEO của Report-service: ~30 endpoint public, gom báo cáo cấp lãnh đạo từ nhiều service (ReportCeoService, KhachHangReportService, ReportService, ReportCeoCommonService). Hầu hết chạy SP SCRM_Report_ReportDashboardCEO_* qua OpenNewConnectionOrDefault. GetAllReportCeo là aggregator gọi lại nhiều SP CEO trong 1 lần."
alias: ["báo cáo ceo", "bao cao ceo", "dashboard ceo", "báo cáo lãnh đạo", "report ceo", "phễu online", "bảng xếp hạng", "chấm công ceo"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: [SCRM_Report_ReportDashboardCEO_Offline_BaoCaoDoanhThuChiNhanh, SCRM_Report_ReportDashboardCEO_Offline_DoanhThuTheoSanPham_Va_SanPhamTheoSoLuongBan, SCRM_Report_ReportDashboardCEO_Offline_Daily, SCRM_Report_ReportDashboardCEO_Offline_BaoCaoLichHen, SCRM_Report_ReportDashboardCEO_Offline_BaoCaoTiLeLapDay, SCRM_Report_ReportDashboardCEO_Offline_BaoCaoChanDungKhachHang, SCRM_Report_ReportDashboardCEO_Online_PheuOnline, SCRM_Report_ReportDashboardCEO_Online_SoLieuChung, SCRM_Report_ReportDashboardCEO_Online_BangXepHangSale, SCRM_Report_ReportDashboardCEO_Online_BangXepHangMkt, SCRM_Report_ReportDashboardCEO_Online_SoLieuGiaoVan, SCRM_Report_ReportDashboardCEO_Online_CongViecSale, SCRM_Report_ReportDashboardCEO_DoanhSo_TheoNguon, SCRM_Report_ReportDashboardCEO_DoanhSo_TheoNgay, SCRM_Report_ReportDashboardCEO_DoanhThu_TheoSanPham_Logistic, SCRM_Report_ReportDashboardCEO_ChiaLead, SCRM_Report_ReportDashboardCEO_ChamCong_TongQuat, SCRM_Report_ReportDashboardCEO_TongDai, SCRM_Report_ReportDashboardCEO_ReportNhanContact_Lead, SCRM_Report_ReportDashboardCEO_ReportNhanContact_DoanhSo, SCRM_Report_ReportDashboardCEO_ReportNhanContact, SCRM_Report_ReportDashboardCEO_ReportTrangThaiContact, SCRM_Report_ReportDashboardCEO_ReportTrangThaiContact_DoanhSo, SCRM_Report_ReportDashboardCEO_ReportTacNghiepTheoTrangThaiKh, SCRM_Report_ReportDoanhSoTongQuan, PRC_SCRM_BookingReport_Bookings]
depends: [ReportCeoService, KhachHangReportService, ReportService, ReportCeoCommonService (GetAllReportCeoV2 aggregator), CommonService (gioi han ngay + log query), SqlSugar OpenNewConnectionOrDefault, Report DB SCRM_Report]
cross_module_in: []
cross_module_out: []
---

# BaoCaoCeoController — Dashboard CEO (báo cáo cấp lãnh đạo)

> Controller view/report THUẦN ĐỌC, hub Dashboard CEO. `[TienIchAttribute(..., TI_Report_CEO)]`; **không có `[SBAuthorize]` controller-level** nhưng MỌI method đều có `[SBAuthorize(...)]` riêng (key `BaoCaoCeoController_*`) → thực chất public. Inject 6 service nhưng phần lớn endpoint route về `IReportCeoService` (SP `SCRM_Report_ReportDashboardCEO_*` chạy qua `OpenNewConnectionOrDefault` mở connection riêng cho dashboard nặng); riêng `CustomerReportVisitors` → `IKhachHangReportService`, `ReportDoanhSoTongQuan` → `IReportService`, `GetAllReportCeo` → `IReportCeoCommonService`. Quyền role đặc trưng: `DV-VIEW-REPORT-CEO` + `DV-AD-TONG`/`HT-AD`/`HT-CSKH`. `GetAllReportCeo` là endpoint aggregator (`[SBAuthorize]` không key) gọi `GetAllReportCeoV2` → chạy nhiều SP CEO trong 1 request (setNewConnection=true).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo cáo offline: doanh thu chi nhánh / cửa hàng / sản phẩm | Widget doanh thu offline | POST api/BaoCaoCeo/OrderReportChiNhanh, .../OrderReportDaily, .../ReportOrderReportProduct | BaoCaoCeoController_OrderReportChiNhanh, ..._OrderReportDaily, ..._ReportOrderReportProduct | public | tab offline | 3 widget doanh thu (qua ReportCeoService). Mỗi cái 1 SP. | **SP** ..._Offline_BaoCaoDoanhThuChiNhanh, ..._Offline_Daily, ..._Offline_DoanhThuTheoSanPham_Va_SanPhamTheoSoLuongBan | — |
| 2 | Báo cáo offline: lịch hẹn / tỉ lệ lấp đầy / lượng khách | Widget lịch hẹn-lấp đầy-khách | POST api/BaoCaoCeo/BookingReportBookings, .../DashboardBaoCaoTiLeLapDay, .../OrderCustomerReportVisitors | BaoCaoCeoController_BookingReportBookings, ..._DashboardBaoCaoTiLeLapDay, ..._OrderCustomerReportVisitors | public | tab offline | 3 widget. Mỗi cái 1 SP. | **SP** ..._Offline_BaoCaoLichHen, ..._Offline_BaoCaoTiLeLapDay, ..._Offline_BaoCaoChanDungKhachHang | — |
| 3 | Báo cáo online: phễu online / số liệu chung | Widget phễu + tổng quát | POST api/BaoCaoCeo/PheuOnline, .../SoLieuChung | BaoCaoCeoController_PheuOnline, ..._SoLieuChung | public | tab online | 2 widget tổng quan online. | **SP** ..._Online_PheuOnline, ..._Online_SoLieuChung | — |
| 4 | Báo cáo online: bảng xếp hạng sale / MKT | Bảng xếp hạng | POST api/BaoCaoCeo/ReportBangXepHangSale, .../ReportBangXepHangMarketing | BaoCaoCeoController_ReportBangXepHangSale, ..._ReportBangXepHangMarketing | public | tab online | 2 bảng xếp hạng. | **SP** ..._Online_BangXepHangSale, ..._Online_BangXepHangMkt | — |
| 5 | Báo cáo online: số liệu giao vận / công việc sale | Widget giao vận + công việc | POST api/BaoCaoCeo/SoLieuGiaoVan, .../CongViecSale | BaoCaoCeoController_SoLieuGiaoVan, ..._CongViecSale | public | tab online | 2 widget. | **SP** ..._Online_SoLieuGiaoVan, ..._Online_CongViecSale | — |
| 6 | Báo cáo chung: chấm công tổng quát / top | Widget chấm công | POST api/BaoCaoCeo/ReportCEOChamCongTongQuat, .../ReportCEOChamCongGetTop | BaoCaoCeoController_ReportCEOChamCongTongQuat, ..._ReportCEOChamCongGetTop | public | tab chung | 2 view chấm công DÙNG CHUNG 1 SP ..._ChamCong_TongQuat (khác cách bóc tách kết quả). | **SP** SCRM_Report_ReportDashboardCEO_ChamCong_TongQuat (cả 2) | Hai endpoint cùng 1 SP. |
| 7 | Báo cáo chung: tổng đài | Widget tổng đài | POST api/BaoCaoCeo/ReportCEOTongDai | BaoCaoCeoController_ReportCEOTongDai | public | tab chung | Chạy SP tổng đài. | **SP** SCRM_Report_ReportDashboardCEO_TongDai | — |
| 8 | Báo cáo doanh số theo nguồn / ngày / sản phẩm logistic | Widget doanh số | POST api/BaoCaoCeo/ReportDashboardCEODoanhSoTheoNguon, .../ReportCEODoanhSoTheoNgay, .../ReportCEODoanhSoTheoSanPhamLogistic | BaoCaoCeoController_ReportDashboardCEODoanhSoTheoNguon, ..._ReportCEODoanhSoTheoNgay, ..._ReportCEODoanhSoTheoSanPhamLogistic | public | tab doanh số | 3 widget. | **SP** ..._DoanhSo_TheoNguon, ..._DoanhSo_TheoNgay, ..._DoanhThu_TheoSanPham_Logistic | — |
| 9 | Báo cáo chia lead | Widget chia lead | POST api/BaoCaoCeo/ReportCEOChiaLead | BaoCaoCeoController_ReportCEOChiaLead | public | tab lead | Chạy SP chia lead. | **SP** SCRM_Report_ReportDashboardCEO_ChiaLead | — |
| 10 | Báo cáo khách hàng (chân dung) | Widget khách hàng | POST api/BaoCaoCeo/CustomerReportVisitors | BaoCaoCeoController_CustomerReportVisitors | public | tab khách hàng | Route sang KhachHangReportService. Validate ngày + gọi cấu hình Customer-service, sau đó SP. | **SP** PRC_SCRM_BookingReport_Bookings (log ghi tên khác — xem known-issue) | **BUG log/exec mismatch** (xem dưới). |
| 11 | Báo cáo doanh số tổng quan (CEO) | Lưới doanh số tổng quan | POST api/BaoCaoCeo/ReportDoanhSoTongQuan | BaoCaoCeoController_ReportDoanhSoTongQuan | public | tab doanh số | Route sang ReportService.ReportDoanhSoTongQuan (chung SP với OnlineController). | **SP** SCRM_Report_ReportDoanhSoTongQuan | — |
| 12 | Báo cáo nhãn/trạng thái contact (lead / doanh số / contact) | Lưới nhãn-trạng thái contact | POST api/BaoCaoCeo/ReportNhanContactLeadCeo, .../ReportNhanContactDoanhSoCeo, .../ReportNhanContactCeo, .../ReportTrangThaiContactLeadCeo, .../ReportTrangThaiContactDoanhSoCeo, .../ReportTacNghiepTheoTrangThaiCeo | BaoCaoCeoController_ReportNhanContactLeadCeo ... ..._ReportTacNghiepTheoTrangThaiCeo | public | tab lead/contact | 6 báo cáo nhãn/trạng thái contact, mỗi cái 1 SP CEO. | **SP** ..._ReportNhanContact_Lead, ..._ReportNhanContact_DoanhSo, ..._ReportNhanContact, ..._ReportTrangThaiContact, ..._ReportTrangThaiContact_DoanhSo, ..._ReportTacNghiepTheoTrangThaiKh | — |
| 13 | Lấy TẤT CẢ báo cáo CEO (aggregator) | Load full dashboard 1 lần | POST api/BaoCaoCeo/GetAllReportCeo | [SBAuthorize] (không key) | public | mở dashboard CEO | Gọi GetAllReportCeoV2 → chạy nhiều SP CEO (setNewConnection=true) gộp 1 response. | **SP** nhiều SCRM_Report_ReportDashboardCEO_* (gọi lại các method ở trên) | Endpoint nặng (fan-out nhiều SP/1 request). |

## Quan hệ
- **Gọi sang:** ReportCeoService (đa số SP dashboard), KhachHangReportService (CustomerReportVisitors), ReportService (ReportDoanhSoTongQuan), ReportCeoCommonService (GetAllReportCeoV2 aggregator); Report CommonService (giới hạn ngày + log). CustomerReportVisitors còn gọi HTTP Customer-service GetCauHinhInit.
- **Dữ liệu (entities):** không đọc/ghi entity nội bộ — 100% qua stored proc DB SCRM_Report. Read-only.
- **Ripple / đồng bộ:** không bắn message/event.

## Lỗi đã biết / lưu ý kỹ thuật
- BE Report/Report.Service/Generated/KhachHangReportService.cs
  - **L199 vs L203 — BUG log/exec mismatch:** CustomerReportVisitors log command với tên SP "PRC_SCRM_CustomerReport_Visitors" (L199) nhưng GetDataSetAll thực thi "PRC_SCRM_BookingReport_Bookings" (L203) → SP THỰC CHẠY là PRC_SCRM_BookingReport_Bookings, log sai tên gây nhầm khi debug query chậm.
- BE Report/Report.Service/Generated/ReportCeoService.cs
  - ReportCEOChamCongTongQuat (L1266 → L1301) và ReportCEOChamCongGetTop (L1468 → L1502) cùng exec SP SCRM_Report_ReportDashboardCEO_ChamCong_TongQuat — chỉ khác output DTO.
  - Phần lớn method có overload setNewConnection → mở SqlSugarClient mới (connection riêng SCRM_Report) cho dashboard nặng; GetAllReportCeoV2 (ReportCeoCommonService L74) gọi loạt method với true → cân nhắc hiệu năng.
  - Có method *Private (ReportWarehouse*Private, BCHHNVCoBan*Private) trong service nhưng KHÔNG expose qua controller này.
- BE Report/Report.API/Controllers/BaoCaoCeoController.cs
  - ReportDoanhSoTheoGiaoVan (L213-219) đã comment toàn bộ. GetAllReportCeo (L277) dùng [SBAuthorize()] không key (key gốc bị comment L276).
