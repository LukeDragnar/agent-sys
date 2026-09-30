# Luồng — Thông báo & realtime nền (chuông thông báo)

> Có **2 kênh realtime** song song, đừng nhầm:
> - **Kênh A — Push notification (FCM, có lưu lịch sử):** sự kiện → tạo NotificationInfo → hàng đợi → job gửi → đẩy FCM tới thiết bị **và** lưu vào chuông (đã đọc/chưa đọc). Đây là "chuông thông báo".
> - **Kênh B — Socket/SignalR (đẩy tức thì, không lưu):** đẩy thẳng tới FE đang mở (cập nhật đơn POS, tiến trình đồng bộ đơn TMĐT, thống kê hóa đơn theo lô…). Không vào chuông, mất kết nối là mất event.

## Tiền điều kiện
- Người dùng đã **đăng nhập**; thiết bị đã lấy được **token FCM** và gọi đăng ký (`registertoken`) → token gắn các **topic**: `HE_THONG` (toàn hệ thống), `USER_<id>`, `DONVI_<dv>`, `DONVI_<dv>_ROLE_<role>`, `DONVI_<dv>_CN_<cn>_ROLE_<role>`, `DONVI_<dv>_CN_<cn>`.
- Trình duyệt/app đã **cho phép thông báo** (với kênh đẩy FCM).
- Với thông báo theo sự kiện nghiệp vụ: đơn vị đã có **Cấu hình thông báo** (sự kiện X → mẫu Y → đối tượng nhận Z). Không cấu hình → không gửi.
- Service nội bộ gọi `Create` phải có **shared secret** `key == Config.Key` (sai → "Bạn không có quyền truy cập").

## Luồng dùng — Kênh A (push + chuông)
1. **Sự kiện xảy ra** ở một module (đơn mới, chat mới, được giao việc, đổi trạng thái đơn/giao hàng, lịch hẹn tới giờ…).
2. Module đó **đọc Cấu hình thông báo** (qua `GetDataUsedBy*`) để biết: dùng **mẫu nào** + **gửi cho ai** (tập cờ đối tượng: Sale / trưởng phòng/nhóm Sale / MKT / thu ngân / admin chi nhánh / admin tổng / danh sách user chỉ định) → suy ra **Topic** đích.
3. Module **build `NotificationInfo`** (Title, Content, Image, Link, Data, Topic) → **POST `Create?key=Config.Key`** sang Notification service.
4. `Create` → `CreateQueue`: **chèn vào `notification_queue`** (TrangThai = `START`, có `SendedDate`) — **chưa gửi ngay**.
5. **Job nền `JobSendNotification`** (Quartz, batch 200) chạy định kỳ: lấy atomic 1 bản queue đủ điều kiện (`SendedDate<=now` và (`START` hoặc (`FAIL` và `CountSend<3`))) → set `PROCESS` → **copy sang `notification`** + **fan-out tạo `user_notification`** cho từng user thuộc topic → lấy token theo topic → **publish `NotificationInfo` (per-token)** cho consumer.
6. **`SendNotificationConsumer`** nhận → `FirebaseMessaging.SendAsync()` đẩy FCM tới từng thiết bị. Token chết (Unregistered/SenderIdMismatch) → `RemoveTokenError` xóa khỏi DB.
7. **FE**: nhận đẩy → hiện popup; đồng thời chuông gọi `UnRead` (badge) + `GetsByUserId` (danh sách). Bấm 1 dòng → `Read/{id}` set đã đọc; "đọc tất cả" → `ReadAll`.

## Luồng dùng — Kênh B (socket/SignalR, không lưu)
1. Service nghiệp vụ gọi **HTTP relay** ở SocketServer (vd `OrderProcess/CreateOrderCrm`, `Invoice/notify-result-from-batch`, `NotiHub/SendMesage…`).
2. Controller relay tới **Hub** (`ProcessEcommerceHub`, `InvoiceHub`, `DonHangThanhToanHub`…) → `Clients.Group(<groupName>).SendAsync("<Event>", payload)`.
3. **FE đang mở & đã join đúng group** nhận event tức thì (cập nhật tiến trình đồng bộ đơn TMĐT, màn hình khách POS, thống kê lô hóa đơn…). FE không mở / chưa join → **không nhận, không có chuông để xem lại**.

## Luồng cấu hình (admin)
1. Admin (HT-AD / DV-AD-TONG / HT-CSKH) vào **Hệ thống → Cấu hình thông báo**.
2. Chọn tab loại sự kiện: **Tác nghiệp** (kết quả thao tác CSKH), **Trạng thái đơn hàng/giao hàng**, **Trạng thái lịch hẹn**.
3. Với mỗi sự kiện: chọn **mẫu thông báo** (WebPatternCode) + **đối tượng nhận** (các cờ IsSendTo*) + thời điểm gửi (trước/sau X phút).
4. Lưu → các service nghiệp vụ đọc cấu hình này lúc runtime để quyết định gửi gì cho ai. (Chi tiết xem feature *Cấu hình thông báo*.)

## Các trường hợp / case biên
- **Token rỗng khi đăng ký** → `registertoken` trả true, bỏ qua (không lưu). Thiết bị đó không nhận đẩy.
- **Trùng device** → khi đăng ký token mới, các bản ghi token cũ cùng device bị **xóa hết** trước khi lưu mới (tránh đẩy trùng theo device cũ).
- **Đổi role/đơn vị** → gọi `RegisterTopic`: **xóa sạch topic cũ rồi sinh lại full** (không diff) → có **cửa sổ rất ngắn user mất topic**; nếu sự kiện rơi đúng lúc đó có thể trượt.
- **Socket rớt (Kênh B)** → FE phải `withAutomaticReconnect()` + **re-join group** khi `onreconnected` (mất group khi đứt). Event phát ra trong lúc rớt **bị mất** (không hàng đợi, không lưu) → fallback bằng cách reload dữ liệu từ API.
- **FCM gửi fail (Kênh A)** → bản queue đánh `FAIL`, job **retry** tới khi `CountSend >= 3` thì bỏ. Token lỗi vĩnh viễn bị xóa.
- **Trùng thông báo** → user thuộc nhiều topic cùng được cấu hình nhận (vd vừa Sale vừa trưởng nhóm) → nhận nhiều bản; hoặc nhiều thiết bị → nhiều popup. Khử trùng bằng cấu hình đối tượng nhận, không phải lỗi hệ thống.
- **Service nội bộ gọi `Create` sai key** → "Bạn không có quyền truy cập", không vào queue → cả luồng thông báo của nghiệp vụ đó **im lặng** (không ai nhận).
- **Cấu hình rỗng cho sự kiện** → không có đối tượng nhận → không sinh NotificationInfo → không ai nhận (đúng thiết kế, dễ tưởng là bug).
- **Job gửi đang chạy (isRunning=true)** → lượt sau **bỏ qua** để không chạy chồng → nếu một lượt treo lâu thì hàng đợi ùn, thông báo trễ.
- **Rác dữ liệu**: xóa 1 thông báo gốc không dọn `user_notification` con → đã có **Job dọn định kỳ** (`JobClearOldNotifications` theo TTL) xử lý.

## Ví dụ cụ thể có số
Đơn vị B có 1 admin tổng, 3 Sale (Lan/Bình/Cường), 1 thu ngân. Cấu hình: sự kiện **"Có đơn online mới"** → mẫu "Đơn mới #{Ma}" → gửi cho **Sale phụ trách** + **admin chi nhánh**.
- 10:00:00 — khách đặt đơn `#A123`, Sale phụ trách là **Lan**. Module Order build NotificationInfo Topic = `USER_<Lan>` + `DONVI_<B>_CN_<cn>_ROLE_AdminCN` → `Create` → vào `notification_queue` (START).
- 10:00:03 — `JobSendNotification` pick bản này: copy sang `notification`, tạo `user_notification` cho **Lan** và **admin chi nhánh** (2 bản), lấy token 2 người → publish 2 message per-token.
- 10:00:04 — `SendNotificationConsumer` đẩy FCM: Lan đang mở web (nhận popup, chuông từ 4 → **5 chưa đọc**) và mở cả app điện thoại (nhận thêm popup). Bình (Sale khác, không phụ trách) **không nhận** — đúng cấu hình.
- Lan bấm vào dòng `#A123` → `Read/{id}` → badge **5 → 4**. Bấm "đọc tất cả" → còn **0**.
- Cùng lúc, đơn TMĐT từ Shopee đang đồng bộ: SocketServer push `ReceiveProcessUpdate` (Kênh B) tới group `{CrmUnit}_{Child}_{Type}` → màn đang xem nhảy tiến trình "3/10 → 4/10". Nếu Lan F5 đúng giây đó (socket rớt rồi reconnect) → vài % tiến trình giữa chừng **không thấy**, nhưng số cuối vẫn đúng khi load lại API.

## Hậu điều kiện
- **Kênh A**: mỗi thông báo còn lưu ở `notification` + `user_notification` (trạng thái đã đọc/đã xem theo user) → tra lại được trên chuông cho tới khi job dọn theo TTL. Token chết bị loại khỏi DB.
- **Kênh B**: **không lưu gì** — chỉ đẩy tức thì cho FE đang kết nối; mất kết nối là mất, phải reload từ API.
- Đọc/đếm/đánh dấu **không sinh thông báo mới**, chỉ đổi cờ đã đọc của user hiện tại.
