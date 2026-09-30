# Báo cáo bậc thang & KPI — Kỹ thuật

> Service: **Report** (BE .NET) · FE module **Report** (Angular 15 + ng-zorro). Chỉ dành cho dev. Thuần đọc qua stored proc DB `SCRM_Report`.

## Màn hình & route (FE)
Prefix: `app-routing.module.ts:212` (`path: 'report'`). Route con: `Report/report-routing.module.ts`.

| Màn hình | Route | Component |
|---|---|---|
| Bảng xếp hạng Sale | `/report/report-bang-xep-hang-sale` | `ReportBangXepHangSaleComponent` |
| Bảng xếp hạng MKT | `/report/report-bang-xep-hang-mkt` | `ReportBangXepHangMktComponent` |
| Phong thần bảng (Sale/MKT/Nhóm sale) | `/report/report-phong-than-bang` | `ReportPhongThanBangComponent` |

## Endpoint / Controller (trace registry)
Trace: `registry/bao-cao/BaoCaoOnlineController.md` (mục #4, #8). BXH CEO: `registry/bao-cao/BaoCaoCeoController.md` (#4).

**BaoCaoOnlineController** (`TI_Report_Online`, public):
| Endpoint | Quyền (key) | SP |
|---|---|---|
| `POST api/BaoCaoOnline/ReportBangXepHangSale` | `BaoCaoOnlineController_ReportBangXepHangSale` | `SCRM_Report_ReportBangXepHang` (ép LoaiNhanVien=1) |
| `POST api/BaoCaoOnline/ReportBangXepHangMarketing` | `BaoCaoOnlineController_ReportBangXepHangMarketing` | `SCRM_Report_ReportBangXepHang` (ép LoaiNhanVien=3) |
| `POST api/BaoCaoOnline/ReportPhongThanBangSale` | `BaoCaoOnlineController_ReportPhongThanBangSale` | `SCRM_Report_ReportPhongThanBang_Sale` |
| `POST api/BaoCaoOnline/ReportPhongThanBangMKT` | `BaoCaoOnlineController_ReportPhongThanBangMKT` | `SCRM_Report_ReportPhongThanBang_Mkt` |
| `POST api/BaoCaoOnline/ReportPhongThanBangNhomSale` | `BaoCaoOnlineController_ReportPhongThanBangNhomSale` | `SCRM_Report_ReportPhongThanBang_NhomSale` |

> CEO dashboard có bảng xếp hạng riêng: `BaoCaoCeoController` → `ReportBangXepHangSale` / `ReportBangXepHangMarketing` (SP `SCRM_Report_ReportDashboardCEO_Online_BangXepHangSale/Mkt`) — khác SP với online thường.

## Entities
- Không đọc/ghi entity nội bộ. 100% qua stored proc DB `SCRM_Report` (read-only).

## Luồng code
1. Validate khoảng ngày + ép chi nhánh user + lọc cơ cấu tổ chức `GetCheckPermissionViewDropdownListNhomVaUser`.
2. **BXH:** `ReportBangXepHangSale`/`Marketing` ép `LoaiNhanVien` (1/3) rồi gọi chung `ReportBangXepHang` → cùng SP `SCRM_Report_ReportBangXepHang`.
3. **Phong thần bảng:** mỗi biến thể gọi SP riêng qua `OpenNewConnectionOrDefault(... "SCRM_Report_ReportPhongThanBang_Mkt/_Sale/_NhomSale", true)` — mở connection riêng cho store nặng.
4. Sắp xếp theo `TruongSapXep`/`KieuSapXep` ở tầng app (sau khi SP trả về).
5. Log query chậm khi super/CSKH/showQuery.

## Bug-prone / lưu ý (path:line)
- **BXH Sale và MKT dùng CHUNG 1 SP** `SCRM_Report_ReportBangXepHang`, chỉ khác `LoaiNhanVien` (1/3) — sửa SP ảnh hưởng cả 2 màn; truyền sai LoaiNhanVien → sai nhóm nhân viên.
- **Phong thần bảng bản gốc (ReportPhongThanBang, BaoCaoOnlineController.cs:86-92) đã comment toàn bộ** — chỉ các biến thể MKT/Sale/NhomSale còn hoạt động.
- **Phong thần bảng** (ReportService.cs:8286/8413/8556) dùng `OpenNewConnectionOrDefault(..., true)` luôn mở connection riêng; dòng `GetDataSetAll` cũ đã comment.
- **Sắp xếp ở app, không ở SP** — với dữ liệu lớn cần chú ý hiệu năng/paging vì sort sau khi lấy về.
- **GetDataSetAll SYNC** dù async.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.API\Controllers\BaoCaoOnlineController.cs` (ReportPhongThanBang gốc L86-92 comment).
- BE service: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.Service\Generated\ReportService.cs` (ReportBangXepHang; phong thần bảng L8286/8413/8556).
- FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\Report\report-routing.module.ts` (route ở bảng trên).
- SP (DB): `SCRM_Report_ReportBangXepHang`, `SCRM_Report_ReportPhongThanBang_Sale/_Mkt/_NhomSale`; CEO: `SCRM_Report_ReportDashboardCEO_Online_BangXepHangSale/Mkt`.
