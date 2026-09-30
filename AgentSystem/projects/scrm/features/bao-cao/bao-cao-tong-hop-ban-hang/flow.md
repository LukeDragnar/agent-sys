# Luồng — Báo cáo tổng hợp bán hàng

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc đơn vị (DonViId) và chi nhánh (IdChiNhanh).
- Có khóa quyền tính năng `BaoCaoOnlineController_ReportTongHopBanHang` (báo cáo + xuất Excel).
- Đơn vị có dữ liệu bán hàng/contact phát sinh trong kỳ.
- Cấu hình giới hạn khoảng ngày (nếu bật) sẽ chặn khoảng quá rộng.

## Luồng dùng (user)
1. Mở **Báo cáo → Báo cáo online → Báo cáo tổng hợp bán hàng**.
2. Chọn bộ lọc: chi nhánh, Từ ngày / Đến ngày, cơ cấu tổ chức (phòng ban / nhóm / nhân viên).
3. Hệ thống kiểm tra giới hạn khoảng ngày; vượt → chặn.
4. User không phải admin/super/test/CSKH và chưa chọn chi nhánh → ép về chi nhánh user.
5. Lọc cơ cấu tổ chức (nhóm/user được phép xem).
6. Chạy báo cáo (stored proc), trả bảng tổng hợp.
7. Có thể bấm **Xuất Excel** → dựng lại cùng dữ liệu thành `.xlsx`.

## Luồng cấu hình (admin)
- Bật/đổi giới hạn khoảng ngày tra báo cáo (ảnh hưởng toàn bộ báo cáo online).
- Phân quyền: gán khóa quyền tính năng cho vai trò.

## Các trường hợp / case biên
- **Khoảng ngày vượt giới hạn** → chặn "Chỉ được xem dữ liệu trong khoảng N ngày", không chạy.
- **User thường không chọn chi nhánh** → ép về chi nhánh user.
- **User không có nhóm/nhân viên được phép xem** → phạm vi rỗng → bảng có thể trống.
- **Không có dữ liệu bán hàng trong kỳ** → bảng trống (không lỗi).
- **Xuất Excel khi service bận** → 503; lỗi khác → 500.
- **Lưu ý kỹ thuật:** báo cáo này LUÔN ghi log SP (điều kiện log bị vô hiệu hóa — xem dev.md) → mỗi request đều ghi log, có overhead nhẹ.

## Ví dụ cụ thể có số
Shop C tháng 6 có 800 contact bán hàng, 300 đơn chốt, doanh thu 600tr.
- Anh Tâm (admin tổng) lọc "tất cả chi nhánh, 01/06–30/06" → bảng tổng hợp hiện đủ các chỉ số: 800 contact, 300 đơn chốt, 600tr doanh thu.
- Chị Mai (nhân viên 1 chi nhánh) mở cùng báo cáo → hệ thống ép về chi nhánh + nhóm của chị → chỉ thấy phần của chị, ví dụ 120 contact / 45 đơn / 90tr.
- Anh Tâm chọn "01/01–30/06" nhưng giới hạn 90 ngày → bị chặn; thu lại "01/04–30/06" thì xem được.
- Anh Tâm bấm Xuất Excel → file chứa đúng bảng tổng hợp đang hiển thị.

## Hậu điều kiện
- Truy vấn **không thay đổi dữ liệu** (read-only); đọc qua stored proc.
- Xuất Excel chỉ tạo file, không sửa dữ liệu gốc.
