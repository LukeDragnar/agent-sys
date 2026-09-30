---
controller: DMNhomSanPhamController
module: kho
service: WarehouseProduct
man_hinh_ui: "Kho > Nhóm sản phẩm (cấp đơn vị) — CRUD nhóm SP; đồng thời cấp dropdown 'Nhóm sản phẩm' cho nhiều màn (quản lý SP, combo, thu ngân, marketing campaign, máy in...). Bản admin hệ thống = NhomSanPhamController (SandBoxArea), KHÁC node này."
muc_dich: "CRUD nhóm sản phẩm cấp đơn vị + lấy danh sách (kèm số lượng / theo chi nhánh / theo điều kiện) làm nguồn dropdown nhóm SP cho toàn hệ thống; cấp nhóm SP cho Website (private)."
alias: ["nhóm sản phẩm", "DM nhóm sản phẩm", "nhom-san-pham", "dropdown nhóm sản phẩm", "lấy tất cả nhóm sản phẩm"]
auth: mixed                # SBAuthorize(key) cho DV; LayTatCaTheoDieuKien để [SBAuthorize] rỗng; 2 endpoint [PrivateSBAuthorize] cho Website
state_machine: []
deprecated: false
entities: [DMNhomSanPham]
stored_procs: []
depends: [WarehouseProduct, WarehouseReportService (LayTatCaTheoDieuKien)]
cross_module_in: [Website (LayTatCa_Private, LayChiTietNhomSp_Private)]
cross_module_out: []
---

# DMNhomSanPhamController — Nhóm sản phẩm (cấp đơn vị)

> CRUD danh mục + nhiều endpoint "lấy tất cả" làm nguồn dropdown. `partial class`. CUD giới hạn `DV-AD-TONG`; các action đọc mở rộng nhiều role.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy tất cả kèm số lượng | list nhóm SP | POST `api/DMNhomSanPham/LayTatCaKemSoLuong` | `DMNhomSanPham_LayTatCaKemSoLuong` | public | màn list | List nhóm SP + số lượng SP mỗi nhóm | EF | role rộng (gồm cả MKT) |
| 1b | Lấy tất cả kèm số lượng (chi nhánh) | list theo chi nhánh | POST `api/DMNhomSanPham/LayTatCaKemSoLuongChiNhanh` | `DMNhomSanPham_LayTatCaKemSoLuongChiNhanh` | public | - | List nhóm SP + số lượng theo chi nhánh user | EF | - |
| 1c | Lấy tất cả (dropdown) | dropdown nhóm SP | POST `api/DMNhomSanPham/LayTatCa` | `DMNhomSanPham_LayTatCa` | public | - | List toàn bộ nhóm SP (không kèm số lượng) | EF | - |
| 2 | Thêm mới nhóm SP | "Thêm mới" | POST `api/DMNhomSanPham/ThemMoi` | `DMNhomSanPham_ThemMoi` | public | popup Thêm | Insert nhóm SP | EF | - |
| 3 | Lấy chi tiết theo Id | "Sửa" | GET `api/DMNhomSanPham/TimTheoId/{id:Guid}` | `DMNhomSanPham_TimTheoId` | public | per-row | Trả nhóm SP theo id (lọc theo CurrentUser trong service) | EF | - |
| 4 | Cập nhật nhóm SP | "Lưu" | POST `api/DMNhomSanPham/CapNhatThongTin` | `DMNhomSanPham_CapNhatThongTin` | public | popup Sửa | Update theo `CapNhatDMNhomSanPhamDto` | EF | - |
| 5 | Xóa nhóm SP | "Xóa" | POST `api/DMNhomSanPham/Xoa/{id:Guid}` | `DMNhomSanPham_Xoa` | public | per-row | Xóa theo id | EF | - |
| 6 | Lấy tất cả theo điều kiện | (màn báo cáo/lọc) | POST `api/DMNhomSanPham/LayTatCaTheoDieuKien` | `[SBAuthorize]` (rỗng — chỉ đăng nhập) | public | - | List nhóm SP theo `DMNhomSanPhamPayload` — đi qua **WarehouseReportService**, KHÔNG phải service chính | EF (WarehouseReportService) | permission key gốc bị comment (controller:71); chỉ còn `[SBAuthorize]` rỗng |

## Endpoint nội bộ (private / service-to-service — `[PrivateSBAuthorize]`)
- [private] Website gọi `POST LayTatCa_Private` (`LayTatCaPrivateFilterDto`) — đồng bộ list nhóm SP cho website.
- [private] Website gọi `POST LayChiTietNhomSp_Private` (`LayChiTietSpPrivateDto`) — chi tiết 1 nhóm SP.

## Quan hệ
- **Gọi sang:** `IDMNhomSanPhamService` (CRUD + lấy list), `IWarehouseReportService` (chỉ `LayTatCaTheoDieuKien`).
- **Dữ liệu (entities):** `DMNhomSanPham`.
- **Ripple / đồng bộ:** là nguồn dropdown "nhóm sản phẩm" cho rất nhiều màn (quản lý SP, combo spa, thu ngân, máy in, marketing campaign). Xóa/sửa nhóm có thể ảnh hưởng SP đang gắn nhóm — *cần xác nhận* service có chặn xóa khi còn SP.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/DMNhomSanPhamController.cs`
  - L71-78 (`LayTatCaTheoDieuKien`): permission key comment, `[SBAuthorize]` rỗng; nguồn là `WarehouseReportService` (không phải `DMNhomSanPhamService`) — triage lỗi list này phải soi report service.
  - `public partial class` — có thể còn endpoint ở file partial khác; node cover file Controllers/DMNhomSanPhamController.cs. *Cần xác nhận* không có partial bổ sung.
  - Bản admin hệ thống = `Areas/SandBoxArea/NhomSanPhamController.cs` (role `HT-AD`, endpoint `*_SA`) — node riêng.
