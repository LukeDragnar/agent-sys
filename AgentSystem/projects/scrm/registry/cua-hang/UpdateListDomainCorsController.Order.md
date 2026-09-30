---
controller: UpdateListDomainCorsController
module: cua-hang
service: Order
man_hinh_ui: "nội bộ — không có UI (ops/infra; reset cache danh sách domain CORS của service Order)"
muc_dich: "Reset cache GlobalConfig.ListDomainCors rồi re-check CORS — thao tác vận hành khi cập nhật danh sách domain được phép CORS."
alias: ["update cors order", "reset domain cors", "làm mới list domain cors"]
auth: internal
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: [Order, CoreConnectStr (CorsHelper)]
cross_module_in: []
cross_module_out: [GlobalConfig.ListDomainCors (cache CORS toàn cục bị reset)]
---

# UpdateListDomainCorsController (Order) — Reset cache domain CORS (ops/infra)

> Controller `Order.API/Areas/SandBoxArea/UpdateListDomainCorsController.cs`, route **`api/QuanTriArea/UpdateListDomainCors`**. `[SBAuthorize]` controller-level + `[TienIch("UpdateListDomainCorsController", ..., MaTienIchSCRM.TI_HeThong, true)]` (superadmin). Thuần vận hành, không UI. (Có node trùng tên cho service PreOrder ở `UpdateListDomainCorsController.md` — đây là bản service Order, đặt tên `-Order` để tránh đè.)

## Hành động (UI / công khai)
| # | Hành động | Endpoint (HTTP path) | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|
| 1 | Update lại list domain CORS | POST `api/QuanTriArea/UpdateListDomainCors/UpdateCors` | `DomainCors_UpdateCors` (role `HT-AD`) | internal | Set `GlobalConfig.ListDomainCors = null` (xóa cache); lấy `CoreConnectStr`; gọi `CorsHelper.IsOriginAllowed("http://localhost:4200", ctr)` để re-load/re-check; trả `true` | config/CORS (không entity) | Origin re-check hardcode `http://localhost:4200` — chỉ để trigger reload danh sách, không phản ánh origin thật của caller |

## Quan hệ
- **Gọi sang:** `IConfiguration.GetConnectionString("CoreConnectStr")`, `CorsHelper.IsOriginAllowed`.
- **Dữ liệu (entities):** không — chỉ thao tác cache cấu hình `GlobalConfig.ListDomainCors`.
- **Ripple / đồng bộ:** reset cache CORS toàn cục của service Order → request sau load lại danh sách domain CORS từ DB (không cần restart service).

## Lỗi đã biết / lưu ý kỹ thuật
- Endpoint vận hành thuần (no-UI); permission `DomainCors_UpdateCors` gắn role `HT-AD`; cờ TienIch superadmin.
- `CorsHelper.IsOriginAllowed("http://localhost:4200", ctr)` dùng origin cố định chỉ nhằm trigger reload cache, không kiểm tra origin thực.
