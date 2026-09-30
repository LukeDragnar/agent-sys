# Kiểm kho — Kỹ thuật (CHỈ dev)

> Service: **WarehouseProduct**. Tài liệu kỹ thuật cho dev, KHÔNG đưa khách.
> Tóm tắt nghiệp vụ: [summarize.md](./summarize.md) · [flow.md](./flow.md)

## Màn hình & Route (FE — Angular 15)
Module: `WarehouseProductModule` (lazy, prefix `/warehouse-product`).

| Màn hình | Route | Component |
|---|---|---|
| Quản lý kiểm kho (grid phiếu) | `/warehouse-product/quan-ly-kiem-kho` | `QuanlyKiemkhoComponent` |
| Thêm/sửa phiếu kiểm | (modal) | `PhieuKiemkhoEditComponent` |
| Màn kiểm tra (quét/đếm) | (modal/screen) | `PhieuKiemkhoCheckinfoComponent` |

- Service FE: `src/app/services/warehouseproduct/kiemkho.service.ts` (`KiemkhoService` extends `BaseService`, base url `api/PhieuKiemKho`).
- Component grid: `src/app/pages/WarehouseProduct/quanly-kiemkho/quanly-kiemkho.component.ts` — extends `BaseListComponent`, filter `{ keyWord, idChiNhanh, idKho }`, dùng `TranslateSBService` (đa ngôn ngữ).
- ⚠️ FE `KiemkhoService.XoaPhieuKiem` gọi `POST {table}/XoaPhieuKiem/{id}` nhưng BE expose route **`Xoa/{id}`** → mismatch, kiểm tra lại khi đụng tới chức năng xóa.

## Controller (BE — .NET 6)
`WarehouseProduct/WarehouseProduct.API/Controllers/PhieuKiemKhoController.cs`
Attr nhóm: `[TienIchAttribute("PhieuKiemKhoController", "Phiếu kiểm kho", MaTienIchSCRM.TI_Kho)]`, route `api/PhieuKiemKho`.

| Method | Route | Permission key (`SBAuthorize`) | Service |
|---|---|---|---|
| POST | `TimTheoDieuKien` | `PhieuKiemKho_TimTheoDieuKien` | `TimTheoDieuKienAsync` ← **trace điểm vào** |
| POST | `TaoPhieuKiem` | `PhieuKiemKho_TaoPhieuKiem` | `InsertAsync` |
| POST | `CapNhatPhieuKiem` | `PhieuKiemKho_CapNhatPhieuKiem` | `UpdateAsync` |
| POST | `Xoa/{id}` | `PhieuKiemKho_Xoa` | `DeleteAsync` |
| GET | `ChiTietPhieuKiem/{id}` | `PhieuKiemKho_ChiTietPhieuKiem` | `GetByIdAsync` |
| POST | `GenerateMaPhieuKiem` | `PhieuKiemKho_GenerateMaPhieuKiem` | `GenerateMaPhieuKiemAsync` |
| POST | `CheckProduct` | `[SBAuthorize]` (chỉ login) | `CheckProductAsync` |
| POST | `UpdateSLDaKiem` | `[SBAuthorize]` (chỉ login) | `UpdateSLDaKiemAsync` |
| POST | `LoadGridDanhSachSanPhamKiem` | `PhieuKiemKho_LoadGridDanhSachSanPhamKiem` | `LoadGridDanhSachSanPhamKiemAsync` |
| POST | `KiemTraKetQua` | `PhieuKiemKho_KiemTraKetQua` | `KiemTraKetQuaAsync` |

Roles dùng cho hầu hết endpoint: `DV-AD-TONG, DV-AD-CN, DV-QL-KHO, DV-QL-KHO-TONG, DV-ADMIN-KIEMKHO, DV-NHANVIEN-KIEMKHO`.
- `CheckImei` (route + service) đã **comment out** — luồng quét gộp về `CheckProduct`.

## Entities
`WarehouseProduct/WarehouseProduct.DataContext/Entity/`
- **`PhieuKiemKho`** (: `BaseEntity`) — phiếu. Field chính: `IdDonVi, IdChiNhanh, IdNganhNghe, IdKho, Ma(VARCHAR50), Ten(NVARCHAR500), NgayBatDauKiemKho, NgayKetThucKiemKho, SoLanKiemLaiToiDa, SoLanDaKiem?, KieuLoaiSanPham?(enum), LstIdNguoiKiemKho(VARCHAR4000, csv ';'), LstIdNhanVienPhuTrachKho, LstIdSanPham(VARCHAR8000, csv ';'), SoThuTuTuTang(DatabaseGenerated.Identity)`.
- **`PhieuKiemKhoChiTietSanPham`** — kết quả kiểm theo SP: `IdDonVi, IdChiNhanh, IdKho, IdPhieuKiemKho, IdSanPham, SoLuongDaKiem?(double), TrangThaiKiem(int), IsSanPhamImei?`.
- **`PhieuKiemKhoChiTietImei`** — IMEI đã quét: `... IdPhieuKiemKho, IdSanPham, Imei(NVARCHAR256)`.

Liên quan (read): `Kho`, `KhoChiTiet` (tồn barcode), `SanPham`, `SanPhamImei`, `User`.

### Enums (`Libs/Models.Core/Enum.cs`)
```
EnumPhieuKiemKho_TrangThaiKiem { KiemDu = 1, KiemSai = 2 }   // [Description] "Kiểm đủ"/"Kiểm sai"
EnumPhieuKiemKho_KieuLoaiSanPham { ALL_PRODUCT, OPTIONAL, SPECIFIC_PRODUCT }  // 0,1,2
```
DTOs: `Libs/Models.Core/DTO/WarehouseProduct.Client/PhieuKiemKho/PhieuKiemKhoDto.cs` (Dto, DetailDto, GridFilterDto, GridResDto, GenMaReqDto) và `.../PhieuKiemKhoChiTietImei/PhieuKiemKhoChiTietImeiDto.cs` (CheckProductReqDto, UpdateSLDaKiemReqDto, GridDanhSachSanPhamKiemReqDto, KiemTraKetQuaReqDto, DSKiemTraKetQuaResView, GridDanhSachSanPhamKiemResDto).

## Luồng code

### Trace: `TimTheoDieuKienAsync` (PhieuKiemKho_TimTheoDieuKien)
`PhieuKiemKhoService.TimTheoDieuKienAsync(PhieuKiemKhoGridFilterDto)`:
1. Base query: `_phieuKiemKhoRepos.TableWithNolock()` lọc `IdDonVi == currentUser.DonViId`, `orderby NgayCapNhat desc`.
2. **Phân quyền dữ liệu:** nếu **không** `IsAdminTong()` và **không** `IsChamSocKhachHang()` → chỉ thấy phiếu mà user là `NguoiTaoUserId`, hoặc nằm trong `LstIdNguoiKiemKho`, hoặc `LstIdNhanVienPhuTrachKho` (so khớp bằng `Contains(user.Id.ToString())` — xem Bug-prone).
3. Filter optional: `IdChiNhanh`, `IdKho`, `KeyWord` (Ma/Ten, ToUpper Contains), `ListIdNguoiKiemKho`, `ListIdSanPham`. (Filter theo `NgayBatDau/KetThuc` đang comment.)
4. `total = query.Count()`; paging `Skip/Take` theo `PageInfo`; map → `PhieuKiemKhoGridResDto`.
5. Join bù: chi nhánh (gọi Core `GetAllChiNhanhByCurrentUser` qua `IClientHelper`), kho (`_khoRepos`), user (tên người kiểm/phụ trách), tên SP (`_commonService.GetSanPhamElasticByIds` → Elasticsearch). `LstTenSanPham` set "Tất cả"/"Tùy chọn sản phẩm" theo `KieuLoaiSanPham`.
6. Trả `MethodResult<List<...GridResDto>>.ResultWithData(datas, "", total)`.

### Tạo/sửa/xóa
- `InsertAsync`: check trùng `Ma`/`Ten` (per DonVi) → map → set `IdDonVi`, `IdNganhNghe` từ currentUser → `base.InsertAsync` → `ReloadFromDBToCacheSTTTuTang` (refresh Redis max STT).
- `UpdateAsync`: check trùng (loại trừ chính nó) → map → `MarkDirty` từng field cho phép sửa → `base.UpdateAsync`. **Không** MarkDirty `Ma` (mã cố định sau tạo).
- `DeleteAsync`: soft delete (`IsDelete=true` + audit), tự xử lý update (vì cột `SoThuTuTuTang` là Identity nên không gọi delete của base) + xóa chi tiết SP/IMEI trong transaction (`ExecutionStrategy` + `BeginTransaction`).
- `GenerateMaPhieuKiemAsync`: lấy max `SoThuTuTuTang` qua Redis lock (`GetMaxSTTTuTangTheoDonVi`, retry 20×300ms, key `CacheKey.PhieuKiemKho_SoThuTuTuTang`), `Ma = {TenKho bỏ dấu, viết hoa, bỏ space}-{ddMMyyyy}-{stt+1}`, check trùng lần nữa.

### Quét/đếm
- `CheckProductAsync(CheckProductReqDto{IdPhieuKiemKho, KeyGen})`: validate phiếu tồn tại + trong khoảng ngày. Thử khớp `KeyGen` vào **SanPhamImei** (đúng DonVi/ChiNhanh/Kho, `TrangThai==DaNhapKho`); nếu là IMEI → check SPECIFIC_PRODUCT, check IMEI đã kiểm chưa, upsert `ChiTietSanPham.SoLuongDaKiem += 1` + insert `ChiTietImei` (transaction). Ngược lại khớp **Barcode** trong `SanPham`+`KhoChiTiet` → upsert ChiTietSanPham. Trả `{IdSanPham, SoLuongDaKiem, TenSanPham, IsSanPhamImei}`.
- `UpdateSLDaKiemAsync`: nhập tay số lượng đã kiểm cho 1 SP (upsert `ChiTietSanPham.SoLuongDaKiem = reqDto.SoLuongDaKiem`). Validate trong khoảng ngày.
- `LoadGridDanhSachSanPhamKiemAsync`: nếu `SPECIFIC_PRODUCT` → list từ `KhoChiTiet⋈SanPham` lọc theo `LstIdSanPham`; else → toàn bộ tồn của kho. Left-join với chi tiết đã kiểm; cờ hiển thị `TrangThaiKiemValue` khi phiếu hết hạn hoặc `SoLuongDaKiem>0`. Trả kèm `IsHetLuotKiem = SoLanKiemLaiToiDa <= SoLanDaKiem`, `SoLanDaKiem`, `SoLanKiemConLai`.
- `KiemTraKetQuaAsync`: lấy đã kiểm (`ChiTietSanPham`), tồn IMEI (group `SanPhamImei` DaNhapKho), tồn barcode (`SanPham⋈KhoChiTiet`, `IsSanPhamImei != true`). Với mỗi món: `GetTrangThaiKiem(SoLuongDaKiem, SoLuongDaNhap)` → `KiemDu` nếu bằng, else `KiemSai`. Upsert TrangThaiKiem + SoLuongDaKiem, **`SoLanDaKiem += 1`** (transaction). Trả `IsHetLuotKiem` (so sánh `==`), `SoLanDaKiem`, `SoLanKiemConLai`.

## Bug-prone / lưu ý khi sửa
- **Phân quyền bằng `string.Contains` trên cột CSV** (`LstIdNguoiKiemKho.Contains(user.Id.ToString())`) → nguy cơ false-positive nếu một GUID là substring của GUID khác (hiếm nhưng có với prefix). Áp dụng cho cả filter và phân quyền dữ liệu.
- **`IsHetLuotKiem` không nhất quán:** `LoadGrid...` dùng `SoLanKiemLaiToiDa <= SoLanDaKiem`, còn `KiemTraKetQua` trả về dùng `==`. Khi `SoLanDaKiem` vượt (>) thì 2 chỗ lệch nhau — cẩn thận khi sửa logic lượt kiểm.
- **Kiểm kho KHÔNG đụng tồn kho thật** (`KhoChiTiet`/`SanPhamImei`) — chỉ ghi `ChiTietSanPham`/`ChiTietImei`. Đừng nhầm là nó tự điều chỉnh tồn.
- **`SoThuTuTuTang` là Identity** → không update được; vì vậy `DeleteAsync` tự viết update soft-delete thay vì gọi base delete. Giữ nguyên pattern này nếu refactor.
- **`GetMaxSTTTuTangTheoDonVi`** có `throw ex;` (mất stack) + dòng `Console.WriteLine` unreachable sau throw — code smell, không sửa khi chỉ viết docs.
- **Route xóa lệch FE/BE**: BE `Xoa/{id}` vs FE `XoaPhieuKiem/{id}` (xem mục Màn hình).
- Validate thời gian dùng `DateTime.Now` (giờ server) cho cả 4 thao tác quét/đếm/kiểm — lệ thuộc timezone server.

## File source
- BE Controller: `WarehouseProduct/WarehouseProduct.API/Controllers/PhieuKiemKhoController.cs`
- BE Service: `WarehouseProduct/WarehouseProduct.Service/Generated/PhieuKiemKhoService.cs`
- BE Entities: `WarehouseProduct/WarehouseProduct.DataContext/Entity/PhieuKiemKho.cs`, `PhieuKiemKhoChiTietSanPham.cs`, `PhieuKiemKhoChiTietImei.cs`
- BE DTO: `Libs/Models.Core/DTO/WarehouseProduct.Client/PhieuKiemKho/PhieuKiemKhoDto.cs`, `.../PhieuKiemKhoChiTietImei/PhieuKiemKhoChiTietImeiDto.cs`
- BE Enum: `Libs/Models.Core/Enum.cs` (EnumPhieuKiemKho_TrangThaiKiem, EnumPhieuKiemKho_KieuLoaiSanPham)
- FE Service: `src/app/services/warehouseproduct/kiemkho.service.ts`
- FE Components: `src/app/pages/WarehouseProduct/quanly-kiemkho/quanly-kiemkho.component.ts`, `phieu-kiemkho-edit/`, `phieu-kiemkho-checkinfo/`
- FE Route: `src/app/pages/WarehouseProduct/warehouseProduct-routing.module.ts` (`quan-ly-kiem-kho`), `src/app/app-routing.module.ts` (prefix `warehouse-product`)
