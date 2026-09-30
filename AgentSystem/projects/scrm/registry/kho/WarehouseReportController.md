---
controller: WarehouseReportController
module: kho
service: WarehouseProduct
man_hinh_ui: "Báo cáo (menu Báo cáo / report) > nhóm Kho vận: Báo cáo nhập-xuất kho (/report/warehouse-import-export), Báo cáo nhập kho (/report/warehouse-import), Báo cáo xuất kho (/report/warehouse-export), Báo cáo tồn kho (/report/bao-cao-ton-kho), SP bán theo ngày (/report/report-san-pham-ban-theo-ngay), Quay vòng SP (/report/baocao-quayvong-sp). Bản Private + ReportTongTheCuaHang = widget Dashboard CEO (dashboard-baocao), không có form lọc riêng."
muc_dich: "Báo cáo kho vận thuần-đọc: nhập / xuất / nhập-xuất gộp / tồn kho / sản phẩm bán theo ngày / quay vòng SP, kèm export Excel. Controller VIEW/REPORT — không ghi dữ liệu kho."
alias: [bao cao kho, báo cáo kho, bao cao nhap xuat kho, bao cao ton kho, bao cao nhap kho, bao cao xuat kho, san pham ban theo ngay, quay vong san pham, warehouse report, import export report, ton kho]
auth: mixed
state_machine: []
deprecated: false
entities: [KhoChiTiet, Kho, SanPham, NhomSanPham, DonViTinh]
stored_procs: [sp_ReportWarehouse_Import_Export_V2_20260505, sp_ReportWarehouse_Import_V2, sp_ReportWarehouse_Import_Group_V2, sp_ReportWarehouse_Export_V2, sp_ReportWarehouse_Export_Group_V2, sp_ReportWarehouse_BaoCaoTonKho, sp_ReportWarehouse_SanPhamBanTheoNgay_V2_20260505, sp_ReportWarehouse_Import_Export_Detail_SanPham_V2_20251128, PRC_WarehouseProduct_QuayVong_SP_20260505, SCRM_Report_BaoCaoTongTheCuaHang, PRC_SCRM_WarehouseProduct_NhanSanPham]
depends: [ReportService, CommonService, CauHinhService, CommonCacheService, ClientHelper, CoreService/ChiNhanh, CustomerService/KhachHang]
cross_module_in: [Dashboard CEO/dashboard-baocao (gọi *Private + ReportTongTheCuaHang)]
cross_module_out: []
---

# WarehouseReportController — Báo cáo kho vận (nhập / xuất / tồn — read-only)

> Controller VIEW/REPORT thuần đọc. **Mỗi action = 1 stored procedure** (đường chính là SQL), service chỉ build `SugarParameter` (DonViId, ép IdChiNhanh theo quyền, che giá nhập) rồi serialize `DataSet`. **KHÔNG có endpoint nào đi EF cho dữ liệu báo cáo** — EF chỉ dùng phụ trợ (đọc `Kho` để map tên ở chi tiết). Hầu hết endpoint gọi `ReportService/CheckGioiHanTruyCap` chặn truy cập đồng thời + `CommonService.CheckCauHinhKhoangThoiGianChoPhepTimKiem` giới hạn khoảng ngày → vượt ngưỡng trả lỗi/`statusCode 250`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo cáo nhập-xuất kho (lưới) | "Báo cáo nhập/xuất kho vận" / nút Tìm | `POST api/WarehouseReport/ReportWarehouseImportExport` | `ReportWarehouseImportExport` | public | màn /report/warehouse-import-export | Check lượt truy cập + khoảng ngày. Non-admin/non-super/non-test ép `IdChiNhanh=user.IdChiNhanh` khi rỗng. `ListIdNhanSanPham` → chạy phụ `PRC_SCRM_WarehouseProduct_NhanSanPham` lấy danh sách IdSanPham. Trả grid + summary + total; enrich TenNhomSanPham (cache), TenDonViTinhHienTai. `GroupType` (1=theo SP / khác=theo kho-SP) ảnh hưởng view detail. | **SP** `sp_ReportWarehouse_Import_Export_V2_20260505` (table0=data, table1=summary; null summary → "Không có bản ghi nào!") | SP name hardcode kèm ngày (`_20260505`) — nhánh cấu hình `Allows_Use_KhoChiTietSync` đã bị comment, luôn dùng bản V2 mới nhất. |
| 2 | Xuất Excel nhập-xuất kho | Nút "Xuất Excel" (nhập-xuất) | `POST api/WarehouseReport/ExportExcelWarehouseImportExport` | `ExportExcelWarehouseImportExport` | public | — | Gọi `ExportExcelWarehouseImportExport_v2` (bản cũ `ExportExcelWarehouseImportExport` đã comment ở controller). Lỗi → BadRequest; OK → file `XuatNhapKho.xlsx` (GemBox). | **SP** (cùng nguồn #1, build excel từ DataSet) | Controller gọi `_v2`; method non-v2 còn trong service nhưng không nối UI. |
| 3 | Báo cáo nhập kho (lưới) | "Báo cáo nhập kho vận" | `POST api/WarehouseReport/ReportWarehouseImport` | `ReportWarehouseImport` | public | /report/warehouse-import | Như #1. **Che giá nhập**: nếu user KHÔNG (admin/CSKH/adminTong) và role ∈ `CauHinh.IdRoles_Che_GiaNhap` → set `TongChiPhi=0`, `GiaNhapSanPham=0`, `IsCheThongTinGiaNhap=true`, Summary.TongChiPhi=0. | **SP** `sp_ReportWarehouse_Import_V2` | — |
| 4 | Báo cáo nhập kho tổng quan | "Báo cáo nhập kho tổng quan" | `POST api/WarehouseReport/ReportWarehouseImportGroup` | `ReportWarehouseImportGroup` | public | — | Như #3 (gộp nhóm, ít param hơn: bỏ IdSanPhamCha/IdSanPham). Cũng áp che giá nhập. | **SP** `sp_ReportWarehouse_Import_Group_V2` | Điều kiện che giá ở đây viết `if (!IsAdmin \|\| IsCSKH != true \|\| !IsAdminTong())` — logic OR khác #3 (AND) → **rủi ro lệch hành vi che giá** giữa lưới nhập và lưới nhập tổng quan. |
| 5 | Báo cáo xuất kho (lưới) | "Báo cáo xuất kho vận" | `POST api/WarehouseReport/ReportWarehouseExport` | `ReportWarehouseExport` | public | /report/warehouse-export | Check lượt + khoảng ngày; ép IdChiNhanh non-admin. Param riêng: `IdDmPhanLoaiXuat`, `TypeSearchDonHang`. Trả grid + total. | **SP** `sp_ReportWarehouse_Export_V2` | — |
| 6 | Báo cáo xuất kho tổng quan | "Báo cáo xuất kho tổng quan" | `POST api/WarehouseReport/ReportWarehouseExportGroup` | `ReportWarehouseExportGroup` | public | — | Như #5, gộp nhóm. (Không gọi CheckGioiHanTruyCap, chỉ check khoảng ngày.) | **SP** `sp_ReportWarehouse_Export_Group_V2` | — |
| 7 | Báo cáo tồn kho (lưới) | "Báo cáo tồn kho" | `POST api/WarehouseReport/ReportWarehouseBaoCaoTonKho` | `ReportWarehouseBaoCaoTonKho` | public | /report/bao-cao-ton-kho | Check lượt truy cập (CheckCauHinhKhoangThoiGian **đã comment** — không giới hạn ngày). `IdChiNhanh` rỗng → Guid.Empty (KHÔNG ép theo user như các báo cáo khác). `ListIdNhanSanPham` → SP nhãn SP. `isShowQuery` quyết định có trả câu query để hiển thị. | **SP** `sp_ReportWarehouse_BaoCaoTonKho` | KHÔNG ép `IdChiNhanh` theo user (khác #1/#3/#5) → tồn kho có thể xem chéo chi nhánh; xác nhận đây là chủ ý (báo cáo tồn toàn đơn vị) hay thiếu guard. |
| 8 | Xuất Excel báo cáo tồn kho | Nút "Xuất Excel" (tồn kho) | `POST api/WarehouseReport/ExportExcelReportWarehouseBaoCaoTonKho` | `ExportExcelReportWarehouseBaoCaoTonKho` | public | — | Lỗi → BadRequest; OK → file `Baocaotonkho.xlsx`. | **SP** (cùng nguồn #7) | — |
| 9 | SP bán theo ngày (lưới) | "Báo cáo sản phẩm bán theo ngày" | `POST api/WarehouseReport/ReportWarehouseSanPhamBanTheoNgay` | `ReportWarehouseSanPhamBanTheoNgay` | public | /report/report-san-pham-ban-theo-ngay | Check lượt + khoảng ngày. `CheckUserIsAdminViewStore` quyết định view admin. | **SP** `sp_ReportWarehouse_SanPhamBanTheoNgay_V2_20260505` | SP name hardcode kèm ngày; nhánh cấu hình V2 đã comment. |
| 10 | Xuất Excel SP bán theo ngày | Nút "Xuất Excel" (SP bán theo ngày) | `POST api/WarehouseReport/ExportExcelReportWarehouseSanPhamBanTheoNgay` | `ExportExcelReportWarehouseSanPhamBanTheoNgay` | public | — | OK → file `SanPhamTheoNgay.xlsx`. | **SP** (cùng nguồn #9) | — |
| 11 | Quay vòng sản phẩm (lưới) | "Báo cáo quay vòng SP" | `POST api/WarehouseReport/ReportWarehouseQuayVong` | `ReportWarehouseImportExport` (key dùng lại) | public | /report/baocao-quayvong-sp | Check lượt + khoảng ngày; ép IdChiNhanh non-admin. Param riêng `TrangThaiBan`. SP trả 3 table: data / total / summary. | **SP** `PRC_WarehouseProduct_QuayVong_SP_20260505` | Quyền key trùng `ReportWarehouseImportExport` (không có key riêng cho quay vòng) → khi map quyền lưu ý. |
| 12 | Xem chi tiết lịch sử nhập/xuất 1 SP | Nút "Xem chi tiết" (drill-down từ lưới #1) | `POST api/WarehouseReport/XemChiTietLichSuImportExport` | `SBAuthorize` (any login) | public | popup chi tiết | `[SBAuthorize()]` trống (không gắn key riêng). Non-(admin/super/CSKH) ép `IdChiNhanh=user.IdChiNhanh`. Map TuKho/DenKho (EF đọc `Kho`), tên chi nhánh (CoreService), NCC: gắn "(NCC)"/"(KH)" qua CustomerService. | **SP** `sp_ReportWarehouse_Import_Export_Detail_SanPham_V2_20251128` (dùng `_dbDapper.Ado.UseStoredProcedure`) | (a) bug copy-paste L2340 `denCn != denCn` luôn false + tham chiếu `tuCn.TenChiNhanh` thay vì `denCn` → **TenDenChiNhanh sai/nguy cơ NRE**. (b) Comment ghi báo cáo này từng lỗi do `IsDaHuyTrongDonHang` update sau khi phiếu hoàn thành làm NgayCapNhat đổi. |
| 13 | Xuất Excel chi tiết lịch sử nhập/xuất | Nút "Xuất Excel" (popup chi tiết) | `POST api/WarehouseReport/ExportExcelXemChiTietLichSuImportExport` | `SBAuthorize` (any login) | public | — | OK → file `Lichsunhapxuatsanpham.xlsx`. | **SP** (cùng nguồn #12) | — |

## Endpoint nội bộ (Dashboard CEO — không có form lọc riêng)
- [internal/UI-widget] Dashboard CEO (`dashboard-baocao`) gọi các bản *Private* — chỉ truyền IdChiNhanh + TuNgay/DenNgay, fix `PageSize=3`, rồi gọi lại đúng handler công khai tương ứng:
  - `POST .../ReportWarehouseImportExportPrivate` (key `ReportWarehouseImportExportPrivate`) → `ReportWarehouseImportExport` (#1).
  - `POST .../ReportWarehouseExportPrivate` (key `ReportWarehouseExportPrivate`) → `ReportWarehouseExport` (#5).
  - `POST .../ReportWarehouseImportPrivate` (key `ReportWarehouseImportPrivate`) → `ReportWarehouseImport` (#3).
- [internal] `POST .../ReportTongTheCuaHang` `[SBAuthorize()]` (không key) → SP `SCRM_Report_BaoCaoTongTheCuaHang`, enrich TenChiNhanh qua `CoreService/ChiNhanh/GetChiNhanhTheoDonVi`. Widget "tổng thể cửa hàng".

## Quan hệ
- **Gọi sang:**
  - Stored proc (đường chính cho TẤT CẢ báo cáo) — xem `stored_procs`. Mọi báo cáo serialize `DataSet` qua `OpenNewConnectionOrDefault` (hoặc `_dbDapper.Ado.UseStoredProcedure` ở #12); chỉ EF phụ trợ đọc `Kho`.
  - `ReportService/api/Common/CheckGioiHanTruyCap` — chặn quá số user đọc report đồng thời (ConfigCode `SO_USER_READ_REPORT_KHO` + nhánh con NHAP/XUAT/NHAP_XUAT/...); finally `ReleaseLockedTicket`.
  - `CommonService.CheckCauHinhKhoangThoiGianChoPhepTimKiem` (giới hạn khoảng ngày), `CommonCacheService.GetOrSetCache_NhomSanPham_DonVi`, `CommonService.DropdownDmSanPham` (ĐVT).
  - `CauHinhService.GetCauHinhInit` — `IdRoles_Che_GiaNhap` (che giá nhập #3/#4).
  - `CoreService/ChiNhanh`, `CustomerService/KhachHang` (map tên ở chi tiết #12).
- **Dữ liệu (entities):** đọc qua SP trên bảng tồn/nhập/xuất kho; EF chỉ đọc `Kho`. KHÔNG ghi.
- **Ripple / đồng bộ:** KHÔNG có (read-only, không bắn message/event).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/WarehouseReportController.cs`
  - L31/L46/L77/L130/L138/L153/L177/L186: nhiều `//[SBAuthorize]` comment — bản hiện hành dùng key `SBAuthorize(...)` riêng.
  - L33 `ReportWarehouseQuayVong` dùng lại key `ReportWarehouseImportExport` (không có key riêng).
  - L122/L154/L162 `ReportTongTheCuaHang`, `XemChiTietLichSuImportExport`, export chi tiết = `[SBAuthorize()]` trống (chỉ cần đăng nhập).
  - L50-51 `ExportExcelWarehouseImportExport` controller gọi `_v2`; bản non-v2 bị comment.
- BE `WarehouseProduct/WarehouseProduct.Service/Generated/WarehouseReportService.cs`
  - L157 / L1005 / L1855 / L2293: tên SP hardcode kèm ngày (`_20260505` / `_20251128`); nhánh cấu hình `Allows_Use_KhoChiTietSync` chọn V1/V2 đã bị comment → luôn dùng bản V2 mới nhất.
  - L750 (#3 AND) vs L905 (#4 OR): điều kiện che giá nhập khác nhau giữa `ReportWarehouseImport` và `ReportWarehouseImportGroup` → cần verify lại logic mong muốn.
  - L2340 (#12): `item.TenDenChiNhanh = denCn != denCn ? tuCn.TenChiNhanh : "";` — `denCn != denCn` luôn false (bug), gán nhầm `tuCn` → TenDenChiNhanh không đúng.
  - L2402-2406 (#7): `CheckCauHinhKhoangThoiGianChoPhepTimKiem` bị comment + L2410 `idChiNhanh = Guid.Empty` (không ép theo user) → báo cáo tồn kho không giới hạn ngày và không khoá theo chi nhánh như các báo cáo khác.
  - Lưu ý: tồn tại bản trùng tên `WarehouseReportController` ở Report.API nhưng phần lớn comment — **bản đang dùng là WarehouseProduct.API** (đã verify theo FE service trỏ `SERVICE_API_WarehouseProduct/api/WarehouseReport`).
