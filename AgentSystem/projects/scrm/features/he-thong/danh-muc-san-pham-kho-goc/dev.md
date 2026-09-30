# Danh mục sản phẩm & kho (gốc — cấp hệ thống) — Kỹ thuật

> Service: **WarehouseProduct** (BE .NET). Controller cấp hệ thống nằm trong `Areas/SandBoxArea`, route `api/SanboxArea/...`, role **HT-AD**, tiện ích `TI_HeThong`. Chỉ dành cho dev.
> Registry liên quan: `projects/scrm/registry/kho/SanPhamController.md` (mục "Lỗi đã biết" có nhắc bản SandBoxArea), `projects/scrm/registry/kho/DMSanPhamController.md`, `projects/scrm/registry/kho/DMKhoController.md` (lưu ý: node này chỉ document bản STUB).

## Endpoint / Controller (trace source SandBoxArea)

**Sản phẩm hệ thống** — `Areas/SandBoxArea/SanPhamController.cs`, route `api/SanboxArea/SanPham`, TI `Quản lý sản phẩm hệ thống`:
| Hành động | HTTP | Route | Quyền (key) | path:line |
|---|---|---|---|---|
| Tìm theo điều kiện | POST | `TimTheoDieuKien_SA` | `SanPham_TimTheoDieuKien_SA` (HT-AD) | `:29-35` |
| Thêm mới | POST | `ThemMoi_SA` | `SanPham_ThemMoi_SA` | `:37-43` |
| Lấy theo Id | GET | `TimTheoId_SA/{id:Guid}` | `SanPham_TimTheoId_SA` | `:44-50` |
| Cập nhật | POST | `CapNhat_SA` | `SanPham_CapNhat_SA` | `:52-58` |
| Xóa | POST | `Xoa_SA/{id:Guid}` | `SanPham_Xoa_SA` | `:59-65` |
| Dropdown SP theo ngành | GET | `DropdownSanPham_SA/{idNganhNghe:Guid}` | `SanPham_DropdownSanPham_SA` | `:68-74` |

**Nhóm sản phẩm hệ thống** — `Areas/SandBoxArea/NhomSanPhamController.cs`, route `api/SanboxArea/NhomSanPham`:
| Hành động | HTTP | Route | Quyền (key) | path:line |
|---|---|---|---|---|
| Lấy tất cả kèm số lượng | GET | `LayTatCa_SA/{idNganhNghe:Guid}` | `NhomSanPham_LayTatCaKemSoLuong` | `:31-37` |
| Thêm mới | POST | `ThemMoi_SA` | `NhomSanPham_ThemMoi_SA` | `:38-44` |
| Lấy theo Id | GET | `TimTheoId_SA/{id:Guid}` | `NhomSanPham_TimTheoId_SA` | `:45-53` |
| Cập nhật | POST | `CapNhat_SA` | `NhomSanPham_CapNhat_SA` | `:54-60` |
| Xóa | POST | `Xoa_SA/{id:Guid}` | `NhomSanPham_Xoa_SA` | `:61-67` |
| Dropdown nhóm SP | GET | `DropdownNhomSanPham_SA/{idNganhNghe:Guid}` | `NhomSanPham_DropdownNhomSanPham_SA` | `:69-74` |
| Dropdown DM SP theo type | GET | `DropdownDmSanPham_SA/{idNganhNghe:Guid}/{type}` | `NhomSanPham_DropdownDmSanPham_SA_Type` | `:77-82` |

**Danh mục sản phẩm hệ thống (DMSanPham)** — `Areas/SandBoxArea/DMSanPhamController.cs`, route `api/SanboxArea/DMSanPham`:
| Hành động | HTTP | Route | Quyền (key) | path:line |
|---|---|---|---|---|
| Tìm theo điều kiện | POST | `TimTheoDieuKien_SA` | `DMSanPhamController_TimTheoDieuKien_SA` | `:24-30` |
| Thêm mới | POST | `ThemMoiDMSanPham_SA` | `DMSanPhamController_ThemMoiDMSanPham_SA` | `:32-39` |
| Lấy theo Id | GET | `TimTheoId_SA/{id:Guid}` | `DMSanPhamController_TimTheoId_SA` | `:41-47` |
| Cập nhật | POST | `CapNhatDMSanPham_SA` | `DMSanPhamController_CapNhatDMSanPham_SA` | `:50-56` |
| Xóa | **GET** | `XoaDMSanPham_SA/{id:Guid}` | `DMSanPhamController_XoaDMSanPham_SA` | `:58-64` |

**Dịch vụ hệ thống** — `Areas/SandBoxArea/DichVuController.cs`, route `api/SanboxArea/DichVu`: `TimTheoDieuKien_SA` `:22-28`, `ThemMoi_SA` `:29-35`, `TimTheoId_SA` `:36-42`, `CapNhat_SA` `:43-49`, `Xoa_SA` `:50-56` (key `DichVu_*_SA`, HT-AD).

**Nhóm dịch vụ hệ thống** — `Areas/SandBoxArea/NhomDichVuController.cs`, route `api/SanboxArea/NhomDichVu`: `LayTatCa_SA/{idNganhNghe}` `:30-36`, `ThemMoi_SA` `:37-43`, `LayTheoId_SA/{id}` `:44-52`, `CapNhat_SA` `:53-59`, `Xoa_SA/{id}` `:60-66`, `DropdownNhomDichVu_SA/{idNganhNghe}` `:68-74`, `LayTatCa_Private` (PrivateSBAuthorize, cho Website) `:76-82`.

**Danh mục kho hệ thống (DMKho)** — `Areas/SandBoxArea/DMKhoController.cs`, route `api/SanboxArea/DMKho`, TI `Quản lý danh mục kho`:
| Hành động | HTTP | Route | Quyền (key) | path:line |
|---|---|---|---|---|
| Tìm theo điều kiện | POST | `TimTheoDieuKien` | `DMKhoController_TimTheoDieuKien` | `:25-31` |
| Thêm mới | POST | `ThemMoi` | `DMKhoController_ThemMoi` | `:33-40` |
| Lấy theo Id | GET | `TimTheoId/{id:Guid}` | `DMKhoController_TimTheoId` | `:42-48` |
| Cập nhật | POST | `CapNhat` | `DMKhoController_CapNhat` | `:51-57` |
| Xóa | **GET** | `Xoa/{id:Guid}` | `DMKhoController_Xoa` | `:59-65` |

## Entities
- `SanPham`, `NhomSanPham`, `DMSanPham`, `DichVu`, `NhomDichVu`, `DMKho` — đều ở cấp hệ thống, lọc theo `idNganhNghe` ở các màn liên quan.

## Bug-prone / lưu ý
- **DMKho — HAI controller trùng tên**: `WarehouseProduct.API/Controllers/DMKhoController.cs` là **STUB rỗng** (constructor inject `IDMKhoService`, `[TienIch]` bị comment, KHÔNG endpoint — xem registry `kho/DMKhoController.md`). Bản **thật** có CRUD đầy đủ là `Areas/SandBoxArea/DMKhoController.cs`. → Registry node `DMKhoController.md` (đang nói "stub, chưa có endpoint") **thiếu** bản SandBoxArea; cần bổ sung node cho bản admin khi quét lại. *Đã đối chiếu source 2026-06-20.*
- **Xóa DMSanPham_SA và DMKho/Xoa dùng HTTP GET** — thao tác phá hủy qua GET, lệch convention REST; rủi ro bị trigger qua link/crawler. Khớp FE, đừng tự đổi.
- **Trùng tên class** với controller cấp đơn vị (`SanPhamController`, `DMSanPhamController`, `DMKhoController` đều có bản non-SA ở `Controllers/`) → grep theo tên class dễ nhầm. Phân biệt bằng area/route (`api/SanboxArea/...`).
- **Combo / Thẻ dịch vụ**: không có controller SandBoxArea → không có danh mục gốc cấp hệ thống. *Cần xác nhận* có chủ ý không.

## File source (path:line)
- `D:\PROJECT\Git\_SourceForCode\Group2\scrm\WarehouseProduct\WarehouseProduct.API\Areas\SandBoxArea\SanPhamController.cs:29` (TimTheoDieuKien_SA) … `:68` (Dropdown).
- `...\Areas\SandBoxArea\NhomSanPhamController.cs:31` … `:82`.
- `...\Areas\SandBoxArea\DMSanPhamController.cs:24` … `:64`.
- `...\Areas\SandBoxArea\DichVuController.cs:22` … `:56`.
- `...\Areas\SandBoxArea\NhomDichVuController.cs:30` … `:82`.
- `...\Areas\SandBoxArea\DMKhoController.cs:25` … `:65`.
- Stub (đừng dùng): `...\WarehouseProduct.API\Controllers\DMKhoController.cs` (23 dòng, không endpoint).
