---
controller: WarehouseReportController
module: bao-cao
service: Report
man_hinh_ui: "Báo cáo > Kho vận: báo cáo nhập kho (chi tiết phiếu nhập) + báo cáo nhập kho tổng quan (group). Bộ lọc: chi nhánh, kho, nhóm sản phẩm, Từ/Đến ngày, paging. (Các báo cáo xuất kho / nhập-xuất / sản phẩm bán theo ngày / tổng thể cửa hàng đều bị COMMENT trong controller — không expose.)"
muc_dich: "2 báo cáo nhập kho vận đang active (chi tiết + tổng quan) đọc qua stored proc; phần lớn endpoint kho khác đã comment."
alias: ["báo cáo kho vận", "bao cao kho van", "báo cáo nhập kho", "nhập kho tổng quan", "warehouse report import"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: [sp_ReportWarehouse_ImportV2, sp_ReportWarehouse_Import_GroupV2]
depends: [SqlSugar (CopyNew + GetDataSetAll stored proc), Report DB SCRM_Report]
cross_module_in: []
cross_module_out: []
---

# WarehouseReportController — Báo cáo kho vận (nhập kho: chi tiết + tổng quan)

> Controller view/report THUẦN ĐỌC. **Chỉ 2 endpoint active** (`ReportWarehouseImport`, `ReportWarehouseImportGroup`), đều đi **stored proc** trên `_dbDapper.CopyNew()`. Rất nhiều endpoint kho khác (ReportWarehouseImportExport, ReportWarehouseExport[Group], ExportExcel*, ReportTongTheCuaHang, ReportWarehouseSanPhamBanTheoNgay, XemChiTietLichSuImportExport) đã **comment toàn bộ** trong controller — service vẫn còn method nhưng không expose. `[TienIchAttribute(..., TI_Report_Chung)]`; không `[SBAuthorize]` controller-level, 2 method có permission key riêng.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo cáo nhập kho (chi tiết) | Lưới phiếu nhập kho | `POST api/WarehouseReport/ReportWarehouseImport` | `ReportWarehouseImport` | public | màn báo cáo nhập kho | Build param lọc (IdDonVi, chi nhánh, kho, nhóm SP, Từ/Đến ngày, paging). Chạy SP, trả jsonData + total. | **SP** `sp_ReportWarehouse_ImportV2` | code cũ ADO comment L368. |
| 2 | Báo cáo nhập kho tổng quan | Lưới nhập kho group | `POST api/WarehouseReport/ReportWarehouseImportGroup` | `ReportWarehouseImportGroup` | public | màn báo cáo nhập kho tổng quan | Build param lọc tương tự #1. Chạy SP group, trả jsonData + total. | **SP** `sp_ReportWarehouse_Import_GroupV2` | code cũ ADO comment L445. |

## Endpoint nội bộ / service-method KHÔNG expose (commented)
- Service còn các method nhưng controller đã comment endpoint:
  - `ReportWarehouseImportExport` → SP `sp_ReportWarehouse_Import_Export`; `ExportExcelWarehouseImportExport` (cùng SP) → xuất Excel.
  - `ReportWarehouseExport` → SP `sp_ReportWarehouse_Export`; `ReportWarehouseExportGroup` → SP `sp_ReportWarehouse_Export_Group`; `ExportExcelWarehouseExport`.
  - `ReportTongTheCuaHang` → SP `SCRM_Report_BaoCaoTongTheCuaHang`; `LayTatCaTheoDieuKien` → SP `PRC_SCRM_WarehouseProduct_GetDanhSachNhomSanPhamTheoKho`; `ReportWarehouseSanPhamBanTheoNgay` → SP `sp_ReportWarehouse_SanPhamBanTheoNgay`; `GetDetailSanPhamNhapXuat` (comment cả method).

## Quan hệ
- **Gọi sang:** chỉ SqlSugar (stored proc DB SCRM_Report).
- **Dữ liệu (entities):** không đọc/ghi entity nội bộ qua 2 endpoint active. Read-only.
- **Ripple / đồng bộ:** không bắn message/event.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.API/Controllers/WarehouseReportController.cs`: ~6 endpoint kho (xuất/nhập-xuất/tổng thể/sản phẩm bán theo ngày/chi tiết) bị comment → chỉ còn 2 báo cáo nhập kho hoạt động. Khi cần khôi phục báo cáo xuất kho, service đã có sẵn method tương ứng (xem "Endpoint nội bộ").
- BE `Report/Report.Service/Generated/WarehouseReportService.cs`: 2 SP active dùng bản `*V2` (`sp_ReportWarehouse_ImportV2` L390, `sp_ReportWarehouse_Import_GroupV2` L467) — không phải bản không-V2 vẫn còn trong method export-excel khác.
