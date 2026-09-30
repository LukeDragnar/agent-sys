# Dev — Báo cáo kho

> CHỈ dev. Path BE gốc: `D:\PROJECT\Git\_SourceForCode\Group2\scrm` — FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend`.

## Tổng quan
Phần lớn báo cáo kho nằm ở service **WarehouseProduct** (controller `WarehouseReportController`, `TI_Kho`). Riêng **Báo cáo sản phẩm bán** (`ReportDonHangSanPhamBanHang`) nằm ở service **Report** (`OrderReportController`). Trong `Report.API` cũng có một `WarehouseReportController` nhưng hầu hết endpoint đã bị comment, chỉ còn `ReportWarehouseImport` / `ReportWarehouseImportGroup` còn active ở đó — endpoint thực dùng là bản trong `WarehouseProduct.API`.

Mọi báo cáo theo cùng khuôn: build `List<SugarParameter>` → gọi stored procedure qua `_dbDapper`/`OpenNewConnectionOrDefault(...)` → `ObjectHelper.FillCollection<T>(ds.Tables[i])` cho từng bảng (detail + summary [+ total]) → enrich tên nhóm SP / đơn vị tính → trả `MethodResult<T>` kèm total.

## Màn hình & route (FE)
File route: `src/app/pages/Report/report-routing.module.ts`

| Báo cáo | path | Component |
|---|---|---|
| Nhập–xuất kho | `warehouse-import-export` | `ReportWarehouseImportExportComponent` |
| Nhập kho | `warehouse-import` | `ReportWarehouseImportComponent` |
| Xuất kho (+ xuất kho theo nhân viên) | `warehouse-export` | `ReportWarehouseExportComponent` |
| Sản phẩm bán | `san-pham-ban-hang` | `ReportBaoCaoSanPhamBanHangComponent` |
| Sản phẩm bán theo ngày | `report-san-pham-ban-theo-ngay` | `ReportSanPhamBanTheoNgayComponent` |
| Tồn kho | `bao-cao-ton-kho` | `ReportBaoCaoTonKhoComponent` |
| Quay vòng sản phẩm | `baocao-quayvong-sp` | `ReportWarehouseQuayvongSanphamComponent` |

> "Báo cáo xuất kho theo nhân viên" không phải route riêng — là chế độ xem (gộp/group) trong màn `warehouse-export` (tiêu đề `tlt-bao-cao-xuat-kho-nv` "Báo cáo xuất kho bởi nhân viên" trong `report-warehouse-export.component.html`).

FE service gọi API:
- `src/app/services/warehouseproduct/report-warehouse.service.ts` — base `${SERVICE_API_WarehouseProduct}api/WarehouseReport`. Có cả nhóm method `V2*` trỏ về cùng endpoint (alias lịch sử).
- `src/app/services/report/report-warehouse.service.ts` — phần thuộc Report service / sản phẩm bán.

## Controller & endpoints (BE)

### WarehouseProduct.API/Controllers/WarehouseReportController.cs (chính)
`[TienIchAttribute("WarehouseReportController","Báo cáo kho vận", MaTienIchSCRM.TI_Kho)]`, route `api/WarehouseReport`.

| Endpoint (POST) | Service method | Quyền (SBAuthorize code) |
|---|---|---|
| `ReportWarehouseImportExport` | `ReportWarehouseImportExport` | ReportWarehouseImportExport |
| `ReportWarehouseImportExportPrivate` | `ReportWarehouseImportExportPrivate` | ReportWarehouseImportExportPrivate |
| `ExportExcelWarehouseImportExport` | `ExportExcelWarehouseImportExport_v2` | ExportExcelWarehouseImportExport |
| `ReportWarehouseExport` | `ReportWarehouseExport` | ReportWarehouseExport |
| `ReportWarehouseExportGroup` | `ReportWarehouseExportGroup` | ReportWarehouseExportGroup |
| `ExportExcelWarehouseExport` | `ExportExcelWarehouseExport` | ExportExcelWarehouseExport |
| `ReportWarehouseImport` | `ReportWarehouseImport` | ReportWarehouseImport |
| `ReportWarehouseImportGroup` | `ReportWarehouseImportGroup` | ReportWarehouseImportGroup |
| `ReportWarehouseQuayVong` | `ReportWarehouseQuayVongSanPham` | ReportWarehouseImportExport |
| `ReportWarehouseSanPhamBanTheoNgay` | `ReportWarehouseSanPhamBanTheoNgay` | ReportWarehouseSanPhamBanTheoNgay |
| `ExportExcelReportWarehouseSanPhamBanTheoNgay` | `ExportExcelReportWarehouseSanPhamBanTheoNgay` | ExportExcelReportWarehouseSanPhamBanTheoNgay |
| `ReportWarehouseBaoCaoTonKho` | `ReportWarehouseBaoCaoTonKho` | ReportWarehouseBaoCaoTonKho |
| `ExportExcelReportWarehouseBaoCaoTonKho` | `ExportExcelReportWarehouseBaoCaoTonKho` | ExportExcelReportWarehouseBaoCaoTonKho |
| `XemChiTietLichSuImportExport` | `GetDetailSanPhamNhapXuat` | (SBAuthorize trống) |
| `ExportExcelXemChiTietLichSuImportExport` | `ExportExcelXemChiTietLichSuImportExport` | (trống) |
| `ReportTongTheCuaHang` | `ReportTongTheCuaHang` | (trống) |

### Report.API/Controllers/OrderReportController.cs (sản phẩm bán)
| Endpoint (POST) | Service method | Quyền |
|---|---|---|
| `ReportDonHangSanPhamBanHang` | `ReportDonHangSanPhamBanHang` | OrderReport_ReportDonHangSanPhamBanHang |
| `ReportDonHangSanPhamBanHangGroup` | `ReportDonHangSanPhamBanHangGroup` | OrderReport_ReportDonHangSanPhamBanHangGroup |

## Entities / DTO & Stored Procedures

### DTO filter
`Libs/Models.Core/DTO/WarehouseProduct.Client/WarehouseReport/WarehouseReportDto.cs`
- `WarehouseReportImportExportFilterDto : GridInfo` — IdChiNhanh, IdNhomSanPham, IdSanPhamCha, IdSanPham, IdKho, GroupType(=1), NgayBatDau, NgayKetThuc, Keyword, ListIdNhanSanPham.
- `WarehouseReportQuayVongFilterDto` — như trên + `TrangThaiBan`.
- (Import / Export dùng `WarehouseReportImportFilterDto` / `WarehouseReportExportFilterDto` cùng nhóm field.)
- `ReportWarehouseBaoCaoTonKhoReq` — IdChiNhanh nullable, IdNhomSanPham, IdSanPhamCha, IdSanPham, IdKho, Keyword, GroupType, ListIdNhanSanPham, PageInfo (Page/PageSize). **Không** có Ngay (tồn tức thời).
- Sản phẩm bán: `Libs/Models.Core/DTO/Order.Client/Report/ReportDonHangSanPham_BanHangDto.cs` → `ReportDonHangSanPhamBanHangFilterDto : GridInfo`.
- Sản phẩm bán theo ngày: `ReportSanPhamNgayFilter`.

### Stored procedures (đang chạy — bản mới nhất)
| Báo cáo | Stored procedure |
|---|---|
| Nhập–xuất kho | `sp_ReportWarehouse_Import_Export_V2_20260505` |
| Nhập kho | `sp_ReportWarehouse_Import_V2` |
| Nhập kho (group) | `sp_ReportWarehouse_Import_Group_V2` |
| Xuất kho | `sp_ReportWarehouse_Export_V2` |
| Xuất kho (group/tổng quan) | `sp_ReportWarehouse_Export_Group_V2` |
| Quay vòng sản phẩm | `PRC_WarehouseProduct_QuayVong_SP_20260505` |
| Sản phẩm bán theo ngày | `sp_ReportWarehouse_SanPhamBanTheoNgay_V2_20260505` |
| Tồn kho | `sp_ReportWarehouse_BaoCaoTonKho` |
| Chi tiết nhập/xuất | `sp_ReportWarehouse_Import_Export_Detail_SanPham_V2_20251128` |
| Sản phẩm bán (đơn hàng) | `sp_ReportDonHangSanPham_BanHang` / `..._Group` |
| Lọc theo nhãn SP (phụ) | `PRC_SCRM_WarehouseProduct_NhanSanPham` |

> Trong code còn nhiều dòng comment đặt SP các phiên bản cũ (`_V2`, `_V2_20251128`); chỉ dòng gán cuối (không comment) là SP thực thi. Khi đổi SP nhớ sửa đúng dòng active.

## Luồng code (service — chung)
File: `WarehouseProduct/WarehouseProduct.Service/Generated/WarehouseReportService.cs` (~2835 dòng).
1. `CheckGioiHanTruyCap` (POST sang Report service) — `ConfigCode.SO_USER_READ_REPORT_KHO` + bước 2 (NHAP / XUAT / NHAP_XUAT / TON_KHO / SAN_PHAM_THEO_NGAY). False → statusCode 250 "Chưa đến lượt truy cập".
2. `CheckCauHinhKhoangThoiGianChoPhepTimKiem` — vượt → lỗi "Chỉ được xem dữ liệu trong khoảng 'N ngày'". (Tồn kho đã comment, không check.)
3. Nếu `ListIdNhanSanPham` có giá trị → SP `PRC_SCRM_WarehouseProduct_NhanSanPham` → ghép `strListIdSanPhambyNhanSP` (";"-join). Rỗng → `null`.
4. Ép chi nhánh: `if (!IsAdmin && !IsSuperUser && !IsTest && idChiNhanh==Guid.Empty) idChiNhanh = userLogin.IdChiNhanh;` (Import/Export còn ép cả khi chọn chi nhánh khác).
5. OrderBy/Dir lấy từ `input.Sorts[0]`; default mỗi báo cáo khác nhau (ImportExport: "TongNhap" dir 2; Import/Export: "SoLuong"; QuayVong: "TongXuat").
6. Gọi SP qua `OpenNewConnectionOrDefault(ds, sp, listParam, setNewConnection)`.
7. `FillCollection`: ImportExport/Import/Export → Tables[0]=detail, Tables[1]=summary. QuayVong → Tables[0]=detail, Tables[1]=total, Tables[2]=summary. TonKho → Tables[0]=detail, Tables[1]=summary (TotalRecord).
8. Enrich: `GetOrSetCache_NhomSanPham_DonVi` (tên nhóm) + `DropdownDmSanPham(DonViDem)` (tên đơn vị tính). `IdDonViTinhHienTai = IdDonViSuDungLoDate ?? IdDonViTinhNhoNhat`.
9. Log câu lệnh (`Helper.LogCommandV2`) khi `IsSuperUser || IsCSKH || isShowQuery/setNewConnection`.
10. `finally`: `ReleaseLockedTicket(...)` giải phóng vé truy cập đồng thời.

## Bug-prone
- **`strListIdSanPhambyNhanSP = null` khi nhãn không khớp SP nào** → SP nhận null = "không lọc theo nhãn", dễ hiểu nhầm là phải ra rỗng. Kiểm tra logic SP nếu khách báo "lọc nhãn vẫn ra hết".
- **Nhiều dòng gán SP comment chồng nhau** — dễ sửa nhầm dòng đã comment; chỉ dòng active mới chạy.
- **Ép chi nhánh ở Import/Export** ghi đè cả khi user chọn chi nhánh khác → user thắc mắc "chọn chi nhánh khác vẫn ra chi nhánh mình". Đúng theo phân quyền, không phải bug.
- **Tồn kho không check khoảng thời gian** (code đã comment) — đừng giả định có ràng buộc ngày.
- **`dataSummary.FirstOrDefault()` / `dataSummary[0]`** — nếu SP không trả bảng summary đúng cấu trúc sẽ ra "Không có bản ghi nào!" hoặc null-ref; khi đổi SP phải giữ đúng số bảng output.
- **Đơn vị tính:** sai bảng quy đổi (`BangQuyDoiJson`) hoặc `IdDonViSuDungLoDate` null → số lượng quy đổi lệch.
- **Hai controller cùng tên `WarehouseReportController`** (Report.API vs WarehouseProduct.API) — endpoint thực dùng nằm ở WarehouseProduct.API; bản Report.API hầu hết đã comment.
- **`ExportExcel...ImportExport` gọi `_v2`** (`ExportExcelWarehouseImportExport_v2`) chứ không phải bản cũ — chú ý khi sửa export.

## File source
BE:
- `WarehouseProduct/WarehouseProduct.API/Controllers/WarehouseReportController.cs`
- `WarehouseProduct/WarehouseProduct.Service/Generated/WarehouseReportService.cs`
- `Report/Report.API/Controllers/WarehouseReportController.cs` (phần lớn comment)
- `Report/Report.API/Controllers/OrderReportController.cs` (sản phẩm bán)
- `Report/Report.Service/Generated/OrderReportService.cs` (`ReportDonHangSanPhamBanHang[Group]`)
- `Libs/Models.Core/DTO/WarehouseProduct.Client/WarehouseReport/WarehouseReportDto.cs`
- `Libs/Models.Core/DTO/Order.Client/Report/ReportDonHangSanPham_BanHangDto.cs`

FE:
- `src/app/pages/Report/report-routing.module.ts`
- `src/app/pages/Report/report-warehouse-import-export/`, `report-warehouse-import/`, `report-warehouse-export/`, `report-bao-cao-ton-kho/`, `report-warehouse-quayvong-sanpham/`, `report-san-pham-ban-theo-ngay/`, `report-bao-cao-san-pham-ban-hang/`
- `src/app/services/warehouseproduct/report-warehouse.service.ts`
- `src/app/services/report/report-warehouse.service.ts`
