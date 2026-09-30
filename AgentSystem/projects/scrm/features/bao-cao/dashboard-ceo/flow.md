# Luồng — Dashboard tổng quan / CEO

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc đơn vị (DonViId) và chi nhánh (IdChiNhanh).
- Có vai trò xem Dashboard CEO: `DV-VIEW-REPORT-CEO` cùng `DV-AD-TONG` / `HT-AD` / `HT-CSKH` (cần product xác nhận chính xác từng widget).
- Đơn vị có dữ liệu (đơn hàng, contact, chấm công, tổng đài...) trong kỳ.
- Cấu hình giới hạn khoảng ngày (nếu bật) chặn khoảng quá rộng.

## Luồng dùng (user)
1. Lãnh đạo mở **Báo cáo → Dashboard CEO**.
2. Chọn bộ lọc: chi nhánh, Từ ngày / Đến ngày, cơ cấu tổ chức.
3. Hệ thống kiểm tra giới hạn khoảng ngày.
4. Dashboard load:
   - **Cách 1 (load 1 lần):** gọi endpoint aggregator → chạy nhiều stored proc CEO trong 1 request, gộp kết quả về (mở connection riêng cho từng phần nặng).
   - **Cách 2 (load từng widget):** mỗi widget gọi endpoint riêng (doanh thu chi nhánh, phễu online, bảng xếp hạng, chấm công, tổng đài...).
5. Hiển thị các nhóm: offline (doanh thu/lịch hẹn/lấp đầy/chân dung KH), online (phễu/số liệu chung/bảng xếp hạng/giao vận/công việc sale), chấm công, tổng đài, doanh số (nguồn/ngày/SP), chia lead, nhãn/trạng thái contact.

## Luồng cấu hình (admin)
- Cấp vai trò xem Dashboard CEO cho lãnh đạo (`DV-VIEW-REPORT-CEO`...).
- Bật/đổi giới hạn khoảng ngày tra báo cáo.

## Các trường hợp / case biên
- **Không có vai trò xem CEO** → không thấy menu hoặc bị chặn quyền.
- **Khoảng ngày vượt giới hạn** → chặn "Chỉ được xem dữ liệu trong khoảng N ngày".
- **Load aggregator với khoảng ngày rộng / dữ liệu lớn** → chạy nhiều SP cùng lúc (fan-out) → chậm; nên thu hẹp khoảng ngày.
- **Một widget lỗi/không có dữ liệu** → widget đó trống, các widget khác vẫn hiển thị.
- **Widget chân dung khách hàng (CustomerReportVisitors)**: có lỗi đã biết ở tầng log (log sai tên stored proc) — kết quả vẫn đúng, chỉ gây nhầm khi debug (xem dev.md).

## Ví dụ cụ thể có số
Chuỗi D có 5 chi nhánh, tháng 6: doanh thu 5 tỉ, 12.000 đơn, 8.000 contact, 30 nhân viên sale.
- CEO mở Dashboard, lọc "tất cả chi nhánh, 01/06–30/06" → widget doanh thu chi nhánh hiện 5 dòng (mỗi chi nhánh 0,8–1,4 tỉ); phễu online hiện 8.000 contact → 3.500 lead → 1.200 đơn chốt; bảng xếp hạng sale hiện top 10 nhân viên; chấm công hiện tổng quát 30 NV.
- CEO chọn "01/01–30/06" nhưng giới hạn 90 ngày → bị chặn; thu lại "01/04–30/06" thì load được.
- CEO mở dashboard bằng aggregator (load 1 lần) → hệ thống chạy ~20 stored proc CEO trong 1 request; nếu chậm, lọc 1 chi nhánh để nhẹ hơn.

## Hậu điều kiện
- Truy vấn **không thay đổi dữ liệu** (read-only); chỉ đọc qua stored proc.
- Không bắn message/event.
