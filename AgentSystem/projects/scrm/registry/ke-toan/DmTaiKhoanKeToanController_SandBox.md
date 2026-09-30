---
controller: DmTaiKhoanKeToanController
module: ke-toan
service: Accounting
man_hinh_ui: "Quản trị (SandBox/QuanTriArea) > Danh mục tài khoản kế toán: lưới list + Thêm/Sửa/Xóa tài khoản kế toán. Màn quản trị hệ thống (TienIch TI_HeThong)."
muc_dich: "CRUD danh mục TÀI KHOẢN KẾ TOÁN (hệ thống tài khoản hạch toán) cho khu vực quản trị SandBox: thêm/sửa/xóa/tìm/lấy theo Id."
alias: ["danh mục tài khoản kế toán", "dm tai khoan ke toan", "hệ thống tài khoản", "tài khoản hạch toán", "TKKT SandBox"]
auth: internal
state_machine: []
deprecated: false
entities: [TaiKhoanKeToan]
stored_procs: []
depends:
  - IDmTaiKhoanKeToanService
cross_module_in: []
cross_module_out: []
---

# DmTaiKhoanKeToanController — Danh mục tài khoản kế toán (Area SandBox / Quản trị)

> ⚠️ **TÊN TRÙNG**: tồn tại 1 controller cùng tên `DmTaiKhoanKeToanController` ở thư mục `Controllers\` (bản nghiệp vụ thường). Đây là bản trong **Area SandBox** (`Accounting.API/Areas/SandBoxArea/DmTaiKhoanKeToanController.cs`, route `api/QuanTriArea/[controller]`) → node lưu tên `DmTaiKhoanKeToanController_SandBox.md` để không đè bản kia.
>
> Controller CRUD thuần, gắn `[TienIch("Accounting_DmTaiKhoanKeToanController", "Quản lý Tài khoản kế toán", MaTienIchSCRM.TI_HeThong, true)]`. Mọi action passthrough về `IDmTaiKhoanKeToanService`, hậu tố `_SA`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm kiếm / lưới | Lưới list TK kế toán + lọc | `POST api/QuanTriArea/DmTaiKhoanKeToan/TimTheoDieuKien_SA` (TaiKhoanKeToanFilterDto) | TienIch `Accounting_DmTaiKhoanKeToanController` (TI_HeThong) | internal | màn quản trị TK kế toán | `TimTheoDieuKien_SA(gridInfo)`. | **EF** TaiKhoanKeToan | — |
| 2 | Thêm mới | Nút "Thêm" / Lưu | `POST api/QuanTriArea/DmTaiKhoanKeToan/ThemMoi_SA` (TaiKhoanKeToanDto) | TienIch | internal | nút Thêm | `ThemMoi_SA(reqData)`. | **EF** Insert | — |
| 3 | Xem chi tiết | (mở form sửa) | `POST api/QuanTriArea/DmTaiKhoanKeToan/TimTheoId_SA` (Guid id) | TienIch | internal | chọn 1 dòng | `TimTheoId_SA(id)`. | **EF** | — |
| 4 | Cập nhật | Nút "Sửa" / Lưu | `POST api/QuanTriArea/DmTaiKhoanKeToan/CapNhat_SA` (TaiKhoanKeToanDto) | TienIch | internal | dòng đã có | `CapNhat_SA(reqData)`. | **EF** Update | — |
| 5 | Xóa | Nút "Xóa" / confirm | `POST api/QuanTriArea/DmTaiKhoanKeToan/Xoa_SA` (Guid? id) | TienIch | internal | mỗi dòng | `Xoa_SA(id)`. | **EF** Delete | — |

## Quan hệ
- **Gọi sang:** `IDmTaiKhoanKeToanService` (toàn bộ logic).
- **Dữ liệu (entities):** `TaiKhoanKeToan` (hệ thống tài khoản kế toán).
- **Ripple / đồng bộ:** KHÔNG. Danh mục TK kế toán được `CommonController` (Accounting) đọc tham chiếu (GetAllTaiKhoanKeToan / TimTheoDieuKien_DmTaiKhoanKeToan) và là nguồn `MaTkKeToan` cho cấu hình tiền mặt/ví.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Areas/SandBoxArea/DmTaiKhoanKeToanController.cs`
  - L13 `[SBAuthorize]` + L14 `[TienIch(...TI_HeThong...)]`; action `[SBAuthorize()]` trống (L35,48,61,74,87).
  - **Trùng tên** với `DmTaiKhoanKeToanController` ở `Controllers\` → node Area này đặt tên `_SandBox` để phân biệt. Route khác: bản Area dưới `api/QuanTriArea/...`.
  - Passthrough thuần — logic/validate ở `IDmTaiKhoanKeToanService`.
