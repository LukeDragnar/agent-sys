---
controller: DashboardProController
module: bao-cao
service: Report
man_hinh_ui: "Báo cáo > Dashboard Pro (dashboard cấu hình được): lưu/đọc cấu hình bố cục dashboard theo user + đơn vị + chi nhánh + loại báo cáo (reportType, mặc định 'MktTheoSanPham'); lưu/đọc cấu hình cột tùy chỉnh (custom column) theo apiSource. Không có lưới dữ liệu — chỉ là backend config cho dashboard cấu hình."
muc_dich: "Quản lý cấu hình mẫu dashboard cấu hình được (DashboardPro): config bố cục + cột tùy chỉnh, lưu MongoDB + cache Redis. Có 1 endpoint migration 1 lần gán IdChiNhanh cho config/design cũ."
alias: ["dashboard pro", "cấu hình dashboard", "dashboard cấu hình", "custom column báo cáo", "cột tùy chỉnh báo cáo", "config dashboard"]
auth: mixed
state_machine: []
deprecated: false
entities: [DashboardProConfig, ReportCustomColumnConfig, FileImportDashboardDesign, User]
stored_procs: []
depends: [DashboardProConfigService (Mongo coll DashboardProConfig + EF ReportDataContext.User cho migration), ReportCustomColumnService (Mongo coll ReportCustomColumnConfig), Redis cache, MongoReportSettings (Mongo SCRM_Report)]
cross_module_in: []
cross_module_out: [FileImportDashboardController (migration cùng gán IdChiNhanh cho FileImportDashboardDesign)]
---

# DashboardProController — Cấu hình mẫu báo cáo / Dashboard Pro

> Controller config (KHÔNG phải report đọc SP). Lưu cấu hình dashboard cấu hình được vào **MongoDB** (`DashboardProConfig`, `ReportCustomColumnConfig`) qua Redis cache (TTL 30 phút). `[TienIchAttribute(..., TI_Report_Chung)]` (utility tag, không auth); hầu hết method `[SBAuthorize]` (public) trừ `MigrateChiNhanh` là `[AllowAnonymous]` (anonymous, comment bỏ check SuperUser). Config gắn theo bộ khóa **user + donViId + chiNhanhId + reportType** (DashboardPro) hoặc **user + donViId + apiSource** (custom column). Gate chi nhánh: `ResolveChiNhanhId` — nếu không `CanViewAllBranches()` (SuperUser/Admin/DV_AD_TONG/HT_CSKH) thì ép `chiNhanhId` về chi nhánh của user. `MigrateChiNhanhAsync` là job 1 lần gán IdChiNhanh từ bảng `User` (SQL Server, EF) cho cả `DashboardProConfig` lẫn `FileImportDashboardDesign` cũ chưa có chi nhánh.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình dashboard | (load layout) | `GET api/DashboardPro/GetConfig?donViId&chiNhanhId&reportType` | `[SBAuthorize]` | public | mở dashboard pro | Resolve chi nhánh theo quyền; đọc Redis trước, fallback Mongo `DashboardProConfig` theo user+donVi+chiNhanh+reportType; cache 30 phút; trả `ConfigJson` (chuỗi). | **Mongo** `DashboardProConfig` (+ Redis) | — |
| 2 | Lưu cấu hình dashboard | Lưu layout | `POST api/DashboardPro/SaveConfig` | `[SBAuthorize]` | public | sau khi sửa layout | Resolve chi nhánh; upsert (ReplaceOne IsUpsert) doc theo bộ khóa; xóa cache Redis. Trả true. | **Mongo** `DashboardProConfig` upsert | — |
| 3 | Lấy cấu hình cột tùy chỉnh | (load custom columns) | `GET api/DashboardPro/GetCustomColumns?donViId&apiSource` | `[SBAuthorize]` | public | màn báo cáo có custom column | Redis trước, fallback Mongo `ReportCustomColumnConfig` theo user+donVi+apiSource; cache 30 phút; trả `ConfigJson`. | **Mongo** `ReportCustomColumnConfig` (+ Redis) | — |
| 4 | Lưu cấu hình cột tùy chỉnh | Lưu cột | `POST api/DashboardPro/SaveCustomColumns` | `[SBAuthorize]` | public | sau khi sửa cột | Upsert doc theo user+donVi+apiSource; xóa cache. Trả true. | **Mongo** `ReportCustomColumnConfig` upsert | — |
| 5 | Migration gán IdChiNhanh (1 lần) | (không có UI — chạy thủ công) | `POST api/DashboardPro/MigrateChiNhanh` | `[AllowAnonymous]` | anonymous | chạy 1 lần khi nâng cấp | Quét `DashboardProConfig` + `FileImportDashboardDesign` chưa có IdChiNhanh → tra bảng `User` (SQL Server, EF) lấy IdChiNhanh theo userId/createdByUserId → update Mongo. Trả số đã update + NotFound. | **EF** `ReportDataContext.User` (đọc) + **Mongo** update 2 collection | Đã comment toàn bộ check `IsSuperUser` (L74-75) + dùng `[AllowAnonymous]` (L70) → endpoint migration KHÔNG bảo vệ quyền. |

## Quan hệ
- **Gọi sang:** `DashboardProConfigService` (Mongo + EF), `ReportCustomColumnService` (Mongo). Migration đọc EF `ReportDataContext.User`.
- **Dữ liệu (entities):** Mongo collections `DashboardProConfig`, `ReportCustomColumnConfig`, `FileImportDashboardDesign` (chỉ trong migration); EF entity `User` (chỉ đọc IdChiNhanh trong migration).
- **Ripple / đồng bộ:** `MigrateChiNhanh` đụng chung `FileImportDashboardDesign` của `FileImportDashboardController` → 2 subsystem chia sẻ cùng concept IdChiNhanh.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.API/Controllers/ActionDashboard/DashboardProController.cs`
  - **L69-79 — Migration không bảo vệ quyền:** `[SBAuthorize]` bị comment, thay `[AllowAnonymous]`; check `CurrentUser.IsSuperUser` cũng comment → bất kỳ ai gọi cũng chạy migration được. Nên đóng lại sau khi migrate xong.
  - `ResolveChiNhanhId` (L81) trả `chiNhanhId ?? ""` khi xem-tất-cả, ngược lại ép chi nhánh user — đúng pattern gate chi nhánh chung của subsystem.
- BE `Report/Report.Service/Generated/ActionDashboard/DashboardProConfigService.cs`
  - `MigrateChiNhanhAsync` (L96) là job nặng (load toàn bộ config + design + user) — chỉ nên chạy 1 lần.
- Cấu hình lưu Mongo `SCRM_Report` (`MongoReportSettings`), KHÔNG đi stored proc — khác hẳn các Report controller đọc SP.
