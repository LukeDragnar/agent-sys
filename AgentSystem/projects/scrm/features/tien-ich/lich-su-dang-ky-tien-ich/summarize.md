---
alias: ["lịch sử tiện ích", "lịch sử mua tiện ích", "lịch sử đăng ký tiện ích", "lịch sử giao dịch tiện ích", "tiện ích đã đăng ký"]
module: tien-ich
trang_thai: Core
goi: cần product xác nhận
quyen: "Đăng nhập là xem được (endpoint LichSuGiaoDich chỉ yêu cầu login, lọc theo đơn vị của người dùng) — cần product xác nhận có giới hạn role thêm không"
phu_thuoc: ["Quản lý tiện ích (mỗi lần mua/đăng ký sinh 1 giao dịch ở đây)", "Hợp đồng tiện ích (nguồn dữ liệu lịch sử giao dịch)"]
link_flow: ./flow.md
---

# Lịch sử đăng ký tiện ích

## Là gì
Đây là **nhật ký các lần mua/đăng ký tiện ích** của đơn vị: mỗi lần bấm mua tiện ích sẽ tạo một **giao dịch** hiện ở đây, kèm **mã giao dịch, ngày giờ, giá trị, danh sách tiện ích trong giao dịch và trạng thái** (chờ duyệt / đã duyệt / hủy…). Dùng để theo dõi đơn vị đã đăng ký những tiện ích nào, khi nào, hết bao nhiêu tiền và yêu cầu nào còn đang chờ duyệt.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem danh sách giao dịch tiện ích | ✅ | đăng nhập; chỉ thấy giao dịch của đơn vị mình |
| Tìm theo **mã giao dịch** | ✅ | gõ vào ô tìm kiếm |
| Lọc theo **khoảng ngày** (từ ngày / đến ngày) | ✅ | chọn ngày rồi tìm |
| Xem **trạng thái** từng giao dịch | ✅ | hiển thị nhãn màu theo trạng thái |
| Ẩn/hiện cột, sắp xếp cột | ✅ | qua menu cấu hình bảng |
| Thêm / sửa / xóa giao dịch | ❌ | màn này **chỉ tra cứu** (read-only); việc tạo giao dịch nằm ở màn Quản lý tiện ích |
| Xuất Excel | ⚠️ | có nút Export trong menu cấu hình nhưng **mặc định bị ẩn** — cần product xác nhận có bật cho màn này không |

## Để làm gì / cho ai
- **Chủ shop / Admin đơn vị:** đối chiếu các lần mua tiện ích, biết yêu cầu nào đã được duyệt, yêu cầu nào còn chờ; tra mã giao dịch khi cần đối soát/khiếu nại.
- **Kế toán:** xem giá trị từng giao dịch tiện ích đã phát sinh.

## Cách dùng
1. Vào **Cài đặt hệ thống → Lịch sử mua tiện ích** (`/caidat/lich-su-mua-tien-ich`).
2. Bảng liệt kê các giao dịch với các cột: **Mã giao dịch, Ngày giao dịch, Giá trị, Danh sách tiện ích, Trạng thái**.
3. Gõ **mã giao dịch** vào ô tìm hoặc chọn **Từ ngày / Đến ngày**, rồi bấm **Tìm kiếm**.
4. Đọc cột **Trạng thái** (nhãn màu) để biết giao dịch đang chờ duyệt hay đã xử lý.

## Cần chú ý
- Đây là màn **chỉ để xem** — không thêm/sửa/xóa giao dịch tại đây. Muốn mua tiện ích thì vào màn **Quản lý / Đăng ký tiện ích**.
- Mỗi người chỉ thấy **giao dịch của đơn vị mình** (lọc theo người đang đăng nhập).
- Một lần bấm mua giỏ **trộn cả tiện ích miễn phí + trả phí** có thể tạo **2 giao dịch riêng** (một auto-duyệt cho miễn phí, một chờ duyệt cho trả phí) → đừng nhầm là trùng.
- Các nút "Thêm mới"/"Sửa"/"Xóa" trong code màn này đang trỏ nhầm sang khuyến mãi (xem dev.md) — **không dùng**.

## Ảnh hưởng tới
- Không thay đổi dữ liệu hệ thống (chỉ đọc).
- Phản ánh kết quả của thao tác mua ở màn **Quản lý tiện ích** và việc **duyệt hợp đồng tiện ích** của quản trị hệ thống.
