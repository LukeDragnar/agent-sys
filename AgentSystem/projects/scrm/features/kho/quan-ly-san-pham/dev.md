# Quản lý sản phẩm — Kỹ thuật (chỉ dev)

> Service BE: **WarehouseProduct**. FE module: **WarehouseProduct** (route prefix `/warehouse`).
> Tài liệu này CHỈ cho dev — không đưa khách. Domain tổng: [../../warehouse.md](../../warehouse.md).

---

## Màn hình & Route (FE)

FE module load tại `/warehouse` (`app-routing.module.ts:396`). Routing chi tiết: `src/app/pages/WarehouseProduct/warehouseProduct-routing.module.ts`.

| Màn hình | Route | Component |
|---|---|---|
| Quản lý sản phẩm | `/warehouse/quan-ly-sp` | `QuanlySanphamComponent` (`quanly-sanpham/`) |
| Quản lý dịch vụ | `/warehouse/quan-ly-dv` | `QuanlyDichvuComponent` (`quanly-dichvu/`) |
| Quản lý Combo (list) | `/warehouse/combo` | `ComboSpaComponent` (`combo-spa/`) |
| Combo thêm/sửa | `/warehouse/combo/:mode/:id` | `ComboSpaEditComponent` |
| Quản lý thẻ | `/warehouse/thedichvu` | `ThedichvuComponent` (`thedichvu/`) |
| Danh mục sản phẩm | `/warehouse/dm-sanpham` | `DmSanphamComponent` (admin: `dm-sanpham-admin` → `DmSanphamAdminComponent`) |
| Nhãn sản phẩm | `/warehouse/nhan-san-pham` | `NhanSanPhamComponent` (`nhan-san-pham/`) |

> Biến thể chi nhánh / admin / SA tồn tại song song (vd `quan-ly-sp-chinhanh`, `quanly-sanpham-admin`, `combo-chinhanh`, `thedichvu-chinhanh`, `combo-khachhang`, `thedichvu-khachhang`).

---

## Controller (BE)

Path: `WarehouseProduct/WarehouseProduct.API/Controllers/`. Base route `api/[controller]`. Auth bằng `[SBAuthorize("<MaTinhNang>", ...)]`.

| Controller | Action chính (route) | MaTinhNang (quyền) |
|---|---|---|
| `SanPhamController` | `POST TimTheoDieuKien`, `ThemMoi`, `CapNhatThongTin`, `Xoa/{id}` | `SanPham_TimTheoDieuKien`, `SanPham_ThemMoi`, `SanPham_CapNhatThongTin`, `SanPham_Xoa` |
| `DichVuController` | `POST TimTheoDieuKien`, `ThemMoi`, `CapNhatThongTin`, `Xoa/{id}`, `GetDichVuByChiNhanh/{id}` | `DichVu_TimTheoDieuKien`, `DichVu_ThemMoi`, `DichVu_CapNhatThongTin`, `DichVu_Xoa` |
| `ComBoSpaController` | `POST TimTheoDieuKien`, `CreateCombo`, `Update/{id}`, `CheckSanPhamVaDichVuThuocChiNhanh` | `ComBoSpa_TimTheoDieuKien` (create/update không gắn SBAuthorize riêng) |
| `TheDichVuController` | `POST TimTheoDieuKien`, `ThemMoi`, `CapNhat`, `Xoa/{id}`, `GET GetTheDichVuTinhHoaHong/{id}`, `ChangeSuDungThe/{id}` | `TheDichVu_TimTheoDieuKien`, `TheDichVu_ThemMoi`, `TheDichVu_CapNhat`, `TheDichVu_Xoa`, `TheDichVu_GetTheDichVuTinhHoaHong` |
| `NhanSanPhamController` | `POST TimTheoDieuKien`, `ThemMoiNhanSanPham`, `CapNhatThongTinNhanSanPham`, `UpdateMany`, `XoaNhanSanPham/{id}` | `NhanSanPham_TimTheoDieuKien`, `NhanSanPham_ThemMoiNhanSanPham`, `NhanSanPham_CapNhatThongTinNhanSanPham`, `NhanSanPham_XoaNhanSanPham` |
| `NhomSanPhamController` (Danh mục) | `ThemMoiDMNhomSanPham`, `CapNhatThongTin`, `XoaTheoId`, `LayTatCa*` | (theo controller) |

> Quyền xem (`*_TimTheoDieuKien`) mở rộng nhiều nhóm: `DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-KT, DV-QLDV, DV-QL-KHO...`. **Thêm/sửa/xóa chủ yếu giới hạn `DV-AD-TONG`.**

---

## Entities

DataContext: `WarehouseProduct/WarehouseProduct.DataContext/Entity/`. Tất cả có `IdDonVi` (multi-tenant), nhiều bảng có `IdNganhNghe`.

- **SanPhamCha** (SP cha) + **SanPham** (SP con sinh theo thuộc tính) — `Ma`, `Ten`, `Slug`, `Barcode`, `GiaSanPham/GiaSanPhamSauVat`, `GiaNhapVao`, `SuDung`, `IsSanPhamImei`, `IsSanPhamLoDate`, `IsSanPhamCauThanh`, `IsSanPhamThuocTinh`, `TypeVat/ValueVat`, `IsUserShipping` + Weight/Length/Width/Height.
- **SanPhamChaChiNhanh** / **SanPhamChiNhanh** — gán SP theo chi nhánh (giá riêng theo chi nhánh).
- **SanPhamChaQuyDoi** (+ `BangQuyDoiJson`) — quy đổi đơn vị tính.
- **SanPhamChaHaCap** — giá theo bậc (GiaTriTu/GiaTriDen/GiaTien).
- **SanPhamCauThanh** — thành phần SP cấu thành. **SanPhamImei** — serial IMEI.
- **DichVu** — `PhutPhucVu`, `GiaDichVu/GiaDichVuSauVat`, `GiaVonDichVu`, `IsTinhGio`, `IsTinhGioTheoBlock`, `SoPhutTinhBlock`, `GiaBlock/GiaBlockSauVat`, `IsDichVuBan`. Phụ: **DichVuChiNhanh**, **DichVuTuyChonGia**, **DichVuSanPham** (SP tiêu hao kèm), **DichVuGiaTheoKhungGio** (TuGio/DenGio/NgayTrongTuan).
- **ComBoSpa** + **ComBoSpaDichVu**, **ComBoSpaSanPham**, **ComBoSpaChiNhanh**, **ComBoSpaKhachHang** (KH đã mua — chặn xóa). `HanSuDung`, `IsGioiHanSoLuong`, `SoLuongToiDa`, `GiaBan/GiaGoc/ChietKhau/ThanhTien`.
- **TheDichVu** + **TheDichVuKhachHang**, **TheDichVuDoiTuongApDung** (gán chi nhánh — `KieuLoai`, `IdDoiTuong`). `LoaiThe` (enum `EnumTheDichVu`: TheSoTien/TheSoLan/TheLich), `SoLanSuDung`, `IsKhongGioiHan`, `GiaTri`, `ThoiHanThang/Ngay`, `TiLeGiamHoaHong`.
- **NhanSanPham** — `Ten`, `BackgroundColor`, `Stt`, `NguoiTaoUserId`, `IsDelete`.
- **DMNhomSanPham** (Danh mục) — `Ten` (unique theo đơn vị).

---

## Luồng code

Service: `WarehouseProduct/WarehouseProduct.Service/Generated/`.

### SanPhamService.cs (~8800 dòng)
- `TimTheoDieuKienAsync(SanPhamFilterDto, user)` (~L265) — LINQ (**không dùng SP `SanPham_TimTheoDieuKien`**, tên trace = MaTinhNang quyền). Filter: `ListIdNhomSanPham` (bắt buộc), `KeyWord` (Ten/Barcode), `IdChiNhanh`; paging; sort `NgayCapNhat desc`. Trả `SanPhamTonKhoDto`.
- `ThemMoi(ThemSanPhamDto, user)` (~L360) — validate `ValidationThemMoiSPCha` (~L1200): bắt buộc Ten/Ảnh/Nhóm/ChiNhanh, chống trùng Ma/Ten/Barcode, IMEI XOR LoDate, VAT [0,100], quy đổi đơn vị (đúng 1 đơn vị bán mặc định). Auto-gen `Ma` qua `_commonService.SinhMaPhieu(TypeSinhMaPhieuEnum.SanPhamCha)`, `Slug`. Tạo: SanPhamCha + SanPhamChaChiNhanh + SanPhamChaQuyDoi(+`BangQuyDoiJson`) + SanPhamChaHaCap + WebsiteSanPhamCha + SanPham con (theo thuộc tính, `Ma = {MaCha}{Stt}`) + SanPhamCauThanh. Sau lưu: `ESIndexSanPhamCha/ESIndexSanPham`, xóa Redis (`XoaResdisSanPhamTheoChiNhanh/DonVi`), `GuiThongBaoInfoThemSp`.
- `CapNhatThongTin(CapNhatSanPhamDto, user, IdChiNhanhTao)` (~L2189) — **không đổi `Ma`**. Nếu đổi đơn vị tính/quy đổi → `KiemTraCoTonKhoHoacDonHang` chặn nếu có tồn/đã bán. Diff & replace child tables; ES + Redis + notify.
- `XoaTheoId` (~L4637) — **hard delete**. Guard: đang trong ComBoSpaSanPham / SanPhamCauThanh / DichVuSanPham → chặn. Cascade SanPham con + ChiNhanh + Imei; xóa ES + Redis + notify.

### DichVuService.cs (~4500 dòng)
- `TimTheoDieuKienAsync` (~L190) — LINQ; filter `ListIdNhomDichVu` (bắt buộc), `KeyWord`, `IdChiNhanh`.
- `ThemMoi(ThemDichVuDto, user)` (~L547) — `ValidateInput` (~L296): bắt buộc Ten/Ảnh/Giá/Nhóm/ChiNhanh, `PhutPhucVu >= 0`, ràng buộc block (`SoPhutTinhBlock`+`GiaBlock`), `IsTinhGio` khi có block/dịch vụ bàn. Tạo DichVu + DichVuChiNhanh + DichVuTuyChonGia + DichVuSanPham + DichVuGiaTheoKhungGio (validate đè giờ ~L468). Redis + notify.
- `CapNhatThongTin` (~L860), `XoaTheoId` (~L1156, **hard delete** + cascade DichVuChiNhanh).

### ComBoSpaService.cs (~5000 dòng)
- `TimTheoDieuKienAsync` (~L265) — LINQ; filter `KeyWord` (Ten/Ma), `IsComboDongSan`; scope theo `ComBoSpaChiNhanh` của `userLogin.IdChiNhanh`.
- `CreateCombo(ReqComboData, user)` (~L687) — `ValidatorRequest` (~L1120) + `CheckSanPhamVaDichVuThuocChiNhanh` (~L1947: mọi SP phải có trong `SanPhamChiNhanh`, DV trong `DichVuChiNhanh` của từng chi nhánh). `ThanhTien` = tổng line items. Auto-gen `Ma` (`GenerateRandomCodeInput`, 8 ký tự). Tạo ComBoSpaDichVu/SanPham/ChiNhanh trong transaction; xóa cache + notify.
- `UpdateAsync(id, ReqComboData, user)` (~L1560) — không đổi `Ma`; diff child theo **delete-all + insert-all** (child soft-delete `IsDelete`).
- `Xoa(id)` (~L2067) — chặn nếu có `ComBoSpaKhachHang` ("đã bán cho khách hàng"); else hard delete + cascade.

### TheDichVuService.cs (~1350 dòng)
- `TimTheoDieuKienAsync(TheDichVuFilterDto)` (~L131) — filter `Keyword`, `LoaiThe`; join `TheDichVuDoiTuongApDung` (scope chi nhánh).
- `Create(TheDichVuDto)` (~L247) — branch theo `LoaiThe`: TheSoLan cần `SoLanSuDung > 0`; TheSoTien cần `GiaTri`; TheLich chỉ 1 thẻ/đơn vị (set `IsKhongGioiHan=true`, bỏ thời hạn). Bắt buộc `ThoiHanTheBatDau` + thời hạn cho thẻ số tiền/lần; >= 1 chi nhánh; chống trùng tên.
- `GetTheDichVuTinhHoaHong(idChiNhanh)` (~L842) — list thẻ theo chi nhánh (`KieuLoai == ChiNhanh`, `IdDoiTuong == Empty || == idChiNhanh`), map `LoaiTheText`. **Read-only**, không tính toán hoa hồng tại đây (chỉ trả thẻ + tỉ lệ để nơi gọi tính).
- `ChangeSuDungThe(id)` (~L861) — toggle `IsSuDung`, xóa cache.

### NhanSanPhamService.cs (378 dòng) — `InsertAsync`/`UpdateAsync`/`UpdateMany`/`TimTheoDieuKienAsync`/`DeleteAsync`. Trùng tên theo đơn vị bị chặn. **Phân quyền theo người tạo**: non-`DV_AD_TONG` chỉ sửa/xóa nhãn `NguoiTaoUserId == userLogin.Id`. Cache `ClearCache_NhanSanPham_DonVi`.

### NhomSanPhamService.cs (584 dòng) — Danh mục: `ThemMoiDMNhomSanPham`/`CapNhatThongTin`/`XoaTheoId`/`LayTatCa*`. Chống trùng tên nhóm; bản `_SA` cho super-admin theo ngành nghề.

---

## Bug-prone

- **Tên trace = MaTinhNang quyền, KHÔNG phải stored procedure.** `SanPham/DichVu/ComBoSpa_TimTheoDieuKien` đều là LINQ trong service. Đừng đi tìm SP trong DB.
- **DichVuService `ValidateInput` (~L380 và ~L1928):** bug copy-paste — tính `isThieuGia` nhưng `if (isThieuTen == true)` → validate giá tùy chọn dịch vụ **bỏ lọt** (giá âm/null không bị chặn). Cần kiểm khi sửa giá tùy chọn.
- **Đổi đơn vị tính** chỉ chặn khi đổi *tập* đơn vị, **không** phát hiện đổi *tỉ lệ quy đổi* (`SoLuongToDvNho`) → có thể lệch tồn mà không bị chặn.
- **`GiaCua1DonViTinhToSauVat` tự tính 3 nơi** (`Math.Round`) khi null → rounding âm thầm, không cảnh báo người dùng.
- **Child SP con `Ma = {MaCha}{Stt}`** không check max length / không check trùng với SP đã có (chỉ check Ma do user nhập).
- **Combo:** `ThanhTien` vừa set từ `req.GiaBan` rồi ghi đè = tổng items — logic giá auto bị comment, dễ nhầm giá nào thực dùng. Child soft-delete nhưng parent hard-delete → orphan nếu rollback nửa chừng.
- **Combo branch scope** dùng cứng `userLogin.IdChiNhanh` ở filter → user khác chi nhánh có thể không thấy combo.
- **`HanSuDung` combo** không validate định dạng ngày, không lọc combo hết hạn ở search.
- **NhanSanPham** validate trùng tên trong RAM (`UpdateMany`) — không transaction; race nếu 2 user lưu cùng lúc.
- **TheDichVu message khi xóa** từng có chuỗi "Đồng bộ quyền thành công" (misleading) — kiểm message trả về.

---

## File source

BE (`D:/PROJECT/Git/_SourceForCode/Group2/scrm/`):
- `WarehouseProduct/WarehouseProduct.API/Controllers/{SanPham,DichVu,ComBoSpa,TheDichVu,NhanSanPham,NhomSanPham}Controller.cs`
- `WarehouseProduct/WarehouseProduct.Service/Generated/{SanPhamService,DichVuService,ComBoSpaService,TheDichVuService,NhanSanPhamService,NhomSanPhamService}.cs`
- `WarehouseProduct/WarehouseProduct.DataContext/Entity/` (các entity ở trên)
- DTO dùng chung: `Libs/Models.Core/DTO/WarehouseProduct.Client/`

FE (`D:/PROJECT/Git/_SourceForCode/Group2/scrm_frontend/`):
- `src/app/pages/WarehouseProduct/warehouseProduct-routing.module.ts`
- `src/app/pages/WarehouseProduct/{quanly-sanpham,quanly-dichvu,combo-spa,thedichvu,nhan-san-pham,dm-sanpham}/`
