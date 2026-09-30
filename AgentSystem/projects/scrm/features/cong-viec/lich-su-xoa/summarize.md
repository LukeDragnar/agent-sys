---
alias: ["lịch sử xóa", "thùng rác", "khôi phục công việc", "khôi phục", "recycle bin", "restore", "undo xóa", "lấy lại công việc đã xóa"]
module: cong-viec
trang_thai: Core
goi: cần product xác nhận
quyen: "cần product xác nhận — xem danh sách lịch sử xóa chỉ DV-AD-TONG (admin tổng đơn vị); Khôi phục / Xóa vĩnh viễn chỉ yêu cầu đăng nhập (SBAuthorize) — lệch quyền so với màn list, xem dev.md"
phu_thuoc: ["Quản lý công việc (nguồn các bản ghi xóa mềm)", "Quy trình & lượt thực thi (lượt thực thi xóa mềm cũng vào đây)"]
link_flow: ./flow.md
---

# Lịch sử xóa (thùng rác công việc)

## Là gì
Đây là **thùng rác / màn khôi phục** cho module Công việc. Khi một công việc hoặc một lượt thực thi quy trình bị xóa, hệ thống **không xóa hẳn** mà ẩn đi (xóa mềm) và ghi lại một bản ghi vào Lịch sử xóa kèm "ảnh chụp" toàn bộ những gì đã bị xóa (công việc, công việc con, người thực hiện, trường mở rộng, file, lượt thực thi). Từ đây có thể **khôi phục lại nguyên cụm** hoặc **xóa vĩnh viễn** bản ghi lịch sử.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem danh sách bản ghi đã xóa (lọc loại / từ khóa / ngày / đã khôi phục) | ✅ | chỉ admin tổng đơn vị (DV-AD-TONG) |
| Khôi phục một cụm đã xóa | ✅ | bản ghi chưa từng khôi phục; (⚠ kỹ thuật chỉ yêu cầu đăng nhập — xem Cần chú ý) |
| Xóa vĩnh viễn bản ghi lịch sử | ⚠️ | mất đường khôi phục cụm đó; (⚠ kỹ thuật chỉ yêu cầu đăng nhập) |
| Thêm / Sửa bản ghi lịch sử xóa thủ công | ❌ | không có — bản ghi chỉ sinh tự động khi xóa công việc / lượt thực thi |

## Để làm gì / cho ai
- **Admin tổng đơn vị:** quản lý thùng rác — tìm lại công việc/lượt thực thi bị xóa nhầm và khôi phục, hoặc dọn sạch bản ghi không cần.
- **Người vận hành:** khi lỡ xóa nhầm công việc, báo admin vào Lịch sử xóa khôi phục lại.

## Cách dùng
1. Vào **Công việc → Lịch sử xóa** (cần quyền admin tổng đơn vị).
2. Lọc theo loại (Công việc / Lượt thực thi), từ khóa (tên gọi), khoảng ngày xóa, hoặc trạng thái "đã khôi phục".
3. Tìm đúng cụm cần lấy lại → bấm **Khôi phục** → cả cụm (công việc + con + dữ liệu liên quan) hiện lại trong Quản lý công việc.
4. Hoặc bấm **Xóa vĩnh viễn** để bỏ bản ghi lịch sử (sau đó không khôi phục được nữa).

## Cần chú ý
- **Khôi phục chỉ "bật lại" bản ghi cũ**, không tạo mới: hệ thống đặt lại trạng thái xóa của đúng các bản ghi đã chụp trong ảnh snapshot. Vì vậy nếu một phần dữ liệu đã bị xóa cứng ở nơi khác thì khôi phục có thể thiếu (im lặng, không báo).
- **Mỗi cụm chỉ khôi phục một lần**: bản ghi đã khôi phục rồi sẽ báo "đã khôi phục trước đó".
- **Xóa vĩnh viễn chỉ xóa bản ghi lịch sử**, KHÔNG dọn dữ liệu công việc đã xóa mềm → công việc đó vẫn nằm ẩn trong DB nhưng **mất đường khôi phục** (snapshot biến mất).
- **Lệch phân quyền (lưu ý kỹ thuật)**: xem danh sách cần quyền admin tổng, nhưng nút Khôi phục / Xóa vĩnh viễn về kỹ thuật chỉ yêu cầu đăng nhập — người biết id vẫn có thể gọi. Cần product/kỹ thuật xem lại (đã ghi trong dev.md).

## Ảnh hưởng tới
- **Quản lý công việc**: khôi phục làm công việc xuất hiện lại trong lưới và được đồng bộ lại lên tìm kiếm nhanh.
- **Quy trình & lượt thực thi**: lượt thực thi xóa mềm cũng khôi phục được từ đây.
