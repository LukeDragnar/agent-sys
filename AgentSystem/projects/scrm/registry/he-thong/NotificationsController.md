---
controller: NotificationsController
module: he-thong
service: Notification
man_hinh_ui: "Header chuông thông báo (badge số chưa đọc + dropdown danh sách); không có màn hình quản trị riêng — phục vụ realtime push của toàn web/app"
muc_dich: "Quản lý token Firebase + đăng ký topic của user, đẩy thông báo vào queue gửi, và đọc/đếm/đánh dấu-đã-đọc thông báo cá nhân (lưu MongoDB)."
alias: ["Notifications", "thông báo realtime", "chuông thông báo", "push firebase", "FCM token"]
auth: mixed
state_machine: [START, PROCESS, FAIL]   # trạng thái bản ghi trong notification_queue (gửi FCM)
deprecated: false
entities: [UserToken, UserTopic, NotificationInfo, UserNotification]   # tất cả là collection MongoDB
stored_procs: []
depends: ["FirebaseMessaging (FCM SubscribeToTopic/SendAsync)", "MongoDB (notification/user_notification/usertoken/usertopic/notification_queue)", "MassTransit IPublishEndpoint", "SB.Core.Notification.Common (sinh topic theo USER/DONVI/CN/ROLE)"]
cross_module_in: ["Mọi service muốn bắn thông báo → POST Create (key=Config.Key)", "Core/UserService → khi thêm/sửa user-role publish UserDangKyTopicDto cho DangKyTopicNotificationConsumer", "AuthService/Login FE → registertoken sau khi lấy FCM token"]
cross_module_out: ["FCM → thiết bị Web/Android/iOS (push)", "MongoDB notification/user_notification (lưu lịch sử + trạng thái đã đọc)"]
---

# NotificationsController — Đăng ký push Firebase & thông báo cá nhân (realtime)

Controller "lõi realtime" của hệ thống: FE/app gọi để **đăng ký token FCM + topic**, các service khác gọi `Create` để **bắn thông báo**, FE gọi đọc/đếm/đánh dấu đã đọc. Dữ liệu nằm hoàn toàn ở **MongoDB** (không phải SQL). Controller-level `[SBAuthorize()]` (cần login), một số method nâng `[PrivateSBAuthorize]` (service-to-service).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Đăng ký token FCM | (ngầm, sau login lấy FCM token) | POST `api/Notifications/registertoken` | — | public (login) | App vừa lấy token Firebase | Xóa token/device cũ trùng → `RegisterUserTopic` cho user → lưu UserToken (Topic=`HE_THONG`, NgayHetHan=+1 tháng). Token rỗng → trả true, bỏ qua. | Mongo `usertoken`/`usertopic` | Device lấy từ `IUserPrincipalService.GetDevice()`; trùng device bị xóa hết bản ghi cũ |
| 2 | Hủy token | (logout / tắt nhận) | POST `api/Notifications/removetoken` | — | public (login) | — | DeleteMany UserToken theo Token HOẶC Device. | Mongo `usertoken` | Không unsubscribe FCM topic (chỉ xóa DB) |
| 3 | Đăng ký lại topic | (sau khi đổi role/đơn vị) | POST `api/Notifications/RegisterTopic` | — | public (login) | — | `RegisterUserTopic`: **xóa sạch** topic cũ của user rồi sinh lại full bộ (HE_THONG, USER_, DONVI_, DONVI_+ROLE_, DONVI_+CN_+ROLE_, DONVI_+CN_). | Mongo `usertopic` | Xóa-rồi-tạo (không diff) → cửa sổ ngắn user mất topic |
| 4 | Đếm thông báo chưa đọc | Badge số trên chuông | GET `api/Notifications/UnRead/{type}` | — | public (login) | Luôn (poll header) | Count UserNotification của UserId có `IsDaXem=false`. (`type` không dùng trong query) | Mongo `user_notification` | Param `type` bị bỏ qua |
| 5 | Danh sách thông báo của tôi | Dropdown chuông (phân trang) | GET `api/Notifications/GetsByUserId` | — | public (login) | Mở dropdown / cuộn | Aggregate `user_notification` $lookup sang `notification`, sort NgayTao desc, skip/limit. Lọc theo fromDate/toDate (dd/MM/yyyy). | Mongo aggregate join | `totalRecord` truyền by-value (không out) → tổng trả luôn 0 (xem lỗi) |
| 6 | Đọc 1 thông báo | Click 1 dòng | POST `api/Notifications/Read/{id}` | — | public (login) | — | Set UserNotification(IsDaXem=true, NgayXem=now) theo NotificationId+UserId. Không tồn tại → lỗi "Thông báo không tồn tại". | Mongo `user_notification` | — |
| 7 | Đọc tất cả | "Đánh dấu đã đọc tất cả" | GET `api/Notifications/ReadAll` | — | public (login) | — | UpdateMany UserNotification IsDaXem=false → true, NgayXem=now. | Mongo `user_notification` | — |
| 8 | Đánh dấu đã nhận | (ngầm từ app) | POST `api/Notifications/Received/{id}` | — | public (login) | — | Get NotificationInfo theo id → Update (gần như no-op, dòng set IsReceived bị comment). id sai → ERR_OBJECT_ID. | Mongo `notification` | Thân hàm gần như rỗng (set cờ bị comment) |
| 9 | Cập nhật view | (mở trang thông báo) | GET `api/Notifications/ViewAll` | — | public (login) | — | Gọi `UpdateView` — **toàn bộ thân đã comment** → không làm gì, chỉ trả UserId. | — | Dead method (no-op) |
| 10 | Xóa 1 thông báo | Xóa | DELETE `api/Notifications/{id}` | — | public (login) | — | `Remove(id)` xóa NotificationInfo (không await — fire & forget). | Mongo `notification` | Xóa bản gốc, không xóa UserNotification con → rác |

## Endpoint nội bộ (private / service-to-service)
- `[PrivateSBAuthorize]` **POST `Create?key=<Config.Key>`** — **điểm vào chính để mọi service bắn thông báo**. Check `key == Config.Key` (sai → "Bạn không có quyền truy cập"). Set Message.Time=now, Link rỗng→BaseClientUrl, rồi `CreateQueue` (insert vào `notification_queue`, KHÔNG gửi ngay). Job `JobSendNotification` sẽ pick & push FCM.
- `[PrivateSBAuthorize]` **POST `unsubcribeallbyuser?userid=`** — xóa toàn bộ UserToken + UserTopic của user (gọi khi xóa/khóa tài khoản).

## Quy tắc nghiệp vụ (topic & vòng đời queue)
Bộ topic 1 user được sinh trong `RegisterUserTopic`/`GetUserTopicByUserId` (prefix tại `SB.Core.Notification.Common.FirebaseTopicPrefix`):
- `HE_THONG` (All — toàn hệ thống), `USER_<userId>`, `DONVI_<donViId>`, `DONVI_<dv>_ROLE_<role>` (mỗi role), `DONVI_<dv>_CN_<cn>_ROLE_<role>` (mỗi role), `DONVI_<dv>_CN_<cn>`. Tất cả uppercase.
- Khi service `Create` đẩy noti với 1 Topic → job gửi tới mọi token đăng ký topic đó.

Vòng đời 1 thông báo (collection `notification_queue`):
1. `Create` → `CreateQueue` insert (TrangThai do producer set, thường `START`, có `SendedDate`).
2. `JobSendNotification` (Quartz, batch 200): `GetNotificationFromQueue` = FindOneAndUpdate atomic lấy bản `SendedDate<=now && (START || (FAIL && CountSend<3))` → set `PROCESS`; copy sang `notification` + tạo `user_notification` cho từng user của topic; lấy token theo topic; publish `NotificationInfo` (per-token) cho `SendNotificationConsumer` push FCM.

## Quan hệ
- **Gọi sang:** FCM (`FirebaseMessaging.DefaultInstance` Subscribe/Send — thực thi ở Service/Consumer), MongoDB, MassTransit publish.
- **Dữ liệu (entities):** `UserToken` (token FCM + Topic + NgayHetHan), `UserTopic` (map user↔topic), `NotificationInfo` (notification & notification_queue), `UserNotification` (đã đọc/đã xem theo user).
- **Ripple / đồng bộ:** đẩy queue → `JobSendNotification` → `SendNotificationConsumer` → FCM → thiết bị; token lỗi (Unregistered/SenderIdMismatch) bị `RemoveTokenError` xóa khỏi DB.

## Luồng xuyên module (service khác bắn thông báo → user nhận realtime)
1. Service nghiệp vụ (Order, Promotion, CSKH...) build `NotificationInfo` (Message Title/Content/Image/Link, Topic, Data) → POST `Create?key=Config.Key`.
2. Lưu `notification_queue` (chưa gửi). `JobSendNotification` pick atomic → ghi `notification` + fan-out `user_notification` cho mọi user của topic → publish per-token.
3. `SendNotificationConsumer` push FCM tới từng token; FE/app nhận realtime, badge `UnRead` tăng. Token chết → xóa.

## Lỗi đã biết / lưu ý kỹ thuật
- `NotificationsController.cs:206-230` — `GetsByUserId`: `totalRecord` truyền **by value** vào service (không `out`/`ref`) → `ResponseResult(totalRecord:...)` luôn 0; FE không có tổng số trang đúng.
- `NotificationsController.cs:128-133` — `CountUnReadNotification` nhận route `{type}` nhưng không dùng → mọi type đếm như nhau.
- `NotificationsController.cs:193-204` + `NotificationService.cs:501-522` — `ViewAll`/`UpdateView` toàn bộ comment = **no-op** (giữ endpoint chỉ để FE không lỗi 404).
- `NotificationsController.cs:160-181` `Received` — logic set cờ `IsReceived` bị comment → chỉ Get rồi Update y nguyên (vô nghĩa).
- `NotificationsController.cs:234-238` `Delete` — gọi `_service.Remove(id)` **không await** (fire-and-forget) và không dọn `user_notification` con → rác tích lũy (đã có Job `JobClearOldNotifications`/`ClearOldDataCascadeAsync` dọn định kỳ).
- `NotificationService.cs:634-641` `CreateQueue` — `@object.Id = new ObjectId()` tạo ObjectId **rỗng (toàn 0)** thay vì `GenerateNewId()` → nhiều bản queue có thể cùng Id rỗng (chỉ an toàn vì job xóa ngay sau khi xử lý).
- Bảo mật `Create` chỉ dựa vào shared secret `Config.Key` truyền qua query-string (lộ trong log/URL).
