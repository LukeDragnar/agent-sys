# Dev — Dashboard tổng quan / CEO

> Service: **Report** (BE .NET) · controller `BaoCaoCeoController` (hub Dashboard CEO, ~30 endpoint public). Báo cáo THUẦN ĐỌC qua stored proc, DB `SCRM_Report`. Chỉ dành cho dev.
> Trace registry: `projects/scrm/registry/bao-cao/BaoCaoCeoController.md`.

## Màn hình & route (FE)
- Màn **Dashboard CEO** trong FE Report. Route/Component cụ thể: **cần xác nhận từ FE** (chưa trace component riêng).

## Endpoint / Controller (trace registry)
`BaoCaoCeoController` — `[TienIchAttribute(..., TI_Report_CEO)]`, KHÔNG có `[SBAuthorize]` controller-level nhưng MỌI method có `[SBAuthorize(...)]` riêng (key `BaoCaoCeoController_*`). Inject 6 service; phần lớn route về `IReportCeoService`.

Vai trò đặc trưng: `DV-VIEW-REPORT-CEO` + `DV-AD-TONG` / `HT-AD` / `HT-CSKH`.

Nhóm endpoint chính (xem đầy đủ ở registry node):
| Nhóm | Endpoint tiêu biểu | Service backing |
|---|---|---|
| Offline doanh thu | `POST api/BaoCaoCeo/OrderReportChiNhanh`, `.../OrderReportDaily`, `.../ReportOrderReportProduct` | `IReportCeoService` |
| Offline khác | `.../BookingReportBookings`, `.../DashboardBaoCaoTiLeLapDay`, `.../OrderCustomerReportVisitors` | `IReportCeoService` / `IKhachHangReportService` |
| Online | `.../PheuOnline`, `.../SoLieuChung`, `.../ReportBangXepHangSale`, `.../ReportBangXepHangMarketing`, `.../SoLieuGiaoVan`, `.../CongViecSale` | `IReportCeoService` |
| Chung | `.../ReportCEOChamCongTongQuat`, `.../ReportCEOChamCongGetTop`, `.../ReportCEOTongDai` | `IReportCeoService` |
| Doanh số / lead / contact | `.../ReportDashboardCEODoanhSoTheoNguon`, `.../ReportCEODoanhSoTheoNgay`, `.../ReportCEOChiaLead`, `.../ReportNhanContact*Ceo`, `.../ReportTrangThaiContact*Ceo` | `IReportCeoService` |
| Doanh số tổng quan (CEO) | `.../ReportDoanhSoTongQuan` | `IReportService` (chung SP với OnlineController) |
| Aggregator | `POST api/BaoCaoCeo/GetAllReportCeo` (`[SBAuthorize]` không key) | `IReportCeoCommonService.GetAllReportCeoV2` |

- Stored proc: `SCRM_Report_ReportDashboardCEO_*` chạy qua `OpenNewConnectionOrDefault` (connection riêng cho dashboard nặng).

## Entities
- **Không đọc/ghi entity nội bộ.** 100% qua stored proc DB `SCRM_Report`. Read-only, không bắn message/event.

## Luồng code
1. Gate khoảng ngày + lọc cơ cấu tổ chức như các báo cáo online (qua `CommonService`).
2. Mỗi endpoint route về service tương ứng, chạy 1 SP `SCRM_Report_ReportDashboardCEO_*` (hoặc SP dùng chung).
3. `GetAllReportCeo` → `GetAllReportCeoV2` (`ReportCeoCommonService` L74) gọi loạt method với `setNewConnection=true` → fan-out nhiều SP trong 1 request.

## Bug-prone / lưu ý
- **BUG log/exec mismatch** (`KhachHangReportService.cs` L199 vs L203): `CustomerReportVisitors` log tên SP `PRC_SCRM_CustomerReport_Visitors` (L199) nhưng thực thi `PRC_SCRM_BookingReport_Bookings` (L203). Kết quả đúng (chạy Bookings) nhưng **log sai tên** → nhầm khi debug query chậm. (Known-issue — KHÔNG sửa ở session này.)
- **2 endpoint chấm công cùng 1 SP** (`ReportCeoService.cs`): `ReportCEOChamCongTongQuat` (L1266→L1301) và `ReportCEOChamCongGetTop` (L1468→L1502) cùng exec `SCRM_Report_ReportDashboardCEO_ChamCong_TongQuat`, chỉ khác output DTO.
- **`GetAllReportCeo` nặng** (fan-out nhiều SP với `setNewConnection=true`) → cân nhắc hiệu năng; `[SBAuthorize()]` không key (key gốc comment L276) → quyền theo auth chung.
- `ReportDoanhSoTheoGiaoVan` (controller L213-219) đã comment toàn bộ — không dùng ở dashboard CEO.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.API\Controllers\BaoCaoCeoController.cs` (GetAllReportCeo L277; ReportDoanhSoTheoGiaoVan L213-219 đã comment).
- BE service CEO: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.Service\Generated\ReportCeoService.cs` (chấm công L1266/L1468).
- BE service KH: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.Service\Generated\KhachHangReportService.cs:199` / `:203` (log/exec mismatch).
- BE aggregator: `ReportCeoCommonService.cs:74` (GetAllReportCeoV2).
- Stored proc (DB): `SCRM_Report_ReportDashboardCEO_*`, `PRC_SCRM_BookingReport_Bookings`, `SCRM_Report_ReportDoanhSoTongQuan`.
- Registry node: `projects/scrm/registry/bao-cao/BaoCaoCeoController.md`.
