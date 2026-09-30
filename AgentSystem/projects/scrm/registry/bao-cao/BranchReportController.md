---
controller: BranchReportController
module: bao-cao
service: Report
man_hinh_ui: "Báo cáo > Chi nhánh / Nhân sự: (1) dashboard doanh số thu-chi chi nhánh; (2) hoa hồng nhân viên áp dụng; (3) báo cáo doanh số nhân viên + (4) chi tiết doanh số 1 nhân viên; (5) báo cáo chấm công tổng quát (theo chi nhánh) + (6) chấm công của chính user. Bộ lọc: chi nhánh, khung giờ, nhóm nhân viên, Từ/Đến ngày, keyword, paging."
muc_dich: "6 báo cáo cấp chi nhánh/nhân viên (doanh số thu-chi, hoa hồng, doanh số NV + chi tiết, chấm công) đọc qua stored proc; báo cáo chấm công còn gọi cross-service Booking (khung giờ) + Core (nhóm nhân viên) để enrich."
alias: ["báo cáo chi nhánh", "bao cao chi nhanh", "doanh số nhân viên", "hoa hồng nhân viên", "báo cáo chấm công", "dashboard doanh số thu chi", "branch report"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: [sp_BranchDashboardReportDoanhSoThuChi, PRC_SCRM_MajorReport_HoaHongNhanVienApDung, PRC_SCRM_MajorReport_BaoCaoDoanhSoNhanVien, PRC_SCRM_MajorReport_BaoCaoDoanhSoNhanVien_ChiTiet, Report_BaoCaoChamCongTongQuat]
depends: [BookingService/KhungGio/LayTatCaTheoDonVi (khung giờ làm việc — chấm công), CoreService/NhomNhanVien/GetListByDonVi (nhóm nhân viên — chấm công), CommonService/GetStatusShowQueryReport, SqlSugar (CopyNew + GetDataSetAll stored proc)]
cross_module_in: []
cross_module_out: []
---

# BranchReportController — Báo cáo chi nhánh / nhân viên (doanh số, hoa hồng, chấm công)

> Controller view/report THUẦN ĐỌC: 6 endpoint, tất cả đi **stored proc** trên `_dbDapper.CopyNew()`. `[TienIchAttribute(..., TI_Branch)]` + `[SBAuthorize]` **controller-level** → cần login. Đa số method có permission key riêng; 2 method `[SBAuthorize()]` trống (chi tiết doanh số NV, chấm công của user). Báo cáo chấm công enrich từ cross-service Booking (khung giờ) + Core (nhóm nhân viên) trước khi gọi SP. `BaoCaoChamCongTongQuatUser` tự ép `ListIdChiNhanh = [user.IdChiNhanh]` + `IdNhanVien = user.Id` (chỉ xem của chính mình).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Dashboard doanh số thu-chi | Widget doanh số thu/chi chi nhánh | `POST api/BranchReport/DashboardReportDoanhSoThuChi` | `DashboardReportDoanhSoThuChi` | public | dashboard chi nhánh | Build param lọc; chạy SP, trả record. | **SP** `sp_BranchDashboardReportDoanhSoThuChi` | code cũ ADO comment L108. |
| 2 | Hoa hồng nhân viên áp dụng | Lưới hoa hồng NV | `POST api/BranchReport/DashboardReportHoaHongNhanVienApDung` | (key comment) → controller `[SBAuthorize]` (any login) | public | màn hoa hồng NV | Build param; chạy SP, trả jsonData + total. | **SP** `PRC_SCRM_MajorReport_HoaHongNhanVienApDung` | `[SBAuthorize("DashboardReportHoaHongNhanVienApDung", ...)]` bị **comment** L35 → mất permission key, chỉ còn login. |
| 3 | Báo cáo doanh số nhân viên | Lưới doanh số NV | `POST api/BranchReport/BaoCaoDoanhSoNhanVien` | `BranchReport_BaoCaoDoanhSoNhanVien` | public | màn doanh số NV | Build param; chạy SP, trả jsonData + total. | **SP** `PRC_SCRM_MajorReport_BaoCaoDoanhSoNhanVien` | — |
| 4 | Chi tiết doanh số 1 nhân viên | (drill-down 1 NV) | `POST api/BranchReport/BaoCaoDoanhSoNhanVien_ChiTiet` | `SBAuthorize()` (any login) | public | click 1 NV; validate `IdNhanVien` bắt buộc | Validate IdNhanVien không null. Build param cố định (DonHangTrangThai=DaThanhToan, LoaiDoiTuong=DichVu, LoaiHoaHong=HoaHongThucHien, PageIndex=Page-1) + `@TotalRecord` OUTPUT. Chạy SP, total lấy từ param output. | **SP** `PRC_SCRM_MajorReport_BaoCaoDoanhSoNhanVien_ChiTiet` | `[SBAuthorize()]` trống → không key riêng. PageIndex = Page-1 (0-based) khác các báo cáo khác. |
| 5 | Báo cáo chấm công tổng quát | Lưới chấm công theo chi nhánh | `POST api/BranchReport/BaoCaoChamCongTongQuat` | `GioLamViecNhanVien_BaoCaoChamCongTongQuat` | public | màn chấm công; ModelState valid | Validate ModelState. Resolve danh sách chi nhánh (cơ cấu phòng ban). Gọi Booking `KhungGio/LayTatCaTheoDonVi` + Core `NhomNhanVien/GetListByDonVi`. Build 16 param; chạy SP trả 4 table (total, data, ca được giao, ca nâng cao); `FillDataBaoCaoChamCongTongQuat` map khung giờ/nhóm/ca vào từng dòng (song song). | **SP** `Report_BaoCaoChamCongTongQuat` (4 table) | `if (... || true)` L611 → log objectLog luôn chạy (vế `true` thừa). |
| 6 | Chấm công của chính user | (xem chấm công của tôi) | `POST api/BranchReport/BaoCaoChamCongTongQuatUser` | `SBAuthorize()` (any login) | public | user tự xem | Như #5 nhưng ép `ListIdChiNhanh=[user.IdChiNhanh]` + `IdNhanVien=user.Id` → chỉ chấm công của chính user. Dùng chung `BaoCaoChamCong` + SP. | **SP** `Report_BaoCaoChamCongTongQuat` | `[SBAuthorize()]` trống. |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Chấm công (#5,#6):** cần `KhungGio` (Booking) + `NhomNhanVien` (Core); thiếu → trả lỗi "Không tìm thấy khung giờ/nhóm nhân viên". Tham số SP cố định trạng thái đơn DaThanhToan, loại đối tượng DichVu, loại hoa hồng HoaHongThucHien ở #4.
- **Phạm vi chi nhánh:** #5 resolve theo cơ cấu phòng ban (nếu có) hoặc chi nhánh user; #6 luôn ép về chi nhánh + NV của chính user.
- **Log query chậm:** `objectLog = LogCommandV2(...)` (ở #5 vế điều kiện chứa `|| true` nên luôn chạy).

## Quan hệ
- **Gọi sang:** Booking `KhungGio/LayTatCaTheoDonVi`; Core `NhomNhanVien/GetListByDonVi`; Report `CommonService` (showQuery).
- **Dữ liệu (entities):** không đọc/ghi entity nội bộ — toàn bộ qua 5 stored proc DB SCRM_Report. Read-only.
- **Ripple / đồng bộ:** không bắn message/event.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Report/Report.API/Controllers/BranchReportController.cs`
  - L35: `[SBAuthorize("DashboardReportHoaHongNhanVienApDung", ...)]` **bị comment** → endpoint #2 mất permission key, chỉ còn `[SBAuthorize]` controller-level (any login).
  - L57, L76: 2 method `[SBAuthorize()]` trống (chi tiết doanh số NV #4, chấm công user #6) → không kiểm permission key.
- BE `Report/Report.Service/Generated/BranchReportService.cs`
  - L611: `if (userLogin.IsSuperUser || (userLogin.IsCSKH ?? false) || true)` → vế `true` thừa, log objectLog luôn chạy.
  - L477: `@PageIndex = input.PageInfo.Page - 1` (0-based) ở `BaoCaoDoanhSoNhanVien_ChiTiet`; total lấy từ `@TotalRecord` OUTPUT param (khác các báo cáo lấy total từ table).
