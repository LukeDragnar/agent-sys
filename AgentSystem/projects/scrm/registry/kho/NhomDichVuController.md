---
controller: NhomDichVuController
module: kho
service: WarehouseProduct
man_hinh_ui: "Quản trị hệ thống (SandBox) > Nhóm dịch vụ hệ thống — CRUD nhóm dịch vụ cấp hệ thống theo ngành nghề; chỉ HT-AD (super user). KHÁC bản đơn vị DMNhomDichVuController."
muc_dich: "CRUD nhóm dịch vụ cấp HỆ THỐNG (IdDonVi = Guid.Empty, theo ngành nghề) cho admin hệ thống; cấp dropdown nhóm DV hệ thống; cấp list nhóm DV cho Website (private). Là bản '_SA' của DMNhomDichVuController."
alias: ["nhóm dịch vụ hệ thống", "nhóm dịch vụ SA", "NhomDichVu_SA", "quản lý nhóm dịch vụ admin"]
auth: mixed                # phần lớn [SBAuthorize(key,...,"HT-AD")]; 1 endpoint [PrivateSBAuthorize] cho Website
state_machine: []
deprecated: false
entities: [DMNhomDichVu]
stored_procs: []
depends: [WarehouseProduct, CommonService (dropdown)]
cross_module_in: [Website (LayTatCa_Private)]
cross_module_out: []
---

# NhomDichVuController (SandBoxArea) — Nhóm dịch vụ hệ thống

> Bản **System Admin** của nhóm dịch vụ: route `api/SanboxArea/NhomDichVu`, tiện ích `MaTienIchSCRM.TI_HeThong`, action role `HT-AD`. Thao tác dữ liệu hệ thống (`IdDonVi == Guid.Empty`), KHÁC `DMNhomDichVuController` (cấp đơn vị). Dùng chung `INhomDichVuService`, gọi method `*_SA`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy tất cả theo ngành nghề | list nhóm DV hệ thống | GET `api/SanboxArea/NhomDichVu/LayTatCa_SA/{idNganhNghe:Guid}` | `NhomDichVu_LayTatCa_SA` | public | màn list | List nhóm DV hệ thống theo ngành nghề | EF | - |
| 2 | Thêm mới | "Thêm mới" | POST `api/SanboxArea/NhomDichVu/ThemMoi_SA` | `NhomDichVu_ThemMoi_SA` | public | popup Thêm | Insert nhóm DV hệ thống | EF | - |
| 3 | Lấy theo Id | "Sửa" | GET `api/SanboxArea/NhomDichVu/LayTheoId_SA/{id:Guid}` | `NhomDichVu_LayTheoId_SA` | public | per-row | Trả nhóm DV; **guard hệ thống**: chỉ trả khi `IdDonVi == Guid.Empty`, ngược lại lỗi (message copy nhầm: "Không tìm thấy nhóm sản phẩm!") | EF | message lỗi ghi "nhóm sản phẩm" thay vì "nhóm dịch vụ" (copy-paste, controller:51) |
| 4 | Cập nhật | "Lưu" | POST `api/SanboxArea/NhomDichVu/CapNhat_SA` | `NhomDichVu_CapNhat_SA` | public | popup Sửa | Update nhóm DV hệ thống | EF | - |
| 5 | Xóa | "Xóa" | POST `api/SanboxArea/NhomDichVu/Xoa_SA/{id:Guid}` | `NhomDichVu_Xoa_SA` | public | per-row | Xóa nhóm DV hệ thống | EF | - |
| 6 | Dropdown nhóm DV hệ thống | dropdown | GET `api/SanboxArea/NhomDichVu/DropdownNhomDichVu_SA/{idNganhNghe:Guid}` | `NhomDichVu_DropdownNhomDichVu_SA` | public | - | Dropdown nhóm DV theo ngành nghề (qua **CommonService**) | EF (CommonService) | - |

## Endpoint nội bộ (private / service-to-service — `[PrivateSBAuthorize]`)
- [private] Website gọi `POST LayTatCa_Private` (`LayTatCaPrivateFilterDto`) — đồng bộ list nhóm DV cho website.

## Quan hệ
- **Gọi sang:** `INhomDichVuService` (method `*_SA` + `LayTatCa_Private`), `ICommonService` (dropdown).
- **Dữ liệu (entities):** `DMNhomDichVu` (bản hệ thống, `IdDonVi == Guid.Empty`).
- **Ripple / đồng bộ:** nhóm DV hệ thống là template cho đơn vị khi khởi tạo danh mục — *cần xác nhận* cơ chế clone.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Areas/SandBoxArea/NhomDichVuController.cs`
  - L51 message lỗi `LayTheoId_SA` ghi "Không tìm thấy nhóm sản phẩm!" (đúng phải "nhóm dịch vụ") — chỉ label, không ảnh hưởng logic.
  - Guard `IdDonVi == Guid.Empty` phân biệt dữ liệu hệ thống vs đơn vị — giữ nguyên.
  - `[TienIchAttribute(..., MaTienIchSCRM.TI_HeThong, true)]` (controller:15) — cờ cuối `true`; controller vẫn hoạt động.
