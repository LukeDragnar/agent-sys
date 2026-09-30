---
controller: TongDaiReportController
module: bao-cao
service: Report
man_hinh_ui: "Báo cáo > Lịch sử cuộc gọi tổng đài: danh sách cuộc gọi CRM gộp cả MyHub và OmiCall (quản lý lịch sử cuộc gọi). Bộ lọc: contact/khách hàng, từ/đến ngày, nhân viên, tổng đài. Có endpoint nội bộ lấy list lịch sử cuộc gọi cho service khác."
muc_dich: "Báo cáo lịch sử cuộc gọi tổng đài (2 endpoint): 1 public gộp MyHub + OmiCall, 1 internal. Đọc qua stored proc DB Report; gọi cross-service Contact lấy danh mục tác nghiệp."
alias: ["báo cáo tổng đài", "bao cao tong dai", "lịch sử cuộc gọi crm", "danh sách cuộc gọi", "tong dai report", "myhub omicall"]
auth: mixed
state_machine: []
deprecated: false
entities: []
stored_procs: [PRC_SCRM_Switchboard_Call_History, PRC_SwitchBoardConnect_CallHistory_GetByCondition, CallHistory_GetByCondition_Test]
depends: [Report/CommonService (CheckCauHinhKhoangThoiGianChoPhepTimKiem), Report/CauHinhService (CauHinhCheSoTheoUserLogin — che số), Report/MyHubCallHistoryReportService (GetCallHistoryWithConditions — lấy lịch sử MyHub), Contact/DMTacNghiep (GetAllPrivate — danh mục tác nghiệp), SqlSugar (GetDataSetAll stored proc), DB SCRM_Report]
cross_module_in: []
cross_module_out: []
---

# TongDaiReportController — Báo cáo lịch sử gọi điện tổng đài (MyHub + OmiCall)

> Controller view/report THUẦN ĐỌC (1 public + 1 internal). Endpoint public gộp 2 nguồn cuộc gọi: MyHub (qua `MyHubCallHistoryReportService` → SP `PRC_SCRM_Switchboard_Call_History`) + OmiCall (SP `PRC_SwitchBoardConnect_CallHistory_GetByCondition`), merge theo ngày gọi mới nhất rồi phân trang in-memory. Gọi cross-service **Contact** lấy danh mục tác nghiệp + che số điện thoại theo cấu hình. `[TienIchAttribute(..., TI_Report_Chung)]`. Auth **mixed**: `GetDanhSachCuocGoiCrm` public (`[SBAuthorize("Key",...)]`), `GetListLichSuCuocGoi` internal (`[PrivateSBAuthorize]`).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách cuộc gọi CRM (MyHub + OmiCall) | Lưới lịch sử cuộc gọi | `POST api/TongDaiReport/GetDanhSachCuocGoiCrm` | `TongDaiReportController_GetDanhSachCuocGoiCrm` | public | màn quản lý lịch sử cuộc gọi | Validate khoảng ngày; lấy cấu hình che số; lấy lịch sử MyHub (qua service, page 10000) + OmiCall (SP), gọi Contact lấy tác nghiệp, map + che SĐT, union 2 nguồn, order theo ngày gọi, phân trang trên API. | **SP** `PRC_SCRM_Switchboard_Call_History` (MyHub, qua service) + `PRC_SwitchBoardConnect_CallHistory_GetByCondition` (OmiCall) | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] `GetListLichSuCuocGoi` (`[PrivateSBAuthorize]`) → SP `CallHistory_GetByCondition_Test` — phục vụ service khác (Switchboard connect) lấy list lịch sử cuộc gọi theo điều kiện. ⚠️ Tên SP có hậu tố `_Test` (xem known-issue).

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Giới hạn khoảng ngày:** `GetDanhSachCuocGoiCrm` gate `CheckCauHinhKhoangThoiGianChoPhepTimKiem` ≠ -1 → chặn "Chỉ được xem dữ liệu trong khoảng 'N ngày'".
- **Validate input:** payload/userLogin null → "Dữ liệu không hợp lệ!".
- **Che số điện thoại:** `CauHinhCheSoTheoUserLogin` → `IsCheSo` ẩn/format SĐT; `IsChoPhepGoiDien==false` → xoá `PhoneCustomer`.
- **Merge + phân trang code:** MyHub lấy page 10000 (lấy hết) rồi union với OmiCall, sort theo `NgayGoiDien` desc, `Skip/Take` phân trang trên API.

## Quan hệ
- **Gọi sang:** Report `CommonService` (CheckCauHinhKhoangThoiGianChoPhepTimKiem), `CauHinhService` (che số), `MyHubCallHistoryReportService` (GetCallHistoryWithConditions); cross-service **Contact** `/api/DMTacNghiep/GetAllPrivate`.
- **Dữ liệu (entities):** không đọc/ghi entity nội bộ — qua stored proc DB SCRM_Report. Read-only.
- **Ripple / đồng bộ:** không bắn message/event.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.Service/Generated/TongDaiReportService.cs`
  - L285 — SP OmiCall `PRC_SwitchBoardConnect_CallHistory_GetByCondition` (GetLichSuCuocGoiOmi).
  - L399 — **SP tên test:** `GetListLichSuCuocGoi` chạy SP `CallHistory_GetByCondition_Test` (hậu tố `_Test`) → nghi vấn còn dùng SP bản test trên môi trường thật; cần xác nhận có SP production tương ứng.
  - L87-88 — MyHub query lấy `PageSize=10000, Page=1` (lấy toàn bộ) rồi phân trang in-memory → rủi ro hiệu năng khi dữ liệu lớn.
- BE `Report/Report.API/Controllers/TongDaiReportController.cs`
  - L50 — `GetListLichSuCuocGoi` `[PrivateSBAuthorize]` → internal.
