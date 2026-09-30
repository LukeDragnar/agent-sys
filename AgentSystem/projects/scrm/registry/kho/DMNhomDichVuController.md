---
controller: DMNhomDichVuController
module: kho
service: WarehouseProduct
man_hinh_ui: "Kho > Nhóm dịch vụ (cấp đơn vị) — CRUD nhóm dịch vụ; đồng thời cấp dropdown 'Nhóm dịch vụ' cho các màn quản lý dịch vụ / thu ngân dịch vụ. Bản admin hệ thống = NhomDichVuController (SandBoxArea), KHÁC node này."
muc_dich: "CRUD nhóm dịch vụ cấp đơn vị + lấy danh sách (kèm số lượng / theo chi nhánh) làm nguồn dropdown nhóm dịch vụ; cấp nhóm dịch vụ cho Website (private)."
alias: ["nhóm dịch vụ", "DM nhóm dịch vụ", "nhom-dich-vu", "dropdown nhóm dịch vụ"]
auth: mixed                # SBAuthorize(key) cho DV; 2 endpoint [PrivateSBAuthorize] cho Website
state_machine: []
deprecated: false
entities: [DMNhomDichVu]
stored_procs: []
depends: [WarehouseProduct]
cross_module_in: [Website (LayTatCa_Private, LayChiTietNhomDv_Private)]
cross_module_out: []
---

# DMNhomDichVuController — Nhóm dịch vụ (cấp đơn vị)

> CRUD danh mục đơn giản (`partial class`). CUD giới hạn `DV-AD-TONG`; action đọc mở rộng nhiều role. Tiêm `INhomDichVuService` (dùng chung interface với SandBoxArea NhomDichVuController).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy tất cả kèm số lượng | list nhóm DV | POST `api/DMNhomDichVu/LayTatCaKemSoLuong` | `DMNhomDichVu_LayTatCaKemSoLuong` | public | màn list | List nhóm DV + số lượng | EF | role rộng (gồm MKT) |
| 1b | Lấy tất cả kèm số lượng (chi nhánh) | list theo chi nhánh | POST `api/DMNhomDichVu/LayTatCaKemSoLuongChiNhanh` | `DMNhomDichVu_LayTatCaKemSoLuongChiNhanh` | public | - | List nhóm DV + số lượng theo chi nhánh | EF | - |
| 2 | Thêm mới nhóm DV | "Thêm mới" | POST `api/DMNhomDichVu/ThemMoi` | `DMNhomDichVu_ThemMoi` | public | popup Thêm | Insert nhóm DV | EF | - |
| 3 | Lấy chi tiết theo Id | "Sửa" | GET `api/DMNhomDichVu/TimTheoId/{id:Guid}` | `DMNhomDichVu_TimTheoId` | public | per-row | Trả nhóm DV theo id (lọc CurrentUser trong service) | EF | - |
| 4 | Cập nhật nhóm DV | "Lưu" | POST `api/DMNhomDichVu/CapNhatThongTin` | `DMNhomDichVu_CapNhatThongTin` | public | popup Sửa | Update theo `CapNhatDMNhomDichVuDto` | EF | - |
| 5 | Xóa nhóm DV | "Xóa" | POST `api/DMNhomDichVu/Xoa/{id:Guid}` | `DMNhomDichVu_Xoa` | public | per-row | Xóa theo id | EF | - |

## Endpoint nội bộ (private / service-to-service — `[PrivateSBAuthorize]`)
- [private] Website gọi `POST LayTatCa_Private` (`LayTatCaPrivateFilterDto`) — đồng bộ list nhóm DV.
- [private] Website gọi `POST LayChiTietNhomDv_Private` (`LayChiTietDvPrivateDto`) — chi tiết 1 nhóm DV.

## Quan hệ
- **Gọi sang:** `INhomDichVuService` (CRUD + lấy list).
- **Dữ liệu (entities):** `DMNhomDichVu`.
- **Ripple / đồng bộ:** nguồn dropdown "nhóm dịch vụ"; xóa/sửa nhóm ảnh hưởng dịch vụ đang gắn nhóm — *cần xác nhận* service có chặn xóa khi còn dịch vụ.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/DMNhomDichVuController.cs`
  - `partial class` — node cover file Controllers/DMNhomDichVuController.cs; *cần xác nhận* không có partial bổ sung.
  - Dùng chung `INhomDichVuService` với `Areas/SandBoxArea/NhomDichVuController.cs` (bản admin `HT-AD`, endpoint `*_SA`) — controller khác, node riêng.
