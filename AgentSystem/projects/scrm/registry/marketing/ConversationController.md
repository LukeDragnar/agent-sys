---
controller: ConversationController
module: marketing
service: FacebookChat
man_hinh_ui: "Marketing > ChatHub (Quản lý hội thoại Facebook): khung chat inbox/comment — danh sách hội thoại bên trái, ô soạn tin/gửi ảnh, nút 'Tạo đơn'/'Gửi đơn hàng' từ box chat"
muc_dich: "Lo toàn bộ tương tác hội thoại Facebook (Messenger + comment): lấy/lọc danh sách hội thoại theo page, gửi tin nhắn & bình luận trả lời, upload media, crawl link hội thoại, và tạo/gửi đơn hàng FB ngay trong khung chat."
alias: ["hội thoại Facebook", "chat FB", "inbox FB", "ChatHub", "nhắn tin fanpage", "trả lời comment", "tạo đơn từ chat", "conversation"]
auth: mixed
state_machine: []
deprecated: false
entities: [Conversation, MessageChatBox, PageMessageChatBox, UserConversation, FBFanpage, FBCustomer]
stored_procs: []
depends:
  - "IConversationService (FacebookChat.Service.Generated) — toàn bộ logic"
  - "ICommonService — GetFbFanpageByFanPageId (cache fanpage), GetNhomQuanLy, SendSocketChat"
  - "ICommonCacheService — cache cơ cấu tổ chức, cache Conversation/UserConversation webhook"
  - "IRedisCacheChatService — set metadata khi bình luận"
  - "IPublishEndpointContactReceiveFB — bắn event phân công user trả lời lần đầu (RabbitMQ)"
  - "MongoDB (collection Conversation / MessageChatBox / PageMessageChatBox / UserConversation)"
  - "MessageHelper / FacebookHelper — gọi Graph API v21.0 (gửi tin, comment, lấy media, tạo đơn)"
  - "External: Contact service (SanPhamLanding), ConfigChiaSoChat service (CauHinhChiaChat), SocketChat service, OrderLogistic service (CauHinh)"
cross_module_in:
  - "ChiaSoChat / webhook receiver — gọi GetConversationChatByIdPageAndIdCustomer khi đẩy tin realtime"
  - "Extension crawl (trình duyệt) — gọi GetConversationLinkToCrawl / SaveCrawledData / GetCrawlDataConversation"
cross_module_out:
  - "Facebook Graph API (graph.facebook.com v21.0) — gửi tin/comment/đơn hàng, lấy media"
  - "ChiaSoChat service — EventPhanCongUserTraLoiLanDau qua RabbitMQ (chia chat cho nhân viên trả lời đầu)"
  - "SocketChat service — bắn socket fake message lên UI + socket link crawl"
  - "OrderLogistic service — đọc cấu hình IsGuiDonGiaSauVat khi gửi đơn hàng"
  - "Contact + ConfigChiaSoChat service — lấy landing + cấu hình chia chat để lọc quyền xem hội thoại"
---

# ConversationController — Quản lý hội thoại Facebook (ChatHub)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới cuộc hội thoại | (ngầm — khi mở chat) | GET `api/Conversation/ThemMoi?pageId&khachHangId` | `Conversation_FacebookChat_ThemMoi` | internal | Có pageId + khachHangId | Tạo/đảm bảo conversation cho cặp page↔khách. | Mongo `Conversation` | — |
| 2 | Lấy toàn bộ hội thoại theo page | (load danh sách chat) | POST `api/Conversation/getall` (body: `List<string>` pageIds) | `Conversation_FacebookChat_GetAll` | internal | Đã chọn page | Map id page → FanPageId, query Mongo theo IdDonVi+IdChiNhanh+ParentId, sort LastTime desc, **trang 1, 10 bản ghi** cố định. | Mongo `Conversation` | Paging hardcode page=1,size=10 (`ConversationService.cs:128`) — không nhận tham số trang. |
| 3 | Lọc hội thoại theo điều kiện (tag/sđt/đọc/từ khóa/thời gian) | Bộ lọc chat | POST `api/Conversation/LocTheoDieuKien` (`ConversationFilterDto`) | `Conversation_FacebookChat_LocTheoDieuKien` | internal | — | Lọc Mongo theo nhiều field; **phân quyền xem theo role**: admin chi nhánh/DV-TRUC-PAGE/SuperUser/CSKH thấy tất; LEAD/QL lọc theo nhóm quản lý; SALE/MKT thường chỉ thấy hội thoại được gán (UserConversation.SaleUserId). Gắn avatar khách từ `FBCustomer.Profile_Pic`. Trang 20/lần. | Mongo `Conversation` + `UserConversation`; EF `FBCustomer` | Lọc + paging làm **in-memory** (load hết rồi Skip/Take) → nặng khi nhiều hội thoại (`ConversationService.cs:294-344`). Khối SALE-only re-query Mongo lần 2 (`:335-336`). |
| 4 | Lấy hội thoại theo điều kiện nhanh (tab Chưa đọc/Có SĐT/Tin nhắn) | Tab lọc nhanh | POST `api/Conversation/GetTheoDieuKien?filterType` | `Conversation_FacebookChat_GetTheoDieuKien` | internal | — | Switch theo `ConversationFilterType` (IsNotRead/IsMessage/HasPhone/NotHasPhone), trả page=1 size=10. | Mongo `Conversation` | **Service tự ghi chú "bỏ hàm này"** (`ConversationService.cs:376-383`) → đang dùng nhưng deprecated theo intent dev. |
| 5 | Gửi tin nhắn (Messenger / private reply) | Gửi | POST `api/Conversation/SendMessage` (multipart `FBMessageInputDto_V1`) | `Conversation_FacebookChat_SendMessage` | internal | Đã chọn hội thoại (trừ private reply) | Tìm fanpage theo DonVi+ChiNhanh; nếu **không phải admin/trực page** và là lần trả lời đầu trên page có landing → publish event chia chat. Gửi text + từng file qua Graph API, **fake message lên socket ngay** (không chờ webhook). Private reply (có CommentId+PostId) thì không fake socket. | Graph API + Mongo + RabbitMQ + socket | `SaveMessageToDatabase` bị **comment** (`:904`) → tin gửi đi KHÔNG lưu Mongo tại đây, phụ thuộc webhook echo. Token test hardcode trong code chết (`ThemMoi`/`GetUserInfo` comment, `:42,:151`). |
| 6 | Gửi bình luận trả lời | Trả lời comment | POST `api/Conversation/SendMessagePost` (multipart `FBMessageCommentDto_V1`) | `Conversation_FacebookChat_SendMessagePost` | internal | Đã chọn hội thoại comment | Tương tự #5 nhưng gửi comment qua Graph API; check trả lời lần đầu (TypeConversation=BinhLuan); fake socket theo kiểu xem (private/anonymous/all). | Graph API + Mongo + RabbitMQ | `SaveMessageCommentToDatabase` cũng comment (`:508,:532`). |
| 7 | Upload/đồng bộ media tin nhắn | (tự động khi ảnh hết hạn) | POST `api/Conversation/UpdateFileMedia` (`UpdateFileMediaDto`) | `Conversation_FacebookChat_UpdateFileMedia` | internal | Có Mid hoặc CommentId | Nếu URL FB còn truy cập được → "không thay đổi"; nếu hết hạn → gọi Graph lấy lại attachment (ảnh/video/audio/file/sticker), update lại MessageChatBox trong Mongo. | Graph API + Mongo `MessageChatBox` | — |
| 8 | Tạo đơn hàng FB (Receipt template) | Tạo đơn | POST `api/Conversation/TaoDonHangFB` (`SendDonHangFBDto`) | (SBAuthorize rỗng — chỉ cần đăng nhập) | internal | Có PageId/KhachHangId/TienDonFB/sản phẩm | Build FB **Receipt template**: subtotal=Σ(đơn giá×SL), total=subtotal+ship+thuế−CK; gửi sang Graph API. | Graph API | FB silently normalize nếu lệch subtotal/total (comment cảnh báo `:1670-1671`). Address city/state ép = country (`:1718-1719`). |
| 9 | Gửi đơn hàng FB (Generic template + text) | Gửi đơn hàng | POST `api/Conversation/GuiDonHangFBGeneric` (`SendDonHangFBDto`) | (SBAuthorize rỗng) | internal | Như #8 | Đọc cấu hình `IsGuiDonGiaSauVat` từ OrderLogistic → tính đơn giá trước/sau VAT; gửi **Generic carousel ≤10 SP** + 1 text tóm tắt đơn qua Graph API. | Graph API + OrderLogistic | Lấy ≤10 sản phẩm đầu (`Take(10)` `:1778`). Lỗi đọc cấu hình → mặc định trước VAT (`:1822-1824`). |

## Endpoint nội bộ (private / service-to-service & crawl)
- `[internal]` `GetListIdNhomQL` (SBAuthorize rỗng) — trả danh sách nhóm user đang quản lý (từ cache cơ cấu tổ chức), dùng để lọc quyền xem hội thoại.
- `[internal]` `GetConversationChatByIdPageAndIdCustomer/{idPage}/{idCustomer}` (SBAuthorize rỗng) — webhook/realtime đẩy 1 hội thoại; **gác quyền 403** nếu user không thuộc nhóm/không được gán (qua UserConversation cache).
- `[crawl/extension]` `GetConversationLinkToCrawl?fanpageId&userScopedId` — lấy link hội thoại từ Graph API rồi bắn socket về extension (group `GetLinkToCrawl_{donvi}_{chinhanh}_{user}`).
- `[crawl/extension]` `SaveCrawledData` (`CrawledDataExtensionDto`) — extension đẩy dữ liệu crawl (Link/AdId/Workplace/Education/City/HomeTown/FbDtsg/IdKhachHangGlobal...) update vào Mongo `Conversation`.
- `[crawl/extension]` `GetCrawlDataConversation?conversationId` — đọc lại dữ liệu crawl đã lưu.
- `[anonymous]` `GetUserInfo?psId`, `GetSDTforMessage?message` — tiện ích crawl, **token Graph hardcode chết trong code** (`ConversationController.cs:151`).

## Quy tắc nghiệp vụ (phân quyền xem hội thoại — blueprint trace)
Thứ tự rẽ nhánh trong `FilterConversation` / `GetConversationChatByIdPageAndIdCustomer`:
1. **Admin chi nhánh OR `DV-TRUC-PAGE` OR `IsSuperUser` OR `IsCSKH`** → thấy TẤT CẢ hội thoại của page (không lọc UserConversation).
2. Ngược lại → cần map qua `UserConversation`. Mỗi page có `LuaChonCachXem` (từ cấu hình chia chat của landing): `1`=private, `2`=anonymous, `3`=all.
   - LEAD/QL (`DV-SALE-LEAD`/`DV-QL-SALE`/`DV-MKT-LEAD`/`DV-QL-MKT`): thấy hội thoại nhóm mình quản lý (private cần uc thuộc nhóm; anonymous cho cả uc null; all cho hết; hoặc uc.SaleUserId==mình).
   - SALE/MKT thường: chỉ private khi `uc.SaleUserId==mình`; anonymous khi uc null hoặc của mình; all cho hết.
   - Field cấu hình: `csc.CauHinhChat.LuaChonCachShowChat`, `ListUserShowAllChat` (user được phép xem tất bất kể role).

## Quan hệ
- **Gọi sang:** `IConversationService` → Graph API v21.0; chéo Contact (landing), ConfigChiaSoChat (cấu hình chia chat), SocketChat, OrderLogistic (cấu hình VAT).
- **Dữ liệu (entities):** Mongo `Conversation`, `MessageChatBox`, `PageMessageChatBox`, `UserConversation`; EF `FBFanpage`, `FBCustomer`, `FBPost`.
- **Ripple / đồng bộ:** publish `EventPhanCongUserTraLoiLanDau` qua RabbitMQ (chia chat lần đầu); bắn socket fake message + socket link crawl; gửi tin/đơn ra Facebook (side-effect ngoài hệ thống).

## Luồng xuyên module (gửi tin lần đầu → chia chat)
1. Nhân viên (không phải admin/trực page) gửi tin/comment đầu tiên trên hội thoại của page có `IdLanding` → service publish `EventPhanCongUserTraLoiLanDau` (kèm ConversationDto, IdLanding, TypeConversation).
2. ChiaSoChat service consume event → gán user trả lời đầu vào UserConversation → quyết định quyền xem các phiên sau.
3. Đồng thời fake message hiển thị ngay lên UI qua SocketChat (không chờ webhook FB echo về).

## Lỗi đã biết / lưu ý kỹ thuật
- `ConversationService.cs:128` — `GetAllConversationByLstPageId` paging cứng page=1 size=10.
- `ConversationService.cs:294-344` — `FilterConversation` load toàn bộ hội thoại rồi Skip/Take in-memory → rủi ro hiệu năng; có khối re-query Mongo lần 2 cho nhánh SALE.
- `ConversationService.cs:376-383` — `GetConversationWithFilterByLstPageId` được dev đánh dấu "bỏ hàm này" nhưng controller vẫn expose endpoint `GetTheoDieuKien`.
- `ConversationService.cs:904` / `:508` / `:532` — `SaveMessageToDatabase` / `SaveMessageCommentToDatabase` bị comment: tin gửi đi không persist tại điểm gửi, dựa vào webhook echo → nếu webhook trễ/mất thì lệch dữ liệu.
- `ConversationController.cs:42,:151` — access token Graph API hardcode chết trong code (ThemMoi comment + GetUserInfo) → rủi ro lộ token, không dùng được khi token đổi.
- `ConversationService.cs:371` — `FilterConversation` có `catch` rồi `throw new Exception(ex.ToString())` (nuốt stacktrace gốc) kèm biến rác `var a = 2314`.
- `TaoDonFB`/`GuiDonHangFBGeneric` dùng SBAuthorize rỗng → chỉ cần đăng nhập (không gate permission key riêng) trong khi các hành động chat khác có key cụ thể.
