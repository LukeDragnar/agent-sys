# Báo cáo sản phẩm — Luồng

## Tiền điều kiện
- Người dùng đã đăng nhập, thuộc một đơn vị + chi nhánh.
- Có quyền xem báo cáo (đa số public ở mức controller).
- Đơn vị đã có danh mục sản phẩm/combo và dữ liệu đơn hàng.
- Nếu bật giới hạn khoảng ngày, khoảng Từ–Đến phải nằm trong giới hạn.

## Luồng dùng (user)
1. Mở module **Báo cáo** → chọn báo cáo sản phẩm (Tỉ lệ chốt đơn SP / Combo / Combo sản phẩm / Số ngày chưa bán được SP).
2. Chọn **chi nhánh** + **khoảng ngày**; tùy báo cáo lọc thêm sản phẩm.
3. Hệ thống validate khoảng ngày → ép chi nhánh user (nếu thường).
4. Chạy stored proc tương ứng (read-only). Tỉ lệ chốt đơn chạy 2 SP (số contact + SP chính) rồi gộp.
5. Trả về bảng + tổng; user phân trang, sắp xếp.
6. Bấm **Xuất Excel** (nếu có).

## Luồng cấu hình (admin)
- Cấu hình giới hạn khoảng ngày xem báo cáo.
- Danh mục sản phẩm/combo phải được khai báo để báo cáo có dữ liệu để gom nhóm.

## Các trường hợp / case biên
- **Vượt giới hạn khoảng ngày** → lỗi "Chỉ được xem dữ liệu trong khoảng N ngày".
- **User thường không chọn chi nhánh** → ép về chi nhánh user.
- **Sản phẩm chưa từng bán** → ở báo cáo "số ngày chưa bán được" hiện số ngày lớn; ở tỉ lệ chốt/combo có thể không xuất hiện.
- **Xuất Excel combo** → build từ `List<ReportComboDto>` FE gửi lên, **không re-query SP**; nếu lưới thiếu dữ liệu thì file thiếu.
- **Xuất Excel combo sản phẩm khi lỗi** → controller trả `BadRequest(Message)` (khác chuẩn các export khác trả 503/500).
- **Không có dữ liệu** → bảng rỗng, không lỗi.

## Ví dụ cụ thể có số
Shop D bán 50 SP, 8 combo trong tháng 6.
- Quản lý mở **Tỉ lệ chốt đơn SP** 01–30/06 → SP "Kem dưỡng" có 120 contact quan tâm, 36 chốt → tỉ lệ 30%.
- Mở **Báo cáo combo** → combo "Spa cơ bản" bán 25 lượt; mở **Combo sản phẩm** thấy trong combo đó SP "Tinh dầu" được dùng 25 lần, "Mặt nạ" 50 lần.
- Mở **Số ngày chưa bán được SP** → SP "Serum X" lần bán gần nhất 20/03, đến 30/06 là 102 ngày chưa bán → nằm đầu danh sách cảnh báo.
- Bấm Xuất Excel combo khi lưới mới tải 1 trang → file chỉ có dòng của trang đó (vì build từ dữ liệu FE).

## Hậu điều kiện
- Truy vấn báo cáo **không thay đổi dữ liệu** (read-only), không bắn message/event.
- Log query chậm khi super/CSKH hoặc đơn vị bật "show query".
