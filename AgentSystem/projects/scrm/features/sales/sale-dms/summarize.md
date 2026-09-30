---
alias: ["Sale DMS", "DMS", "bán hàng DMS", "đi tuyến sale", "lịch trình sale", "phân phối", "viếng thăm điểm bán"]
module: sales
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: cần product xác nhận
phu_thuoc: ["Sản phẩm & kho (bán hàng + kiểm tồn theo điểm bán)", "Đơn hàng (lên đơn từ điểm bán)", "Khách hàng / điểm bán (danh sách điểm bán theo tuyến)", "Nhân viên sale (gán tuyến / lịch trình)"]
link_flow: ./flow.md
---

# Sale DMS

> ⚠️ **Lưu ý quan trọng (cần product xác nhận):** Trong bản mã nguồn hiện tại (snapshot 2026-06-20) **không tìm thấy controller/màn hình/quyền nào** mang dấu vết `SaleDMS_*`. Tính năng này xuất hiện trong **bảng menu (Page) trên staging** nhưng **chưa có code triển khai** trong nguồn đang quét — có thể là **module/ứng dụng riêng (app DMS đi tuyến)**, đang phát triển, hoặc tích hợp ngoài. Toàn bộ mô tả dưới đây là **khung dự kiến theo tên menu**, **chưa kiểm chứng được bằng code**. Product cần xác nhận phạm vi thật trước khi công bố cho khách.

## Là gì
**Sale DMS** (Distribution Management System — hệ quản lý phân phối/đi tuyến) là công cụ dành cho **nhân viên sale đi thị trường**: theo **tuyến** và **lịch trình** ghé thăm các điểm bán/đại lý, tại mỗi điểm có thể **bán hàng (lên đơn)**, **kiểm tra tồn kho**, ghi nhận kết quả viếng thăm. Mục tiêu là số hóa hoạt động bán hàng ngoài thị trường (field sales) thay vì làm thủ công trên giấy.

Theo khung menu, Sale DMS gộp 4 mảng việc: **Bán hàng** · **Kho (tồn tại điểm bán)** · **Tuyến** · **Lịch trình sale**.

## Hành động làm được
> Các giá trị dưới đây **chưa kiểm chứng bằng code** — đánh dấu ⚠️ và "cần product xác nhận".

| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tạo tuyến đi (lộ trình điểm bán) | ⚠️ | cần product xác nhận — chưa thấy code |
| Lập lịch trình sale (ngày/tuyến/nhân viên) | ⚠️ | cần product xác nhận |
| Viếng thăm điểm bán & ghi nhận kết quả | ⚠️ | cần product xác nhận |
| Bán hàng / lên đơn tại điểm bán | ⚠️ | cần product xác nhận — có thể dùng chung luồng đơn hàng |
| Kiểm tra tồn kho tại điểm bán | ⚠️ | cần product xác nhận |
| Báo cáo đi tuyến / doanh số DMS | ⚠️ | cần product xác nhận |

## Để làm gì / cho ai
- **Nhân viên sale thị trường (field sales):** biết hôm nay đi tuyến nào, ghé những điểm bán nào, lên đơn ngay tại chỗ.
- **Quản lý sale / giám sát bán hàng:** giao tuyến, theo dõi nhân viên có đi đúng lịch trình không, doanh số theo tuyến/điểm bán.
- **Chủ shop / nhà phân phối:** quản lý mạng lưới điểm bán và hoạt động phân phối.

## Cách dùng
> Khung dự kiến — **cần product xác nhận** vì chưa có màn hình trong nguồn.
1. Quản lý **lập tuyến** (danh sách điểm bán) và **gán lịch trình** cho nhân viên sale theo ngày.
2. Nhân viên sale mở app/màn DMS → xem **lịch trình hôm nay** → đi đến từng điểm bán.
3. Tại điểm bán: **kiểm tồn**, **lên đơn**, ghi nhận kết quả viếng thăm.
4. Quản lý xem **báo cáo** đi tuyến và doanh số.

## Cần chú ý
- **Đây là feature CHƯA xác nhận có trong sản phẩm đang chạy.** Không khẳng định với khách rằng tính năng này đã có cho tới khi product xác nhận.
- Rất có thể Sale DMS là **một ứng dụng/sản phẩm riêng** (mobile field-sales) tách khỏi luồng web SCRM lõi — khi đó các "hành động" ở trên thuộc app đó, không phải các màn web SCRM.
- Nếu thực tế Sale DMS chỉ là **gom các màn có sẵn** (đơn hàng + kho + khách hàng) theo góc nhìn "đi tuyến", thì nên trỏ về các feature đã có (Quản lý đơn hàng, Quản lý tồn kho, Quản lý Contact) thay vì coi là feature mới.

## Ảnh hưởng tới
- **Đơn hàng:** nếu lên đơn tại điểm bán thì sinh đơn như luồng bán hàng thường.
- **Tồn kho:** kiểm/bán tại điểm bán đụng số liệu tồn.
- **Nhân viên sale & báo cáo:** kết quả đi tuyến đổ về báo cáo sale.
