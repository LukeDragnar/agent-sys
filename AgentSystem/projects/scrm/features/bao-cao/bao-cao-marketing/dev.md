# Báo cáo marketing — Kỹ thuật

> Service: **Report** (BE .NET) · FE module **Report** (Angular 15 + ng-zorro). Chỉ dành cho dev. Thuần đọc qua stored proc DB `SCRM_Report`.

## Màn hình & route (FE)
Prefix: `app-routing.module.ts:212` (`path: 'report'`). Route con: `Report/report-routing.module.ts`.

| Màn hình | Route | Component |
|---|---|---|
| Báo cáo công việc MKT | `/report/report-cong-viec-mkt` | `ReportCongViecMktComponent` |
| MKT theo nguồn landing (doanh số MKT quản lý) | `/report/report-doanh-so-mkt-quan-ly`, `/report/report-doanh-so-mkt` | `ReportMktTheoNguonLandingComponent` |
| (Liên quan) Lead theo nhân sự MKT | `/report/report-lead-by-nhan-su` | `ReportLeadByNhanSuComponent` |
| (Liên quan) Báo cáo MKT theo sản phẩm | `/report/bao-cao-mkt-theo-sp` | `BaoCaoMktTheoSanPhamComponent` |

## Endpoint / Controller (trace registry)
Trace: `registry/bao-cao/BaoCaoOnlineController.md` (mục #6, #7, #20).

**BaoCaoOnlineController** (`TI_Report_Online`, public):
| Endpoint | Quyền (key) | SP |
|---|---|---|
| `POST api/BaoCaoOnline/ReportCongViecMktViewTacNghiep` (+ Export) | `BaoCaoOnlineController_ReportCongViecMktViewTacNghiep` (+ Export) | `SCRM_Report_ReportCongViecMkt_View_TacNghiep` |
| `POST api/BaoCaoOnline/ReportCongViecMktViewSale` (+ Export) | `BaoCaoOnlineController_ReportCongViecMktViewSale` (+ Export) | `SCRM_Report_ReportCongViecMkt_View_Sale` |
| `POST api/BaoCaoOnline/ReportMktTheoNguonLanding` | `BaoCaoOnlineController_ReportMktTheoNguonLanding` | `SCRM_Report_ReportDoanhSoMktForManager` |

> Báo cáo MKT theo sản phẩm (`bao-cao-mkt-theo-sp`) thuộc nhóm Marketing (controller `Report_BaoCaoMktTheoSanPham` / `MarketingDashboard`) — xem module Marketing. Bảng xếp hạng MKT (KPI) ở feature `bao-cao-bac-thang-kpi`.

## Entities
- Không đọc/ghi entity nội bộ. 100% qua stored proc DB `SCRM_Report` (read-only).

## Luồng code
1. Validate khoảng ngày + ép chi nhánh user + lọc cơ cấu tổ chức (LoaiNhanVien=3 cho MKT).
2. **Công việc MKT:** 2 view dùng 2 SP riêng (`_View_TacNghiep`, `_View_Sale`) — cùng dữ liệu MKT, khác cách trình bày.
3. **MKT theo nguồn landing:** SP `SCRM_Report_ReportDoanhSoMktForManager` (riêng cho manager; khác SP sale dùng `ReportDoanhSoTheoNguon`).
4. Export: build lại cùng SP → `.xlsx` (GemBox); trả 503 nếu `Service_Unavailable`.
5. Log query chậm khi super/CSKH/showQuery.

## Bug-prone / lưu ý (path:line)
- **Hai view công việc MKT dùng 2 SP khác nhau** trên cùng nguồn dữ liệu — đừng nhầm là 2 chỉ số độc lập; cộng dồn 2 view sẽ sai.
- **MKT theo nguồn landing (manager)** dùng SP khác với Sale theo nguồn landing — cùng màn nguồn landing nhưng 2 nhánh SP (`ReportDoanhSoTheoNguon` cho sale, `ReportDoanhSoMktForManager` cho MKT).
- **Logic ép chi nhánh + lọc cơ cấu tổ chức lặp lại** ở nhiều method báo cáo online.
- **GetDataSetAll SYNC** dù async; SP nặng + `CopyNew` per request.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.API\Controllers\BaoCaoOnlineController.cs`.
- BE service: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.Service\Generated\ReportService.cs` (ReportCongViecMkt_View_TacNghiep/_Sale, ReportDoanhSoMktForManager).
- FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\Report\report-routing.module.ts` (route ở bảng trên).
- SP (DB): `SCRM_Report_ReportCongViecMkt_View_TacNghiep`, `SCRM_Report_ReportCongViecMkt_View_Sale`, `SCRM_Report_ReportDoanhSoMktForManager`.
