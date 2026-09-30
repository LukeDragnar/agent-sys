---
controller: DMDashboardController
module: he-thong
service: Core
man_hinh_ui: "Quản trị > Danh mục Dashboard (QuanTriArea — TienIch TI_HeThong); CRUD danh mục dashboard cho admin (nhóm quyền HT-AD)"
muc_dich: "CRUD + tra cứu Danh mục dashboard (DMDashboard) cho màn quản trị: thêm/sửa/xóa, tìm theo điều kiện, lấy theo Id, lấy tất cả, lấy group code."
alias: ["DMDashboard", "danh mục dashboard", "quản lý dashboard", "dashboard admin"]
auth: internal
state_machine: []
deprecated: false
entities: [DMDashboard]
stored_procs: []
depends: ["IDMDashboardService", "CurrentUser (truyền vào service mọi action)"]
cross_module_in: ["Màn cấu hình dashboard (admin HT-AD) gọi CRUD danh mục"]
cross_module_out: ["Dashboard hiển thị cho user (biến thể non-Area GetAllByGroup đọc danh mục đã cấu hình)"]
---

# DMDashboardController (QuanTriArea) — Quản lý danh mục Dashboard (admin)

Biến thể **Area** `Core.API.Areas.SanboxArea`, route thật `[Route("api/QuanTriArea/[controller]")]` → path gốc `/api/QuanTriArea/DMDashboard/...` (KHÔNG có `[Area]` attribute, dù namespace là SanboxArea). Có `[TienIch("DMDashboardController", ..., MaTienIchSCRM.TI_HeThong)]`. Kế thừa `BaseController`. KHÔNG có `[SBAuthorize]` controller-level — mỗi action có permission key riêng `DMDashboard_*` (nhóm quyền `HT-AD`). Mọi action truyền `CurrentUser` xuống service. Đây là biến thể quản trị đầy đủ; biến thể đọc cho user ở `DMDashboardController.md` (route `api/DMDashboard/GetAllByGroup`).

File: `Core/Core.API/Areas/SanboxArea/DMDashboardController.cs`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Cập nhật | Nút "Lưu" | POST `/api/QuanTriArea/DMDashboard/CapNhat` | `DMDashboard_CapNhat` (HT-AD) | internal | sửa bản ghi tồn tại | Validate model → `Update(model, CurrentUser)` | EF (service) | — |
| 2 | Thêm mới | Nút "Thêm" | POST `/api/QuanTriArea/DMDashboard/ThemMoi` | `DMDashboard_ThemMoi` (HT-AD) | internal | — | Validate → `Create(model, CurrentUser)` | EF | — |
| 3 | Tìm theo điều kiện | Lưới danh mục | POST `/api/QuanTriArea/DMDashboard/TimTheoDieuKien` | `DMDashboard_TimTheoDieuKien` (HT-AD) | internal | — | Validate `DMDashboardTimKiemPayload` → `TimTheoDieuKien(model, CurrentUser)` (lọc/paging) | EF | — |
| 4 | Lấy theo Id | (mở form sửa) | GET `/api/QuanTriArea/DMDashboard/LayTheoId/{id:Guid}` | `DMDashboard_LayTheoId` (HT-AD) | internal | — | `LayTheoId(id, CurrentUser)` → 1 danh mục dashboard | EF | — |
| 5 | Xóa | Nút "Xóa" | GET `/api/QuanTriArea/DMDashboard/Xoa/{id:Guid}` | `DMDashboard_Xoa` (HT-AD) | internal | — | `Delete(id, CurrentUser)` | EF | **Dùng `[HttpGet]` cho hành động xóa** (side-effect qua GET) — lệch convention REST/an toàn |
| 6 | Lấy group code | Dropdown nhóm | GET `/api/QuanTriArea/DMDashboard/GetGroupCode` | `DMDashboard_GetGroupCode` (HT-AD) | internal | — | `GetGroupCode(CurrentUser)` → danh sách tên nhóm | EF | — |
| 7 | Lấy tất cả | (list) | GET `/api/QuanTriArea/DMDashboard/LayTatCa` | `DMDashboard_LayTatCa` (HT-AD) | internal | — | `LayTatCa(CurrentUser)` → toàn bộ danh mục dashboard | EF | — |

## Quan hệ
- **Gọi sang:** `IDMDashboardService` (Update, Create, TimTheoDieuKien, LayTheoId, Delete, GetGroupCode, LayTatCa) — mọi call kèm `CurrentUser`.
- **Dữ liệu (entities):** `DMDashboard` (DTO `DMDashboardDto`, payload tìm kiếm `DMDashboardTimKiemPayload`).
- **Ripple / đồng bộ:** danh mục cấu hình ở đây là nguồn cho dashboard hiển thị phía user (biến thể non-Area `GetAllByGroup`).

## Lỗi đã biết / lưu ý kỹ thuật
- `DMDashboardController.cs:68-74` (Area) — action `Xoa` dùng `[HttpGet]` → xóa qua GET, dễ bị trigger ngoài ý muốn (prefetch/crawler), lệch convention nên dùng POST.
- Trùng tên class `DMDashboardController` với biến thể non-Area (`Core/Core.API/DMDashboardController.cs`, route `api/DMDashboard`) → 2 controller cùng tên ở 2 namespace; lưu ý khi tra route.
- TienIch biến thể Area là `TI_HeThong`, còn biến thể non-Area là `TI_Chung` → 2 nhóm tiện ích khác nhau cho cùng entity.
