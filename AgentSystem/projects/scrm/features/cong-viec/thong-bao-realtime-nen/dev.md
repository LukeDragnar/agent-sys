# Dev — Thông báo & realtime nền (chuông + socket)

> Feature **nền, cross-cutting** — không có màn hình/menu riêng. Gồm 2 hạ tầng:
> - **Push notification (FCM + chuông)** — service **Notification** (MongoDB), job Quartz + MassTransit consumer.
> - **Socket realtime (SignalR)** — service **SocketServer**, các Hub + controller relay HTTP→Hub.
> Registry liên quan: `registry/he-thong/NotificationsController.md`, `NotificationController.md`, `NotiHubController.md`, `InvoiceController_SocketServer.md`, `OrderProcessController_SocketServer.md`, `NotificationConfigController.md`.

## Màn hình & route (FE)
Không có route riêng. Điểm chạm FE:
| Điểm chạm | Mô tả |
|---|---|
| Chuông thông báo (header) | Badge `UnRead` + dropdown `GetsByUserId`; bấm dòng → `Read/{id}`; "đọc tất cả" → `ReadAll`. |
| Đăng ký FCM sau login | Lấy token Firebase → `registertoken`; đổi role → `RegisterTopic`. |
| Socket listeners | `@microsoft/signalr` `HubConnectionBuilder` → join group → `.on('<Event>')` đẩy vào `BehaviorSubject` (xem `skills/socket-realtime.md`). |

## Endpoint / Controller (trace registry)

### Push notification — service Notification (MongoDB)
`NotificationsController` (`[SBAuthorize]` controller-level) — **lõi realtime chuông** (registry `NotificationsController.md`):
| MaTinhNang / route | HTTP | Mục đích |
|---|---|---|
| `registertoken` | POST `api/Notifications/registertoken` | Đăng ký token FCM + topic cho user |
| `removetoken` | POST `api/Notifications/removetoken` | Hủy token (logout) |
| `RegisterTopic` | POST `api/Notifications/RegisterTopic` | Đăng ký lại full topic (sau đổi role) |
| `UnRead/{type}` | GET | Đếm chưa đọc (badge) — `type` **bị bỏ qua** |
| `GetsByUserId` | GET | Danh sách thông báo của tôi (aggregate join) |
| `Read/{id}` / `ReadAll` | POST/GET | Đánh dấu đã đọc 1 / tất cả |
| `Received/{id}` / `ViewAll` | POST/GET | **Gần như no-op** (thân bị comment) |
| `Create?key=<Config.Key>` | POST `[PrivateSBAuthorize]` | **Điểm vào chính: mọi service bắn thông báo** → CreateQueue (không gửi ngay) |
| `unsubcribeallbyuser?userid=` | POST `[PrivateSBAuthorize]` | Xóa hết token+topic khi khóa user |
| `{id}` | DELETE | Xóa NotificationInfo (fire-and-forget) |

`NotificationController` (service **Core**, registry `NotificationController.md`) — đăng ký token web push tách riêng: `RegisterToken` (`api/Notification/RegisterToken`), `RegisterAllTopic` (`api/Notification/RegisterAllTopic`, **không gate admin** — key comment).

`NotificationConfigController` (registry `NotificationConfigController.md`) — **không gửi noti**, chỉ là bảng tra quy tắc "sự kiện → mẫu + đối tượng nhận"; service nghiệp vụ đọc qua `GetDataUsedByTacNghiep / GetDataUsedByTrangThaiGiaoHang / GetDataUsedByTrangThaiLichHen` (`[PrivateSBAuthorize]`).

### Socket realtime — service SocketServer (SignalR)
| Controller (registry) | Hub | Group | Event | Auth |
|---|---|---|---|---|
| `NotiHubController` | `DonHangThanhToanHub` | `RoomUser_{userId}` / `DangDon_{dv}_{cn}` / `HuyDangDon_*` / group tùy ý | `message` / `SendMesageManHinhKhach` / `SendMesageDangDon` / `ReceiveGroupMessage`… | mixed (vài endpoint anonymous — xem Bug-prone) |
| `InvoiceController` (_SocketServer) | `InvoiceHub` | `CHINHANH_{IdChiNhanh}_STATISTICS_BATCH_UPDATE` / `_RESULT_BATCH_UPDATE` | `OnStatisticalBatchUpdate` / `OnResultBatchUpdate` | `[PrivateSBAuthorize]` |
| `OrderProcessController` (_SocketServer) | `ProcessEcommerceHub` | `{CrmUnitId}_{CrmUnitChildId}_{Type}` | `ReceiveProcessUpdate` | **anonymous** |

## Entities
**Push (MongoDB collections):**
- `UserToken` — token FCM + Topic + Device + NgayHetHan (+1 tháng).
- `UserTopic` — map user ↔ topic.
- `NotificationInfo` — dùng cho cả collection `notification` (đã gửi) và `notification_queue` (chờ gửi). Fields: Message (Title/Content/Image/Link/Time), Topic, Data, TrangThai (`START`/`PROCESS`/`FAIL`), SendedDate, CountSend.
- `UserNotification` — bản fan-out theo từng user: NotificationId, UserId, IsDaXem, NgayXem.

**Cấu hình (SQL — NotificationDataContext):**
- `NotificationConfig` — 1 dòng = 1 quy tắc: `LoaiCauHinhThongBao` + sự kiện (IdTacNghiepCan/IdKetQua | GiaoHangTrangThaiMa | TrangThaiLichHen) + mẫu (WebPatternCode/WebPatternCodeNext) + tập cờ đối tượng nhận (IsSendToSale / IsSendToTruongPhongSale / …Nhom… / IsSendToMkt / IsSendToThuNgan / IsSendToAdminChiNhanh / IsSendToAdminTong) + `SendToListIdUser` + WebSendLaterMinute / WebSendBeforMinute.

**Socket:** DTO push thuần (HubSendMesage*Dto, UpdateStatisticsBatchInvoiceSocketDto, HubSendMessageProcessDto…) — không lưu DB.

## Luồng code
**Vòng đời 1 push (Kênh A):**
1. Service nghiệp vụ → `NotificationsController.Create` (`NotificationsController.cs:106`) check `key == Config.Key` → `_service.CreateQueue(model)` (`:121`) → insert `notification_queue` (START).
2. `JobSendNotification.Execute` (`JobSendNotification.cs:43`) → `WorkerXuLy` (`:60`): vòng lặp `GetNotificationFromQueue` (`:66`) = FindOneAndUpdate atomic (`START` hoặc `FAIL && CountSend<3`, `SendedDate<=now`) → set PROCESS → copy `notification` + fan-out `user_notification` theo topic → lấy token → **publish per-token** `NotificationInfo`. Batch `CountMessage=200` (`:27`), cờ `isRunning` (`:28`) chống chạy chồng.
3. `SendNotificationConsumer.Consume` (`SendNotificationConsumer.cs:30`) → `FirebaseMessaging.DefaultInstance.SendAsync` (`:53`); `FirebaseMessagingException` (`:88`) → `RemoveTokenError` (`:90`).
4. Dọn rác: `JobClearOldNotifications.Execute` (`JobClearOldNotifications.cs:31`) → `ClearOldDataCascadeAsync(ttlDays, batchSize, throttleMs)` (`:49`) xóa `notification` + `user_notification` theo TTL; có guard "previous run still in progress".

**Topic generation:** `RegisterUserTopic` / `GetUserTopicByUserId` (prefix tại `SB.Core.Notification.Common.FirebaseTopicPrefix`) → `HE_THONG`, `USER_<id>`, `DONVI_<dv>`, `DONVI_<dv>_ROLE_<role>`, `DONVI_<dv>_CN_<cn>_ROLE_<role>`, `DONVI_<dv>_CN_<cn>` (uppercase).

**Socket relay (Kênh B):** controller `IHubContext<THub>.Clients.Group(groupName).SendAsync("<Event>", payload)`. Group name chuẩn hóa qua `FunctionCommon.GetGroupNameSocket(prefix, id, suffix)` (Invoice). Hubs ở `SocketServer/SocketServer.API/Hubs/` (`NotificationHub.cs`, `InvoiceHub.cs`, `ProcessEcommerceHub.cs`, `FileImportHub.cs`, `LogDataProcessHub.cs`). Multi-instance cần **Redis backplane** (xem `skills/socket-realtime.md`).

## Bug-prone / lưu ý (path:line — từ registry, KHÔNG sửa ở session này)
- **`NotificationsController.cs:206-230` `GetsByUserId`** — `totalRecord` truyền **by value** (không `out`/`ref`) → tổng trả về luôn **0**, FE mất tổng số trang.
- **`NotificationsController.cs:128-133` `CountUnReadNotification`** — route `{type}` không dùng → mọi type đếm như nhau.
- **`NotificationsController.cs:160-181` `Received`** + **`:193-204` `ViewAll`** (+ `NotificationService.cs:501-522 UpdateView`) — thân comment hết = **no-op** (giữ endpoint tránh 404).
- **`NotificationsController.cs:234-238` `Delete`** — `_service.Remove(id)` **không await** (fire-and-forget) + không dọn `user_notification` con → rác (đã có job dọn).
- **`NotificationService.cs:634-641` `CreateQueue`** — `@object.Id = new ObjectId()` tạo ObjectId **rỗng (toàn 0)** thay vì `GenerateNewId()` → nhiều bản queue cùng Id rỗng (chỉ an toàn vì job xóa ngay sau xử lý).
- **`Create` bảo mật yếu** — chỉ dựa shared secret `Config.Key` truyền qua **query-string** (lộ trong log/URL).
- **`NotificationController.cs:54` `RegisterAllTopic`** — không gắn permission key (`:55` comment) → bất kỳ user login nào cũng kích hoạt vòng lặp đăng ký topic toàn bộ user (nặng).
- **`RegisterTopic` xóa-rồi-tạo** topic (không diff) → cửa sổ ngắn user mất hết topic, sự kiện rơi đúng lúc đó có thể trượt.
- **`NotiHubController.cs:51,65,106`** — `[PrivateSBAuthorize]` bị **comment** trên SendMesageDangDon/HuyDangDon/PublishInvoice → **anonymous**, ai cũng push được noti đăng/hủy đơn.
- **`NotiHubController.cs:27` `SendMesage`** — `[HttpGet]` nhưng có side-effect push (không idempotent). **`:94 SendMessageToGroup`** anonymous + nhận GroupId tùy client → bơm message vào group bất kỳ. **`:107-115 SendMesagePublishInvoice`** no-op (comment hết).
- **`OrderProcessController.cs:29`** (SocketServer) — endpoint **không auth** → anonymous, spoof noti tiến trình đồng bộ đơn được.
- **`InvoiceController.cs:41-44`** (SocketServer) — `notify-result-from-batch` bọc try/catch rỗng → push fail vẫn trả Ok (**mất noti im lặng**).
- **`NotificationConfigController.cs:59-66` `SaveTrangThaiLichHen`** — key quyền comment → mọi user login lưu được (lỏng hơn 3 nút Save kia). **`:96-103 TimTheoDonViCustom`** `[AllowAnonymous]`. **`:67-73 DeleteByIdUnit`** dùng HTTP GET cho xóa hàng loạt.
- **`NotificationConfigService.cs:60-105 UpdateAsync`** — MarkDirty thủ công ~30 field; thêm field mới mà quên MarkDirty → update bỏ sót field (lỗi âm thầm).

## File source (path:line)
Gốc BE: `D:\PROJECT\Git\_SourceForCode\Group2\scrm`
- Push controller: `Notification\Notification.API\Controllers\NotificationsController.cs:48` (registertoken), `:89` (RegisterTopic), `:105` (Create), `:127` (UnRead), `:144` (Read), `:184` (ReadAll), `:206` (GetsByUserId).
- Push config controller: `Notification\Notification.API\Controllers\NotificationConfigController.cs`.
- Job gửi: `Notification\Notification.API\Job\JobSendNotification.cs:25` (class), `:43` (Execute), `:60` (WorkerXuLy), `:66` (GetNotificationFromQueue).
- Job dọn: `Notification\Notification.API\Job\JobClearOldNotifications.cs:14` (class), `:31` (Execute), `:49` (ClearOldDataCascadeAsync). Job khác: `JobRemoveToken.cs`, `JobSendRequestNotiToQueue.cs`.
- Consumer đẩy FCM: `Notification\Notification.API\Consumer\SendNotificationConsumer.cs:18` (class), `:30` (Consume), `:53` (SendAsync), `:90` (RemoveTokenError). Consumer đăng ký topic: `DangKyTopicNotificationConsumer.cs`.
- Service: `Notification\Notification.API\Services\NotificationService.cs` (CreateQueue ~`:634`, UpdateView ~`:501`); `Notification\Notification.Sevice\Generated\NotificationConfigService.cs:60` (UpdateAsync).
- Core token controller: `Core\Core.API\Controllers\NotificationController.cs:54` (RegisterAllTopic).
- SB.Core push lib: `Libs\SB.Core\Notification\NotificationService.cs`, `SendNotificationService.cs`, `SB.Core.Notification.Common` (FirebaseTopicPrefix).
- Socket relay controllers: `SocketServer\SocketServer.API\Controllers\NotiHubController.cs`, `InvoiceController.cs`, `OrderProcessController.cs`, `ProductProccessController.cs`, `FileImportController.cs`, `LogDataProcessController.cs`.
- Hubs: `SocketServer\SocketServer.API\Hubs\NotificationHub.cs`, `InvoiceHub.cs`, `ProcessEcommerceHub.cs`, `FileImportHub.cs`, `LogDataProcessHub.cs`.
- DTO push: `Libs\Models.Core\DTO\Notification.Client\Notihub\*` (HubSendMesage*Dto), `NotiEcommerce\HubSendMessageProcessDto.cs`.
- Recipe nội bộ kho: `projects/scrm/skills/socket-realtime.md`, `projects/scrm/skills/send-notification.md`.
