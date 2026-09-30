---
controller: ConfigSystemController
module: khach-hang
service: Customer
man_hinh_ui: "Quản trị/cấu hình hệ thống Customer (CRUD config theo group/sort) — biến thể admin SandBox/QuanTri area, route api/QuanTriArea/ConfigSystemController; nội bộ — không có màn hình KH thường"
muc_dich: "CRUD cấu hình hệ thống (ConfigSystem) cho service Customer: lấy theo mã/id, theo group, dạng cây sort, thêm/sửa/xóa config"
alias: ["ConfigSystem", "Config Customer", "Cấu hình hệ thống Customer", "ConfigSystem (SandBoxArea/admin)"]
auth: public
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends: [IConfigSystemService, BaseController]
cross_module_in: []
cross_module_out: []
---

# ConfigSystemController — Quản lý cấu hình hệ thống (Customer, biến thể admin)

> Thuộc namespace `Customer.API.Areas.SandBoxArea` (vùng admin/quản trị hệ thống của service Customer).
> **Lưu ý route:** controller này dùng route prefix **`api/QuanTriArea/[controller]`** (KHÔNG phải `api/SanboxArea/`), khác với 4 controller danh mục KH cùng area.
> Không trùng class name với controller non-SA nào (đây là controller riêng của vùng quản trị).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy config theo mã | (tra cứu) | `GET api/QuanTriArea/ConfigSystemController/TimTheoMa?ma=` | ConfigSystem_Customer_TimTheoMa | public | — | `TimTheoMaAsync(ma)` trả config khớp mã | EF (IConfigSystemService) | — |
| 2 | Lấy tất cả config | Lưới cấu hình | `GET .../LayTatCa` | ConfigSystem_Customer_LayTatCa | public | — | `GetAllAsync()` trả toàn bộ config | EF | — |
| 3 | Lấy tất cả theo group + sort | Cây cấu hình (sort) | `POST .../LayTatCaByGroupSort` | ConfigSystem_Customer_LayTatCaTheoGroupVaSort | public | — | `GetLikeTreeSort(filter)` — dạng cây theo group, sắp xếp theo sort | EF | — |
| 4 | Lấy tất cả theo group | Cây cấu hình | `GET .../LayTatCaByGroup` | ConfigSystem_Customer_LayTatCaTheoGroup | public | — | `GetLikeTree()` — gom theo group | EF | — |
| 5 | Thêm mới config | Nút "Thêm mới" | `POST .../ThemMoi` | ConfigSystem_Customer_ThemMoi | public | — | ModelState invalid → `MESS_Model_UnValidate`; map DTO→entity, set `NgayTao`/`NguoiTaoUserId/UserName` từ `CurrentUser`, gán `Id=Guid.NewGuid()`, `Insert` | EF | — |
| 6 | Cập nhật config | Nút "Lưu" | `POST .../CapNhatThongTin` | ConfigSystem_Customer_CapNhatThongTin | public | — | ModelState invalid → lỗi; `GetByIdAsync` → null trả "Không tìm thấy ConfigSystem !" (404); set `NguoiCapNhat*`/`NgayCapNhat`, `Update` | EF | — |
| 7 | Lấy config theo Id | (mở chi tiết) | `GET .../TimTheoId/{id:Guid}` | ConfigSystem_Customer_TimTheoId | public | — | `GetByIdAsync(id)`; có data → map entity→DTO; null → trả raw result | EF | — |
| 8 | Lấy danh sách nhóm config | Dropdown group | `GET .../LayCacNhomCauHinh` | ConfigSystem_Customer_LayCacGroupConfigSystem | public | — | `GetGroupCode()` trả các group code | EF | — |
| 9 | Xóa config theo Id | Nút "Xóa" | `POST .../Xoa/{id:Guid}` | ConfigSystem_Customer_XoaConfigSystem | public | — | `GetByIdAsync` → null trả `MESS_NotFound`; ngược lại `Delete(id)`. Mô tả quyền ghi chú **ONLY_SUPERUSER** | EF | Mô tả gợi ý chỉ superuser được xóa, nhưng attribute không khai báo role cứng — kiểm soát thực tế nằm ở phân quyền key |

## Quan hệ
- **Gọi sang:** `IConfigSystemService` (CRUD + GetLikeTree/GetLikeTreeSort/GetGroupCode/GetByIdAsync).
- **Dữ liệu (entities):** `ConfigSystem` (entity gốc, map qua `AutoMapperConfiguration.Mapper`).
- **Ripple / đồng bộ:** Cấu hình hệ thống của service Customer — thay đổi config có thể ảnh hưởng hành vi các module đọc config (chưa xác định caller cụ thể trong source này).

## Lỗi đã biết / lưu ý kỹ thuật
- `ConfigSystemController.cs:15` — route prefix `api/QuanTriArea/[controller]`, KHÁC `api/SanboxArea/` của các controller danh mục KH cùng area → dễ nhầm khi build URL.
- `ConfigSystemController.cs:76-85` (`XoaTheoId`) — mô tả quyền ghi "ONLY_SUPERUSER" nhưng `[SBAuthorize]` không truyền role string → ràng buộc superuser phụ thuộc cấu hình phân quyền ngoài code.
- `ConfigSystemController.cs:127-133` — endpoint `GetConfigSystemByCode` đã bị **comment** (không còn dùng).
- Controller-level `[SBAuthorize]` không kèm role → mức `public` (chỉ yêu cầu đăng nhập); từng action siết bằng permission key.
