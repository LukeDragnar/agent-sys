# Báo cáo sản phẩm — Kỹ thuật

> Service: **Report** (BE .NET) · FE module **Report** (Angular 15 + ng-zorro). Chỉ dành cho dev. Thuần đọc qua stored proc DB `SCRM_Report`.

## Màn hình & route (FE)
Prefix: `app-routing.module.ts:212` (`path: 'report'`). Route con: `Report/report-routing.module.ts`.

| Màn hình | Route | Component |
|---|---|---|
| Tỉ lệ chốt đơn sản phẩm | `/report/report-tyle-chotdon-sanpham` | `ReportTyleChotdonSanphamComponent` |
| Báo cáo combo | `/report/report-combo` | `ReportComboComponent` |
| Báo cáo combo sản phẩm | `/report/report-combo-san-pham` | `ReportComboSanPhamComponent` |
| Số ngày chưa bán được sản phẩm | `/report/report-thongke-songay-chuabanduoc-sanpham` | `ReportThongkeSongayChuabanduocSanphamComponent` |

## Endpoint / Controller (trace registry)
Trace: `registry/bao-cao/BaoCaoOnlineController.md` (mục #10, #11, #17, #18).

**BaoCaoOnlineController** (`TI_Report_Online`, public):
| Endpoint | Quyền (key) | SP |
|---|---|---|
| `POST api/BaoCaoOnline/ReportTyLeChotDonSanPham` (+ `ExportReportTyLeChotDonSanPham`) | `BaoCaoOnlineController_ReportTyLeChotDonSanPham` (+ Export) | `SCRM_Report_ReportTyLeChotDonSanPham` (+ `_SoContact`) |
| `POST api/BaoCaoOnline/ReportCombo` (+ `ExportExcelReportCombo`) | `BaoCaoOnlineController_ReportCombo` (+ Export) | `SCRM_Report_OrderLogistic_BaoCaoComBo` (lưới); Export build từ payload FE |
| `POST api/BaoCaoOnline/ReportComboSanPham` (+ `ExportExcelReportComboSanPham`) | `BaoCaoOnlineController_ReportComboSanPham` (+ Export) | `SCRM_Report_OrderLogistic_BaoCaoComBoSanPham` |
| `POST api/BaoCaoOnline/ReportThongKeSoNgayChuaBanDuocSanPham` (+ `ExportReport...`) | `BaoCaoOnlineController_ReportThongKeSoNgayChuaBanDuocSanPham` (+ Export) | `SCRM_Report_ReportSoNgayChuaBanDuoc` |

> Báo cáo doanh thu theo SP (số tiền) thuộc nhóm doanh số: `OrderReportController` → `ReportOrderReportProduct` (SP `PRC_SCRM_OrderReport_Products`), `ReportDonHangSanPhamBanHang(+Group)` — xem feature `bao-cao-doanh-so`.

## Entities
- Không đọc/ghi entity nội bộ. 100% qua stored proc DB `SCRM_Report` (read-only).

## Luồng code
1. Validate khoảng ngày + ép chi nhánh user.
2. **Tỉ lệ chốt đơn:** chạy SP `_SoContact` (số contact) + SP chính `ReportTyLeChotDonSanPham` rồi gộp kết quả.
3. **Combo:** lưới chạy SP `BaoCaoComBo`; **Export Combo nhận `List<ReportComboDto>` từ FE và KHÔNG re-query SP** — build `.xlsx` trực tiếp.
4. **Combo sản phẩm:** lưới + export đều qua SP `BaoCaoComBoSanPham`.
5. Log query chậm khi super/CSKH/showQuery.

## Bug-prone / lưu ý (path:line)
- **`ExportExcelReportCombo` build từ payload FE, không chạy lại SP** (ReportService.cs) — nếu FE chỉ gửi 1 trang dữ liệu thì Excel thiếu dòng. Đây là hành vi thiết kế, dễ gây hiểu nhầm "xuất thiếu".
- **`ExportExcelReportComboSanPham` (BaoCaoOnlineController.cs:946)** khi lỗi trả `BadRequest(Message)` — KHÁC chuẩn các export khác (503/500 + body Status/Message/Code). FE cần xử lý riêng status code.
- **`ExportExcelReportThongKeSoNgayChuaBanDuocSanPham` (L716-749) đã comment toàn bộ** trong controller — kiểm tra endpoint export này còn hoạt động không trước khi dựa vào.
- **GetDataSetAll SYNC** dù async; SP nặng + `CopyNew` per request.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.API\Controllers\BaoCaoOnlineController.cs` (Export combo L946, Export số ngày chưa bán L716-749 comment).
- BE service: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Report\Report.Service\Generated\ReportService.cs` (ReportTyLeChotDonSanPham, ReportComBo, ReportComboSanPham, ReportSoNgayChuaBanDuoc).
- FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\Report\report-routing.module.ts` (route ở bảng trên).
- SP (DB): `SCRM_Report_ReportTyLeChotDonSanPham(+_SoContact)`, `SCRM_Report_OrderLogistic_BaoCaoComBo`, `SCRM_Report_OrderLogistic_BaoCaoComBoSanPham`, `SCRM_Report_ReportSoNgayChuaBanDuoc`.
