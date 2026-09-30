# SQL Compare Suite

Tool tự build (thay Redgate SQL Compare + SQL Data Compare) cho SQL Server — không cần license.

## ⚠️ NOLOCK (READ UNCOMMITTED)
Mọi thao tác **đọc/compare** (schema metadata + data) đều mở connection bằng `OpenRead()` → set `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED` (tương đương `WITH (NOLOCK)`). An toàn để compare **production ↔ staging** mà không khóa/bị khóa bởi traffic prod. Xem `src/SqlCompareSuite.Core/SqlReadExtensions.cs`.

- Luồng **đọc**: `SchemaReader`, `DataTableReader`, `DataCompareService`, `SqlServerBrowser` → tất cả dùng `OpenRead()`.
- Luồng **ghi** (`SqlScriptExecutor` — execute sync): KHÔNG nolock. Vì isolation level có thể dính theo connection khi trả về connection pool, executor **ép `SET TRANSACTION ISOLATION LEVEL READ COMMITTED`** ngay sau khi mở connection (trước transaction) để không thừa hưởng READ UNCOMMITTED → tránh ghi prod ở mức isolation sai.

## Tính năng

### Schema Compare
- So sánh: tables (column, data type, nullable, default, **identity + seed/increment**, computed), **PK / FK / unique / check constraints**, indexes (clustered/nonclustered, **included columns, filtered index**), views, stored procedures, functions, triggers, **sequences, synonyms, schemas, extended properties (MS_Description)**.
- **Security**: **Users, Roles, Role memberships, Permissions** (GRANT/DENY/REVOKE theo class DATABASE / SCHEMA / OBJECT).
- **Partition**: **Partition Function + Partition Scheme** (boundary tự bọc nháy theo kiểu dữ liệu).
- Phân loại: Only in Source / Only in Target / Different / Identical (+ filter trên grid).
- **Sync script đúng thứ tự phụ thuộc**: CREATE SCHEMA → roles/users/memberships → partition function → scheme → drop FK ảnh hưởng → drop object thừa → sequences → synonyms → CREATE table (topological theo FK) → ALTER (ADD/ALTER/DROP column, check, ext prop) → CREATE OR ALTER programmables → ADD FK → permissions. Guard `IF EXISTS`, ưu tiên ALTER thay vì DROP/CREATE, tuỳ chọn bọc transaction (programmable lớn dùng `sp_executesql` nối chunk để không bị truncate 4000 ký tự).
- **So với script folder / source control**: load 1 thư mục `*.sql` (git) làm Source/Target thay cho DB live (parser nhẹ — xem giới hạn ở cuối).
- **Ignore rules**: ignore whitespace / comments / constraint names / collation / identity seed / fillfactor.
- **Object filter**: include/exclude theo wildcard (`dbo.*`, `Audit.*`) và theo loại object.
- **Warning engine**: cảnh báo data loss (thu nhỏ kiểu/độ dài), object drop, column drop, dependency break, ADD cột NOT NULL không default.
- **Snapshot**: lưu schema ra file (`.snapshot`) để compare sau (vd Prod snapshot ↔ Current Prod), hoặc load snapshot làm Source/Target thay cho DB live.
- **Reports**: export HTML / XML / CSV.

### Data Compare
- So sánh rows: Only in Source / Only in Target / Different / Identical.
- **Comparison key**: primary key → unique index (fallback) → all-columns (fallback cuối) → hoặc **custom key** do bạn chọn (vd `Email`).
- **Filters**: WHERE filter, **Top-N** rows.
- **Sync script** INSERT/UPDATE/DELETE, tự `SET IDENTITY_INSERT`, chia batch `GO`.
- **Backup before sync**: tự `SELECT * INTO [table_<timestamp>]` target trước khi sync.
- **Reports**: export HTML / CSV.

### Giao diện app (WPF — phong cách Redgate)
- **Activity bar dọc** (kiểu VS Code): New Schema / New Data / toggle panel / Refresh / About.
- **Multi-tab**, tab phân biệt màu (Schema = hồng, Data = lavender) + hiện **tên project** khi mở/lưu; **đóng tab có confirm**.
- **Banner Source ⇄ Target** + chevron hướng deploy; **double-click banner = đảo chiều** (swap + so lại). Giữa banner: **Selected X of Y** + nút **Deploy**.
- **🔄 Refresh / F5** = chạy lại so sánh.
- **Grid nhóm theo loại khác biệt** (Different/Only in.../Identical) với **group header tristate + đếm sel/total + mở/đóng**; **master checkbox ở header** chọn/bỏ tất cả; mặc định **All + select all**.
- **Panel diff side-by-side** căn dòng (LCS) + **highlight đoạn khác thực sự trong dòng** (intra-line) + **gutter ✗ đỏ (add/xóa) / ➡ (đổi)** + **Previous/Next nhảy tới change** + chỉnh cỡ chữ + cuộn đồng bộ + **GridSplitter** kéo chỉnh chiều cao.
- **Deployment Wizard 3 bước** (Method → Dependencies → Deploy): script Redgate-style (header + `SET` options + transaction + `IF @@ERROR` guard + `PRINT` + footer `@Success`/rollback), tab Script/Summary/Warnings, Save/Copy/Execute. Deploy đi qua wizard.
- **Theme hồng macaron** + **icon app** riêng.

### Lưu trữ (file-based, không cần DB)
- **Saved Projects panel** (trái): Save/Load project tự hiện; double-click mở lại; Add existing / Remove.
- **Saved Connections panel**: lưu server+db có tên, áp nhanh → Source/Target.
- **Recently used servers** trong dialog Edit project.
- **Session restore**: đóng app lưu các tab đang mở (connection + bảng), mở lại tự khôi phục.
- **Password mã hóa DPAPI** (CurrentUser) trong mọi file lưu (connections / project / session).
- **Edit project dialog** (Source/Target + Test + Load DB + Save/Save as/Swap), **Execute on Target** (transaction + rollback), **Reports** (HTML/XML/CSV), **Snapshot**, **script-folder compare**.
- **CLI** cho CI/CD (xem dưới).

## Yêu cầu
- .NET 8 SDK. App là WPF (Windows). CLI + Core chạy cross-platform.
- Quyền đọc metadata + data trên các SQL Server cần so sánh.

## Build & Run (UI)
```powershell
cd D:\PROJECT\Git\scrm_claude\SQLCompare
dotnet build SqlCompareSuite.sln
dotnet run --project src\SqlCompareSuite.App\SqlCompareSuite.App.csproj
```
Exe: `src\SqlCompareSuite.App\bin\Debug\net8.0-windows\SqlCompareSuite.exe`

## CLI (CI/CD)
```powershell
# Schema: chỉ ghi report (exit 1 nếu có khác biệt — dùng làm CI gate)
sqlcompare schema /server1:SRV1 /db1:Shop_Dev /server2:SRV2 /db2:Shop_Prod /report:diff.html

# Schema: sinh script + đồng bộ luôn
sqlcompare schema /server1:SRV1 /db1:Shop_Dev /server2:SRV2 /db2:Shop_Prod /output:sync.sql /sync

# Data: 1 bảng, xuất report CSV
sqlcompare data /server1:A /db1:DbA /server2:B /db2:DbB /table:dbo.Customer /report:rows.csv

# SQL auth
sqlcompare data /server1:A /db1:DbA /user1:sa /pw1:p@ss /server2:B /db2:DbB /user2:sa /pw2:p@ss /table:dbo.Customer /sync
```
Chạy `dotnet run --project src\SqlCompareSuite.Cli` (không tham số) để xem help. Exit code: `0` identical/sync OK, `1` có khác biệt mà không `/sync`, `2` lỗi.

> Script sinh ra hướng **Target ← Source**. **Luôn review trước khi chạy trên DB thật**, đặc biệt DROP/DELETE.

## Kiến trúc
```
SqlCompareSuite.sln
├─ src/SqlCompareSuite.Core/        # engine (net8.0), không UI
│  ├─ Models/                       # DbConnectionInfo + models schema/data/comparison
│  ├─ Schema/                       # SchemaReader, SchemaComparer, SchemaScriptGenerator, SchemaCompareOptions
│  ├─ Data/                         # DataCompareService, DataTableReader, SqlLiteral
│  ├─ Execution/                    # SqlScriptExecutor (sync), SqlServerBrowser (test/list db)
│  ├─ Reporting/                    # SchemaReport, DataReport (HTML/XML/CSV)
│  ├─ Project/                      # ComparisonProject (save/load), SchemaSnapshot
│  ├─ SourceControl/                # ScriptFolderReader, ScriptObjectParser (DB ↔ folder *.sql)
│  └─ SqlReadExtensions.cs          # OpenRead() = NOLOCK cho mọi đường đọc
├─ src/SqlCompareSuite.App/         # WPF UI (net8.0-windows), multi-tab
└─ src/SqlCompareSuite.Cli/         # CLI cho CI/CD (sqlcompare.exe)
```

## Trạng thái tính năng

### ✅ ĐÃ CÓ
**Schema Compare**
- [x] So sánh DB ↔ DB, DB ↔ Snapshot, DB ↔ Script folder (git)
- [x] Tables: column, data type, nullable, default, identity (+ seed/increment), computed
- [x] Keys: Primary Key, Foreign Key, Unique constraint
- [x] Indexes: clustered/nonclustered, filtered index, included columns
- [x] Programmability: Stored Procedure, Function, Trigger, View, Synonym
- [x] Schemas, Sequences, Extended properties (MS_Description)
- [x] Security: Users, Roles, Role memberships, Permissions (GRANT/DENY/REVOKE)
- [x] Advanced: Partition Function, Partition Scheme
- [x] Difference engine (Only in Source/Target, Different, Identical)
- [x] Dependency analysis → sinh script đúng thứ tự (topological FK)
- [x] Deployment script: ưu tiên ALTER thay vì DROP/CREATE, guard IF EXISTS, tuỳ chọn transaction
- [x] Ignore rules (whitespace/comments/constraint-names/collation/identity-seed/fillfactor)
- [x] Object filter (include/exclude wildcard + theo loại)
- [x] Warning engine (data loss / drop / dependency break / NOT NULL không default)
- [x] Reports HTML / XML / CSV
- [x] Snapshot (lưu/đọc schema ra file)
- [x] Partial deploy (chọn từng object để sync)
- [x] Command line (CI/CD)

**Data Compare**
- [x] Table selection, row difference engine
- [x] Comparison key: PK → unique index → all-columns → **custom key**
- [x] Data filter (WHERE), Row filter (Top-N)
- [x] Sync script INSERT/UPDATE/DELETE + SET IDENTITY_INSERT + batch GO
- [x] Deployment preview (counts)
- [x] Backup before sync (SELECT INTO bảng `_<timestamp>`)
- [x] Reports HTML / CSV
- [x] Command line (CI/CD)

**Chung**: multi-tab, Execute on Target (transaction + rollback), detail diff viewer side-by-side, Save/Load project, Test connection, Load DB list, Swap source/target, Select all/none, **NOLOCK toàn bộ đường đọc**.

### ❌ CHƯA CÓ
**Schema**
- [ ] Security nâng cao: Application Roles, column-level permissions
- [ ] User Defined Type (UDT) + User Defined Table Type (UDTT)
- [ ] XML Schema Collection
- [ ] Temporal Tables / Graph Tables / Memory-Optimized Tables (đọc + sinh DDL)
- [ ] Full-text index, partition đầy đủ option (filegroup/compression)

**Data**
- [ ] Column mapping (map cột khác tên: `Source.Name` → `Target.FullName`)
- [ ] Large DB: chunk compare / hash compare / incremental compare (hiện target nạp vào RAM)

**Nguồn so sánh**
- [ ] Compare / restore từ **backup `.bak`** (cần hạ tầng RESTORE/VDI)
- [ ] **Restore single row / single table** từ backup
- [ ] Source Control 2 chiều thực thụ (hiện chỉ đọc folder *.sql 1 chiều)

### 🔜 TODO / Roadmap
- [ ] **P1 — Fix script-folder false-diff**: parser hiện không đọc PK/FK/index/check từ file `.sql` → bảng có thể báo *Different giả*. Cần: (a) parse constraint trong CREATE TABLE, hoặc (b) thêm ignore-option tự bật khi 1 bên là script folder.
- [ ] **P2 — UDT / UDTT / XML Schema Collection**: thêm reader (sys.types / sys.table_types / sys.xml_schema_collections) + generator. Mức trung bình.
- [ ] **P3 — Temporal / Memory-Optimized / Graph**: đọc thuộc tính bảng (system_versioning, is_memory_optimized, graph) + sinh DDL. Phức tạp.
- [ ] **P4 — Data column mapping + large-table chunk/hash compare**.
- [ ] **P5 — Backup (.bak) compare + restore single row** (Phase 4 — cần SMO/VDI, lớn nhất).
- [ ] Test thực tế với SQL Server prod ↔ staging, fix lỗi runtime.

## Giới hạn đã biết
- **Script folder ↔ live DB**: parser `.sql` là parser NHẸ (regex), chỉ đọc column name + type + nullable + identity + default; KHÔNG đọc PK/FK/index/check/ext-prop trong file → bảng có thể báo **Different giả**. Programmable (View/SP/Func/Trigger) so bằng full text nên chính xác hơn.
- **Data Compare bộ nhớ**: phía Source được stream, nhưng phía **Target nạp vào dictionary trong RAM** → bảng target hàng chục triệu row vẫn tốn RAM. Dùng WHERE/Top-N để giới hạn.
- **Schema chưa phủ**: các object ở mục "Chưa có" sẽ KHÔNG xuất hiện trong diff (coi như không tồn tại) → đừng dựa vào tool cho DB dùng nặng partition nâng cao/temporal/UDT.
- **Script sinh ra cần review tay**: đặc biệt DROP/DELETE. `Execute on Target` chạy trong transaction + rollback khi lỗi nhưng vẫn nên review.

Các phần thiếu mở rộng dần trong `Core` mà phần lớn không đụng UI (object schema mới tự hiện trong grid qua `DbObjectType`).
