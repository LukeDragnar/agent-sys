# Luồng — Báo cáo giao vận

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị (DonViId) và chi nhánh (IdChiNhanh).
- Có khóa quyền tính năng `BaoCaoOnlineController_ReportDoanhSoTheoGiaoVan` (báo cáo và xuất Excel dùng CHUNG khóa này).
- Đơn vị đã có đơn hàng giao vận phát sinh, đã gắn đơn vị vận chuyển.
- Cấu hình "giới hạn khoảng ngày được phép tra báo cáo" của đơn vị (nếu bật) sẽ chặn khoảng quá rộng.

## Luồng dùng (user)
1. Người dùng mở **Báo cáo → Báo cáo online → Báo cáo doanh số theo giao vận**.
2. Chọn bộ lọc: chi nhánh, Từ ngày / Đến ngày, cơ cấu tổ chức (phòng ban / nhóm / nhân viên sale-MKT).
3. Hệ thống kiểm tra giới hạn khoảng ngày; nếu vượt → chặn ("Chỉ được xem dữ liệu trong khoảng N ngày").
4. Nếu user không phải admin/super/test/CSKH và chưa chọn chi nhánh → hệ thống ép về chi nhánh của user.
5. Hệ thống lọc cơ cấu tổ chức: chỉ lấy nhóm/nhân viên mà user được phép xem (qua kiểm quyền dropdown nhóm & user).
6. Chạy báo cáo (stored proc), trả về bảng doanh số/đơn theo từng đơn vị vận chuyển.
7. Người dùng có thể bấm **Xuất Excel** → hệ thống dựng lại cùng dữ liệu thành file `.xlsx`.

## Luồng cấu hình (admin)
- Admin đơn vị bật/đổi **giới hạn khoảng ngày được phép tra báo cáo** (ảnh hưởng tất cả báo cáo online, không riêng giao vận).
- Phân quyền: gán khóa quyền tính năng cho vai trò để cho phép xem báo cáo này.

## Các trường hợp / case biên
- **Khoảng ngày vượt giới hạn cấu hình** → chặn ngay, trả thông báo "Chỉ được xem dữ liệu trong khoảng N ngày", không chạy báo cáo.
- **User thường, không chọn chi nhánh** → ép về chi nhánh của user (không xem toàn đơn vị).
- **User không có nhóm/nhân viên nào được phép xem** → báo cáo lọc về phạm vi rỗng → bảng có thể trống dù vẫn có đơn trong đơn vị.
- **Không có đơn giao vận trong kỳ** → bảng trống (không lỗi).
- **Xuất Excel khi service xuất bận** → trả mã 503 (FE báo không xuất được lúc này); lỗi khác → 500.
- **Đơn chưa gắn đơn vị vận chuyển** → không nằm trong nhóm doanh số theo ĐVVC.

## Ví dụ cụ thể có số
Shop B trong tháng 6 có 500 đơn giao đi, chia: Viettel Post 220 đơn (doanh thu 180tr), GHN 150 đơn (120tr), J&T 90 đơn (70tr), GHTK 40 đơn (30tr).
- Anh Minh (admin tổng) lọc "chi nhánh = tất cả, 01/06–30/06" → báo cáo hiện đủ 4 hãng, tổng 500 đơn / 400tr.
- Chị Hà (nhân viên 1 chi nhánh) mở cùng báo cáo: hệ thống ép về chi nhánh của chị → chỉ thấy phần đơn của chi nhánh đó, ví dụ Viettel 60 / GHN 40 / J&T 20.
- Anh Minh lọc "01/01–30/06" nhưng đơn vị giới hạn 90 ngày → bị chặn "Chỉ được xem dữ liệu trong khoảng 90 ngày"; thu lại còn "01/04–30/06" thì xem được.
- Anh Minh bấm Xuất Excel → file chứa đúng 4 dòng hãng vận chuyển với số đơn/doanh thu tương ứng.

## Hậu điều kiện
- Truy vấn **không thay đổi dữ liệu** (read-only); chỉ đọc qua stored proc trên DB báo cáo.
- Xuất Excel không sửa đơn hàng; chỉ tạo file.
