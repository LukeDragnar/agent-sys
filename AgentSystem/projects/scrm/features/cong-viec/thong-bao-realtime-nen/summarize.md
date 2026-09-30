---
alias: ["chuông thông báo", "thông báo realtime", "thông báo đẩy", "push notification", "báo có đơn mới", "báo có chat mới", "thông báo việc được giao", "nhắc deadline"]
module: cong-viec
trang_thai: Core (chạy nền — không có menu riêng)
goi: cần product xác nhận
quyen: "Không gate theo quyền — mọi user đã đăng nhập đều nhận thông báo cá nhân của mình. Việc 'sự kiện nào gửi cho ai' do cấu hình ở màn Cấu hình thông báo (Hệ thống) quyết định (HT-AD / DV-AD-TONG / HT-CSKH)."
phu_thuoc: ["Cấu hình thông báo (NotificationConfig) — quy tắc sự kiện nào → gửi mẫu nào cho ai", "Mẫu thông báo (NotiTemplate)", "Đăng nhập (lấy token thiết bị + đăng ký topic theo user/đơn vị/chi nhánh/role)", "Firebase Cloud Messaging (FCM) — kênh đẩy web/mobile", "Các module nghiệp vụ sinh sự kiện: Đơn hàng, Chat, Công việc, Lịch hẹn, Giao hàng…"]
link_flow: ./flow.md
---

# Thông báo & realtime nền (chuông thông báo)

## Là gì
Đây là **bộ máy chạy ngầm** đứng sau cái **chuông thông báo** ở góc màn hình. Khi có việc liên quan đến bạn xảy ra trong hệ thống — có đơn mới, có tin nhắn chat mới, được giao một công việc, sắp tới deadline, đổi trạng thái đơn/giao hàng, tới giờ lịch hẹn… — hệ thống tự **tạo một thông báo** và **đẩy ngay tới bạn** (hiện trên chuông + bắn thông báo đẩy về trình duyệt/điện thoại), mà bạn không cần bấm load lại trang.

Phần này **không có màn hình riêng để mở**. Người dùng chỉ thấy nó qua: (1) **chuông thông báo** (badge số chưa đọc + danh sách khi bấm vào), và (2) **thông báo đẩy** (popup của trình duyệt/app). Còn "sự kiện nào thì gửi thông báo cho ai" thì được khai báo ở màn **Hệ thống → Cấu hình thông báo** (xem feature riêng).

## Hành động làm được
| Hành động | Được? | Điều kiện / quyền |
|---|---|---|
| Nhận thông báo realtime (chuông + đẩy) | ✅ | tự động sau khi đăng nhập; cần cho phép thông báo trên trình duyệt/app |
| Xem danh sách thông báo của tôi | ✅ | bấm vào chuông; có phân trang, lọc theo ngày |
| Đếm số thông báo chưa đọc (badge) | ✅ | tự động hiện trên chuông |
| Đánh dấu đã đọc 1 thông báo | ✅ | bấm vào dòng thông báo |
| Đánh dấu đã đọc tất cả | ✅ | nút "Đánh dấu đã đọc tất cả" |
| Xóa 1 thông báo | ✅ | trong danh sách thông báo |
| **Tự soạn / gửi thông báo thủ công cho user khác** | ❌ | không có nút cho người dùng cuối — thông báo do hệ thống tự sinh từ sự kiện; muốn đổi "gửi gì cho ai" thì sửa ở Cấu hình thông báo |
| **Chọn nhận / không nhận từng loại thông báo** | ⚠️ | cần product xác nhận — việc bật/tắt theo sự kiện nằm ở Cấu hình thông báo (cấp đơn vị), không phải mỗi cá nhân tự tắt |
| Tắt nhận đẩy trên 1 thiết bị | ✅ | khi đăng xuất / tắt quyền thông báo của trình duyệt thì thiết bị đó ngừng nhận |

## Để làm gì / cho ai
- **Nhân viên Sale / CSKH / kho:** biết ngay khi có đơn mới, chat mới, việc được giao mà không phải ngồi F5 liên tục.
- **Quản lý / chủ shop:** nắm tình hình theo thời gian thực (đơn về, lịch hẹn tới giờ, cảnh báo nghiệp vụ).
- **Người được giao việc:** nhận thông báo "bạn vừa được giao công việc X" và lời nhắc khi sắp tới hạn.
- Về bản chất đây là **hạ tầng dùng chung** cho toàn hệ thống — gần như mọi module đều "mượn" nó để báo tin cho người dùng.

## Cách dùng
1. **Đăng nhập** vào web/app. Lần đầu trình duyệt/điện thoại sẽ hỏi **"cho phép gửi thông báo?"** — chọn **Cho phép** thì mới nhận được thông báo đẩy. Khi đăng nhập, hệ thống tự đăng ký thiết bị của bạn để biết đường gửi.
2. Khi có sự kiện liên quan, **chuông** sẽ tăng số và bạn nhận **popup thông báo đẩy** (nếu app/trình duyệt đang mở hoặc cho phép chạy nền).
3. Bấm **chuông** để xem danh sách thông báo gần đây (cuộn để xem thêm, lọc theo ngày nếu cần).
4. Bấm vào **một dòng** để mở/đọc — dòng đó được đánh dấu **đã đọc**, badge giảm đi. Có thể bấm **"Đánh dấu đã đọc tất cả"**.
5. Muốn **đổi loại thông báo nào gửi cho ai** thì vào **Hệ thống → Cấu hình thông báo** (cần quyền quản trị) — đây là nơi cấu hình, không phải ở chuông.

## Cần chú ý
- **Phải cho phép thông báo** trên trình duyệt/điện thoại thì mới nhận được đẩy. Nếu trước đó bấm "Chặn" thì sẽ không thấy popup, dù chuông vẫn cập nhật khi mở web.
- **Realtime gần như tức thì nhưng không tuyệt đối**: thông báo được đưa vào hàng đợi rồi một tiến trình nền gửi đi theo từng đợt — bình thường chỉ vài giây, lúc tải nặng có thể trễ hơn một chút.
- **Đổi vai trò / chi nhánh / đơn vị**: bạn nhận thông báo theo nhóm mình thuộc về (đơn vị, chi nhánh, vai trò). Sau khi bị đổi vai trò, có thể cần đăng nhập lại để "đăng ký lại" đúng nhóm thì mới nhận đúng các thông báo theo vai trò mới.
- **Đăng nhập nhiều thiết bị**: thông báo đẩy tới tất cả thiết bị bạn đang đăng nhập. Đăng xuất một thiết bị thì thiết bị đó ngừng nhận.
- **"Bạn không có quyền truy cập" khi service nội bộ gửi**: đây là chuyện kỹ thuật giữa các hệ thống, người dùng không gặp; nếu cả nhà tự nhiên không ai nhận được thông báo → báo kỹ thuật kiểm tra (xem FAQ).

## Ảnh hưởng tới
- **Chuông thông báo** trên mọi trang (badge số chưa đọc, dropdown danh sách) lấy dữ liệu từ đây.
- **Mọi module gửi thông báo** (Đơn hàng, Chat, Công việc, Lịch hẹn, Giao hàng, Khuyến mãi…) đều đi qua bộ máy này — nếu nó hỏng thì cả hệ thống mất thông báo, không riêng module nào.
- **Cấu hình thông báo** quyết định nội dung & người nhận; sửa cấu hình sẽ đổi việc bạn nhận hay không nhận một loại thông báo.
