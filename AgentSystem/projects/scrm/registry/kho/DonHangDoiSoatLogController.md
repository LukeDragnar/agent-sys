---
controller: DonHangDoiSoatLogController
module: kho
service: OrderLogistic
man_hinh_ui: "Đơn hàng logistic > màn Đối soát đơn hàng (đối soát phí ship/COD với đối tác vận chuyển): import file đối soát, chọn phiên đối soát, list đơn chưa xử lý, cập nhật trạng thái đối soát (single/nhiều), lịch sử + chi tiết lịch sử, tải file mẫu/file lỗi"
muc_dich: "Quản lý đối soát đơn hàng giao vận (so khớp phí ship/COD giữa hệ thống và đối tác): import file Excel đối soát theo phiên, load + lọc đơn chưa xử lý, cập nhật trạng thái đối soát từng đơn / theo lô, tạo phiếu từ đơn đối soát, và tra cứu lịch sử đối soát."
alias: ["đối soát đơn hàng", "đối soát log", "đối soát phí ship", "đối soát COD", "import đối soát", "phiên đối soát", "lịch sử đối soát"]
auth: mixed
state_machine: []
deprecated: false
entities: [DonHangDoiSoatLog, DonHangDoiSoatSession]
stored_procs: []
depends:
  - "IDonHangDoiSoatLogService (OrderLogistic.Services/Generated)"
  - "GemBox.Spreadsheet (xuất file Excel template / file lỗi .xlsm)"
cross_module_in: []
cross_module_out:
  - "Accounting/Kế toán (CapNhatDoiSoatDonTaoPhieuAsync — tạo phiếu từ đơn đối soát; CapNhatTrangThaiDoiSoatSingleDonHangAsync_ForKT — luồng riêng kế toán)"
---

# DonHangDoiSoatLogController — Đối soát đơn hàng (log/phiên)

> Controller `partial`. Nhiều biến thể `[SBAuthorize("key", ...)]` ĐÃ comment, hiện đa số endmaster dùng `[SBAuthorize]` mở (chỉ cần login). Block đối soát "version 20260113 (TamDC)" là luồng đang dùng.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy log đối soát tổng | màn tổng quan | POST `api/DonHangDoiSoatLog/GetDonHangDoiSoatLogTotal` | `DonHangDoiSoatLog_GetDonHangDoiSoatLogTotal` | public | màn list | Trả log tổng đối soát theo user/đơn vị | EF | - |
| 2 | Xóa log đối soát | "Xóa log" | POST `api/DonHangDoiSoatLog/DeleteDonHangDoiSoatLog` | `DonHangDoiSoatLog_DeleteDonHangDoiSoatLog` | public | - | Xóa danh sách log đối soát của user | EF | - |
| 3 | Import đối soát đơn hàng | "Import" (upload file) | POST `api/DonHangDoiSoatLog/ImportDoiSoatDonHang` | `DonHangDoiSoatLog_ImportDoiSoatDonHang` | public | chọn file | Upload Excel đối soát → tạo phiên đối soát (DonHangDoiSoatSessionImportAsync) | EF | - |
| 4 | Tải file lỗi import | "Tải file lỗi" | POST `api/DonHangDoiSoatLog/DownloadDataDoiSoatDonHangWrong/{id}` | `DonHangDoiSoatLog_DownloadDataDoiSoatDonHangWrong` | public | có id phiên lỗi | Trả file `DataImportFail.xlsm` các dòng import sai (GemBox) | GemBox | - |
| 5 | Tải file mẫu import | "Tải mẫu" | POST `api/DonHangDoiSoatLog/DownloadFileImportDoiSoatDonHang` | `DonHangDoiSoatLog_DownloadFileImportDoiSoatDonHang` | public | - | Trả file template `DataImportTemplate.xlsm` | GemBox | - |
| 6 | Load dữ liệu phiên đối soát | mở phiên | POST `api/DonHangDoiSoatLog/DonHangDoiSoatSessionLoadData` | (key comment → `[SBAuthorize]` mở) | public | có phiên | Load dữ liệu đối soát của 1 phiên | EF | Permission key đã comment, hiện chỉ cần login (DonHangDoiSoatLogController.cs:93-94) |
| 7 | Dropdown phiên đối soát | combo chọn phiên | GET `api/DonHangDoiSoatLog/DropdownPhienDoiSoat` | `[SBAuthorize]` | public | - | List phiên đối soát để chọn | EF | - |
| 8 | List đơn chưa xử lý (filter) | grid "Chưa xử lý" | POST `api/DonHangDoiSoatLog/GetDonHangDoiSoatChuaXuLyFilter` | `[SBAuthorize]` | public | - | Lọc + phân trang đơn đối soát chưa xử lý | EF | - |
| 9 | Cập nhật trạng thái đối soát (lô) | "Cập nhật" | POST `api/DonHangDoiSoatLog/CapNhatTrangThaiDoiSoat` | (key comment → mở) | public | tick đơn | Cập trạng thái đối soát theo lô | EF | Key comment (`:115`) |
| 10 | Cập trạng thái đối soát từng đơn | per-row | POST `api/DonHangDoiSoatLog/CapNhatTrangThaiDoiSoatSingleDonHang` | (key comment → mở) | public | 1 đơn | Cập trạng thái đối soát 1 đơn | EF | - |
| 11 | Cập trạng thái đối soát (cho KT) | luồng kế toán | POST `api/DonHangDoiSoatLog/CapNhatTrangThaiDoiSoatSingleDonHangAsync_ForKT` | `[SBAuthorize]` | public | 1 đơn (KT) | Biến thể cập trạng thái dành cho kế toán | EF | - |
| 12 | Lịch sử đối soát | tab "Lịch sử" | POST `api/DonHangDoiSoatLog/GetLichSuDoiSoatDonHang` | (key comment → mở) | public | - | List lịch sử đối soát theo filter | EF | - |
| 13 | Chi tiết lịch sử đối soát | mở dòng lịch sử | POST `api/DonHangDoiSoatLog/GetChiTietLichSuDoiSoatDonHang` | (key comment → mở) | public | - | Chi tiết 1 bản ghi lịch sử | EF | - |
| 14 | Lấy mã đơn đối soát theo Ids | nội bộ tra cứu | POST `api/DonHangDoiSoatLog/GetListMaDonDoiSoatTheoMas` | `[SBAuthorize]` | public | - | Trả list mã đơn theo Ids truyền vào | EF | - |
| 15 | Cập IdKho cho chi tiết lịch sử | per-row | POST `api/DonHangDoiSoatLog/CapNhatIdKhoDoiSoatLichSuDetail` | `[SBAuthorize]` | public | - | Gán IdKho cho các bản ghi chi tiết theo Ids | EF | - |
| 16 | Cập đối soát + tạo phiếu | "Tạo phiếu" | POST `api/DonHangDoiSoatLog/CapNhatDoiSoatDonTaoPhieuAsync` | `[SBAuthorize]` | public | đơn đủ điều kiện | Cập trạng thái đối soát đồng thời tạo phiếu (ripple kế toán) | EF | Side-effect tạo phiếu → kiểm tra service |

## Quan hệ
- **Gọi sang:** `IDonHangDoiSoatLogService` (toàn bộ logic import/đối soát/lịch sử/tạo phiếu); GemBox cho xuất Excel template/file lỗi.
- **Dữ liệu (entities):** `DonHangDoiSoatLog` / phiên đối soát (`DonHangDoiSoatSession`) + chi tiết đối soát từng đơn (so khớp phí ship/COD đối tác vs hệ thống).
- **Ripple / đồng bộ:** `CapNhatDoiSoatDonTaoPhieuAsync` và nhánh `_ForKT` tạo/cập chứng từ kế toán từ kết quả đối soát → ripple sang Kế toán (kiểm tra service để biết phiếu nào).

## Lỗi đã biết / lưu ý kỹ thuật
- Nhiều `[SBAuthorize("key",...)]` đã COMMENT, thay bằng `[SBAuthorize]` mở (vd `:93, :115, :123, :144, :152`) → phân quyền key NHẠT, chỉ còn login + tiện ích `TI_OrderLogistic`. Triage "user không có quyền vẫn thao tác được" có thể do key bị comment.
- 2 endpoint cũ `UpdateDoiSoatTheoNhieuMaDon` và `ContinueDoWork` đã COMMENT toàn bộ (`:26-39`) — không còn route, bỏ qua.
- File xuất là `.xlsm` (macro) qua GemBox — content-type XlsxDefault nhưng tên file `.xlsm`.
- Logic tạo phiếu/ripple kế toán nằm trong service `CapNhatDoiSoatDonTaoPhieuAsync`, không ở controller.
