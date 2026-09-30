---
controller: UpdateListDomainCorsController
module: he-thong
service: File
man_hinh_ui: "nội bộ — không có UI (tiện ích quản trị, gọi để refresh cache CORS)"
muc_dich: "Reset cache danh sách domain CORS (GlobalConfig.ListDomainCors = null) để hệ thống nạp lại từ DB lần kế tiếp."
alias: ["domain cors", "cập nhật cors", "refresh cors", "DomainCors_UpdateCors"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: ["IConfiguration (CoreConnectStr)", "GlobalConfig.ListDomainCors", "CorsHelper"]
cross_module_in: ["Admin/QuanTri gọi UpdateCors sau khi sửa danh sách domain CORS để áp dụng ngay"]
cross_module_out: ["Toàn hệ thống File.API (CORS policy đọc lại GlobalConfig.ListDomainCors ở request kế)"]
---

# UpdateListDomainCorsController — Cập nhật danh sách domain CORS (File service)

Tiện ích quản trị nằm trong area `QuanTriArea` của File.API. Khi danh sách domain CORS trong DB bị sửa, gọi `UpdateCors` để xóa cache tĩnh `GlobalConfig.ListDomainCors` → CORS policy nạp lại từ DB ở request tiếp theo. Controller có `[TienIch(..., MaTienIchSCRM.TI_HeThong)]` (đăng ký quyền tiện ích hệ thống).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Refresh CORS | (tiện ích) | POST `/api/QuanTriArea/UpdateListDomainCorsController/UpdateCors` | `DomainCors_UpdateCors` | internal (`[SBAuthorize]`) | — | `GlobalConfig.ListDomainCors = null` → CORS policy nạp lại từ `CoreConnectStr` lần sau. Gọi `CorsHelper.IsOriginAllowed("http://localhost:4200", ctr)` để warm/verify. Trả `true`. | — (đọc connection string, không entity) | Có gọi `IsOriginAllowed` với origin hardcode `localhost:4200` → giá trị `rs` không dùng tới (chỉ side-effect warm cache) |

## Quan hệ
- **Gọi sang:** `CorsHelper.IsOriginAllowed`, đọc `CoreConnectStr` qua `IConfiguration`.
- **Dữ liệu (entities):** không — chỉ thao tác biến tĩnh `GlobalConfig.ListDomainCors`.
- **Ripple / đồng bộ:** reset cache CORS → mọi request kế tiếp tới File.API nạp lại danh sách domain hợp lệ.

## Lỗi đã biết / lưu ý kỹ thuật
- `UpdateListDomainCorsController.cs:27` — origin kiểm tra hardcode `"http://localhost:4200"`; biến `rs` không được sử dụng (chỉ làm warm cache).
- Controller-level `[SBAuthorize]` + action key `DomainCors_UpdateCors` → cần quyền tiện ích hệ thống mới gọi được.
- Bản trùng tên với `UpdateListDomainCorsController` ở service khác (OriginalProject) → file này suffix `_File`.
