# Quản lý sản phẩm chi nhánh — Kỹ thuật (DEV ONLY)

> Không cho khách. Service: **WarehouseProduct**. Code thật, trace từ source.
> BE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm`
> FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend`

## Ý tưởng chung
Danh mục **cấp chi nhánh** = bản ghi gốc (SanPham/DichVu/ComBoSpa/TheDichVu) có `IdChiNhanhTao` = chi nhánh tạo, **cộng** một bảng nối `*ChiNhanh` định nghĩa chi nhánh nào được dùng. Mọi API "...ChiNhanh" đều:
- Lọc `IdDonVi == userLogin.DonViId` (+ thường `IdNganhNghe`) **và** `IdChiNhanhTao == userLogin.IdChiNhanh`.
- Join bảng nối `*ChiNhanh` theo `IdChiNhanh == userLogin.IdChiNhanh`.
→ Khác hẳn màn cấp **đơn vị** (không khóa theo `IdChiNhanhTao`/junction chi nhánh).

---

## Màn hình & Route (FE)
Prefix module: `/warehouse-product` (`app-routing.module.ts` → `WarehouseProductModule`).

| Màn hình | Route | Component (.ts) | Service FE |
|---|---|---|---|
| Quản lý sản phẩm chi nhánh | `/warehouse-product/quan-ly-sp-chinhanh` | `pages/WarehouseProduct/quanly-sanphm-chinhanhtao/quanly-sanphm-chinhanhtao.component.ts` | `services/warehouseproduct/sanPham.service.ts` |
| Quản lý dịch vụ chi nhánh | `/warehouse-product/quan-ly-dv-chinhanh` | `pages/WarehouseProduct/quanly-dichvu-chinhanhtao/quanly-dichvu-chinhanhtao.component.ts` | `services/warehouseproduct/dichVu.service.ts` |
| Quản lý Combo chi nhánh | `/warehouse-product/combo-chinhanh` | `pages/WarehouseProduct/combo-spa-chinhanh/combo-spa-chinhanh.component.ts` | `services/warehouseproduct/combo-spa.service.ts` |
| Quản lý thẻ chi nhánh | `/warehouse-product/thedichvu-chinhanh` | `pages/WarehouseProduct/thedichvu-chinhanhtao/thedichvu-chinhanhtao.component.ts` | `services/warehouseproduct/thedichvu-chinhanhtao.service.ts` |
| Gói mở rộng | *(cần xác nhận — không tìm thấy literal "Gói mở rộng" trong source FE/BE; nhiều khả năng là label menu ở bảng `Page` (DB), hoặc tên hiển thị gộp 4 màn trên)* | — | — |

Tiêu đề hiển thị (key đa ngôn ngữ trong .html):
- SP: `tlt-quan-ly-san-pham-chi-nhanh` → "Quản lý sản phẩm chi nhánh"
- Combo: `tlt-quan-ly-combo` → "Quản lý combo"
- Thẻ: `tlt-quan-ly-the-dich-vu-chi-nhanh-tao` → "Quản lý thẻ dịch vụ chi nhánh tạo"

Routing được khai báo trong `pages/WarehouseProduct/warehouseProduct-routing.module.ts` (đường dẫn `quan-ly-sp-chinhanh`, `quan-ly-dv-chinhanh`, `combo-chinhanh`, `thedichvu-chinhanh`).

---

## Controller → Service → Entity (BE)

### 1. Sản phẩm chi nhánh
- **Endpoint:** `POST api/SanPham/TimTheoDieuKienSpChiNhanhAsync`
- **Controller:** `SanPhamController.TimTheoDieuKienSpChiNhanhAsync(SanPhamFilterDto gridInfo)`
  `WarehouseProduct/WarehouseProduct.API/Controllers/SanPhamController.cs`
  `[SBAuthorize("SanPham_TimTheoDieuKienSpChiNhanhAsync", ..., "DV-AD-TONG, DV-AD-CN, DV-QLDV, DV-QL-KHO, DV-QL-KHO-TONG, DV-TP-QL-KHO, DV-TN-QL-KHO")]`
- **Service:** `SanPhamService.TimTheoDieuKienSpChiNhanhAsync(SanPhamFilterDto, UserLogin?)`
  `WarehouseProduct/WarehouseProduct.Service/Generated/SanPhamService.cs`
- **DTO:** `SanPhamFilterDto` (`Libs/Models.Core/DTO/WarehouseProduct.Client/SanPham/SanPhamFilterDto.cs`) — `KeyWord`, `ListIdNhomSanPham` (bắt buộc), `IdChiNhanh`, `IdNganhNghe`, `PageInfo`.
- **Entity:** `SanPhamCha` (`...DataContext/Entity/SanPhamCha.cs`) — `IdDonVi`, `IdNganhNghe`, `IdChiNhanhTao`, `IdNhomSanPham`, `Ma`, `Ten`, `GiaSanPham`, `SuDung`.
- **Junction:** `SanPhamChaChiNhanh` (`...DataContext/Entity/SanPhamChaChiNhanh.cs`) — `IdSanPhamCha`, `IdChiNhanh`, `IdDonVi`.

### 2. Dịch vụ chi nhánh
- **Endpoint:** `POST api/DichVu/TimTheoDieuKienAsyncDvChiNhanh`
- **Controller:** `DichVuController.TimTheoDieuKienAsyncDvChiNhanh(DichVuFilterDto gridInfo)`
  `WarehouseProduct/WarehouseProduct.API/Controllers/DichVuController.cs`
- **Service:** `DichVuService.TimTheoDieuKienAsyncDvChiNhanh(DichVuFilterDto, UserLogin?)`
  `WarehouseProduct/WarehouseProduct.Service/Generated/DichVuService.cs`
- **DTO:** `DichVuFilterDto` — `KeyWord`, `ListIdNhomDichVu` (bắt buộc), `IdChiNhanh`, `IdNganhNghe`, `PageInfo`.
- **Entity:** `DichVu` (`...DataContext/Entity/DichVu.cs`) — `IdChiNhanhTao`, `IdNhomDichVu`, `GiaDichVu`, `SuDung`, `PhutPhucVu`.
- **Junction:** `DichVuChiNhanh` (`...DataContext/Entity/DichVuChiNhanh.cs`) — `IdDichVu`, `IdChiNhanh`.
- **Logic thật (xác minh trực tiếp):**
  ```
  tblDichVu      : IdDonVi==DonViId && IdNganhNghe==IdNganhNghe && IdChiNhanhTao==IdChiNhanh
  tblDichVuChiNhanh: IdChiNhanh==userLogin.IdChiNhanh
  query: ListIdNhomDichVu.Contains(dv.IdNhomDichVu) && tblDichVuChiNhanh.Any(a => a.IdDichVu == dv.Id)
  ```
  → Nếu `ListIdNhomDichVu` null/empty → `ResultWithError("Hãy truyền nhóm dịch vụ")`.

### 3. Combo chi nhánh
- **Endpoint:** `POST api/ComBoSpa/TimTheoDieuKienAsyncChiNhanh` (FE gọi qua `combo-spa.service.ts`)
- **Controller:** `ComBoSpaController.TimTheoDieuKienAsyncChiNhanh(ComBoSpaFilterDto gridInfo)`
  `WarehouseProduct/WarehouseProduct.API/Controllers/ComBoSpaController.cs`
- **Service:** `ComBoSpaService.TimTheoDieuKienAsyncChiNhanh(ComBoSpaFilterDto)`
  `WarehouseProduct/WarehouseProduct.Service/Generated/ComBoSpaService.cs`
- **DTO:** `ComBoSpaFilterDto` — `KeyWord`, `IsComboDongSan`, `PageInfo`.
- **Entity:** `ComBoSpa` (`...DataContext/Entity/ComBoSpa.cs`) — `IdChiNhanhTao`, `Ten`, `Ma`, `GiaBan`, `IsSuDung`.
- **Junction:** `ComBoSpaChiNhanh` (`...DataContext/Entity/ComBoSpaChiNhanh.cs`) — `IdCombo`, `IdChiNhanh`, `IdDonVi`, **`SoLuong`** (giới hạn số lượng theo chi nhánh), **`UnLimited`**.
- **Logic:** lọc theo `IdChiNhanhTao == userLogin.IdChiNhanh`; với user non-admin còn join `ComBoSpaChiNhanh` để check quyền dùng + lấy hạn mức số lượng riêng của chi nhánh.

### 4. Thẻ chi nhánh
- **Endpoint:** `POST api/TheDichVuChiNhanhTao/TimTheoDieuKien`
- **Controller:** `TheDichVuChiNhanhTaoController.TimTheoDieuKien(TheDichVuFilterDto filter)`
  `WarehouseProduct/WarehouseProduct.API/Controllers/TheDichVuChiNhanhTaoController.cs`
  `[TienIchAttribute("TheDichVuChiNhanhTaoController", "Quản lý thẻ dịch vụ chi nhánh", MaTienIchSCRM.TI_Kho)]`
- **Service:** `TheDichVuChiNhanhTaoService.TimTheoDieuKienAsync(TheDichVuFilterDto)`
  `WarehouseProduct/WarehouseProduct.Service/Generated/TheDichVuChiNhanhTaoService.cs`
- **DTO:** `TheDichVuFilterDto` — `Keyword`, `LoaiThe` (0=thẻ tiền, 1=thẻ số lần), `IdChiNhanh`, `PageInfo`.
- **Entity:** `TheDichVu` (`...DataContext/Entity/TheDichVu.cs`) — `IdChiNhanhTao`, `IdChiNhanh`, `GiaBan`, `GiaTri`, `SoTienKhuyenMai`, `LoaiThe`, `SoLanSuDung`, `ThoiHanThang`, `ThoiHanNgay`, `IsKhongGioiHan`, `IsSuDung`.
- **Áp dụng chi nhánh:** `TheDichVuDoiTuongApDung` (`KieuLoai` = cấp chi nhánh, `IdDoiTuong` = IdChiNhanh).
- **Logic:** lọc `IdDonVi == userLogin.DonViId && IdChiNhanhTao == userLogin.IdChiNhanh`.

---

## Entities (tóm tắt junction theo loại)
| Loại | Bản ghi gốc | Bảng nối chi nhánh | Cấu hình riêng theo chi nhánh |
|---|---|---|---|
| Sản phẩm | `SanPhamCha` | `SanPhamChaChiNhanh` | chỉ phạm vi (availability) |
| Dịch vụ | `DichVu` | `DichVuChiNhanh` | chỉ phạm vi |
| Combo | `ComBoSpa` | `ComBoSpaChiNhanh` | **`SoLuong` / `UnLimited`** theo chi nhánh |
| Thẻ | `TheDichVu` | `TheDichVuDoiTuongApDung` | giá, giá trị, KM, thời hạn, số lần (per record) |

---

## Luồng code (đọc danh sách)
```
FE component (quanly-*-chinhanhtao) → service FE (sanPham/dichVu/combo-spa/thedichvu-chinhanhtao)
  → POST api/{Sp|DichVu|ComBoSpa|TheDichVuChiNhanhTao}/...
    → Controller [SBAuthorize/TienIch] → Service.TimTheoDieuKien*
      → Lọc IdDonVi + IdChiNhanhTao==userLogin.IdChiNhanh
      → Join *ChiNhanh (IdChiNhanh==userLogin.IdChiNhanh)
      → Lọc theo nhóm + keyword + (combo) hạn mức số lượng
      → Paging → MethodResult.ResultWithData(data, "", count)
```

## Bug-prone
- **Quên truyền nhóm** (`ListIdNhomSanPham`/`ListIdNhomDichVu`) → SP/DV trả rỗng hoặc lỗi "Hãy truyền nhóm". Luôn check FE đã set nhóm.
- **`IdChiNhanhTao` vs junction:** món hiện trên màn cần **cả hai** — đúng `IdChiNhanhTao` của chi nhánh **và** có bản ghi `*ChiNhanh`. Thiếu junction → món "tàng hình" ở màn chi nhánh dù đã tạo.
- **`userLogin.IdChiNhanh` rỗng** (tài khoản không gắn chi nhánh / token thiếu) → query trả rỗng. Kiểm tra phiên đăng nhập có chi nhánh.
- **Combo `SoLuong`/`UnLimited`:** đừng nhầm hạn mức combo theo chi nhánh với tồn kho SP. Kiểm tra `ComBoSpaChiNhanh` khi combo "hết suất" sai.
- **Admin vẫn bị giới hạn chi nhánh:** lưu ý đoạn lọc junction theo chi nhánh có chỗ từng comment-out cho role tổng (xem `DichVuService.TimTheoDieuKienAsyncDvChiNhanh`) — hiện đang lọc cứng theo `userLogin.IdChiNhanh`. Khi role tổng cần xem nhiều chi nhánh, để ý logic này.
- **Phân biệt với màn đơn vị:** đừng dùng nhầm `SanPham/TimTheoDieuKien` (cấp đơn vị) cho màn chi nhánh — endpoint khác (`...SpChiNhanhAsync`).

## File source
**BE** (`...\scrm`):
- `WarehouseProduct/WarehouseProduct.API/Controllers/SanPhamController.cs`
- `WarehouseProduct/WarehouseProduct.API/Controllers/DichVuController.cs`
- `WarehouseProduct/WarehouseProduct.API/Controllers/ComBoSpaController.cs`
- `WarehouseProduct/WarehouseProduct.API/Controllers/TheDichVuChiNhanhTaoController.cs`
- `WarehouseProduct/WarehouseProduct.Service/Generated/SanPhamService.cs`
- `WarehouseProduct/WarehouseProduct.Service/Generated/DichVuService.cs`
- `WarehouseProduct/WarehouseProduct.Service/Generated/ComBoSpaService.cs`
- `WarehouseProduct/WarehouseProduct.Service/Generated/TheDichVuChiNhanhTaoService.cs`
- Entities: `WarehouseProduct/WarehouseProduct.DataContext/Entity/{SanPhamCha, SanPhamChaChiNhanh, DichVu, DichVuChiNhanh, ComBoSpa, ComBoSpaChiNhanh, TheDichVu}.cs`
- DTO: `Libs/Models.Core/DTO/WarehouseProduct.Client/{SanPham/SanPhamFilterDto, DichVu/DichVuFilterDto, ComBoSpa/ComBoSpaFilterDto, TheDichVu/TheDichVuFilterDto}.cs`

**FE** (`...\scrm_frontend`):
- `src/app/app-routing.module.ts` (path `warehouse-product`)
- `src/app/pages/WarehouseProduct/warehouseProduct-routing.module.ts`
- `src/app/pages/WarehouseProduct/quanly-sanphm-chinhanhtao/`
- `src/app/pages/WarehouseProduct/quanly-dichvu-chinhanhtao/`
- `src/app/pages/WarehouseProduct/combo-spa-chinhanh/`
- `src/app/pages/WarehouseProduct/thedichvu-chinhanhtao/`
- `src/app/services/warehouseproduct/{sanPham, dichVu, combo-spa, thedichvu-chinhanhtao}.service.ts`
