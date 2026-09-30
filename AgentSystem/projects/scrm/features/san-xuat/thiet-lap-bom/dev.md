# Dev — Thiết lập BOM (định mức)

> Service: **SanXuat** (`SanXuat.API` / `SanXuat.Service`) · FE module **sanxuat/bomsetup**. Chỉ dành cho dev.
> Trace registry: `registry/san-xuat/BomSetupController.md`, `registry/san-xuat/DmCongDoanController.md`.

## Màn hình & route (FE)
| Màn hình | Route | Component / folder |
|---|---|---|
| Thiết lập BOM (list + quy trình) | `/sanxuat/lenh-bomsetup` | `bomsetup/bomsetup.component` |
| Sửa BOM (header) | (trong module bomsetup) | `bomsetup/bomsetup-edit/bomsetup-edit.component` |

- Route con: `sanxuat-routing.module.ts:27` (`lenh-bomsetup` → `BomSetupModule`).
- BOM list + quy trình + bảng Nguyên vật liệu/Kết quả ở 1 màn (`bomsetup.component.html`): danh sách BOM (edit/copy/delete inline), kéo–thả bước (`cdkDropList`), popover chọn sản phẩm/đơn vị/kho theo dòng.

## Endpoint / Controller (trace registry)
**BOM** — `BomSetupController` (`[ApiController]`):
| Endpoint | Quyền | Hành vi |
|---|---|---|
| `POST api/BomSetup/ThemMoi` | `BomSetup_ThemMoi` (AD-TONG/AD-CN/SANXUAT) | map DTO→entity, set org/branch; IsMacDinh → clear cờ BOM khác cùng SP; InsertAsync |
| `POST .../CapNhatThongTin` | `_CapNhatThongTin` | GetById null → "Không tìm thấy"; **chặn nếu IdDonVi≠user.DonViId hoặc IdChiNhanh≠user.IdChiNhanh** (NotFound); UpdateAsync |
| `POST .../Xoa/{id}` | `_Xoa` | GetById null → "Không tìm thấy"; DeleteAsync. **Không tenant guard** |
| `POST .../TimTheoDieuKien` · `GET .../TimTheoId/{id}` | `_TimTheoDieuKien` / `_TimTheoId` | list / chi tiết |
| `POST .../CloneBomSetup/{id}` | `[SBAuthorize]` rỗng (chỉ đăng nhập) | deep-copy BomSetup+BomStep+BomProduct, remap stepId, mã mới, tên "- Copy". **Không tenant guard** |
| `POST .../CapNhatQuyTrinh` | `_CapNhatQuyTrinh` | **delete-all BomStep+BomProduct rồi insert lại** (NVL Loai=0 / kết quả Loai=1); transaction |

**Công đoạn** — `DmCongDoanController` (`[ApiController]`): `ThemMoi`/`CapNhatThongTin`/`Xoa`/`TimTheoId`/`TimTheoDieuKien` cho `DmCongDoan {Ma, Ten}`. **Toàn bộ permission key bị comment** (controller L31/43/61/87) → chỉ cần đăng nhập; không tenant guard.

## Entities
- `BomSetup {IdSanPham, SoLuongTheoCongThuc, KichThuoc, IsMacDinh, IdDonViTinh, IdDonVi, IdChiNhanh}`.
- `BomStep {ThuTu, Ten, thoiGianCho*, thoiGianHoanThanh*}`.
- `BomProduct {Loai(0=NVL đầu vào / 1=kết quả đầu ra), IsSanPhamChinh, IsBanThanhPham, SoLuong (định mức), IdSanPham, IdKho, IdDonViTinh, phanTramSaiLech}`.
- `DmCongDoan {Ma, Ten}` (master data, liên kết lỏng với BomStep theo Ten/Ma).

## Luồng code
- `ThemMoi`/IsMacDinh clear logic: `BomSetupService.cs:45/:50`.
- Tenant guard (sửa): controller `BomSetupController.cs` L78-85.
- `CloneBomSetup`: deep-copy + remap (`:58`); key thật comment (controller L135).
- `CapNhatQuyTrinh` (replace-all): `:300` (xóa+insert `:315-382`).
- `IWarehouseProductService` inject nhưng phần dùng bị **comment** (`BomSetupService.cs:232`).
- Là nguồn cho `WorkOrderController.ChuyenWOPlanner` (scale `SoLuong/SoLuongTheoCongThuc`) và `SaleOrderProductionController` (LotSize/KichThuoc).

## Bug-prone / lưu ý (ghi nhận, KHÔNG sửa — để session security)
- **Tenant guard chỉ ở `CapNhatThongTin`**; `Xoa` và `CloneBomSetup` **thiếu guard** đơn vị/chi nhánh → rủi ro thao tác chéo tenant.
- **`CloneBomSetup` để `[SBAuthorize]` rỗng** (key thật comment) → chỉ cần đăng nhập là clone được BOM bất kỳ.
- **`DmCongDoanController` comment toàn bộ permission key** → bất kỳ ai đăng nhập đều CRUD công đoạn được; không tenant guard.
- **`CapNhatQuyTrinh` = replace-all**: chạy lại tạo step/product Id mới hoàn toàn → mọi tham chiếu Id cũ gãy; WO đã vật chất hóa không tự đồng bộ.
- **Liên kết DmCongDoan ↔ BomStep lỏng** (theo Ten/Ma, không FK) → đổi tên công đoạn có thể không đồng bộ với bước đã đặt.

## File source (path:line)
- BE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\SanXuat\SanXuat.API\Controllers\BomSetupController.cs` (CapNhatThongTin guard L78-85, Clone L135), `DmCongDoanController.cs` (key comment L31/43/61/87).
- BE service: `...\SanXuat\SanXuat.Service\BomSetupService.cs` (:45/:50/:58/:232/:300/:315-382), `DmCongDoanService.cs` (:31/:41).
- FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\sanxuat\bomsetup\` (`bomsetup.component.html/.ts`, `bomsetup-routing.module.ts:27`, `bomsetup-edit/`).
