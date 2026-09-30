---
controller: UpdateListDomainCorsController
module: sales
service: Contact
man_hinh_ui: "nội bộ — quản trị (QuanTriArea), route api/QuanTriArea/UpdateListDomainCors (quản lý domain CORS, SanboxArea)"
muc_dich: "Reset/làm mới danh sách domain CORS đang cache trong bộ nhớ ứng dụng (GlobalConfig.ListDomainCors) — buộc đọc lại từ DB sau khi cập nhật whitelist CORS."
alias: [UpdateListDomainCors, "domain cors", CORS, "whitelist domain"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: ["IConfiguration (CoreConnectStr)", "GlobalConfig.ListDomainCors", "CorsHelper.IsOriginAllowed"]
cross_module_in: []
cross_module_out: ["GlobalConfig.ListDomainCors (cache CORS toàn app) — reset null buộc reload"]
---

# UpdateListDomainCors — Làm mới danh sách domain CORS (QuanTriArea)

> Controller admin trong `Areas/SanboxArea`, route `api/QuanTriArea/[controller]`, `partial`. `TienIch(..., MaTienIchSCRM.TI_HeThong, true)` → tiện ích Hệ thống, cờ admin.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Update list domain CORS | Cập nhật CORS | POST `api/QuanTriArea/UpdateListDomainCors/UpdateCors` | `DomainCors_UpdateCors` (HT-AD) | public | nút quản trị | Set `GlobalConfig.ListDomainCors = null` (xóa cache) rồi gọi `CorsHelper.IsOriginAllowed(...)` với CoreConnectStr để buộc reload whitelist từ DB; trả true. | CorsHelper + DB | Side-effect toàn cục; xem lưu ý |

## Quan hệ
- **Gọi sang:** `IConfiguration` (lấy connection `CoreConnectStr`), `CorsHelper.IsOriginAllowed`.
- **Dữ liệu (entities):** không có entity riêng — thao tác trên cache CORS toàn cục `GlobalConfig.ListDomainCors` + đọc DB Core.
- **Ripple / đồng bộ:** reset `GlobalConfig.ListDomainCors = null` ảnh hưởng **toàn bộ instance app** (mọi request CORS sau đó reload lại whitelist). Đây là cấu hình bảo mật → thay đổi CORS sai có thể chặn FE hợp lệ hoặc mở quá rộng.

## Lỗi đã biết / lưu ý kỹ thuật
- `UpdateListDomainCorsController.cs:26` — method khai báo `async Task<IActionResult>` nhưng **không có `await`** (chỉ thao tác đồng bộ) → warning compiler, không thực sự bất đồng bộ.
- `UpdateListDomainCorsController.cs:30` — gọi `CorsHelper.IsOriginAllowed("http://localhost:4200", ctr)` với origin **hardcode `localhost:4200`** chỉ để trigger reload cache; kết quả bị bỏ qua — ý đồ là warm cache, nhưng việc dùng origin cố định dễ gây hiểu nhầm.
- Endpoint reset cache CORS toàn cục: chỉ HT-AD gọi được (đúng), nhưng là thao tác nhạy cảm về bảo mật biên (CORS) → cần kiểm soát chặt.
