# Dev — Quản lý tồn (dự báo)

> Service BE: **WarehouseProduct** (KhoChiTiet read-only + SanPhamGioiHanTonKho cấu hình). FE module **WarehouseProduct** (Angular 15 + ng-zorro). Chỉ dành cho dev.
> Registry: `projects/scrm/registry/kho/KhoChiTietController.md`, `projects/scrm/registry/mua-hang/SanPhamGioiHanTonKhoController.md`.

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Báo cáo tồn kho đơn vị (xem + dự báo hết hàng) | `warehouse-product/baocao-tonkho-hethong` | `BaocaoTonkhoDonviComponent` |
| Kiểm tra tồn kho (đơn vị + chi nhánh) | `warehouse-product/kiemtra-tonkho-hethong` | `KiemtraTonkhoComponent` (con: `kiemtra-tonkho-chinhanh`, `baocao-tonkho-donvi`, `checklist-taophieu`) |
| Quản lý tồn kho (lưới tồn theo SP) | `quanly-kho/quanly-kho-hangban` | `QuanlyKhoHangbanComponent` |
| Giới hạn tồn kho (cấu hình ngưỡng) | `warehouse-product/gioi-han-ton-kho` | `GioiHanTonKhoComponent` (con: create / initial / import) |
| Tạo đơn đặt hàng từ dự báo | (popup trong baocao-tonkho-donvi) | `tao-don-dat-hang.component.ts` |

- Route warehouse-product: `WarehouseProduct/warehouseProduct-routing.module.ts:217` (baocao-tonkho-hethong), `:213` (kiemtra-tonkho-hethong), `:221` (gioi-han-ton-kho), `:241` (quan-ly-don-dat-hang).

## Endpoint / Controller (trace registry)

**Báo cáo / dự báo tồn — `KhoChiTietController`** (read-only, service WarehouseProduct):
| MaTinhNang (key) | Route | Mục đích |
|---|---|---|
| `KhoChiTiet_ReportTonKhoDonVi` | `POST api/KhoChiTiet/ReportTonKhoDonVi` | Báo cáo tồn kho & sản lượng bán đơn vị. Non-admin **bị ép `IdChiNhanh = user.IdChiNhanh`** (invariant bảo mật). Store `PRC_WarehouseProduct_BaoCaoTonKho`. |
| `KhoChiTiet_GetTonKhoSanPham_ViewChiNhanh_DuBaoHetHang` | `POST .../GetTonKhoSanPham_ViewChiNhanh_DuBaoHetHang` | Set `IsDuBaoHetHang=true`; nếu `IsTinhGioiHanTonKhoTuDong` bật → bắt buộc `KieuKhoangNgay`. Store `PRC_WarehouseProduct_SoLuongTonKhoSanPham_TheoChiNhanh`. |
| `KhoChiTiet_ExportExcelReportTonKhoDonVi` | `POST .../ExportExcelReportTonKhoDonVi` | Xuất `BaoCaoTonKho.xlsx`, **chỉ admin** (DV-AD-TONG/AD-CN/AD-MCN); non-admin ép `IdChiNhanh`. |
| `SBAuthorize` (any login) | `POST .../ExportExcelReportTonKhoDonVi_CheckDieuKien` | Validate trước export: OffSet≥0, PageSize>0, PageSize≤10.000. |
| `KhoChiTiet_TimTheoDieuKien` | `POST .../TimTheoDieuKien` | Lưới Quản lý tồn kho (bắt buộc `ListIdNhomSanPham`). |
| `KhoChiTiet_GetSoLuongGioiHanTonKho_TheoSanPham` | `POST .../GetSoLuongGioiHanTonKho_TheoSanPham` | Store `PRC_WarehouseProduct_GioiHanTonKho_TheoSanPham`. |

**Cấu hình ngưỡng — `SanPhamGioiHanTonKhoController`** (service WarehouseProduct, role mặc định `DV-AD-TONG, DV-AD-CN, DV-MCN`):
| MaTinhNang (key) | Route | Mục đích |
|---|---|---|
| `SanPhamGioiHanTonKho_SearchByConditions` | `POST api/SanPhamGioiHanTonKho/SearchByConditions` | Lưới ngưỡng (SP `PRC_SCRM_WarehouseProduct_GioiHanTonKho_Search`). Non-admin ép `ListChiNhanh=[user.IdChiNhanh]`. |
| `SanPhamGioiHanTonKho_Create` | `POST .../Create` | Thêm ≤100 dòng; upsert `CreatePrivate` (isImport=false). |
| `SanPhamGioiHanTonKho_Update` | `POST .../Update` | Ép `IsApDungTatCaChiNhanh=false`, bắt buộc `IdChiNhanh`. |
| `SanPhamGioiHanTonKho_Delete` | `POST .../Delete/{id:Guid}` | Hard delete; check đơn vị + chi nhánh. |
| `SanPhamGioiHanTonKho_Initial` | `POST .../Initial` | Khởi tạo hàng loạt (quét SP đơn vị, paging 400). |
| `SanPhamGioiHanTonKho_Import` / `_DownloadFileTemplateImport` / `_DownloadDataWrong` | `POST .../Import`, `/DownloadFileTemplateImport`, `GET .../DownloadDataWrong/{id}` | Import ≤2.000 dòng; file lỗi. |
| `SBAuthorize()` trống | `POST .../GetListKhoangNgayTinhGiaTriTrungBinh` | Dropdown 13 mục khoảng ngày (chỉ cần đăng nhập). |

## Entities
- **`KhoChiTiet`** (read): `SoLuong`, `SoLuongChoXuat`, `GiaNhapTrungBinh`, `SoLuongDungTichChiTiet`, `IdKho/IdChiNhanh/IdDonVi/IdSanPham`; join `SanPham`, `Kho`, `SanPhamImei`, `SanPhamCauThanh`, `CauHinh`. Controller KHÔNG ghi KhoChiTiet.
- **`SanPhamGioiHanTonKho : BaseEntity`** — 1 bản ghi = (IdSanPham × IdChiNhanh × IdDonVi). Trường: TonKhoToiThieu/ToiDa/KhanCap, SoNgayTonKho*, SoLuongBanTrungBinhNhoNhat, KhoangNgayTinhGiaTriTrungBinh, IsApDungTatCaChiNhanh.
- Enum `EnumKhoangNgayTinhGiaTriTrungBinh` (1..13): `Libs/Models.Core/Enum/Warehouse/WarehouseEnum.cs:145`.

## Luồng code
**Báo cáo/dự báo** (`KhoChiTietService`):
- `ReportTonKhoDonVi` → ép chi nhánh non-admin → store `PRC_WarehouseProduct_BaoCaoTonKho` → serialize DataSet.
- `GetTonKhoSanPham_ViewChiNhanh(_DuBaoHetHang)` (`KhoChiTietService.cs:868+`): `if (true)` ép luôn dùng SQL (bỏ nhánh Elastic vì cần dữ liệu Giới hạn tồn kho); dự báo set `IsDuBaoHetHang=true`, tính khoảng ngày khi `IsTinhGioiHanTonKhoTuDong`.
- `XuatExcelReportTonKhoDonVi`: query → GemBox → `BaoCaoTonKho.xlsx` (ép chi nhánh non-admin).

**Cấu hình ngưỡng** (`SanPhamGioiHanTonKhoService` + `SanPhamGioiHanTonKhoImportService`):
- Mọi ghi quy về `CreatePrivate` = upsert theo (IdDonVi, IdSanPham, IdChiNhanh): tồn tại → BulkUpdate (giữ NgayTao/NguoiTao), chưa có → BulkInsert.
- Phân quyền chi nhánh ở tầng service: `FunctionCommon.CheckUserLoginIsRoleAdmin(userLogin)==false` → ép `IsApDungTatCaChiNhanh=false` + chỉ `userLogin.IdChiNhanh`.
- Validate ngưỡng dùng chung cho Create/Update/Initial (null/âm/min>max/khoảng ngày ∉ [1..13]).
- Import: đọc 11 cột từ dòng 4, match SP/chi nhánh theo TÊN, quy đổi ĐVT (isImport=true) theo `BangQuyDoiJson`.

## Bug-prone / lưu ý (path:line)
> Chỉ ghi nhận — KHÔNG sửa code ở session này.
- **Update lưới Giới hạn tồn kho dễ fail validate**: FE `onUpdate` chỉ gửi 4 field (idSanPham/idChiNhanh/tonKhoToiThieu/tonKhoToiDa) nhưng service validate "không null" cả các trường SoNgay*/TonKhoKhanCap/SoLuongBanTB/KhoangNgay (`SanPhamGioiHanTonKhoService.cs:91-184`) → payload thiếu → Update lưới có thể luôn lỗi. Cần kiểm chứng hợp đồng FE↔BE.
- **Export tồn kho lệch đường (rủi ro lệch số)**: lưới Quản lý tồn kho (#1) đi store `sp_ReportWarehouse_KhoChiTiet_Search` (có áp `@AnGiaNhap/@CheGiaNhap`), còn `XuatExcelQuanLyTonKho` (`KhoChiTietService.cs:704-838`) đi EF/LINQ KHÔNG áp logic ẩn giá → file Excel có thể lệch số liệu & lộ giá nhập. (Báo cáo tồn kho đơn vị #11/#13 dùng chung store nên ít rủi ro hơn.)
- **Lỗi DB bị nuốt khi ghi ngưỡng**: BulkInsert/BulkUpdate trong `try/catch{}` rỗng (`SanPhamGioiHanTonKhoService.cs:262-272 / 347-364`); Create báo "Chưa hoàn thành tiến độ N/M" dựa trên đếm.
- **Initial luôn báo "Hoàn thành!"**: `soBanGhiDaHoanThanh += soBanGhiDaHoanThanh;` (`:577`, đáng lẽ `+= total`) → biến đếm luôn 0; Initial trả success cứng kể cả khi mọi batch fail.
- **Dead code / điều kiện thừa**: `SanPhamGioiHanTonKhoService.cs:627` `if(... || true)`; `:664-665` `return ...` rồi `throw;`.
- **File lỗi import sai cột**: `SanPhamGioiHanTonKhoImportService.cs:703-704` ghi 2 lần `SoNgayTonKhoToiDa` (cột 7 & 8) → cột "Số ngày tối thiểu" sai.
- **Lệch text khoảng ngày**: map `KhoangNgayTinhGiaTriTrungBinh` theo chuỗi hoa "1 Ngày trước"/... (`:496+`) khác `[Description]` enum thường → lệch hoa/thường báo "không hợp lệ".
- **Anonymous endpoint** `api/KhoChiTiet/CheckTonKho` = `[AllowAnonymous]` (`KhoChiTietController.cs:196-202`) — không phải UI dự báo nhưng cùng controller, lưu ý bề mặt tấn công.
- **License GemBox hardcode** trong code export (`KhoChiTietService.cs:770`).

## File source (path:line)
- BE controller báo cáo: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\WarehouseProduct\WarehouseProduct.API\Controllers\KhoChiTietController.cs` (ReportTonKhoDonVi, ExportExcelReportTonKhoDonVi `:127-179`, DuBaoHetHang).
- BE service báo cáo: `...\WarehouseProduct\WarehouseProduct.Service\Generated\KhoChiTietService.cs` (`:133-220` lưới, `:704-838` export, `:868+` dự báo).
- BE controller ngưỡng: `...\WarehouseProduct\WarehouseProduct.API\Controllers\SanPhamGioiHanTonKhoController.cs:110` (dropdown khoảng ngày).
- BE service ngưỡng: `...\WarehouseProduct\WarehouseProduct.Service\Generated\SanPhamGioiHanTonKhoService.cs` (`:91-184` validate, `:577` Initial, `:627`/`:664`).
- BE import ngưỡng: `...\WarehouseProduct.Service\Generated\Imports\SanPhamGioiHanTonKhoImportService.cs:496+ / :703-704`.
- DTO/enum: `Libs/Models.Core/DTO/WarehouseProduct.Client/SanPham/SanPhamGioiHanTonKhoDto.cs`, `Libs/Models.Core/Enum/Warehouse/WarehouseEnum.cs:145`.
- Stored procs (DB, không trong repo): `PRC_WarehouseProduct_BaoCaoTonKho`, `PRC_WarehouseProduct_SoLuongTonKhoSanPham_TheoChiNhanh`, `PRC_SCRM_WarehouseProduct_GioiHanTonKho_Search`, `sp_ReportWarehouse_KhoChiTiet_Search`.
- FE báo cáo: `...\scrm_frontend\src\app\pages\WarehouseProduct\quanly-kho\kiemtra-tonkho\baocao-tonkho-donvi\baocao-tonkho-donvi.component.ts` (+ popup `tao-don-dat-hang`).
- FE ngưỡng: `...\WarehouseProduct\gioi-han-ton-kho\gioi-han-ton-kho.component.ts` (+ create/initial/import); service `services\warehouseproduct\gioi-han-ton-kho.service.ts`.
- FE routing: `...\WarehouseProduct\warehouseProduct-routing.module.ts:217/:221`.
