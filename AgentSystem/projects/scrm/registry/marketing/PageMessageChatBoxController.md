---
controller: PageMessageChatBoxController
module: marketing
service: FacebookChat
man_hinh_ui: "Marketing > Chat (ChatHub) > khung hội thoại Facebook — cuộn/lazy-load tin nhắn cũ, mở chi tiết tin, gán tag cho hội thoại"
muc_dich: "Truy vấn tin nhắn của 1 hội thoại Facebook chatbox (lazy-load theo trang, lấy chi tiết theo id/msId) và gán tag cho khung tin nhắn."
alias: ["chat box", "tin nhắn page", "lazy load tin nhắn", "gán tag hội thoại", "PageMessage", "ChatHub message"]
auth: mixed
state_machine: []
deprecated: false
entities: [PageMessageChatBox, MessageChatBox, FBTagChat]
stored_procs: []
depends:
  - "IPageMessageChatBoxService (FacebookChat.Service.Generated.PageMessageChatBoxService)"
  - "MongoDB (tin nhắn chat lưu Mongo — using MongoDB.Bson)"
  - "Core/TienIch (MaTienIchSCRM.TI_ChatHub — đơn vị phải mua tiện ích ChatHub)"
cross_module_in:
  - "FE ChatHub (màn chat Facebook) — gọi lazy-load + chi tiết khi user cuộn/mở hội thoại"
cross_module_out:
  - "FBTagChats — tag gán vào hội thoại lấy từ kho tag chat (cùng service Facebook)"
---

# PageMessageChatBoxController — Tin nhắn chatbox Facebook (lazy-load + gán tag)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lazy-load trang tin nhắn theo hội thoại | (auto khi cuộn lên) | GET `api/PageMessageChatBox/GetMaxPageMessage?conversationId&Stt` | — | anonymous | Khi user cuộn khung chat lên trên | `GetLazyLoad(conversationId, Stt)` → trả trang tin nhắn cũ kế tiếp tính từ `Stt` (số thứ tự) của hội thoại. | Mongo (PageMessageChatBox) | `[AllowAnonymous]` — không kiểm token; chỉ chặn bằng TienIch ChatHub ở controller-level. |
| 2 | Lấy chi tiết nhiều tin theo box id | (mở/hiển thị nội dung tin) | POST `api/PageMessageChatBox/GetDetailMessage` (body `List<string> messageBoxId`) | — | anonymous | Khi cần render nội dung 1 nhóm tin | `GetDetailMessage(messageBoxId)` → danh sách chi tiết tin theo các id box. | Mongo | `[AllowAnonymous]`. |
| 3 | Lấy chi tiết tin theo id / msId | (mở 1 tin) | GET `api/PageMessageChatBox/GetMessageById?id` · GET `api/PageMessageChatBox/GetDetailMessageByMsId?msId` | — | anonymous | Khi mở 1 tin cụ thể (id nội bộ hoặc messageId FB) | `GetDetailMessageById(id)` / `GetDetailMessageByMsId(msId)` → chi tiết 1 tin. | Mongo | `[AllowAnonymous]`. |
| 4 | Gán tag cho khung tin nhắn | Gán tag | POST `api/PageMessageChatBox/GanTag?messageChatBoxId` (body `List<Guid> lstTagIds`) | (SBAuthorize không key — chỉ yêu cầu đăng nhập) | mixed | User đã đăng nhập, đang xem 1 hội thoại | `AttachTag(lstTagIds, messageChatBoxId, CurrentUser)` → gắn danh sách tag vào khung tin nhắn, ghi người gán = CurrentUser. | Mongo + FBTagChat | `[SBAuthorize()]` rỗng → chỉ chặn theo đăng nhập, không theo permission key/role (dòng role mẫu bị comment `:55`). |

## Quy tắc nghiệp vụ (gating)
- **Tiện ích gate:** `[TienIch("PageMessageChatBoxController", "Quản lý tin nhắn chatbox", MaTienIchSCRM.TI_ChatHub)]` — đơn vị chưa mua ChatHub không vào được.
- **Auth hỗn hợp:** 3 endpoint đọc tin (`GetMaxPageMessage`, `GetDetailMessage`, `GetMessageById`, `GetDetailMessageByMsId`) đều `[AllowAnonymous]`; chỉ `GanTag` mới yêu cầu đăng nhập → controller-level auth = `mixed`.

## Quan hệ
- **Gọi sang:** `IPageMessageChatBoxService` (lazy-load, get-detail, attach-tag) — đọc/ghi tin chat trong MongoDB.
- **Dữ liệu (entities):** tin nhắn chatbox (PageMessageChatBox/MessageChatBox) + tag (FBTagChat).
- **Ripple / đồng bộ:** tag gán ở đây lấy từ kho tag của `FBTagChatsController` (cùng service Facebook); không phát event cross-service.

## Lỗi đã biết / lưu ý kỹ thuật
- `PageMessageChatBoxController.cs:23-54` — 4 endpoint đọc tin đều `[AllowAnonymous]`: ai biết `conversationId`/`id`/`msId` đều đọc được tin → rủi ro lộ nội dung chat nếu id đoán được.
- `PageMessageChatBoxController.cs:55-56` — `GanTag` dùng `[SBAuthorize()]` rỗng (dòng role mẫu bị comment) → không giới hạn role; bất kỳ user đăng nhập nào cũng gán tag được.
