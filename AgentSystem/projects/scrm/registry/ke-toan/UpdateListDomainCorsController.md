---
controller: UpdateListDomainCorsController
module: ke-toan
service: Invoice
man_hinh_ui: "Khu Quản trị (QuanTriArea) > nút làm mới danh sách domain CORS — quản trị hệ thống bấm sau khi đổi cấu hình CORS"
muc_dich: "Reset cache danh sách domain CORS (GlobalConfig.ListDomainCors = null) để service nạp lại từ DB — dùng khi thêm/sửa domain được phép gọi API"
alias: ["update cors", "làm mới domain cors", "reset cors"]
auth: internal   # QuanTriArea, [TienIch(..., TI_HeThong, true)], super-user
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends:
  - IConfiguration          # connection string CoreConnectStr
  - GlobalConfig            # cache ListDomainCors
  - CorsHelper              # IsOriginAllowed (nạp lại từ DB)
cross_module_in: []
cross_module_out: []
---

# UpdateListDomainCorsController — Làm mới cache domain CORS

> `partial class`, khu `api/QuanTriArea/[controller]`, `[TienIch("UpdateListDomainCorsController", ..., TI_HeThong, true)]` — quản trị hệ thống (super-user). Hạ tầng, không nghiệp vụ HĐĐT.
> Chỉ 1 endpoint: clear cache `GlobalConfig.ListDomainCors` rồi gọi `CorsHelper.IsOriginAllowed` để buộc nạp lại danh sách domain từ DB.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Update lại list domain CORS | Nút "Update CORS" | `POST UpdateCors` | DomainCors_UpdateCors (HT-AD) | internal | quản trị hệ thống | Set `GlobalConfig.ListDomainCors = null`; lấy conn `CoreConnectStr`; gọi `CorsHelper.IsOriginAllowed("http://localhost:4200", ctr)` (trigger nạp lại); trả `true` | DB (CoreConnectStr) qua CorsHelper | origin test hard-code `localhost:4200` — chỉ để kích nạp lại cache, không phải config thật |

## Quan hệ
- **Gọi sang:** `CorsHelper.IsOriginAllowed` (đọc DB qua `CoreConnectStr`), `GlobalConfig.ListDomainCors` (cache tĩnh).
- **Dữ liệu (entities):** không có entity riêng — đọc bảng cấu hình domain CORS qua CorsHelper.
- **Ripple / đồng bộ:** reset cache CORS toàn service Invoice; chỉ ảnh hưởng request CORS tiếp theo.

## Lỗi đã biết / lưu ý kỹ thuật (path:line)
- `UpdateListDomainCorsController.cs:28` — origin `http://localhost:4200` hard-code, chỉ dùng để gọi `IsOriginAllowed` ép nạp lại cache (kết quả bool bị bỏ qua) → endpoint thực chất là "clear cache + warm-up".
- Luôn trả `true` bất kể nạp lại thành công hay không.
