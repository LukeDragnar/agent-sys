---
controller: CeoDashBoardController
module: he-thong
service: Core
man_hinh_ui: "Dashboard CEO > Bố cục widget — cấu hình widget hiển thị theo user (api/CeoDashBoard)"
muc_dich: "Quản lý cấu hình widget trên dashboard CEO theo từng user (đọc/cập nhật bố cục widget) và lấy danh sách dashboard template để chọn."
alias: ["CeoDashBoard", "dashboard CEO", "config widget CEO", "bố cục widget dashboard"]
auth: internal
state_machine: []
deprecated: false
entities: [CeoDashboardWidgetConfig, CeoDashboardTemplate]
stored_procs: []
depends: ["ICeoDashBoardService", "ICeoDashboardTemplateService", "CurrentUser"]
cross_module_in: ["Màn dashboard CEO gọi GetConfigWidgetByUser khi render bố cục widget"]
cross_module_out: []
---

# CeoDashBoardController — Cấu hình widget dashboard CEO

Biến thể **non-Area** namespace `Core.API.Controllers`, route `[Route("api/[controller]")]` → path gốc `/api/CeoDashBoard/...`. Controller-level `[SBAuthorize]` (không TienIch). Kế thừa `BaseController`. Inject 2 service: `ICeoDashBoardService` (config widget) và `ICeoDashboardTemplateService` (lấy template). Action gắn `CurrentUser` — cấu hình per-user.

File: `Core/Core.API/Controllers/CeoDashBoardController.cs`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình widget của user | (render dashboard) | GET `/api/CeoDashBoard/GetConfigWidgetByUser` | — (`[SBAuthorize]` controller-level) | internal | — | `GetConfigWidgetByUser(CurrentUser)` → bố cục widget của user hiện tại | EF (service) | Có code test (mock UserLogin SuperUser) đã comment dòng 29-34 |
| 2 | Cập nhật cấu hình widget | Lưu bố cục | POST `/api/CeoDashBoard/ConfigWidgetUpdate` | — | internal | — | `ConfigWidgetUpdate(request, CurrentUser)` → lưu bố cục widget cho user | EF (service) | Có code test (mock user) đã comment dòng 45-49 |
| 3 | Lấy tất cả dashboard template | (chọn template) | GET `/api/CeoDashBoard/LayTatCaDashboardTemplate` | — | internal | — | `_ceoDashboardTemplateService.LayTatCa(CurrentUser)` → danh sách template để chọn | EF (service) | — |

## Quan hệ
- **Gọi sang:** `ICeoDashBoardService` (GetConfigWidgetByUser / ConfigWidgetUpdate); `ICeoDashboardTemplateService` (LayTatCa).
- **Dữ liệu (entities):** cấu hình widget per-user, dashboard template; DTO `RequestConfigWidget` (namespace `Models.Core.DTO.Core.Client.CeoDashBoard`).
- **Ripple / đồng bộ:** chỉ ảnh hưởng hiển thị dashboard của chính user. Template lấy từ `CeoDashboardTemplateController` (CRUD).

## Lỗi đã biết / lưu ý kỹ thuật
- Còn block code test mock `UserLogin` (SuperUser) bị comment trong `GetConfigWidgetByUser` (dòng 29-34) và `ConfigWidgetUpdate` (dòng 45-49) → dọn khi refactor.
- Không có permission key chi tiết — mọi user authenticated tự cấu hình widget của mình.
- Cùng nhóm dashboard CEO với `CeoDashBaoCaoTongQuanController` và `CeoDashboardTemplateController`.
