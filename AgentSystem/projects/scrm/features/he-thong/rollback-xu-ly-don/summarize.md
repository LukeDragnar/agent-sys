---
alias: ["rollback đơn", "hoàn tác đơn", "xử lý đơn lỗi", "đơn hàng lỗi", "OrderProcess", "rollback logistic", "khôi phục trạng thái đơn"]
module: he-thong
trang_thai: Core (nội bộ super-admin — không cho khách hàng cuối)
goi: cần product xác nhận (cơ chế nền của SaaS, không bán theo gói)
quyen: "Màn tra cứu 'Danh sách đơn hàng lỗi' chỉ admin hệ thống (quantri) — quyền nội bộ `OrderProcess_TimTheoDieuKien`. Các API tạo/cập nhật rollback là service-to-service (PrivateSBAuthorize), không có UI."
phu_thuoc: ["Đơn hàng (Order/OrderLogistic) — nguồn phát sinh đơn cần hoàn tác", "Kho (WarehouseProduct) — hoàn xuất kho", "Khách hàng (Customer) — hoàn điểm/hạng/công nợ", "Booking — hủy lịch hẹn", "Khuyến mãi (Promotion) — gỡ voucher", "Thu chi (ThuChi/Branch) — hoàn phiếu thu/chi", "ConfigSystem — tham số điều khiển job rollback"]
link_flow: ./flow.md
---

# RollBack & xử lý đơn

## Là gì
Đây là **cơ chế nền của hệ thống** dùng để **hoàn tác (rollback) một đơn hàng** khi đơn bị hủy, hủy thanh toán hoặc trả khách. Khi một đơn cần hoàn tác, hệ thống không chỉ đổi trạng thái đơn mà còn phải **trả ngược lại tất cả tác động** mà đơn đó đã gây ra ở nhiều phân hệ: hoàn lại số lượng tồn kho, hoàn điểm/công nợ cho khách, hủy lịch hẹn, gỡ khuyến mãi đã áp, hoàn lại phiếu thu/chi.

Phần này phục vụ **admin hệ thống (super-admin nội bộ SaaS)**, không hiển thị cho khách hàng cuối. Trên giao diện quản trị chỉ có **1 màn hình duy nhất**: **"Danh sách đơn hàng lỗi"** — nơi admin tra cứu các đơn đang trong quá trình rollback và xem từng phân hệ đã hoàn tác thành công hay chưa (Kho / Khách / Khuyến mãi / Booking).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem danh sách đơn hàng lỗi (đang rollback) | ✅ | admin hệ thống (nội bộ) |
| Lọc theo mã đơn / kiểu lỗi (hủy đơn, thanh toán đơn) / đơn vị / chi nhánh | ✅ | admin hệ thống |
| Xem trạng thái hoàn tác từng phân hệ (Kho/Khách/KM/Booking) | ✅ | hiển thị cờ ✔ theo từng cột |
| **Tạo rollback thủ công từ UI** | ❌ | Không có nút tạo/sửa trên màn quản trị — rollback do hệ thống/đơn tự sinh, đây chỉ là màn **theo dõi (read-only)** |
| **Bấm "rollback lại"/retry từ UI** | ❌ | Không có nút retry trên màn — retry do **job nền** tự chạy theo `SoLanDaGui` |
| Sàn TMĐT báo cần rollback giao vận | ✅ | qua API public `TMDT_CreateRollBackLogistic` (không phải UI) |

## Để làm gì / cho ai
- **Admin vận hành SaaS / kỹ thuật:** theo dõi các đơn đang rollback, phát hiện đơn nào bị kẹt (một phân hệ con hoàn tác lỗi) để can thiệp.
- **Hệ thống (tự động):** đảm bảo khi đơn bị hủy/hoàn thì dữ liệu tồn kho, công nợ, điểm thưởng, khuyến mãi, thu chi luôn được trả về đúng — không để "đơn hủy rồi mà kho vẫn trừ".

## Cách dùng
1. Admin vào **Quản trị → Danh sách đơn hàng lỗi**.
2. Lọc theo **mã đơn hàng**, **kiểu lỗi** (Hủy đơn / Thanh toán đơn), **đơn vị**, **chi nhánh**.
3. Bấm **Tìm kiếm** → bảng liệt kê các bản ghi rollback.
4. Nhìn các cột **BookingMajorSuccess / CustomerSuccess / PromotionSuccess / WarehouseProductSuccess**: cột nào có dấu ✔ là phân hệ đó đã hoàn tác xong; cột trống là **chưa xong / đang chờ job xử lý hoặc bị lỗi**.
5. Nếu một đơn kẹt lâu (nhiều cột trống) → báo kỹ thuật kiểm tra job rollback và log của phân hệ con.

## Cần chú ý
- Đây là **màn theo dõi**, không phải màn thao tác — không tạo/sửa/xóa/retry được từ giao diện. Mọi rollback do hệ thống tự sinh khi đơn bị hủy/hoàn.
- **Rollback có thể từng phần:** mỗi phân hệ (kho, khách, KM, booking, thu chi) hoàn tác độc lập. Một phân hệ lỗi **không chặn** các phân hệ khác → có thể thấy đơn đã hoàn kho nhưng chưa hoàn công nợ. Đây là thiết kế, nhưng cần giám sát để không sót.
- Hệ thống tự **retry** đơn chưa hoàn tác xong (đếm số lần qua `SoLanDaGui`) → tránh lặp vô hạn; nếu một phân hệ con liên tục lỗi thì cần điều tra API phân hệ đó.
- Có **API public** cho sàn TMĐT bắn yêu cầu rollback giao vận — đây là điểm tích hợp, không phải thao tác của admin.

## Ảnh hưởng tới
- **Tồn kho, công nợ khách, điểm thưởng, voucher đã áp, phiếu thu/chi, lịch hẹn** — tất cả được trả ngược khi rollback chạy. Đụng tới logic rollback có thể gây lệch số liệu các phân hệ này.
- **Đơn hàng (Order/OrderLogistic):** trạng thái đơn sau hủy/hoàn phụ thuộc rollback chạy đủ.
