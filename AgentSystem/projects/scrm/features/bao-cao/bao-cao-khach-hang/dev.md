# Báo cáo khách hàng — Kỹ thuật

> Service: **Report** (BE .NET) · FE module **Report** (Angular 15 + ng-zorro). Chỉ dành cho dev. Thuần đọc qua stored proc DB `SCRM_Report`.

## Màn hình & route (FE)
Prefix: `app-routing.module.ts:212` (`path: 'report'`). Route con: `Report/report-routing.module.ts`.

| Màn hình | Route | Component |
|---|---|---|
| Báo cáo theo khách hàng | `/report/report-theo-khach-hang` | `ReportTheoKhachHangComponent` |
| Khách hàng cũ / mới | `/report/report-khach-hang-cu-moi` | `ReportKhachhangCumoiComponent` |
| Số liệu khách hàng hiện tại theo tháng | `/report/report-solieu-khachhang-hientai` | `ReportSolieuKhachhangHienTaiComponent` |
| Số liệu khách hàng quay lại theo tháng | `/report/report-solieu-khachhang-quaylai` | `ReportSolieuKhachhangQuaylaiComponent` |

## Endpoint / Controller (trace registry)
Trace: `registry/bao-cao/BaoCaoOnlineController.md` (mục #13, #15, #19).

**BaoCaoOnlineController** (`TI_Report_Online`, public):
| Endpoint | Quyền (key) | SP |
|---|---|---|
| `POST api/BaoCaoOnline/ReportTheoKhachHang` (+ `ExportExcelReportTheoKhachHang`) | `BaoCaoOnlineController_ReportTheoKhachHang` (+ `_ExportExcel...`) | `SCRM_Report_ReportTheoKhachHang` |
| `POST api/BaoCaoOnline/ReportKhachHangCuMoi` (+ `ExportExcelReportKhachHangCuMoi`) | `BaoCaoOnlineController_ReportKhachHangCuMoi` (+ `_ExportExcel...`) | `SCRM_Report_ReportKhachHangCuMoi` |
| `POST api/BaoCaoOnline/ReportSoLieuHienTaiTheoThang` (+ Export) | `BaoCaoOnlineController_ReportSoLieuHienTaiTheoThang` (+ Export) | `SCRM_Report_ReportDoanhSoMuaLaiTheoThang` |
| `POST api/BaoCaoOnline/ReportSoLieuQuayLaiTheoThang` (+ Export) | `BaoCaoOnlineController_ReportSoLieuQuayLaiTheoThang` (+ Export) | `SCRM_Report_ReportDoanhSoMuaLaiTheoThang_QuayLai` |

> Liên quan (chân dung KH, không thuộc 4 màn trên nhưng cùng nhóm báo cáo KH): `OrderReportController` → `OrderCustomerReportVisitors` (SP `PRC_SCRM_Order_CustomerReport_Visitors`), FE route `/report/visitor`. `BaoCaoCeoController.CustomerReportVisitors` cho dashboard CEO.

## Entities
- Không đọc/ghi entity nội bộ. 100% qua stored proc DB `SCRM_Report` (read-only).

## Luồng code
Khuôn chung trong `ReportService`:
1. Validate khoảng ngày (`CommonService.CheckCauHinhKhoangThoiGianChoPhepTimKiem`).
2. Ép chi nhánh user khi không admin/super/test/CSKH.
3. Lọc cơ cấu tổ chức `GetCheckPermissionViewDropdownListNhomVaUser` → IdNhomString/IdUserString truyền SP.
4. `db.Ado.UseStoredProcedure().GetDataSetAll(<SP>, ...)` trên `_dbDapper.CopyNew()`.
5. Log query chậm khi super/CSKH/showQuery.
6. Export: build lại cùng SP → `.xlsx` (GemBox); trả 503 nếu `Service_Unavailable`.

## Bug-prone / lưu ý (path:line)
- **`ReportSoLieuHienTaiTheoThang` và `ReportSoLieuQuayLaiTheoThang`** dùng cùng họ SP `SCRM_Report_ReportDoanhSoMuaLaiTheoThang` (biến thể `_QuayLai`) — coi chừng đổi 1 SP ảnh hưởng cả 2 màn.
- **Logic ép chi nhánh + lọc cơ cấu tổ chức lặp lại ở nhiều method** — sửa 1 phải rà các báo cáo khác (giống pattern ở các hub báo cáo online).
- **GetDataSetAll là call SYNC** dù method async — SP nặng + `CopyNew` per request.
- Ranh giới "khách cũ/mới" phụ thuộc SP `SCRM_Report_ReportKhachHangCuMoi` và cờ đánh dấu khách cũ — không xử lý ở tầng C# controller.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.API\Controllers\BaoCaoOnlineController.cs`.
- BE service: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.Service\Generated\ReportService.cs` (ReportTheoKhachHang, ReportKhachHangCuMoi, ReportDoanhSoMuaLaiTheoThang(+_QuayLai)).
- FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\Report\report-routing.module.ts` (route ở bảng trên).
- SP (DB): `SCRM_Report_ReportTheoKhachHang`, `SCRM_Report_ReportKhachHangCuMoi`, `SCRM_Report_ReportDoanhSoMuaLaiTheoThang(+_QuayLai)`.
