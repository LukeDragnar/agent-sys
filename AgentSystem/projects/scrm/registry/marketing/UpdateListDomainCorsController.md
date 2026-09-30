---
controller: UpdateListDomainCorsController
module: marketing
service: Promotion
man_hinh_ui: "Quản trị hệ thống > (nội bộ) — refresh danh sách domain CORS cho service Promotion"
muc_dich: "Làm mới (reset cache) danh sách domain CORS được phép của service Promotion sau khi admin cập nhật cấu hình CORS ở DB."
alias: ["domain cors", "UpdateListDomainCors", "refresh cors", "cập nhật cors"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends:
  - "Core/TienIch (TienIch TI_HeThong)"
  - "GlobalConfig.ListDomainCors (static cache) + CoreConnectStr (CorsHelper đọc DB)"
cross_module_in:
  - "Quản trị hệ thống (gọi sang TẤT CẢ service để refresh CORS đồng loạt — đây là bản của Promotion service)"
cross_module_out: []
---
> ⚠️ Controller hạ tầng (Area SanboxArea/QuanTriArea), không thuộc nghiệp vụ marketing — chỉ nằm trong service Promotion. Mỗi microservice có 1 bản tương tự.

# UpdateListDomainCors — Refresh danh sách domain CORS (Promotion)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Cập nhật list domain CORS | Update CORS | POST `api/QuanTriArea/UpdateListDomainCors/UpdateCors` | `DomainCors_UpdateCors` (HT-AD) | public | Set `GlobalConfig.ListDomainCors = null` (xóa cache) rồi gọi `CorsHelper.IsOriginAllowed` (đọc lại từ CoreConnectStr) → lần request sau nạp lại list domain CORS từ DB. Luôn trả `true`. | DB qua CorsHelper | Trả `true` cứng dù logic check không dùng kết quả |

## Quan hệ
- **Gọi sang:** `CorsHelper.IsOriginAllowed("http://localhost:4200", coreConnectStr)` — đọc DB Core để rebuild cache CORS.
- **Dữ liệu (entities):** không (chỉ static `GlobalConfig.ListDomainCors`).
- **Ripple/đồng bộ:** mỗi service có endpoint UpdateCors riêng; Quản trị gọi đồng loạt khi đổi danh sách domain.

## Lỗi đã biết / lưu ý kỹ thuật
- `UpdateListDomainCorsController.cs:27` — hardcode origin `"http://localhost:4200"` chỉ để trigger rebuild cache; kết quả `IsOriginAllowed` bị bỏ (không dùng `rs`) → mục đích thực là side-effect reset cache, không phải validate origin đó.
- `:23` — action `async` nhưng không có `await` nào → warning compile (chạy đồng bộ).
- Class `partial` + Area `QuanTriArea` route khác convention `api/[controller]` của các controller nghiệp vụ.
