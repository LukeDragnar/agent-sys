# Luồng — Báo cáo quỹ (Báo cáo quỹ tài khoản)

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị + chi nhánh.
- Phải có quyền xem báo cáo đơn hàng (gate `CheckQuyenXemReportOrder`, cấu hình bên module Đơn hàng).
- Đã có phiếu thu-chi phát sinh (nguồn dữ liệu cho báo cáo).

## Luồng dùng
1. Mở **Kế toán → Báo cáo → Báo cáo quỹ tài khoản**.
2. Hệ thống gọi cổng kiểm quyền xem báo cáo đơn hàng — `Data==false` → chặn ("Bạn không có quyền xem báo cáo đơn hàng"); không xác định được → "Không xác định được quyền hạn của user".
3. Người dùng chọn bộ lọc: chi nhánh, Từ/Đến ngày, loại tài khoản, loại đối tượng (LoaiDoiTuong), kiểu xem (TypeView), từ khóa.
4. Hệ thống build tham số → chạy stored proc `PRC_SCRM_Accounting_ButToan_BaoCaoQuyTaiKhoan` → trả bảng số dư quỹ + tổng số bản ghi.
5. Người dùng bấm một dòng → xem **chi tiết theo mã phiếu**: gọi `GetBaoCaoQuyTaiKhoan_ChiTiet` (thêm tham số IdDoiTuong, MaDoiTuong) → stored proc `..._ByMaPhieu` → liệt kê từng giao dịch.
6. Nếu thời gian chạy stored proc vượt ngưỡng (`GetTimeToLog` từ Report) → ghi log query chậm.

## Luồng cấu hình (admin)
- Không có luồng cấu hình riêng trong phạm vi báo cáo này; quyền xem điều khiển bởi cấu hình "quyền xem báo cáo đơn hàng" bên module Đơn hàng.

## Các trường hợp / case biên
- **Không có quyền xem báo cáo:** chặn ngay trước khi chạy báo cáo.
- **Khoảng ngày dài / dữ liệu lớn:** báo cáo chạy chậm (stored proc nặng); ghi log nếu vượt ngưỡng.
- **Không có phiếu trong kỳ:** báo cáo trả rỗng (0 dòng).
- **Tên đối tượng thiếu:** nếu dữ liệu KH/NCC không có, cột tên có thể trống.
- **Báo cáo là read-only:** mọi thao tác không thay đổi dữ liệu.

## Ví dụ cụ thể có số
Chi nhánh A, lọc tháng 6, loại tài khoản = tiền mặt:
- Quỹ tiền mặt đối tượng "Quỹ chính": đầu kỳ 10.000.000đ, thu trong kỳ 5.000.000đ, chi 2.000.000đ → số dư cuối 13.000.000đ.
- Bấm vào dòng "Quỹ chính" → chi tiết theo mã phiếu: PT001 +3.000.000, PT002 +2.000.000, PC001 −1.500.000, PC002 −500.000.
- Đổi loại tài khoản = ngân hàng → báo cáo hiện số dư tài khoản ngân hàng tương ứng.

## Hậu điều kiện
- Read-only: báo cáo không thay đổi dữ liệu, chỉ phản ánh kết quả của các phiếu thu-chi/quỹ.
