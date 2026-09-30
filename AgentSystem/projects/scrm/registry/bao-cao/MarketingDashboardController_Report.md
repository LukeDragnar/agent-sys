---
controller: MarketingDashboardController
module: bao-cao
service: Report
man_hinh_ui: "Marketing > Dashboard: báo cáo lead theo nguồn (gom theo landing/utm), thống kê dữ liệu landing 7 ngày gần nhất (biểu đồ đường), xem chi tiết campaign, và xuất Excel báo cáo lead theo nguồn. Bộ lọc: chi nhánh, phòng ban/nhóm/nhân viên MKT, từ/đến ngày, nguồn/landing/campaign, điều kiện contact."
muc_dich: "Dashboard báo cáo marketing (4 endpoint public): báo cáo lead theo nguồn, biểu đồ landing 7 ngày, chi tiết campaign, xuất Excel. Đọc qua nhiều stored proc DB Report (fan-out theo nguồn dữ liệu: contact/order-logistic/đơn hàng/đặt lịch)."
alias: ["marketing dashboard", "báo cáo lead theo nguồn", "bao cao lead theo nguon", "dashboard marketing", "thống kê landing", "chi tiết campaign", "xuất excel lead"]
auth: mixed
state_machine: []
deprecated: false
entities: []
stored_procs: [SCRM_ReportBaoCaoLeadTheoNguon_Contact_Final, SCRM_ReportBaoCaoLeadTheoNguon_OrderLogistic_Final, SCRM_ReportBaoCaoLeadTheoNguon_DonHang_Final, SCRM_ReportBaoCaoLeadTheoNguon_DatLich_Final, SCRM_ReportTongSoLieuNganSachAllLandingChiNhanh, SCRM_ReportBaoCaoLeadTheoNguon_ChiTietCampaign_Contact_Final, SCRM_ReportBaoCaoLeadTheoNguon_ChiTietCampaign_OrderLogistic_Final, SCRM_ReportBaoCaoLeadTheoNguon_ChiTietCampaign_DonHang_Final, SCRM_ReportBaoCaoLeadTheoNguon_ChiTietCampaign_DatLich_Final, SCRM_ReportBaoCaoDuLieuLanding7NgayGanNhat_20250821]
depends: [Report/CommonService (GetTimeToLog), Report/CommonPermisionValidate (CheckPermissionViewrReportContactWithMultiUser), Report/TicketInfoService (ReleaseLockedTicket — giới hạn đọc đồng thời), SqlSugar (GetDataSetAll stored proc), GemBox.Spreadsheet (xuất Excel), DB SCRM_Report]
cross_module_in: []
cross_module_out: []
---

# MarketingDashboardController — Dashboard báo cáo marketing (lead theo nguồn)

> COLLISION node-name (tồn tại MarketingDashboardController ở module khác) → file `MarketingDashboardController_Report.md`. Controller view/report THUẦN ĐỌC trong Report-service (4 endpoint public). Đọc qua **nhiều stored proc** DB SCRM_Report: báo cáo lead "theo nguồn" fan-out 4 SP `_Final` (Contact / OrderLogistic / DonHang / DatLich) qua `GetSourceData` rồi merge trong code (`MergeSourceData`); biến thể chi tiết campaign dùng 4 SP `_ChiTietCampaign_*_Final`. `[TienIchAttribute(..., TI_Report_Chung)]`. Auth **mixed**: 2 method có permission key (`MarketingDashboardController_*`, public), 2 method `[SBAuthorize]` không key (GetCampaingStatisticDetails, ExportExcel). Gate: `CheckPermissionViewrReportContactWithMultiUser` (lọc user/nhóm MKT theo quyền) + `ReleaseLockedTicket` (giới hạn số user đọc report online/marketing đồng thời) + lấy cấu hình thời gian log. Implementation thực ở `MarketingDashboardService_20250609.cs` (partial), không phải file `MarketingDashboardService.cs` gốc.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo cáo lead theo nguồn | Lưới lead theo nguồn | `POST api/MarketingDashboard/TimTheoDieuKien` | `MarketingDashboardController_TimTheoDieuKien` | public | màn marketing dashboard | Check quyền multi-user MKT + release ticket đồng thời. `GetSourceData` chạy 4 SP `_Final` + SP ngân sách landing, merge + lọc theo LoaiContact + order + phân trang trong code. | **SP** `SCRM_ReportBaoCaoLeadTheoNguon_Contact_Final`, `_OrderLogistic_Final`, `_DonHang_Final`, `_DatLich_Final`, `SCRM_ReportTongSoLieuNganSachAllLandingChiNhanh` | — |
| 2 | Biểu đồ landing 7 ngày gần nhất | Line chart landing | `POST api/MarketingDashboard/GetStatisticsLast7DayOfLanding` | `MarketingDashboardController_GetStatisticsLast7DayOfLanding` | public | màn marketing dashboard | Chạy 1 SP trả số liệu landing 7 ngày, build LineChart. | **SP** `SCRM_ReportBaoCaoDuLieuLanding7NgayGanNhat_20250821` | tên SP gắn suffix ngày `_20250821` (bản version-theo-ngày). |
| 3 | Chi tiết campaign (báo cáo lead) | Drill-down campaign | `POST api/MarketingDashboard/GetCampaingStatisticDetails` | `[SBAuthorize]` (không key) | public | drill từ #1 | Như #1 nhưng dùng bộ SP chi tiết campaign (4 SP `_ChiTietCampaign_*_Final`), merge + phân trang code. | **SP** `SCRM_ReportBaoCaoLeadTheoNguon_ChiTietCampaign_Contact_Final`, `_OrderLogistic_Final`, `_DonHang_Final`, `_DatLich_Final` | permission key bị comment (L39) → để `[SBAuthorize]` không key. |
| 4 | Xuất Excel báo cáo lead theo nguồn | Nút Xuất Excel | `POST api/MarketingDashboard/ExportExcelMarketingDashBoard` | `[SBAuthorize]` (không key) | public | màn marketing dashboard | Gọi lại `BaoCaoLeadTheoNguonAsync` (#1) rồi build file xlsx (GemBox). Lỗi `Service_Unavailable` → 503; lỗi khác → 500; OK → trả file `BaoCaoLeadTheoNguon.xlsx`. | **SP** như #1 (qua BaoCaoLeadTheoNguonAsync) + GemBox xuất Excel | — |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Quyền multi-user MKT:** `CheckPermissionViewrReportContactWithMultiUser` (role DV_MKT) lọc danh sách user/nhóm MKT được xem → fail → trả lỗi xác thực quyền. Set `StrUserId`, `StrIdNhomNhanVien`, `IsAdminChonCoCauToChuc` vào filter.
- **Admin view store:** `CheckUserIsAdminViewStore` → `filter.IsAdminView = true`.
- **Giới hạn đọc đồng thời:** `ReleaseLockedTicket(SO_USER_READ_REPORT_ONLINE_DONG_THOI)` và `(SO_USER_READ_REPORT_MARKETING_DASHBOARD)` — null → trả lỗi "Lỗi ReleaseLockedTicket Step1/Step2".
- **Merge + lọc trong code:** 4 SP nguồn được merge (`MergeSourceData`) rồi lọc theo `LoaiContact` (CoContactOrChotDon / KhongCoContactVe), order theo `OrderBy` (SoContact/TyLeChot/DoanhSo), phân trang trên API (KHÔNG trên SP).
- **Tác nghiệp loại trừ:** `GetDropdownDMTacNghiepByXepHang(3, ...)` lấy danh sách tác nghiệp xếp hạng → loại khỏi thống kê khi `TacNghiepThongKe == null`.

## Quan hệ
- **Gọi sang:** Report `CommonService` (GetTimeToLog), `CommonPermisionValidate` (CheckPermissionViewrReportContactWithMultiUser), `TicketInfoService` (ReleaseLockedTicket).
- **Dữ liệu (entities):** không đọc/ghi entity nội bộ — qua nhiều stored proc DB SCRM_Report. Read-only.
- **Ripple / đồng bộ:** không bắn message/event.

## Lỗi đã biết / lưu ý kỹ thuật
- BE implementation thực ở `Report/Report.Service/Generated/MarketingDashboardService_20250609.cs` (partial class), KHÔNG ở `MarketingDashboardService.cs` (file gốc chỉ chứa method `*_20241105` cũ + interface). Dễ nhầm khi grep.
  - L1350 `BaoCaoLeadTheoNguonAsync` (#1) → `GetSourceData` chạy SP `_Final` (L79/L127/L173/L221) + ngân sách L235.
  - L1233 `ChiTietCampaign_BaoCaoLeadTheoNguonAsync` (#3) → SP `_ChiTietCampaign_*_Final` (L805/L851/L895/L944).
  - L1615 `GetStatisticsLast7DayOfLandingAsync` (#2) → SP `SCRM_ReportBaoCaoDuLieuLanding7NgayGanNhat_20250821` (L1643).
  - L1705 `ExportExcelMarketingDashBoard` (#4) → gọi lại `BaoCaoLeadTheoNguonAsync` (L1726).
- BE `Report/Report.API/Controllers/MarketingDashboardController.cs`
  - L39 — permission key `MarketingDashboardController_GetCampaingStatisticDetails` bị comment, để `[SBAuthorize]` trống → endpoint #3 chỉ check đăng nhập, không gắn quyền chi tiết.
  - L1404 (service) — `isShowQuery = true` hardcode (bỏ qua `GetStatusShowQueryReport`) → luôn ghi log query.
  - `BaoCaoLeadTheoNguonAsync` hiệu năng nặng: fan-out 4-5 SP rồi merge + phân trang in-memory trên API.
