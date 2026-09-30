---
controller: DMSanPhamController
module: kho
service: WarehouseProduct
man_hinh_ui: "Kho > Danh mục sản phẩm (route dm-sanpham, dm-sanpham.component) — list danh mục SP cấp đơn vị, nút Thêm mới + Sửa/Xóa per-row, popup edit (dm-sanpham-edit) + popup khởi tạo mặc định (dm-sanpham-khoitao). Bản admin hệ thống (SUPER_ADMIN) = CÙNG controller name `DMSanPhamController` nhưng nằm ở Areas/SandBoxArea (route api/SanboxArea/DMSanPham, endpoint *_SA, quyền HT-AD) — xem khối SA bên dưới. KHÔNG phải NhomSanPhamController (đó là quản lý nhóm SP, entity khác)."
muc_dich: "CRUD danh mục sản phẩm cấp đơn vị (DV): tìm/thêm/sửa/xóa, khởi tạo bộ danh mục mặc định cho đơn vị, và cấp danh mục SP cho service Website (private)."
alias: ["danh mục sản phẩm", "DM sản phẩm", "dm-sanpham", "loại sản phẩm", "khởi tạo danh mục mặc định"]
auth: mixed                # SBAuthorize(key) cho DV; 1 endpoint [PrivateSBAuthorize] cho Website
state_machine: []
deprecated: false
entities: [DMSanPham]
stored_procs: []
depends: [WarehouseProduct]
cross_module_in: [Website (TimTheoDieuKien_DV_Private)]
cross_module_out: []
---

# DMSanPhamController — Danh mục sản phẩm (cấp đơn vị)

> CRUD danh mục đơn giản. Mọi action giới hạn role `DV-AD-TONG` (admin tổng đơn vị).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid) | "Tìm kiếm" | POST `api/DMSanPham/TimTheoDieuKien_DV` | `DMSanPhamController_TimTheoDieuKien_DV` | public | màn list | List DM SP theo filter trong scope đơn vị | EF | - |
| 2 | Thêm mới DM SP | "Thêm mới" → popup dm-sanpham-edit | POST `api/DMSanPham/ThemMoiDMSanPham_DV` | `DMSanPhamController_ThemMoiDMSanPham_DV` | public | popup Thêm | Map `DMSanPhamCreateDto`→entity, insert | EF | - |
| 3 | Cập nhật DM SP | "Lưu" (popup Sửa) | POST `api/DMSanPham/CapNhatDMSanPham_DV` | `DMSanPhamController_CapNhatDMSanPham_DV` | public | popup Sửa | Update theo model | EF | - |
| 4 | Xóa DM SP | "Xóa" (per-row) | GET `api/DMSanPham/XoaDMSanPham_DV/{id:Guid}` | `DMSanPhamController_XoaDMSanPham_DV` | public | per-row | Xóa theo id | EF | Dùng HTTP **GET** cho thao tác xóa (lệch convention POST của các Phieu) — không đổi (khớp FE) |
| 5 | Khởi tạo danh mục mặc định | nút/flow Khởi tạo (dm-sanpham-khoitao) | POST `api/DMSanPham/KhoiTaoMacDinh` | `DMSanPhamController_KhoiTaoMacDinh` | public | popup khởi tạo | Sinh bộ DM SP mặc định cho đơn vị theo `DMSanPham_KhoiTaoDto` | EF | - |
| 6 | Lấy chi tiết theo Id | mở popup Sửa | GET `api/DMSanPham/TimTheoId_DV/{id:Guid}` | `DMSanPhamController_TimTheoId_DV` | public | per-row | Trả entity map sang `DMSanPhamUpdateDto` | EF | KHÔNG có guard tenant ở controller (khác NhomNhaCungCap/BienBan) — *cần xác nhận* service tự lọc theo đơn vị |

## Hành động (cấp hệ thống / SUPER_ADMIN — Areas/SandBoxArea)
> File `Areas/SandBoxArea/DMSanPhamController.cs` (CÙNG tên controller, KHÁC file): `[TienIch(..., MaTienIchSCRM.TI_HeThong, true)]`, route `api/SanboxArea/DMSanPham`, mọi action quyền `HT-AD`. Đây là bản quản trị danh mục SP toàn hệ thống cho SUPER_ADMIN (không scope đơn vị).

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| SA1 | Tìm theo điều kiện (grid, hệ thống) | "Tìm kiếm" | POST `api/SanboxArea/DMSanPham/TimTheoDieuKien_SA` | `DMSanPhamController_TimTheoDieuKien_SA` (role HT-AD) | public | màn list admin | List DM SP toàn hệ thống theo `DMSanPhamFilterDto` | EF | - |
| SA2 | Thêm mới DM SP (hệ thống) | "Thêm mới" | POST `api/SanboxArea/DMSanPham/ThemMoiDMSanPham_SA` | `DMSanPhamController_ThemMoiDMSanPham_SA` (role HT-AD) | public | popup Thêm | Map `DMSanPhamCreateDto`→entity, insert | EF | - |
| SA3 | Lấy chi tiết theo Id (hệ thống) | mở popup Sửa | GET `api/SanboxArea/DMSanPham/TimTheoId_SA/{id:Guid}` | `DMSanPhamController_TimTheoId_SA` (role HT-AD) | public | per-row | `GetById_SA`, map entity→`DMSanPhamUpdateDto` | EF | - |
| SA4 | Cập nhật DM SP (hệ thống) | "Lưu" (popup Sửa) | POST `api/SanboxArea/DMSanPham/CapNhatDMSanPham_SA` | `DMSanPhamController_CapNhatDMSanPham_SA` (role HT-AD) | public | popup Sửa | Update theo `DMSanPham` model | EF | - |
| SA5 | Xóa DM SP (hệ thống) | "Xóa" (per-row) | GET `api/SanboxArea/DMSanPham/XoaDMSanPham_SA/{id:Guid}` | `DMSanPhamController_XoaDMSanPham_SA` (role HT-AD) | public | per-row | Xóa theo id | EF | Dùng HTTP **GET** cho thao tác xóa (lệch convention POST) — khớp FE, đừng tự đổi |

## Endpoint nội bộ (private / service-to-service)
- [private] Website gọi `POST TimTheoDieuKien_DV_Private` (`DMSanPhamFilterDtoAPI`) — list DM SP cho website.

## Quan hệ
- **Gọi sang:** `IDMSanPhamService` (toàn bộ nghiệp vụ).
- **Dữ liệu (entities):** `DMSanPham` (IdDonVi).
- **Ripple / đồng bộ:** danh mục dùng để phân loại `SanPham`; đổi/xóa DM có thể ảnh hưởng SP đang gắn DM đó (service tự kiểm tra — *cần xác nhận*).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/DMSanPhamController.cs`
  - L57-63 (`XoaDMSanPham_DV`): dùng `[HttpGet]` cho thao tác xóa — lệch convention nhưng khớp FE, đừng tự đổi sang POST.
  - L74-80 (`TimTheoId_DV`): không có guard `IdDonVi == CurrentUser.DonViId` ở controller (khác `NhomNhaCungCapController`/`BienBan*`); guard nằm trong service hay không *cần xác nhận*.
  - Bản admin hệ thống (SUPER_ADMIN) tương ứng = `Areas/SandBoxArea/DMSanPhamController.cs` — **CÙNG tên controller**, route `api/SanboxArea/DMSanPham`, `MaTienIchSCRM.TI_HeThong`, role `HT-AD`, endpoint `*_SA` (xem bảng "Hành động (cấp hệ thống)" ở trên). ĐÍNH CHÍNH: trước đây ghi nhầm là `NhomSanPhamController` — sai. `Areas/SandBoxArea/NhomSanPhamController.cs` là controller KHÁC (quản lý "nhóm sản phẩm hệ thống", entity `DMNhomSanPham`, service `IDMNhomSanPhamService`), không phải bản SA của DMSanPham.
  - Xóa SA (`XoaDMSanPham_SA`, L58-64 SandBoxArea) cũng dùng `[HttpGet]` — giống bản DV, lệch convention POST.
