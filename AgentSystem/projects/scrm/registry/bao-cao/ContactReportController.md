---
controller: ContactReportController
module: bao-cao
service: Report
man_hinh_ui: "Báo cáo > Chia số: báo cáo chia số contact theo ngày (lưới chia số khách hàng/contact cho nhân viên). Bộ lọc: chi nhánh, Từ/Đến ngày, nhân viên/nhóm, keyword, paging."
muc_dich: "1 báo cáo chia số contact theo ngày, đọc qua stored proc PRC_SCRM_ReportBaoCaoChiaSo."
alias: ["báo cáo chia số", "bao cao chia so", "chia số theo ngày", "contact report", "báo cáo contact"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: [PRC_SCRM_ReportBaoCaoChiaSo]
depends: [CommonService/CheckCauHinhKhoangThoiGianChoPhepTimKiem (giới hạn khoảng ngày), CommonService/GetStatusShowQueryReport (bật log query), SqlSugar (CopyNew + GetDataSetAll stored proc)]
cross_module_in: []
cross_module_out: []
---

# ContactReportController — Báo cáo chia số contact theo ngày

> Controller view/report THUẦN ĐỌC chỉ **1 endpoint**, đi **stored proc** `PRC_SCRM_ReportBaoCaoChiaSo` (`db.Ado.UseStoredProcedure().GetDataSetAll`) trên `_dbDapper.CopyNew()`. `[TienIchAttribute(..., TI_Report_Chung)]`; không có `[SBAuthorize]` controller-level, method có permission key riêng. Validate khoảng ngày trước khi chạy; log query chậm khi super/CSKH/showQuery.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo cáo chia số theo ngày | Lưới chia số contact theo ngày | `POST api/ContactReport/BaoCaoChiaSo` | `ContactReportController_BaoCaoChiaSo` | public | màn báo cáo chia số | Validate khoảng ngày. Build param lọc (IdDonVi, IdChiNhanh, Từ/Đến ngày, nhân viên/nhóm, keyword, paging). Chạy SP, serialize DataSet trả jsonData + total. | **SP** `PRC_SCRM_ReportBaoCaoChiaSo` | code cũ ADO `CreateCommand` comment L93+. |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Giới hạn khoảng ngày:** `CheckCauHinhKhoangThoiGianChoPhepTimKiem` ≠ -1 → chặn "Chỉ được xem dữ liệu trong khoảng 'N ngày'".
- **Log query chậm:** set `objectLog = LogCommandV2(...)` khi `IsSuperUser || IsCSKH || GetStatusShowQueryReport==true`.

## Quan hệ
- **Gọi sang:** Report `CommonService` (CheckCauHinhKhoangThoiGianChoPhepTimKiem, GetStatusShowQueryReport).
- **Dữ liệu (entities):** không đọc/ghi entity — toàn bộ qua stored proc DB SCRM_Report. Read-only.
- **Ripple / đồng bộ:** không bắn message/event.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.API/Controllers/ContactReportController.cs`: controller không `[SBAuthorize]` controller-level; quyền chỉ ở method `BaoCaoChiaSoAsync`.
- BE `Report/Report.Service/Generated/ContactReportService.cs`
  - L84-85: log + chạy SP `PRC_SCRM_ReportBaoCaoChiaSo`; code cũ ADO comment từ L93.
