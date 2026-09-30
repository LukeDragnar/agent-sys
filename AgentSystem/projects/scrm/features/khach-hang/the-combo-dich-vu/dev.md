# Thẻ & combo dịch vụ khách hàng — Kỹ thuật (DEV ONLY)

> Không cho khách. Service BE chính: **WarehouseProduct** (thẻ + combo của khách, template); báo cáo ở **Report**; trừ thẻ/combo phát sinh từ **Order** (thanh toán đơn).
> BE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm`
> FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend`
> Code thật, trace từ source.

## Ý tưởng chung
- **Danh mục gốc** (loại thẻ `TheDichVu`, combo `ComBoSpa`) định nghĩa ở module Kho. Khi cấp cho khách sẽ "đẻ" ra **bản ghi của khách** mang số dư riêng:
  - Thẻ → `TheDichVuKhachHang` (giữ `SoLanConLai` / `SoTienConLai` / `NgayHetHan` / `TrangThai`).
  - Combo → `ComBoSpaKhachHang` (header) + `ComBoSpaKhachHangChiTiet` (mỗi dịch vụ trong gói có `SoLan` / `SoLanConLai` riêng → đây là cơ chế "liệu trình").
- **Trừ thẻ** không xảy ra ở `TheDichVuKhachHangService.Create` (đó chỉ là cấp). Việc **trừ buổi/tiền** xảy ra khi **thanh toán đơn hàng** → `WarehouseProductUpdateService.TinhToanTheCuaKhachHang`. **Trừ combo** qua `ComBoSpaKhachHangService.TacDongDinhLuong_*`.
- Mọi tác động ghi log: `TheDichVuKhachHangLichSuGiaoDich` (giao dịch tiền/lần), Mongo `TheDichVuKhachHangLichSu` (cấp/sửa/xóa), và `KhachHangLichSuTuongTac` (timeline hồ sơ KH).

---

## Màn hình & Route (FE)
Prefix module: `/warehouse-product` (`WarehouseProductModule`); báo cáo ở `/report` (`ReportModule`). Tab trong hồ sơ KH nằm ở `/customer`.

Routing khai báo: `pages/WarehouseProduct/warehouseProduct-routing.module.ts`, `pages/Report/report-routing.module.ts`.

| Màn hình | Route | Component (.ts) | Service FE | API |
|---|---|---|---|---|
| Danh sách thẻ-khách | `/warehouse-product/thedichvu-khachhang` | `pages/WarehouseProduct/thedichvu-khachhang/thedichvu-khachhang.component.ts` | `services/warehouseproduct/thedichvukhachhang.service.ts` | `api/TheDichVuKhachHang/TimTheoDieuKien` (+ `LayTatCaThePt`) |
| Thẻ khách hàng (cấp/sửa/nạp/gia hạn) | nested ở `thedichvu-khachhang` (modal) | `.../thedichvu-khachhang-edit`, `-update`, `-napthe`, `-giahan` | nt | `ThemMoi`, `CapNhat`, `NapTheTrucTiep`, `GiaHanMulti`, `LayTheKhachHangPhanTrang` |
| Lịch sử dùng thẻ (theo từng thẻ) | modal trong `thedichvu-khachhang` | `.../thedichvu-khachhang-lichsu/thedichvu-khachhang-lichsu.component.ts` | nt | `api/TheDichVuKhachHangLichSu/TimTheoDieuKien` |
| Lịch sử dùng thẻ (báo cáo tổng) | `/report/used-card` | `pages/Report/used-card/used-card.component.ts` | `services/customer/khachhang-report.service.ts` | `api/OrderReport/CustomerOrderReportUsedCard` |
| Danh sách combo-khách | `/warehouse-product/combo-khachhang` *(verify slug)* | `pages/WarehouseProduct/combo-spa/combo-spa-khachhang/combo-spa-khachhang.component.ts` | `services/warehouseproduct/combo-spa-khachhang.service.ts` | `api/ComBoSpaKhachHang/TimTheoDieuKien` |
| Khách hàng-combo (bán/chi tiết) | nested combo (modal) | `pages/WarehouseProduct/combo-spa/combo-spa-khachhang-edit/...` | nt | `ThemMoi`, `GetListComBoByIdKhachHang`, `GetComboSpaKhDetail`, `GetListComBoByIdKhachHangFilterDto` |
| Template mẫu | `/warehouse-product/admin-template` | `pages/WarehouseProduct/admin-template/admin-template.component.ts` | `services/warehouseproduct/donviTemplate.service.ts` | `api/DonViTemplate/TimTheoDieuKienAdmin`, `CoppyMau/{id}` |
| Template đơn vị | `/warehouse-product/donvi-template` | `pages/WarehouseProduct/donvi-template/donvi-template.component.ts` (+ `-design`, `-edit`) | nt | `api/DonViTemplate/TimTheoDieuKien`, `CapNhatMacDinh`, `LayTemplateMacDinh` |
| Tab Thẻ DV trong hồ sơ KH | `/customer/...detail` | `pages/Customer/quanly-khachhang/quanly-khachhang-detail/the-dich-vu/...` | thedichvukhachhang.service | `LayTheKhachHangPhanTrang` |
| Tab Combo trong hồ sơ KH | nt | `.../quanly-khachhang-detail/solan-combo/...` | combo-spa-khachhang.service | `GetListComBoByIdKhachHangFilterDto` |

> Slug route chính xác của các màn combo-khách cần đối chiếu trực tiếp `warehouseProduct-routing.module.ts` (literal có thể khác `combo-khachhang`).

---

## Controller (BE)

### TheDichVuKhachHangController — `WarehouseProduct/WarehouseProduct.API/Controllers/TheDichVuKhachHangController.cs`
`[TienIchAttribute("TheDichVuKhachHangController", "Quản lý thẻ dịch vụ của khách hàng", MaTienIchSCRM.TI_Kho)]`
Endpoint chính (route `api/TheDichVuKhachHang/...`):
- `ThemMoi` — cấp thẻ (trace `TheDichVuKhachHang_ThemMoi`, quyền `DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-QLDV`).
- `TimTheoDieuKien` (POST) — danh sách thẻ-khách (filter `TheDichVuKhachHangFilterDto`).
- `LayTheKhachHangPhanTrang`, `LayTatCaTheLan`, `LayTatCaTheTien`, `LayTatCaThePt` — list theo KH / theo loại.
- `CapNhat`, `Xoa`/`XoaMulti`, `Lock`/`UnLock`/`LockMulti`/`UnLockMulti`, `ThuHoi`/`ThuHoiMulti`, `GiaHanMulti`.
- `NapTheTrucTiep` — nạp tiền vào thẻ không qua đơn (`NapTheTienDto`).
- `*_Private` (`PrivateSBAuthorize`) — gọi cross-service từ Order/Website (`GetListTheDVByIdKhachHang_Private`, `ChiTietTheoIds`, `GetDetailByIdPrivate`, `CreateFromImportKH`, `TaoTheTienAoKhachHang`).
- Anonymous: `TaoTheLichKhachHang`, `LayTheKhachHangTheoIdThe_Anonymous`.

### ComBoSpaKhachHangController — `.../Controllers/ComBoSpaKhachHangController.cs`
`[TienIchAttribute("ComBoSpaKhachHangController", "Quản lý combo của khách hàng", MaTienIchSCRM.TI_Kho)]`
- `ThemMoi` / `CapNhat` / `Xoa` / `GetDetailById`.
- `TimTheoDieuKien` (trace `ComBoSpaKhachHang_TimTheoDieuKien`) — danh sách combo-khách.
- `GetListComBoByIdKhachHang`, `GetListComBoByIdKhachHangPt`, `GetListComBoByIdKhachHangFilterDto`, `GetComboSpaKhDetail`, `LayThongFullThongTinComboKhachHangById`.
- `TacDongDinhLuong_Single` / `_Multi` — **cộng/trừ buổi từng dịch vụ trong combo**.
- Import: `ImportComBoSpaKhachHang` (mẫu 1/2/3), `ImportUpdateComBoSpaKhachHang` + download template/lỗi.
- `*_Private` cho cross-service (`GetListComBoByIdKhachHang_Private`, `GetListComboKhach`, `GetListComboByIds`).

### DonViTemplateController — `.../Controllers/DonViTemplateController.cs`
`[TienIchAttribute("DonViTemplate", "Quản lý đơn vị template", MaTienIchSCRM.TI_Kho)]`, quyền `DV-AD-TONG`.
- `TimTheoDieuKien` — template **của đơn vị** (`IdDonVi == DonViId`).
- `TimTheoDieuKienAdmin` (trace `DonViTemplate_TimTheoDieuKienAdmin`) — template **mẫu hệ thống** (`IdDonVi` null/empty); non-admin chỉ thấy bản đang `SuDung`.
- `CoppyMau/{id}` — copy mẫu hệ thống → template riêng của đơn vị.
- `LayTemplateMacDinh`, `CapNhatMacDinh` (đảm bảo 1 mặc định/đơn vị), `ThemMoi`, `CapNhatThongTin`, `Xoa`.

### OrderReportController — `Report/Report.API/Controllers/OrderReportController.cs`
- `CustomerOrderReportUsedCard` (trace `OrderReport_CustomerOrderReportUsedCard`, route `api/OrderReport/CustomerOrderReportUsedCard`) — báo cáo lịch sử dùng thẻ, quyền `DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-KT, DV-KT-TONG, HT-AD, HT-CSKH, DV-QLDV`.

---

## Service — logic cốt lõi

### Cấp thẻ — `TheDichVuKhachHangService.Create(TheDichVuKhachHangDto)`
`WarehouseProduct/WarehouseProduct.Service/Generated/TheDichVuKhachHangService.cs`
- Validate: `IdTheDichVu`, `ListKhachHang`, `NgayCap` bắt buộc; loại thẻ phải tồn tại trong `DonViId`.
- Loại **TheSoTien (2)**: nếu `SoTienKhuyenMai != null` bắt buộc `CauHinhTienKhuyenMai`; chặn `SoTienKhuyenMaiDaDungKhoiTao > SoTienKhuyenMai` và `SoTienDaDungKhoiTao > GiaTri`.
- Loại **TheSoLan (1)**: chặn `SoLanDaDungKhoiTao > SoLanSuDung`.
- Tính ngày hết hạn:
  ```
  ngayHetHan = (ThoiHanTheBatDau == TuNgayTaoThe) ? theDichVu.NgayTao : item.NgayCap;
  if (ThoiHanThang != null) ngayHetHan = ngayHetHan.AddMonths(ThoiHanThang);
  if (ThoiHanNgay  != null) ngayHetHan = ngayHetHan.AddDays(ThoiHanNgay);
  ```
- Với mỗi KH × `SoLuongCap`: sinh `Ma` (`CommonHelper.GenerateRandomCodeGuid(8,"THE")` + check trùng), `TrangThai = SuDung (2)`, và:
  ```
  SoLanConLai          = LoaiThe==TheSoLan  ? SoLanSuDung - SoLanDaDungKhoiTao : SoLanSuDung
  SoTienConLai         = LoaiThe==TheSoTien ? GiaTri      - SoTienDaDungKhoiTao : GiaTri
  SoTienKhuyenMaiConLai= LoaiThe==TheSoTien ? SoTienKhuyenMai - SoTienKhuyenMaiDaDungKhoiTao : SoTienKhuyenMai
  ```
- `InsertAsync(datas)` → ghi `KhachHangLichSuTuongTac` (KieuLoai=TheDichVu) + Mongo `TheDichVuKhachHangLichSu` (event ThemMoi).
- Lấy KH qua `CustomerServiceUrl/api/KhachHang/TimTheoIdsBase` (cross-service).

### TRỪ THẺ khi thanh toán — `WarehouseProductUpdateService.TinhToanTheCuaKhachHang(...)`
`WarehouseProduct/WarehouseProduct.Service/Generated/WarehouseProductUpdateService.cs` (gọi trong `ThanhToanDonHang`)
- Input: danh sách giao dịch thẻ (`ListTheTinhToan`), với `SoLuong` (số lần) / `SoTienThanhToanThe` **mang dấu âm** khi trừ.
- **Thẻ tiền:** `checkExsit.SoTienConLai += item.SoTienThanhToanThe;` và `SoTienKhuyenMaiConLai += item.SoTienThanhToanTheKhuyenMai;` (vì giá trị âm nên thực chất là trừ) + `MarkDirty`.
- **Thẻ lần:** `checkExsit.SoLanConLai += (int)item.SoLuong;` (`SoLuong` âm → trừ) + `MarkDirty`.
- Ghi `TheDichVuKhachHangLichSuGiaoDich`: `KieuLoai = DungTheTien(2)` hoặc `DungTheLan(3)`, lưu `MaDonHang`, `SoTienTacDong`/`SoLanTacDong` (giá trị tuyệt đối), `NguoiTaoUserName`.
- Rollback khi hủy đơn: `RollbackTheDichVuTinhToan(...)` → `SoTienConLai += ls.SoTienTacDong;` / `SoLanConLai += ls.SoLanTacDong;` (cộng trả).

### TRỪ COMBO — `ComBoSpaKhachHangService.TacDongDinhLuong_Single / _Multi`
`WarehouseProduct/WarehouseProduct.Service/Generated/ComBoSpaKhachHangService.cs`
- Tác động ở mức **dòng dịch vụ** (`ComBoSpaKhachHangChiTiet`):
  ```
  // cộng (trả lại):
  dichVu.SoLanConLai += SoLuong; if (dichVu.SoLanConLai > dichVu.SoLan) -> error (vượt tổng buổi)
  // trừ (dùng):
  dichVu.SoLanConLai -= SoLuong; if (dichVu.SoLanConLai < 0) -> error (âm buổi)
  ```
- `Create(ComBoSpaKhachHangCreateDto)`: tạo header `ComBoSpaKhachHang` + các `ComBoSpaKhachHangChiTiet` với `SoLan` và `SoLanConLai` ban đầu = số buổi từng dịch vụ.
- `TimTheoDieuKien(ComBoSpaKhachHangFilterDto)`: lọc `IdDonVi`, keyword (Ten/MaComboKhachHang), `IdKhachHang`, **lọc theo `SoLanSuDungDichVuConLai` + `Operator`** (2=≥, 3==, default=≤) qua join `ComBoSpaKhachHangChiTiet`; cuối cùng `x.IdChiNhanhKhachHang == userLogin.IdChiNhanh`. Có nhánh tìm theo `KeywordKhachHang` (gọi `Customer/api/KhachHang/PrivateSearchCustomer`). *Lưu ý: nhiều đoạn lọc theo cấu hình chi nhánh đã bị comment-out, hiện khóa cứng theo `IdChiNhanhKhachHang == IdChiNhanh`.*

### Báo cáo lịch sử dùng thẻ — `OrderReportService.CustomerOrderReportUsedCard(...)`
`Report/Report.Service/Generated/OrderReportService.cs`
- Gọi **Stored Procedure `PRC_SCRM_OrderReport_UsedCard`** qua SqlSugar (`db.Ado.UseStoredProcedure().GetDataSetAll`).
- Params: `@KeyWord, @IdDonVi, @IdChiNhanh, @TuNgay, @DenNgay, @IdThe, @PageNumber, @PageSize`.
- Check khoảng thời gian cho phép (`CheckCauHinhKhoangThoiGianChoPhepTimKiem`); non-admin/non-CSKH ép `idChiNhanh = userLogin.IdChiNhanh`.
- Trả `CustomerOrderReportUsedCardResponse { Summary, LstData }`; enrich tên chi nhánh từ Core (`ChiNhanh/GetAllChiNhanhTheoDonViPrivate`).

### Template — `DonViTemplateService`
`WarehouseProduct/WarehouseProduct.Service/Generated/DonViTemplateService.cs`
- `TimTheoDieuKienAsync` (template đơn vị, `IdDonVi==DonViId`, sort IsMacDinh desc), `TimTheoDieuKienAdmin` (mẫu hệ thống, `IdDonVi` null/empty), `CoppyMau`, `CapNhatMacDinh` (1 mặc định/đơn vị), `LayTemplateMacDinh`.
- Template = **giao diện hiển thị** thẻ/combo (ảnh nền, tiêu đề, kích thước), không liên quan số dư.

---

## Entities
| Loại | Entity (file `WarehouseProduct.DataContext/Entity/...`) | Field quan trọng |
|---|---|---|
| Thẻ của khách | `TheDichVuKhachHang.cs` | `LoaiThe`, `SoLanSuDung`, **`SoLanConLai`**, `GiaTri`, **`SoTienConLai`**, `SoTienKhuyenMai`/`SoTienKhuyenMaiConLai`, `*DaDungKhoiTao`, `TrangThai`, `NgayHetHan`, `NgayCap`, `Ma`, `IdTheDichVu`, `IdKhachHang`, `IdDonHang`, `IsSuaGiaVaSoLan` |
| Combo của khách (header) | `ComBoSpaKhachHang.cs` | `IdCombo`, `IdKhachHang`, `HanSuDung`, `GiaGoc/GiaBan/ChietKhau/ThanhTien`, `MaComboKhachHang`, `IdDonHang` |
| Combo chi tiết (liệu trình) | `ComBoSpaKhachHangChiTiet.cs` | `IdComBoSpaKhachHang`, `IdDichVu`, `KieuLoai`, **`SoLan`**, **`SoLanConLai`**, `SoPhutSuDung`/`SoPhutSuDungConLai`, `IsQuaTang` |
| Lịch sử giao dịch thẻ | `TheDichVuKhachHangLichSuGiaoDich.cs` | `KieuLoai` (enum giao dịch), `SoTienTacDong`, `SoLanTacDong`, `SoTienThanhToanTheKhuyenMai`, `IdDonHang`, `MaDonHang` |
| Lịch sử thẻ (Mongo) | `CollectionMongoDB/TheDichVuKhachHangLichSu.cs` | snapshot thẻ + `TypeImpact` (ThemMoi/Xoa/CapNhat) |
| Template | `DonViTemplate.cs` | `IdDonVi`, `TenTemplate`, `AnhNenUrl`, `TieuDe`/`TieuDeCss`, `ChieuRong`/`ChieuCao`, `IsMacDinh`, `SuDung` |

### Enums
`Libs/Models.Core/ContantsWarehouseProduct/Enums.cs`:
- `EnumTheDichVu`: **`TheSoLan = 1`** ("Thẻ số lần"), **`TheSoTien = 2`** ("Thẻ số tiền").

`WarehouseProduct.Service/Generated/Enums/EnumTheDichVu.cs`:
- `EnumTheDichVu_TrangThai`: `ThuHoi=0`, `Khoa=1`, `SuDung=2`, `HoanThanh=3`, `HetHan=4`, `HetHanSau1Tuan=5`, `HetHanSau1Thang=6`.
- `EnumTheDichVu_ThoiHanBatDau`: `TuNgayCapThe=1`, `TuNgayTaoThe=2`.
- Loại giao dịch (`EnumTypeImpact_Transaction`, qua trace log): `NapTien=1`, `DungTheTien=2`, `DungTheLan=3`, `DungDichVuComBo=4`, `TichDiem=5`, `DoiDiem=6`, `LaySanPhamComBo=7`. *(giá trị cần đối chiếu file enum gốc nếu chỉnh logic giao dịch.)*

---

## Luồng code (tóm tắt)
```
Cấp thẻ:  FE thedichvu-khachhang-edit → api/TheDichVuKhachHang/ThemMoi
          → TheDichVuKhachHangService.Create (validate, tính NgayHetHan, set SoLanConLai/SoTienConLai)
          → Insert + log (LichSuTuongTac + Mongo LichSu)

Dùng thẻ: Order ThanhToanDonHang → WarehouseProductUpdateService.TinhToanTheCuaKhachHang
          → SoLanConLai/SoTienConLai += (giá trị âm) ; ghi LichSuGiaoDich (DungTheLan/DungTheTien)
          → Hủy đơn: RollbackTheDichVuTinhToan (cộng trả)

Bán/dùng combo: ComBoSpaKhachHangService.Create (tạo ChiTiet SoLan/SoLanConLai)
          → TacDongDinhLuong_Single/_Multi (SoLanConLai -= / += theo dòng dịch vụ)

Báo cáo:  FE report/used-card → api/OrderReport/CustomerOrderReportUsedCard
          → OrderReportService → SP PRC_SCRM_OrderReport_UsedCard

Template: FE admin-template / donvi-template → api/DonViTemplate/TimTheoDieuKienAdmin|TimTheoDieuKien|CoppyMau|CapNhatMacDinh
```

---

## Bug-prone
- **Trừ thẻ dùng `+=` với số âm:** `TinhToanTheCuaKhachHang` cộng `SoLuong`/`SoTienThanhToanThe` đã mang dấu âm. Nếu phía gọi truyền sai dấu (dương) → thẻ **tăng** số dư thay vì giảm. Khi debug số dư sai, check dấu của `ListTheTinhToan`.
- **Hết hạn ≠ hết buổi:** thẻ còn `SoLanConLai`/`SoTienConLai` > 0 nhưng quá `NgayHetHan` vẫn không dùng được (lọc `NgayHetHan >= today` ở `CommonTheDichVuService.GetAllTheDichVuKhachHang`). Đừng chỉ check số dư.
- **Validate "đã dùng ban đầu":** quên check `*DaDungKhoiTao > giá trị thẻ` sẽ tạo thẻ số dư âm. Logic đã chặn trong `Create` — giữ nguyên khi refactor.
- **Combo trừ theo dòng dịch vụ:** số dư combo là tổng các `ComBoSpaKhachHangChiTiet.SoLanConLai`, không phải 1 con số ở header. Trừ nhầm dòng → "hết liệu trình" sai. `TacDongDinhLuong` chặn `< 0` và `> SoLan`.
- **Lọc combo theo chi nhánh khóa cứng:** `TimTheoDieuKien` ép `IdChiNhanhKhachHang == userLogin.IdChiNhanh` (các nhánh cấu hình theo `ChiNhanhHienThiTatCaKhachHangDonVi` đã comment-out). Role tổng có thể không thấy combo của chi nhánh khác — chú ý khi sửa.
- **Rollback phải khớp giao dịch:** hoàn buổi/tiền dựa trên `TheDichVuKhachHangLichSuGiaoDich`. Nếu log giao dịch thiếu/sai → rollback hoàn sai số. Đảm bảo mỗi lần trừ đều ghi đủ `SoTienTacDong`/`SoLanTacDong`.
- **Template không đụng nghiệp vụ:** đừng nhầm `DonViTemplate` (giao diện) với loại thẻ/combo gốc. Quyền `DV-AD-TONG` only.
- **Endpoint `_Private`:** Order/Website gọi qua `PrivateSBAuthorize` (không qua token user) — khi đổi DTO/route private phải sync cả phía gọi cross-service.
- **Mã thẻ trùng:** `Create` sinh `Ma` rồi loop check trùng trong `DonViId`; nếu sửa generator giữ check uniqueness.

---

## File source
**BE** (`...\scrm`):
- Controllers: `WarehouseProduct/WarehouseProduct.API/Controllers/{TheDichVuKhachHangController, ComBoSpaKhachHangController, DonViTemplateController}.cs`; `Report/Report.API/Controllers/OrderReportController.cs`
- Services: `WarehouseProduct/WarehouseProduct.Service/Generated/{TheDichVuKhachHangService, ComBoSpaKhachHangService, DonViTemplateService, TheDichVuKhachHangLichSuService, WarehouseProductUpdateService}.cs`; `.../Generated/Common/CommonTheDichVuService.cs`; `Report/Report.Service/Generated/OrderReportService.cs`
- Entities: `WarehouseProduct/WarehouseProduct.DataContext/Entity/{TheDichVuKhachHang, ComBoSpaKhachHang, ComBoSpaKhachHangChiTiet, TheDichVuKhachHangLichSuGiaoDich, DonViTemplate}.cs`; `.../CollectionMongoDB/TheDichVuKhachHangLichSu.cs`
- Enums: `Libs/Models.Core/ContantsWarehouseProduct/Enums.cs`; `WarehouseProduct.Service/Generated/Enums/EnumTheDichVu.cs`
- DTO: `Libs/Models.Core/DTO/WarehouseProduct.Client/{TheDichVuKhachHang, ComBoSpaKhachHang}/...`; `Libs/Models.Core/DTO/Order.Client/Report/CustomerOrderReportUsedCardDto.cs`
- SP: `PRC_SCRM_OrderReport_UsedCard` (DB)

**FE** (`...\scrm_frontend`):
- `pages/WarehouseProduct/warehouseProduct-routing.module.ts`, `pages/Report/report-routing.module.ts`
- `pages/WarehouseProduct/thedichvu-khachhang/` (+ `-edit`, `-update`, `-napthe`, `-giahan`, `-lichsu`, `-lichhen-danhsach`)
- `pages/WarehouseProduct/combo-spa/combo-spa-khachhang/` (+ `-edit`)
- `pages/WarehouseProduct/{admin-template, donvi-template}/` (+ `-design`, `-edit`)
- `pages/Report/used-card/`
- `pages/Customer/quanly-khachhang/quanly-khachhang-detail/{the-dich-vu, solan-combo}/`
- Services: `services/warehouseproduct/{thedichvukhachhang, combo-spa-khachhang, donviTemplate}.service.ts`; `services/customer/khachhang-report.service.ts`
