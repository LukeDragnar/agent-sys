# Noti nội bộ SCRM — 2 đường gửi, 2 DB Mongo, bug URL thiếu `/api`

> Sự cố (session 2026-07-22, task 591-9 noti realtime ticket): noti ticket **không gửi được** dù code chạy, vì gọi nhầm đường HTTP có **URL thiếu `/api` → 404 im lặng**. Rework sang MassTransit dùng chung consumer công việc thì chạy. Ghi lại bản đồ 2 đường noti + các bẫy để lần sau khỏi mò.

## 2 đường gửi noti — 2 service, 2 DB Mongo KHÁC nhau

| Đường | Cách gửi | Service xử lý | DB Mongo (collection) | Dùng cho |
|---|---|---|---|---|
| **A. HTTP** | `POST {NotiServiceUrl}/api/Notifications/Create?key=ecosale@6886` | **Notification.API** (`Notification/Notification.API`) → `CreateQueue` → worker đẩy FCM | **`SCRM_Notification`** (`notification_queue` → `notification`) — host riêng (xem appsettings Notification.API) | Noti hệ thống: đơn, thanh toán... (rất nhiều doc) |
| **B. MassTransit** | `IPublishEndpoint.Publish(new CongViecNotiInfo{...})` | **WorkflowNotiConsumer** (`WorkflowNoti/WorkflowNotiConsumer.API`) → `_service.Create` + bắn FCM trực tiếp | **`SCRM_WorkFlowNoti`** (`notification`) — host base staging `10.0.161.23` | **Công việc + Ticket** (chuông FE đọc từ đây) |

→ **2 DB khác nhau, khác cả host.** Chuông FE "Công việc"/"Ticket" đọc **đường B** (`SCRM_WorkFlowNoti`); đừng nhầm với `SCRM_Notification`.

## 🔴 Bug: `INotificationService.SendNotification` (SB.Core) — URL thiếu `/api`

`Libs/SB.Core/Notification/NotificationService.cs` build:
```csharp
var url = $"{_serviceSettings.NotiServiceUrl}/Notifications/Create?key=ecosale@6886";  // ❌ thiếu /api
```
Controller thật là `[Route("api/[controller]")]` + `[HttpPost("Create")]` = **`/api/Notifications/Create`**. → POST vào URL thiếu `/api` **404**, `_clientHelper` trả lỗi, code gọi thường bọc try/catch nuốt → **noti biến mất im lặng**.
- Đối chiếu chỗ gọi ĐÚNG: `AuthenService` dùng `NotiServiceUrl + "/api/Notifications/removetoken"` (có `/api`).
- Probe xác minh: `POST /Notifications/Create` → **404**; `POST /api/Notifications/Create` → **401** (route có, chỉ thiếu token).

**→ Kết luận: KHÔNG dùng `INotificationService.SendNotification` (SB.Core) để gửi noti.** Dùng **đường B (MassTransit `CongViecNotiInfo`)** — đã proven, và noti về đúng DB chuông FE đọc.

## Cách dùng đúng — publish `CongViecNotiInfo` (đường B)

Contract `SB.Core/DTO/NotificationInfo.cs > CongViecNotiInfo` (dùng chung cho công việc + nguồn khác):
- `Action` (null/rỗng → mặc định `"CONGVIEC_MOI"`; nguồn khác đặt `"TICKET_CHAT_MOI"`/`"TICKET_DOI_TRANGTHAI"`...).
- `DataThem` (Dictionary) — nhét data phụ vào payload (vd ticket: `IdTicket`, `MaTicket` để FE deep-link).
- `ListUserIdReceive` — consumer tự lặp từng người → topic `USER_<id>` → `Create` + FCM.
- `Title`/`Content` dựng sẵn; `UserIdSend`, `NguoiTaoTen`.

Consumer `CongViecNotiConsumer` (WorkflowNotiConsumer.API) đã generalize: `action = item.Action ?? "CONGVIEC_MOI"`, chỉ thêm `IdCongViec` khi CONGVIEC, merge `DataThem`. → Thêm nguồn noti mới = **thêm Action + DataThem**, KHÔNG cần consumer/queue mới (reuse tối đa). Backward-compat công việc: message cũ (Action=null) ra data y hệt.

## FE phân tab chuông theo `data["Action"]`

`header.component.ts`: chuông có 3 tab — Thông báo / Công việc / Ticket.
- **Công việc + Ticket đọc CHUNG** `workflowNotiService` (store `SCRM_WorkFlowNoti`), **tách client-side theo Action**: Công việc lọc `!startsWith("TICKET")`, Ticket lọc `startsWith("TICKET")`.
- Thông báo đọc `notificationService` (store `SCRM_Notification`, đường A) — **KHÁC service/DB**.
- 🔴 Bẫy: đừng cho tab Ticket đọc `notificationService` (đường A) — ticket đi đường B nên sẽ **rỗng**; và tab Công việc phải **lọc TICKET_* ra** kẻo vơ luôn noti ticket. Đếm badge từ list đã lọc (không dùng `countUnReadNotification` — nó tính cả ticket).
- FCM real-time (`receiveMessage`) cũng branch theo `data["Action"]` vào đúng list.

## Gotcha khác
- **Tạo ticket KHÔNG bắn noti.** Hook chỉ ở **chat mới** (`TicketCustomerChatService.GuiTinAsync`) + **đổi trạng thái** (`TicketCustomerService.CapNhatTicketAsync` khi `logTrangThai != null`). Test noti = chat/đổi trạng thái, KHÔNG phải tạo mới. Người nhận **trừ actor** → tự thao tác ticket của chính mình (không ai khác liên quan) = 0 người nhận = không noti.
- **`NgayTao` Mongo lưu UTC** (local = UTC+7). Filter theo giờ nhớ quy đổi (11:25 UTC = 18:25 local).
- **Recipient CSKH-SB**: `Core GetCskhSbTeamUserIds` lấy user `IdDonVi == SANDBOX_TEAM_DON_VI_ID && (IsCSKH || IsAdmin) && !IsDaNghiViec` — gồm **cả admin** SB team, không chỉ CSKH.

## Debug staging bằng pymongo
Query trực tiếp để verify noti có tạo không + đúng Action/data:
```python
# connection lấy từ Libs/SB.Core/appsettings.base.Staging.json > MongoSettings.UrlConnect (host 10.0.161.23) — KHÔNG hardcode creds vào kho
db = client["SCRM_WorkFlowNoti"]["notification"]
db.count_documents({"Data.Action": {"$in": ["TICKET_CHAT_MOI","TICKET_DOI_TRANGTHAI"]}})
# doc mới nhất: .find().sort("NgayTao", -1).limit(3) → xem Data.Action, Data.IdTicket, Message.Title
```

## File nguồn
- Client sai (đừng dùng): `Libs/SB.Core/Notification/NotificationService.cs`
- Contract: `Libs/SB.Core/DTO/NotificationInfo.cs` (`CongViecNotiInfo`)
- Consumer: `WorkflowNoti/WorkflowNotiConsumer.API/Consumer/CongViecNotiConsumer.cs`
- Producer mẫu: `WorkFlow/WorkFlow.Service/Generated/CongViecService.cs` (công việc), `.../Ticket/TicketNotiService.cs` (ticket)
- FE chuông: `scrm_frontend/src/app/shared/template/header/header.component.ts`
