# Dev — Báo cáo Marketing

> Service: **Report** (BE .NET, view/report thuần đọc, 100% qua stored proc DB SCRM_Report). 2 controller chính. Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Khu vực | Ghi chú |
|---|---|---|
| Dashboard báo cáo lead theo nguồn | Marketing > Dashboard | lưới lead theo nguồn + biểu đồ landing 7 ngày + chi tiết campaign + xuất Excel |
| Báo cáo MKT theo sản phẩm | Báo cáo > Marketing | lưới + 2 kiểu xuất Excel |
| Báo cáo doanh số giao vận (MKT) / công việc MKT / lead theo nhân sự MKT | Báo cáo > Marketing | các lưới phụ |

## Endpoint / Controller (trace registry)

**MarketingDashboardController** (service Report) — registry `registry/bao-cao/MarketingDashboardController_Report.md` (collision name → file `_Report`). Auth **mixed**:
| # | Hành động | Endpoint | Quyền (key) |
|---|---|---|---|
| 1 | Báo cáo lead theo nguồn | `POST api/MarketingDashboard/TimTheoDieuKien` | `MarketingDashboardController_TimTheoDieuKien` |
| 2 | Biểu đồ landing 7 ngày | `POST api/MarketingDashboard/GetStatisticsLast7DayOfLanding` | `MarketingDashboardController_GetStatisticsLast7DayOfLanding` |
| 3 | Chi tiết campaign | `POST api/MarketingDashboard/GetCampaingStatisticDetails` | `[SBAuthorize]` (không key — đã comment) |
| 4 | Xuất Excel lead theo nguồn | `POST api/MarketingDashboard/ExportExcelMarketingDashBoard` | `[SBAuthorize]` (không key) |

**ReportController** (service Report) — registry `registry/bao-cao/ReportController.md`. Class-level `[SBAuthorize]` → mọi method public:
| # | Hành động | Endpoint | Quyền (key) |
|---|---|---|---|
| 1 | Doanh số giao vận (MKT view) | `POST api/Report/ReportDoanhSoTongQuan_SoLieuGiaoVanMarketing` | `Report_ReportDoanhSoTongQuan_SoLieuGiaoVanMarketing` |
| 2 | Doanh số giao vận (sale view) | `POST api/Report/ReportDoanhSoTongQuan_SoLieuGiaoVan` | `Report_ReportDoanhSoTongQuan_SoLieuGiaoVan` |
| 3 | Công việc MKT view sale | `POST api/Report/ReportCongViecMktViewSale` | `Report_ReportCongViecMktViewSale` |
| 4 | Lead theo nhân sự MKT | `POST api/Report/ReportLeadByNhanSuMktSearch` | `Report_ReportLeadByNhanSuMktSearch` |
| 5 | MKT theo sản phẩm | `POST api/Report/BaoCaoMktTheoSanPham` | `Report_BaoCaoMktTheoSanPham` |
| 6 | Xuất Excel MKT theo SP (2 kiểu gom) | `POST api/Report/ExportExcel_BaoCaoMktTheoSanPham_NhomTheoSanPham` · `..._NhomTheoNguonDuLieu` | `ExportExcel_BaoCaoMktTheoSanPham_NhomTheoSanPham` / `..._NhomTheoNguonDuLieu` |

## Entities / Stored procs
- **Không đọc/ghi entity nội bộ** — 100% qua stored proc DB SCRM_Report.
- Lead theo nguồn (#1): `SCRM_ReportBaoCaoLeadTheoNguon_Contact_Final`, `_OrderLogistic_Final`, `_DonHang_Final`, `_DatLich_Final`, `SCRM_ReportTongSoLieuNganSachAllLandingChiNhanh`.
- Chi tiết campaign (#3): `SCRM_ReportBaoCaoLeadTheoNguon_ChiTietCampaign_*_Final` (4 SP).
- Biểu đồ landing (#2): `SCRM_ReportBaoCaoDuLieuLanding7NgayGanNhat_20250821`.
- MKT theo SP (#5/6): `PRC_SCRM_Report_BaoCaoMktTheoSanPham` (+ `_SoContact`, `_AllLanding`), `SCRM_ReportTongSoLieuNganSachAllLandingChiNhanh`.

## Luồng code
- **Lead theo nguồn:** implementation thực ở `Report/Report.Service/Generated/MarketingDashboardService_20250609.cs` (partial), KHÔNG ở `MarketingDashboardService.cs` gốc. `BaoCaoLeadTheoNguonAsync` (L1350) → `GetSourceData` chạy 4 SP `_Final` (L79/L127/L173/L221) + ngân sách (L235) → `MergeSourceData` → lọc `LoaiContact` → order → phân trang in-memory.
- **Gate:** `CheckPermissionViewrReportContactWithMultiUser` (quyền multi-user MKT) + `CheckUserIsAdminViewStore` (IsAdminView) + `ReleaseLockedTicket(SO_USER_READ_REPORT_ONLINE_DONG_THOI / ..._MARKETING_DASHBOARD)` (giới hạn đọc đồng thời) + `GetTimeToLog`.
- **MKT theo SP:** `ReportService.BaoCaoMktTheoSanPham` (L16450+) chạy 3-4 SP `PRC_SCRM_Report_BaoCaoMktTheoSanPham*` qua `_dbDapper1` rồi gộp; xuất Excel build lại cùng bộ SP. Gate chung ReportController: validate khoảng ngày (`CheckCauHinhKhoangThoiGianChoPhepTimKiem`) + ép chi nhánh non-admin + log query.

## Bug-prone / lưu ý
- **Grep nhầm file:** logic lead-theo-nguồn ở `MarketingDashboardService_20250609.cs` (partial), file gốc `MarketingDashboardService.cs` chỉ chứa method `*_20241105` cũ + interface.
- **`MarketingDashboardController.cs:39`** (Report) — permission key `..._GetCampaingStatisticDetails` bị **comment** → endpoint chi tiết campaign + xuất Excel chỉ `[SBAuthorize]` (login), không gắn quyền chi tiết.
- **`MarketingDashboardService_20250609.cs:1404`** — `isShowQuery = true` hardcode (bỏ qua `GetStatusShowQueryReport`) → luôn ghi log query.
- **Hiệu năng nặng:** `BaoCaoLeadTheoNguonAsync` fan-out 4-5 SP rồi **merge + phân trang in-memory trên API** → khoảng ngày rộng tốn RAM/CPU, dễ chậm/timeout.
- **SP gắn suffix ngày:** `SCRM_ReportBaoCaoDuLieuLanding7NgayGanNhat_20250821` — tên SP gắn version-theo-ngày, đổi SP cẩn thận đụng version.
- **`ReportController.cs`** — class-level `[SBAuthorize]` (L12) → mọi endpoint public (khác `BaoCaoOnlineController` không có auth class-level → mixed). `ReportDoanhSoTongQuan_SoLieuGiaoVanMarketing` / `ReportCongViecMktViewSale` dùng chung service method + SP với `BaoCaoOnlineController`, chỉ khác permission key & ép `LoaiNhanVien`.
- `ReportDoanhSoTheoGiaoVanMarketing` (ReportController.cs:52-59) đã comment toàn bộ → chỉ còn 6 endpoint thực.

## File source (path:line)
- Registry: `projects/scrm/registry/bao-cao/MarketingDashboardController_Report.md`, `registry/bao-cao/ReportController.md`.
- BE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.API\Controllers\MarketingDashboardController.cs` (`:39`), `...\ReportController.cs` (`:12`, `:52-59`).
- BE service: `...\Report\Report.Service\Generated\MarketingDashboardService_20250609.cs` (`:1350`, `:1233`, `:1615`, `:1705`, `:1404`), `...\ReportService.cs` (`:16450+`, `:3702`).
