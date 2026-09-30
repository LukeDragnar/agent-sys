---
controller: DMDashboardController
module: he-thong
service: Core
man_hinh_ui: "nội bộ — không có UI quản trị; cấp dữ liệu danh mục dashboard (gom theo group) cho màn dashboard user hiển thị"
muc_dich: "Đọc danh mục dashboard gom theo group cho user thường (biến thể non-Area): chỉ 1 endpoint GetAllByGroup — phục vụ render dashboard, không CRUD."
alias: ["DMDashboard", "dashboard user", "danh mục dashboard theo group", "GetAllByGroup"]
auth: internal
state_machine: []
deprecated: false
entities: [DMDashboard]
stored_procs: []
depends: ["IDMDashboardService", "CurrentUser"]
cross_module_in: ["Màn dashboard user gọi GetAllByGroup để render danh mục theo nhóm"]
cross_module_out: []
---

# DMDashboardController (non-Area) — Đọc danh mục Dashboard theo group cho user

Biến thể **non-Area** namespace `Core.API`, route `[Route("api/[controller]")]` → path gốc `/api/DMDashboard/...`. Có `[TienIch("DMDashboardController", ..., MaTienIchSCRM.TI_Chung)]` (lưu ý: TI_Chung, KHÁC biến thể Area dùng TI_HeThong). Kế thừa `BaseController`. Chỉ 1 endpoint, `[SBAuthorize]` trống (không permission key — commented). Đây là biến thể đọc cho user; CRUD quản trị nằm ở `DMDashboardController_Core.md` (route `api/QuanTriArea/DMDashboard`).

File: `Core/Core.API/Controllers/DMDashboardController.cs`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy danh mục theo group | (render dashboard) | GET `/api/DMDashboard/GetAllByGroup` | — (`[SBAuthorize]` trống, key bị comment) | internal | — | `GetAllByGroup(CurrentUser)` → danh mục dashboard gom theo group để hiển thị | EF (service) | Permission key `DMDashboard_GetAllByGroup` bị comment (dòng 20) → chỉ còn auth chung |

## Quan hệ
- **Gọi sang:** `IDMDashboardService.GetAllByGroup(CurrentUser)`.
- **Dữ liệu (entities):** `DMDashboard`.
- **Ripple / đồng bộ:** chỉ đọc; nguồn dữ liệu là danh mục đã cấu hình qua biến thể Area.

## Lỗi đã biết / lưu ý kỹ thuật
- `DMDashboardController.cs:20` (non-Area) — permission key `DMDashboard_GetAllByGroup` bị comment, chỉ còn `[SBAuthorize]` trống → mọi user authenticated đọc được.
- Trùng tên class `DMDashboardController` với biến thể Area (`Areas/SanboxArea/DMDashboardController.cs`, route `api/QuanTriArea/DMDashboard`) → 2 controller cùng tên ở 2 namespace.
- TienIch ở đây là `TI_Chung`, biến thể Area là `TI_HeThong` → khác nhóm tiện ích.
