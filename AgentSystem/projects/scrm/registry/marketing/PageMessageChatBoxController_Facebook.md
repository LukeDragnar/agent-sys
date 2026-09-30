---
controller: PageMessageChatBoxController
module: marketing
service: Facebook
man_hinh_ui: "ChatHub > khung chat 1 hội thoại (lazy-load trang tin nhắn theo Stt khi cuộn, lấy chi tiết 1/nhiều message-box từ Mongo)"
muc_dich: "Đọc tin nhắn của hội thoại từ Mongo (collection PageMessageChatBox/MessageChatBox): lazy-load trang gần Stt nhất khi cuộn, lấy chi tiết message theo list Id / theo Id / theo MsId, và gắn tag vào message-box (hiện đang là stub)."
alias: ["lazy load tin nhắn", "chi tiết message", "khung chat", "PageMessageChatBox", "gắn tag hội thoại"]
auth: mixed
state_machine: []
deprecated: false
entities: [PageMessageChatBox, MessageChatBox, FBTagChat]
stored_procs: []
depends:
  - "IPageMessageChatBoxService (Facebook.Service.Generated)"
  - "IMongoDbClient — đọc PageMessageChatBox / MessageChatBox"
  - "IRepository<FBTagChat> — (chỉ inject, AttachTag đang stub)"
cross_module_in:
  - "ChatHub UI — cuộn/đọc tin nhắn hội thoại"
cross_module_out: []
---

# PageMessageChatBoxController (Facebook) — Đọc tin nhắn hội thoại (Mongo)

> Controller đọc Mongo cho khung chat. Không có `[TienIch]`. 4 endpoint GET/POST không có `[SBAuthorize]` (anonymous), riêng `GanTag` có `[SBAuthorize()]` (đăng nhập) → auth controller-level: **mixed**.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lazy-load trang tin nhắn theo Stt | (cuộn khung chat) | GET `api/PageMessageChatBox/GetMaxPageMessage?conversationId&Stt` | (không attribute) | **anonymous** | Đang xem 1 hội thoại | `GetLazyLoad`: lấy tất cả PageMessageChatBox theo `ConversationId`, chọn page có `Stt` GẦN giá trị truyền vào nhất (`OrderBy Abs(Stt - Stt)`). | Mongo (PageMessageChatBox) | **Không auth** → ai có conversationId đều đọc được tin nhắn. |
| 2 | Chi tiết nhiều message-box | (load batch) | POST `api/PageMessageChatBox/GetDetailMessage` | (không attribute) | **anonymous** | — | `GetDetailMessage`: filter `Id In messageBoxId`, trả list MessageChatBox. | Mongo (MessageChatBox) | Không auth. |
| 3 | Chi tiết 1 message theo Id | — | GET `api/PageMessageChatBox/GetMessageById?id` | (không attribute) | **anonymous** | — | `GetDetailMessageById`: FindOne theo `Id == id`. | Mongo | Không auth. |
| 4 | Chi tiết 1 message theo MsId | — | GET `api/PageMessageChatBox/GetDetailMessageByMsId?msId` | (không attribute) | **anonymous** | — | `GetDetailMessageByMsId`: FindOne theo `MsId == msId`. | Mongo | Không auth. |
| 5 | Gắn tag vào message-box | Gắn tag | POST `api/PageMessageChatBox/GanTag?messageChatBoxId` (body: List\<Guid\> lstTagIds) | (key rỗng — `[SBAuthorize()]`) | public | Có chọn tag | **STUB**: `AttachTag` trả cứng `ResultWithData(123)`; toàn bộ logic update tag vào Conversation đã bị comment-out (PageMessageChatBoxService.cs:67-74). | (không tác dụng) | Endpoint không thực sự gắn tag — luôn trả 123, không ghi DB/Mongo. |

## Quan hệ
- **Gọi sang:** Mongo (`IMongoDbClient`).
- **Dữ liệu (entities):** `PageMessageChatBox` (page tin nhắn theo Stt), `MessageChatBox` (1 message), `FBTagChat` (chỉ inject, chưa dùng).
- **Ripple / đồng bộ:** không (chỉ đọc; GanTag là stub).

## Lỗi đã biết / lưu ý kỹ thuật
- `PageMessageChatBoxService.cs:67-74` — `AttachTag` là stub: `return ResultWithData(123)`, logic thật comment-out → nút Gắn tag không hoạt động.
- 4 endpoint đọc tin nhắn (GetMaxPageMessage/GetDetailMessage/GetMessageById/GetDetailMessageByMsId) **không có `[SBAuthorize]`** → anonymous, không lọc đơn vị/chi nhánh → rủi ro lộ nội dung chat nếu biết conversationId/Id.
- `GetPageSttMax` có trong interface nhưng không expose qua endpoint.
