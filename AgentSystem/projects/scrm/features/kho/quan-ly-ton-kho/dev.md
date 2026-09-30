# Dev — Quản lý tồn kho (CHỈ dev)

> Service: **WarehouseProduct**. MaTienIch: `MaTienIchSCRM.TI_Kho`.
> Path BE: `WarehouseProduct/` · Path FE: `scrm_frontend/src/app/pages/WarehouseProduct/`

## Màn hình & route (FE)
File routing: `src/app/pages/WarehouseProduct/warehouseProduct-routing.module.ts`

| Màn hình | Route path | Component | Dòng routing |
|---|---|---|---|
| Quản lý tồn kho | `quan-ly-ton-kho` | `QlTonKhoComponent` (`quanly-kho/ql-ton-kho/`) | :169-170 |
| Kiểm tra tồn kho (hệ thống) | `kiemtra-tonkho-hethong` | `KiemtraTonkhoComponent` (`quanly-kho/kiemtra-tonkho/`) | :213-214 |
| — view theo chi nhánh | — | `kiemtra-tonkho-chinhanh.component.ts` | — |
| Danh sách kho | `kho` | `KhoComponent` (`kho/kho.component.ts`) | :125-126 |
| Danh mục kho (dm) | `dm-kho` | `DmKhoComponent` (`dm-kho/`) | :121-122 |
| Cấu hình kho | `config-warehouse-product`* | `ConfigWarehouseProductComponent` | :43, :210 |
| Cấu hình kho chi nhánh | nhúng trong `kho/kho-edit/` | dùng `CauHinhKhoChiNhanhService` | kho-edit.component.ts:14,75 |

> *Lưu ý: route của màn cấu hình bị viết qua thư mục alias `'./n/n.component'` ở dòng import — xác nhận lại path thực `config-warehouse-product/config-warehouse-product.component.ts` khi sửa.

FE service: `src/app/services/warehouseproduct/khoChiTiet.service.ts`, `kho.service.ts`, `cau-hinh.service.ts` (`CauHinhKhoService`), `cau-hinh-kho-chi-nhanh.service.ts`. Base URL: `${environment.SERVICE_API_WarehouseProduct}api/...`.

## MaTinhNang (SBAuthorize) / Controller

### KhoChiTietController — `WarehouseProduct.API/Controllers/KhoChiTietController.cs`
| MaTinhNang | HTTP | Method service | Vai trò |
|---|---|---|---|
| `KhoChiTiet_TimTheoDieuKien` | POST `TimTheoDieuKien` | `TimTheoDieuKienAsync` :29 | AD-TONG/CN/MCN, KT, QLDV, QL-KHO... |
| `KhoChiTiet_ExportExcelKhoChiTiet` | POST `ExportExcelKhoChiTiet` | `XuatExcelQuanLyTonKho` :37 | như trên |
| `KhoChiTiet_GetTonKhoSanPham_ViewChiNhanh` | POST `GetTonKhoSanPham_ViewChiNhanh` | `GetTonKhoSanPham_ViewChiNhanh` :80 | +SALE, NV |
| (cùng) `_DuBaoHetHang` | POST `..._DuBaoHetHang` | set `IsDuBaoHetHang=true` :88 | — |
| `KhoChiTiet_ReportTonKhoDonVi` | POST `ReportTonKhoDonVi` | `ReportTonKhoDonVi` :121 | AD + QL-KHO |
| `KhoChiTiet_ExportExcelReportTonKhoDonVi` | POST `ExportExcelReportTonKhoDonVi` | :150 | AD only |

### KhoController — `WarehouseProduct.API/Controllers/KhoController.cs`  (class-level `[SBAuthorize()]`)
| MaTinhNang | HTTP | Method | Vai trò |
|---|---|---|---|
| `Kho_TimTheoDieuKien` | POST `TimTheoDieuKien` | `TimTheoDieuKienAsync` :41 | AD-TONG/CN/MCN, KT, QLDV |
| `Kho_ThemMoi` | POST `ThemMoi` | `ThemMoiAsync` :54 | AD only |
| `Kho_CapNhatThongTin` | POST `CapNhatThongTin` | `Update` :98 | AD only |
| `Kho_Xoa` | POST `Xoa/{id}` | `DeleteAsync` :116 | AD only |
| `Kho_GetAllKhoByUserLogin` | GET | :141 | AD, KT, QLDV, kiểm kho |
| `Kho_GetListUserQuanKhoByKhoId` | POST | :207 | nhiều role |

### CauHinhController — `WarehouseProduct.API/Controllers/CauHinhController.cs`
`[TienIch("CauHinh_Warehouse_Controller", ..., TI_Kho)]`, base `SBBaseController<CauHinh, CauHinh>`.
| MaTinhNang | HTTP | Method |
|---|---|---|
| `CauHinh_Warehouse_LuuCauHinhKhachHang` | POST `SaveCauHinhInit` | `SaveCauHinhInit` :28 |
| (anon) | GET `GetCauHinhInit` / `Get` | `GetCauHinhInit` / `GetAsync` :41/:51 |
| `CauHinh_Warehouse_LuuCauHinhWareHouseSuperAdmin` | POST `Save_SuperAdmin` | :75 (role HT-AD/HT-CSKH) |
| `CauHinh_Warehouse_SaveCauHinhSuperAdminManCauHinhDonVi` | POST | :93 |
| `CauHinh_Warehouse_SaveCauHinhGioiHanTonKho` | POST `SaveCauHinhGioiHanTonKho` | :173 |
| (auth) | POST `SaveCauHinhQLSPInit` / GET `GetCauHinhQLSPInit` | :154/:166 |

> Trace yêu cầu `CauHinh_Warehouse_LuuCauHinhKho` không tồn tại nguyên văn; màn "Cấu hình kho" map vào nhóm `CauHinh_Warehouse_*` ở trên (đặc biệt `SaveCauHinhInit` / `SaveCauHinhGioiHanTonKho` / `SaveCauHinhQLSPInit`).

### CauHinhKhoChiNhanhController — `CauHinhKhoChiNhanhController.cs`
`[TienIch("CauHinhKhoChiNhanhController", ..., TI_Kho)]`
| MaTinhNang | HTTP | Method |
|---|---|---|
| `CauHinhKhoChiNhanhController_UpdateOrCreate` | POST `UpdateOrCreate` | :22 (AD-TONG/CN/MCN) |
| `CauHinhKhoChiNhanhController_GetById` | POST `GetById` | :34 |
| (auth) | POST `GetAll` | :47 |

## Entities — `WarehouseProduct.DataContext/Entity/`
- **`KhoChiTiet`** (`KhoChiTiet.cs`): khóa logic `IdChiNhanh + IdSanPham (+IdKho)`. Fields: `SoLuong`, `SoLuongChoXuat`, `GiaNhapTrungBinh`, `IdSanPhamCha`, `SoLuongDungTichChiTiet`, `IdDungTich`. **1 record = 1 SP trong 1 kho.**
- **`Kho`**: `IsMacDinh`, `IsChoPhepChotAm`, `IsAnKhoDoiVoiSale`, `UserQuanKho` (JSON), `IsUseShipping`, `GiaoHangDoiTacMa`.
- **`CauHinh`**: `JsonSetting` (deserialize → `CauHinhSettingDto`), chứa `IdChiNhanhTong`, `IdRoles_Che_GiaNhap`, `ChoPhepCheckTonKhoQuaAPIDoiTac`, `IsTinhGioiHanTonKhoTuDong`.
- **`lnChiNhanh`** (`CauHinhKhoChiNhanh.cs`): cấu hình kho theo chi nhánh.

## Luồng code chính

### Tìm tồn kho theo điều kiện (`KhoChiTietService.TimTheoDieuKienAsync`, :133)
File: `WarehouseProduct.Service/Generated/KhoChiTietService.cs`
1. **Guard**: `ListIdNhomSanPham` rỗng → trả `{"table1":[],"table2":[{"totalRecord":0}]}` ngay (đây là lý do FE trống khi chưa chọn nhóm). :135
2. Trim/null hóa `KeyWord` (tìm theo tên/barcode/mã SP). :151
3. Lấy `GetCauHinhInit` → tính cờ `anGiaNhap` / `cheGiaNhap`:
   - `IdChiNhanhTong != userLogin.IdChiNhanh` → `anGiaNhap = true`. :168
   - User không admin/CSKH và role ∈ `IdRoles_Che_GiaNhap` → ẩn + che. :175
4. Gọi **stored proc `sp_ReportWarehouse_KhoChiTiet_Search`** qua SqlSugar (`_dapperDB`), params gồm `@AnGiaNhap`, `@CheGiaNhap`, paging. :198-201
5. Trả DataSet serialize camelCase: `table1` data + `table2[0]` total. Có log thời gian chạy store nếu vượt ngưỡng `GetTimeToLog`. :207-213

### Tồn theo sản phẩm view chi nhánh (`GetTonKhoSanPham_ViewChiNhanh`, :868)
- Hiện **luôn đi đường SQL** (`if (true)` :879) — comment cũ định dùng ElasticSearch nhưng tắt vì cần dữ liệu giới hạn tồn.
- Store mặc định: **`PRC_WarehouseProduct_SoLuongTonKhoSanPham_TheoChiNhanh`**.
- Nếu `CauHinhSettingDto.IsTinhGioiHanTonKhoTuDong == true && IsDuBaoHetHang == true` → store **`..._TheoChiNhanh_Auto`**, cần `@TuNgay/@DenNgay/@SoNgay/@KieuKhoangNgay`; thiếu `KieuKhoangNgay` → báo lỗi. :892-908
- Nhánh ElasticSearch (dead code) dùng index `khochitiet`, agg Terms theo `IdChiNhanh`, BucketScript `soLuong - soLuongChoXuat`. :946-986

### Giá nhập SP cấu thành (:3386)
`GiaNhapTrungBinh` SP cha = Σ (giá vốn SP con × DinhLuong / ValueDonViDem), không lấy trực tiếp từ KhoChiTiet.

### Kho mặc định (`KhoService`, `KhoService.cs`)
- Khi `ThemMoi`/`Update` với `IsMacDinh == true` → bỏ cờ mặc định các kho khác cùng chi nhánh. :256-265, :332-341
- `MarkDirty(IsMacDinh / IsAnKhoDoiVoiSale / IsChoPhepChotAm)` trước khi save. :230,247,248

## Bug-prone
- **Bảng trống** thường không phải bug — `ListIdNhomSanPham` rỗng → guard trả mảng rỗng (`TimTheoDieuKienAsync` :135).
- **Tồn lệch**: do còn phiếu chưa xác nhận (logic cộng/trừ tồn nằm ở consumer phiếu, không ở màn này). Đối chiếu qua kiểm kho.
- **`SoLuongChoXuat`**: nhiều chỗ tính `Có thể bán = SoLuong - SoLuongChoXuat` (:590, :2718, :2907). Sửa công thức phải đồng bộ tất cả.
- **Ẩn giá nhập**: phụ thuộc cấu hình `IdChiNhanhTong` + `IdRoles_Che_GiaNhap`; test với user khác chi nhánh tổng để verify.
- **Export Excel report**: validate `OffSet >= 0`, `0 < PageSize <= 10000` (`KhoChiTietController` :181-191) — bỏ qua sẽ trả 10k+ rows.
- **Performance**: TimTheoDieuKien + GetTonKhoSanPham đi qua stored proc, có log cảnh báo khi chạy quá `GetTimeToLog` giây → để ý khi data lớn.
- **Trace `CauHinh_Warehouse_LuuCauHinhKho`** không có nguyên văn trong code — đừng search literal, map sang nhóm `CauHinh_Warehouse_*`.

## File source (path:line)
BE (`D:\PROJECT\Git\_SourceForCode\Group2\scrm\`):
- `WarehouseProduct/WarehouseProduct.API/Controllers/KhoChiTietController.cs` (TI_Kho :13; TimTheoDieuKien :27-33; GetTonKhoSanPham_ViewChiNhanh :78-84; Export validate :169-194)
- `WarehouseProduct/WarehouseProduct.API/Controllers/KhoController.cs` (Kho_TimTheoDieuKien :40-45; ThemMoi :53; CapNhat :96; Xoa :114)
- `WarehouseProduct/WarehouseProduct.API/Controllers/CauHinhController.cs` (:26,:73,:91,:172)
- `WarehouseProduct/WarehouseProduct.API/Controllers/CauHinhKhoChiNhanhController.cs` (:20-51)
- `WarehouseProduct/WarehouseProduct.Service/Generated/KhoChiTietService.cs` (TimTheoDieuKien :133-220; GetTonKhoSanPham_ViewChiNhanh :868-998; SP cấu thành :3386-3411)
- `WarehouseProduct/WarehouseProduct.Service/Generated/KhoService.cs` (TimTheoDieuKien :352; IsMacDinh handling :256-265,:332-341)
- `WarehouseProduct/WarehouseProduct.DataContext/Entity/KhoChiTiet.cs` (:13-44)

FE (`D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\`):
- `src/app/pages/WarehouseProduct/warehouseProduct-routing.module.ts` (routes :121,:125,:169,:213; config import :43,:210)
- `src/app/pages/WarehouseProduct/quanly-kho/ql-ton-kho/ql-ton-kho.component.ts`
- `src/app/pages/WarehouseProduct/quanly-kho/kiemtra-tonkho/kiemtra-tonkho-chinhanh/kiemtra-tonkho-chinhanh.component.ts`
- `src/app/pages/WarehouseProduct/kho/kho.component.ts` (+ `kho-edit/kho-edit.component.ts` :14,:75 dùng CauHinhKhoChiNhanhService)
- `src/app/pages/WarehouseProduct/config-warehouse-product/config-warehouse-product.component.ts`
- `src/app/services/warehouseproduct/khoChiTiet.service.ts` (TimTheoDieuKien :19; GetTonKhoSanPham_ViewChiNhanh :33)
