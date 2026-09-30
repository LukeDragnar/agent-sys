---
controller: ConfigSystemController
module: cua-hang
service: PreOrder
man_hinh_ui: "Quản trị hệ thống (SandBox/QuanTriArea) — màn quản lý ConfigSystem của service PreOrder (CRUD config theo group, dạng cây/sort)"
muc_dich: "CRUD bảng ConfigSystem (cấu hình hệ thống dạng key-value theo group) cho service PreOrder: thêm/sửa/xóa, lấy theo mã/id/group, dựng cây + sort."
alias: ["config hệ thống preorder", "cấu hình hệ thống", "quản lý config system", "config theo group"]
auth: internal
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends: [PreOrder]
cross_module_in: [JobClearDonHangPreExpired đọc config 'SoNgayLuuDonHangPre' (qua ConfigSystemService.GetValueByCode); các luồng PreOrder đọc config theo mã]
cross_module_out: [giá trị config điều khiển hành vi hệ thống PreOrder (vd số ngày lưu đơn đặt trước)]
---

# ConfigSystemController — Quản lý ConfigSystem của service PreOrder

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy config theo mã | tra theo mã | GET `api/QuanTriArea/ConfigSystem/TimTheoMa?ma=` | `ConfigSystem_PreOrder_TimTheoMa` | internal | - | `TimTheoMaAsync(ma)` — trả config theo mã | EF | - |
| 2 | Lấy tất cả config | list | GET `api/QuanTriArea/ConfigSystem/LayTatCa` | `ConfigSystem_PreOrder_LayTatCa` | internal | - | `GetAllAsync()` | EF | - |
| 3 | Lấy tất cả theo group + sort | list dạng cây sort | POST `api/QuanTriArea/ConfigSystem/LayTatCaByGroupSort` | `ConfigSystem_PreOrder_LayTatCaTheoGroupVaSort` | internal | - | `GetLikeTreeSort(filter)` — cây theo group, sắp theo group+sort | EF | - |
| 4 | Lấy tất cả theo group | list dạng cây | GET `api/QuanTriArea/ConfigSystem/LayTatCaByGroup` | `ConfigSystem_PreOrder_LayTatCaTheoGroup` | internal | - | `GetLikeTree()` | EF | - |
| 5 | Thêm mới config | "Thêm mới" | POST `api/QuanTriArea/ConfigSystem/ThemMoi` | `ConfigSystem_PreOrder_ThemMoi` | internal | - | ModelState invalid → lỗi; map DTO→entity, set NgayTao/NguoiTao + Id mới → `Insert` | EF | - |
| 6 | Xóa config theo id | "Xóa" (ONLY_SUPERUSER) | POST `api/QuanTriArea/ConfigSystem/Xoa/{id:Guid}` | `ConfigSystem_PreOrder_XoaConfigSystem` | internal | chỉ superuser | Tra theo id, null → "Không tìm thấy"; `Delete(id)` (xóa cứng) | EF | Mô tả ghi "ONLY_SUPERUSER" nhưng không thấy guard riêng — chỉ permission key thường |
| 7 | Cập nhật config | "Lưu" | POST `api/QuanTriArea/ConfigSystem/CapNhatThongTin` | `ConfigSystem_PreOrder_CapNhatThongTin` | internal | - | ModelState invalid → lỗi; tra theo Id, null → "Không tìm thấy ConfigSystem !"; set NguoiCapNhat/NgayCapNhat → `Update` | EF | - |
| 8 | Lấy 1 config theo id | mở chi tiết | GET `api/QuanTriArea/ConfigSystem/TimTheoId/{id:Guid}` | `ConfigSystem_PreOrder_TimTheoId` | internal | - | Tra theo id; có → map entity→DTO trả về; không → trả raw result | EF | Nhánh "không thấy" trả `ResultWithData(data)` (bọc nguyên MethodResult) — hơi lạ |
| 9 | Lấy danh sách group config | dropdown group | GET `api/QuanTriArea/ConfigSystem/LayCacNhomCauHinh` | `ConfigSystem_PreOrder_LayCacGroupConfigSystem` | internal | - | `GetGroupCode()` — list mã group | EF | - |

## Quan hệ
- **Gọi sang:** không (CRUD thuần `IConfigSystemService` → EF; map bằng `AutoMapperConfiguration.Mapper`).
- **Dữ liệu (entities):** `ConfigSystem` (Ma, Value, Group, Sort, NgayTao/NguoiTao, NgayCapNhat/NguoiCapNhat).
- **Ripple / đồng bộ:** giá trị config điều khiển hành vi PreOrder. Ví dụ trọng yếu: `SoNgayLuuDonHangPre` — `JobClearDonHangPreExpired` đọc qua `GetValueByCode` (mặc định 30 nếu thiếu/không parse) để quyết định ngưỡng xóa đơn TrangThai=0.

## Lỗi đã biết / lưu ý kỹ thuật
- **Controller-level `[SBAuthorize]` + `[TienIch(..., MaTienIchSCRM.TI_HeThong, true)]`** — thuộc nhóm Hệ thống (cờ `true` = ẩn/đặc biệt), area QuanTriArea.
- **"ONLY_SUPERUSER" chỉ ghi trong mô tả Xóa** — không thấy code chặn role superuser, thực tế chỉ check permission key.
- Tất cả method `virtual` (cho phép override ở service khác kế thừa controller này).
- Code thật: BE `PreOrder/PreOrder.API/Areas/SandBoxArea/ConfigSystemController.cs`, service `PreOrder/PreOrder.Service/Generated/ConfigSystemService.cs`, entity `PreOrder.DataContext/Entity/ConfigSystem.cs`.
