---
controller: MyHubCallHistoryReportController
module: bao-cao
service: Report
man_hinh_ui: "Báo cáo > Lịch sử cuộc gọi (MyHub): tìm kiếm lịch sử cuộc gọi khi nghe ghi âm, quản lý lịch sử cuộc gọi (manager xem all), thống kê tổng đài cuộc gọi MyHub, và báo cáo cuộc gọi thành công. Bộ lọc: contact/khách hàng, từ/đến ngày, nhân viên sale/kho, tổng đài."
muc_dich: "Báo cáo lịch sử cuộc gọi tổng đài MyHub (4 endpoint public): tìm/quản lý lịch sử cuộc gọi, thống kê tổng đài, cuộc gọi thành công. Đọc qua stored proc DB Report; gọi cross-service Contact lấy danh mục tác nghiệp + check quyền."
alias: ["lịch sử cuộc gọi myhub", "lich su cuoc goi myhub", "báo cáo cuộc gọi", "nghe ghi âm", "thống kê tổng đài myhub", "cuộc gọi thành công", "call history report"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: [PRC_SCRM_Switchboard_Call_History, PRC_SCRM_SwitchBoard_GetByConditionDashboard_MyHub, SCRM_Report_ReportTongDaiMyHub, SCRM_Report_ReportTongDaiOmi]
depends: [Report/CommonService (CheckCauHinhKhoangThoiGianChoPhepTimKiem cho ReportCuocGoiThanhCong), Report/CauHinhService (GetAsync — cấu hình che số), Contact/DMTacNghiep (GetAllPrivate — danh mục tác nghiệp), Contact (check permission view report multi-user), SqlSugar (GetDataSetAll stored proc), DB SCRM_Report]
cross_module_in: []
cross_module_out: []
---

# MyHubCallHistoryReportController — Báo cáo lịch sử cuộc gọi MyHub

> Controller view/report THUẦN ĐỌC (4 endpoint public). Inject 2 service: `IMyHubCallHistoryReportService` (3 endpoint MyHub) + `IReportService` (endpoint ReportCuocGoiThanhCong). Đọc qua stored proc DB SCRM_Report; gọi cross-service **Contact** lấy danh mục tác nghiệp (`/api/DMTacNghiep/GetAllPrivate`) và check quyền view report multi-user. `[TienIchAttribute(..., TI_Report_Chung)]`; mỗi method `[SBAuthorize("Key", ...)]` (public). Lưu ý: ReportCuocGoiThanhCong dùng permission key tiền tố `BaoCaoOnlineController_*` và backing service khác (IReportService) — nó dispatch sang MyHub + OmiCall (2 SP). Một số endpoint validate ModelState trước.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm lịch sử cuộc gọi (nghe ghi âm) | Nút nghe ghi âm | `POST api/MyHubCallHistoryReport/MyHubGetCallHistoryWithConditions` | `MyHubCallHistory_MyHubGetCallHistoryWithConditions` | public | click nghe ghi âm trên contact | ModelState invalid → trả lỗi model. Lấy cấu hình che số; gọi Contact check quyền; chạy SP lịch sử cuộc gọi; map tác nghiệp + che SĐT. | **SP** `PRC_SCRM_Switchboard_Call_History` | — |
| 2 | Quản lý lịch sử cuộc gọi (manager) | Lưới quản lý cuộc gọi | `POST api/MyHubCallHistoryReport/MyHubGetCallHistoryWithConditions_Manager` | `MyHubCallHistory_MyHubGetCallHistoryWithConditions_Manager` | public | role quản lý | Như #1 nhưng set `payload.IsManager = true` (xem toàn bộ, không giới hạn theo user). | **SP** `PRC_SCRM_Switchboard_Call_History` | dùng chung handler với #1, khác cờ IsManager. |
| 3 | Thống kê tổng đài cuộc gọi MyHub | Widget thống kê tổng đài | `POST api/MyHubCallHistoryReport/GetThongKeTongDaiCuocGoiMyHub` | `MyHubCallHistory_GetThongKeTongDaiCuocGoiMyHub` | public | dashboard tổng đài | Lấy cấu hình + check quyền multi-user; chạy SP thống kê dashboard MyHub. | **SP** `PRC_SCRM_SwitchBoard_GetByConditionDashboard_MyHub` | — |
| 4 | Báo cáo cuộc gọi thành công | Lưới cuộc gọi thành công | `POST api/MyHubCallHistoryReport/ReportCuocGoiThanhCong` | `BaoCaoOnlineController_ReportCuocGoiThanhCong` | public | màn cuộc gọi thành công | Validate khoảng ngày; bắt buộc chọn `IdLoaiTongDai`. Dispatch theo tổng đài → gọi sub MyHub + OmiCall, mỗi cái 1 SP, gộp kết quả. | **SP** `SCRM_Report_ReportTongDaiMyHub`, `SCRM_Report_ReportTongDaiOmi` (qua `IReportService`) | backing service `IReportService` (khác 3 endpoint trên); key tiền tố `BaoCaoOnlineController_*`. |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **ModelState:** #1/#2 trả `MESS_Model_UnValidate` nếu payload không hợp lệ.
- **IsManager:** #2 ép `IsManager=true` → bỏ giới hạn theo user (xem all theo quyền manager).
- **Che số điện thoại:** cấu hình che số (`CauHinhSettingDto` / CauHinhService) → ẩn/format SĐT khách trong kết quả.
- **ReportCuocGoiThanhCong (#4):** gate `CheckCauHinhKhoangThoiGianChoPhepTimKiem` ≠ -1 → chặn; `IdLoaiTongDai == null` → "Vui lòng chọn tổng đài!"; ép `IsManager=true`; dispatch theo loại tổng đài (MyHub/OmiCall).

## Quan hệ
- **Gọi sang:** Report `CauHinhService` (cấu hình che số), `CommonService` (CheckCauHinhKhoangThoiGianChoPhepTimKiem — #4); cross-service **Contact** `/api/DMTacNghiep/GetAllPrivate` (danh mục tác nghiệp) + check permission view report multi-user.
- **Dữ liệu (entities):** không đọc/ghi entity nội bộ — qua stored proc DB SCRM_Report. Read-only.
- **Ripple / đồng bộ:** không bắn message/event.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.Service/Generated/MyHubCallHistoryReportService.cs`
  - L201 — SP `PRC_SCRM_Switchboard_Call_History` (GetCallHistoryWithConditions, #1/#2); L439 — SP `PRC_SCRM_SwitchBoard_GetByConditionDashboard_MyHub` (GetThongKeTongDaiCuocGoiMyHub, #3).
- BE `Report/Report.Service/Generated/ReportService.cs`
  - L14053 — `ReportCuocGoiThanhCong` (#4) ép `input.IsManager = true`, dispatch sang `ReportCuocGoiThanhCongTongDaiMyHub` (SP `SCRM_Report_ReportTongDaiMyHub`) + `ReportCuocGoiThanhCongTongDaiOmiCall` (SP `SCRM_Report_ReportTongDaiOmi`).
- BE `Report/Report.API/Controllers/MyHubCallHistoryReportController.cs`
  - L67 — ReportCuocGoiThanhCong dùng permission key tiền tố `BaoCaoOnlineController_*` (di sản controller online cũ) và backing `IReportService` thay vì service MyHub.
  - Controller `partial` (có thể có file partial khác mở rộng).
