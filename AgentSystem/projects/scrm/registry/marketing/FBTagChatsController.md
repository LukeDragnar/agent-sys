---
controller: FBTagChatsController
module: marketing
service: FacebookChat
man_hinh_ui: "ChatHub > Cấu hình > Tag hội thoại; và panel tag trong màn chat (gắn/bỏ tag cho 1 cuộc trò chuyện)"
muc_dich: "CRUD tag chat (nhãn màu phân loại hội thoại) theo đơn vị/chi nhánh; và gắn/bỏ tag cho một Conversation (lưu trên MongoDB) kèm clear cache + bắn socket realtime."
alias: ["tag chat", "tag hội thoại", "gắn tag", "bỏ tag", "nhãn hội thoại", "FBTagChat"]
auth: internal
state_machine: []
deprecated: false
entities: [FBTagChat, Conversation]
stored_procs: []
depends:
  - "IFBTagChatService (FBTagChatService)"
  - "IMongoDbClient (collection 'Conversation' — đọc/ghi TagChats)"
  - "ICommonCacheService (ClearCache_Conversation_Chat_Webhook / ClearCache_Conversation_Comment_Webhook)"
  - "ICommonService.SendSocketChat (đẩy realtime UpdateTagConversation)"
  - "Core/TienIch (MaTienIchSCRM.TI_ChatHub)"
cross_module_in: []
cross_module_out:
  - "MongoDB collection 'Conversation' — gắn/bỏ tag ghi vào field TagChats của hội thoại"
  - "Socket ChatHub (SendSocketChat, SocketFBChatType.UpdateTagConversation) — đồng bộ tag realtime tới các client đang mở hội thoại"
  - "Cache webhook hội thoại (chat + comment) bị clear sau mỗi lần gắn/bỏ tag"
---

# FBTagChatsController — Quản lý tag hội thoại (ChatHub)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy tất cả tag | Danh sách tag | GET `api/FBTagChats/LayTatCa` | `FBTagChats_FacebookChat_LayTatCa` | internal | Có TI_ChatHub | Trả list tag lọc `IdDonVi==DonViId && IdChiNhanh==IdChiNhanh` (`GetByDonViAsync`). | EF `FBTagChat` | — |
| 2 | Tìm theo Id | (chi tiết tag) | GET `api/FBTagChats/TimTheoId/{id:Guid}` | `FBTagChats_FacebookChat_TimTheoId` | internal | Có TI_ChatHub | Trả 1 tag theo Id (`GetByIdAsync`). | EF `FBTagChat` | `GetByIdAsync` không filter đơn vị/chi nhánh → rò rỉ ngang tenant nếu đoán Guid. |
| 3 | Thêm mới | Thêm tag | POST `api/FBTagChats/ThemMoi` | `FBTagChats_FacebookChat_ThemMoi` | internal | Có TI_ChatHub | Set `IdDonVi/IdChiNhanh` từ CurrentUser → validate → map DTO → `InsertAsync`. | EF `FBTagChat` | — |
| 4 | Cập nhật | Lưu | POST `api/FBTagChats/CapNhatThongTin` | `FBTagChats_FacebookChat_CapNhatThongTin` | internal | Có TI_ChatHub | Validate → check tồn tại (NotFound nếu null) → `UpdateAsync` mark dirty `Color, Description`. | EF `FBTagChat` | Update chỉ lưu `Color, Description`; field khác bỏ. Không set lại đơn vị/chi nhánh. |
| 5 | Xóa | Xóa | POST `api/FBTagChats/Xoa/{id:Guid}` | `FBTagChats_FacebookChat_Xoa` | internal | Có TI_ChatHub | Check tồn tại → `DeleteAsync(id)` → trả true. | EF `FBTagChat` | Xóa cứng tag nhưng KHÔNG gỡ tagId này khỏi field `TagChats` của các Conversation đã gán → mồ côi tag trên hội thoại cũ. |
| 6 | Lấy tag của 1 hội thoại | (panel tag trong chat) | GET `api/FBTagChats/GetAllTagForConversation?conversationId=` | `FBTagChats_FacebookChat_GetAllTagForConversation` | internal | Có TI_ChatHub; đang mở 1 hội thoại | Đọc Conversation từ Mongo → trả `TagChats` (list tagId, rỗng nếu chưa có). NotFound nếu không thấy hội thoại. | Mongo `Conversation` | — |
| 7 | Gắn tag vào hội thoại | Gắn tag | POST `api/FBTagChats/GanTag?tagId=&conversationId=` | `FBTagChats_FacebookChat_GanTag` | internal | Có TI_ChatHub | Load Conversation; nếu đã có tagId → báo lỗi "đã gán rồi"; verify tag tồn tại trong DB; add tagId vào `TagChats`; `ReplaceOne` Mongo; clear cache chat (+ comment nếu có ConversationId); bắn socket `UpdateTagConversation`. | Mongo `Conversation` + EF `FBTagChat` (verify) | — |
| 8 | Bỏ tag khỏi hội thoại | Bỏ tag | POST `api/FBTagChats/BoTag?tagId=&conversationId=` | `FBTagChats_FacebookChat_BoTag` | internal | Có TI_ChatHub; tag đã được gán | Load Conversation; nếu `TagChats` chứa tagId → remove, `ReplaceOne`, clear cache, bắn socket; ngược lại báo "chưa gán". | Mongo `Conversation` | `BoTagChatChoConversation` không null-check `conversation` trước khi đọc `TagChats` (`FBTagChatService.cs:127-128`) → NullRef nếu conversationId sai. Điều kiện `if (checkTag != null)` sai logic (`checkTag` là bool). |

## Quy tắc nghiệp vụ (gating / luồng gắn-bỏ tag)
- **Tiện ích gate:** `[TienIch("FBTagChatsController", "Quản lý tag tin nhắn", MaTienIchSCRM.TI_ChatHub)]`.
- **Phân quyền:** mở cho cả MKT + SALE + TRUC-PAGE (giống FBQuickReply).
- **Gắn tag (GanTag):** thứ tự kiểm tra → (1) hội thoại tồn tại? không thì lỗi; (2) khởi tạo `TagChats=[]` nếu null; (3) tag đã gán? → chặn trùng; (4) tag tồn tại trong DB? → không thì lỗi; (5) add + persist + clear cache + socket.
- **Bỏ tag (BoTag):** (1) load hội thoại; (2) `TagChats` chứa tagId? → remove + persist + clear cache + socket; ngược lại báo chưa gán.

## Quan hệ
- **Gọi sang:** `IFBTagChatService` → MongoDB (`Conversation`), `ICommonCacheService` (clear cache webhook), `ICommonService.SendSocketChat`.
- **Dữ liệu (entities):** `FBTagChat` (Color, Description, IdDonVi, IdChiNhanh) trên SQL; danh sách tagId nằm ở field `TagChats` của `Conversation` trên MongoDB.
- **Ripple / đồng bộ:** mỗi lần gắn/bỏ tag → ghi Mongo Conversation + clear 2 loại cache webhook (chat, comment) + đẩy socket `UpdateTagConversation` để các client đang xem hội thoại cập nhật ngay.

## Luồng xuyên module (gắn tag realtime)
1. User bấm **Gắn tag** trên 1 hội thoại → POST `GanTag(tagId, conversationId)`.
2. Service verify tag (SQL) + cập nhật `TagChats` của Conversation (Mongo).
3. Clear cache `Conversation_Chat_Webhook` (+ `Conversation_Comment_Webhook` nếu có ConversationId).
4. `SendSocketChat(UpdateTagConversation)` → mọi client đang mở hội thoại thấy tag mới ngay.

## Lỗi đã biết / lưu ý kỹ thuật
- `FBTagChatService.cs:127-129` — `BoTagChatChoConversation`: không null-check `conversation` → đọc `conversation.TagChats` có thể NullRef; `if (checkTag != null)` so sánh bool với null (luôn true) → logic kiểm tra sai, đúng phải `if (checkTag)`.
- `FBTagChatsController.cs:78-85` (`Xoa`) — xóa tag không dọn tagId khỏi `TagChats` của các Conversation đã gán → tag mồ côi.
- `FBTagChatService.cs:34` — `GetByIdAsync` (base) không lọc đơn vị/chi nhánh → `TimTheoId` rò rỉ ngang tenant.
- `FBTagChatService.cs:57-63` — `UpdateAsync` chỉ mark dirty `Color, Description`.
- `IHubContext<ChatHub>` bị comment trong ctor (`FBTagChatService.cs:36,43,50`); realtime đi qua `ICommonService.SendSocketChat` thay vì hub trực tiếp.
