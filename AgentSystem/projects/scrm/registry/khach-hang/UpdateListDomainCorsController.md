---
controller: UpdateListDomainCorsController
module: khach-hang
service: Customer
man_hinh_ui: "nội bộ — không có UI (tiện ích quản trị reset cache danh sách domain CORS); route api/QuanTriArea/UpdateListDomainCorsController"
muc_dich: "Reset cache danh sách domain CORS được phép (GlobalConfig.ListDomainCors) để service Customer nạp lại danh sách origin từ DB"
alias: ["UpdateListDomainCors", "UpdateCors", "Cập nhật domain CORS", "Domain CORS admin"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: [IConfiguration, GlobalConfig, CorsHelper]
cross_module_in: []
cross_module_out: []
---

# UpdateListDomainCorsController — Cập nhật danh sách domain CORS (tiện ích quản trị)

> Khai báo trong file `Areas/SandBoxArea/UpdateListDomainCorsController.cs` nhưng namespace thực tế là `Customer.API.Controllers` (KHÔNG nằm trong namespace SandBoxArea — cần lưu ý khi tra cứu).
> Route prefix **`api/QuanTriArea/[controller]`**. Class name không trùng controller non-SA nào.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Reset cache danh sách domain CORS | (tiện ích quản trị — gọi tay) | `POST api/QuanTriArea/UpdateListDomainCorsController/UpdateCors` | DomainCors_UpdateCors (role **HT-AD**) | public | — | Set `GlobalConfig.ListDomainCors = null` (xóa cache); lấy connection string `CoreConnectStr`; gọi `CorsHelper.IsOriginAllowed("http://localhost:4200", ctr)` để nạp lại; luôn trả `true` | (đọc DB qua CorsHelper, không EF entity) | Trả về `true` cứng, không phản ánh kết quả thực; origin test hardcode `http://localhost:4200` |

## Quan hệ
- **Gọi sang:** `IConfiguration` (lấy `CoreConnectStr`), `GlobalConfig.ListDomainCors` (cache static), `CorsHelper.IsOriginAllowed` (nạp lại danh sách từ DB).
- **Dữ liệu (entities):** không map entity trực tiếp; danh sách domain CORS lưu/đọc qua CorsHelper.
- **Ripple / đồng bộ:** sau khi reset, middleware CORS của service Customer nạp lại danh sách origin được phép → ảnh hưởng request cross-origin toàn service.

## Lỗi đã biết / lưu ý kỹ thuật
- `UpdateListDomainCorsController.cs:6` — namespace `Customer.API.Controllers` (không phải `...Areas.SandBoxArea` dù file nằm trong thư mục SandBoxArea).
- `UpdateListDomainCorsController.cs:24-30` (`UpdateCors`) — luôn `return ... true` bất kể `CorsHelper.IsOriginAllowed` cho kết quả gì; origin kiểm thử hardcode `http://localhost:4200` (chỉ để trigger reload cache).
- Quyền `DomainCors_UpdateCors` siết role **HT-AD** (admin hệ thống) — đây là tiện ích nhạy cảm, không expose UI thường.
