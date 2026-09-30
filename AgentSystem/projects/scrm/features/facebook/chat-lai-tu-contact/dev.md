---
module: Facebook
feature: chat-lai-tu-contact (realtime SignalR + gửi chat từ Danh sách Contact)
type: feature-note
status: draft
snapshot: "Snapshot — VERIFY lại với code hiện tại. Kho là bản đồ, code là chân lý."
watermark_hash: e617abcbbf   # HEAD origin/release_product lúc quét (git rev-parse --short)
verified_files:
  - Facebook/Facebook.API/Controllers/FacebookSocketNotifyController.cs (đọc full, đã verify)
  - Facebook/FacebookSocket.API/Hubs/FacebookContactChatHub.cs (đọc full, đã verify)
  - Facebook/FacebookSocket.API/Consumer/FacebookSocketReloadConsumer.cs (đọc full, đã verify)
  - Facebook/Facebook.DataContext/Entity/FBCustomerAdId.cs (đọc full, đã verify)
  - Facebook/Facebook.DataContext/FacebookDto/MessageInput.cs:60-78 (đã verify field)
  - Facebook/Facebook.Service/Generated/Helper/FacebookHelper.cs:15,27,81-88,339-347 (đã verify)
  - Facebook/Facebook.Service/Generated/FBFanpageService.cs:1209-1254 (đã verify)
related_registry:
  - registry/facebook/FBMessageController.md (CẬP NHẬT: +SendMessageByContactId, +GetOneItem)
  - registry/facebook/FBFanpageController.md (CẬP NHẬT: +ReSubscribeAllFanpage)
  - registry/facebook/FacebookSocketNotifyController.md (TẠO MỚI)
---

# Facebook — "Chat lại từ Danh sách Contact" (realtime SignalR)

> ⚠️ **Snapshot theo watermark `e617abcbbf`.** Đọc để ĐỊNH HƯỚNG (kiến trúc/ripple/known-issue). `path:line` có thể lệch sau các commit sau — verify lại bằng codemap/ripgrep trước khi chốt số/behavior.

## Mục đích

Cho phép nhân viên **mở modal chat từ màn Danh sách Contact** và (1) gửi tin **thẳng lên Facebook không lưu trữ**, (2) nhận **realtime "RELOAD"** khi có tin mới của đúng hội thoại đang xem — mà **không cần polling** và **không auto-join theo role** (chỉ hội thoại đang mở mới nhận signal).

## Service mới: `FacebookSocket.API`

- Service SignalR realtime **riêng** cho luồng contact-chat (khác `FacebookChat`/`ChatHub` cũ — hub đó auto-join theo role).
- Cần **bổ sung vào `architecture.md`** (danh sách ~40 service).
- Thành phần chính:
  - `Hubs/FacebookContactChatHub.cs` — hub client kết nối.
  - `Consumer/FacebookSocketReloadConsumer.cs` — consume RabbitMQ → bắn signal.
  - `Program.cs` — wiring SignalR + MassTransit consumer.

## Kiến trúc tín hiệu (đường đi 1 signal RELOAD)

```
[FB webhook có tin mới]
   → FacebookWebhookProcess (env HOME của process)
   → HTTP POST api/FacebookSocketNotify/Reload  (fire-and-forget, tới ENV chứa fanpage)
        └─ FacebookSocketNotifyController.Reload  [AllowAnonymous]
             ├─ GATE 1: SCARD fb:watching:{convKey} > 0 ?  (không ai xem → bỏ, không publish)
             └─ publish FacebookSocketReloadEvent (RabbitMQ, bus BusReceiveFB env này)
                  └─ FacebookSocketReloadConsumer.Consume
                       ├─ GATE 2 (rẻ, chống bắn thừa): SCARD fb:watching:{convKey} > 0 ?
                       └─ _hub.Clients.Group(convKey).SendAsync("RELOAD", { convKey, item })
                            → client đang Join(convKey) nhận signal
```

**Vì sao 2 chặng (HTTP rồi RabbitMQ)?** `FacebookWebhookProcess` chạy ở env HOME của nó; fanpage có thể ở **env khác**. HTTP cross-env đóng vai "webhook nội bộ" ([AllowAnonymous] giống webhook receiver, payload chỉ là `convKey` + item shape — **không mang nội dung tin nhắn nhạy cảm**). Sang đúng env rồi mới publish RabbitMQ nội bộ env đó để consumer bắn SignalR.

## Watching-set — cơ chế "chỉ ai đang xem mới nhận"

Redis set key: `fb:watching:{convKey}`, **member = connectionId**.

| Sự kiện | Hub làm gì |
|---|---|
| `Join(convKey)` | `Groups.AddToGroupAsync` + `SADD fb:watching:{convKey}` + `EXPIRE 4h` (backstop) + nhớ vào map in-memory để cleanup |
| `Leave(convKey)` | `RemoveFromGroup` + `SREM` + xóa khỏi map |
| `OnDisconnectedAsync` | duyệt hết convKey của connection (đóng tab/F5/mất mạng) → `SREM` best-effort (nuốt lỗi, TTL dọn nốt) |

- **TTL 4h** là backstop chống rác nếu SREM lỡ không chạy (mất mạng đột ngột).
- Map in-memory (`ConcurrentDictionary<connectionId, HashSet<convKey>>`) **chỉ** phục vụ `OnDisconnected` biết SREM key nào — **không** phải nguồn gate; nguồn gate là Redis (shared cross-service với webhook process).
- **KHÔNG auto-join theo role** — đây là khác biệt cốt lõi so với `ChatHub`: chỉ hội thoại **đang mở modal** mới nằm trong watching-set.

`FacebookContactChatHub.cs:16` map in-memory · `:20` TTL 4h · `:31` Join · `:46` Leave · `:59` OnDisconnected.

## Item shape trong signal RELOAD

`SendAsync("RELOAD", { convKey, item })`:
- `item` **có sẵn shape** để client **append thẳng** vào UI, không cần gọi API lấy lại.
- `item` **rỗng** → client **fallback reload** hội thoại.

## Endpoint mới — gửi/lấy chat từ contact (`FBMessageController`)

### `POST api/FBMessage/SendMessageByContactId` `[FromForm]`
Gửi **thẳng lên Facebook, KHÔNG lưu trữ**. Nhận form-data để kèm file.

DTO `SendMessageByContactIdInput` (`MessageInput.cs:60`):
| Field | Ý nghĩa |
|---|---|
| `ContactId` (Guid) | contact cần chat |
| `MessageId` (string?) | **FBComment.CommentId** để định vị đúng hội thoại; **rỗng → lấy hội thoại đầu tiên** của contact |
| `Message` (string?) | nội dung |
| `File` (IFormFile?) | file đính kèm |
| `ClientMsgId` (string?) | **GUID client sinh** cho tin optimistic — BE **echo lại** để client map `GUID → id FB thật`, replace tin fake |

DTO `SendMessageByContactIdResult` (`MessageInput.cs:72`): `MessageId` (message_id/comment id FB trả về) + `ClientMsgId` (echo GUID để client định vị tin optimistic cần replace).

> **Optimistic UI pattern:** client hiển thị tin ngay với GUID tạm → BE trả về `{ MessageId thật, ClientMsgId echo }` → client thay tin fake bằng tin thật. Đây là căn cứ để reviewer kiểm: BE **phải echo đúng** ClientMsgId nhận vào, không được tự sinh mới.

### `POST api/FBMessage/GetOneItem`
`GetOneItemByContact` — lấy 1 item hội thoại (phục vụ fallback/refresh khi signal item rỗng).

Service: `Facebook.Service/Generated/FBMessageService.cs` (`SendMessageByContactId` + `GetOneItemByContact`, ~+217 dòng).

## Endpoint admin mới — `GET api/FBFanpage/ReSubscribeAllFanpage`

`FBFanpageService.ReSubscribeAllFanpage()` (`FBFanpageService.cs:1209`):
- Re-subscribe webhook cho **MỌI đơn vị**.
- **Dedupe theo FanpageId** — mỗi PageId gọi FB **1 lần** (dù nhiều đơn vị dùng chung page).
- Chỉ page `IsSuDung && IsDangKyWebhook == true`.
- **CHỈ subscribe, KHÔNG update DB.**
- Trả summary: `TongPage` / `SoThanhCong` / `SoLoi` (`:1251-1254`).

> Đây là tool vận hành (khôi phục subscription hàng loạt), **không** đổi trạng thái DB → an toàn chạy lại nhiều lần (idempotent phía DB).

## Liên quan — Entity `FBCustomerAdId` (lưu ad_id contact)

Feature phụ đi kèm delta (nên tách doc riêng `features/facebook/ad-id-contact/`, tóm ở đây):

- Entity `FBCustomerAdId` (`Entity/FBCustomerAdId.cs`): `IdDonVi`, `IdChiNhanh`, `PageId (long?)`, `PsId [VARCHAR(255)]`, `AdId [VARCHAR(500)]`. Đã đăng ký DbSet trong `FacebookDataContext`.
- `CommonService.SaveFBCustomerAdId / GetFBCustomerAdId` — **write-through Redis**, key `Fb_FBCustomerAdId:{donvi}:{chinhanh}:{pageId}:{psId}` TTL **1440'**; upsert **unique theo (IdDonVi, IdChiNhanh, PsId)**, dùng `MarkDirty`.
- Webhook process lưu `ad_id` khi contact về (đường contact-received: `FBDataWebhookService` / `CommonService` / `ContactPrivateHelper`).

## Liên quan — Fix bắt số điện thoại FB (3-PASS) — **GHI THÀNH LESSON**

`FacebookHelper.GetPhoneInContent` (`FacebookHelper.cs:15`) viết lại thành **3 pass trên RAW**:
1. **PASS1** `patternPhoneNumberRaw` (`:88`) = `(?<![0-9.])(\+?[0-9]{8,15})(?![0-9])` — bắt số liền thuần 8–15 digit, **neo biên** `(?<![0-9.])...(?![0-9])` chống dính giá tiền / số nước ngoài.
2. **PASS2** `patternVNPhoneNumberRaw` (`:84`) — số VN liền có dấu chấm/gạch, neo `(?<![0-9])...(?![0-9])`.
3. **PASS3** fallback cũ (`ReplaceTextToNumber` + gộp digit).

- Chuẩn hóa `\+84(?=[0-9])` → `0` (`:27`) — chỉ khi `+84` đứng **ngay trước digit**, **KHÔNG đụng `+856` (Lào)**.
- `IsHavePhoneNumber` (`:339`) giờ = `!IsNullOrEmpty(GetPhoneInContent(message))` → **gate đồng nhất** với extractor (một nguồn bắt số duy nhất).

> **Bug bản cũ (lý do fix):** bản cũ `Cleanup` cả message làm **gộp digit lạc** → số quốc tế / giá tiền bị carve nhầm thành số ĐT sai. Fix: bắt **trên RAW**, neo biên, ưu tiên số liền trước khi logic VN carve.

## Known-issue / điểm cần soi khi review-trace

- `FacebookSocketNotifyController.Reload` **nuốt mọi exception → trả `Ok()`** (fire-and-forget). Đúng ý đồ (không làm chết caller), nhưng **lỗi realtime âm thầm** — nếu Redis/broker chết thì signal mất không log ở đây.
- Consumer cũng **nuốt exception** (`realtime lỗi không được làm chết consumer`) — cùng đặc tính "im lặng khi hỏng".
- **Gate 2 lần** (controller + consumer) là chủ ý chống bắn thừa khi user đóng modal giữa publish↔consume — không phải thừa; đừng "tối ưu" bỏ đi.
- Trace 3-pass phone: chuẩn bị case số **giá tiền** (vd `2.000.000`), **số Lào `+856...`**, **số VN có dấu chấm**, **số quốc tế `+8562077328239` / `02009094834`** để verify không carve nhầm và `+84` normalize không đụng `+856`.
- `SendMessageByContactId` **không lưu trữ** → verify không có nhánh nào vô tình persist; và **echo đúng** `ClientMsgId`.

## Việc cập nhật kho gợi ý (không tự ghi — cần user duyệt)

1. **TẠO** `registry/facebook/FacebookSocketNotifyController.md` (endpoint nội bộ cross-env, gate watching-set, ripple → `FacebookSocket.API` consumer + `FacebookWebhookProcess`).
2. **TẠO** `features/facebook/chat-lai-tu-contact/` (summarize + flow + dev — doc này là bản dev).
3. **TẠO** `features/facebook/ad-id-contact/` (FBCustomerAdId + write-through cache).
4. **CẬP NHẬT** `registry/facebook/FBMessageController.md` (+SendMessageByContactId, +GetOneItem) và `FBFanpageController.md` (+ReSubscribeAllFanpage).
5. **GHI LESSON** `projects/scrm/lessons/` — bắt số FB 3-pass (bug Cleanup gộp digit → fix RAW neo biên).
6. **CẬP NHẬT** `architecture.md` — thêm service `FacebookSocket.API`.
