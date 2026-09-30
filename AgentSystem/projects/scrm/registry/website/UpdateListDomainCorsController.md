---
controller: UpdateListDomainCorsController
module: website
service: Website
man_hinh_ui: "Quản trị hệ thống (SuperAdmin / QuanTriArea) — nút Update lại danh sách domain CORS sau khi thay đổi cấu hình domain cho phép"
muc_dich: "Reset/refresh cache danh sách domain CORS được phép (GlobalConfig.ListDomainCors) — gọi sau khi cập nhật cấu hình domain để áp dụng ngay không cần restart service."
alias: ["update domain cors", "refresh CORS website", "reset list domain cors", "cập nhật domain cho phép"]
auth: public               # [SBAuthorize] controller-level + [SBAuthorize("DomainCors_UpdateCors", ..., "HT-AD")] action
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: [Website (IConfiguration, GlobalConfig, CorsHelper)]
cross_module_in: []
cross_module_out: []
---

# UpdateListDomainCorsController — Refresh cache domain CORS

> Controller `Website.API/Areas/SanboxArea/UpdateListDomainCorsController.cs`, route `api/QuanTriArea/UpdateListDomainCors`, base `BaseCoreController`, `[ApiController]`, `partial`. `[SBAuthorize]` controller-level + `[TienIch(..., TI_HeThong, true)]` (superadmin). 1 action. Không truy vấn DB entity riêng — thao tác trên cache tĩnh `GlobalConfig.ListDomainCors` + đọc connection string.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Update lại list domain CORS | nút "Update CORS" (SA) | POST `api/QuanTriArea/UpdateListDomainCors/UpdateCors` | `DomainCors_UpdateCors` | public | sau khi đổi cấu hình domain | Set `GlobalConfig.ListDomainCors = null` (xóa cache); gọi `CorsHelper.IsOriginAllowed("http://localhost:4200", connStr)` để nạp lại từ DB; trả `true` | static cache + CorsHelper (đọc DB qua connStr) | Origin nạp lại hardcode `http://localhost:4200` (chỉ để trigger reload — kết quả check không dùng) |

## Quan hệ
- **Gọi sang:** `CorsHelper.IsOriginAllowed` (đọc list domain từ DB qua `CoreConnectStr`), `GlobalConfig.ListDomainCors` (cache tĩnh).
- **Dữ liệu (entities):** không có entity riêng — cấu hình domain CORS lưu ở core DB.
- **Ripple / đồng bộ:** reset cache → mọi request sau dùng list domain mới (ảnh hưởng toàn bộ storefront cross-origin).

## Lỗi đã biết / lưu ý kỹ thuật
- `UpdateCors` (L21-29) là async nhưng không await gì (CorsHelper.IsOriginAllowed sync) — cảnh báo CS1998 vô hại.
- Hardcode origin `http://localhost:4200` chỉ để **kích hoạt reload cache**; biến `rs` không dùng → đừng hiểu nhầm là logic check thật.
- Tác động hệ thống: xóa cache CORS toàn cục → chỉ SuperAdmin (HT-AD) được gọi.
