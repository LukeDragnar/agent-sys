# Báo cáo khách hàng — Luồng

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị + chi nhánh.
- Có quyền xem báo cáo (đa số public ở mức controller).
- Đơn vị đã có dữ liệu khách + đơn hàng trong khoảng ngày cần xem.
- Nếu bật giới hạn khoảng ngày báo cáo, khoảng Từ–Đến phải nằm trong giới hạn.

## Luồng dùng (user)
1. Mở module **Báo cáo** → chọn báo cáo khách hàng (Theo khách hàng / Khách hàng cũ-mới / Số liệu hiện tại theo tháng / Số liệu quay lại theo tháng).
2. Chọn **chi nhánh** + **khoảng ngày Từ–Đến**; tùy báo cáo lọc thêm cơ cấu tổ chức.
3. Hệ thống validate giới hạn khoảng ngày → ép phạm vi chi nhánh (nếu user thường) → lọc cơ cấu tổ chức (`GetCheckPermissionViewDropdownListNhomVaUser`).
4. Chạy stored proc báo cáo tương ứng (read-only).
5. Trả về bảng + tổng; user phân trang, sắp xếp.
6. Bấm **Xuất Excel** (nếu có) → build lại + xuất `.xlsx`.

## Luồng cấu hình (admin)
- Cấu hình giới hạn khoảng ngày xem báo cáo.
- Cấu hình cơ cấu tổ chức và quyền xem khách toàn đơn vị / theo nhân viên → ảnh hưởng phạm vi báo cáo.

## Các trường hợp / case biên
- **Vượt giới hạn khoảng ngày** → lỗi "Chỉ được xem dữ liệu trong khoảng N ngày", không chạy.
- **User thường không chọn chi nhánh** → ép về chi nhánh user.
- **User thuần Sale/MKT + cấu hình không cho xem khách nhân viên khác** → chỉ thấy khách mình phụ trách.
- **Không có khách phát sinh trong kỳ** → bảng rỗng, không lỗi.
- **Xuất Excel khi service bận** → trả 503.
- **Khách cũ/mới ranh giới kỳ**: khách mua lần đầu đúng vào ngày đầu kỳ tính là "mới"; phụ thuộc cờ đánh dấu khách cũ của hệ thống.

## Ví dụ cụ thể có số
Shop C trong tháng 6 có 200 khách phát sinh giao dịch.
- Quản lý mở **Khách hàng cũ/mới** 01–30/06 → báo cáo: 70 khách mới, 130 khách cũ.
- Mở **Số liệu quay lại theo tháng** → bảng theo tháng: tháng 4 có 50 khách mua lần đầu, trong đó 18 người quay lại mua ở tháng 5, 12 người quay lại ở tháng 6.
- Sale Lan (chỉ phụ trách 40 khách) mở **Báo cáo theo khách hàng** → chỉ thấy 40 khách của mình kèm số liệu mua/chăm sóc.
- Quản lý chọn khoảng 200 ngày trong khi đơn vị giới hạn 90 ngày → báo lỗi giới hạn ngày.

## Hậu điều kiện
- Truy vấn báo cáo **không thay đổi dữ liệu** (read-only), không bắn message/event.
- Log query chậm khi super/CSKH hoặc đơn vị bật "show query".
