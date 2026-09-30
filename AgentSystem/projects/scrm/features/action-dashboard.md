# Action Dashboard (Sandbox Action Dashboard)

> **Module**: Report | **Route FE**: `/report/sandbox-action-dashboard` | **Controller prefix BE**: `api/DashboardPro`, `api/FileImportDashboard`, `api/FilterDefinition`, `api/ReportSourceDefinition`
>
> ⚠️ **QUY TẮC DUY TRÌ TÀI LIỆU** — Mỗi lần sửa code thuộc feature Action Dashboard (BE `Report/.../ActionDashboard/*`, các Mongo collection liên quan, hoặc FE `sandbox-action-dashboard/**`) **PHẢI cập nhật lại file này** ngay trong cùng commit. Phần [Changelog](#changelog) ghi lại từng thay đổi. Đây là tài liệu nguồn (source of truth) cho mọi lần làm việc với Action Dashboard.

---

## 1. Action Dashboard là gì

Action Dashboard là một **framework dashboard linh hoạt** nằm trong module Report. Khác với các báo cáo cố định (BaoCaoCeo, BaoCaoCuaHang…), Action Dashboard cho phép **người dùng tự thiết kế dashboard**:

- Tạo nhiều **trang (page/tab)**, mỗi trang chứa nhiều **widget**.
- Mỗi widget gắn vào 1 **nguồn dữ liệu** (~19 nguồn có sẵn + nguồn import Excel + nguồn tự định nghĩa).
- Chọn **kiểu hiển thị**: bảng, cột, đường, tròn, scatter, funnel, treemap, gauge, KPI…
- Cấu hình **bộ lọc, công thức, cột tùy chỉnh, vùng đánh giá (rating zone), phân tích 4 góc (quadrant)**.
- **Import dữ liệu từ Excel** theo mẫu tự thiết kế và đưa vào dashboard như một nguồn dữ liệu.
- SuperUser có thể định nghĩa **nguồn báo cáo mới** (qua API/Stored Procedure) và **danh mục bộ lọc** dùng chung toàn hệ thống.

Trang được đăng ký trong [report-routing.module.ts](D:/WORK/SCRM/GIT/FE/scrm_frontend/src/app/pages/Report/report-routing.module.ts) với path `sandbox-action-dashboard` → `SandboxActionDashboardComponent`.

**`reportType`** mặc định = `MktTheoSanPham`. Đây là khóa phân loại config dashboard — về sau có thể có nhiều `reportType` khác nhau dùng chung framework này. `maPage` của trang = `ListMaPage_Filter.BAOCAO_MKT_THEO_SANPHAM`.

---

## 2. Backend

Toàn bộ code BE nằm trong module **Report** (`.NET 6`):

| Loại | Đường dẫn |
|------|-----------|
| Controllers | `Report/Report.API/Controllers/ActionDashboard/` |
| Services | `Report/Report.Service/Generated/ActionDashboard/` (+ `ReportCustomColumnService.cs` ở thư mục `Generated/`) |
| Mongo collections | `Report/Report.DataContext/MongoDB/` |
| Tên collection (hằng số) | `Libs/SB.Core/Contants.cs` → class `MongoDBCollection` |

Dữ liệu lưu trên **MongoDB** (DB cấu hình qua `MongoReportSettings:UrlConnect` / `:DatabaseName`), **KHÔNG dùng SQL Server** — trừ:
- `MigrateChiNhanhAsync` đọc bảng `User` (SQL, qua `ReportDataContext` EF Core).
- `ReportSourceDefinition.Execute` khi `fetchMode = storedProcedure` (gọi SP qua SqlSugar).

Cache: **Redis** (`IRedisCacheService`), pattern `GetIncludeNullOrAsync` — cache cả giá trị null để tránh hit DB lặp lại. Key sinh từ `SB.Core.CacheKey`.

### 2.1. Controllers & Endpoints

#### `DashboardProController` — `api/DashboardPro` ([file](D:/WORK/SCRM/GIT/BE/scrm/Report/Report.API/Controllers/ActionDashboard/DashboardProController.cs))
Quản lý cấu hình dashboard (pages + layout + widgets) và cột tùy chỉnh.

| Method | Route | Mô tả |
|--------|-------|-------|
| GET | `GetConfig` | Lấy config dashboard theo `user + donVi + chiNhanh + reportType` → trả JSON string |
| POST | `SaveConfig` | Lưu config (`SaveDashboardProRequest`) |
| GET | `GetCustomColumns` | Lấy cột công thức tùy chỉnh theo `user + donVi + apiSource` |
| POST | `SaveCustomColumns` | Lưu cột công thức (`SaveCustomColumnsRequest`) |
| POST | `MigrateChiNhanh` | **`[AllowAnonymous]`** — migration 1 lần: gán `IdChiNhanh` cho config/design cũ chưa có, lấy từ bảng `User` |

#### `FileImportDashboardController` — `api/FileImportDashboard` ([file](D:/WORK/SCRM/GIT/BE/scrm/Report/Report.API/Controllers/ActionDashboard/FileImportDashboardController.cs))
Quản lý **mẫu import** (design) và **dữ liệu import** từ Excel.

| Method | Route | Mô tả |
|--------|-------|-------|
| GET | `GetDesigns` | List mẫu import khả dụng theo `donVi + chiNhanh` |
| GET | `GetDesignById` | Lấy 1 mẫu theo id |
| POST | `SaveDesign` | Tạo/sửa mẫu. Chỉ SuperUser được set `IsGlobal` |
| POST | `DeleteDesign` | Xóa mẫu — 2 bước: chưa `Confirmed` → trả `recordCount`; đã confirm → xóa (kèm data nếu `DeleteData`) |
| GET | `DownloadTemplate` | Tải file Excel mẫu (header + 2 dòng sample) |
| POST | `AnalyzeExcelSchema` | Phân tích schema cột từ file Excel (khi tạo mẫu) |
| POST | `AnalyzeExcelForImport` | Phân tích file + tìm mẫu khớp (exact/partial match) |
| POST | `ImportExcel` | Import 1 lần (legacy, giữ cho backward compat) |
| POST | `ImportExcelStart` | **Import theo batch — bước 1**: parse Excel → lưu temp collection, trả `sessionId` + `totalRows` |
| POST | `ImportExcelBatch` | **Import theo batch — bước 2**: xử lý 1 batch (mặc định 300 dòng) từ temp |
| POST | `ImportExcelCleanup` | Xóa temp rows của session |
| POST | `GetData` | Lấy dữ liệu đã import (`GetImportDataRequest`) — phân trang + lọc ngày + `FilterCustom` |
| POST | `DeleteDataBatch` | Xóa dữ liệu theo design, theo batch |
| DELETE | `DeleteRecord` | Xóa 1 bản ghi data |

#### `FilterDefinitionController` — `api/FilterDefinition` ([file](D:/WORK/SCRM/GIT/BE/scrm/Report/Report.API/Controllers/ActionDashboard/FilterDefinitionController.cs))
Danh mục bộ lọc dùng chung toàn hệ thống. `GetAll`/`GetById` cho mọi user; `Create`/`Update`/`Delete` **chỉ SuperUser**.

#### `ReportSourceDefinitionController` — `api/ReportSourceDefinition` ([file](D:/WORK/SCRM/GIT/BE/scrm/Report/Report.API/Controllers/ActionDashboard/ReportSourceDefinitionController.cs))
Danh mục nguồn báo cáo tự định nghĩa. `GetAll`/`GetById`/`Execute` cho mọi user; `Create`/`Update`/`Delete`/`TestExecute`/`GetDbConnections` **chỉ SuperUser**.
- `Execute` — chạy nguồn báo cáo lúc runtime (widget) hoặc test UI.
- `TestExecute` — test cấu hình API/SP khi đang thiết lập nguồn.

### 2.2. Services

| Service | File | Trách nhiệm |
|---------|------|-------------|
| `DashboardProConfigService` | `ActionDashboard/DashboardProConfigService.cs` | Get/Save config dashboard; `MigrateChiNhanhAsync` |
| `ReportCustomColumnService` | `Generated/ReportCustomColumnService.cs` | Get/Save cột công thức tùy chỉnh (theo `user+donVi+apiSource`) |
| `FileImportDesignService` | `ActionDashboard/FileImportDesignService.cs` | CRUD mẫu import; export template Excel; **detect schema**, **match design** (exact/partial) |
| `FileImportDataService` | `ActionDashboard/FileImportDataService.cs` | Import Excel (legacy + batched); query data; xóa data; **dedup theo hash**; **accumulate mode** |
| `FilterDefinitionConfigService` | `ActionDashboard/FilterDefinitionConfigService.cs` | CRUD danh mục bộ lọc |
| `ReportSourceDefinitionConfigService` | `ActionDashboard/ReportSourceDefinitionConfigService.cs` | CRUD nguồn báo cáo; `Execute`/`TestExecute` (gọi API qua RestSharp, gọi SP qua SqlSugar) |

### 2.3. MongoDB Collections

Tất cả collection đều dùng `[BsonIgnoreExtraElements]` + `[BsonIgnoreIfNull]`.

| Collection | Class | Nội dung |
|------------|-------|----------|
| `DashboardProConfig` | `DashboardProConfig.cs` | 1 doc/`user+donVi+chiNhanh+reportType`. `ConfigJson` = `{ pages: [...] }` serialize |
| `ReportCustomColumnConfig` | `ReportCustomColumnConfig.cs` | 1 doc/`user+donVi+apiSource`. `ConfigJson` = mảng `ICustomColumn` |
| `FileImportDashboardDesign` | `FileImportDashboardDesign.cs` | Mẫu import: `Columns[]` (`FileImportColumnDef`), `UniqueKeyColumnIds`, `DateTimeColumnId`, ownership (`IsGlobal`/`IdDonVi`/`IdChiNhanh`) |
| `FileImportDashboardData` | `FileImportDashboardData.cs` | Dữ liệu đã import. `Json` = full row; `FilterDateTime` = trường ngày chính; `FilterCustom` = `BsonDocument` các trường lọc; `UniqueKeyHash` = SHA256 để dedup |
| `FileImportDashboardDataTemp` | `FileImportDashboardDataTemp.cs` | Bảng tạm cho import theo batch. Nhóm theo `SessionId`. Tự cleanup session > 2 giờ |
| `FilterDefinitionConfig` | `FilterDefinitionConfig.cs` | Định nghĩa 1 bộ lọc: `Key`, `Label`, `Type`, `OptionsJson`/`OptionsApiJson`, `ComponentId`… |
| `ReportSourceDefinitionConfig` | `ReportSourceDefinitionConfig.cs` | Định nghĩa 1 nguồn báo cáo: `SourceKey`, `FetchMode`, `ParsedApiJson`/`ParsedSpJson`, `ParameterMappingsJson`, `OutputColumnsJson`, `MaTienIchsJson`, `RoleCodesJson` |

### 2.4. Phân quyền — `CanViewAllBranches()`

`DashboardProController` và `FileImportDashboardController` đều có method `CanViewAllBranches()` (logic giống nhau, copy ở mỗi controller):

```
SuperUser  → true
IsAdmin    → true
RoleCode chứa ROLE_DV_AD_TONG hoặc ROLE_HT_CSKH → true
còn lại    → false
```

- `CanViewAllBranches = true` → tôn trọng `chiNhanhId` FE gửi (xem mọi chi nhánh).
- `CanViewAllBranches = false` → **ép `chiNhanhId = CurrentUser.IdChiNhanh`** (chỉ xem chi nhánh mình).
- `donViId` khi import/get data **luôn lấy từ token** (`CurrentUser.DonViId`), không tin FE.
- Chỉ SuperUser được set `FileImportDashboardDesign.IsGlobal`.

### 2.5. Luồng Import Excel theo batch

Import chia 3 bước để tránh timeout với file lớn:

1. **`ImportExcelStart`** → `FileImportDataService.ImportExcelStartAsync`
   - Cleanup các temp session quá hạn (> 2h).
   - Parse toàn bộ Excel → `FileImportDashboardDataTemp[]`, gán `SessionId` + `ImportMode`.
   - Mỗi dòng: map theo `design.Columns`, tính `UniqueKeyHash` = SHA256(`donVi|chiNhanh|user|designId|<các giá trị cột unique>`), build `Json`, `FilterCustomJson`, `FilterDateTime`.
   - Bulk insert vào `FileImportDashboardDataTemp`. Trả `sessionId` + `totalRows`.
2. **`ImportExcelBatch`** (FE gọi lặp đến khi `Remaining = 0`) → `ImportExcelNextBatchAsync`
   - Lấy 1 batch (mặc định 300) từ temp.
   - Bulk fetch record đã tồn tại theo `UniqueKeyHash` (1 query/batch).
   - Tách insert/update; xử lý trùng hash trong cùng batch.
   - **`importMode`**: `overwrite` = ghi đè; `accumulate` = cộng dồn các cột số (`DataType = number`), giữ nguyên cột text/date từ dòng mới.
   - Bulk insert + bulk `ReplaceOneModel`; xóa temp rows đã xử lý.
3. **`ImportExcelCleanup`** — xóa temp rows còn sót của session.

### 2.6. Luồng Execute nguồn báo cáo

`ReportSourceDefinitionConfigService.ExecuteAsync(ExecuteSourceRequest{ sourceKey, filterValues })`:
1. Load `ReportSourceDefinitionConfig` theo `SourceKey`.
2. Deserialize `ParameterMappingsJson`, `OutputColumnsJson`.
3. Resolve tham số: ưu tiên `filterValues[filterId]`, fallback `constantValue`.
4. Theo `FetchMode`:
   - `storedProcedure` → SqlSugar `UseStoredProcedure().GetDataSetAll`, connection lấy từ `ConnectionStrings:<name>`.
   - `api` → merge tham số vào body → `CallApiAsync` (RestSharp). Tự thêm header `SystemAuthorization`/`SystemUserId`/`Authorization` khi `UseSystemAuth`.
5. `ExtractDataByPath` (theo `ResponseDataPath`) → map rows theo `OutputColumns` → trả `ExecuteSourceResponse`.

`ToSqlValue` chuẩn hóa giá trị (`JsonElement`/`JValue`/chuỗi `"NULL"`) → kiểu native cho `SugarParameter`, `null` → `DBNull.Value`.

---

## 3. Frontend

`Angular 15 + ng-zorro + Highcharts + angular-gridster2`. Code tại `src/app/pages/Report/sandbox-action-dashboard/`.

### 3.1. Cây component

```
sandbox-action-dashboard.component.ts        ← container: header filter (chi nhánh, ngày T), searchModelCore
└── tab-dashboard-pro/
    ├── tab-dashboard-pro.component.ts        ← component chính (~1868 dòng): pages, widget, cache, load data
    │   ├── dashboard-toolbar/                ← thanh công cụ: tạo widget, edit layout, export, fullscreen
    │   ├── widget-item/                      ← render 1 widget (Highcharts / table / KPI), OnPush
    │   │   └── large-select/                 ← custom select cho list > 1000 item (virtual scroll)
    │   ├── template-editor/                  ← modal cấu hình 1 widget
    │   ├── formula-builder/                  ← UI dựng công thức (kéo thả field/toán tử/ngoặc)
    │   ├── custom-column-form-modal/         ← modal tạo/sửa cột công thức
    │   ├── raw-data-view/                    ← tab "Dữ liệu thô" (data từ API nguồn)
    │   ├── excel-import-view/                ← tab "Dữ liệu Excel"
    │   ├── excel-import-modal/               ← modal upload + import Excel
    │   ├── import-progress-modal/            ← tiến trình import theo batch
    │   ├── delete-progress-modal/            ← tiến trình xóa data theo batch
    │   ├── file-import-design-manager/       ← quản lý mẫu import Excel
    │   ├── filter-management/                ← quản lý danh mục bộ lọc (SuperUser)
    │   ├── source-management/                ← quản lý nguồn báo cáo (SuperUser)
    │   └── simple-date-range-picker/         ← chọn khoảng ngày (preset + custom)
    ├── dashboard-template.model.ts           ← toàn bộ interface + const (DataSourceType, field defs…)
    ├── dashboard-template.service.ts         ← gọi api/DashboardPro
    ├── file-import.model.ts                  ← interface import (design/column/result)
    ├── source-management.model.ts            ← interface filter/source definition
    ├── data-dashboard-import.service.ts      ← gọi api/FileImportDashboard
    ├── date-range.util.ts                    ← computeDateRange(preset, ngàyT)
    └── utils/
        ├── curl-parser.service.ts            ← parse cURL → {url, method, headers, body}
        └── sp-parser.service.ts              ← parse EXEC proc @p=v → {proc, params}
```

### 3.2. Component container — `SandboxActionDashboardComponent`

Kế thừa `BaseListReportComponentV1`. Chỉ phụ trách **header filter chung**, không chứa logic nghiệp vụ.
- `searchModelCore`: object filter chung — `ngayMoc` (ngày mốc T), `tuNgay`/`denNgay` (full day của T), `idChiNhanh`, `isChietKhau`, `isVat`. Truyền xuống `tab-dashboard-pro` qua `@Input`.
- **Không auto-load** khi vào trang — user phải bấm "Tìm Kiếm".
- `onSearch()`: chống spam → `syncDatesFromNgayMoc()` → **spread `searchModelCore`** tạo reference mới → `ngOnChanges` ở component con chạy → reload.

### 3.3. Component chính — `TabDashboardProComponent`

- **3 tab**: Dữ liệu thô / Dữ liệu Excel / Dashboard (nhiều page).
- State: `pages[]`, `activePage`, `sourceDataCache: Map`, `importDataMap: Map`.
- Load: `loadConfig()` (GetConfig) → `loadPageData()` → `loadAllSourceData()` + `loadImportData()`.
- **Batch API**: gom các widget cùng `(source:preset:filterHash)` thành 1 request.
- **Lazy load per tab/page** — chỉ load page đang active; cache miss mới gọi API; đổi page → prune cache.
- Layout dùng `angular-gridster2`; `saveConfig()` debounce ~400ms khi kéo/resize.
- `gridSchemaVersion` migrate layout legacy → mới (idempotent, chỉ migrate 1 lần khi load).

### 3.4. Model quan trọng — `dashboard-template.model.ts`

| Interface / const | Vai trò |
|-------------------|---------|
| `IDashboardPage` | 1 trang: `id, name, order, templates[], layout[], gridSchemaVersion` |
| `IDashboardTemplate` | Cấu hình 1 widget: `dataSource, displayType, filters[], sort, labelField, fields…` |
| `IWidgetLayout` | Vị trí widget trên grid: `templateId, x, y, cols, rows, minItemRows` |
| `IFilterRule` | Quy tắc lọc data widget — `simple` (so sánh cột) / `formula` (công thức rồi so sánh), AND logic |
| `IFormulaToken` | Token công thức infix: `field` / `op` (+−×÷) / `literal` / `paren` |
| `ICustomColumn` | Cột tùy chỉnh: `name, formula[], format` (number/currency/percent) |
| `IExtraSeries` | Chuỗi dữ liệu phụ trên chart |
| `IRatingZone` | Vùng đánh giá tô màu trục Y |
| `IQuadrantConfig` | Chia 4 góc cho scatter chart |
| `IWidgetCustomFilter` | Bộ lọc ngày riêng từng widget (`datePreset` / `custom`) |
| `IWidgetAction` | Action khi click data point — hiện chỉ `createTask` |
| `ISourceFilterDef` | Định nghĩa 1 filter control của nguồn dữ liệu |
| `DataSourceType` | ~19 nguồn: `sanPham, nguonDuLieu, summaryProduct, summaryNguon, import, kenhBanHang, tongTheCuaHang, leadByNhanSu, doanhSoTongQuat, baoCaoDoanhSoTheoNgay, trangThaiKhachHang, phiSan, congViec, tonKho, tyLeChotDonSanPham, khachHangCuMoi, doanhSoTheoGiaoVan, thoiGianPhanHoiTB, soLieuKhQuayLai` |
| `DATE_PRESET_OPTIONS` | Preset khoảng ngày tính tương đối từ ngày T |
| `PRODUCT_FIELDS`, `SOURCE_FILTER_DEFS`, … | Định nghĩa field/filter tĩnh theo từng nguồn |

`displayType` hỗ trợ: `table, bar, barh, pie, donut, line, spline, area, areaspline, scatter, bubble, funnel, treemap, radar, gauge, kpi`.

### 3.5. Service FE

| Service | Endpoint BE | Method chính |
|---------|-------------|--------------|
| `DashboardTemplateService` | `api/DashboardPro` | `loadConfig`, `saveConfig`, `loadCustomColumns`, `saveCustomColumns` |
| `DataDashboardImportService` (`data-dashboard-import.service.ts`) | `api/FileImportDashboard` | quản lý design, import Excel, download template, get/delete data |
| `FilterDefinitionService` (`services/report/filter-definition.service.ts`) | `api/FilterDefinition` | `getAll, getById, create, update, delete` + preview options |
| `ReportSourceDefinitionService` (`services/report/report-source-definition.service.ts`) | `api/ReportSourceDefinition` | `getAll, getById, create, update, delete, testExecute, execute, getDbConnections` |

---

## 4. Bản đồ FE ↔ BE

```
Header filter (chi nhánh, ngày T)        → searchModelCore
        │
TabDashboardProComponent
  ├─ loadConfig()        → GET  api/DashboardPro/GetConfig         → DashboardProConfig.ConfigJson
  ├─ saveConfig()        → POST api/DashboardPro/SaveConfig
  ├─ loadCustomColumns() → GET  api/DashboardPro/GetCustomColumns  → ReportCustomColumnConfig
  ├─ widget nguồn 'import'
  │     ├─ GetDesigns / SaveDesign / DeleteDesign  → api/FileImportDashboard → FileImportDashboardDesign
  │     ├─ ImportExcelStart/Batch/Cleanup          → FileImportDashboardDataTemp → FileImportDashboardData
  │     └─ GetData                                 → FileImportDashboardData (phân trang + lọc)
  └─ widget nguồn tự định nghĩa
        └─ POST api/ReportSourceDefinition/Execute → SP (SqlSugar) hoặc API (RestSharp)
SuperUser:
  ├─ filter-management   → api/FilterDefinition          → FilterDefinitionConfig
  └─ source-management   → api/ReportSourceDefinition     → ReportSourceDefinitionConfig
```

---

## 5. Lưu ý khi maintain

- **`widget-item` dùng `ChangeDetectionStrategy.OnPush`** — mọi thay đổi state nội bộ phải `cdr.markForCheck()`. CDK overlay (nz-popover) có thể trigger CD ngoài scope OnPush.
- **Cache FE** theo key `<source/designId>:<dateRange>:<configHash>` — sửa logic filter/config phải đảm bảo hash thay đổi, nếu không widget hiển thị data cũ.
- **`donViId` không tin FE** — BE luôn lấy từ token. Đừng thêm logic phụ thuộc `donViId` từ request body.
- **`CanViewAllBranches()` bị lặp ở 2 controller** — sửa quyền phải sửa cả `DashboardProController` và `FileImportDashboardController`.
- **`MigrateChiNhanh` đang `[AllowAnonymous]`** — chỉ là migration 1 lần; cân nhắc khóa lại hoặc xóa sau khi chạy xong production.
- **Import theo batch**: temp collection tự cleanup sau 2h; nếu FE crash giữa chừng, temp rows vẫn còn → lần `ImportExcelStart` sau sẽ dọn.
- **`accumulate` mode** chỉ cộng cột `DataType = number`; thêm cột số mới vào design sẽ tự được cộng dồn.
- **Dedup**: `UniqueKeyHash` phụ thuộc `UniqueKeyColumnIds` của design — đổi danh sách cột unique sẽ làm data import trước/sau không nhận diện trùng nhau.
- **GemBox.Spreadsheet** dùng license key hardcode (`ERDD-TNCL-YKZ5-3ZTU`) — set ở mỗi method đọc/ghi Excel.
- Query Mongo ở đây dùng `Builders<T>.Filter` (driver gốc), **không** phải LINQ `.TableWithNolock()` của SQL Server — quy tắc NOLOCK của SCRM không áp dụng cho phần này.

---

## 6. Changelog

> Mỗi lần sửa code Action Dashboard, thêm 1 dòng vào đây: `YYYY-MM-DD — mô tả thay đổi — file ảnh hưởng`.

- **2026-05-27** — Căn giữa 6 cell dữ liệu nguồn `trangThaiKhachHang` trong bảng modal/sandbox cho khớp với header. Trước đây các `<td>` ở case 1-6 (Số lead / Chưa tác nghiệp / Số đơn / Tỷ lệ chốt / Doanh số / Chiết khấu) dùng `class="text-right n-bg-progress-..."` (căn phải) nhưng header `<th>` lại `class="text-center"` → lệch trục. Fix: `class="text-center n-bg-progress-..."` cho cả 6 td. File: `raw-data-view.component.html` (line 1426/1435/1444/1453/1462/1471).
- **2026-05-27** — Fix bug modal "Xem chi tiết" widget nguồn `trangThaiKhachHang`: filter "Phòng/Nhóm/NV Sale" + "Phòng/Nhóm/NV MKT" cấu hình ở widget nhưng popup không lọc data + dropdown hiển thị RỖNG. **2 root cause song song, fix 2 chỗ**:
  - **(1) Payload BE — singular/plural mismatch**: `FILTER_STAFF_SALE.filterKeys` ([dashboard-template.model.ts:549](D:/WORK/SCRM/GIT/FE/scrm_frontend/src/app/pages/Report/sandbox-action-dashboard/tab-dashboard-pro/dashboard-template.model.ts#L549)) lưu SINGULAR (`idPhongBanSale`...) nhưng BE `ReportTrangThaiContact` expect PLURAL (`idPhongBanSales`...). `raw-data-view.fetchTrangThaiKhachHang:1026-1042` (modal) chỉ spread `...sc` AS-IS → BE ignore. Fix: thêm 6 dòng explicit map ở payload — `idPhongBanSales: sc.idPhongBanSale ?? sc.idPhongBanSales ?? null` (+ 2 cấp Sale + 3 dòng defensive MKT). Pattern khớp `tab-dashboard-pro.fetchTrangThaiKhachHangData:1310-1318`.
  - **(2) UI dropdown rỗng — FilterService chưa populate + race với async ngOnInit**: Component `<app-drop-down-list-sale-marketing-auto-complete>` (raw-data-view.html:731-756) KHÔNG đọc từ `searchModelCore` mà subscribe `_filterService.filterCacheObject$` (component file line 232-266) để render value. HTML attrs `[idPhongSaleModelCustom]="'idPhongBanSales'"` + `[idPhongMktModelCustom]="'idPhongBanMkts'"` chỉ định component đọc key PLURAL từ FilterService. Khi modal mở, FilterService trống → dropdown empty dù widget config có data. Fix:
    - Inject `FilterService` vào constructor (providers đã có sẵn ở @Component).
    - Helper `pushStaffFiltersToFilterService(cfg, idChiNhanh)` gọi `_filterService.updatefilterCacheObject(...)` — convert hết về PLURAL + wrap array (cho `isMultiChoose=true`).
    - **Event-driven timing fix** (thay vì random setTimeout): component dropdown `ngOnInit` async `callAPIGetDropdownList()` (fire-and-forget, ~vài trăm ms HTTP) → khi resolve `onChangeChiNhanh()` RESET `searchModel.* = null` (component line 286-291) → sau đó emit `(soLuongPhongBan)` event ở line 223 component. Listen event này trong raw-data-view.html (`(soLuongPhongBan)="onStaffDropdownReady()"` cho cả 2 instance Sale + MKT) → handler `onStaffDropdownReady()` push lại FilterService — chắc chắn timing đúng (post-reset, post-load-options). Push lần đầu trong `applyDetailContext` vẫn giữ để cover case modal mở lần 2 cùng instance (event không re-fire).
  Files: `raw-data-view.component.ts` + `raw-data-view.component.html`.
- **2026-05-26** — **Bổ sung nguồn 17 = `marketingDashboard`** (BaoCaoLeadTheoNguon) vào cả tab DataSandbox và Widget Action Dashboard. Endpoint: `POST /api/MarketingDashboard/TimTheoDieuKien` (controller `MarketingDashboardController.cs`) trả `BaoCaoLeadTheoNguon_GridTableDto` (`TableSummary` + `TableData` tree 3 level Landing → Source → Campaign). Reuse `MarketingDashboardService.timTheoDieuKien()` đã có (`services/landing/marketing-dashboard.service.ts:93-97`) — không tạo service mới. **Tab DataSandbox**: render dạng **tree expand** giống báo cáo gốc — block `TableSummary` (đậm) phía trên + nested rows toggle qua `[nzExpand]`. Helper `convertMktTreeToList` + `collapseMkt` + `setMktRowColor` port từ `marketing-dashboard.component.ts:680-721`. **Widget action dashboard**: **CHỈ lấy LEVEL 1 (roots = Landing)** — không flatten children vào chart (per user requirement) → mỗi cột/slice = 1 Landing, không lẫn Source/Campaign. Filter widget riêng theo `MarketingDashboardFilterDto` (reuse component `FILTER_STAFF_MKT` / `FILTER_PRODUCT_HIERARCHY` / `FILTER_LANDING_PAGE_SOURCE` / `FILTER_KET_QUA_TAC_NGHIEP` + primitive: `tenNguonDuLieu`, `maUtm`, `isChamSoc`, `loaiDoanhSo`, `loaiKetNoi`, `isKhachHangCu`, `isSoTrung`, `isChietKhau`, `isVat`, `khongGioiHanNgayChot`, `maQuocGia`). Helper `buildMktDashboardPayload` (similar `buildMktPayload`) merge `searchModelCore + cfg + strIdNguonDuLieu(joined ';') + strIdLandingSanPham(joined ',')`. Files: `dashboard-template.model.ts` (DataSourceType enum + `MKT_DASHBOARD_FIELDS` + `MKT_DASHBOARD_FILTERS` + `SOURCE_FILTER_DEFS['marketingDashboard']`), `raw-data-view.component.ts` (inject service + property `gridTableSummary`/`marketingTreeRoots`/`marketingGridData` + case switch + `fetchMarketingDashboardData` + 3 helpers), `raw-data-view.component.html` (option 17 + render tree block + exclude `marketingDashboard` khỏi bảng chung), `raw-data-view.component.css` (class `.mkt-row-summary`), `tab-dashboard-pro.component.ts` (`API_SOURCES` thêm `'marketingDashboard'` + `buildMktDashboardPayload` + case ở `loadAllSourceData` + `loadSingleWidgetData`), `template-editor.component.ts` (import `MKT_DASHBOARD_FIELDS` + case), `template-editor.component.html` (option 17), `widget-item.component.ts` (import + case). Per-widget loading tự động tham gia pipeline existing vì `buildWidgetSrcKey` generic theo `dataSource + getConfigHash`.
- **2026-05-26** — Fix bug widget render với data=[] gây flash "tầng 0 → có data sau 1-3s". Root cause: `recomputeDerived()` sau `loadConfig()` fallback `data=[]` (cache miss) → widget render chart NGAY → API resolve sau → re-render với data thật → flash. **Per-widget loading approach** (user chốt qua 2 vòng feedback — không chấp nhận render với data=[] hay defer toàn bộ dashboard): (a) `tab-dashboard-pro.component.ts` thêm `loadingSourceKeys` + `attemptedSourceKeys: Set<string>` track per-source-key state; thêm helper `buildWidgetSrcKey(tpl)` build key đồng bộ với `loadAllSourceData`/`loadImportData` (cũng fix bug nhỏ: recomputeDerived trước đó dùng `getEffectivePreset` đơn thuần, không xử lý `dateMode='custom'` → cache key mismatch); `recomputeDerived` compute thêm `srcKey + loading` cho mỗi item. (b) `loadAllSourceData` / `loadImportData` / `loadSingleWidgetData`: wrap fetch với `loadingSourceKeys.add(key)` trước, `.finally()` xóa + `attemptedSourceKeys.add(key)` + `recomputeDerived()` PER-KEY — widget data về trước render trước, không chờ widget chậm. Cache hit: `attemptedSourceKeys.add(key)` ngay (skip spinner). (c) `searchModelCore` change (header Tìm Kiếm) clear cả 2 Set. (d) HTML pass `[isLoading]="item.loading"` xuống `<app-widget-item>`. (e) `widget-item.component.ts:600` ngOnChanges thêm condition `&& !this.isLoading` + listen `changes['isLoading']` — skip buildWidget khi loading → chart chỉ render LẦN ĐẦU với data thật, không flash; `<nz-spin>` overlay sẵn có ([widget-item.component.html:50](D:/WORK/SCRM/GIT/FE/scrm_frontend/src/app/pages/Report/sandbox-action-dashboard/tab-dashboard-pro/widget-item/widget-item.component.html#L50)) hiện spinner khi loading. Edge: fetch fail vẫn add attempted → widget render data=[] (không kẹt spinner mãi); cache hit ngay từ đầu → render NGAY không spinner. **Follow-up fix (cùng ngày)**: per-widget loading approach exposed 1 latent bug — `loadConfig()` cũ comment "KHÔNG auto-load sau khi load config, chỉ load khi user bấm Tìm Kiếm" → user mở dashboard không click gì → fetch không trigger → widget loading=true forever (chưa attempted) → spinner vô tận. Fix: thêm `this.loadPageData(this.activePage)` ngay sau `recomputeDerived()` trong `loadConfig.subscribe.next` (~line 590) → auto-fetch widgets của activePage khi load lần đầu, không cần user thao tác.
- **2026-05-23** — Fix label dài funnel widget bị clip ở widget container hẹp (vd "S6: Trình bày giải pháp và demo" bị cắt thành "ình bày giải pháp và demo" trên widget Team có col-span nhỏ hơn widget chính). Root cause: `chart.renderer.text` ở `sides` mode + `dataLabels.formatter` ở `inside` mode render full string không có truncate → SVG text bị mép container clip. Fix: thêm helper `truncateName(n)` cắt với `…` khi vượt threshold (22 ký tự cho sides, 26 cho inside) — áp dụng vào cả 2 mode. Tooltip giữ tên đầy đủ (vẫn dùng `this.point.name` raw). File: `widget-item.component.ts:1022+` nhánh `isFunnel`.
- **2026-05-23** — Fix 2 vấn đề ở tab DataSandbox nguồn 8 `trangThaiKhachHang`: (a) **CSS gạch chân**: HTML có `[class.link-text]` và `[class.box-progress-button]` (port từ báo cáo gốc) nhưng `raw-data-view.component.css` thiếu rule → user không thấy gạch chân để phân biệt cell click được (kieuXem=4 Sale / 5 MKT). Thêm rule `.link-text { text-decoration: underline; text-decoration-color: #0000ff; text-underline-offset: 4px }` + `.box-progress-button { cursor: pointer; transition: ... }` copy từ `report-trang-thai-khach-hang.component.scss`. (b) **Modal drill-down "Danh sách contact theo trạng thái" empty**: `onShowContactTrangThaiKH` (~line 1287) chỉ spread `...sc` mà không map sandbox key → BE key — sandbox dùng `locTrangThaiKH`/`strIdNguonDuLieu` còn BE expect `trangThaiKhachHang`/`idLandingSanPham`. Fix: thêm mapping inline giống `fetchTrangThaiKhachHang` (line 1014-1038): `trangThaiKhachHang: sc.locTrangThaiKH ?? 1`, `idLandingSanPham: convertStrNguon(sc.strIdNguonDuLieu)`. Files: `raw-data-view.component.css` + `raw-data-view.component.ts`.
- **2026-05-23** — Fix bug widget MKT (nguồn `nguonDuLieu` / `sanPham` / `summaryProduct` / `summaryNguon`) **bỏ qua filter "Nguồn dữ liệu"** của widget. User report: widget config chọn "Shopee + Tiktok - Tên shop" nhưng kết quả ra cả GT Duyên Hải, GT Hà Nội, GT Tây Bắc... Root cause: 8 chỗ trong `tab-dashboard-pro.component.ts` (4 case × 2 hàm `loadAllSourceData`/`loadSingleWidgetData`) chỉ spread `...this.searchModelCore` (global dashboard) mà KHÔNG merge `cfg` (widget `sourceFilterConfig`) — khác hẳn các nguồn khác (`kenhBanHang`, `doanhSoTongQuat`, ...) đều dùng `c('strIdNguonDuLieu', null)`. Thêm filter nào cũng: lưu vào cfg → BE không nhận → vô hiệu hóa. Fix: thêm helper `buildMktPayload(cfg, tuNgay, denNgay, pageInfo, sorts)` (~line 851) merge `{ ...searchModelCore, ...cfg, strIdNguonDuLieu: <converted>, tuNgay, denNgay, pageInfo, sorts }`. Convert `cfg.strIdNguonDuLieu` array → string với `;` separator (theo convention `isConvertToString=true` của `app-drop-down-list-landing-page-soure` ở báo cáo gốc `bao-cao-mkt-theo-san-pham.component.html:215-218`). Override thứ tự: searchModelCore → cfg (widget override global) → strIdNguonDuLieu converted (override cfg.array) → tuNgay/denNgay/page (luôn override). Thay 8 chỗ gọi `baocaoMktTheoSanpham({...})` thành `baocaoMktTheoSanpham(this.buildMktPayload(...))`.
- **2026-05-23** — Debug Gauge widget hiển thị 0% cho segment trạng thái KH "S15: Đã Upsales" (báo cáo gốc 65.9M). User confirm template `extraSeries[i].field` lưu đúng key composite (vd `"515"`) → bug ở data layer. Hai thay đổi: (a) `widget-item.component.ts` thêm `console.warn('[gauge-debug]', ...)` (~line 1567) — chỉ trigger khi source là `trangThaiKhachHang` và có series val=0; log series field + sample row[0] keys → user/khách F12 paste lại để pinpoint root cause (BE filter loại trạng thái 15, BE trả doanhSo=0, hay ID format mismatch giữa `GetAllTrangThaiKhachHangByDonVi` vs `ReportTrangThaiContact`). (b) **Defensive fix** trong `tab-dashboard-pro.fetchTrangThaiKhachHangData`: thêm tham số `tpl?: IDashboardTemplate`, scan `tpl.yField` + `tpl.extraSeries[].field`, parse char đầu thành `optId` (1-6), tự ép include vào `optionChoices` payload. Tránh case widget config inconsistent (template lưu field "515" nhưng `sourceFilterConfig.optionChoices` không có 5 → BE không sinh `row[515]`). Update 2 caller pass `tpl`. Log debug + caller signature sẽ cleanup sau khi xác định root cause cụ thể.
- **2026-05-20** — Tạo tài liệu lần đầu. Trạng thái code khảo sát: 4 controller, 6 service, 7 Mongo collection (BE); cây component FE `sandbox-action-dashboard` đầy đủ. CodeMap SCRM index ActionDashboard từ bản scan 2026-04-24.
- **2026-05-20** — Nâng cấp phần thể hiện widget funnel (FE, giữ nguyên logic nguồn dữ liệu). Thêm 6 field vào `IDashboardTemplate`: `funnelLabelPosition` (inside/sides — nhãn 2 bên kiểu phễu bán hàng), `funnelLabelShowName/Value/Percent` (bật/tắt nội dung nhãn), `funnelNeck` (normal/trapezoid/point — đáy nhọn tới điểm), `funnelLegendPosition` (bottom/top/left/right/hidden). Sửa nhánh funnel `buildChart()` (widget-item.component.ts ~954-1085): dataLabels dạng array khi `sides`, palette `FUNNEL_PALETTE` cho tầng chưa set màu, legend theo vị trí cấu hình. Thêm control panel funnel trong template-editor. Giữ `funnelShape`/`funnelShowPercent` cũ trong model cho backward-compat.
- **2026-05-20 (VChart — ĐÃ REVERT)** — Từng thử chuyển funnel sang **VChart** (`@visactor/vchart`) để vẽ bề rộng tầng tỉ lệ giá trị, nhưng **user đã revert toàn bộ** (gỡ package, gỡ code VChart). Funnel **giữ render bằng Highcharts** — trạng thái như vòng 2. Không còn `funnelEngine`, `renderFunnelVChart`, `@visactor/vchart` trong code.
- **2026-05-20** — Feature "Xem chi tiết widget": mỗi widget có nút (icon `table`) → mở modal xem dữ liệu chi tiết. Modal tái dùng `raw-data-view` (nguồn API) / `excel-import-view` (nguồn Excel) ở **chế độ chỉ-xem** (`readOnly`): vùng điều khiển (chọn nguồn + bộ lọc + ngày) vẫn hiển thị nhưng disable (`pointer-events:none`), chỉ phân trang hoạt động. Giao tiếp widget→modal qua **1 object** `IWidgetDetailContext` (model mới) — chứa source/designId/sourceFilterConfig/ngày; component con tự `applyDetailContext()`: gộp filter vào `searchModelCore`, set nguồn, gọi `onSourceChange()`/auto-chọn design. Files: `dashboard-template.model.ts` (interface), `widget-item` (@Output `viewDetailClick`+nút), `tab-dashboard-pro` (`openWidgetDetailModal()`), `raw-data-view` + `excel-import-view` (@Input `detailContext`/`readOnly` + `applyDetailContext` + HTML bọc `rdv-readonly`/`eiv-readonly` + CSS). `summaryProduct/summaryNguon` map → `sanPham/nguonDuLieu`. Fix tiếp: (1) nút toggle panel bộ lọc trong modal vẫn click được (`.rdv-readonly .rdv-filter-toggle { pointer-events:auto }`) + mở sẵn panel để user xem filter; (2) modal luôn truyền ngày dạng `custom` với khoảng ngày HIỆU LỰC (`getEffectiveDateRange`) → hiện rõ từ–đến thay vì preset rỗng. (3) Chế độ view: ẩn nút "Thêm cột công thức" + các nút Excel ("Mẫu Excel", "Nhập dữ liệu", "Quản lý mẫu", reload mẫu) qua `*ngIf="!readOnly"`; ẩn icon sửa/xóa cột công thức trên header bảng. (4) Fix nút toggle filter trong modal "không click được": chuyển toolbar `rdv-header` ra NGOÀI wrapper `.rdv-readonly` (wrapper giờ chỉ bọc panel lọc + col-picker); các control toolbar khóa qua `[nzDisabled]` (source/date/preset/kieuNgay/tham chiếu), `app-simple-date-range-picker` không có disabled input → khóa qua class `.rdv-ro-lock` (pointer-events:none + opacity). Nút toggle filter giờ ở vùng tự do → click được mở/đóng panel xem filter. (5) Truyền thêm các mảng dropdown options (`lstKho`, `lstDonViGiaoHang`, `lstDmGiaoHangTrangThai`, `lstQuocGia`, `lstKenhTiepNhan`, `lstKieuXem`, `roleCodeMKT`) vào `IWidgetDetailContext` để panel bộ lọc trong modal hiển thị ĐÚNG TÊN giá trị đang chọn (trước đó thiếu → nz-select chỉ hiện ID). (6) Đảo: bỏ getter `showFilterPanel`, panel `*ngIf` quay về `&& showAdvancedFilter`, nút toggle filter hiện luôn trong modal (xóa `*ngIf="!readOnly"`). Panel responds đúng theo toggle: mặc định panel mở (vì `showAdvancedFilter=true` từ `applyDetailContext`); user toggle để đóng/mở. Toggle button click được nhờ toolbar `rdv-header` đã nằm NGOÀI wrapper `.rdv-readonly` (pointer-events:none). (7) Sau build user báo panel **vẫn KHÔNG hiện** trong modal mặc dù `showAdvancedFilter=true` set ở 3 chỗ (field init + `applyDetailContext` + `onSourceChange`) — kết luận có CD/timing issue khi mở qua NzModal, không tìm được nguyên nhân chính xác. Fix triệt để: thêm getter `showFilterPanel = readOnly || showAdvancedFilter` trong `raw-data-view.component.ts`; 11 panel `*ngIf` đổi sang `&& showFilterPanel`; **giữ nguyên** toggle button (line 72-73 vẫn dùng `showAdvancedFilter`) → ở modal panel LUÔN hiện độc lập state (vì `readOnly=true` short-circuit getter), toggle button vẫn click được và icon đổi màu theo `showAdvancedFilter`. Trang gốc DataSandbox (`readOnly=false`): hành vi cũ — toggle mở/đóng panel qua `showAdvancedFilter`. (8) Sau build vẫn không hiện — bullet-proof thêm: inline `(readOnly || showAdvancedFilter)` trực tiếp vào 11 *ngIf (bỏ getter để loại bỏ rủi ro AOT optimize), inject `ChangeDetectorRef` và `_cdr.detectChanges()` ở cuối `applyDetailContext` (force re-render sau khi set state), thêm `console.log` tạm để confirm `readOnly`/`activeSource`/`showAdvancedFilter` ở F12 console khi mở modal. (9) **ROOT CAUSE thật** từ F12 console: `ERROR NullInjectorError: No provider for FilterService! at DropDownListSaleMarketingAutoCompleteComponent_Factory`. `console.log` xác nhận `readOnly=true, activeSource=trangThaiKhachHang, showAdvancedFilter=true, showFilterPanel=true` → panel ĐANG render, nhưng `<app-drop-down-list-sale-marketing-auto-complete>` (Sale + MKT autocomplete bên trong panel `trangThaiKhachHang` line 731+745) throw ở constructor do thiếu DI → Angular abort render TOÀN BỘ panel parent. `FilterService` (`shared/services/filter.service.ts`) `@Injectable()` không có `providedIn` → phải provide ở component-level. Các page báo cáo gốc (vd `report-trang-thai-khach-hang.component.ts:45`) đều có `providers: [TableService, FilterService, ...]`. Modal "Xem chi tiết" không có parent page nào provide hộ → fail. Fix: thêm `providers: [FilterService]` vào `@Component` decorator của `RawDataViewComponent` (import `FilterService` từ `src/app/shared/services/filter.service`). Hậu quả: panel render đầy đủ trong modal cho cả các nguồn có MKT autocomplete (`sanPham`, `nguonDuLieu`, `trangThaiKhachHang`, các nguồn 12/13/14/16 dùng panel SHARED). (10) Cleanup sau khi đã fix root: bỏ getter `showFilterPanel` không dùng (đã inline `(readOnly || showAdvancedFilter)`), bỏ `console.log` debug; ẩn 10 nút "Tìm kiếm" (`(click)="loadSourceData()"`) trong các panel bằng `*ngIf="!readOnly"` (chế độ chỉ-xem không cho re-search); ẩn luôn nút toggle filter trong modal (`*ngIf="!readOnly"`) — panel đã LUÔN hiện qua inline expression nên toggle vô nghĩa và gây hiểu nhầm cho user.
- **2026-05-20 (vòng 2b)** — Funnel sides mode: nhãn phải đổi format `giá trị + % sát nhau` → **`giá trị (%)`** (% trong ngoặc) khi bật cả 2; chỉ một trong hai thì hiện riêng. Sửa render-event vẽ nhãn 2 bên trong nhánh `if(isFunnel)` của `buildChart()`.
- **2026-05-20 (vòng 2)** — Sửa lỗi hiển thị funnel. (1) Nhãn `inside` thiếu `inside: true` → thêm vào để nhãn nằm GIỮA tầng (trước bị Highcharts đẩy ra ngoài mờ). (2) Nhãn `sides` đổi từ `dataLabels` array (funnel Highcharts không tách được trái/phải) sang VẼ THỦ CÔNG trong `chart.events.render`: dùng `point.dlBox` tính vị trí, `chart.renderer.text/path` vẽ tên trái + giá trị/% phải + đường nối; dọn nhãn cũ qua `chart.__funnelSideLabels`. (3) Thêm field `funnelEqualHeight` — tầng cao đều nhau (feed `y` hằng số cho hình học, giữ giá trị thật ở `realY`; formatter/tooltip đọc qua `point.options.realY`). Default funnel mới = true, funnel cũ = false. Thêm checkbox trong template-editor.
