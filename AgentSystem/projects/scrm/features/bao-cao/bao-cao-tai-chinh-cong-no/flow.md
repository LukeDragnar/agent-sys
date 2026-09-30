# Luồng — Báo cáo tài chính (công nợ)

> Luồng nghiệp vụ đầy đủ (lập/cấn trừ phiếu bù trừ công nợ) ở `features/ke-toan/cong-no/flow.md`. Đây chỉ là luồng phần **xem báo cáo**.

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc đơn vị.
- Qua được cổng kiểm quyền xem báo cáo đơn hàng (`CheckQuyenXemReportOrder` — cấu hình bên Đơn hàng).
- Đơn vị có phát sinh công nợ (từ phiếu mua/bán).

## Luồng dùng (user)
1. Vào **Kế toán → Báo cáo → Báo cáo công nợ tổng hợp**.
2. Chọn tab loại tài khoản (131 phải thu / 331 phải trả), lọc chi nhánh, Từ/Đến ngày, nhóm đối tượng, từ khóa.
3. Hệ thống gate quyền (`CheckQuyenXemReportOrder`); không có quyền → chặn.
4. Nếu chọn **nhóm đối tượng** → hệ thống resolve list mã KH+NCC; nếu rỗng truyền sentinel để SP trả về rỗng.
5. Chạy báo cáo → bảng công nợ từng đối tượng (đã enrich tên đối tượng từ Khách hàng + Nhà cung cấp).
6. Bấm một dòng đối tượng → drill-down xem chi tiết từng chứng từ.

## Luồng cấu hình (admin)
- Cấp quyền "xem báo cáo đơn hàng" bên cấu hình Đơn hàng cho vai trò cần xem báo cáo công nợ.

## Các trường hợp / case biên
- **Không qua gate quyền** → "Bạn không có quyền xem báo cáo đơn hàng" (hoặc "Không xác định được quyền hạn của user" nếu không kiểm tra được).
- **Không chọn loại tài khoản** → báo cáo công nợ tổng hợp validate `LoaiTaiKhoan` (xem dev.md — logic validate hiện lỏng, giá trị lạ vẫn lọt).
- **Nhóm đối tượng rỗng** → truyền sentinel `"KHONG_CO_DU_LIEU"` → SP không trả gì → bảng trống.
- **Mã đối tượng trùng cả KH lẫn NCC** → tên hiển thị "KH: ... - NCC: ...".
- **Không có công nợ trong kỳ** → bảng trống (không lỗi).

## Ví dụ cụ thể có số
Đơn vị E có 30 khách còn nợ (phải thu) tổng 200tr, và nợ 8 nhà cung cấp (phải trả) tổng 80tr.
- Kế toán mở báo cáo, tab 131, lọc "tất cả, 01/06–30/06" → bảng 30 dòng khách, tổng phải thu 200tr; bấm 1 khách → chi tiết các phiếu bán chưa thu.
- Chuyển tab 331 → bảng 8 nhà cung cấp, tổng phải trả 80tr.
- Lọc "nhóm đối tượng = Đại lý" còn 5 đối tượng → bảng 5 dòng.
- Một đối tác vừa là KH vừa là NCC (mã `DT001`) → hiển thị "KH: Công ty A - NCC: Công ty A".

## Hậu điều kiện
- Truy vấn **không thay đổi dữ liệu** (read-only); chỉ đọc qua stored proc, không bắn message/event.
