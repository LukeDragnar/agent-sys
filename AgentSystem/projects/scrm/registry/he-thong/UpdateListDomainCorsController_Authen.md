---
controller: UpdateListDomainCorsController
module: he-thong
service: Authen
man_hinh_ui: "Khu quản trị hệ thống (QuanTriArea): nút 'Cập nhật CORS' để reload danh sách domain được phép gọi API sau khi đổi cấu hình."
muc_dich: "Reset cache danh sách domain CORS (GlobalConfig.ListDomainCors) của service Authen, buộc nạp lại từ DB Core khi admin thêm/sửa domain."
alias: ["update cors", "cập nhật domain cors", "reload cors", "làm mới danh sách domain"]
auth: internal
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: ["IConfiguration (CoreConnectStr)", "CorsHelper.IsOriginAllowed (đọc DB Core nạp lại list domain)", "GlobalConfig.ListDomainCors (static cache)"]
cross_module_in: ["Admin hệ thống (QuanTriArea) → UpdateCors sau khi đổi cấu hình domain CORS"]
cross_module_out: ["GlobalConfig.ListDomainCors (reset cache static toàn service Authen)"]
---

# UpdateListDomainCorsController (Authen) — Reload cache domain CORS

Controller khu quản trị (`api/QuanTriArea/...`, `[TienIch(..., isHidden=true)]`). Khi admin thêm/sửa domain được phép trong DB Core, gọi endpoint này để **xóa cache static** `GlobalConfig.ListDomainCors` và buộc `CorsHelper` nạp lại danh sách. File `_Authen` để tránh trùng node cùng tên ở project khác. Controller-level `[SBAuthorize]` + endpoint gắn permission key riêng (role HT-AD).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Cập nhật danh sách CORS | "Cập nhật CORS" | POST `api/QuanTriArea/UpdateListDomainCors/UpdateCors` | `DomainCors_UpdateCors` | login `[SBAuthorize]` | Admin hệ thống (HT-AD) | Set `GlobalConfig.ListDomainCors = null` (xóa cache) → gọi `CorsHelper.IsOriginAllowed("http://localhost:4200", CoreConnectStr)` để trigger nạp lại list từ DB Core → trả `true`. | CorsHelper đọc DB Core | Dùng origin warm-up hardcode `http://localhost:4200` chỉ để kích hoạt reload — không phản ánh kết quả check thật; luôn trả `true` bất kể nạp được hay không |

## Quan hệ
- **Gọi sang:** `CorsHelper.IsOriginAllowed` (đọc DB Core qua `CoreConnectStr`).
- **Dữ liệu (entities):** không có entity riêng; danh sách domain nằm trong DB Core, cache static `GlobalConfig.ListDomainCors`.
- **Ripple / đồng bộ:** reset cache CORS toàn process Authen → request CORS sau đó được đánh giá lại theo list mới.

## Lỗi đã biết / lưu ý kỹ thuật
- `UpdateListDomainCorsController.cs:25-28` — origin warm-up hardcode `"http://localhost:4200"`; chỉ để ép `CorsHelper` nạp lại cache, kết quả `IsOriginAllowed` bị bỏ qua (`rs` không dùng), luôn `return true`.
- `UpdateListDomainCorsController.cs:23` — method `async` nhưng không `await` gì (CorsHelper gọi đồng bộ) → cảnh báo compiler vô hại.
- Reset cache chỉ tác động process hiện tại — nếu Authen chạy nhiều instance, phải gọi trên từng instance (không broadcast).
