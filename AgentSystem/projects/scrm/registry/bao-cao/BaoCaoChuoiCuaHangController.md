---
controller: BaoCaoChuoiCuaHangController
module: bao-cao
service: Report
man_hinh_ui: "Báo cáo > Chuỗi cửa hàng: báo cáo tổng thể toàn chuỗi (gom doanh số/doanh thu/lợi nhuận theo chi nhánh) và báo cáo tổng thể theo kênh bán hàng. Bộ lọc: chi nhánh (đa chọn), từ/đến ngày, kiểu ngày, sản phẩm/nhóm SP/combo, kho, đối tác giao hàng, trạng thái đối soát, VAT, KH cũ, chiết khấu."
muc_dich: "Báo cáo tổng thể chuỗi cửa hàng (2 endpoint): tổng theo chi nhánh + tổng theo kênh bán hàng. Đọc qua stored proc DB Report, đồng thời gọi cross-service sang WarehouseProduct để merge số liệu kho."
alias: ["báo cáo chuỗi cửa hàng", "bao cao chuoi cua hang", "báo cáo tổng thể cửa hàng", "báo cáo theo kênh bán hàng", "report tong chi nhanh", "report kenh ban hang"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: [SCRM_Report_BaoCaoTongTheCuaHang, SCRM_Report_BaoCaoTongTheCuaHang_KenhBanHang]
depends: [Report/CommonService (CheckCauHinhKhoangThoiGianChoPhepTimKiem, GetStatusShowQueryReport, GetTimeToLog), WarehouseProduct/WarehouseReport (ReportTongTheCuaHang — merge số liệu kho), SqlSugar (CopyNew + GetDataSetAll stored proc), DB SCRM_Report]
cross_module_in: []
cross_module_out: [WarehouseProduct]
---

# BaoCaoChuoiCuaHangController — Báo cáo chuỗi cửa hàng (tổng thể + theo kênh)

> Controller view/report THUẦN ĐỌC, nhỏ (2 endpoint public). Cả 2 đi stored proc (`db.Ado.UseStoredProcedure().GetDataSetAll(...)` trên `_dbDapper.CopyNew()`, DB SCRM_Report) rồi **gọi cross-service sang WarehouseProduct** (`POST {WarehouseProductServiceUrl}/api/WarehouseReport/ReportTongTheCuaHang`) để merge thêm số liệu kho vào kết quả. `[TienIchAttribute(..., TI_Report_Chuoi_CuaHang)]`; mỗi method có `[SBAuthorize("Key", ...)]` riêng (public). Gate chung mỗi method: validate khoảng ngày (`CheckCauHinhKhoangThoiGianChoPhepTimKiem` ≠ -1 → chặn "Chỉ được xem dữ liệu trong khoảng 'N ngày'"); log query chậm khi `IsSuperUser || IsCSKH || GetStatusShowQueryReport==true`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo cáo tổng thể chuỗi theo chi nhánh | Lưới tổng thể cửa hàng | `POST api/BaoCaoChuoiCuaHang/ReportTongChiNhanh` | `BaoCaoChuoiCuaHangController_ReportTongChiNhanh` | public | màn báo cáo chuỗi | Validate khoảng ngày. Chạy SP chính lấy data + dataUser; gọi WarehouseProduct/ReportTongTheCuaHang để merge số liệu kho; trả lưới tổng hợp + total. | **SP** `SCRM_Report_BaoCaoTongTheCuaHang` (+ cross-service WarehouseProduct/ReportTongTheCuaHang) | — |
| 2 | Báo cáo tổng thể theo kênh bán hàng | Lưới theo kênh bán hàng | `POST api/BaoCaoChuoiCuaHang/ReportKenhBanHang` | `BaoCaoChuoiCuaHangController_ReportKenhBanHang` | public | màn báo cáo theo kênh | Như #1; chạy SP KenhBanHang, map tên kênh, trả data + dataTotal. | **SP** `SCRM_Report_BaoCaoTongTheCuaHang_KenhBanHang` | — |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Giới hạn khoảng ngày:** cả 2 báo cáo gate `CheckCauHinhKhoangThoiGianChoPhepTimKiem` ≠ -1 → chặn với thông báo số ngày.
- **Admin view store:** `FunctionCommon.CheckUserIsAdminViewStore(userLogin)` → set `isAdminView` (ảnh hưởng scope chi nhánh hiển thị).
- **Log query chậm:** `LogCommandV2(db, "SCRM_Report_BaoCaoTongTheCuaHang", ...)` chỉ ghi khi `IsSuperUser || IsCSKH || isShowQuery==true`.
- **Merge kho:** kết quả SP Report được union/merge với data trả về từ WarehouseProduct (cross-service) trước khi trả ra UI.

## Quan hệ
- **Gọi sang:** Report `CommonService` (CheckCauHinhKhoangThoiGianChoPhepTimKiem, GetStatusShowQueryReport, GetTimeToLog); cross-service **WarehouseProduct** `/api/WarehouseReport/ReportTongTheCuaHang` (merge số liệu kho).
- **Dữ liệu (entities):** không đọc/ghi entity nội bộ — qua 2 stored proc DB SCRM_Report. Read-only.
- **Ripple / đồng bộ:** không bắn message/event.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.Service/Generated/ReportChuoiCuaHangService.cs`
  - L133 — SP `SCRM_Report_BaoCaoTongTheCuaHang` (ReportTongChiNhanh); L340 — SP `SCRM_Report_BaoCaoTongTheCuaHang_KenhBanHang` (ReportKenhBanHang).
  - L137 — gọi cross-service WarehouseProduct/ReportTongTheCuaHang để merge data kho; nếu service kho fail thì chỉ còn data Report (phụ thuộc availability service kho).
  - L66-70 — logic `CheckDongThoi2Step` (giới hạn số user đọc report đồng thời) đã bị comment → hiện không gate đồng thời.
  - Body `GetDataSetAll` là call SYNC của SqlSugar; chỉ call phụ (CommonService, cross-service kho) là await.
