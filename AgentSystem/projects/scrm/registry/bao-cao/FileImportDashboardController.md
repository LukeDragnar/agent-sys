---
controller: FileImportDashboardController
module: bao-cao
service: Report
man_hinh_ui: "Báo cáo > Dashboard import file: quản lý mẫu import (design) từ Excel + import/đọc/xóa dữ liệu đã import. Gồm: danh sách & tạo/sửa/xóa mẫu, tải template Excel, phân tích schema từ file, import Excel (legacy 1-call + batched start/batch/cleanup), xem dữ liệu (paging + filter ngày/custom), xóa theo batch / xóa 1 bản ghi."
muc_dich: "Subsystem import dữ liệu ngoài (Excel) vào dashboard cấu hình: định nghĩa mẫu cột (design) + import dữ liệu theo mẫu, lưu MongoDB. Hỗ trợ import lớn theo batch (temp collection) + 2 chế độ overwrite/accumulate (cộng dồn cột số theo unique-key hash)."
alias: ["import file dashboard", "import excel dashboard", "mẫu import báo cáo", "file import design", "dashboard import dữ liệu", "upload excel báo cáo"]
auth: public
state_machine: []
deprecated: false
entities: [FileImportDashboardDesign, FileImportDashboardData, FileImportDashboardDataTemp, FileImportColumnDef]
stored_procs: []
depends: [FileImportDesignService (Mongo coll FileImportDashboardDesign + GemBox Excel), FileImportDataService (Mongo coll FileImportDashboardData/Temp + GemBox), Redis cache, MongoReportSettings (Mongo SCRM_Report)]
cross_module_in: [DashboardProController (MigrateChiNhanh gán IdChiNhanh cho FileImportDashboardDesign)]
cross_module_out: []
---

# FileImportDashboardController — Import dữ liệu dashboard từ Excel

> Controller config/import (KHÔNG đọc SP). Toàn bộ ghi/đọc **MongoDB** (`FileImportDashboardDesign` = mẫu cột, `FileImportDashboardData` = dữ liệu, `FileImportDashboardDataTemp` = staging cho import batch), parse Excel bằng **GemBox.Spreadsheet**. `[TienIchAttribute(..., TI_Report_Chung)]`; mọi method `[SBAuthorize]` (public). Gate chi nhánh chung: `CanViewAllBranches()` (SuperUser/Admin/DV_AD_TONG/HT_CSKH) → giữ chiNhanhId FE gửi; ngược lại ép về chi nhánh user. donViId LUÔN lấy từ token (FE gửi bị bỏ qua). Import lớn theo 3 bước: `ImportExcelStart` (parse → temp) → `ImportExcelBatch` lặp (300 dòng/lần, upsert theo `UniqueKeyHash`) → `ImportExcelCleanup`. Mode `accumulate` cộng dồn các cột `DataType=number` lên bản ghi cùng hash; `overwrite` ghi đè. Tạo/xóa mẫu có phân quyền: chỉ SuperUser được set `IsGlobal`; user thường chỉ xóa mẫu của đơn vị mình.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách mẫu import | Lưới mẫu | `GET api/FileImportDashboard/GetDesigns?donViId&chiNhanhId` | `[SBAuthorize]` | public | màn quản lý mẫu | Resolve chi nhánh theo quyền; trả mẫu IsGlobal + của đơn vị (+ khớp/null chi nhánh). Redis cache 1 phút. | **Mongo** `FileImportDashboardDesign` | — |
| 2 | Chi tiết 1 mẫu | (mở mẫu) | `GET api/FileImportDashboard/GetDesignById?id` | `[SBAuthorize]` | public | mở mẫu | Đọc mẫu theo id (Redis cache 10 phút). | **Mongo** `FileImportDashboardDesign` | — |
| 3 | Lưu mẫu | Lưu mẫu | `POST api/FileImportDashboard/SaveDesign` | `[SBAuthorize]` | public | tạo/sửa mẫu | Chỉ SuperUser được set `IsGlobal`; nếu không global → ép IdDonVi/IdChiNhanh của user nếu trống. Upsert + xóa cache. | **Mongo** `FileImportDashboardDesign` upsert | — |
| 4 | Xóa mẫu (2 bước) | Xóa mẫu | `POST api/FileImportDashboard/DeleteDesign` | `[SBAuthorize]` | public | nút xóa | Bước 1 (chưa Confirmed): trả `recordCount` để FE hiện modal. Bước 2 (Confirmed): nếu DeleteData → xóa toàn bộ data; soft-delete mẫu (IsDelete=true), phân quyền global/đơn vị. | **Mongo** `FileImportDashboardData` (xóa) + `Design` (soft-delete) | — |
| 5 | Tải template Excel | Tải mẫu | `GET api/FileImportDashboard/DownloadTemplate?designId` | `[SBAuthorize]` | public | màn mẫu | Sinh file .xlsx (header cột + 2 dòng mẫu) bằng GemBox; trả file. 404 nếu không có mẫu. | (sinh file GemBox) | — |
| 6 | Import Excel (legacy 1-call) | Import (cũ) | `POST api/FileImportDashboard/ImportExcel` (form-data) | `[SBAuthorize]` | public | (giữ backward compat) | Parse Excel toàn bộ → upsert từng dòng theo UniqueKeyHash. Trả Total/Success/Error. | **Mongo** `FileImportDashboardData` upsert | Giữ cho tương thích cũ; import lớn nên dùng #7-9. |
| 7 | Import batched — bắt đầu | Import (bước 1) | `POST api/FileImportDashboard/ImportExcelStart` (form-data, importMode) | `[SBAuthorize]` | public | import file lớn | Cleanup temp session quá hạn (>2h); parse Excel → `FileImportDashboardDataTemp` (gắn sessionId + mode). Trả sessionId + totalRows. | **Mongo** `FileImportDashboardDataTemp` insert | — |
| 8 | Import batched — chạy batch | Import (bước 2, lặp) | `POST api/FileImportDashboard/ImportExcelBatch` | `[SBAuthorize]` | public | sau Start | Lấy ≤batchSize (mặc định 300) temp rows → bulk upsert main; overwrite/accumulate theo hash (cộng dồn cột số); xóa temp đã xử lý. Trả Inserted/Overwritten/Accumulated/Remaining. | **Mongo** `Data` upsert + `Temp` delete | — |
| 9 | Import batched — dọn session | (tự gọi khi xong/hủy) | `POST api/FileImportDashboard/ImportExcelCleanup` | `[SBAuthorize]` | public | sau khi xong/hủy | Xóa toàn bộ temp rows của sessionId. | **Mongo** `FileImportDashboardDataTemp` delete | — |
| 10 | Xem dữ liệu đã import | Lưới dữ liệu | `POST api/FileImportDashboard/GetData` | `[SBAuthorize]` | public | màn xem dữ liệu | donViId/userId từ token; ép chi nhánh nếu không xem-tất-cả. Lọc theo design + ngày (FilterDateTime) + FilterCustom (text contains regex / range `_from`/`_to`); paging (PageSize>0) hoặc lấy tất (widget). | **Mongo** `FileImportDashboardData` query | — |
| 11 | Xóa dữ liệu theo batch | Xóa data (lặp) | `POST api/FileImportDashboard/DeleteDataBatch` | `[SBAuthorize]` | public | xóa data lớn | Xóa ≤batchSize bản ghi theo designId; trả Deleted + Remaining (lặp tới hết). | **Mongo** `FileImportDashboardData` delete | — |
| 12 | Xóa 1 bản ghi | Xóa dòng | `DELETE api/FileImportDashboard/DeleteRecord?id&donViId` | `[SBAuthorize]` | public | nút xóa dòng | Xóa 1 doc theo id. | **Mongo** `FileImportDashboardData` delete | `donViId` query không dùng để lọc khi xóa (xóa thuần theo id) — xem lưu ý. |
| 13 | Phân tích schema (tạo mẫu) | (khi tạo mẫu) | `POST api/FileImportDashboard/AnalyzeExcelSchema` (form-data) | `[SBAuthorize]` | public | wizard tạo mẫu | Detect cột từ file (header + suy kiểu 3 dòng đầu, bỏ cột STT) → trả `FileImportColumnDef[]`. | (GemBox parse) | — |
| 14 | Phân tích + tìm mẫu khớp (khi import) | (khi import) | `POST api/FileImportDashboard/AnalyzeExcelForImport` (form-data) | `[SBAuthorize]` | public | wizard import | Detect cột → tìm exact-match design; nếu không → partial-match (file là superset) trả thêm ExtraColumns; nếu không → trả detectedCols để tạo mẫu mới. | **Mongo** `Design` (đọc) + GemBox | — |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Quyền xem-tất-cả chi nhánh** (`CanViewAllBranches`): SuperUser | Admin | role `DV_AD_TONG` | role `HT_CSKH` → không bị ép chi nhánh; ngược lại ép `IdChiNhanh` của user.
- **donViId luôn từ token** (GetData/ImportExcelStart ép `CurrentUser.DonViId`), FE gửi bị bỏ qua.
- **IsGlobal chỉ SuperUser** được set (SaveDesign): non-global ép IdDonVi user.
- **UniqueKeyHash** = SHA256 của `donVi|chiNhanh|user|designId|<các cột UniqueKeyColumnIds>` → quyết định upsert/cộng dồn.
- **Mode import:** `accumulate` → cộng dồn cột `DataType=number` (kể cả trùng hash trong cùng batch); `overwrite` (mặc định) → ghi đè.

## Quan hệ
- **Gọi sang:** `FileImportDesignService`, `FileImportDataService` (Mongo + GemBox Excel). Không gọi service ngoài Report.
- **Dữ liệu (entities):** Mongo `FileImportDashboardDesign`, `FileImportDashboardData`, `FileImportDashboardDataTemp` (DB `SCRM_Report`). KHÔNG đi stored proc/EF.
- **Ripple / đồng bộ:** `DashboardProController.MigrateChiNhanh` cũng cập nhật `FileImportDashboardDesign.IdChiNhanh` (cạnh vào). Dữ liệu import sau đó được hiển thị qua dashboard cấu hình (DashboardPro / ReportSourceDefinition).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.API/Controllers/ActionDashboard/FileImportDashboardController.cs`
  - **L176-180 `DeleteRecord`:** nhận `donViId` qua query nhưng `DeleteRecordAsync(id, donViId)` (service L520) chỉ filter theo `id`, KHÔNG dùng donViId → về lý thuyết xóa được bản ghi khác đơn vị nếu biết id. Cân nhắc thêm filter donVi.
  - `ImportExcel` (L100, legacy) vẫn còn — import lớn dễ timeout, nên dùng cụm batched #7-9.
- BE `Report/Report.Service/Generated/ActionDashboard/FileImportDataService.cs`
  - `ImportExcelNextBatchAsync` (L310) xử lý kỹ trùng-hash trong cùng batch (L399) để cộng dồn đúng; có `GroupBy` chống duplicate-key khi DB đã có records trùng hash (L336).
  - Temp session tự cleanup khi quá 2h (`CleanupExpiredTempSessionsAsync` L473) gọi trong ImportExcelStart.
  - License GemBox hardcode `"ERDD-TNCL-YKZ5-3ZTU"` lặp nhiều nơi (L220, L540, FileImportDesignService L134/167).
