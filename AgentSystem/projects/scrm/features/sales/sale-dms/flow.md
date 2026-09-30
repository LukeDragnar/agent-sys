# Luồng — Sale DMS

> ⚠️ **Chưa kiểm chứng bằng code.** Sale DMS không có controller/màn hình trong nguồn snapshot hiện tại; luồng dưới đây là **khung dự kiến theo tên menu** (Bán hàng + Kho + Tuyến + Lịch trình sale), **cần product xác nhận**. Không dùng làm đề test cho tới khi tính năng có code thật.

## Tiền điều kiện (dự kiến)
- Đơn vị đã bật/được cấp tính năng Sale DMS (cần product xác nhận có là tiện ích riêng không).
- Đã khai báo danh sách **điểm bán/đại lý** (khách hàng dạng điểm bán) để xếp vào tuyến.
- Có nhân viên sale thị trường để gán tuyến/lịch trình.

## Luồng dùng (nhân viên sale — dự kiến)
1. Nhân viên mở màn/app Sale DMS → xem **lịch trình hôm nay** (các điểm bán cần ghé).
2. Đến điểm bán → mở chi tiết điểm bán → **kiểm tồn** (nếu có) → **lên đơn** sản phẩm.
3. Ghi nhận **kết quả viếng thăm** (đã ghé / chưa ghé / lý do).
4. Kết thúc tuyến → dữ liệu đổ về báo cáo.

## Luồng cấu hình (quản lý — dự kiến)
1. Quản lý tạo **tuyến** = danh sách điểm bán theo thứ tự.
2. **Gán lịch trình**: tuyến + ngày + nhân viên sale.
3. Theo dõi nhân viên thực hiện theo lịch trình.

## Các trường hợp / case biên (dự kiến — cần xác nhận khi có code)
- **Không có lịch trình cho ngày hiện tại** → màn hình rỗng, không có điểm bán để ghé.
- **Điểm bán chưa khai báo trong tuyến** → không xuất hiện khi đi tuyến (phải thêm vào tuyến trước).
- **Lên đơn tại điểm bán nhưng tồn = 0** → ứng xử (chặn/cho đặt) theo cấu hình đơn hàng — cần xác nhận có dùng chung cấu hình bán hàng không.
- **Nhân viên không đi đúng lịch trình** → ghi nhận "chưa viếng thăm"; cách cảnh báo cần xác nhận.

## Ví dụ cụ thể có số (minh họa khung — KHÔNG phải đề test thật)
> Vì chưa có code, đây chỉ là ví dụ minh họa khái niệm, không phản ánh hành vi thật của hệ thống.
- Quản lý tạo tuyến "Quận 1" gồm 8 điểm bán, gán cho NV An ngày 20/06.
- An mở Sale DMS sáng 20/06 → thấy 8 điểm bán. Ghé 6 điểm, lên 4 đơn (tổng 12 triệu), 2 điểm "đóng cửa".
- Cuối ngày: lịch trình thể hiện 6/8 đã viếng thăm, doanh số tuyến 12 triệu.

## Hậu điều kiện (dự kiến)
- Đơn lên tại điểm bán sinh đơn hàng như luồng bán hàng (cần xác nhận).
- Kết quả viếng thăm + doanh số đổ về báo cáo sale/DMS.
