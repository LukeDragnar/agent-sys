---
controller: DonHangTtghLogController
module: kho
service: OrderLogistic
man_hinh_ui: "Kế toán/Kho > Cập nhật trạng thái giao hàng hàng loạt (paste/import mã đơn) — textarea nhập mã đơn + chọn trạng thái, hoặc import Excel; grid log kết quả + nút Tải file lỗi."
muc_dich: "Cập nhật trạng thái giao hàng (TTGH) hàng loạt theo nhiều mã đơn / mã vận đơn: enqueue log rows từ paste hoặc import Excel, ContinueDoWork xử lý từng đơn (validate chuyển trạng thái hợp lệ → đổi GiaoHangTrangThaiMa của đơn logistic), kiểm tra đơn đối soát, tải file mẫu/file lỗi."
alias: ["TTGH log", "cập nhật trạng thái giao hàng hàng loạt", "DonHangTtghLog", "đổi trạng thái nhiều đơn", "import cập nhật trạng thái đơn hàng", "kiểm tra đơn đối soát"]
auth: public
state_machine: [ChuaXuly, DaXuLy, ThanhCong, ThatBai]
deprecated: false
entities: [DonHangTtghLog, DonHangLogistic]
stored_procs: [PRC_SCRM_OrderLogistic_DonHangLogistic_GetByMaDon, PRC_SCRM_OrderLogistic_DonHangLogistic_GetByMaDonGiaoVan]
depends:
  - "IDonHangTtghLogService (DonHangTtghLogService.cs — enqueue + ContinueDoWork worker + Import Excel)"
  - "INghiepVuPendingService (tạo nghiệp vụ pending khi đổi trạng thái)"
  - "ICauHinhService (cấu hình override sale/mkt to customer)"
cross_module_in: []
cross_module_out:
  - "Customer/Contact (publish KhachHangUpdateSaleMktDto khi đơn vào trạng thái Đã giao/Đã thanh toán/Giao một phần — override sale/mkt)"
  - "Loyalty (đổi trạng thái TĂNG/GIẢM chi tiêu → sync DiemTamTru, idempotent theo IdDonHang ở Customer)"
  - "NghiepVuPending (tạo nghiệp vụ pending theo trạng thái mới)"
---

# DonHangTtghLogController — Cập nhật trạng thái giao hàng hàng loạt (log + worker)

> TienIch `DonHangTtghLogController` / `MaTienIchSCRM.TI_OrderLogistic`. Controller `partial` (1 file). Pattern **queue + worker**: enqueue log rows (paste hoặc import Excel) với StatusProcessd=ChuaXuly, FE poll `ContinueDoWork` xử lý từng đơn rồi đánh dấu ThanhCong/ThatBai.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Enqueue cập nhật TTGH theo nhiều mã đơn | "Cập nhật" (sau khi paste mã đơn + chọn trạng thái) | POST `api/DonHangTtghLog/UpdateTrangThaiGiaoHangTheoNhieuMaDon` | `DonHangTtghLog_UpdateTrangThaiGiaoHangTheoNhieuMaDon` | public | textarea có mã đơn | Tách chuỗi mã đơn theo `;`/newline → BulkInsert log rows (ChuaXuly) theo loại mã (mã đơn / mã vận đơn). Chặn nếu >5000 log | EF (Bulk) | - |
| 2 | Xử lý từng đơn trong hàng đợi | (poll nền) | POST `api/DonHangTtghLog/ContinueDoWork` | `DonHangTtghLog_ContinueDoWork` | public | có log ChuaXuly | Bốc 1 log ChuaXuly → lấy đơn bằng SP theo mã đơn/mã vận đơn → validate chuyển trạng thái hợp lệ (xem Quy tắc) → đổi `GiaoHangTrangThaiMa` + tạo NghiepVuPending → đánh dấu ThanhCong/ThatBai | SP + EF | Chạy trong execution strategy + transaction; rollback nếu lỗi |
| 3 | Đếm tổng log | (badge) | POST `api/DonHangTtghLog/GetDonHangTtghLogTotal` | `DonHangTtghLog_GetDonHangTtghLogTotal` | public | - | Đếm log Tổng/ĐãXử lý/ChưaXử lý | EF | - |
| 4 | List log theo user | (grid) | POST `api/DonHangTtghLog/GetDonHangTtghLogByUser` | `DonHangTtghLog_GetDonHangTtghLogByUser` | public | - | List log phân trang theo đơn vị+chi nhánh | EF | - |
| 5 | Xóa toàn bộ log | "Xóa danh sách" | POST `api/DonHangTtghLog/DeleteDonHangTtghLog` | `DonHangTtghLog_DeleteDonHangTtghLog` | public | - | Xóa toàn bộ log của đơn vị+chi nhánh | EF | - |
| 6 | Kiểm tra đơn đối soát | "Kiểm tra đối soát" | POST `api/DonHangTtghLog/KiemTraDonDoiSoat` | `DonHangTtghLog_KiemTraDonDoiSoat` | public | - | Nhập danh sách mã đơn → trả thông tin đơn nào đã/chưa đối soát (không đổi trạng thái) | EF | - |
| 7 | Tải file mẫu import | "Tải file mẫu" | POST `api/DonHangTtghLog/DownloadFileImportCapNhatTrangThaiDonhang` | `DonHangTtghLog_DownloadFileImportCapNhatTrangThaiDonhang` | public | - | Trả file `DataImportTemplate.xlsm` | File tĩnh (Template/) | - |
| 8 | Import Excel cập nhật trạng thái | "Import To Excel" | POST `api/DonHangTtghLog/ImportCapNhatTrangThaiDonHang` | `DonHangTtghLog_ImportCapNhatTrangThaiDonHang` | public | - | Đọc Excel, validate từng dòng → enqueue log rows hợp lệ; dòng lỗi lưu file lỗi (đường dẫn theo UserName). Chặn vượt giới hạn maxRowImport | EF + file | - |
| 9 | Tải file lỗi import | "Tải file lỗi" | POST `api/DonHangTtghLog/DownloadDataCapNhatTrangThaiWrong/{id}` | `DonHangTtghLog_DownloadDataCapNhatTrangThaiWrong` | public | có id file lỗi | Trả file `DataImportFail.xlsm` các dòng import lỗi theo id | File (ImportFail/) | - |

## Quy tắc nghiệp vụ (gating chuyển trạng thái trong ContinueDoWork)
1. Đơn đã ở đúng trạng thái đích → log ThatBai "Đơn hàng đã ở trạng thái: X".
2. Không cho chuyển **chéo ngưỡng DaDang(20)**: nếu đơn ≥ DaDang mà target < DaDang (hoặc ngược lại) → ThatBai (DonHangTtghLogService.cs:344-352).
3. Thành công → set `GiaoHangTrangThaiMa = target`, cập nhật ngày + người, tạo NghiepVuPending trong transaction.
4. Nếu target ∈ {DaGiaoHang(31), DaThanhToan, GiaoHangMotPhan} và đơn vị bật `IsOverrideSaleToInfoCustomer`/`IsOverrideMktToInfoCustomer` → publish `KhachHangUpdateSaleMktDto` (gán sale/mkt của đơn vào info khách hàng).
5. Sync điểm tạm trú (Loyalty): khi đơn vào/ra trạng thái TĂNG chi tiêu → apply/revert DiemTamTru ở Customer (idempotent theo IdDonHang).

## Quan hệ
- **Gọi sang:** lấy đơn bằng SP `..._DonHangLogistic_GetByMaDon` / `..._GetByMaDonGiaoVan`; tạo NghiepVuPending; publish event Customer/Loyalty.
- **Dữ liệu (entities):** `DonHangTtghLog` (log queue: MaDon/MaDonGiaoVan, GiaoHangTrangThaiMa target, StatusProcessd, MaTrangThai), `DonHangLogistic` (đơn được đổi GiaoHangTrangThaiMa).
- **Ripple / đồng bộ:** đổi TTGH → NghiepVuPending + (điều kiện cấu hình) override sale/mkt khách hàng + sync DiemTamTru Loyalty.

## Luồng xuyên module (cập nhật TTGH hàng loạt)
1. Paste/import mã đơn → enqueue log rows (ChuaXuly).
2. `ContinueDoWork` bốc từng log → resolve đơn → validate ngưỡng DaDang → đổi `GiaoHangTrangThaiMa` (transaction) + NghiepVuPending.
3. Side-effect cross-module: publish Customer override sale/mkt; sync DiemTamTru Loyalty (apply/revert theo trạng thái TĂNG chi tiêu).

## Lỗi đã biết / lưu ý kỹ thuật
- Worker xử lý **tuần tự từng đơn** qua poll `ContinueDoWork` — bulk lớn cần nhiều vòng poll, không có batch atomic.
- Lấy đơn bằng SP theo mã đơn HOẶC mã vận đơn; nếu cả hai param có thể query nhầm (logic gán param `@MaDonGiaoVan` chỉ thêm khi MaDonGiaoVan có giá trị — DonHangTtghLogService.cs:146-158).
- Override sale/mkt vào khách hàng chỉ chạy khi đơn vào trạng thái đã giao/đã thanh toán/giao một phần + cấu hình bật (DonHangTtghLogService.cs:373-400).
