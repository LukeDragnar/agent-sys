---
controller: FilterDefinitionController
module: bao-cao
service: Report
man_hinh_ui: "Báo cáo > Quản lý bộ lọc dashboard (màn admin cấu hình): CRUD danh mục định nghĩa bộ lọc (filter) dùng chung cho dashboard cấu hình — mỗi filter có id, label, kiểu, thứ tự (Order). Chỉ SuperUser được tạo/sửa/xóa."
muc_dich: "CRUD danh mục FilterDefinitionConfig — định nghĩa các bộ lọc tái sử dụng cho nguồn báo cáo (ReportSourceDefinition map filterId → tham số SP/API). Lưu MongoDB, cache Redis, soft-delete."
alias: ["định nghĩa bộ lọc", "filter definition", "quản lý bộ lọc dashboard", "filter config báo cáo", "danh mục bộ lọc"]
auth: public
state_machine: []
deprecated: false
entities: [FilterDefinitionConfig]
stored_procs: []
depends: [FilterDefinitionConfigService (Mongo coll FilterDefinitionConfig), Redis cache, MongoReportSettings (Mongo SCRM_Report)]
cross_module_in: [ReportSourceDefinitionController (Execute dùng filterId của filter này để map tham số SP/API)]
cross_module_out: []
---

# FilterDefinitionController — Định nghĩa bộ lọc dashboard

> Controller config CRUD (KHÔNG đọc SP). Lưu danh mục bộ lọc vào **MongoDB** (`FilterDefinitionConfig`), cache Redis (TTL 30 phút), soft-delete (`IsDelete`). KHÔNG có `[TienIchAttribute]` controller-level; mọi method `[SBAuthorize]` (public) nhưng **ghi (Create/Update/Delete) chặn cứng chỉ SuperUser** (return 403 nếu không phải). Danh mục này là "từ điển filter" dùng chung: `ReportSourceDefinitionController.Execute` nhận `filterValues` (filterId → giá trị) và map sang tham số SP/API — filterId chính là Id của các bản ghi ở đây. GetAll sắp xếp theo `Order`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách bộ lọc | Lưới bộ lọc | `GET api/FilterDefinition/GetAll` | `[SBAuthorize]` | public | màn quản lý / khi build form lọc | Redis trước, fallback Mongo (`!IsDelete`), sort theo `Order`; cache 30 phút. | **Mongo** `FilterDefinitionConfig` (+ Redis) | — |
| 2 | Chi tiết 1 bộ lọc | (mở bộ lọc) | `GET api/FilterDefinition/GetById?id` | `[SBAuthorize]` | public | mở bộ lọc | Đọc theo id (`!IsDelete`), cache 30 phút; 404 nếu không thấy. | **Mongo** `FilterDefinitionConfig` | — |
| 3 | Tạo bộ lọc | Thêm mới | `POST api/FilterDefinition/Create` | `[SBAuthorize]` + check `IsSuperUser` | public | nút thêm (chỉ SuperUser) | Không phải SuperUser → 403. Sinh Guid Id, set NgayCapNhat, upsert; xóa cache list. Trả true. | **Mongo** `FilterDefinitionConfig` upsert | — |
| 4 | Sửa bộ lọc | Lưu | `POST api/FilterDefinition/Update` | `[SBAuthorize]` + check `IsSuperUser` | public | nút sửa (chỉ SuperUser) | Không SuperUser → 403. Upsert theo Id, set NgayCapNhat; xóa cache list + by-id. | **Mongo** `FilterDefinitionConfig` upsert | — |
| 5 | Xóa bộ lọc | Xóa | `POST api/FilterDefinition/Delete/{id}` | `[SBAuthorize]` + check `IsSuperUser` | public | nút xóa (chỉ SuperUser) | Không SuperUser → 403. Soft-delete (IsDelete=true); 404 nếu không thấy; xóa cache. | **Mongo** `FilterDefinitionConfig` soft-delete | — |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Đọc (GetAll/GetById):** mọi user đăng nhập (`[SBAuthorize]`).
- **Ghi (Create/Update/Delete):** chỉ `CurrentUser.IsSuperUser` — ngược lại trả `MethodResult.ResultWithError(..., status: 403)`.

## Quan hệ
- **Gọi sang:** `FilterDefinitionConfigService` (Mongo). Không gọi service ngoài Report.
- **Dữ liệu (entities):** Mongo `FilterDefinitionConfig` (DB `SCRM_Report`). KHÔNG đi stored proc/EF.
- **Ripple / đồng bộ:** Là danh mục nguồn cho `ReportSourceDefinitionController` — `Execute` map `filterValues[filterId]` của filter này vào tham số SP/API qua `ParameterMappingDto.FilterId`.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.API/Controllers/ActionDashboard/FilterDefinitionController.cs`
  - Check quyền ghi lặp lại thủ công ở mỗi method (L44/55/66) — không gom vào attribute; nhất quán nhưng dễ sót nếu thêm method mới.
- BE `Report/Report.Service/Generated/ActionDashboard/FilterDefinitionConfigService.cs`
  - `Create`/`Update`/`Delete` đều dùng `ReplaceOneAsync(IsUpsert=true)` (L72/84/102) — Update của bản chưa tồn tại sẽ tạo mới (upsert) thay vì lỗi.
  - Soft-delete: dữ liệu vẫn còn trong Mongo, chỉ set `IsDelete=true`.
