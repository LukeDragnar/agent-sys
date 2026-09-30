---
controller: ConfigSystemController
module: ke-toan
service: Invoice
man_hinh_ui: "Khu Quản trị (SandBox/QuanTriArea) > Cấu hình hệ thống Invoice: CRUD cấu hình hệ thống (config theo mã/group/sort) — chỉ super-user/quản trị hệ thống"
muc_dich: "CRUD bảng ConfigSystem của service Invoice (cấu hình hệ thống nội bộ: theo mã, theo group, sắp xếp, tree) — quản trị viên hệ thống"
alias: ["config system invoice", "cấu hình hệ thống", "ConfigSystem"]
auth: internal   # area QuanTriArea, [TienIch(..., TI_HeThong, true)] — quản trị hệ thống
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends:
  - IConfigSystemService
cross_module_in: []
cross_module_out: []
---

# ConfigSystemController — Cấu hình hệ thống Invoice (khu quản trị)

> Nằm trong `Areas/SandBoxArea`, route `api/QuanTriArea/[controller]` — khu **quản trị hệ thống** (`[TienIch("ConfigSystemController_Invoice", ..., TI_HeThong, true)]`, flag cuối = chỉ super-user). Controller-level `[SBAuthorize]`.
> CRUD chuẩn của bảng `ConfigSystem` (key-value config nội bộ, có group/sort/tree). Không liên quan HĐĐT nghiệp vụ — là hạ tầng cấu hình.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy config theo mã | (tra cứu) | `GET TimTheoMa?ma` | ConfigSystem_Invoice_TimTheoMa | internal | quản trị | `_config.TimTheoMaAsync(ma)` | EF `ConfigSystem` | — |
| 2 | Lấy tất cả config | Lưới config | `GET LayTatCa` | ConfigSystem_Invoice_LayTatCa | internal | — | `_config.GetAllAsync()` | EF | — |
| 3 | Tất cả config theo group+sort | Tree (lọc) | `POST LayTatCaByGroupSort` | ConfigSystem_Invoice_LayTatCaTheoGroupVaSort | internal | — | `_config.GetLikeTreeSort(filter)` | EF | — |
| 4 | Tất cả config theo group | Tree | `GET LayTatCaByGroup` | ConfigSystem_Invoice_LayTatCaTheoGroup | internal | — | `_config.GetLikeTree()` | EF | — |
| 5 | Thêm config | Nút "Thêm" | `POST ThemMoi` | ConfigSystem_Invoice_ThemMoi | internal | — | Validate; map DTO→entity, set NgayTao/NguoiTao/Id mới; `_config.Insert` | EF | — |
| 6 | Xóa config theo Id | Nút "Xóa" (ONLY_SUPERUSER) | `POST Xoa/{id:Guid}` | ConfigSystem_Invoice_XoaConfigSystem | internal | super-user | GetById null → MESS_NotFound; có → `Delete(id)` | EF | mô tả "ONLY_SUPERUSER" |
| 7 | Cập nhật config | Nút "Lưu" | `POST CapNhatThongTin` | ConfigSystem_Invoice_CapNhatThongTin | internal | — | Validate; map; GetById null → 404 "Không tìm thấy ConfigSystem"; set NguoiCapNhat/NgayCapNhat; `Update` | EF | — |
| 8 | Lấy 1 config theo Id | (mở form sửa) | `GET TimTheoId/{id:Guid}` | ConfigSystem_Invoice_TimTheoId | internal | — | GetById; có Data → map DTO; null → trả wrapper | EF | nếu Data==null trả `ResultWithData(data)` (bọc cả MethodResult vào Data) — hơi lạ |
| 9 | Lấy các group config | (dropdown group) | `GET LayCacNhomCauHinh` | ConfigSystem_Invoice_LayCacGroupConfigSystem | internal | — | `_config.GetGroupCode()` | EF | — |

## Quan hệ
- **Gọi sang:** `IConfigSystemService` → `IRepository<ConfigSystem>`. Dùng AutoMapper (ConfigSystemDto ↔ ConfigSystem).
- **Dữ liệu (entities):** `ConfigSystem` (key-value config nội bộ, có Group/Sort).
- **Ripple / đồng bộ:** không có; cấu hình đọc nội bộ service.

## Lỗi đã biết / lưu ý kỹ thuật (path:line)
- `ConfigSystemController.cs:107-115` (`TimTheoIdAsync`) — nhánh `Data == null` trả `ResultWithData(data)` đóng gói cả object MethodResult vào field Data (không phải DTO) → client nhận structure khác nhánh thành công.
- Khu quản trị hệ thống (`TI_HeThong`, super-user) — không thuộc nghiệp vụ HĐĐT, là hạ tầng cấu hình per-service.
