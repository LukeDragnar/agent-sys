---
alias: ["lịch hẹn", "đặt lịch", "booking", "lịch hẹn gặp khách", "đặt lịch trước", "lịch telesale", "duyệt lịch", "chờ duyệt lịch"]
module: sales
trang_thai: Core
goi: cần product xác nhận
quyen: "cần product xác nhận — Xem/tạo/sửa/đổi trạng thái/xóa lịch có mã quyền riêng (`DatLich_*`); QLDV phải được phép xem POS. Bản telesale dùng key `DatLich_ViewSale_*`. Tạo lịch end-user web là ẩn danh (AllowAnonymous). Duyệt/hủy duyệt lịch từ contact: sale leader (key `GetListContactDatLichChoDuyet_*`, `DuyetContactDeTaoLich_*`). Một số key (import, gắn nhãn) bị comment → chỉ cần đăng nhập."
phu_thuoc: ["Contact / Lead (đặt lịch từ contact, đồng bộ trạng thái lịch ↔ contact, leader duyệt lịch)", "Khách hàng (tìm/tạo KH khi đặt lịch)", "Sản phẩm & combo (dịch vụ/combo gắn vào lịch)", "SMS / Zalo (gửi nhắc lịch, link check-in/đánh giá)", "Đơn hàng (thanh toán đơn cập nhật trạng thái lịch)", "Cấu hình đơn vị (che số, cho phép QLDV xem POS, yêu cầu duyệt lịch)"]
link_flow: ./flow.md
---

# Lịch hẹn gặp khách

## Là gì
Đây là nơi **đặt và quản lý lịch hẹn (booking)** với khách: hẹn khách tới cửa hàng / spa vào thời gian cụ thể, gắn dịch vụ/combo, gán nhân viên phục vụ, gửi tin nhắn nhắc lịch, và theo dõi trạng thái lịch (Chưa xác nhận → Đã xác nhận → Chờ phục vụ → Đang phục vụ → Hoàn thành / Hủy / Không đến).

Lịch có thể đặt từ nhiều nguồn: **nhân viên đặt** trong hệ thống, **khách tự đặt** qua trang web công khai, **đặt hàng loạt** theo nhiều khung giờ, hoặc **đặt từ contact** (telesale) — trong đó lịch đặt từ contact có thể cần **leader duyệt** trước khi tạo thật.

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Xem lịch (theo thời gian / vị trí / nhân viên / ngày) | ✅ | tùy quyền; nhân viên thường chỉ thấy lịch của mình |
| Tạo lịch (nhân viên đơn vị) | ✅ | tùy quyền tạo lịch; QLDV phải được phép xem POS |
| Tạo lịch (khách tự đặt qua web) | ✅ | trang public, không cần đăng nhập |
| Tạo lịch hàng loạt (nhiều khung giờ) | ✅ | tùy quyền đặt lịch hàng loạt |
| Sửa thông tin lịch | ✅ | tùy quyền; sale chỉ sửa lịch của mình |
| Đổi trạng thái lịch | ✅ | tùy quyền; sale theo phân cấp |
| Xóa lịch hẹn | ✅ | tùy quyền; lịch online có thể bị chặn xóa theo cấu hình |
| Gắn nhãn khách hàng cho lịch | ✅ | chỉ cần đăng nhập (key bị comment) |
| Import lịch hẹn từ Excel | ✅ | chỉ cần đăng nhập (key import bị comment) |
| Đặt lịch từ contact (telesale) | ✅ | tùy quyền (xem feature Quản lý Contact) |
| **Duyệt lịch contact (chờ duyệt)** | ✅ | **sale leader**; chỉ với lịch đang Chờ duyệt |
| **Hủy duyệt lịch contact** | ✅ | sale leader; trả lịch về Chờ duyệt |
| Gửi SMS/Zalo nhắc lịch | ✅ | theo cấu hình gửi SMS + job nhắc trước ngày hẹn |

## Để làm gì / cho ai
- **Lễ tân / nhân viên cửa hàng:** đặt lịch cho khách, xem lịch trong ngày, đổi trạng thái khi khách đến / phục vụ / hoàn thành.
- **Telesale:** đặt lịch từ contact đang chăm.
- **Sale leader:** duyệt / hủy duyệt lịch mà sale đặt từ contact (nếu đơn vị bật yêu cầu duyệt).
- **Khách hàng:** tự đặt lịch qua trang web công khai.
- **Quản lý:** theo dõi lịch theo nhân viên / vị trí / thời gian, gửi nhắc lịch tự động.

## Cách dùng
**Đặt lịch (nhân viên):**
1. Vào màn **Đặt lịch hẹn**, bấm tạo lịch mới.
2. Chọn khách (hoặc nhập mới), dịch vụ/combo, thời gian, nhân viên phục vụ, chỗ ngồi.
3. Lưu → hệ thống tạo lịch, gửi SMS/Zalo nhắc lịch theo cấu hình.

**Đổi trạng thái lịch:**
1. Mở lịch → đổi trạng thái (Đã xác nhận → Chờ phục vụ → Đang phục vụ → Hoàn thành), hoặc Hủy / Không đến.
2. Hệ thống đồng bộ trạng thái sang contact (nếu lịch gắn contact) và gửi thông báo.

**Duyệt lịch từ contact (leader):**
1. Vào tab **Chờ duyệt lịch** (danh sách contact đặt lịch chờ duyệt).
2. Bấm **Duyệt** → lịch được tạo thật; hoặc **Hủy duyệt** → trả về trạng thái chờ.

## Cần chú ý
- **Lịch đặt hàng loạt không "tất cả hoặc không":** nếu đặt nhiều khung giờ mà một khung bị lỗi, các lịch trước đó **đã được tạo** (không hoàn tác toàn bộ). Kiểm tra lại danh sách lịch sau khi đặt hàng loạt.
- **QLDV phải được phép xem POS:** hầu hết thao tác lịch yêu cầu đơn vị cho phép quản lý đơn vị dùng tính năng POS; nếu không sẽ báo "Đơn vị của bạn không cho phép sử dụng tính năng này!".
- **Thiếu cấu hình → không tạo được lịch:** nếu đơn vị thiếu cấu hình đặt lịch, hệ thống báo "Không tìm thấy cấu hình!" và ngắt việc tạo lịch (đây là điểm cứng đã được ghi nhận để xử lý lại — xem dev.md).
- **Khách đính kèm phải đủ tên + SĐT:** khi đặt lịch nhiều khách, khách thứ 2 trở đi phải có tên và số điện thoại, không được trùng nhau.
- **Lịch online có thể bị chặn xóa** nếu đơn vị bật cấu hình không cho admin xóa lịch online (trừ admin/CSKH/super).
- Một số quyền (import, gắn nhãn) **để mở** (chỉ cần đăng nhập) do key bị comment — *cần product xác nhận*.

## Ảnh hưởng tới
- **Contact:** đặt lịch từ contact / đổi trạng thái lịch → đồng bộ trạng thái + nhãn sang contact.
- **Khách hàng (Customer):** tìm/tạo khách khi đặt lịch; ghi log tương tác khách.
- **SMS / Zalo:** gửi nhắc lịch, link check-in / đánh giá.
- **Đơn hàng:** thanh toán đơn / đổi thời gian đơn → cập nhật trạng thái và thời gian phục vụ của lịch.
- **Thông báo:** tạo / đổi trạng thái lịch → gửi thông báo cho thu ngân / quản lý lịch hẹn / admin.
