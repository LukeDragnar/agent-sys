# Đối soát đơn online (Kế toán tác nghiệp) — Luồng

## Tiền điều kiện
- Đơn vị bật tiện ích logistic kế toán (`TI_KeToanLogistic`).
- Người dùng có vai kế toán (`DV-KT`, `DV-KT-TONG`) hoặc CSKH (`HT-CSKH`); một số thao tác admin (đăng/hủy đăng) thêm `DV-AD-*`.
- Có đơn hàng online (`DonHangLogistic`) để tác nghiệp.
- Cấu hình `ChoPhepKeToanDangDon` (mặc định cho phép) nếu muốn kế toán đăng đơn.

## Luồng dùng (kế toán)
1. Mở **Kế toán > Kế toán tác nghiệp** → `TimTheoDieuKien` list đơn theo bộ lọc, hoặc `TimTheo_TrangThaiGiaoHang` theo tab trạng thái.
2. Xem chi tiết đơn → `OrderLogistic_KeToanTacNghiepViewDetail`; tra theo SĐT → `GetOrderLogisticByPhone` / `GetOrderLogisticHistoryByPhone`.
3. Đối soát/tác nghiệp:
   - **Đăng đơn:** `InsertDonHangDangDon` (enqueue) → `ProcessDonHangDangDon` (bốc SP có lock → gửi ĐTGV + trừ kho).
   - **Hủy đăng đơn:** `InsertDonHangHuyDangDon` → `ProcessDonHangHuyDangDon` (hủy vận đơn + hoàn kho).
   - **Cập trạng thái giao hàng:** `CapNhatTrangThaiGiaoHangTheoDon`.
   - **Ghi chú kế toán:** `UpdateGhiChuKeToan`; **tin nhắn nội bộ:** `SaveMessage`/`GetListMessageByIdDonHangLogistic`/`GetListMessageByPhone`.
   - **Blacklist:** `InsertOrUpdateBlackList`/`DeleteBlackList`; **care đơn:** `UpdateTrangThaiCareDon`; **đổi ngày giao:** `UpdateNgayMuonNhanHang`; **đổi mã đơn:** `ThayDoiMaDon`.
4. **Xuất Excel** đối chiếu → `ExportExcelKeToanTacNghiepKieu1..4`.

## Luồng cấu hình (admin)
- Bật cấu hình cho phép kế toán đăng đơn (`ChoPhepKeToanDangDon`).
- Cấu hình mốc trạng thái giao hàng để tạo phiếu bán hàng kế toán / phát hành hóa đơn tự động (liên quan feature "Tạo hóa đơn tự động").

## Các trường hợp / case biên
- **"Đối soát nội bộ" (đánh dấu đã đối soát):** endpoint `UpdateDoiSoatNoiBo` đang **comment** (`KeToanTacNghiepController.cs:254-256`) → chức năng đánh dấu đối soát riêng KHÔNG dùng được.
- **Hủy đăng đơn TMĐT:** đơn sàn bị chặn hủy đăng ở service chung.
- **Đăng đơn khi cấu hình `ChoPhepKeToanDangDon=false`:** kế toán không đăng được đơn (trừ nhánh Admin đăng đơn bỏ qua gate).
- **Hủy đăng admin:** `AdminHuyDangDon` chỉ `DV-AD-TONG`/`HT-CSKH`.
- **Đăng đơn thiếu tồn / lock:** giống Thủ kho tác nghiệp (dùng chung `IThuKhoTacNghiepService`) → xem node `KhoTacNghiepController.md`.
- **Xuất Excel lỗi:** service bận → trả 503 (`Service_Unavailable`), lỗi khác → 500 (`EXPORT_ERROR`).
- **Export kiểu 1 và 2 trùng tên file** `KeToanTacNghiep.xlsx`.

## Ví dụ cụ thể có số
Kế toán chi nhánh online cần đối soát 50 đơn giao trong ngày.
- Lọc trạng thái "Đã giao hàng" → ra 50 đơn → xuất Excel kiểu 2 (`KeToanTacNghiep.xlsx`) để đối chiếu doanh thu với báo cáo giao vận.
- 3 đơn khách báo sai địa chỉ → kế toán ghi chú kế toán + đổi ngày muốn nhận hàng, 1 đơn cần hủy đăng (đơn thường, không phải sàn) → `InsertDonHangHuyDangDon` → `ProcessDonHangHuyDangDon` hủy vận đơn + hoàn kho.
- 1 số điện thoại boom hàng nhiều lần → thêm vào blacklist.

## Hậu điều kiện
- **Đăng đơn:** sinh vận đơn ĐTGV + trừ kho; đơn chuyển trạng thái DaDang.
- **Hủy đăng:** hủy vận đơn + hoàn kho.
- **Ghi chú/care/blacklist/đổi ngày:** cập nhật dữ liệu đơn/contact, không sinh chứng từ kế toán trực tiếp.
- **Xuất Excel:** read-only.
- Khi đơn đạt mốc trạng thái cấu hình → có thể trigger tạo phiếu bán hàng kế toán + hóa đơn tự động (luồng riêng).
