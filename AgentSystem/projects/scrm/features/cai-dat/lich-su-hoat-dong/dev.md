# Dev — Lịch sử hoạt động

> Service: **LogService** (BE .NET) · FE trong module **CaidatHethongModule** (Angular 15 + ng-zorro), nhóm `lich-su-*` + `Log/`. Gồm 3 controller: LichSuTacDong, LichSuBoLoc, LichSuXemThongTin. Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Lịch sử hoạt động (tác động) | `/caidat/lich-su-tac-dong` | `LichSuTacDongComponent` |
| Lịch sử bộ lọc | `/caidat/lich-su-bo-loc` | `LogBoLocComponent` (`Log/Log-bo-loc`) |
| Lịch sử tìm kiếm | `/caidat/lich-su-tim-kiem` | `LogSearchComponent` (`Log/Log-search`) |
| Lịch sử xem thông tin | `/caidat/lich-su-xem-thong-tin` | `LogLichSuMoThongTinChiTietComponent` (`Log/LogLichSuMoThongTinChiTiet`) |

- Route gốc module: `app-routing.module.ts:140` (`path: 'caidat'` → lazy `CaidatHethongModule`).
- Route con: `caidat-hethong-routing.module.ts:252` (tac-dong), `:256` (bo-loc), `:260` (tim-kiem), `:264` (xem-thong-tin).
- Màn phụ liên quan xem thông tin: `Log/LogLichSuMoThongTin` (GetTopCount tổng).

## Endpoint / Controller (trace registry)

**LichSuTacDongController** (registry: `he-thong/LichSuTacDongController.md`), service LogService, route `api/LichSuTacDong`:
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Dropdown loại tác động | GET `.../DropdownActivity` | `LichSuTacDong_DropdownActivity` |
| Dropdown bảng | GET `.../DropdownTable` | `LichSuTacDong_DropdownTable` |
| Tìm theo điều kiện | POST `.../TimTheoDieuKien` | `LichSuTacDong_TimTheoDieuKien` |
| Xóa 1 dòng | POST `.../Xoa/{id}` | `LichSuTacDong_Xoa` (FE chỉ hiện nút khi `isSuperUser`) |

**LichSuBoLocController** (registry: `he-thong/LichSuBoLocController.md`), route `api/LichSuBoLoc` — phục vụ cả màn Lịch sử bộ lọc và Lịch sử tìm kiếm:
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Thêm mới bộ lọc (ghi log) | POST `.../InsertAsync` | `LichSuBoLocController_InsertAsync` |
| Tìm theo điều kiện | POST `.../TimTheoDieuKien` | `LichSuBoLocController_TimTheoDieuKien` |
| Lấy theo người tạo | POST `.../GetLstByUserCreate` | `LichSuBoLocController_GetLstByUserCreate` |
| Xuất Excel lịch sử bộ lọc | POST `.../ExportExcelLichSuBoLoc` | `LichSuBoLocController_ExportExcelLichSuBoLoc` |
| Xuất Excel lịch sử tìm kiếm | POST `.../ExportExcelLichSuTimKiem` | `LichSuBoLocController_ExportExcelLichSuTimKiem` |

**LichSuXemThongTinController** (registry: `he-thong/LichSuXemThongTinController.md`), route `api/LichSuXemThongTin`:
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Tìm theo điều kiện | POST `.../TimTheoDieuKien` | `LichSuXemThongTinController_TimTheoDieuKien` |
| Top count tổng | POST `.../GetTopCount` | `LichSuXemThongTinController_GetTopCount` |
| Top count theo contact | POST `.../GetTopCountDetailByIdContact` | `LichSuXemThongTinController_GetTopCountDetailByIdContact` |
| Chi tiết theo user | POST `.../GetlstDetailByIdUser` | `LichSuXemThongTinController_GetlstDetailByIdUser` |
| Lịch sử chi tiết theo contact | POST `.../GetHistoryDetailByIdContact` | `LichSuXemThongTinController_GetHistoryDetailByIdContact` |
| Xuất Excel | POST `.../ExportExcelLichSuXemThongTin` | `LichSuXemThongTinController_ExportExcelLichSuXemThongTin` |

> Mọi action 3 controller này đều `[SBAuthorize]` key riêng → internal; service tự ghép `CurrentUser` để giới hạn phạm vi.

## Entities
- **`LichSuTacDong`** — bản ghi audit: activityId (ADD/UPDATE/DELETE/SELECT/ExportExcel — enum FE `enumActivity`), tableId/tableName/tableNameDev, objectId/objectName, danh sách thay đổi `activityLogs` (propertiName, oldValue, newValue), `boLoc` (filterName, value với SELECT), nguoiTao/ngayTao.
- **`LichSuBoLoc`** (+ DTO `EventLichSuBoLoc`) — log bộ lọc / tìm kiếm của user.
- **`LichSuXemThongTin`** — log mở xem thông tin KH/contact (IdUser, IdContact, thời điểm) + thống kê top count.

## Luồng code
- FE Lịch sử tác động: lọc theo `searchModelCore` (tableId, activityId, date range, keyWord) → `TimTheoDieuKien`; render diff old→new; `isSuperUser` quyết hiện nút Xóa.
- Export Excel dùng `GemBox.Spreadsheet` ở BE; `rs.Success=false` → BadRequest.
- LichSuXemThongTin có drilldown: GetTopCount (tổng) → GetTopCountDetailByIdContact / GetlstDetailByIdUser / GetHistoryDetailByIdContact.

## Bug-prone / lưu ý (path:line) — chỉ GHI, không sửa
- `LichSuBoLocController.cs:60,76` — cả 2 endpoint export (`ExportExcelLichSuBoLoc`, `ExportExcelLichSuTimKiem`) dùng cùng tên file `LichSuBoLoc.xlsx` → tải nhầm/ghi đè. Export "lịch sử tìm kiếm" nên đặt tên khác.
- `LichSuXemThongTinController.cs:77` — file Excel export đặt tên `LichSuBoLoc.xlsx` (copy-paste sai, nên là `LichSuXemThongTin.xlsx`).
- Nút Xóa lịch sử tác động chỉ gated bằng `isSuperUser` ở FE (`lich-su-tac-dong.component.html:53,139`); BE `Xoa` chỉ check key `LichSuTacDong_Xoa` + CurrentUser → đối chiếu xem có cần chặn chặt hơn ở BE.

## File source (path:line)
- BE LichSuTacDong: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\LogService\Log.API\Controllers\LichSuTacDongController.cs`.
- BE LichSuBoLoc: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\LogService\Log.API\Controllers\LichSuBoLocController.cs`.
- BE LichSuXemThongTin: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\LogService\Log.API\Controllers\LichSuXemThongTinController.cs`.
- FE Lịch sử tác động: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\caidat-hethong\lich-su-tac-dong\lich-su-tac-dong.component.html` / `.ts`.
- FE Lịch sử bộ lọc/tìm kiếm: `...\pages\caidat-hethong\Log\Log-bo-loc\` , `...\Log\Log-search\`.
- FE Lịch sử xem thông tin: `...\pages\caidat-hethong\Log\LogLichSuMoThongTinChiTiet\` (+ `LogLichSuMoThongTin\`).
- FE routing: `...\pages\caidat-hethong\caidat-hethong-routing.module.ts:252`.
- Registry node: `projects/scrm/registry/he-thong/LichSuTacDongController.md`, `.../LichSuBoLocController.md`, `.../LichSuXemThongTinController.md`.
