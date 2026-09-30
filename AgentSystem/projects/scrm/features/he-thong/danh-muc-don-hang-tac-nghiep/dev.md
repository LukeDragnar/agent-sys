# Danh mục đơn hàng & tác nghiệp (gốc — cấp hệ thống) — Kỹ thuật

> Danh mục đơn hàng → service **Order** (area SandBox). Danh mục tác nghiệp → service **Contact** (bản hệ thống QuanTriArea + bản đơn vị). Chỉ dành cho dev.
> Registry: `projects/scrm/registry/cua-hang/DMDonHangController.QuanTri.md`, `projects/scrm/registry/sales/DMTacNghiepController.md`.

## Endpoint / Controller (trace registry)

**Danh mục đơn hàng (QuanTri/SandBox)** — `Order.API/Areas/SandBoxArea/DMDonHangController.cs`, route **`api/SanboxArea/DMDonHang`** (chính tả "Sanbox" thiếu 'd' — verbatim), `[TienIch(..., TI_Chung, true)]` (cờ true = superadmin). Controller-level `[SBAuthorize]` đã comment:
| Hành động | HTTP | Endpoint | Quyền (key) |
|---|---|---|---|
| Tìm theo điều kiện | POST | `.../TimTheoDieuKien` | `DMDonHang_TimTheoDieuKien` |
| Lấy theo Id | GET | `.../TimTheoId/{id:Guid}` | `[SBAuthorize()]` rỗng (login) |
| Thêm mới | POST | `.../ThemMoi` | `DMDonHang_ThemMoi` |
| Cập nhật | POST | `.../CapNhat` | `DMDonHang_CapNhat` |
| Xóa | **GET** | `.../Xoa/{id:Guid}` | `DMDonHang_Xoa` (→ `ClearCache_DMDonHang_HT()`) |

> Bản public chỉ-dropdown ở `Controllers/DMDonHangController.cs` (route `api/DMDonHang`, có `DropDownList`) — KHÁC class trùng tên, đừng nhầm.

**Danh mục tác nghiệp** — `DMTacNghiepController` (service Contact). Hai bản:
- **Bản đơn vị**: `Controllers/DMTacNghiepController.cs`, route `api/DMTacNghiep`, role đơn vị (AD-TONG/CN/MCN…): `InsertAsync`, `UpdateOneAsync`, `TimTheoDieuKienAsync`, `TimTheoDieuKienChiNhanhAsync`, `GetAllDMTacNghiepByChiNhanh`, `DeleteOneItemAsync/{id}`, `TimTheoDieuKienByNganhNghe`, `GetAllPrivate` (PrivateSBAuthorize).
- **Bản hệ thống**: `Areas/SanboxArea/DMTacNghiepController.cs`, route **`api/QuanTriArea/DMTacNghiep`**, `TI_HeThong` isAdmin=true, role **HT-AD, HT-CSKH**; gọi các method `HeThong*` của cùng service. Cùng permission key nhưng khác route + role.

## Entities
- **`DMDonHang`** — master data loại đơn hàng (dùng chung hệ thống); cache `ClearCache_DMDonHang_HT`.
- **`DMTacNghiep`** — `Ten`, `Color`, `IsKhoiDau`, `IsKhoSo`, `KhoSoThoiGian`, `IsSuaGio`; gắn `IdChiNhanh` tùy cờ `IsEnableTacNghiepTheoChiNhanh`.

## Luồng code
- DMDonHang: `IDMDonHangService` (TimTheoDieuKien/GetByIdAsync/Create/Update/DeleteAsync); `ICommonCacheService.ClearCache_DMDonHang_HT` chỉ gọi ở `Xoa`.
- DMTacNghiep: `IDMTacNghiepService` + `GetCauHinhInit` (đọc cờ tác nghiệp theo chi nhánh). Bản hệ thống gọi method `HeThong*`.

## Bug-prone / lưu ý (đã ghi registry — KHÔNG sửa code)
- **DMDonHang `Xoa` qua `[HttpGet]`** (`Areas/SandBoxArea/DMDonHangController.cs:43`) — thao tác xóa bằng GET, rủi ro trigger qua link/crawler.
- **DMDonHang không clear cache ở Thêm/Sửa** — chỉ `Xoa` mới `ClearCache_DMDonHang_HT()`; bản ghi mới/sửa có thể chưa phản ánh tới cache HT cho tới lần clear kế tiếp. (Lưu ý: prompt scan trước từng nói ThemMoi clear cache — code thực tế KHÔNG.)
- **Route misspelling "Sanbox"** (`:14` `[Route("api/SanboxArea/[controller]")]`) — FE `dmdonhang.service.ts` cũng dùng đúng chuỗi sai → đừng sửa lẻ.
- **Trùng class name `DMDonHangController`** (bản SandBox CRUD vs bản public dropdown) — phân biệt bằng route/area.
- **DMTacNghiep — drift quyền** (`DMTacNghiepController.cs:32`): `UpdateOneAsync` cho cả role lead/QL (MKT-LEAD, SALE-LEAD, QL-SALE, QL-MKT) sửa, rộng hơn Insert/Delete (chỉ AD).
- **DMTacNghiep — Authorize rỗng** (`:48`, `:63`): `TimTheoDieuKienChiNhanhAsync` và `GetAllDMTacNghiepByChiNhanh` chỉ cần login (`[SBAuthorize]` rỗng) trong khi list chính cần role → khe hở nếu lộ UI.
- **DMTacNghiep — bản sinh đôi Base** hệ thống (`Areas/SanboxArea/DMTacNghiepController.cs`, `api/QuanTriArea/DMTacNghiep`, HT-AD/HT-CSKH) cùng key nhưng khác route/role → đừng nhầm 2 màn.

## File source (path:line)
- BE DMDonHang (QuanTri): `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Order\Order.API\Areas\SandBoxArea\DMDonHangController.cs:14` (route), `:43` (Xoa HttpGet).
- FE DMDonHang: `...\scrm_frontend\src\app\services\order\dmdonhang.service.ts`; màn `pages/Order/dm-donhang/`.
- BE DMTacNghiep (đơn vị): `...\Contact\...\Controllers\DMTacNghiepController.cs:32` (UpdateOneAsync), `:48`/`:63` (Authorize rỗng).
- BE DMTacNghiep (hệ thống): `...\Contact\...\Areas\SanboxArea\DMTacNghiepController.cs` (route `api/QuanTriArea/DMTacNghiep`).
