# Báo cáo sale & lead — Luồng

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị + chi nhánh.
- Có quyền xem báo cáo (đa số public ở mức controller).
- Đơn vị đã khai báo cơ cấu tổ chức Sale (nhóm/nhân viên) và có dữ liệu công việc/contact/đơn.
- (Tối ưu sale) Nếu dùng tổng đài, dữ liệu cuộc gọi MyHub/OmiCall phải có để ghép.
- Nếu bật giới hạn khoảng ngày, khoảng Từ–Đến phải nằm trong giới hạn.

## Luồng dùng (user)
1. Mở module **Báo cáo** → chọn Báo cáo công việc sale hoặc Báo cáo tối ưu sale.
2. Chọn **chi nhánh** + **khoảng ngày** + lọc **cơ cấu tổ chức Sale**.
3. Hệ thống validate khoảng ngày → ép chi nhánh user (nếu thường) → lọc cơ cấu tổ chức (LoaiNhanVien=1 cho Sale).
4. Chạy stored proc tương ứng (read-only). **Tối ưu sale** chạy SP chính + 2 SP tổng đài (MyHub, OmiCall) để enrich.
5. Trả về bảng + tổng; user phân trang, sắp xếp.
6. Bấm **Xuất Excel** (nếu có) → build lại + xuất `.xlsx`.

## Luồng cấu hình (admin)
- Khai báo cơ cấu tổ chức Sale (nhóm/nhân viên) — quyết định ai vào báo cáo.
- (Tối ưu sale) Cấu hình tích hợp tổng đài MyHub/OmiCall để có dữ liệu cuộc gọi.
- Cấu hình giới hạn khoảng ngày xem báo cáo.

## Các trường hợp / case biên
- **Vượt giới hạn khoảng ngày** → lỗi "Chỉ được xem dữ liệu trong khoảng N ngày".
- **User thường không chọn chi nhánh** → ép về chi nhánh user; chỉ thấy phạm vi cơ cấu tổ chức được phép.
- **Không dùng tổng đài** → tối ưu sale vẫn chạy SP chính nhưng cột cuộc gọi (MyHub/OmiCall) trống.
- **Không có công việc trong kỳ** → bảng rỗng, không lỗi.
- **Xuất Excel khi service bận** → trả 503.

## Ví dụ cụ thể có số
Shop F có 8 Sale (2 nhóm), dùng tổng đài MyHub, kỳ 01–30/06.
- Quản lý mở **Báo cáo công việc sale** → bảng: Sale Hùng có 320 lượt chăm sóc, 45 đơn chốt; Sale Lan 280 lượt, 38 đơn.
- Mở **Báo cáo tối ưu sale** → ngoài số liệu chăm sóc/chốt còn có cột cuộc gọi: Hùng 410 cuộc gọi (lấy từ MyHub), tỉ lệ chốt/cuộc gọi…
- Trưởng nhóm B (chỉ xem nhóm B) → chỉ thấy 4 Sale của nhóm mình.
- Đơn vị khác chưa dùng tổng đài mở tối ưu sale → cột cuộc gọi trống, các cột còn lại bình thường.
- Chọn khoảng 200 ngày nhưng giới hạn 90 ngày → báo lỗi giới hạn ngày.

## Hậu điều kiện
- Truy vấn báo cáo **không thay đổi dữ liệu** (read-only), không bắn message/event.
- Log query chậm khi super/CSKH hoặc đơn vị bật "show query".
