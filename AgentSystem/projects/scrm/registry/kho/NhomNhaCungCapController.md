---
controller: NhomNhaCungCapController
module: kho
service: WarehouseProduct
man_hinh_ui: "Kho > Nhóm nhà cung cấp — CRUD nhóm NCC; đồng thời cấp dropdown 'Nhóm NCC' cho màn quản lý nhà cung cấp / phiếu mua hàng."
muc_dich: "CRUD nhóm nhà cung cấp cấp đơn vị (tìm/thêm/sửa/xóa) + dropdown danh sách nhóm NCC. Guard cross-tenant theo DonViId."
alias: ["nhóm nhà cung cấp", "nhóm NCC", "DM nhóm NCC", "dropdown nhóm nhà cung cấp"]
auth: mixed                # SBAuthorize(key) cho CRUD; Dropdown để [SBAuthorize] rỗng
state_machine: []
deprecated: false
entities: [NhomNhaCungCap]
stored_procs: []
depends: [WarehouseProduct]
cross_module_in: []
cross_module_out: []
---

# NhomNhaCungCapController — Nhóm nhà cung cấp

> CRUD danh mục đơn giản (`partial class`). CUD giới hạn `DV-AD-TONG`; có guard cross-tenant (`IdDonVi == CurrentUser.DonViId`) ở `TimTheoId` và `Xoa`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid) | "Tìm kiếm" | POST `api/NhomNhaCungCap/TimTheoDieuKien` | `NhomNhaCungCap_TimTheoDieuKien` | public | màn list | List nhóm NCC theo filter trong scope user | EF | - |
| 2 | Thêm mới nhóm NCC | "Thêm mới" | POST `api/NhomNhaCungCap/ThemMoi` | `NhomNhaCungCap_ThemMoi` | public | popup Thêm | Insert nhóm NCC | EF | - |
| 3 | Tìm theo Id | "Sửa" | GET `api/NhomNhaCungCap/TimTheoId/{id:Guid}` | `NhomNhaCungCap_TimTheoId` | public | per-row | Trả nhóm NCC; **guard tenant**: chỉ trả khi `IdDonVi == CurrentUser.DonViId`, ngược lại lỗi (message copy nhầm: "Không tìm thấy nhà cung cấp!") | EF | message ghi "nhà cung cấp" thay vì "nhóm nhà cung cấp" (controller:52) |
| 4 | Cập nhật nhóm NCC | "Lưu" | POST `api/NhomNhaCungCap/CapNhatThongTin` | `NhomNhaCungCap_CapNhatThongTin` | public | popup Sửa | Update theo `CapNhatNhomNhaCungCapDto` | EF | - |
| 5 | Xóa nhóm NCC | "Xóa" | GET `api/NhomNhaCungCap/Xoa/{id:Guid}` | `NhomNhaCungCap_Xoa` | public | per-row | Guard tenant trước (`IdDonVi == CurrentUser.DonViId`), không khớp → lỗi NotFound; rồi xóa | EF | dùng HTTP **GET** cho xóa (lệch convention) — khớp FE, đừng đổi |
| 6 | Dropdown nhóm NCC | dropdown | POST `api/NhomNhaCungCap/DropdownDsNhomNhaCungCap` | `[SBAuthorize]` (rỗng — chỉ đăng nhập) | public | - | Trả list nhóm NCC làm dropdown theo CurrentUser | EF | quyền rỗng (chỉ đăng nhập) |

## Quan hệ
- **Gọi sang:** `INhomNhaCungCapService` (CRUD + dropdown).
- **Dữ liệu (entities):** `NhomNhaCungCap` (IdDonVi).
- **Ripple / đồng bộ:** nhóm NCC dùng phân loại `NhaCungCap`; xóa nhóm còn NCC gắn — *cần xác nhận* service có chặn.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/NhomNhaCungCapController.cs`
  - L52 (`TimTheoId`) message lỗi "Không tìm thấy nhà cung cấp!" (đúng phải "nhóm nhà cung cấp") — chỉ label.
  - L61-70 (`Xoa`): dùng `[HttpGet]` cho thao tác xóa — lệch convention, khớp FE.
  - L50, L66: guard cross-tenant `IdDonVi == CurrentUser.DonViId` — invariant bảo mật, giữ nguyên.
  - `partial class` — node cover file Controllers/NhomNhaCungCapController.cs; *cần xác nhận* không có partial bổ sung.
