---
controller: DomainCorsController
module: he-thong
service: Core
man_hinh_ui: "Hệ thống > Quản trị > Quản lý domain CORS (whitelist domain được phép gọi API)"
muc_dich: "Quản lý danh sách domain CORS (whitelist origin được phép truy cập hệ thống): CRUD + tìm theo điều kiện. Mỗi lần thêm/sửa/xóa tự gọi UpdateLaiDomainGlobal để cập nhật lại cấu hình CORS toàn cục."
alias: ["domain cors", "CORS", "whitelist domain", "cấu hình domain"]
auth: internal
state_machine: []
deprecated: false
entities: [DomainCors]
stored_procs: []
depends: ["IDomainCorsService"]
cross_module_in: ["Frontend QuanTriArea (quản lý domain cors)"]
cross_module_out: ["Cấu hình CORS toàn cục (UpdateLaiDomainGlobal) — ảnh hưởng mọi service kiểm tra origin"]
---

# DomainCorsController — Quản lý domain CORS (whitelist origin)

Controller CRUD danh sách domain được phép gọi API (CORS whitelist). Nằm trong namespace `Core.API.Controllers` với `[Route("api/QuanTriArea/[controller]")]` → **path thật `api/QuanTriArea/DomainCors/...`**. Class-level `[SBAuthorize]`, mỗi action có quyền riêng (role gợi ý `HT-AD`). Đặc điểm quan trọng: sau mỗi lần `ThemMoi`/`CapNhatThongTin`/`Xoa` controller **luôn gọi `_service.UpdateLaiDomainGlobal(CurrentUser)`** để refresh cấu hình CORS toàn cục → đây là ripple chính.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Xem chi tiết | (chi tiết) | GET `api/QuanTriArea/DomainCors/TimTheoId/{id:Guid}` | `DomainCors_TimTheoId` | internal | — | Trả 1 domain theo id (map `DomainCors`→`DomainCorsDto`) | EF (service) | — |
| 2 | Lấy tất cả | (danh sách) | GET `api/QuanTriArea/DomainCors/LayTatCa` | `DomainCors_LayTatCa` | internal | — | Trả toàn bộ domain | EF (service) | — |
| 3 | Thêm mới | Thêm | POST `api/QuanTriArea/DomainCors/ThemMoi` (body `DomainCorsDto`) | `DomainCors_ThemMoi` | internal | — | Validate model → insert → **`UpdateLaiDomainGlobal`** (refresh CORS toàn cục) | EF (service) | — |
| 4 | Cập nhật | Lưu | POST `api/QuanTriArea/DomainCors/CapNhatThongTin` (body `DomainCorsDto`) | `DomainCors_CapNhatThongTin` | internal | — | Validate + check tồn tại → set NguoiCapNhat → update → **`UpdateLaiDomainGlobal`** | EF (service) | — |
| 5 | Xóa | Xóa | POST `api/QuanTriArea/DomainCors/Xoa/{id:Guid}` | `DomainCors_Xoa` | internal | — | Check tồn tại → delete → **`UpdateLaiDomainGlobal`** | EF (service) | — |
| 6 | Tìm theo điều kiện | (tìm/lọc) | POST `api/QuanTriArea/DomainCors/TimTheoDieuKien` (body `DomainCorsFilterDto`) | `DomainCors_TimTheoDieuKien` | internal | — | Trả danh sách domain theo filter (phân trang) | EF (service) | — |

## Luồng xuyên module
- Thêm/sửa/xóa domain → `_service.UpdateLaiDomainGlobal(CurrentUser)` ghi lại cấu hình CORS toàn cục → mọi service/middleware kiểm tra origin sẽ áp danh sách mới (không cần restart).

## Quan hệ
- **Gọi sang:** `IDomainCorsService` (CRUD + `UpdateLaiDomainGlobal` + `TimTheoDieuKienAsync`).
- **Dữ liệu (entities):** `DomainCors` ↔ DTO `DomainCorsDto` (AutoMapper).
- **Ripple / đồng bộ:** cấu hình CORS global được cập nhật lại mỗi lần thay đổi danh sách.

## Lỗi đã biết / lưu ý kỹ thuật
- `DomainCorsController.cs:13` — file ở `Core.API.Controllers` nhưng route `api/QuanTriArea/[controller]` → path thật `api/QuanTriArea/DomainCors/...`.
- `DomainCorsController.cs:70,103,127` — `UpdateLaiDomainGlobal` gọi sau insert/update/delete nhưng **không có rollback nếu nó ném lỗi** → có thể lệch trạng thái giữa DB domain và cache CORS global.
- `DomainCorsController.cs:15` — class khai báo `partial` (`public partial class DomainCorsController`) → có thể có phần định nghĩa khác ở file riêng (cần kiểm tra nếu mở rộng).
