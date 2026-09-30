---
controller: ReportSourceDefinitionController
module: bao-cao
service: Report
man_hinh_ui: "Báo cáo > Quản lý nguồn báo cáo dashboard (màn admin cấu hình + runtime widget): CRUD định nghĩa nguồn báo cáo (ReportSourceDefinition) — mỗi nguồn là 1 SP hoặc API kèm cấu hình tham số (parameterMappings: filterId→param), cột đầu ra (outputColumns), responseDataPath. Execute để runtime widget lấy dữ liệu; TestExecute/GetDbConnections cho màn cấu hình của SuperUser."
muc_dich: "CRUD + thực thi 'nguồn báo cáo động' cho dashboard cấu hình: nguồn có thể là Stored Procedure (SqlSugar GetDataSetAll trên connection cấu hình) HOẶC REST API (RestSharp). Execute map filterValues → tham số rồi chạy SP/API, trả columns + rows + rawJson. Định nghĩa lưu MongoDB."
alias: ["nguồn báo cáo", "report source", "report source definition", "định nghĩa nguồn báo cáo", "execute nguồn báo cáo", "widget nguồn dữ liệu"]
auth: public
state_machine: []
deprecated: false
entities: [ReportSourceDefinitionConfig]
stored_procs: ["<động — SP do admin cấu hình runtime trong ParsedSpJson.ProcedureName, không cố định>"]
depends: [ReportSourceDefinitionConfigService (Mongo coll ReportSourceDefinitionConfig + SqlSugar dynamic SP + RestSharp API), Redis cache, IConfiguration ConnectionStrings, IUserPrincipalService (system auth khi gọi API nội bộ), MongoReportSettings]
cross_module_in: []
cross_module_out: ["<bất kỳ DB/API nào admin cấu hình trong nguồn — Execute chạy SP/API tùy cấu hình, ripple ra hệ thống bên ngoài>"]
---

# ReportSourceDefinitionController — Định nghĩa & thực thi nguồn báo cáo dashboard

> Controller config CRUD **+ executor động**. Định nghĩa nguồn (`ReportSourceDefinitionConfig`) lưu **MongoDB**, cache Redis (30 phút), soft-delete. Điểm đặc biệt: `Execute`/`TestExecute` **chạy SP hoặc API do admin cấu hình runtime** — `FetchMode=storedProcedure` → mở `SqlSugarClient` mới bằng connection string theo `ConnectionName` rồi `Ado.UseStoredProcedure().GetDataSetAll(ProcedureName, params)`; `FetchMode=api` → RestSharp gọi URL cấu hình (tự thêm SystemAuthorization + Authorization của user qua `IUserPrincipalService`). Vì SP/API là dữ liệu cấu hình nên **không có SP cố định** — `stored_procs` ở đây là động. KHÔNG có `[TienIchAttribute]`; mọi method `[SBAuthorize]` (public) nhưng ghi (Create/Update/Delete) + TestExecute + GetDbConnections **chặn cứng SuperUser** (403). `Execute` (runtime widget) cho mọi user. Tham số resolve từ `ParameterMappingDto` (filterId của `FilterDefinitionController` → giá trị FE gửi, fallback ConstantValue); cột lọc theo `outputColumns`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách nguồn báo cáo | Lưới nguồn | `GET api/ReportSourceDefinition/GetAll` | `[SBAuthorize]` | public | màn quản lý / build widget | Redis trước, fallback Mongo (`!IsDelete`); cache 30 phút. | **Mongo** `ReportSourceDefinitionConfig` | — |
| 2 | Chi tiết 1 nguồn | (mở nguồn) | `GET api/ReportSourceDefinition/GetById?id` | `[SBAuthorize]` | public | mở nguồn | Đọc theo id (`!IsDelete`); cache 30 phút; 404 nếu không thấy. | **Mongo** `ReportSourceDefinitionConfig` | — |
| 3 | Tạo nguồn | Thêm mới | `POST api/ReportSourceDefinition/Create` | `[SBAuthorize]` + `IsSuperUser` | public | nút thêm (chỉ SuperUser) | Không SuperUser → 403. Sinh Guid, upsert; xóa cache list. | **Mongo** upsert | — |
| 4 | Sửa nguồn | Lưu | `POST api/ReportSourceDefinition/Update` | `[SBAuthorize]` + `IsSuperUser` | public | nút sửa (chỉ SuperUser) | Không SuperUser → 403. Upsert theo Id; xóa cache list + by-id. | **Mongo** upsert | — |
| 5 | Xóa nguồn | Xóa | `POST api/ReportSourceDefinition/Delete/{id}` | `[SBAuthorize]` + `IsSuperUser` | public | nút xóa (chỉ SuperUser) | Không SuperUser → 403. Soft-delete; 404 nếu không thấy; xóa cache. | **Mongo** soft-delete | — |
| 6 | Thực thi nguồn (runtime) | (load widget) | `POST api/ReportSourceDefinition/Execute` | `[SBAuthorize]` | public | widget dashboard chạy | Load source theo SourceKey; resolve params (filterValues→param / ConstantValue); chạy SP (SqlSugar) hoặc API (RestSharp); map rows theo outputColumns; trả Columns/Rows/TotalCount/RawJson. | **SP/API động** (cấu hình trong nguồn) | Bắt mọi Exception trả `Success=false` + message → không throw 500. |
| 7 | Test thực thi (cấu hình) | Test | `POST api/ReportSourceDefinition/TestExecute` | `[SBAuthorize]` + `IsSuperUser` | public | màn cấu hình (chỉ SuperUser) | Không SuperUser → 403. Chạy thử SP/API từ ParsedSp/ParsedApi; detect cột + 5 dòng mẫu; trả ExtractedData/DetectedColumns/SampleRows. | **SP/API động** | — |
| 8 | Danh sách DB connection | (dropdown chọn DB) | `GET api/ReportSourceDefinition/GetDbConnections` | `[SBAuthorize]` + `IsSuperUser` | public | màn cấu hình SP (chỉ SuperUser) | Không SuperUser → 403. Liệt kê key trong `ConnectionStrings` của config. | (đọc IConfiguration) | — |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Đọc + Execute (runtime widget):** mọi user `[SBAuthorize]`.
- **Ghi + Test + GetDbConnections:** chỉ `IsSuperUser` (403 nếu không).
- **Resolve tham số (Execute/TestExecute):** với mỗi `ParameterMappingDto` theo `Order`: nếu có `FilterId` và `filterValues` chứa key đó → dùng giá trị FE; ngược lại dùng `ConstantValue`. `ToSqlValue` chuyển JsonElement/JValue/"NULL" → kiểu native/DBNull.
- **FetchMode:** `storedProcedure` → SqlSugar `GetDataSetAll(ProcedureName)` trên `ConnectionName` (DbType SqlServer); `api` → RestSharp, tự thêm `SystemAuthorization` + (nếu `UseSystemAuth`) `Authorization`/`SystemUserId` của user đăng nhập.

## Quan hệ
- **Gọi sang:** `ReportSourceDefinitionConfigService` (Mongo + SqlSugar dynamic SP + RestSharp). API nội bộ SCRM được gọi với token user qua `IUserPrincipalService.GetUserLogin()`.
- **Dữ liệu (entities):** Mongo `ReportSourceDefinitionConfig` (DB `SCRM_Report`). SP/API thực thi là **động** — không cố định entity/SP.
- **Ripple / đồng bộ:** Execute có thể gọi **bất kỳ SP DB nào** (theo ConnectionName) hoặc **bất kỳ API nội bộ/ngoài nào** đã cấu hình → cạnh ra phụ thuộc dữ liệu cấu hình. Dùng `FilterDefinitionConfig` (filterId) làm nguồn map tham số.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.Service/Generated/ActionDashboard/ReportSourceDefinitionConfigService.cs`
  - **Surface động — rủi ro bảo mật:** `ExecuteAsync` (L263) chạy SP/API tùy cấu hình nguồn; `Execute` cho mọi user `[SBAuthorize]` → nguồn cấu hình sai/độc có thể chạy SP ngoài ý muốn. Việc tạo nguồn chỉ SuperUser là rào chính.
  - `Execute`/`TestExecute` mở `SqlSugarClient` mới mỗi lần (L322/515) bằng connection string theo `ConnectionName`; `IsAutoCloseConnection=true`.
  - `CallApiAsync` (L435) timeout 60s, tự inject `SystemAuthorization: Bearer ServiceInfo.Token` + token user; bỏ qua header Authorization/SystemAuthorization từ cURL nếu `UseSystemAuth`.
  - `GetDbConnectionsAsync` (L415) trả TẤT CẢ key ConnectionStrings (chỉ tên, không lộ chuỗi kết nối) cho SuperUser.
- BE `Report/Report.API/Controllers/ActionDashboard/ReportSourceDefinitionController.cs`
  - Check `IsSuperUser` lặp thủ công mỗi method ghi (L44/55/66/92/103).
