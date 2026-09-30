# Báo cáo sale & lead — Kỹ thuật

> Service: **Report** (BE .NET) · FE module **Report** (Angular 15 + ng-zorro). Chỉ dành cho dev. Thuần đọc qua stored proc DB `SCRM_Report`.

## Màn hình & route (FE)
Prefix: `app-routing.module.ts:212` (`path: 'report'`). Route con: `Report/report-routing.module.ts`.

| Màn hình | Route | Component |
|---|---|---|
| Báo cáo công việc sale | `/report/report-cong-viec-sale` | `ReportCongViecSaleComponent` |
| Báo cáo tối ưu sale | `/report/report-toi-uu-sale` | `ReportToiUuSaleComponent` |
| (Liên quan) Doanh số sale theo nguồn landing | `/report/report-doanh-so-sale`, `/report/report-sale-theo-nguon-landing` | `ReportSaleTheoNguonlandingComponent` |

## Endpoint / Controller (trace registry)
Trace: `registry/bao-cao/BaoCaoOnlineController.md` (mục #9, #12, #20).

**BaoCaoOnlineController** (`TI_Report_Online`, public):
| Endpoint | Quyền (key) | SP |
|---|---|---|
| `POST api/BaoCaoOnline/ReportCongViecSale` (+ `ExportExcelReportCongViecSale`) | `BaoCaoOnlineController_ReportCongViecSale` (+ Export) | `SCRM_Report_ReportCongViecSale` |
| `POST api/BaoCaoOnline/ReportToiUuSale` (+ `ExportExcelReportToiUuSale`) | `BaoCaoOnlineController_ReportToiUuSale` (+ Export) | `SCRM_Report_ReportToiUuSale` (+ `_TongDai_MyHub`, `_TongDai_OmiCall`) |
| `POST api/BaoCaoOnline/ReportSaleTheoNguonLanding` (+ Export) | `BaoCaoOnlineController_ReportSaleTheoNguonLanding` (+ Export) | `SCRM_Report_ReportDoanhSoTheoNguon` |

> Bảng xếp hạng Sale (KPI) ở feature `bao-cao-bac-thang-kpi`. Doanh số sale theo nguồn landing chia sẻ component với màn chia lead (`report-doanh-so-sale`).

## Entities
- Không đọc/ghi entity nội bộ. 100% qua stored proc DB `SCRM_Report` (read-only).

## Luồng code
1. Validate khoảng ngày + ép chi nhánh user + lọc cơ cấu tổ chức (LoaiNhanVien=1 cho Sale).
2. **Công việc sale:** chạy SP `SCRM_Report_ReportCongViecSale`.
3. **Tối ưu sale:** chạy SP chính `SCRM_Report_ReportToiUuSale` + 2 SP tổng đài (`_TongDai_MyHub`, `_TongDai_OmiCall`) để enrich số liệu cuộc gọi; `ReportToiUuSaleTongDaiMyHub/OmiCall` là helper nội bộ.
4. Export: build lại cùng SP → `.xlsx` (GemBox); trả 503 nếu `Service_Unavailable`.
5. Log query chậm khi super/CSKH/showQuery.

## Bug-prone / lưu ý (path:line)
- **Tối ưu sale gọi 3 SP/1 request** (chính + 2 tổng đài) — chú ý hiệu năng khi khoảng ngày dài; nếu đơn vị không có tổng đài thì 2 SP enrich trả rỗng.
- **Logic ép chi nhánh + lọc cơ cấu tổ chức lặp lại** ở nhiều method báo cáo online — sửa 1 phải rà các báo cáo khác.
- **GetDataSetAll SYNC** dù async; SP nặng + `CopyNew` per request.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.API\Controllers\BaoCaoOnlineController.cs`.
- BE service: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.Service\Generated\ReportService.cs` (ReportCongViecSale, ReportToiUuSale + ReportToiUuSaleTongDaiMyHub/OmiCall, ReportDoanhSoTheoNguon).
- FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\Report\report-routing.module.ts` (route ở bảng trên).
- SP (DB): `SCRM_Report_ReportCongViecSale`, `SCRM_Report_ReportToiUuSale(+_TongDai_MyHub/_OmiCall)`, `SCRM_Report_ReportDoanhSoTheoNguon`.
