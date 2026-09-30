---
controller: ReportController
module: bao-cao
service: Report
man_hinh_ui: "Báo cáo > Marketing: báo cáo doanh số tổng quan số liệu giao vận (view sale + view marketing), báo cáo công việc MKT view sale, báo cáo lead theo nhân sự MKT, và báo cáo marketing theo sản phẩm (lưới + 2 kiểu xuất Excel: nhóm theo sản phẩm / nhóm theo nguồn dữ liệu). Bộ lọc: chi nhánh, Từ/Đến ngày, sản phẩm/nhóm sản phẩm, nguồn landing, phòng ban/nhóm/user MKT."
muc_dich: "Nhóm báo cáo marketing phụ của Report-service (controller-level [SBAuthorize]): doanh số tổng quan số liệu giao vận MKT/sale, công việc MKT view sale, lead theo nhân sự MKT, báo cáo MKT theo sản phẩm. TẤT CẢ đọc qua stored proc DB SCRM_Report. Một số method trùng tên với BaoCaoOnlineController nhưng gọi cùng service method (share SP)."
alias: ["báo cáo marketing", "bao cao marketing", "báo cáo mkt theo sản phẩm", "báo cáo lead theo nhân sự mkt", "report mkt"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: [SCRM_Report_ReportDoanhSoTongQuan_SoLieuGiaoVan, SCRM_Report_ReportCongViecMkt_View_Sale, SCRM_Report_ReportLeadByNhanSuMkt_Search, PRC_SCRM_Report_BaoCaoMktTheoSanPham, PRC_SCRM_Report_BaoCaoMktTheoSanPham_SoContact, PRC_SCRM_Report_BaoCaoMktTheoSanPham_AllLanding, SCRM_ReportTongSoLieuNganSachAllLandingChiNhanh]
depends: [CommonService/CheckCauHinhKhoangThoiGianChoPhepTimKiem (giới hạn khoảng ngày), CommonService/GetStatusShowQueryReport (bật log query), SqlSugar (CopyNew + GetDataSetAll stored proc / OpenNewConnectionOrDefault), Report DB SCRM_Report]
cross_module_in: []
cross_module_out: []
---

# ReportController — Báo cáo marketing (doanh số giao vận MKT, lead, MKT theo sản phẩm)

> Controller view/report THUẦN ĐỌC. `[TienIchAttribute(..., TI_Report_Chung)]` + **`[SBAuthorize]` controller-level** → toàn bộ method là public (auth bắt buộc); mỗi method còn có permission key riêng (`Report_*`, `ExportExcel_*`). Tất cả đọc qua stored proc (`db.Ado.UseStoredProcedure().GetDataSetAll(...)` trên `_dbDapper.CopyNew()` hoặc `OpenNewConnectionOrDefault`, DB SCRM_Report) — không EF/LINQ trên entity. Gate chung: validate khoảng ngày (`CheckCauHinhKhoangThoiGianChoPhepTimKiem` ≠ -1 → chặn) + ép chi nhánh user khi không admin/super/test/CSKH; log query chậm khi super/CSKH/showQuery. Method `ReportDoanhSoTongQuan_SoLieuGiaoVan` & `ReportCongViecMktViewSale` trùng tên/SP với BaoCaoOnlineController (chỉ khác permission key & `LoaiNhanVien` ép sẵn).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo cáo doanh số tổng quan số liệu giao vận (MKT view) | Lưới giao vận MKT | `POST api/Report/ReportDoanhSoTongQuan_SoLieuGiaoVanMarketing` | `Report_ReportDoanhSoTongQuan_SoLieuGiaoVanMarketing` | public | màn doanh số tổng quan (view MKT) | Ép `LoaiNhanVien=3` (MKT) rồi gọi `ReportDoanhSoTongQuan_SoLieuGiaoVan`. Validate ngày + ép chi nhánh. | **SP** `SCRM_Report_ReportDoanhSoTongQuan_SoLieuGiaoVan` (qua OpenNewConnectionOrDefault) | — |
| 2 | Báo cáo doanh số tổng quan số liệu giao vận (sale view) | Lưới giao vận | `POST api/Report/ReportDoanhSoTongQuan_SoLieuGiaoVan` | `Report_ReportDoanhSoTongQuan_SoLieuGiaoVan` | public | màn doanh số tổng quan | Như #1, không ép LoaiNhanVien (theo role user). | **SP** `SCRM_Report_ReportDoanhSoTongQuan_SoLieuGiaoVan` | — |
| 3 | Báo cáo công việc MKT view sale | Lưới công việc MKT | `POST api/Report/ReportCongViecMktViewSale` | `Report_ReportCongViecMktViewSale` | public | màn công việc MKT | Chạy SP công việc MKT view sale. | **SP** `SCRM_Report_ReportCongViecMkt_View_Sale` | — |
| 4 | Báo cáo lead theo nhân sự MKT | Lưới lead theo nhân sự | `POST api/Report/ReportLeadByNhanSuMktSearch` | `Report_ReportLeadByNhanSuMktSearch` | public | màn lead theo nhân sự MKT | Chạy SP search lead theo nhân sự MKT. | **SP** `SCRM_Report_ReportLeadByNhanSuMkt_Search` | — |
| 5 | Báo cáo marketing theo sản phẩm | Lưới MKT theo SP | `POST api/Report/BaoCaoMktTheoSanPham` | `Report_BaoCaoMktTheoSanPham` | public | màn MKT theo sản phẩm | Chạy bộ SP (đơn hàng + số contact + all landing + ngân sách landing) rồi gộp. | **SP** `PRC_SCRM_Report_BaoCaoMktTheoSanPham` (+ `_SoContact`, `_AllLanding`, `SCRM_ReportTongSoLieuNganSachAllLandingChiNhanh`) | — |
| 6 | Xuất Excel MKT theo SP (nhóm theo sản phẩm / nhóm theo nguồn dữ liệu) | Nút Xuất Excel | `POST api/Report/ExportExcel_BaoCaoMktTheoSanPham_NhomTheoSanPham`, `.../ExportExcel_BaoCaoMktTheoSanPham_NhomTheoNguonDuLieu` | `ExportExcel_BaoCaoMktTheoSanPham_NhomTheoSanPham`, `..._NhomTheoNguonDuLieu` | public | sau khi có dữ liệu #5 | 2 biến thể gom nhóm. Build lại dữ liệu (cùng bộ SP #5) → xuất `.xlsx`. Lỗi → 503 nếu `Service_Unavailable`, 500 còn lại. | **SP** như #5 | — |

## Quan hệ
- **Gọi sang:** Report `CommonService` (CheckCauHinhKhoangThoiGianChoPhepTimKiem, GetStatusShowQueryReport, GetTimeToLog).
- **Dữ liệu (entities):** không đọc/ghi entity nội bộ — 100% qua stored proc DB SCRM_Report. Read-only.
- **Ripple / đồng bộ:** không bắn message/event.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.API/Controllers/ReportController.cs`
  - Controller có `[SBAuthorize]` ở class-level (L12) → mọi endpoint public; khác với BaoCaoOnlineController (không có auth class-level → mixed).
  - `ReportDoanhSoTheoGiaoVanMarketing` (L52-59) đã comment toàn bộ (chỉ còn 6 endpoint thực).
  - `ReportDoanhSoTongQuan_SoLieuGiaoVanMarketing`/`ReportCongViecMktViewSale` dùng chung service method với BaoCaoOnlineController → share SP, chỉ khác permission key & ép `LoaiNhanVien`.
- BE `Report/Report.Service/Generated/ReportService.cs`
  - `ReportDoanhSoTongQuan_SoLieuGiaoVan` (L3702) dùng `OpenNewConnectionOrDefault(... "SCRM_Report_ReportDoanhSoTongQuan_SoLieuGiaoVan", setNewConnection)` — phần `using (var command = CreateCommand(...))` cũ (L3850-3889) đã comment.
  - `BaoCaoMktTheoSanPham` (L16450+) chạy 3-4 SP `PRC_SCRM_Report_BaoCaoMktTheoSanPham*` qua `_dbDapper1` rồi gộp; xuất Excel build lại cùng bộ SP.
