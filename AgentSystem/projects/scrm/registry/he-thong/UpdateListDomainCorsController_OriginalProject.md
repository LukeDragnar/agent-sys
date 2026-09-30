---
controller: UpdateListDomainCorsController
module: he-thong
service: OriginalProject
man_hinh_ui: "nội bộ — Quản trị > cấu hình CORS (làm mới danh sách domain được phép)"
muc_dich: "Làm mới (reset cache) danh sách domain CORS cho phép: xóa GlobalConfig.ListDomainCors và nạp lại từ DB qua CorsHelper."
alias: ["Update CORS", "domain cors", "reset cors", "list domain cors"]
auth: internal
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: ["IConfiguration (CoreConnectStr)", "GlobalConfig.ListDomainCors", "CorsHelper.IsOriginAllowed"]
cross_module_in: ["Admin gọi sau khi sửa danh sách domain CORS để áp dụng ngay"]
cross_module_out: ["Middleware CORS đọc GlobalConfig.ListDomainCors cho mọi request"]
---

# UpdateListDomainCorsController (OriginalProject) — Làm mới danh sách CORS

Reset cache CORS: set `GlobalConfig.ListDomainCors = null` rồi gọi `CorsHelper.IsOriginAllowed` (với connection string `CoreConnectStr`) để nạp lại từ DB. `[SBAuthorize]` cấp class + key action → `internal`. Tiện ích `TI_HeThong`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Cập nhật danh sách CORS | nút Update CORS | POST `api/QuanTriArea/UpdateListDomainCors/UpdateCors` | `DomainCors_UpdateCors` | internal | — | Xóa cache `GlobalConfig.ListDomainCors`=null, gọi `CorsHelper.IsOriginAllowed("http://localhost:4200", ctr)` để reload từ DB; luôn trả `true`. | EF/DB qua CorsHelper | — |

## Quan hệ
- **Gọi sang:** `CorsHelper.IsOriginAllowed` (đọc DB qua `CoreConnectStr`), `GlobalConfig.ListDomainCors`.
- **Dữ liệu (entities):** danh sách domain CORS (lưu DB, cache static).
- **Ripple / đồng bộ:** Sau khi reset, middleware CORS dùng danh sách mới cho mọi request → ảnh hưởng toàn bộ FE đang gọi API.

## Lỗi đã biết / lưu ý kỹ thuật
- `UpdateListDomainCorsController.cs:28` — hard-code origin kiểm tra `"http://localhost:4200"` chỉ để trigger reload; mục đích thực là reset cache, không phải check origin đó. Tên/ý đồ dễ gây hiểu nhầm.
- `UpdateListDomainCorsController.cs:26` — set cache static `GlobalConfig.ListDomainCors=null` (global mutable) → có thể race với request đồng thời trong lúc reload.
