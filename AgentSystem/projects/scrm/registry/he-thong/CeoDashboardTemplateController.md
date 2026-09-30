---
controller: CeoDashboardTemplateController
module: he-thong
service: Core
man_hinh_ui: "Dashboard CEO > Quản lý template — thêm/xóa dashboard template (api/CeoDashboardTemplate)"
muc_dich: "Quản lý dashboard template cho dashboard CEO: thêm mới template, xóa template theo id."
alias: ["CeoDashboardTemplate", "dashboard template", "template dashboard CEO", "mẫu dashboard"]
auth: internal
state_machine: []
deprecated: false
entities: [CeoDashboardTemplate]
stored_procs: []
depends: ["ICeoDashboardTemplateService", "CurrentUser"]
cross_module_in: ["CeoDashBoardController.LayTatCaDashboardTemplate đọc danh sách template do controller này tạo"]
cross_module_out: ["Template tạo ở đây dùng cho bố cục widget dashboard CEO của các user"]
---

# CeoDashboardTemplateController — Quản lý dashboard template CEO

Biến thể **non-Area** namespace `Core.API.Controllers`, route `[Route("api/[controller]")]` → path gốc `/api/CeoDashboardTemplate/...` (dòng route `api/QuanTriArea/...` đã bị comment). Có `[TienIch("CeoDashboardTemplateController", ..., MaTienIchSCRM.TI_HeThong, true)]`. Kế thừa `BaseController`. Action dùng `[SBAuthorize()]` trống (permission key bị comment) → chỉ cần authenticated. Có validate `ModelState` ở ThemMoi.

File: `Core/Core.API/Controllers/CeoDashboardTemplateController.cs`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới template | Thêm mới | POST `/api/CeoDashboardTemplate/ThemMoi` | — (`[SBAuthorize()]` trống, key comment) | internal | — | Validate ModelState (lỗi → `MESS_Model_UnValidate`); hợp lệ → `Create(model, CurrentUser)` tạo template | EF (service) | Permission key `CeoDashboardTemplate_ThemMoi` bị comment (dòng 22) |
| 2 | Xóa template theo id | Xóa | POST `/api/CeoDashboardTemplate/Xoa/{id}` | — (`[SBAuthorize()]` trống, key comment) | internal | — | `Delete(id, CurrentUser)` → xóa template theo id | EF (service) | Permission key `CeoDashboardTemplate_Xoa` bị comment (dòng 37) |

## Quan hệ
- **Gọi sang:** `ICeoDashboardTemplateService` (Create / Delete).
- **Dữ liệu (entities):** `CeoDashboardTemplate`; DTO `RequestDashboadTemplate` (namespace `Models.Core.DTO.Core.Client.CeoDashBoard` / `...DMDashboard`).
- **Ripple / đồng bộ:** template được `CeoDashBoardController.LayTatCaDashboardTemplate` đọc để user chọn bố cục.

## Lỗi đã biết / lưu ý kỹ thuật
- Permission key bị comment ở cả 2 action (dòng 22, 37) → mọi user authenticated thêm/xóa được template (rủi ro phân quyền).
- Dòng route `api/QuanTriArea/[controller]` bị comment, route thực tế là `api/[controller]` → không thuộc QuanTriArea dù TienIch là TI_HeThong.
- DTO `RequestDashboadTemplate` typo trong tên gốc ("Dashboad" thiếu chữ r) — giữ nguyên theo source.
