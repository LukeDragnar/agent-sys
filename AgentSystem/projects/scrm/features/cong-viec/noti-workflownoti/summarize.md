---
module: WorkflowNoti
feature: noti-cong-viec
type: feature-note
status: new
snapshot: true
watermark_hash: e617abcbbf   # origin/release_product HEAD lúc quét (scrm_build)
verify_note: "Snapshot — kho là bản đồ, code là chân lý. VERIFY lại path:line với source hiện tại (feature init f79b7a3e 2026-06-24; hotfix f90c80d 2026-07-09)."
cross_module_in: [WorkFlow.CongViecService]
services: [WorkflowNoti.API, WorkflowNotiConsumer.API]
message_contract: SB.Core.Notification.CongViecNotiInfo
---

# WorkflowNoti — Noti giao việc công việc (WorkFlow → WorkflowNoti → FCM)

> **Snapshot, verify code.** Doc này mô tả MODULE MỚI `WorkflowNoti` (chưa từng có trong kho) + luồng noti giao việc cross-module. Số dòng/behavior chốt từ source tại watermark `e617abcbbf`; đọc để định hướng ripple/behavior, nhưng phải mở source verify lại trước khi sửa.

## 1. Module là gì

`WorkflowNoti` là cụm 2 microservice độc lập:

| Service | Vai trò | Data store |
|---|---|---|
| **WorkflowNoti.API** | REST quản lý thông báo / token / topic / template email / cấu hình noti | Mongo `NotificationDataContext` + SQL entity (NotiTemplate, NotificationConfig, ThongBao...) |
| **WorkflowNotiConsumer.API** | MassTransit consumer, nhận message nghiệp vụ → đẩy FCM push | dùng chung Mongo Notification (usertopic/usertoken) |

Feature trọng tâm của delta init: **"Thêm noti cho công việc"** — khi WorkFlow tạo/đổi/gỡ người của công việc, người liên quan nhận push notification qua FCM.

## 2. Luồng noti giao việc (end-to-end)

```
WorkFlow.Service/CongViecService (publish)
        │  Publish CongViecNotiInfo  (SB.Core.Notification)
        ▼  RabbitMQ queue QueueKeys.WORKFLOWNOTI_CONGVIEC  (PrefetchCount=20, retry 2×3000ms)
WorkflowNotiConsumer.API/Consumer/CongViecNotiConsumer.Consume
        │  mỗi userId trong ListUserIdReceive → topic = user_<id> (Common.GetTopicFromUser)
        │  Create NotificationInfo vào Mongo + push FCM tới MỌI token của topic
        ▼
FCM (Parallel MaxDegreeOfParallelism = 10)
```

### 2a. Bên PUBLISH — `WorkFlow.Service/Generated/CongViecService.cs`
Ba tình huống publish `CongViecNotiInfo` (đều nuốt lỗi, KHÔNG fail nghiệp vụ công việc):

1. **TẠO MỚI công việc** (`LuuAsync`, `isCreate`) — local func `GuiNotiGiaoViec(ids, vaiTro)` (`:2300`):
   - `dto.ListIdUser` → vai trò **"Người thực hiện"** (`:2324`)
   - mỗi trường mở rộng kiểu user (`userExtFieldValues`) → vai trò = tên trường (`:2325-2329`)
   - Content: `"Bạn được giao công việc với vai trò {vaiTro}: {TieuDe}"`, Title `"Công việc mới"`.
2. **GỠ người khỏi công việc** khi update (`!isCreate`, `:2333-2360`) — chỉ `NguoiThucHien` + `CotMoRong` trong `toRemove`; Title `"Gỡ khỏi công việc"`.
3. **UPDATE người được giao qua `CapNhatTruong`** (`:3744-3789`) — tính **delta thật**: `usersToInsert`/`usersToDelete` lọc chéo → `nguoiThemMoi` (Title "Công việc mới") + `nguoiBiGo` (Title "Gỡ khỏi công việc"). Bọc bởi `if (!notiBoQuaGiaoViec)`.

### 2b. Bên CONSUME — `WorkflowNotiConsumer.API/Consumer/CongViecNotiConsumer.cs`
- Class `:13`, `Consume` `:22`. Guard: `ListUserIdReceive` rỗng → `return`.
- Vòng lặp `ListUserIdReceive.Distinct()` `:47` → topic `user_<id>` (`Common.GetTopicFromUser`, `:49`).
- Tạo `NotificationInfo` với `Id = ObjectId.GenerateNewId()` sinh **1 lần** (`:52`) để `_id` Mongo / bản JSON push / FCM trùng nhau.
- Data payload: `Action = "CONGVIEC_MOI"`, `IdCongViec`, `Link` (nếu có) (`:38-44`) + `data["notificationInfo"]` = JSON của noti để FE parse `value.data.notificationInfo` (`:64`).
- `SendToFireBase` (`:76`): `_service.Create(copy)` (lưu Mongo) → `GetUserTokenByTopic` (`:92`) → `Parallel.ForEach` MaxDegreeOfParallelism=10 (`:95`) → `SendFirebase` từng token.
- `SendFirebase` (`:109`): gửi `FirebaseMessaging.DefaultInstance.SendAsync` (Notification + Android + Apns, sound `KhoDuocsound` / `KhoDuocsound.caf`). Token lỗi `FirebaseMessagingException` với `ErrorCode.NotFound` → `_service.RemoveTokenError(token)` (`:142-144`).
- **Nội dung lấy THẲNG từ message**, KHÔNG tra `NotiTemplate` DB.

## 3. Quy ước / behavior đúng (invariants)

- 🔴 **NGƯỜI PHÊ DUYỆT KHÔNG nhận noti giao việc.** Field `"listIdNguoiPheDuyet"` (map `approverUserIds`, `CongViecService:794`) set `notiBoQuaGiaoViec = true` (`:3525`) → nhánh publish `:3744` bị bỏ. Chỉ **Người thực hiện** + **user trường mở rộng** nhận noti.
- 🔴 **Lỗi noti nuốt hoàn toàn — KHÔNG fail nghiệp vụ công việc.** Cả bên publish (mọi `catch {}` quanh `Publish`) lẫn bên consume (`Consume` bọc `try/catch` trống `:68`, `SendToFireBase`/`SendFirebase` cũng nuốt). Việc công việc luôn thành công dù noti hỏng.
- **1 người = 1 topic `user_<id>`, push tới MỌI token** đã đăng ký của topic. Token chết (`NotFound`) tự bị `RemoveTokenError`.
- `_id` ObjectId sinh 1 lần và tái dùng cho cả record Mongo lẫn JSON push (đừng sinh lại → sẽ lệch id giữa Mongo/FE).

## 4. Backbone REST (module registry — cần tạo cụm `registry/workflownoti/`)

WorkflowNoti.API controllers (đều `[Route("api/[controller]")]`, `[SBAuthorize]` trừ nơi ghi khác):

| Controller | Route | Endpoint chính |
|---|---|---|
| **NotificationsController** (`:18`) | `api/Notifications` | `registertoken`, `removetoken`, `unsubcribeallbyuser` (PrivateSBAuthorize), `RegisterTopic`, `Create` (PrivateSBAuthorize + key=Config.Key → CreateQueue), `UnRead/{type}`, `Read/{id}`, `Received/{id}`, `ReadAll`, `ViewAll`, `GetsByUserId`, `DELETE {id}` |
| **ThongBaoController** | `api/ThongBao` | `TimTheoDieuKien`, `TimTheoId`, `ThemMoi`, `CapNhatThongTin`, `Xoa`, `send-test` |
| **NotificationConfigController** | `api/NotificationConfig` | `TimTheoDonVi`, `SaveTacNghiep`, `SaveTrangThaiDonHang`, `SaveTrangThaiLichHen`, `DeleteByIdUnit`, `GetDataUsedBy*`, `TimTheoDonViCustom` |
| **NotiTemplateController** | `api/NotiTemplate` | `TimTheoDieuKien`, `TimTheoMa`, `CapNhatThongTin`, `Xoa`, `DanhSachMaThongBao`, `DanhSachNhomThongBao` |
| **EmailController** | `api/Email` | quản lý gửi email / cấu hình `CauHinhEmail` |
| ThongBaoNguoiNhanController | (route commented) | **INACTIVE** — route bị comment, không mount |

Consumer registry node: **CongViecNotiConsumer** = MassTransit `IConsumer<CongViecNotiInfo>`, `cross_module_in` từ `WorkFlow.CongViecService`.

## 5. Ripple cần bổ sung vào node WorkFlow

Thêm `cross_module_out` cho registry node của **WorkFlow CongViecController / CongViecService**:
> `CongViecService.LuuAsync` / `CapNhatTruong` publish `CongViecNotiInfo` → `WorkflowNoti` (CongViecNotiConsumer) lưu Mongo + push FCM. Đổi tập người / field mở rộng / logic delta người giao ⇒ ảnh hưởng ai nhận noti.

## 6. Known-issue / cạm bẫy

- ⚠️ **4 consumer cũ đã bị XÓA** ở hotfix `f90c80d` (2026-07-09): `DangKyTopicNotification`, `KhoSo_SendNotification`, `SendNotification`, `XuLyGuiThongBao` + trim `MassTransitExtensions`. **Consumer service hiện CHỈ còn `CongViecNotiConsumer` active** (`WorkflowNotiConsumer.API/Services/MassTransitExtensions.cs:53-58`, endpoint `QueueKeys.WORKFLOWNOTI_CONGVIEC`). ĐỪNG tham chiếu 4 consumer đã gỡ.
- ⚠️ **Debug bypass:** nếu `ASPNETCORE_ENVIRONMENT` là `*Debug` (`IsDebug`), `ReceiveEndpoint` KHÔNG mount → consumer **không nhận** message (MassTransitExtensions `:51`). Debug env sẽ "mất" noti — đúng thiết kế, không phải bug.
- ⚠️ **Catch trống nuốt lỗi gửi noti** (publish + consume + per-token). Khi noti không tới, KHÔNG có exception/log nghiệp vụ → khó trace; chỉ có `Console.WriteLine` trong `SendFirebase`. Điều tra phải bám FCM log + token store, không trông vào lỗi trả về.
- Nội dung push cứng từ message (không qua `NotiTemplate`) → sửa wording noti giao việc phải sửa ở **WorkFlow.CongViecService** (`Content`/`Title`), không phải ở template DB.

## 7. Entity / contract mới

- **SQL** (`WorkflowNoti.DataContext/Entity/`): `NotiGroupCode`, `NotiTemplate`, `NotificationConfig`, `ThongBao`, `ThongBaoNguoiNhan`.
- **Mongo** (`EntityMongo/`): `Notification`, `NotificationJoinResult`, `UserNotification`, `UserToken`, `UserTopic`, `UserTopicRequest`, `ThongBaoHeThong`. DbContext: `Generated/DbContexts/NotificationDataContext.cs`.
- **Model/DTO**: `CauHinhEmail`, `NotificationInfoDTO`.
- **Message contract**: `CongViecNotiInfo` (namespace `SB.Core.Notification`) — publish ở WorkFlow, consume ở WorkflowNoti. Field dùng: `IdCongViec`, `TieuDe`, `Title`, `Content`, `DonViIdReceive`, `ListUserIdReceive`, `UserIdSend`, `NguoiTaoTen`, `Link`, `NgayTao`.
