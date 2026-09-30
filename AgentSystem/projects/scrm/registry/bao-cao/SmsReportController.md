---
controller: SmsReportController
module: bao-cao
service: Report
man_hinh_ui: "Báo cáo > SMS: báo cáo gửi SMS theo điều kiện (số lượng gửi/thành công/thất bại theo thời gian, nội dung, trạng thái). Bộ lọc: từ/đến ngày, trạng thái gửi, nội dung/điều kiện SMS."
muc_dich: "Báo cáo SMS (1 endpoint public): thống kê SMS gửi theo điều kiện. Đọc qua 1 stored proc DB Report."
alias: ["báo cáo sms", "bao cao sms", "thống kê sms", "sms report", "báo cáo gửi tin nhắn"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: [SCRM_SmsSend_GetReportByConditions]
depends: [SqlSugar (GetDataSetAll stored proc), DB SCRM_Report]
cross_module_in: []
cross_module_out: []
---

# SmsReportController — Báo cáo SMS

> Controller view/report THUẦN ĐỌC, nhỏ (1 endpoint public). Đi 1 stored proc DB SCRM_Report (`_dbDapper.Ado.UseStoredProcedure().GetDataSetAll("SCRM_SmsSend_GetReportByConditions", ...)`). `[TienIchAttribute(..., TI_Report_Chung)]`; method `[SBAuthorize("SmsSend_GetReportByConditions", ...)]` (public). Controller `partial`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo cáo SMS theo điều kiện | Lưới báo cáo SMS | `POST api/SmsReport/GetReportByConditions` | `SmsSend_GetReportByConditions` | public | màn báo cáo SMS | Build param từ `SmsConditionModel`, log query (LogCommandV2), chạy SP, trả kết quả thống kê SMS. | **SP** `SCRM_SmsSend_GetReportByConditions` | — |

## Quan hệ
- **Gọi sang:** không gọi service phụ (chỉ chạy SP trực tiếp).
- **Dữ liệu (entities):** không đọc/ghi entity nội bộ — qua 1 stored proc DB SCRM_Report. Read-only.
- **Ripple / đồng bộ:** không bắn message/event.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.Service/Generated/SmsReportService.cs`
  - L158 — SP `SCRM_SmsSend_GetReportByConditions`.
  - L155-156 — `LogCommandV2` được gọi 2 lần liên tiếp (trong nhánh điều kiện rồi gán đè ngay sau) → câu lệnh log gần như luôn ghi, không thực sự gate theo điều kiện super/CSKH/showQuery như các report khác.
- BE `Report/Report.API/Controllers/SmsReportController.cs` — controller `partial`, hiện chỉ có 1 file.
