---
controller: DonHangTrangThaiPhatHanhHoaDonLogController
module: kho
service: OrderLogistic
man_hinh_ui: "Kế toán > Cập nhật trạng thái phát hành hóa đơn hàng loạt (paste mã đơn) — textarea nhập mã đơn + chọn trạng thái phát hành, grid log kết quả."
muc_dich: "Cập nhật trạng thái phát hành hóa đơn (InvNo/InvDate/TrangThaiPhatHanhHoaDon) hàng loạt theo nhiều mã đơn / mã vận đơn theo pattern queue + worker: enqueue log rows, ContinueDoWork xử lý từng đơn (validate đơn đủ điều kiện phát hành → set/clear thông tin hóa đơn trên đơn logistic)."
alias: ["phát hành hóa đơn log", "cập nhật trạng thái phát hành hóa đơn", "DonHangTrangThaiPhatHanhHoaDonLog", "phát hành hóa đơn hàng loạt", "hủy phát hành hóa đơn"]
auth: public
state_machine: [ChuaXuly, DaXuLy, ThanhCong, ThatBai]
deprecated: false
entities: [DonHangTrangThaiPhatHanhHoaDonLog, DonHangLogistic]
stored_procs: [PRC_SCRM_OrderLogistic_DonHangLogistic_GetByMaDon, PRC_SCRM_OrderLogistic_DonHangLogistic_GetByMaDonGiaoVan, PRC_SCRM_Order_Logistic_DonHangLogistic_DonHangTrangThaiPhatHanhHoaDonLog_Delete_By_IdDonVi]
depends:
  - "IDonHangTrangThaiPhatHanhHoaDonLogService (DonHangTrangThaiPhatHanhHoaDonLogService.cs — enqueue + ContinueDoWork worker)"
  - "IDonHangRabitService (KiemTraThayDoiDonHang khi phát hành thành công)"
  - "ILogLichSuTacNghiepService (ghi lịch sử tác nghiệp PhatHanhThanhCong)"
  - "ICommonElasticSearchService (ESIndexDonHangLogistic)"
cross_module_in: []
cross_module_out:
  - "Customer/Contact (KiemTraThayDoiDonHang qua DonHangRabitService khi phát hành thành công — đồng bộ thay đổi đơn về khách hàng)"
---

# DonHangTrangThaiPhatHanhHoaDonLogController — Cập nhật trạng thái phát hành hóa đơn hàng loạt (log + worker)

> TienIch `DonHangTrangThaiPhatHanhHoaDonLogController` / `MaTienIchSCRM.TI_OrderLogistic`. Pattern **queue + worker** giống DonHangTtghLog. **Đặc biệt: tất cả endpoint dùng `[SBAuthorize]` rỗng** (permission key chi tiết đã bị comment) — chỉ cần đăng nhập là gọi được.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Enqueue cập nhật phát hành hóa đơn theo nhiều mã đơn | "Cập nhật" (sau paste mã đơn + chọn trạng thái) | POST `api/DonHangTrangThaiPhatHanhHoaDonLog/UpdateTrangThaiPhatHanhHoaDonTheoNhieuMaDon` | `[SBAuthorize]` (mở — key bị comment) | public | textarea có mã đơn | Tách chuỗi mã đơn → BulkInsert log rows (ChuaXuly). Nếu trạng thái ≠ ChuaPhatHanh → sinh InvNo `MANUAL{yyyyMMdd}` + InvDate + ActionType=Publish; nếu = ChuaPhatHanh → clear hóa đơn. Chặn nếu >5000 log; cleanup log cũ nếu >2000 | EF (Bulk) + SP delete | InvNo gắn tiền tố `MANUAL` để phân biệt phát hành thủ công vs tự động |
| 2 | Xử lý từng đơn trong hàng đợi | (poll nền) | POST `api/DonHangTrangThaiPhatHanhHoaDonLog/ContinueDoWork` | `[SBAuthorize]` (mở) | public | có log ChuaXuly | Bốc 1 log → lấy đơn bằng SP theo mã → validate (xem Quy tắc) → set/clear InvNo/InvDate/TrangThaiPhatHanhHoaDon/InvoiceActionType trên đơn → ThanhCong/ThatBai; phát hành OK → KiemTraThayDoiDonHang + log tác nghiệp | SP + EF | Chạy trong execution strategy + transaction |
| 3 | Đếm tổng log | (badge) | POST `api/DonHangTrangThaiPhatHanhHoaDonLog/GetDonHangTrangThaiPhatHanhHoaDonLogTotal` | `[SBAuthorize]` (mở) | public | - | Đếm log Tổng/ĐãXử lý/ChưaXử lý/ThanhCong/Lỗi | EF | - |
| 4 | List log theo user | (grid) | POST `api/DonHangTrangThaiPhatHanhHoaDonLog/GetDonHangTrangThaiPhatHanhHoaDonLogByUser` | `[SBAuthorize]` (mở) | public | - | List log phân trang, lọc theo MaTrangThai/StatusProcessd/KeyWord (mã đơn hoặc mã vận đơn) | EF | - |
| 5 | Xóa toàn bộ log | "Xóa danh sách" | POST `api/DonHangTrangThaiPhatHanhHoaDonLog/DeleteDonHangTrangThaiPhatHanhHoaDonLog` | `[SBAuthorize]` (mở) | public | - | Xóa toàn bộ log của đơn vị+chi nhánh | EF | - |

## Quy tắc nghiệp vụ (gating phát hành trong ContinueDoWork)
1. Đơn đã ở đúng trạng thái phát hành đích → ThatBai "Đơn hàng đã ở trạng thái: X".
2. Chỉ phát hành khi `GiaoHangTrangThaiMa` ∈ {DaThanhToan, DaGiaoHang, GiaoHangMotPhan} — ngoài ra ThatBai (DonHangTrangThaiPhatHanhHoaDonLogService.cs:200-208).
3. Nếu đơn đã có `InvNo` KHÔNG chứa `MANUAL` (= phát hành tự động) → ThatBai "đã phát hành tự động nên không thể hủy thủ công" (c.cs:210-215).
4. Đơn chưa chốt (`DonHangTrangThaiChotDon != DaChotDon`) → ThatBai (c.cs:217-222).
5. Thành công + target ≠ ChuaPhatHanh → set InvNo/InvDate/TrangThaiPhatHanhHoaDon/InvoiceActionType; target = ChuaPhatHanh → clear hết về null (hủy phát hành).

## Quan hệ
- **Gọi sang:** lấy đơn bằng SP `..._GetByMaDon` / `..._GetByMaDonGiaoVan`; sau phát hành OK gọi `DonHangRabitService.KiemTraThayDoiDonHang` (đồng bộ thay đổi về khách hàng) + ghi LogLichSuTacNghiep + ES reindex đơn.
- **Dữ liệu (entities):** `DonHangTrangThaiPhatHanhHoaDonLog` (log queue), `DonHangLogistic` (InvNo, InvDate, TrangThaiPhatHanhHoaDon, NgayTacDongPhatHanhHoaDon, InvoiceActionType).
- **Ripple / đồng bộ:** phát hành → cập nhật thông tin hóa đơn trên đơn + đồng bộ thay đổi đơn về khách hàng (Customer).

## Lỗi đã biết / lưu ý kỹ thuật
- **Toàn bộ endpoint `[SBAuthorize]` rỗng** — permission key chi tiết (`DonHangTrangThaiPhatHanhHoaDonLog_*`) bị comment hết (c.cs:24-25,32-33,40-41,48-49,56-57). Chỉ cần đăng nhập là cập nhật/hủy phát hành hóa đơn hàng loạt được. Cần xác nhận có chủ ý.
- `GetDonHangTrangThaiPhatHanhHoaDonLogByUserAsync` có `throw;` sau `return` (c.cs:109-110) — dead code (không reachable).
- `ContinueDoWork`: khi log đầu không có dữ liệu, gọi `MethodResult.ResultWithError` nhưng KHÔNG return (c.cs:128-132) → tiếp tục dùng biến null `donHangTrangThaiPhatHanhHoaDonLog` ở dòng sau → tiềm ẩn NullReferenceException nếu queue rỗng lọt qua filter.
- Phân biệt phát hành thủ công vs tự động qua tiền tố `MANUAL` trong InvNo — sửa logic phát hành phải giữ quy ước này.
