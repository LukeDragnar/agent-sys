# Dev — Quản lý lô date & Imei

> CHỈ dành cho dev. Service: **WarehouseProduct** (.NET 6, EF Core 7 + SqlSugar/Dapper, MongoDB cho log). Trace gốc: `SinhLoDateTuDong_TimTheoDieuKien`, `SanPhamImei_TimTheoDieuKien`, `SinhImeiTuDong_TimTheoDieuKien`.

---

## Màn hình & route (FE — Angular 15, module `WarehouseProduct`)
Base API: `{environment.SERVICE_API_WarehouseProduct}api/`

| Màn | Route | Component (folder) |
|---|---|---|
| Danh sách / Quản lý sản phẩm IMEI | `WarehouseProduct/ds-imei` | `pages/WarehouseProduct/view-sanpham-imei/` |
| Sinh IMEI tự động | `WarehouseProduct/sinh-imei-tu-dong` | `pages/WarehouseProduct/sinh-imei-tudong/` (+ `sinh-imei-tudong-add/`) |
| Báo cáo / Quản lý sản phẩm lô date | `WarehouseProduct/ds-sp-lo-date` | `pages/WarehouseProduct/view-sanpham-lodate/` |
| Sinh lô date tự động | `WarehouseProduct/sinh-lo-date-tu-dong` | `pages/WarehouseProduct/sinh-lo-date-tu-dong/` (+ `sinh-lo-date-tu-dong-add/`) |

Services FE:
- `services/warehouseproduct/sanPhamImei.service.ts`
- `services/warehouseproduct/sinhImeiTuDong.service.ts`
- `services/warehouseproduct/sanPhamLoDate.service.ts`
- `services/warehouseproduct/sinhLoDateTuDong.service.ts`

> Lưu ý: yêu cầu liệt kê 6 màn nhưng FE thực tế có **4 component**. "Quản lý lô date" và "Báo cáo SP lô date" dùng chung màn `ds-sp-lo-date`; "Quản lý IMEI" và "Danh sách IMEI" dùng chung màn `ds-imei`.

---

## Controllers (BE) — `WarehouseProduct.API/Controllers/`

### SinhLoDateTuDongController.cs — `[TienIch("SinhLoDateTuDongController", "Sinh lô date tự động", MaTienIchSCRM.TI_Kho)]`
| Endpoint | Quyền | Service |
|---|---|---|
| POST `TimTheoDieuKien` | `SinhLoDateTuDong_TimTheoDieuKien` | `TimTheoDieuKienAsync` |
| POST `ThemMoi` | `SinhLoDateTuDong_ThemMoi` | `ThemMoi` |
| POST `InDon` | (auth thường) | `InDon` |
| GET `Xoa/{id}` | `SinhLoDateTuDong_Xoa` | `XoaLoDate` |

### SinhImeiTuDongController.cs — `[TienIch("SinhImeiTuDongController", "Sinh imei tự động", MaTienIchSCRM.TI_Kho)]`
| Endpoint | Quyền | Service |
|---|---|---|
| POST `TimTheoDieuKien` | `SinhImeiTuDong_TimTheoDieuKien` | `TimTheoDieuKienAsync` |
| POST `ThemMoi` | `SinhImeiTuDong_ThemMoi` | `ThemMoi` |
| POST `PreviewSinhImei/{soLuong}` | auth | `PreviewSinhImei` (chỉ trả mã, KHÔNG persist) |
| POST `ThemMoiCauHinhIn` / `DsCauHinh` / `CapNhatCauHinhIn` | auth | cấu hình mẫu in |
| POST `InDon` | auth | `InDon` |
| GET `Xoa/{id}` | `SinhImeiTuDong_Xoa` | `DeleteAsync` |
| POST `ExportExcelGanSanPham` / `ExportExcelGanSanPhamFromSanPhamImei` | `..._ExportExcelGanSanPham*` | xuất Excel GemBox |

### SanPhamImeiController.cs — `[TienIch("SanPhamImeiController", "Quản lý sản phẩm", MaTienIchSCRM.TI_Kho)]`
| Endpoint | Quyền | Service |
|---|---|---|
| POST `TimTheoDieuKien` | `SanPhamImei_TimTheoDieuKien` | `TimTheoDieuKienAsync` |
| POST `TimTheoDieuKienPrivate/{idDonVi}` | `PrivateSBAuthorize` (S2S, AuthenPartner) | `TimTheoDieuKienApiAsync` |
| POST `NhapKhoImei` / `XoaImei` / `NhapKhoImeiTrucTiep` / `XuatKhoImeiTrucTiep` / `ChuyenKhoImeiTrucTiep` / `QuetImeiNhanTuPhieuChuyen` / `ThayDoiHaCapImei` | quyền nhóm kho tương ứng | … |
| GET `CheckSanPhamImei/{imei}`, GET `KiemTraImei/{imei}`, POST `KiemTraImeiV2` | auth | tra cứu (V2 dùng POST cho imei chứa ký tự `/`) |
| POST `CheckImeiBindSanPhamDonHang`, `GetSanPhamImei` | `PrivateSBAuthorize` | gọi từ Order khi bán |
| POST `LichSuThaoTacImei` | auth | lịch sử thao tác IMEI (MongoDB) |

### SanPhamLoDateController.cs — `[TienIch("SanPhamLoDateController", "Quản lý sản phẩm lô date", MaTienIchSCRM.TI_Kho)]`
| Endpoint | Quyền | Ghi chú |
|---|---|---|
| POST `TimTheoDieuKien` | `SanPhamImei_TimTheoDieuKien` (⚠ dùng lại quyền của IMEI) | báo cáo/lưới lô date |
| POST `GetSanPhamLoDateWithKhoMacDinh` / `GetSanPhamLoDateByIdKho` | `PrivateSBAuthorize` | gọi từ Order |
| POST `DropdownLoDateTheoSpAndKho` / `...Pt` / `DropdownTheoKeywordAndIdSp` | quyền dropdown lô date | chọn lô khi bán |
| POST `TimChiTietLoDate` / `TimChiTietLoDateTheoLoSanXuat` | auth | chi tiết lô |
| POST `TinhToanTruKhoLoDateV2` | auth | tính trừ kho theo lô khi scan |

---

## Entities — `WarehouseProduct.DataContext/Entity/`

### SanPhamImei.cs (bản ghi 1 IMEI thực tế trong kho)
- `Imei`, `IdSanPham`, `MaSanPham`, `TenSanPham`, `IdSanPhamCha`, `IdNhomSanPham`
- `IdChiNhanh`, `IdKho`, `IdPhieu`, `IdPhieuSanPham`, `IdPhieuNhapCu`, `IdPhieuMuaHang`
- `TrangThai` (SanPhamImeiTrangThai), `KieuNhap` (SanPhamImeiKieuNhap), `TrangThaiSanPham` (SanPhamImeiTrangThaiSanPham)
- Lô/date: `LoSanPham`, `NgaySanXuat`, `NgayHetHanSanPham`, `SoDangKy`, `GiaBan`
- Hạ cấp: `IdSanPhamChaHaCap`, `SanPhamChaHaCapJson`, `GiaBanTheoSpHaCap`, `GiaTriHaCapTu/Den`, `SpHaCapGhiChu`
- NCC snapshot: `IdNhaCungCap`, `MaNhaCungCap`, `TenNhaCungCap` (**chỉ set khi nhập qua phiếu nhập, KHÔNG propagate khi chuyển/xuất kho**)
- Cờ quét: `IsDaQuetNhanChuyenKho`, `IsQuetChuyenKhoTrucTiep`, `IsQuetXuatKhoTrucTiep`, `IsQuetImeiNhapKhoTrucTiep`, `IsNhapLai`

### SinhImeiTuDong.cs (mã IMEI sinh sẵn, chưa nhất thiết gắn SP)
- `Imei`, `TienTo`, `SoTuTang`, `TrangThaiIn` (SinhImeiTuDong_TrangThaiIn_Enum), `LoSanPham`, `NgaySanXuat`, `NgayHetHanSanPham`, `SoDangKy`, `GiaBan`, `IdPhieuNhap`

### SinhLoDateTuDong.cs (mã lô date sinh sẵn)
- `Ma`, `TienTo`, `SoTuTang`, `TrangThaiIn`, `LoSanPham`, `NgaySanXuat`, `NgayHetHanSanPham`, `SoDangKy`, `GiaBan`, `IdPhieuNhap`, `TenSanPham`, `MaSanPham`

### Cờ trên SanPham.cs
- `IsSanPhamImei` (dòng ~162), `IsSanPhamLoDate` (dòng ~180) — bật/tắt theo dõi IMEI / lô date cho từng SP.

### Enums
- `SanPhamImeiTrangThai` (SB.Core/Contants.cs ~1325): 1=ChoNhapKho, 2=DaNhapKho, 3=DaXuatKho, 4=DangChuyenKho.
- `SanPhamImeiKieuNhap` (~1338): 1=Import, 2=ThuCong, 3=TuDong, 4=TuPhieuMuaHang.
- `SanPhamImeiTrangThaiSanPham` (~1350): 1=Moi, 2=DaSuDung, 3=Demo.
- `SinhImeiTuDong_TrangThaiIn_Enum` (Models.Core/Enum.cs ~936): 1=ChuaIn, 2=DaIn. (SinhLoDate dùng chung enum này.)

---

## Luồng code

### Tìm theo điều kiện (cả 3 màn)
Cả `SinhLoDateTuDongService.TimTheoDieuKienAsync`, `SinhImeiTuDongService.TimTheoDieuKienAsync`, `SanPhamImeiService.TimTheoDieuKienAsync` đều **gọi stored procedure qua Dapper/SqlSugar** (`_dbDapper.Ado.UseStoredProcedure().GetDataSetAll(...)`), KHÔNG phải LINQ:
- `PRC_SCRM_WarehouseProduct_SinhLoDateTuDong_Search`
- `PRC_SCRM_WarehouseProduct_SinhImeiTuDong_Search`
- `PRC_SCRM_WarehouseProduct_SanPhamImei_Search` (+ `_Api` cho partner)
- Param chung: `@IdDonVi`, `@IdChiNhanh`, `@Keyword`, `@PageIndex`, `@PageSize`, `@TuNgay`, `@DenNgay`; IMEI thêm `@KhoId`, `@IdNhomSanPham`, `@IdSanPhamCha`, `@IdSanPham`, `@TrangThaiImei`, `@IdNhaCungCap`.
- Trả về DataSet 2 bảng: `Tables[0]` = data → `ObjectHelper.FillCollection<...>`, `Tables[1].Rows[0][0]` = total.
- `objectLog` dùng `Helper.LogCommandV2` để trace SQL. ⚠ Trong `SinhLoDate` và `SinhImei`, điều kiện log là `if (true || ...)` → **luôn log** (hardcode `true`, có thể là sót debug). `SanPhamImei` log có điều kiện `IsSuperUser || IsCSKH`.
- IMEI map text trạng thái/kiểu nhập/trạng thái SP qua `StringHelper.GetDescriptionFromValue`; nối `SpHaCapGhiChu` với % hạ cấp.
- `SinhLoDate` convert `TenSanPham` sang không dấu (`StringHelper.utf8Convert1`) và giữ bản gốc ở `TenSanPhamGoc`.

### Sinh tự động — `SinhLoDateTuDongService` / `SinhImeiTuDongService`
- `GenerateIMEI(prefix, idDonVi, soLuong)`: mã = `{prefix}{DateTime.Now:ddMMyy}{counter}`. `counter` = max `SoTuTang` của đơn vị **trong ngày hiện tại** + 1; ≤ 999 → `PadLeft(3,'0')`, > 999 → để số nguyên. (Có `Random random` khai báo nhưng **không dùng**.)
- `ThemMoi` validate: TienTo bắt buộc, regex `^\d{1,2}$` (chỉ số, ≤ 2 ký tự); NgaySanXuat ≤ NgayHetHanSanPham; SoDangKy ≤ 255; LoSanPham bắt buộc & ≤ 255; check trùng `LoSanPham` theo đơn vị (bỏ qua nếu `IsBoQuaCheckTrungLoDate`).
- Khi `IsGenTuDong == true` (gen bulk): cộng `+4ms` vào `TimeStart` mỗi bản ghi để `NgayTao` khác nhau; insert qua `_context.BulkInsertAsync`.
- `SinhImeiTuDong.PreviewSinhImei`: chỉ sinh & trả mã, KHÔNG persist — dùng cho luồng sửa phiếu nhập đã hoàn thành (FE giữ client-side, BE add lúc submit + check trùng).
- `InDon`: set `TrangThaiIn = DaIn` cho các mã, `MarkDirty(nameof(TrangThaiIn))`.

### Tạo bản ghi IMEI/lô date thực tế (qua phiếu nhập)
`PhieuNhapService.cs`: khi tạo dòng phiếu, `phieuNhapSp.SinhImeiTuDong = item.SinhImeiTuDong == true || (gridInfo.SinhImeiTuDong && sanPham.IsSanPhamImei)`. Khi **phiếu nhập hoàn thành** (`DmKhoMaTinhTrang == TinhTrangPhieuNhapEnum.HoanThanh`) và SP có cờ IMEI + cờ sinh tự động → tạo `new SanPhamImei { TrangThai = ChoNhapKho ... }` (xem nhánh ~dòng 1938-2002). Lô date xử lý song song (`gridInfo.SinhLoDateTuDong`).

### XoaLoDate (SinhLoDateTuDongService)
- Tìm theo `Id + IdDonVi`. Nếu `IdPhieuNhap` có giá trị → load phiếu nhập; nếu phiếu `HoanThanh` → chặn xóa. Ngược lại `DeleteAsync`.

### NhapKhoImei (SanPhamImeiService)
- Tìm `SanPhamImei` theo `Imei + IdDonVi + IdChiNhanh` (lọc thêm `IdPhieu` nếu truyền). Phải đang `ChoNhapKho` → set `DaNhapKho`, `MarkDirty`, `PushLogsQueue(... NhapKhoImei, ThuCong)`.
- Log thao tác IMEI đẩy qua MassTransit (`PushLogsQueue` → consumer `CreateLogsSanPhamImei`) lưu MongoDB; cờ `isRevert` đánh dấu log phát từ flow revert (sửa/xóa phiếu hoàn thành).

---

## Bug-prone / lưu ý khi sửa
- **Log SQL luôn bật** ở `SinhLoDate`/`SinhImei` (`if (true || ...)`) → tốn IO/log thừa trên prod; cân nhắc khi tối ưu.
- **Quyền màn lô date dùng nhầm tên của IMEI**: `SanPhamLoDateController.TimTheoDieuKien` gắn `SanPhamImei_TimTheoDieuKien` (không có quyền riêng `SanPhamLoDate_...`). Sửa phân quyền cần để ý.
- **Search là stored procedure** — đổi field lọc/cột phải sửa cả SP trong DB (`PRC_SCRM_WarehouseProduct_*_Search`), không chỉ C#.
- **NCC trên IMEI là snapshot**, không cập nhật khi chuyển/xuất kho — đừng "đồng bộ lại" tránh phá thông tin nhập gốc.
- **Counter sinh mã reset theo ngày + theo đơn vị**: `MaxAsync(SoTuTang)` filter `NgayTao.Date == Today` → mã chỉ unique trong ngày/đơn vị; nếu nhiều request đồng thời cùng đơn vị có thể đụng counter (không có lock) → coi chừng trùng mã ở tải cao.
- **`IsGenTuDong` cộng 4ms** để phân biệt NgayTao — nếu bỏ sẽ có bản ghi cùng mốc thời gian, ảnh hưởng sort theo NgayTao.
- **IMEI chứa ký tự đặc biệt** (`/`): dùng `KiemTraImeiV2` (POST) thay `KiemTraImei` (GET param URL).
- **`utf8Convert1` ở SinhLoDate**: tên SP trả về đã bỏ dấu, giữ bản gốc ở `TenSanPhamGoc` — FE muốn hiển thị có dấu phải lấy `TenSanPhamGoc`.

---

## File source
BE — `D:\PROJECT\Git\_SourceForCode\Group2\scrm\WarehouseProduct\`
- `WarehouseProduct.API/Controllers/SinhLoDateTuDongController.cs`
- `WarehouseProduct.API/Controllers/SinhImeiTuDongController.cs`
- `WarehouseProduct.API/Controllers/SanPhamImeiController.cs`
- `WarehouseProduct.API/Controllers/SanPhamLoDateController.cs`
- `WarehouseProduct.Service/Generated/SinhLoDateTuDongService.cs`
- `WarehouseProduct.Service/Generated/SinhImeiTuDongService.cs`
- `WarehouseProduct.Service/Generated/SanPhamImeiService.cs`
- `WarehouseProduct.Service/Generated/PhieuNhapService.cs` (tạo IMEI/lô date khi hoàn thành phiếu)
- `WarehouseProduct.DataContext/Entity/SanPhamImei.cs` · `SinhImeiTuDong.cs` · `SinhLoDateTuDong.cs` · `SanPham.cs`
- `Libs/SB.Core/Contants.cs` (enums IMEI ~1325) · `Libs/Models.Core/Enum.cs` (TrangThaiIn ~936)
- Stored procedures: `PRC_SCRM_WarehouseProduct_SinhLoDateTuDong_Search`, `PRC_SCRM_WarehouseProduct_SinhImeiTuDong_Search`, `PRC_SCRM_WarehouseProduct_SanPhamImei_Search` (+ `_Api`)

FE — `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\`
- `pages/WarehouseProduct/view-sanpham-imei/`, `sinh-imei-tudong/`, `view-sanpham-lodate/`, `sinh-lo-date-tu-dong/`
- `services/warehouseproduct/sanPhamImei.service.ts`, `sinhImeiTuDong.service.ts`, `sanPhamLoDate.service.ts`, `sinhLoDateTuDong.service.ts`
