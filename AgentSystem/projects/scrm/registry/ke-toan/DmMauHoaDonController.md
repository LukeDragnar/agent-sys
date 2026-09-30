---
controller: DmMauHoaDonController
module: ke-toan
service: Accounting
man_hinh_ui: "Quản trị (SandBox/QuanTriArea) > Danh mục mẫu hóa đơn: lưới list + Thêm/Sửa/Xóa mẫu hóa đơn. Màn quản trị hệ thống (TienIch TI_HeThong), không phải màn nghiệp vụ thường."
muc_dich: "CRUD danh mục MẪU HÓA ĐƠN (DmMauHoaDon) cho khu vực quản trị SandBox: thêm/sửa/xóa/tìm/lấy theo Id mẫu hóa đơn dùng chung."
alias: ["danh mục mẫu hóa đơn", "dm mau hoa don", "mẫu hóa đơn", "quản trị mẫu HĐ", "MauHoaDon SandBox"]
auth: internal
state_machine: []
deprecated: false
entities: [MauHoaDon]
stored_procs: []
depends:
  - IDmMauHoaDonService
cross_module_in: []
cross_module_out: []
---

# DmMauHoaDonController — Danh mục mẫu hóa đơn (Area SandBox / Quản trị)

> Controller CRUD thuần trong **Area SandBox** (route `api/QuanTriArea/[controller]`), gắn `[TienIch("Accounting_DmMauHoaDonController", "Quản lý Danh mục mẫu hóa đơn", MaTienIchSCRM.TI_HeThong, true)]` → quyền theo tiện ích hệ thống. Mọi action ủy thác về `IDmMauHoaDonService`, đặt hậu tố `_SA` (SandBox Area). Toàn bộ logic ở service.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm kiếm / lưới | Lưới list mẫu HĐ + lọc | `POST api/QuanTriArea/DmMauHoaDon/TimTheoDieuKien_SA` (MauHoaDonFilterDto) | TienIch `Accounting_DmMauHoaDonController` (TI_HeThong) | internal | màn quản trị mẫu HĐ | `TimTheoDieuKien_SA(gridInfo)` → list + total. | **EF** MauHoaDon | — |
| 2 | Thêm mới | Nút "Thêm" / Lưu | `POST api/QuanTriArea/DmMauHoaDon/ThemMoi_SA` (MauHoaDonDto) | TienIch (TI_HeThong) | internal | nút Thêm | `ThemMoi_SA(reqData)`. | **EF** Insert | — |
| 3 | Xem chi tiết | (mở form sửa) | `POST api/QuanTriArea/DmMauHoaDon/TimTheoId_SA` (Guid id) | TienIch | internal | chọn 1 dòng | `TimTheoId_SA(id)`. | **EF** | — |
| 4 | Cập nhật | Nút "Sửa" / Lưu | `POST api/QuanTriArea/DmMauHoaDon/CapNhat_SA` (MauHoaDonDto) | TienIch | internal | dòng đã có | `CapNhat_SA(reqData)`. | **EF** Update | — |
| 5 | Xóa | Nút "Xóa" / confirm | `POST api/QuanTriArea/DmMauHoaDon/Xoa_SA` (Guid? id) | TienIch | internal | mỗi dòng | `Xoa_SA(id)`. | **EF** Delete | — |

## Quan hệ
- **Gọi sang:** `IDmMauHoaDonService` (toàn bộ logic). Controller chỉ passthrough.
- **Dữ liệu (entities):** `MauHoaDon` (danh mục mẫu hóa đơn).
- **Ripple / đồng bộ:** KHÔNG. Danh mục được `CommonController` (Accounting) đọc tham chiếu (GetAllMauHoaDon / TimTheoDieuKien_DmMauHoaDon).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Areas/SandBoxArea/DmMauHoaDonController.cs`
  - L13 `[SBAuthorize]` class-level + L14 `[TienIch(...TI_HeThong...)]`; mỗi action thêm `[SBAuthorize()]` trống (L35,48,61,74,87) → quyền thực chất do tiện ích hệ thống.
  - Route `api/QuanTriArea/[controller]` (khác các controller Controllers\) → endpoint dưới `/api/QuanTriArea/DmMauHoaDon/...`.
  - Controller passthrough thuần — mọi validate/logic nằm ở `IDmMauHoaDonService` (cần xem service nếu trace nghiệp vụ).
