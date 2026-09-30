---
alias: ["lịch sử sản xuất", "lịch sử đơn đặt hàng", "lịch sử production order", "lịch sử work order", "lịch sử lệnh công việc", "tra cứu lệnh sản xuất", "tra cứu đơn sản xuất"]
module: san-xuat
trang_thai: "cần product xác nhận — thuộc module Sản Xuất còn sơ khai (level 1). Là các màn tra cứu/lịch sử của 3 đối tượng: Đơn đặt hàng, Yêu cầu sản xuất (PR), Lệnh công việc (WO)."
goi: "cần product xác nhận"
quyen: "cần product xác nhận — các endpoint tìm lịch sử (`TimTheoDieuKienLichSu`) hiện để quyền rỗng → chỉ cần đăng nhập. Một số nút Xóa trên màn lịch sử dùng chung quyền của đối tượng tương ứng."
phu_thuoc: ["Quản lý sản xuất (dữ liệu Đơn đặt hàng / Yêu cầu sản xuất / Lệnh công việc được tra cứu ở đây)"]
link_flow: ./flow.md
---

# Lịch sử sản xuất

## Là gì
Đây là nhóm màn hình **tra cứu lịch sử** của quá trình sản xuất — xem lại các **đơn đặt hàng**, **yêu cầu sản xuất (PR)** và **lệnh công việc (WO)** đã tạo, kèm trạng thái và thời gian. Dùng để theo dõi, đối chiếu, tìm lại một lệnh/đơn cụ thể.

Gồm 3 màn tra cứu:
- **Lịch sử đơn đặt hàng**: các đơn đặt hàng sản xuất đã nhập.
- **Lịch sử sản xuất (Production Order / PR)**: các yêu cầu sản xuất, đã tách hay chưa.
- **Lịch sử lệnh công việc (Work Order)**: các lệnh sản xuất, lọc theo trạng thái + trạng thái sản xuất, xem chi tiết nguyên vật liệu cần.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tìm / lọc theo mã, sản phẩm, thời gian | ✅ | mỗi màn có bộ lọc riêng |
| Lọc theo trạng thái (màn WO) | ✅ | lọc theo Trạng thái + Trạng thái sản xuất |
| Xem chi tiết nguyên vật liệu cần (màn WO) | ✅ | mở modal chi tiết |
| Xóa yêu cầu sản xuất **chưa tách** (màn PR) | ✅ | chỉ khi chưa tách (isTachDon=false) |
| Xóa lệnh công việc (màn WO) | ⚠️ | xóa được nhưng **KHÔNG hoàn phiếu kho** đã tạo (xem Cần chú ý) |
| Xuất Excel / In từ màn lịch sử | ❌ | không có nút xuất/in trên các màn lịch sử (cần product xác nhận nếu cần) |

## Để làm gì / cho ai
- **Quản lý sản xuất:** theo dõi tiến độ, tra lại lệnh/đơn cũ, kiểm tra trạng thái.
- **Kế toán / kho:** đối chiếu lệnh sản xuất với phiếu nhập/xuất kho.
- **CSKH / hỗ trợ:** tra cứu nhanh một mã đơn/lệnh khi khách hỏi.

## Cách dùng
1. Vào **Sản xuất → Lịch sử** (3 mục: Lịch sử đơn đặt hàng `/sanxuat/lich-su-donhang`, Lịch sử sản xuất `/sanxuat/lich-su-sanxuat`, Lịch sử công việc `/sanxuat/lich-su-congviec`).
2. Nhập từ khóa (mã PR, mã lệnh, mã/tên sản phẩm) hoặc chọn khoảng thời gian, rồi bấm **Tìm kiếm**. Bấm **Đặt lại** để xóa bộ lọc.
3. Màn **Lệnh công việc** còn lọc theo **Trạng thái** và **Trạng thái sản xuất**; bấm **Xem chi tiết** để mở danh sách nguyên vật liệu cần.
4. Kết quả hiển thị dạng bảng có phân trang; một số dòng có nút **Xóa** (tùy điều kiện).

## Cần chú ý
- **Xóa lệnh công việc không hoàn phiếu kho.** Như ở Quản lý sản xuất: xóa WO đã tạo phiếu nhập/xuất sẽ **không** tự hủy các phiếu đó → tồn kho có thể lệch.
- **Chỉ xóa được yêu cầu sản xuất chưa tách.** Trên màn lịch sử PR, nút Xóa chỉ hiện khi yêu cầu **chưa tách** thành lệnh.
- **Các màn lịch sử chủ yếu để tra cứu**, không phải nơi thao tác chính của quy trình — thao tác chạy sản xuất nằm ở Planner (xem Quản lý sản xuất).
- **Endpoint lịch sử hiện để quyền rỗng** (chỉ cần đăng nhập) — cần product xác nhận có cần siết quyền không.

## Ảnh hưởng tới
- Chỉ **đọc** dữ liệu của Đơn đặt hàng / PR / WO; trừ thao tác Xóa (đụng tới dữ liệu sản xuất và gián tiếp tồn kho như nêu trên).
