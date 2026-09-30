---
alias: ["telesales", "tele", "đội tele", "gọi điện chăm sóc", "gọi data", "nhân viên gọi điện", "tác nghiệp tele"]
module: sales
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: "cần product xác nhận — telesales dùng chung màn Danh sách contact (Contact). Nhiều endpoint contact yêu cầu role sale/MKT; một số nút (Nhận data, tạo lịch sử cuộc gọi) chỉ cần đăng nhập (mã quyền bị nới lỏng — xem dev.md)."
phu_thuoc: ["Quản lý Contact (data/lead để gọi)", "Chia Lead / số (cấp số cho tele gọi)", "Tổng đài (thực hiện cuộc gọi + ghi âm + lịch sử)", "Lịch hẹn (đặt lịch sau khi gọi)", "Đơn hàng (chốt đơn sau khi gọi)"]
link_flow: ./flow.md
---

# Telesales

> **Telesales không phải một màn hình riêng** — đây là **cách dùng** của đội nhân viên gọi điện trên nền **Danh sách Contact**. Nhân viên tele nhận data (số điện thoại khách/lead), gọi qua tổng đài, rồi cập nhật kết quả tác nghiệp, gắn nhãn, đặt lịch hoặc chốt đơn ngay trên contact.

## Là gì
**Telesales** là quy trình **đội gọi điện** chăm sóc/bán hàng qua điện thoại: mỗi nhân viên được **chia data (contact/lead)**, lần lượt **gọi** từng số (thường qua tổng đài tích hợp), sau mỗi cuộc gọi **ghi nhận kết quả** (đã liên hệ / hẹn gọi lại / từ chối / chốt đơn), **gắn nhãn khách**, và có thể **đặt lịch hẹn** hoặc **lên đơn**. Mọi thao tác diễn ra trên **màn Danh sách contact** kết hợp **lịch sử cuộc gọi** của tổng đài.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Nhận data để gọi (chia xoay vòng) | ✅ | có data được chia; nút "Nhận data" hiện khi contact còn ẩn |
| Gọi điện (click-to-call qua tổng đài) | ✅ | đơn vị có bật tổng đài; tùy phiên bản tổng đài |
| Ghi nhận kết quả tác nghiệp | ✅ | tùy quyền sale |
| Gắn nhãn khách (đơn / hàng loạt) | ✅ | tùy quyền |
| Chuyển tác nghiệp / tác nghiệp tiếp | ✅ | tùy quyền |
| Đặt lịch hẹn từ contact | ✅ | tùy quyền; có thể phải chờ leader duyệt |
| Chốt đơn / tạo đơn khách cũ | ✅ | tùy quyền |
| Xem lịch sử cuộc gọi / nghe ghi âm | ⚠️ | tùy phiên bản tổng đài & quyền (xem feature Tổng đài) |
| Ghi chú nội bộ trên contact | ✅ | tùy quyền |
| Xóa contact | ⚠️ | xóa lẻ tùy quyền; xóa hàng loạt chỉ admin tổng (DV-AD-TONG) |

## Để làm gì / cho ai
- **Nhân viên telesales:** nhận số → gọi → ghi kết quả → đặt lịch/chốt đơn, không phải nhớ thủ công.
- **Trưởng nhóm tele / sale leader:** chia data cho nhân viên, duyệt lịch hẹn, theo dõi tiến độ gọi.
- **Quản lý:** xem báo cáo chia số, KPI cuộc gọi, tỉ lệ chốt (qua Báo cáo Sales).

## Cách dùng
1. Trưởng nhóm/cấu hình **chia data** (lead/số) cho nhân viên tele (xem feature Chia Lead/số).
2. Nhân viên mở **Sale → Danh sách contact**, bấm **Nhận data** để mở thông tin số cần gọi.
3. **Gọi** (qua nút click-to-call nếu có tổng đài), nói chuyện với khách.
4. Sau cuộc gọi: **cập nhật kết quả tác nghiệp**, **gắn nhãn**, ghi chú; nếu khách quan tâm → **đặt lịch** hoặc **lên đơn**.
5. Lặp lại với số tiếp theo; cuối ngày dữ liệu vào báo cáo.

## Cần chú ý
- **Telesales dùng chung dữ liệu Contact** — không phải hệ thống tách riêng. Mọi thao tác (gọi, ghi kết quả, nhãn) đều ghi lên contact.
- **Phải có data được chia** thì nhân viên mới có số để gọi; nếu chưa chia, danh sách rỗng.
- **Cuộc gọi & ghi âm phụ thuộc tổng đài**: có 3 phiên bản tổng đài (Voice IP / Sim số / IP mới) — khả năng gọi/nghe ghi âm khác nhau theo phiên bản đơn vị đang dùng.
- **Phân quyền vài thao tác bị nới lỏng** ở tầng code (vd "Nhận data", tạo lịch sử cuộc gọi chỉ cần đăng nhập) — xem dev.md; không nên dựa vào đó để khẳng định quyền với khách.
- **Đặt lịch có thể cần leader duyệt** trước khi thành lịch hẹn thật (trạng thái Chờ duyệt).

## Ảnh hưởng tới
- **Tổng đài** (thực hiện cuộc gọi + lịch sử + ghi âm).
- **Lịch hẹn / Booking** (đặt lịch từ contact).
- **Đơn hàng** (chốt đơn / tạo đơn khách cũ).
- **Khách hàng** (gán sale, đồng bộ thông tin khi tạo đơn).
- **Báo cáo Sales** (chia số, KPI, tỉ lệ chốt lấy số liệu từ tác nghiệp tele).
