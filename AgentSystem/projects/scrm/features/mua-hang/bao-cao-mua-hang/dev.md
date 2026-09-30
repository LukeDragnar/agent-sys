# Dev — Báo cáo mua hàng

> ⚠️ **Trạng thái:** INDEX mục "Báo cáo mua hàng" = **"(chưa gate)"** — KHÔNG có controller/màn "Báo cáo mua hàng" tổng hợp riêng. Báo cáo có gate thật trong mảng Mua hàng là **`DepositReportController`** (báo cáo đặt cọc). Báo cáo tồn kho đơn vị thuộc feature "Quản lý tồn (dự báo)" (`KhoChiTietController`). Báo cáo thống kê đơn đặt hàng (PO) dedicated: **chưa xác định — cần product xác nhận**.
> Registry: `projects/scrm/registry/mua-hang/DepositReportController.md` (+ `kho/KhoChiTietController.md` cho báo cáo tồn).

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Báo cáo đặt cọc | `deposit-receipt/deposit-report` | `DepositReportComponent` |
| Báo cáo tồn kho đơn vị | `warehouse-product/baocao-tonkho-hethong` | `BaocaoTonkhoDonviComponent` (xem feature Quản lý tồn dự báo) |

- Route báo cáo đặt cọc: `deposit-receipt/deposit-receipt-routing.module.ts:24`.
- FE service: `src/app/services/deposit-receipt/deposit-report.service.ts` → base URL `${SERVICE_API_DEPOSIT_RECEIPT}api/DepositReport`.

## Endpoint / Controller (trace registry)
**`DepositReportController`** (service Deposit, role `DV-AD-TONG, DV-QL-KHO-TONG`, read-only):
| Key | Route | Mục đích |
|---|---|---|
| `DepositReportController_ReportDeposit` | `POST api/DepositReport/ReportDeposit` | Lưới chi tiết (SP `PRC_SCRM_Deposit_Report`); gating CheckGioiHanTruyCap. |
| `DepositReportController_ReportDepositTotal` | `POST api/DepositReport/ReportDepositTotal` | Dòng tổng (SP `PRC_SCRM_Deposit_ReportTotal`). |
| `DepositReportController_ExportExcelReportDeposit` | `POST api/DepositReport/ExportExcelReportDeposit` | Xuất `ReportDoanhSoTongQuan.xlsx` (gộp chi tiết + tổng). |

Tham chiếu báo cáo tồn: `KhoChiTiet_ReportTonKhoDonVi` / `_ExportExcelReportTonKhoDonVi` (xem dev feature Quản lý tồn dự báo).

## Entities
- `ReportDepositDto` (map từ SP, không ghi DB), đọc `DepositReceipt`.
- Stored procs: `PRC_SCRM_Deposit_Report`, `PRC_SCRM_Deposit_ReportTotal`.

## Luồng code
- Trước khi chạy SP: `CheckGioiHanTruyCap` (HTTP sang ReportService) — chưa đến lượt → false (statusCode 250); ticket nhả trong `finally`.
- Chuẩn hóa ngày (TuNgay 00:00:00 / DenNgay 23:59:998); lọc kho (MaKho từ CauHinhService giao IdReceivingBranch); SP/chi nhánh nối chuỗi `;`; `IsGroupByBranch` gộp theo chi nhánh.
- Export: gọi cả ReportDeposit + ReportDepositTotal → dựng workbook GemBox.

## Bug-prone / lưu ý (path:line)
> Chỉ ghi nhận — KHÔNG sửa code ở session này.
- **Lệch tên file ↔ class**: file vật lý `RepositReportController.cs` nhưng class & route = `DepositReportController` (route thực `api/DepositReport`); `[TienIch]` ghi nhầm "DepositReceiptController". Dễ lạc khi tìm file theo tên controller.
- **`throw;` sau catch nuốt error result**: `ReportDepositService.cs:207` (ReportDeposit) và `:320` (ReportDepositTotal) — catch log rồi `throw;` → method bung exception thay vì trả lỗi êm qua MethodResult.
- **License GemBox hardcode**: `ReportDepositService.cs:355`.
- **Ánh xạ cột Excel lệch chú thích**: `:449 / :501` gán `SoLuongDatCocChuaThanhToan` nhưng chú thích cột (F/E/I) lệch → cần verify cột xuất đúng.
- **Chưa có báo cáo mua hàng hợp nhất**: nếu cần xây, phải product xác nhận phạm vi (đơn đặt hàng PO + đặt cọc + tồn).

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Deposit\Deposit.API\Controllers\RepositReportController.cs` (class `DepositReportController`, route `api/DepositReport`).
- BE service: `...\Deposit\Deposit.Service\Generated\ReportDepositService.cs` (`:207`, `:320`, `:355`, `:449`, `:501`).
- Stored procs (DB): `PRC_SCRM_Deposit_Report`, `PRC_SCRM_Deposit_ReportTotal`.
- FE: `...\scrm_frontend\src\app\pages\deposit-receipt\deposit-report\deposit-report.component.ts`; service `...\src\app\services\deposit-receipt\deposit-report.service.ts`.
- FE routing: `...\deposit-receipt\deposit-receipt-routing.module.ts:24`.
- Báo cáo tồn kho đơn vị: xem `projects/scrm/features/mua-hang/quan-ly-ton-du-bao/dev.md`.
