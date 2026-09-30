---
controller: TaiSanCoDinhController
module: ke-toan
service: Accounting
man_hinh_ui: "Quản trị (SandBox/QuanTriArea) > Danh mục tài sản cố định: lưới list + Thêm/Sửa/Xóa tài sản cố định. Màn quản trị hệ thống (TienIch TI_HeThong)."
muc_dich: "CRUD danh mục TÀI SẢN CỐ ĐỊNH (TaiSanCoDinh) cho khu vực quản trị SandBox: thêm/sửa/xóa/tìm/lấy theo Id tài sản cố định."
alias: ["tài sản cố định", "tai san co dinh", "TSCĐ", "danh mục tài sản cố định", "TSCD SandBox"]
auth: internal
state_machine: []
deprecated: false
entities: [TaiSanCoDinh]
stored_procs: []
depends:
  - ITaiSanCoDinhService
cross_module_in: []
cross_module_out: []
---

# TaiSanCoDinhController — Danh mục tài sản cố định (Area SandBox / Quản trị)

> Controller CRUD thuần trong **Area SandBox** (route `api/QuanTriArea/[controller]`), gắn `[TienIch("Accounting_TaiSanCoDinhController", "Quản lý Tài sản cố định", MaTienIchSCRM.TI_HeThong, true)]`. Mọi action passthrough về `ITaiSanCoDinhService`, hậu tố `_SA`. Toàn bộ logic ở service.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm kiếm / lưới | Lưới list TSCĐ + lọc | `POST api/QuanTriArea/TaiSanCoDinh/TimTheoDieuKien_SA` (TaiSanCoDinhFilterDto) | TienIch `Accounting_TaiSanCoDinhController` (TI_HeThong) | internal | màn quản trị TSCĐ | `TimTheoDieuKien_SA(gridInfo)`. | **EF** TaiSanCoDinh | — |
| 2 | Thêm mới | Nút "Thêm" / Lưu | `POST api/QuanTriArea/TaiSanCoDinh/ThemMoi_SA` (TaiSanCoDinhDto) | TienIch | internal | nút Thêm | `ThemMoi_SA(reqData)`. | **EF** Insert | — |
| 3 | Xem chi tiết | (mở form sửa) | `POST api/QuanTriArea/TaiSanCoDinh/TimTheoId_SA` (Guid id) | TienIch | internal | chọn 1 dòng | `TimTheoId_SA(id)`. | **EF** | — |
| 4 | Cập nhật | Nút "Sửa" / Lưu | `POST api/QuanTriArea/TaiSanCoDinh/CapNhat_SA` (TaiSanCoDinhDto) | TienIch | internal | dòng đã có | `CapNhat_SA(reqData)`. | **EF** Update | — |
| 5 | Xóa | Nút "Xóa" / confirm | `POST api/QuanTriArea/TaiSanCoDinh/Xoa_SA` (Guid? id) | TienIch | internal | mỗi dòng | `Xoa_SA(id)`. | **EF** Delete | — |

## Quan hệ
- **Gọi sang:** `ITaiSanCoDinhService` (toàn bộ logic).
- **Dữ liệu (entities):** `TaiSanCoDinh` (danh mục tài sản cố định).
- **Ripple / đồng bộ:** KHÔNG. Danh mục được `CommonController` (Accounting) đọc tham chiếu (GetAllTaiSanCoDinh / TimTheoDieuKien_TaiSanCoDinh).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Areas/SandBoxArea/TaiSanCoDinhController.cs`
  - L13 `[SBAuthorize]` + L14 `[TienIch(...TI_HeThong...)]`; action `[SBAuthorize()]` trống (L35,48,61,74,87).
  - Route `api/QuanTriArea/[controller]` → endpoint dưới `/api/QuanTriArea/TaiSanCoDinh/...`.
  - Passthrough thuần — logic/validate ở `ITaiSanCoDinhService`.
  - Comment XML constructor (L22) ghi nhầm "DanhMucKeToanBaseController" (copy-paste) — không ảnh hưởng.
