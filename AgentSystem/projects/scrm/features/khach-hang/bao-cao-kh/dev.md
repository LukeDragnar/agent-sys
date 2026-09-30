# Dev — Báo cáo & phân tích khách hàng (chân dung KH)

> CHỈ dev. Path BE gốc: `D:\PROJECT\Git\_SourceForCode\Group2\scrm` — FE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend`.

## Tổng quan
Màn **Báo cáo chân dung khách hàng** = 1 component FE gọi **2 endpoint** thuộc **Report service**:
1. `OrderReport/OrderCustomerReportVisitors` — lượng khách theo ngày + summary (tổng khách, khách mới trong tuần). Stored proc `PRC_SCRM_Order_CustomerReport_Visitors`.
2. `KhachHangReport/CustomerReportVisitors` — chân dung (địa chỉ, giới tính, nhóm, hạng). **Stored proc thực tế đang gọi là `PRC_SCRM_BookingReport_Bookings`** (không phải `PRC_SCRM_CustomerReport_Visitors` — bản đó đã bị comment) ⚠️.

Cả 2 đều theo khuôn report chung: check giới hạn thời gian → lấy cấu hình chi nhánh → build `List<SugarParameter>` → `db.Ado.UseStoredProcedure().GetDataSetAll(...)` → `ObjectHelper.FillCollection<T>(ds.Tables[i])` → trả `MethodResult<T>`.

## Màn hình & route (FE)
- Route prefix `report` (`src/app/app-routing.module.ts:212`), file route `src/app/pages/Report/report-routing.module.ts` → path **`visitor`** → `ReportVisitorComponent`.
- Component: `src/app/pages/Report/report-visitors/report-visitors.component.{ts,html,scss}`.
- Tiêu đề màn: i18n key `title-bao-cao-chan-dung-khach-hang` ("Báo cáo chân dung khách hàng").
- maPage filter / grid column key: `ListMaPage_Filter.REPORT_VISITOR` = `Filter_REPORT_VISITOR`, `ListMaPage_GridColumns.REPORT_VISITOR` = `GridColumns_REPORT_VISITOR` (`src/app/constants/config.ts:279,373`).
- FE service: `src/app/services/customer/khachhang-report.service.ts` (`KhachHangReportService`):
  - `OrderCustomerReportVisitors(body)` → POST `…/api/OrderReport/OrderCustomerReportVisitors`
  - `CustomerReportVisitors(body)` → POST `…/api/KhachHangReport/CustomerReportVisitors`
  - cùng file còn `CustomerReportProduct`, `CustomerReportProductDetail`, `CustomerOrderReportUsedCard` → `OrderReport/*`.

## Controller & endpoints (BE)
File: `Report/Report.API/Controllers/OrderReportController.cs` — attr `[TienIchAttribute("OrderReportController", "Báo cáo chung", MaTienIchSCRM.TI_Report_Chung)]`, route `api/OrderReport`.
- `OrderCustomerReportVisitors` — quyền `OrderReport_OrderCustomerReportVisitors` "Báo cáo lượng khách hàng"; nhận `OrderCustomerReportVisitorsFilterDto`.
- `CustomerReportProduct` / `CustomerReportProductDetail` — quyền `OrderReport_CustomerReportProduct*`; báo cáo phân tích KH dùng sản phẩm.
- `CustomerOrderReportUsedCard` — quyền `OrderReport_CustomerOrderReportUsedCard` "Lịch sử dùng thẻ dịch vụ".
- `HistoriesUsingServiceCustomer` — `[PrivateSBAuthorize]`.

File: `Report/Report.API/Controllers/KhachHangReportController.cs` — endpoint `CustomerReportVisitors` (chân dung: địa chỉ/giới tính/nhóm/hạng).

Endpoint cùng tên cũng tồn tại trong `BaoCaoCeoController.cs` (`BaoCaoCeoController_OrderCustomerReportVisitors`, `BaoCaoCeoController_CustomerReportVisitors`, quyền `DV-VIEW-REPORT-CEO`) — dùng cho dashboard CEO, **khác** màn chân dung KH này (nhận `ReportCeoFilter`, gọi qua `ReportCeoService` → service URL `/api/BaoCaoCeo/...`).

## Entities / DTO / SP
DTO filter+response: `Libs/Models.Core/DTO/Order.Client/Report/OrderCustomerReportVisitorsFilterDto.cs`
- `OrderCustomerReportVisitorsFilterDto`: `IdChiNhanh?`, `TuNgay?`, `DenNgay?`, `KieuNgay?`.
- `OrderCustomerReportVisitorsResponse`: `LstData: List<OrderCustomerReportVisitorsDto>` + `Summary: ReportVisitorsCustomerSummary`.
- `OrderCustomerReportVisitorsDto`: `Ngay`, `SoLuong`, `SoLuongKhachCu`, `SoKhachMoi`, `SoLuongKhachLe`.
- `ReportVisitorsCustomerSummary`: `TongSoKhachHang`, `KhachMoiTrongTuan`.
- `WrapperReportVisitors` (Table1/Table2) — dùng cho biến thể CEO deserialize JSON.

DTO chân dung (`CustomerReportVisitorDto`): `lstAddress` (`CustomerReportVisitorAddress`: `MaTinh`, `Ten`, `SoLuongKhachHang`), `lstGender` (`TenGioiTinh`, `SoLuongKhachHang`), `lstGroup` (`TenNhomKhachHang`, `SoLuongNhomKhachHang`), `lstCustomerLevel` (`TenHangKhachHang`, `SoLuongHangKhachHang`).

Stored procedures:
- `PRC_SCRM_Order_CustomerReport_Visitors` — params `@IdDonVi, @IdChiNhanh, @TuNgay, @DenNgay, @KieuNgay`; trả `Tables[0]` = lượng khách theo ngày, `Tables[1]` = summary.
- `PRC_SCRM_BookingReport_Bookings` — SP thực dùng cho `CustomerReportVisitors`; trả 4 bảng theo thứ tự: `[0]` address, `[1]` gender, `[2]` group, `[3]` customer level.

## Luồng code
**API 1 — `OrderReportService.OrderCustomerReportVisitors` (`Report/Report.Service/Generated/OrderReportService.cs:1220`):**
1. `GetStatusShowQueryReport(DonViId)` — quyết định có log query.
2. `CheckCauHinhKhoangThoiGianChoPhepTimKiem(...)` ≠ -1 → trả lỗi giới hạn ngày.
3. GET `CustomerService/api/CauHinh/GetCauHinhInit` → `CauHinhSettingInitDto`. Null → lỗi "Không lấy được cấu hình!".
4. Nếu `ChiNhanhHienThiTatCaKhachHangDonVi` false/null → `isViewAll=false`. Nếu user **không** Admin/SuperUser/Test/CSKH/kế toán và `idChiNhanh==null` và `isViewAll==false` → ép `idChiNhanh = userLogin.IdChiNhanh`.
5. Build params → nếu SuperUser/CSKH/isShowQuery → `Helper.LogCommandV2(...)`.
6. `GetDataSetAll("PRC_SCRM_Order_CustomerReport_Visitors", listParam)`; `FillCollection` 2 bảng; `Summary = dataSummary.FirstOrDefault()`. `dataSummary == null` → "Không có bản ghi nào!".

**API 2 — `KhachHangReportService.CustomerReportVisitors` (`Report/Report.Service/Generated/KhachHangReportService.cs:149`):**
1. Chạy song song `GetStatusShowQueryReport` + `CheckCauHinhKhoangThoiGianChoPhepTimKiem` + GET cấu hình (`Task.WhenAll`).
2. Validate giới hạn ngày + cấu hình giống API 1; ép `idChiNhanh` theo cùng logic phân quyền.
3. `Task.Run` chạy SP `PRC_SCRM_BookingReport_Bookings` → fill 4 bảng (address/gender/group/level), song song với `SA_GetAllAddressLevel1()` lấy danh mục tỉnh thành.
4. Map `MaTinh` → tên tỉnh: khớp dict (chỉ `NationalCode=="VN"`) → `Ten = "(VN) {name}"`; không khớp → `Ten = "Không chọn thành phố"`.

**FE — `ReportVisitorComponent.gridLoadData()`:**
- Gộp `getGridOption()` + `searchModelCore` (gồm `idChiNhanh`, `TuNgay`/`DenNgay` từ `date[0]`/`date[1]`, `trangThaiThanhToan=2`).
- Gọi `OrderCustomerReportVisitors(filter)` → `showchart(data.lstData)` (line: tổng/khách lẻ/khách cũ) + set `summary`.
- Gọi `CustomerReportVisitors(filter)` → vẽ `showchartGender`, `showchartAddress` (gộp các "Không chọn thành phố"), `showchartCustomerLevel`, `showchartGroup`.
- `loadDataUser()` set `isShowSuggestChiNhanh`/`isSuper` theo `roleCodes` (`DV_AD_TONG`, `HT_CSKH`), set `searchModelCore.idChiNhanh` mặc định, rồi `flagSync.next(true)`.

## Bug-prone
- ⚠️ **Tên SP gây hiểu nhầm:** `CustomerReportVisitors` thực chất chạy `PRC_SCRM_BookingReport_Bookings`, không phải SP "CustomerReport_Visitors". Sửa logic chân dung phải sửa đúng SP booking này. Có khối code cũ (ExecuteReader + `DataSet.Load`) đã comment — đừng nhầm.
- **Thứ tự `ds.Tables` cứng:** OrderVisitors dựa `Tables[0]`=data, `Tables[1]`=summary; CustomerReportVisitors dựa `[0..3]`=address/gender/group/level. Đổi thứ tự `SELECT` trong SP làm vỡ mapping mà không báo lỗi.
- **Ép `idChiNhanh` theo phân quyền** ở cả 2 service — quên điều kiện `isViewAll`/role sẽ làm lộ dữ liệu chéo chi nhánh hoặc thiếu dữ liệu. Logic này lặp lại nhiều chỗ (V2, CEO) → sửa phải đồng bộ.
- **`dataSummary == null` trả lỗi** "Không có bản ghi nào!" — kỳ rỗng vẫn có thể rơi vào nhánh này nếu SP không trả bảng summary; cần SP luôn trả ít nhất 1 dòng summary (kể cả 0).
- **Phụ thuộc CustomerService** (`GetCauHinhInit`) và **catalog tỉnh thành** (`SA_GetAllAddressLevel1`): service down → cả báo cáo fail / địa chỉ về "Không chọn thành phố" hàng loạt.
- **FE gộp địa chỉ rỗng** dựa so sánh chuỗi cứng `x.ten=="Không chọn thành phố"` — đổi chuỗi BE (kể cả khoảng trắng) làm FE không gộp được.
- **`KieuNgay`/`date` mapping FE:** `gridLoadData` chỉ set `TuNgay/DenNgay` từ `date[0]/[1]`; control kiểu ngày (`drop-down-list-mix-datepicker-kieungay`) phải đẩy đúng `date` qua `changeInit`, nếu không màn không tự load.
- **CEO vs màn KH:** đừng nhầm endpoint `BaoCaoCeoController.OrderCustomerReportVisitors` (nhận `ReportCeoFilter`, quyền `DV-VIEW-REPORT-CEO`, gọi qua HTTP `/api/BaoCaoCeo/...` trong `ReportCeoService`) với endpoint dùng cho màn chân dung này.

## File source
**BE:**
- `Report/Report.API/Controllers/OrderReportController.cs` (`OrderCustomerReportVisitors`, `CustomerReportProduct*`, `CustomerOrderReportUsedCard`)
- `Report/Report.API/Controllers/KhachHangReportController.cs` (`CustomerReportVisitors`)
- `Report/Report.API/Controllers/BaoCaoCeoController.cs` (biến thể CEO)
- `Report/Report.Service/Generated/OrderReportService.cs:1220` (`OrderCustomerReportVisitors`)
- `Report/Report.Service/Generated/KhachHangReportService.cs:149` (`CustomerReportVisitors`), `:313` (`CustomerReportVisitorsV2`)
- `Report/Report.Service/Generated/ReportCeoService.cs:409` (`OrderCustomerReportVisitors` CEO), `ReportCeoCommonService.cs`
- `Libs/Models.Core/DTO/Order.Client/Report/OrderCustomerReportVisitorsFilterDto.cs`
- SP: `PRC_SCRM_Order_CustomerReport_Visitors`, `PRC_SCRM_BookingReport_Bookings`

**FE:**
- `src/app/pages/Report/report-visitors/report-visitors.component.{ts,html,scss}`
- `src/app/pages/Report/report-routing.module.ts` (path `visitor`)
- `src/app/pages/Report/report.module.ts` (declare `ReportVisitorComponent`)
- `src/app/services/customer/khachhang-report.service.ts`
- `src/app/constants/config.ts` (`REPORT_VISITOR` keys)
