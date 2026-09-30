# Dev — Báo cáo giao vận

> Service: **Report** (BE .NET) · controller `BaoCaoOnlineController` (hub báo cáo online). Báo cáo THUẦN ĐỌC qua stored proc, DB `SCRM_Report`. Chỉ dành cho dev.
> Trace registry: `projects/scrm/registry/bao-cao/BaoCaoOnlineController.md` (hành động #5).

## Màn hình & route (FE)
- Nằm trong cụm màn **Báo cáo online** của FE Report (menu "Báo cáo doanh số theo giao vận"). Route/Component cụ thể: **cần xác nhận từ FE** (module báo cáo online gồm nhiều tab; chưa trace component riêng cho tab giao vận).

## Endpoint / Controller (trace registry)
`BaoCaoOnlineController` — `[TienIchAttribute(..., TI_Report_Online)]`, **KHÔNG có `[SBAuthorize]` controller-level** (auth mixed; báo cáo này public theo khóa quyền tính năng).

| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Báo cáo doanh số theo giao vận | `POST api/BaoCaoOnline/ReportDoanhSoTheoGiaoVan` | `BaoCaoOnlineController_ReportDoanhSoTheoGiaoVan` |
| Xuất Excel | `POST api/BaoCaoOnline/ExportExcelReportDoanhSoTheoGiaoVan` | `BaoCaoOnlineController_ReportDoanhSoTheoGiaoVan` (DÙNG CHUNG khóa với báo cáo) |

- Stored proc: `SCRM_Report_ReportDoanhSoTheoGiaoVan`.
- Báo cáo giao vận chạy qua `OpenNewConnectionOrDefault` (mở connection riêng cho store nặng) — giống phong thần bảng.

## Entities
- **Không đọc/ghi entity nội bộ.** ~100% qua stored proc DB `SCRM_Report`. Read-only, không bắn message/event.

## Luồng code
1. Gate khoảng ngày: `CommonService.CheckCauHinhKhoangThoiGianChoPhepTimKiem` ≠ -1 → chặn "Chỉ được xem dữ liệu trong khoảng N ngày".
2. Ép chi nhánh: user thường (không admin/super/test/CSKH) + `IdChiNhanh==Empty` → mặc định chi nhánh user.
3. Lọc cơ cấu tổ chức: `GetCheckPermissionViewDropdownListNhomVaUser(loaiNhanVien, ...)` → `IdNhomString`/`IdUserString` truyền vào SP.
4. Chạy SP `SCRM_Report_ReportDoanhSoTheoGiaoVan` qua `db.Ado.UseStoredProcedure().GetDataSetAll(...)` (`OpenNewConnectionOrDefault`).
5. Export: `ExportExcelReportDoanhSoTheoGiaoVan` build lại cùng SP, xuất `.xlsx` bằng GemBox; lỗi `Service_Unavailable` → 503, lỗi khác → 500.

## Bug-prone / lưu ý
- **Export dùng CHUNG permission key với báo cáo** (`BaoCaoOnlineController.cs` L127): ai xem được báo cáo thì xuất Excel được luôn — không có khóa quyền xuất riêng. Lưu ý khi cần tách quyền "xem" vs "xuất".
- Log query chậm `LogCommandV2(...)` chỉ chạy khi `IsSuperUser || IsCSKH || GetStatusShowQueryReport(DonViId)==true`.
- Báo cáo giao vận đi `OpenNewConnectionOrDefault` (connection riêng) → store nặng; cân nhắc hiệu năng khi gọi nhiều.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.API\Controllers\BaoCaoOnlineController.cs` (ReportDoanhSoTheoGiaoVan; Export L127 dùng chung key).
- BE service: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.Service\Generated\ReportService.cs` (SP `SCRM_Report_ReportDoanhSoTheoGiaoVan`).
- Stored proc (DB, không trong repo source): `SCRM_Report_ReportDoanhSoTheoGiaoVan`.
- Registry node: `projects/scrm/registry/bao-cao/BaoCaoOnlineController.md`.
