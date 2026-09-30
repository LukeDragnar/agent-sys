---
controller: BookingReportController
module: bao-cao
service: Report
man_hinh_ui: "Báo cáo > Đặt lịch: (1) dashboard số lượng đặt lịch / ra đơn (widget đếm); (2) thống kê đặt lịch (biểu đồ nguồn đặt lịch, trạng thái đặt lịch, lượng khách theo khung giờ). Bộ lọc: chi nhánh, Từ/Đến ngày."
muc_dich: "2 báo cáo đặt lịch (Booking) đọc qua stored proc: dashboard đếm số lượng đặt lịch/ra đơn + thống kê chi tiết (nguồn, trạng thái, khách theo giờ)."
alias: ["báo cáo đặt lịch", "bao cao dat lich", "thống kê đặt lịch", "dashboard đặt lịch", "booking report"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: [sp_DashboardGetCountDatLich, PRC_SCRM_BookingReport_Bookings]
depends: [ReportService/Common/GetStatusShowQueryReport (bật log query), CommonService/CheckCauHinhKhoangThoiGianChoPhepTimKiem (giới hạn khoảng ngày), SqlSugar (CopyNew + GetDataSetAll stored proc)]
cross_module_in: []
cross_module_out: []
---

# BookingReportController — Báo cáo đặt lịch (dashboard + thống kê Booking)

> Controller view/report THUẦN ĐỌC: cả 2 endpoint đi **stored proc** (`db.Ado.UseStoredProcedure().GetDataSetAll`) trên `_dbDapper.CopyNew()` (DB SCRM_Report). Có `[TienIchAttribute(..., TI_Report_Chung)]` ở controller (không có `[SBAuthorize]` controller-level → quyền nằm ở từng method). Mỗi method có permission key riêng. Trước khi chạy SP: validate khoảng ngày (`CheckCauHinhKhoangThoiGianChoPhepTimKiem` ≠ -1 → chặn), và bật log query chậm khi `IsSuperUser || IsCSKH || GetStatusShowQueryReport==true`. Service còn có biến thể `*V2` (ReportCeoFilter, dùng `OpenNewConnectionOrDefault` mở connection mới cho CEO dashboard) — **không expose qua controller này**.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Dashboard đếm đặt lịch / ra đơn | Widget số lượng đặt lịch, ra đơn | `POST api/BookingReport/DashboardGetCountDatLich` | `DashboardGetCountDatLich` | public | dashboard đặt lịch | Validate khoảng ngày. Build param (IdDonVi, IdChiNhanh, ngayBatDau/ngayKetThuc). Chạy SP, trả record đầu (`Tables[0]`). | **SP** `sp_DashboardGetCountDatLich` | catch nuốt lỗi trả ResultWithError "Lỗi lấy thông tin"; code cũ comment lớn L175-192. |
| 2 | Thống kê đặt lịch | Biểu đồ nguồn/trạng thái đặt lịch + khách theo giờ | `POST api/BookingReport/BookingReportBookings` | `BookingReportBookings` | public | màn thống kê đặt lịch | Validate khoảng ngày. Nếu user không phải admin/super/test/CSKH và `IdChiNhanh==Empty` → ép `idChiNhanh = userLogin.IdChiNhanh`. Build 4 param (IdDonVi, IdChiNhanh, Từ/Đến ngày). Chạy SP trả 4 table: nguồn đặt lịch, trạng thái đặt lịch, khách theo giờ, total (`Tables[3].Rows[0]["total"]`). | **SP** `PRC_SCRM_BookingReport_Bookings` (4 table) | `Int32.Parse(Tables[3]...["total"])` không guard → SP đổi shape sẽ ném. catch nuốt lỗi. |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Giới hạn khoảng ngày:** `CheckCauHinhKhoangThoiGianChoPhepTimKiem` trả số ngày cho phép; ≠ -1 → chặn "Chỉ được xem dữ liệu trong khoảng 'N ngày'".
- **Ép chi nhánh (#2):** user thường (không admin/super/test/CSKH) + không chọn chi nhánh → mặc định chi nhánh của user.
- **Log query chậm:** chỉ set `objectLog = LogCommandV2(...)` khi `IsSuperUser || IsCSKH || GetStatusShowQueryReport(DonViId)==true`.

## Quan hệ
- **Gọi sang:** Report `CommonService` (CheckCauHinhKhoangThoiGianChoPhepTimKiem, GetStatusShowQueryReport).
- **Dữ liệu (entities):** không đọc/ghi entity nội bộ — toàn bộ qua 2 stored proc DB SCRM_Report. Read-only.
- **Ripple / đồng bộ:** không bắn message/event.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.API/Controllers/BookingReportController.cs`
  - Controller có `[TienIchAttribute(...)]` nhưng KHÔNG `[SBAuthorize]` controller-level → quyền hoàn toàn ở 2 method (đều có key riêng).
- BE `Report/Report.Service/Generated/BookingReportService.cs`
  - L228: `Int32.Parse(ds.Tables[3].Rows[0]["total"])` không try-parse — SP đổi shape/thiếu table → ném exception (bị catch nuốt thành "Lỗi lấy thông tin").
  - L225/L143: log query chậm dựa `objectLog` chỉ khi super/CSKH/showQuery; biến thể V2 (`BookingReportBookingsV2`, L266) dùng `OpenNewConnectionOrDefault` mở connection riêng — không qua controller.
  - Phần code cũ (ADO `CreateCommand`) đã comment toàn bộ (L175-192, L236-257).
