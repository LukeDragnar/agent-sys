---
controller: ConfigSystemController
module: he-thong
service: Core
man_hinh_ui: "Quản trị > Quản lý cấu hình ({domain}/quantri/quanLyCauHinh) — biến thể QuanTriArea (super admin)"
muc_dich: "CRUD + đọc cấu hình hệ thống (ConfigSystem) cho màn quản trị cấu hình: tìm theo mã/id, lấy theo group/sort, thêm/sửa/xóa (xóa chỉ SuperUser)."
alias: ["ConfigSystem", "cấu hình hệ thống", "quản lý cấu hình", "quanLyCauHinh", "config"]
auth: mixed
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends: ["IConfigSystemService", "AutoMapper (ConfigSystemDto ↔ ConfigSystem)", "CurrentUser (audit NgayTao/NguoiTao...)"]
cross_module_in: ["Service nội bộ gọi TimTheoMa để đọc 1 config theo mã (vd IS_SHOW_MOBILE)"]
cross_module_out: ["Mọi module đọc cấu hình hệ thống (giá trị config ảnh hưởng hành vi toàn hệ thống)"]
---

# ConfigSystemController (QuanTriArea) — Quản lý cấu hình hệ thống (super admin)

Biến thể **Area** `Core.API.Areas.SanboxArea`, route thật `[Route("api/QuanTriArea/[controller]")]` → path gốc `/api/QuanTriArea/ConfigSystem/...` (KHÔNG có `[Area]` attribute, dù namespace là SanboxArea). Có `[TienIch("ConfigSystemController", ..., MaTienIchSCRM.TI_HeThong)]`. Kế thừa `BaseController` (không phải `SBBaseController`). Controller-level `[SBAuthorize]`; hầu hết action có permission key riêng (`ConfigSystem_*`). Đây là biến thể đầy đủ phục vụ màn quản trị; biến thể user thường (đọc-only) ở `ConfigSystemController.md`.

File: `Core/Core.API/Areas/SanboxArea/ConfigSystemController.cs`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm config theo mã | (service đọc config) | GET `/api/QuanTriArea/ConfigSystem/TimTheoMa?ma=` | — (permission bị comment) | internal (`[SBAuthorize]` controller) | — | `TimTheoMaAsync(ma)` → trả 1 config theo mã (vd IS_SHOW_MOBILE) | EF | Attr permission bị comment (dòng 35) → chỉ còn auth controller-level |
| 2 | Lấy tất cả config | Lưới cấu hình | GET `/api/QuanTriArea/ConfigSystem/LayTatCa` | `ConfigSystem_LayTatCa` | internal | — | `GetAllAsync()` → toàn bộ ConfigSystem | EF | — |
| 3 | Lấy theo group + sort | Cây cấu hình | POST `/api/QuanTriArea/ConfigSystem/LayTatCaByGroupSort` | `ConfigSystem_LayTatCaTheoGroupVaSort` | internal | — | `GetLikeTreeSort(filter)` → gom theo group + sort, hiển thị dạng cây | EF | — |
| 4 | Lấy theo group | Cây cấu hình | GET `/api/QuanTriArea/ConfigSystem/LayTatCaByGroup` | `ConfigSystem_LayTatCaTheoGroup` | internal | — | `GetLikeTree()` → gom theo group | EF | — |
| 5 | Thêm mới | Nút "Thêm" | POST `/api/QuanTriArea/ConfigSystem/ThemMoi` | `ConfigSystem_ThemMoi` | internal | — | Validate → map Dto→Entity, set NgayTao/NguoiTao=CurrentUser, Id mới → `Insert` | EF | — |
| 6 | Xóa config | Nút "Xóa" | POST `/api/QuanTriArea/ConfigSystem/Xoa/{id:Guid}` | `ConfigSystem_XoaConfigSystem` | internal | chỉ SuperUser (ghi chú ONLY_SUPERUSER trong key) | Check tồn tại (`GetByIdAsync.Data`) → `Delete(id)` | EF | Ghi chú "ONLY_SUPERUSER" nằm ở mô tả key — thực thi giới hạn super user do phân quyền, không hardcode trong code |
| 7 | Cập nhật | Nút "Lưu" | POST `/api/QuanTriArea/ConfigSystem/CapNhatThongTin` | `ConfigSystem_CapNhatThongTin` | internal | sửa bản ghi đã tồn tại | Validate → map → check tồn tại → set NguoiCapNhat/NgayCapNhat → `Update`; không thấy → NotFound 404 | EF | — |
| 8 | Lấy 1 config theo Id | (mở form sửa) | GET `/api/QuanTriArea/ConfigSystem/TimTheoId/{id:Guid}` | `ConfigSystem_TimTheoId` | internal | — | `GetByIdAsync(id)` → map Entity→Dto trả về để sửa | EF | — |
| 9 | Lấy danh sách group | Dropdown nhóm | GET `/api/QuanTriArea/ConfigSystem/LayCacNhomCauHinh` | `ConfigSystem_LayCacGroupConfigSystem` | internal | — | `GetGroupCode()` → danh sách groupCode đang có | EF | — |
| 10 | Lấy giá trị config theo code | (service đọc) | GET `/api/QuanTriArea/ConfigSystem/GetConfigSystem/{code}` | — (`[SBAuthorize]` trống) | internal | — | `GetValueByCode(code)` → trả value (string) theo code | EF | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] Service/màn khác gọi `TimTheoMa?ma=` để đọc 1 config theo mã (vd `IS_SHOW_MOBILE` ẩn/hiện điện thoại).
- [internal] `GetConfigSystem/{code}` → đọc raw value theo code (dùng trong logic backend cần giá trị config).

## Quan hệ
- **Gọi sang:** `IConfigSystemService` (TimTheoMa, GetAll, GetLikeTree(Sort), Insert, Update, Delete, GetByIdAsync, GetGroupCode, GetValueByCode).
- **Dữ liệu (entities):** `ConfigSystem` (map qua `ConfigSystemDto`).
- **Ripple / đồng bộ:** giá trị config thay đổi → ảnh hưởng hành vi nhiều module đọc config (vd ẩn/hiện field, key tích hợp).

## Lỗi đã biết / lưu ý kỹ thuật
- `ConfigSystemController.cs:35` (Area) — attribute `[SBAuthorize("ConfigSystem_TimTheoMa", ...)]` bị comment → endpoint `TimTheoMa` chỉ còn auth controller-level, mất kiểm soát quyền chi tiết.
- Trùng tên controller với biến thể user thường `Core/Core.API/Controllers/ConfigSystemController.cs` (route `api/ConfigSystem`, đọc-only, có `[AllowAnonymous]` GetConfigApp) → cùng class name `ConfigSystemController` ở 2 namespace, dễ nhầm khi tra route.
- Nhiều endpoint sửa/đọc cùng service nhưng method tên không đồng nhất (`Insert` vs `InsertAsync`, `Delete` vs `DeleteAsync`) — lưu ý khi trace.
