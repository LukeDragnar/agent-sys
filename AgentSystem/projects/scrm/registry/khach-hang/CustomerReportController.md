---
controller: CustomerReportController
module: khach-hang
service: Customer
man_hinh_ui: "(cần xác nhận — báo cáo, chưa định vị UI; dashboard số lượng khách cũ/mới, có thể nằm ở module Report/Dashboard ngoài thư mục Customer)"
muc_dich: "Báo cáo dashboard số lượng khách hàng cũ/mới theo đơn vị/chi nhánh/khoảng ngày (bản thường + bản private cho report CEO)"
alias: [CustomerReport, "Báo cáo đặt lịch", "Dashboard khách hàng", "Số lượng khách cũ khách mới"]
auth: public
state_machine: []
deprecated: false
entities: [KhachHang]
stored_procs: [sp_DashboardGetSummaryCustomer]
depends: [ICustomerReportService, "SqlSugar (Dapper-style stored proc)", "ConnectionString: SCRM_ReportStr (read replica khi isPrivate)"]
cross_module_in: ["Report/Dashboard CEO (DashboardGetSummaryCustomerPrivate)"]
cross_module_out: []
---

# CustomerReport — Báo cáo số lượng khách hàng (dashboard)

> Controller view/report nhỏ (2 endpoint), `[SBAuthorize]` controller-level. Cả 2 endpoint **đi stored procedure** `sp_DashboardGetSummaryCustomer` qua SqlSugar (`UseStoredProcedure().GetDataSetAll`), KHÔNG dùng EF.
> Bản private route qua connection string riêng `SCRM_ReportStr` (read replica) khi `isPrivate=true`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo cáo số lượng khách cũ/mới | Card/dashboard số liệu KH | `POST DashboardGetSummaryCustomer` | DashboardGetSummaryCustomer (DV-AD-TONG/CN/MCN, HT-AD, HT-CSKH) | public | — | Gọi SP `sp_DashboardGetSummaryCustomer` với param IdDonVi/IdChiNhanh/ngayBatDau/ngayKetThuc/MaQuocGia/IsVat/IsChietKhau; non-admin/non-super/non-test bị **ép `IdChiNhanh` = chi nhánh của user**; trả 1 dòng tổng hợp | **SP** `sp_DashboardGetSummaryCustomer` | Phân quyền chi nhánh xử lý trong service (controller không thấy) |
| 2 | Báo cáo KH cũ/mới (private/CEO) | (report CEO nội bộ) | `POST DashboardGetSummaryCustomerPrivate` | DashboardGetSummaryCustomerPrivate (DV-AD-TONG/CN/MCN, HT-AD, HT-CSKH) | public | — | Map `ReportCeoFilter` → `Customer_DashboardFilterDTO` (IdChiNhanh/TuNgay/DenNgay) rồi gọi lại #1 với `isPrivate=true` → đọc qua connection `SCRM_ReportStr` (read replica) | **SP** `sp_DashboardGetSummaryCustomer` (qua replica) | Bản private chỉ truyền IdChiNhanh + 2 mốc ngày, **không** map MaQuocGia/IsVat/IsChietKhau |

## Quan hệ
- **Gọi sang:** SQL trực tiếp — SP `sp_DashboardGetSummaryCustomer` (SqlSugar `UseStoredProcedure`).
- **Dữ liệu (entities):** `KhachHang` (qua SP, không map entity EF).
- **Ripple/đồng bộ:** không có side-effect ghi; chỉ đọc báo cáo.

## Lỗi đã biết / lưu ý kỹ thuật
- `CustomerReportService.cs:68` — cả 2 endpoint chạy **stored procedure** `sp_DashboardGetSummaryCustomer`; thay đổi cột/filter báo cáo phải sửa SP, không thấy trong C#.
- `CustomerReportService.cs:105-126` (`OpenNewConnectionOrDefault`) — bản private mở **connection string `SCRM_ReportStr`** riêng (read replica) → dữ liệu có thể trễ so với DB chính.
- `CustomerReportService.cs:97-104` (`DashboardGetSummaryCustomerPrivate`) — chỉ map IdChiNhanh + TuNgay/DenNgay; bỏ các filter MaQuocGia/IsVat/IsChietKhau so với bản thường.
- `CustomerReportService.cs:47-52` — logic ép chi nhánh theo user có 2 nhánh if gần trùng (cuối cùng đều gán `userLogin.IdChiNhanh`); comment "todo check chi nhanh has permission by user" → kiểm quyền chi nhánh sâu hơn **chưa hoàn thiện**.
- UI: tiện ích khai báo "Báo cáo đặt lịch" nhưng nội dung là số lượng khách — chưa định vị màn hình trong thư mục Customer FE (cần xác nhận, có thể ở module Report/Dashboard).
