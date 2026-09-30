---
alias: ["lịch sử giao hàng", "log trạng thái đơn", "theo dõi nghiệp vụ đơn", "lịch sử nghiệp vụ logistics", "nhật ký xử lý đơn giao vận"]
trang_thai: cần product xác nhận
goi: cần product xác nhận
quyen: cần product xác nhận
phu_thuoc: [DonHangLogistic (đơn giao vận), Trạng thái giao hàng (GiaoHangTrangThai), Customer (chi tiêu/điểm/nợ)]
link_flow: ./flow.md
module: kho
---

# Lịch sử nghiệp vụ logistics

## Là gì
Màn hình **theo dõi nhật ký xử lý nghiệp vụ tự động của đơn giao vận**. Mỗi khi một **đơn logistics đổi trạng thái giao hàng** (đang giao → giao thành công, hoàn, đối soát...), hệ thống tự sinh ra các "nghiệp vụ" cần chạy theo đơn đó — ví dụ cộng/trừ chi tiêu, điểm, công nợ của khách. Màn hình này cho bạn **tra theo từng đơn** xem các nghiệp vụ đó đang **chờ chạy (Pending)**, đã **chạy thành công**, hay **thất bại**, và xem được chi tiết từng lần xử lý.

Nói ngắn gọn: đây là **bảng tổng kết "đơn này đã được hệ thống xử lý hậu kỳ tới đâu"** — không phải nơi tạo đơn, mà là nơi soi lại lịch sử xử lý.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Tra đơn theo mã / khoảng ngày / trạng thái giao hàng / trạng thái nghiệp vụ | ✅ | ngày lọc theo **ngày tạo đơn**, không phải ngày chạy nghiệp vụ |
| Xem 3 con số đếm Pending / Thành công / Thất bại + lần xử lý gần nhất | ✅ | mỗi dòng là một đơn |
| Xem chi tiết nghiệp vụ đang chờ + lịch sử đã xử lý của đơn | ✅ | mỗi loại chỉ lấy tối đa 200 bản ghi gần nhất/đơn |
| Sửa đơn / sửa trạng thái giao hàng / chạy lại nghiệp vụ | ❌ | màn hình read-only với người dùng thường |

## Để làm gì / cho ai
- **Cho CSKH / vận hành kho:** tra nhanh một đơn xem các nghiệp vụ đi kèm đã chạy đủ chưa, có cái nào lỗi/treo không.
- **Cho người xử lý sự cố:** khi khách than "đơn đã giao mà chưa thấy cộng điểm / chưa trừ nợ", vào đây để xem nghiệp vụ đó đang Pending, thất bại hay bị bỏ qua, kèm lý do lỗi.
- **Actor chính:** nhân viên CSKH, vận hành (nhóm quyền "HT-CSKH").

## Cách dùng
1. Mở màn hình **Lịch sử nghiệp vụ logistics** (trong module Kho).
2. **Tìm đơn:** gõ **mã đơn**, chọn **khoảng ngày** (theo ngày tạo đơn), chọn **trạng thái giao hàng**, và/hoặc **lọc trạng thái nghiệp vụ** (Đang chờ / Thành công / Thất bại). Bấm **Tìm kiếm**.
3. Bảng trả về **mỗi dòng là một đơn**, kèm 3 con số đếm: **Pending**, **Thành công**, **Thất bại**, và **thời điểm xử lý gần nhất**.
4. Bấm **Xem** ở một dòng để mở chi tiết: liệt kê từng nghiệp vụ **đang chờ** và toàn bộ **lịch sử đã xử lý** của đơn (nghiệp vụ gì, hướng tăng/giảm, kết quả, số lần thử, thời điểm, ghi chú lỗi).

## Cần chú ý (cạm bẫy)
- Đây là **màn hình tra cứu (read-only)** với người dùng thường — không phải nơi sửa đơn hay sửa trạng thái giao hàng.
- Con số **Pending > 0 không có nghĩa là lỗi** — nghiệp vụ thường được hẹn xử lý **trễ một khoảng** (để chờ trạng thái đơn ổn định), nên đơn vừa đổi trạng thái sẽ thấy Pending là bình thường.
- **Thất bại** mới là cái cần để ý: mở chi tiết đọc **ghi chú lỗi** để biết nguyên nhân.
- Bộ lọc **ngày lọc theo ngày tạo đơn**, không phải ngày chạy nghiệp vụ — tra đơn cũ nhớ nới khoảng ngày.
- Tìm kiếm có **giới hạn**: phần chi tiết Pending/History mỗi loại chỉ lấy tối đa **200 bản ghi gần nhất** mỗi đơn.

## Ảnh hưởng tới
- **Chi tiêu / điểm / hạng khách hàng (Customer):** các nghiệp vụ ở đây chính là cái cộng/trừ những giá trị đó khi đơn giao thành công hoặc hoàn.
- **Công nợ khách:** nghiệp vụ trừ/ghi nợ chạy theo trạng thái đơn cũng phản ánh ở đây.
- **Không tự đổi trạng thái đơn:** màn hình chỉ phản ánh kết quả; nguồn sự thật là **trạng thái giao hàng của đơn logistics**.
