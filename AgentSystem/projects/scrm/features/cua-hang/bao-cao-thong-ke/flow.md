# Luồng — Báo cáo & thống kê

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc đơn vị + chi nhánh.
- Đơn vị đã bật tiện ích báo cáo (TI_Report_Chung cho báo cáo đơn hàng; TI_Branch cho báo cáo chi nhánh).
- Có dữ liệu nguồn: đơn hàng **đã thanh toán** (doanh thu/doanh số/hoa hồng), nhân viên + xếp nhân viên (doanh số/hoa hồng NV), khung giờ + nhóm nhân viên (chấm công).
- Có quyền tương ứng; người dùng thường bị ép theo chi nhánh của mình.

## Luồng dùng (xem báo cáo)
1. Người dùng mở màn báo cáo, chọn bộ lọc (chi nhánh, Từ/Đến ngày, SP/DV, nhân viên…).
2. Hệ thống **gate khoảng ngày**: kiểm tra `CheckCauHinhKhoangThoiGianChoPhepTimKiem` — nếu vượt giới hạn đơn vị → chặn với thông báo giới hạn.
3. **Ép chi nhánh**: nếu user không phải admin/super/test/CSKH và không chọn chi nhánh → mặc định lấy chi nhánh của user.
4. Hệ thống build tham số → chạy **stored procedure** trên DB báo cáo (SCRM_Report) → trả dữ liệu lưới (jsonData + total) hoặc biểu đồ.
5. Một số báo cáo doanh thu gọi thêm SP enrich tên sản phẩm; báo cáo chấm công gọi cross-service Booking (khung giờ) + Core (nhóm nhân viên) trước khi chạy SP.
6. Lưới/biểu đồ hiển thị; báo cáo dạng lưới có phân trang.

## Báo cáo chính
**Nhóm đơn hàng / doanh thu (OrderReport):**
- Doanh thu theo ngày, theo chi nhánh, theo khách hàng, **theo thu ngân**, theo sản phẩm, theo nhân viên.
- Báo cáo bán sản phẩm (chi tiết / gom nhóm); thống kê KH dùng sản phẩm + chi tiết; lịch sử dùng thẻ dịch vụ; lượng khách (visitors).
- Dashboard: đếm đặt lịch thanh toán, tổng doanh thu, doanh số, thu-chi, biểu đồ SP/combo/nạp thẻ, tỉ lệ lấp đầy.

**Nhóm chi nhánh / nhân viên (BranchReport):**
- Dashboard doanh số thu-chi chi nhánh.
- **Hoa hồng nhân viên áp dụng.**
- Báo cáo doanh số nhân viên + **chi tiết 1 nhân viên** (drill-down, cần chọn nhân viên).
- Báo cáo chấm công tổng quát (theo chi nhánh) + chấm công của chính user.

## Các trường hợp / case biên
- **Vượt khoảng ngày cho phép** → chặn, thông báo "Chỉ được xem dữ liệu trong khoảng N ngày".
- **User thường không chọn chi nhánh** → tự ép chi nhánh của user.
- **Báo cáo chi tiết doanh số 1 NV** không truyền `IdNhanVien` → lỗi bắt buộc chọn nhân viên. Lưu ý PageIndex của báo cáo này là `Page - 1` (0-based), khác các báo cáo khác.
- **Chấm công thiếu khung giờ / nhóm nhân viên** → lỗi "Không tìm thấy khung giờ/nhóm nhân viên".
- **Chấm công bản user** → tự ép `ListIdChiNhanh = [chi nhánh user]` + `IdNhanVien = id user` → chỉ thấy của chính mình.
- **Đơn chưa thanh toán** → không vào doanh thu (SP cố định lọc trạng thái Đã thanh toán; doanh số NV cố định loại đối tượng Dịch vụ + hoa hồng thực hiện).
- **Widget biểu đồ dịch vụ (DashboardOrderReportDichVu)** → hiện lỗi runtime do tên SP placeholder (known-issue) → endpoint này luôn lỗi.

## Ví dụ cụ thể có số
Chuỗi 2 chi nhánh, ngày 01–30/06:
- Quản lý mở **báo cáo doanh thu thu ngân** lọc chi nhánh CN1, khoảng 01–30/06 → lưới: thu ngân A 120 đơn / 45.000.000đ, thu ngân B 80 đơn / 31.000.000đ.
- Mở **doanh số nhân viên** CN1 → NV An doanh số dịch vụ 18.000.000đ; bấm vào An → **chi tiết** từng đơn dịch vụ An phục vụ.
- Mở **hoa hồng nhân viên áp dụng** → An hoa hồng 5% × 18.000.000đ = 900.000đ (chỉ tính đơn đã thanh toán, phần dịch vụ có xếp An).
- Một nhân viên thường mở báo cáo chấm công → chỉ thấy chấm công của chính mình ở chi nhánh mình.
- Mở khoảng 01/01–30/06 (180 ngày) trong khi đơn vị giới hạn 90 ngày → bị chặn "Chỉ được xem dữ liệu trong khoảng 90 ngày".

## Hậu điều kiện
- Báo cáo **không thay đổi dữ liệu** (read-only, 100% qua stored proc, không bắn message/event).
- Có thể ghi log query chậm khi user là super/CSKH hoặc đơn vị bật show-query (phục vụ chẩn đoán hiệu năng).
