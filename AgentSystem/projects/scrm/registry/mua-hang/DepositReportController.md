---
controller: DepositReportController
module: mua-hang
service: Deposit
man_hinh_ui: "Mua hàng/Đặt cọc > màn 'Báo cáo đặt cọc' (/deposit-receipt/deposit-report). Lưới báo cáo theo (chi nhánh × sản phẩm) + dòng tổng + bộ lọc (chi nhánh nhận, sản phẩm, đợt trả cọc, group theo chi nhánh, khoảng ngày). Nút Xuất Excel."
muc_dich: "Báo cáo dữ liệu đặt cọc tổng hợp (SL cọc/đã thanh toán/chưa thanh toán/đã giao/hoàn cọc...) theo chi nhánh & sản phẩm, qua stored proc. Có gating giới hạn truy cập (CheckGioiHanTruyCap) chống quá tải report."
alias: ["báo cáo đặt cọc", "bao cao dat coc", "report đặt cọc", "báo cáo doanh số đặt cọc", "thống kê đặt cọc", "deposit report"]
auth: public
state_machine: []
deprecated: false
entities: [ReportDepositDto, DepositReceipt]
stored_procs: [PRC_SCRM_Deposit_Report, PRC_SCRM_Deposit_ReportTotal]
depends: [ReportService/CheckGioiHanTruyCap (HTTP gating), CommonService/GetCoCauDonViTuCache, CommonService/GetSanPhamByListMa, CauHinhService/GetAsync (cấu hình kho), SqlSugar/Dapper (SP), GemBox.Spreadsheet (export)]
cross_module_in: []
cross_module_out: []
---

# DepositReportController — Báo cáo đặt cọc (report theo chi nhánh × sản phẩm, đi stored proc + gating truy cập)

> Controller view/report THUẦN ĐỌC. Lưu ý: **file vật lý là `RepositReportController.cs` nhưng class & route là `DepositReportController`** (lệch tên file ↔ class; `[TienIch]` cũng ghi nhầm "DepositReceiptController"). Đọc dữ liệu qua 2 stored proc (`PRC_SCRM_Deposit_Report` chi tiết phân trang + `PRC_SCRM_Deposit_ReportTotal` dòng tổng). Trước khi chạy report luôn gọi **`CheckGioiHanTruyCap`** (HTTP sang ReportService) để giới hạn lượt truy cập (statusCode 250 = "Chưa đến lượt truy cập") + nhả ticket trong finally. Controller hằng `[SBAuthorize(key,..., "DV-AD-TONG, DV-QL-KHO-TONG")]`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo cáo (lưới chi tiết) | Lưới báo cáo + bộ lọc | `POST api/DepositReport/ReportDeposit` | `DepositReportController_ReportDeposit` | public | màn báo cáo đặt cọc | Gọi SP `PRC_SCRM_Deposit_Report` (params: IdDonVi, ListIdReceivingBranch nối `;`, StrIdProduct nối `;`, IdDepositOrderDeliveryBatch, IsGroupByBranch, TuNgay, DenNgay, OrderBy/OrderType, paging). Gating CheckGioiHanTruyCap (config SO_USER_READ_QUAN_LY_DAT_COC / SO_USER_READ_REPORT_DAT_COC). Trả list + total; nhả ticket finally. | **SP** `PRC_SCRM_Deposit_Report` | catch log rồi `throw;` lại (L207) → method chết trước khi trả error result. |
| 2 | Báo cáo (dòng tổng) | Dòng tổng cuối lưới | `POST api/DepositReport/ReportDepositTotal` | `DepositReportController_ReportDepositTotal` | public | màn báo cáo đặt cọc | Gọi SP `PRC_SCRM_Deposit_ReportTotal` (gọn hơn: IdDonVi, ListIdReceivingBranch, StrIdProduct, IdBatch, TuNgay, DenNgay) → 1 dòng tổng. Gating khác config (..._TOTAL). | **SP** `PRC_SCRM_Deposit_ReportTotal` | catch `throw;` lại (L320). |
| 3 | Xuất Excel báo cáo | Nút "Xuất Excel" | `POST api/DepositReport/ExportExcelReportDeposit` | `DepositReportController_ExportExcelReportDeposit` | public | màn báo cáo đặt cọc | Gọi cả ReportDeposit + ReportDepositTotal → dựng workbook GemBox (header + dòng tổng + format số nguyên ngăn ngàn). File `ReportDoanhSoTongQuan.xlsx`. Lỗi `Service_Unavailable`→503, khác→500. | **SP** + GemBox | L355: license GemBox hardcode; L449/L501: ánh xạ cột Excel ("SoLuongDatCocChuaThanhToan") lệch chú thích cột → cần verify cột xuất. |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Gating truy cập:** trước khi chạy SP → `CheckGioiHanTruyCap` (HTTP ReportService); chưa đến lượt → trả false (statusCode 250). Ticket nhả trong finally.
- **Chuẩn hóa ngày:** TuNgay→00:00:00, DenNgay→23:59:998.
- **Lọc kho:** MaKho (từ CauHinhService) giao với IdReceivingBranch nếu cả hai có.
- **Lọc SP/chi nhánh:** optional, truyền dạng chuỗi nối `;` cho SP.
- **Group:** `IsGroupByBranch` cho phép gộp theo chi nhánh.

## Quan hệ
- **Gọi sang:** ReportService (`CheckGioiHanTruyCap` HTTP), CommonService (cơ cấu đơn vị, tên SP theo mã), CauHinhService (cấu hình kho), SqlSugar/Dapper (2 SP).
- **Dữ liệu (entities):** đọc tổng hợp qua SP, map `ReportDepositDto` (không ghi DB).
- **Ripple / đồng bộ:** KHÔNG bắn message. Report thuần đọc.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Deposit/Deposit.API/Controllers/RepositReportController.cs` — **file đặt tên `RepositReportController.cs` nhưng class = `DepositReportController`** (lệch tên file/class; `[TienIch]` ghi nhầm "DepositReceiptController"). Route thực `api/DepositReport`.
- BE `Deposit/Deposit.Service/Generated/ReportDepositService.cs`
  - L207 / L320: catch ghi log rồi `throw;` lại → method chết trước khi return error result (lỗi báo cáo bung exception thay vì trả lỗi êm).
  - L355: license GemBox hardcode trong code.
  - L449 / L501: gán cột Excel `SoLuongDatCocChuaThanhToan` nhưng chú thích cột (F/E/I) lệch nhau → cần verify cột xuất đúng.
- FE `scrm_frontend/src/app/pages/deposit-receipt/deposit-report/`; route `deposit-receipt/deposit-report`.
