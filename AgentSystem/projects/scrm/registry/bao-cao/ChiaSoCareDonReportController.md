---
controller: ChiaSoCareDonReportController
module: bao-cao
service: Report
man_hinh_ui: "Báo cáo > Care đơn (OrderLogistic): (1) báo cáo chia số care đơn — phân bổ đơn cho nhân viên care; (2) báo cáo care đơn — thống kê đã nhận/đang care/care thành công + doanh số + tỉ lệ; (3) báo cáo tác nghiệp care đơn (số lượng thao tác/nhân viên) + (4) chi tiết tác nghiệp 1 user; (5,6,7) xuất Excel cho các báo cáo trên. Bộ lọc: chi nhánh tạo đơn, phòng ban, user/nhóm care, Từ/Đến ngày, keyword, paging."
muc_dich: "Báo cáo care đơn (kho vận/OrderLogistic): chia số + care đơn đọc qua stored proc; tác nghiệp + chi tiết tác nghiệp đọc từ MongoDB (collection LogThayDoiTrangThaiCareDon); 3 endpoint xuất Excel (GemBox) tái dùng data các báo cáo trên."
alias: ["báo cáo care đơn", "bao cao care don", "chia số care đơn", "tác nghiệp care đơn", "care đơn report", "report care don"]
auth: public
state_machine: []
deprecated: false
entities: [LogThayDoiTrangThaiCareDon]
stored_procs: [PRC_SCRM_ReportCareDon, PRC_SCRM_ReportBaoCaoChiaSoCareDon]
depends: [CommonService/CheckCauHinhKhoangThoiGianChoPhepTimKiem (giới hạn khoảng ngày), CheckIsChiNhanhTong + CheckPermissionViewrReportRoleKho (gate role kho/care), CauHinhService/GetCauHinhInit (LstUserAllowExportExcel — quyền xuất Excel), MongoDbClient (SCRM_OrderLogistic — tác nghiệp), GemBox.Spreadsheet (xuất Excel), SqlSugar (CopyNew + GetDataSetAll stored proc)]
cross_module_in: []
cross_module_out: []
---

# ChiaSoCareDonReportController — Báo cáo care đơn (chia số / care / tác nghiệp + Excel)

> Controller view/report THUẦN ĐỌC, **NGUỒN HỖN HỢP**: (a) báo cáo chia số + care đơn đi **stored proc** (SqlSugar `_dbDapper.CopyNew()`); (b) báo cáo tác nghiệp + chi tiết tác nghiệp đọc **MongoDB** (collection `LogThayDoiTrangThaiCareDon`, DB `SCRM_OrderLogistic`); (c) 3 endpoint xuất **Excel** (GemBox) gọi lại data của (a)/(b). `[TienIchAttribute(..., TI_OrderLogistic)]`; không `[SBAuthorize]` controller-level, mỗi method có permission key riêng. Gate chung: validate khoảng ngày → `CheckIsChiNhanhTong` (đơn vị có cấu hình chi nhánh tổng?) → `CheckPermissionViewrReportRoleKho` (resolve user/nhóm care được xem). Excel cần quyền riêng `CheckUserIsAllowExportExcel` (CSKH/super hoặc nằm trong `LstUserAllowExportExcel`).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo cáo chia số care đơn | Lưới chia số care đơn | `POST api/ChiaSoCareDonReport/BaoCaoChiaSoCareDon` | `ChiaSoCareDonReport_BaoCaoChiaSoCareDon` | public | màn chia số care đơn | Validate ngày → CheckIsChiNhanhTong → RoleKho. Build 11 param (KeyWord, IdDonVi, IdChiNhanh, IsAdminView, IsNhanDuLieu, StrIdNhomNhanVien, StrUserId, Từ/Đến ngày, paging, Sort). Chạy SP, trả list + total. | **SP** `PRC_SCRM_ReportBaoCaoChiaSoCareDon` | — |
| 2 | Báo cáo care đơn | Lưới thống kê care đơn (đã nhận/đang care/thành công + doanh số) | `POST api/ChiaSoCareDonReport/BaoCaoCareDon` | `ChiaSoCareDonReport_BaoCaoCareDon` | public | màn care đơn | Như #1. Chạy SP trả 2 table (data + total). Tính dòng "Tổng" client-side: cộng các cột số lượng & doanh số, tỉ lệ care thành công = `CareThanhCong*100/(DaNhan - ThanhCongAuto - HoanDonAuto + epsilon)`. | **SP** `PRC_SCRM_ReportCareDon` (2 table) | epsilon `+0.00000000001` để tránh chia 0 — tỉ lệ méo nhẹ khi mẫu ~0. |
| 3 | Xuất Excel báo cáo care đơn | Nút "Xuất Excel" | `POST api/ChiaSoCareDonReport/ExportExcelReportCareDon` | `ChiaSoCareDonReport_ExportExcelReportCareDon` | public | có quyền export | Check quyền export → gọi lại data #2 → GemBox build .xlsx. Lỗi `Service_Unavailable` → 503; lỗi khác → 500. | **SP** `PRC_SCRM_ReportCareDon` (qua #2) + GemBox | xử lý lỗi trả raw 500/503 (không qua ResponseResult). |
| 4 | Báo cáo tác nghiệp care đơn | Lưới số lượng tác nghiệp / nhân viên | `POST api/ChiaSoCareDonReport/BaoCaoTacNghiepCareDon` | `ChiaSoCareDonReport_BaoCaoTacNghiepCareDon` | public | màn tác nghiệp care | Validate ngày → CheckIsChiNhanhTong → RoleKho. Đọc **MongoDB** `LogThayDoiTrangThaiCareDon`: filter IdDonVi/IdChiNhanh/NguoiTaoUserId∈userKho/khoảng ngày/keyword; Count + Find; group theo `NguoiTaoUserId`, đếm số log; join `User` (EF) lấy tên; tính `Percent = SoLuong*100/maxSoLuong`; sort desc; thêm dòng Tổng. | **EF/Mongo** (Mongo `LogThayDoiTrangThaiCareDon` + EF `User`) | nếu `isChiNhanhTong==false` và không vào nhánh → trả `ResultWithData(null)` (data null, FE phải guard). |
| 5 | Xuất Excel tác nghiệp care | Nút "Xuất Excel" | `POST api/ChiaSoCareDonReport/ExportExcelReportTacNghiepCare` | `ChiaSoCareDonReport_ExportExcelReportTacNghiepCare` | public | có quyền export | Check quyền export → gọi lại #4 → GemBox .xlsx. | **EF/Mongo** (qua #4) + GemBox | header Excel ghi "Vận đơn" cho cột tên NV (label hơi lệch). |
| 6 | Chi tiết tác nghiệp care đơn (1 user) | (drill-down 1 user) | `POST api/ChiaSoCareDonReport/ChiTietTacNghiepCareDon` | `ChiaSoCareDonReport_ChiTietTacNghiepCareDon` | public | click 1 user | Đọc **MongoDB** `LogThayDoiTrangThaiCareDon`: filter IdDonVi/IdChiNhanh(của user login)/khoảng ngày/`NguoiTaoUserId==UserIdCareDon`; Count + Find paging; trả list log thô + total. | **Mongo** `LogThayDoiTrangThaiCareDon` | dùng `userLogin.IdChiNhanh` (không phải chi nhánh tạo đơn) — chỉ xem trong chi nhánh của user login. |
| 7 | Xuất Excel chi tiết tác nghiệp | Nút "Xuất Excel" | `POST api/ChiaSoCareDonReport/ExportExcelReportChiTietTacNghiepCareDon` | `ChiaSoCareDonReport_ExportExcelReportChiTietTacNghiepCareDon` | public | có quyền export | Check quyền export → ép PageSize=5000 → gọi lại #6 → GemBox .xlsx. | **Mongo** (qua #6) + GemBox | PageSize cứng 5000 → >5000 log/user bị cắt khi xuất. |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Giới hạn khoảng ngày:** `CheckCauHinhKhoangThoiGianChoPhepTimKiem` ≠ -1 → chặn (mọi báo cáo).
- **Chi nhánh tổng:** `CheckIsChiNhanhTong` true → dùng `IdChiNhanhTaoDon` từ filter làm chi nhánh; false → chi nhánh của user. Không lấy được cấu hình → "Không tìm thấy cấu hình của đơn vị".
- **Role kho/care (#1,#2,#4):** `CheckPermissionViewrReportRoleKho(IdPhongBan, StrUserIdNhomNhanVienCareDon, StrUserIdCareDon, idChiNhanh, userLogin)` → trả danh sách user/nhóm care được phép; gán vào filter. Thất bại → "Không tìm thấy thông tin nhân viên kho!".
- **Admin view:** `CheckUserIsAdminViewStore(userLogin)` true → set `IsAdminView=true` (SP bỏ giới hạn user).
- **Quyền xuất Excel (#3,#5,#7):** `CheckUserIsAllowExportExcel`: super/CSKH → cho; ngược lại phải nằm trong `LstUserAllowExportExcel` (chuỗi id split bằng `;`) của cấu hình, không có → "Không có quyền xuất excel".

## Quan hệ
- **Gọi sang:** Report `CommonService` (khoảng ngày), `CauHinhService.GetCauHinhInit` (LstUserAllowExportExcel), `CheckPermissionViewrReportRoleKho` + `CheckIsChiNhanhTong` (nội bộ service).
- **Dữ liệu (entities):** **Mongo** `LogThayDoiTrangThaiCareDon` (DB SCRM_OrderLogistic) cho tác nghiệp; EF `User` (join tên NV); SP cho chia số/care. Read-only.
- **Ripple / đồng bộ:** không bắn message/event.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.Service/Generated/ChiaSoCareDonReportService.cs`
  - L759: `BaoCaoTacNghiepCareDon` trả `ResultWithData(null)` khi `isChiNhanhTong==false` (không có nhánh xử lý) → FE cần guard data null.
  - L162-167, L181-186: tỉ lệ care thành công cộng epsilon `+0.00000000001` tránh chia 0 → kết quả méo nhẹ khi mẫu xấp xỉ 0.
  - L770: `ChiTietTacNghiepCareDonByUserId` filter theo `userLogin.IdChiNhanh` (chi nhánh user login), không theo chi nhánh tạo đơn của filter → có thể lệch phạm vi so với báo cáo cha #4.
  - L886: `ExportExcelReportChiTietTacNghiepCareDon` ép `PageSize=5000` → log > 5000/user bị cắt.
  - L811/L831: header Excel tác nghiệp ghi "Vận đơn" cho cột tên nhân viên (label lệch nghĩa).
- BE `Report/Report.API/Controllers/ChiaSoCareDonReportController.cs`: các endpoint Excel trả raw `StatusCode(503/500)` thay vì `ResponseResult` chuẩn (FE phải xử lý riêng 2 mã này).
