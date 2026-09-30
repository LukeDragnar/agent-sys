---
controller: NhomSanPhamController
module: kho
service: WarehouseProduct
man_hinh_ui: "Quản trị hệ thống (SandBox) > Nhóm sản phẩm hệ thống (màn quanly-sanpham-admin / edit-nhomsp-admin) — CRUD nhóm SP cấp hệ thống theo ngành nghề; chỉ HT-AD (super user). KHÁC bản đơn vị DMNhomSanPhamController."
muc_dich: "CRUD nhóm sản phẩm cấp HỆ THỐNG (IdDonVi = Guid.Empty, theo ngành nghề) cho admin hệ thống; cấp dropdown nhóm SP / danh mục SP hệ thống. Là bản '_SA' (SandBox/System Admin) của DMNhomSanPhamController."
alias: ["nhóm sản phẩm hệ thống", "nhóm sản phẩm SA", "NhomSanPham_SA", "quản lý nhóm sản phẩm admin"]
auth: mixed                # toàn bộ [SBAuthorize(key,...,"HT-AD")]; tiện ích TI_HeThong + flag deprecated=true ở attribute
state_machine: []
deprecated: false          # controller còn hoạt động; chỉ TienIchAttribute đặt cờ "true" (xem lưu ý)
entities: [DMNhomSanPham]
stored_procs: []
depends: [WarehouseProduct, CommonService (dropdown)]
cross_module_in: []
cross_module_out: []
---

# NhomSanPhamController (SandBoxArea) — Nhóm sản phẩm hệ thống

> Bản **System Admin** của nhóm SP: route `api/SanboxArea/NhomSanPham`, tiện ích `MaTienIchSCRM.TI_HeThong`, mọi action role `HT-AD`. Thao tác trên dữ liệu hệ thống (`IdDonVi == Guid.Empty`), KHÁC `DMNhomSanPhamController` (cấp đơn vị). Dùng chung `IDMNhomSanPhamService` nhưng gọi method `*_SA`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy tất cả theo ngành nghề | list nhóm SP hệ thống | GET `api/SanboxArea/NhomSanPham/LayTatCa_SA/{idNganhNghe:Guid}` | `NhomSanPham_LayTatCaKemSoLuong` | public | màn list | List nhóm SP hệ thống theo ngành nghề | EF | key đặt tên `_LayTatCaKemSoLuong` nhưng method là `LayTatCa_SA` (lệch tên) |
| 2 | Thêm mới | "Thêm mới" | POST `api/SanboxArea/NhomSanPham/ThemMoi_SA` | `NhomSanPham_ThemMoi_SA` | public | popup Thêm | Insert nhóm SP hệ thống | EF | - |
| 3 | Tìm theo Id | "Sửa" | GET `api/SanboxArea/NhomSanPham/TimTheoId_SA/{id:Guid}` | `NhomSanPham_TimTheoId_SA` | public | per-row | Trả nhóm SP; **guard hệ thống**: chỉ trả khi `IdDonVi == Guid.Empty`, ngược lại lỗi "Không tìm thấy nhóm sản phẩm!" | EF | invariant: chỉ thao tác bản hệ thống (IdDonVi rỗng) |
| 4 | Cập nhật | "Lưu" | POST `api/SanboxArea/NhomSanPham/CapNhat_SA` | `NhomSanPham_CapNhat_SA` | public | popup Sửa | Update nhóm SP hệ thống | EF | - |
| 5 | Xóa | "Xóa" | POST `api/SanboxArea/NhomSanPham/Xoa_SA/{id:Guid}` | `NhomSanPham_Xoa_SA` | public | per-row | Xóa nhóm SP hệ thống | EF | - |
| 6 | Dropdown nhóm SP hệ thống | dropdown | GET `api/SanboxArea/NhomSanPham/DropdownNhomSanPham_SA/{idNganhNghe:Guid}` | `NhomSanPham_DropdownNhomSanPham_SA` | public | - | Dropdown nhóm SP theo ngành nghề (qua **CommonService**) | EF (CommonService) | - |
| 7 | Dropdown DM SP hệ thống theo type | dropdown | GET `api/SanboxArea/NhomSanPham/DropdownDmSanPham_SA/{idNganhNghe:Guid}/{type}` | `NhomSanPham_DropdownDmSanPham_SA_Type` | public | - | Dropdown danh mục SP theo ngành nghề + type (qua CommonService) | EF (CommonService) | - |

## Quan hệ
- **Gọi sang:** `IDMNhomSanPhamService` (method `*_SA`), `ICommonService` (dropdown).
- **Dữ liệu (entities):** `DMNhomSanPham` (bản hệ thống, `IdDonVi == Guid.Empty`).
- **Ripple / đồng bộ:** nhóm SP hệ thống là template cho các đơn vị (khi khởi tạo danh mục mặc định). Đổi bản hệ thống có thể ảnh hưởng đơn vị mới khởi tạo — *cần xác nhận* cơ chế clone.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Areas/SandBoxArea/NhomSanPhamController.cs`
  - `[TienIchAttribute("NhomSanPhamController", ..., MaTienIchSCRM.TI_HeThong, true)]` (controller:14) — tham số cờ cuối `true` (có thể = "ẩn/system"); controller **vẫn hoạt động**, đừng nhầm là deprecated logic.
  - L31 key `NhomSanPham_LayTatCaKemSoLuong` gắn cho method `LayTatCa_SA` (không kèm số lượng) — tên key lệch nội dung.
  - Guard `IdDonVi == Guid.Empty` (TimTheoId_SA L50) phân biệt dữ liệu hệ thống vs đơn vị — giữ nguyên.
  - Constructor format xuống dòng lạ (L21-28) — cosmetic.
