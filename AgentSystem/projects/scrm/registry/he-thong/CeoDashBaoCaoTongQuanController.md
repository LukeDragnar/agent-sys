---
controller: CeoDashBaoCaoTongQuanController
module: he-thong
service: Core
man_hinh_ui: "Dashboard CEO > Báo cáo tổng quan — cấu hình hiển thị báo cáo tổng quan theo user (api/CeoDashBaoCaoTongQuan)"
muc_dich: "Quản lý cấu hình báo cáo tổng quan trên dashboard CEO theo từng user: đọc cấu hình, cập nhật, reset về mặc định."
alias: ["CeoDashBaoCaoTongQuan", "báo cáo tổng quan CEO", "config báo cáo tổng quan", "dashboard CEO báo cáo"]
auth: internal
state_machine: []
deprecated: false
entities: [CeoBaoCaoTongQuanConfig]
stored_procs: []
depends: ["ICeoDashBaoCaoTongQuan", "CurrentUser"]
cross_module_in: ["Màn dashboard CEO gọi GetConfigCeoBaoCaoTongQuanByUser khi render báo cáo tổng quan"]
cross_module_out: []
---

# CeoDashBaoCaoTongQuanController — Cấu hình báo cáo tổng quan dashboard CEO

Biến thể **non-Area** namespace `Core.API.Controllers`, route `[Route("api/[controller]")]` → path gốc `/api/CeoDashBaoCaoTongQuan/...`. Controller-level `[SBAuthorize]` (không TienIch). Kế thừa `BaseController`. Toàn bộ action gắn với `CurrentUser` (cấu hình per-user). Không có permission key chi tiết → chỉ cần authenticated.

File: `Core/Core.API/Controllers/CeoDashBaoCaoTongQuanController.cs`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình báo cáo của user | (render báo cáo tổng quan) | GET `/api/CeoDashBaoCaoTongQuan/GetConfigCeoBaoCaoTongQuanByUser` | — (`[SBAuthorize]` controller-level) | internal | — | `GetConfigCeoBaoCaoTongQuanByUser(CurrentUser)` → cấu hình báo cáo tổng quan của user hiện tại | EF (service) | — |
| 2 | Cập nhật cấu hình báo cáo | Lưu cấu hình | POST `/api/CeoDashBaoCaoTongQuan/CeoBaoCaoTongQuanConfigUpdate` | — | internal | — | `CeoBaoCaoTongQuanConfigUpdate(request, CurrentUser)` → lưu cấu hình báo cáo cho user | EF (service) | — |
| 3 | Reset cấu hình về mặc định | Đặt lại | GET `/api/CeoDashBaoCaoTongQuan/CeoBaoCaoTongQuanConfigReset` | — | internal | — | `CeoBaoCaoTongQuanConfigReset(CurrentUser)` → reset cấu hình báo cáo của user về mặc định | EF (service) | — |

## Quan hệ
- **Gọi sang:** `ICeoDashBaoCaoTongQuan` (GetConfigCeoBaoCaoTongQuanByUser / CeoBaoCaoTongQuanConfigUpdate / CeoBaoCaoTongQuanConfigReset).
- **Dữ liệu (entities):** cấu hình báo cáo tổng quan per-user; DTO `RequestCeoBaoCaoTongQuanConfig` (namespace `Models.Core.DTO.Core.Client.CeoDashBoard`).
- **Ripple / đồng bộ:** chỉ ảnh hưởng hiển thị dashboard của chính user.

## Lỗi đã biết / lưu ý kỹ thuật
- Không có permission key chi tiết — mọi user authenticated tự cấu hình báo cáo tổng quan của mình.
- Cùng nhóm dashboard CEO với `CeoDashBoardController` và `CeoDashboardTemplateController`.
