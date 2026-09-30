# Dev — Báo cáo tổng hợp bán hàng

> Service: **Report** (BE .NET) · controller `BaoCaoOnlineController` (hub báo cáo online). Báo cáo THUẦN ĐỌC qua stored proc, DB `SCRM_Report`. Chỉ dành cho dev.
> Trace registry: `projects/scrm/registry/bao-cao/BaoCaoOnlineController.md` (hành động #21).

## Màn hình & route (FE)
- Nằm trong cụm màn **Báo cáo online** của FE Report (menu "Báo cáo tổng hợp bán hàng"). Route/Component cụ thể: **cần xác nhận từ FE** (chưa trace component riêng).

## Endpoint / Controller (trace registry)
`BaoCaoOnlineController` — `[TienIchAttribute(..., TI_Report_Online)]`, KHÔNG có `[SBAuthorize]` controller-level (báo cáo này public theo khóa quyền tính năng).

| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Báo cáo tổng hợp bán hàng | `POST api/BaoCaoOnline/ReportTongHopBanHang` | `BaoCaoOnlineController_ReportTongHopBanHang` |
| Xuất Excel | `POST api/BaoCaoOnline/ExportExcelReportTongHopBanHang` | `BaoCaoOnlineController_ExportExcelReportTongHopBanHang` |

- Stored proc: `PRC_SCRM_Contact_BangTongHopBanHang` (đọc từ dữ liệu contact/bán hàng).
- Chạy qua `db.Ado.UseStoredProcedure().GetDataSetAll(...)` trên `_dbDapper.CopyNew()`.

## Entities
- **Không đọc/ghi entity nội bộ.** Qua stored proc DB `SCRM_Report`. Read-only, không bắn message/event.

## Luồng code
1. Gate khoảng ngày: `CommonService.CheckCauHinhKhoangThoiGianChoPhepTimKiem` ≠ -1 → chặn.
2. Ép chi nhánh user khi không admin/super/test/CSKH + `IdChiNhanh==Empty`.
3. Lọc cơ cấu tổ chức `GetCheckPermissionViewDropdownListNhomVaUser(...)` → `IdNhomString`/`IdUserString` vào SP.
4. Chạy SP `PRC_SCRM_Contact_BangTongHopBanHang`.
5. Export build lại cùng SP → `.xlsx` (GemBox); lỗi `Service_Unavailable` → 503, lỗi khác → 500.

## Bug-prone / lưu ý
- **Log SP LUÔN bật** (`ReportService.cs` L14948): điều kiện viết `if (true || userLogin.IsSuperUser ...)` → vế `true` thừa làm `LogCommandV2` chạy MỌI request → ghi log mỗi lần gọi, overhead. (Phát hiện known-issue — KHÔNG sửa code ở session này.)
- Báo cáo này có endpoint Export riêng (khóa quyền xuất riêng `..._ExportExcelReportTongHopBanHang`) — khác báo cáo giao vận (dùng chung khóa).

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.API\Controllers\BaoCaoOnlineController.cs` (ReportTongHopBanHang + ExportExcelReportTongHopBanHang).
- BE service: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.Service\Generated\ReportService.cs:14948` (log SP luôn bật).
- Stored proc (DB): `PRC_SCRM_Contact_BangTongHopBanHang`.
- Registry node: `projects/scrm/registry/bao-cao/BaoCaoOnlineController.md`.
