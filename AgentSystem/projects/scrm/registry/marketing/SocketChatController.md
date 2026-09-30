---
controller: SocketChatController
module: marketing
service: FacebookChat
man_hinh_ui: "nội bộ — không có UI; gateway SignalR realtime cho màn hình chat Facebook (đẩy tin nhắn xuống client theo group)"
muc_dich: "Nhận lệnh push realtime từ các service chat (qua HTTP private) rồi phát message tới client SignalR theo nhóm (group) — cầu nối giữa backend chat và web/app realtime."
alias: ["SocketChat", "socket chat", "realtime chat FB", "SignalR chat", "push tin nhắn chat"]
auth: internal
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: ["SignalR IHubContext<ChatHub>", "SocketRequestDto (GroupName + Message)"]
cross_module_in: ["FacebookChat services (CommonService.SendSocketChat, DongBoChatFBService, FBWebhookService) → POST SocketChatSB", "ChiaSoChat / FacebookReceive → đẩy socket khi có tin/chia chat"]
cross_module_out: ["ChatHub (SignalR) → client web/app trong các group (RECEIVE_MESSAGE / SendMesageCrawlLink)"]
---

# SocketChatController — Gateway push realtime chat (SignalR)

Controller **service-to-service** (cả 2 endpoint `[PrivateSBAuthorize]`), không có UI và không chạm DB. Chỉ làm 1 việc: nhận `SocketRequestDto` (danh sách `GroupName` + `Message`) rồi bơm qua `IHubContext<ChatHub>` tới từng group SignalR. Các service chat build danh sách group (theo fanpage/chi nhánh/user/nhóm nhân viên) rồi gọi `urlSocket = {SocketChatServiceUrl}/api/SocketChat/SocketChatSB`.

## Endpoint nội bộ (private / service-to-service)
- `[PrivateSBAuthorize]` **POST `api/SocketChat/SocketChatSB`** — body `SocketRequestDto`. Lặp từng `GroupName` → `_chatHub.Clients.Group(group).SendAsync("RECEIVE_MESSAGE", Message)`. Trả `Ok("Thành công")`. **Caller chính:** `CommonService.SendSocketChat`, `DongBoChatFBService`, `FBWebhookService` (FacebookChat); dùng để đẩy tin nhắn/cập nhật hội thoại realtime tới client đang mở màn chat.
- `[PrivateSBAuthorize]` **POST `api/SocketChat/SendMesageCrawlLink`** — body `SocketRequestDto`. Lặp từng group → `SendAsync("SendMesageCrawlLink", Message)`. Dùng cho luồng crawl link (đẩy kết quả/tiến trình crawl xuống client). Trả `Ok("Thành công")`.

## Quan hệ
- **Gọi sang:** SignalR `ChatHub` (push tới client, không DB).
- **Dữ liệu (entities):** không có — chỉ trung chuyển `SocketRequestDto.Message` (object) tới group.
- **Ripple / đồng bộ:** event SignalR `RECEIVE_MESSAGE` / `SendMesageCrawlLink` tới mọi client đang join group tương ứng (group sinh theo fanpage/chi nhánh/user/nhóm nhân viên).

## Luồng xuyên module (tin nhắn FB → client realtime)
1. Service chat (FacebookChat/FacebookReceive/ChiaSoChat) có tin nhắn/sự kiện mới → build `SocketChatHubPre_Payload`, xác định danh sách group đích (Admin chi nhánh, user sale, nhóm nhân viên, group chung).
2. Gọi HTTP private POST `SocketChatSB` (qua `SocketChatServiceUrl`) với `{ GroupName[], Message }`.
3. `SocketChatController` fan-out `SendAsync("RECEIVE_MESSAGE")` tới từng group → client web/app đang join group nhận realtime, cập nhật khung chat.

## Lỗi đã biết / lưu ý kỹ thuật
- Cả 2 endpoint chỉ `[PrivateSBAuthorize]` (shared secret) — không gọi trực tiếp từ FE; FE nhận qua kết nối SignalR (join group), không qua HTTP controller này.
- Không validate `socketRequestDto.GroupName == null` (`SocketChatController.cs:22,32`) → payload thiếu GroupName sẽ NullReference khi foreach.
- Event name thứ 2 viết là `SendMesageCrawlLink` (thiếu chữ 's' trong "Message") — FE phải subscribe đúng tên sai chính tả này.
