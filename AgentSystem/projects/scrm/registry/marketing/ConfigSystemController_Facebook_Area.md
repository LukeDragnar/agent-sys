---
controller: ConfigSystemController
module: marketing
service: Facebook
man_hinh_ui: "Quản trị > Quản lý cấu hình ({domain}/quantri/quanLyCauHinh). CRUD + tra cứu config hệ thống (ConfigSystem). Area SanboxArea, route api/QuanTriArea/ConfigSystem."
muc_dich: "Quản trị bảng config hệ thống (ConfigSystem) của service Facebook: lấy theo mã/id/group, list tất cả + sắp xếp theo group, thêm/sửa/xóa config, lấy danh sách group. Bản Area QuanTri có endpoint thật (khác bản Controllers rỗng node _Facebook)."
alias: ["quản lý cấu hình", "ConfigSystem QuanTri", "quanLyCauHinh", "config theo mã", "TimTheoMa"]
auth: public
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends:
  - "IConfigSystemService — TimTheoMaAsync/GetAllAsync/GetLikeTreeSort/GetLikeTree/Insert/Delete/Update/GetByIdAsync/GetGroupCode/GetValueByCode"
  - "AutoMapperConfiguration — map ConfigSystemDto ↔ ConfigSystem"
cross_module_in:
  - "Web quản trị (quanLyCauHinh) — CRUD config"
  - "Service nội bộ — TimTheoMa/GetConfigSystem đọc 1 giá trị config"
cross_module_out: []
---

# ConfigSystemController (QuanTriArea) — Quản lý cấu hình hệ thống

> Area `SanboxArea`, route `api/QuanTriArea/ConfigSystem`. Controller-level `[SBAuthorize]` (cần đăng nhập) + `[TienIch(..., TI_HeThong, true)]`. Bản này có endpoint thật (đối lập bản Controllers rỗng `ConfigSystemController_Facebook.md`).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm config theo mã | (service dùng) | GET `api/QuanTriArea/ConfigSystem/TimTheoMa?ma=` | (chỉ check đăng nhập — `[SBAuthorize]` controller; key `ConfigSystem_TimTheoMa` bị comment) | public | — | `TimTheoMaAsync(ma)` → 1 config theo mã (vd `IS_SHOW_MOBILE`). | EF ConfigSystem | Key permission `ConfigSystem_TimTheoMa` comment → chỉ check đăng nhập. |
| 2 | Lấy tất cả config | (danh sách) | GET `api/QuanTriArea/ConfigSystem/LayTatCa` | `ConfigSystem_LayTatCa` ("Lấy tất cả config") | public | — | `GetAllAsync()`. | EF ConfigSystem | — |
| 3 | Lấy theo group + sort | (cây config) | POST `api/QuanTriArea/ConfigSystem/LayTatCaByGroupSort` | `ConfigSystem_LayTatCaTheoGroupVaSort` | public | — | `GetLikeTreeSort(filter)` → gom + sort theo group. | EF ConfigSystem | — |
| 4 | Lấy theo group | (cây config) | GET `api/QuanTriArea/ConfigSystem/LayTatCaByGroup` | `ConfigSystem_LayTatCaTheoGroup` | public | — | `GetLikeTree()`. | EF ConfigSystem | — |
| 5 | Thêm config | Thêm | POST `api/QuanTriArea/ConfigSystem/ThemMoi` | `ConfigSystem_ThemMoi` ("Thêm mới config") | public | ModelState hợp lệ | Map DTO→ConfigSystem, set NgayTao/NguoiTao=CurrentUser, Id mới, `Insert`. | EF ConfigSystem | — |
| 6 | Xóa config | Xóa | POST `api/QuanTriArea/ConfigSystem/Xoa/{id:Guid}` | `ConfigSystem_XoaConfigSystem` ("Xóa một config theo Id (ONLY_SUPERUSER)") | public | Tồn tại theo Id | `GetByIdAsync` null → `MESS_NotFound`; có → `Delete`. | EF ConfigSystem | Nhãn ghi ONLY_SUPERUSER nhưng chỉ map permission key — ràng buộc superuser thật phụ thuộc role gán cho key. |
| 7 | Cập nhật config | Lưu | POST `api/QuanTriArea/ConfigSystem/CapNhatThongTin` | `ConfigSystem_CapNhatThongTin` | public | Tồn tại theo Id | Map DTO; `GetByIdAsync` null → "Không tìm thấy ConfigSystem" (404); có → set NguoiCapNhat/NgayCapNhat, `Update`. | EF ConfigSystem | — |
| 8 | Lấy 1 config theo id | (sửa) | GET `api/QuanTriArea/ConfigSystem/TimTheoId/{id:Guid}` | `ConfigSystem_TimTheoId` | public | — | `GetByIdAsync(id)`; có data → map `ConfigSystemDto`. | EF ConfigSystem | — |
| 9 | Lấy danh sách group | (dropdown) | GET `api/QuanTriArea/ConfigSystem/LayCacNhomCauHinh` | `ConfigSystem_LayCacGroupConfigSystem` | public | — | `GetGroupCode()` → list groupCode. | EF ConfigSystem | — |
| 10 | Lấy giá trị config theo code | (service dùng) | GET `api/QuanTriArea/ConfigSystem/GetConfigSystem/{code}` | (chỉ check đăng nhập — `[SBAuthorize]` không key) | public | — | `GetValueByCode(code)` → string value. | EF ConfigSystem | — |

## Quan hệ
- **Gọi sang:** `IConfigSystemService` (CRUD + tra cứu bảng `ConfigSystem`).
- **Dữ liệu (entities):** `ConfigSystem`.
- **Ripple / đồng bộ:** sửa config ảnh hưởng hành vi toàn service đọc qua `GetConfig`/`GetConfigFacebook` (CommonController).

## Lỗi đã biết / lưu ý kỹ thuật
- `ConfigSystemController.cs:31` (Area) — key `ConfigSystem_TimTheoMa` comment → endpoint công khai cho mọi user đăng nhập đọc config theo mã.
- `[TienIch(..., true)]` → đánh dấu tiện ích nội bộ/quản trị; UI nằm ở `quantri/quanLyCauHinh`.
- Trùng tên với bản Controllers rỗng (node `ConfigSystemController_Facebook.md`) → node này suffix `_Facebook_Area`.
