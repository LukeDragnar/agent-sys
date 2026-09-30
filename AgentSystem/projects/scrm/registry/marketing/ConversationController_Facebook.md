---
controller: ConversationController
module: marketing
service: Facebook
man_hinh_ui: "ChatHub > Hội thoại (danh sách hội thoại theo page; lọc theo tag/đọc-chưa đọc/có SĐT/khoảng thời gian/keyword; gửi tin nhắn + file; tạo & gửi đơn hàng sang FB; crawl link hội thoại qua extension)"
muc_dich: "Trung tâm hội thoại FB ở service Facebook: lấy/lọc danh sách hội thoại (Mongo) theo list page, gửi tin nhắn text/file sang Graph (trực tiếp hoặc qua RabbitMQ), làm tươi media tin nhắn, lấy/lưu dữ liệu crawl từ extension, và tạo/gửi đơn hàng sang Facebook (Receipt Template + Generic Template)."
alias: ["hội thoại facebook", "danh sách chat", "gửi tin nhắn FB", "tạo đơn hàng FB", "lọc hội thoại", "crawl hội thoại", "Conversation"]
auth: mixed
state_machine: []
deprecated: false
entities: [Conversation, FBFanpage, FBConversationInfo, MessageChatBox, FBPost]
stored_procs: []
depends:
  - "IConversationService (Facebook.Service.Generated)"
  - "ISendMessageRabbitService — gửi tin nhắn qua RabbitMQ (SendMessagePost)"
  - "IMongoDbClient — Conversation / MessageChatBox / FBPostAttachmentMG"
  - "ICommonService.GetFbFanpageByFanPageId — fanpage từ cache"
  - "MessageHelper / FacebookHelper (Graph: SendMessageFB/SendMessageFBFileData/TaoDonSangFB/GuiGenericSangFB/GuiTextSangFB/GetLinkConversation/GetMessageFB*...)"
  - "IClientHelper → Core (CauHinhCore) + OrderLogistic (CauHinhOrder: IsGuiDonGiaSauVat) + SocketChat (gửi link crawl)"
cross_module_in:
  - "ChatHub UI + Browser extension (crawl) — đọc/gửi hội thoại, tạo đơn"
cross_module_out:
  - "Facebook Graph API — gửi tin nhắn/file, gửi đơn hàng (Receipt/Generic Template)"
  - "RabbitMQ — SendMessagePost (gửi tin nhắn bất đồng bộ)"
  - "SocketChat service — đẩy link crawl về extension qua group socket"
  - "OrderLogistic service — đọc cấu hình IsGuiDonGiaSauVat khi gửi đơn"
  - "Mongo (Conversation/MessageChatBox/FBPostAttachmentMG)"
---

# ConversationController (Facebook) — Hội thoại + gửi tin nhắn + tạo đơn FB

> Controller lớn của ChatHub. Không có `[TienIch]`. Auth hỗn hợp: nhiều endpoint có `[SBAuthorize]` (key rỗng hoặc trần), vài endpoint gửi tin/media KHÔNG có attribute (anonymous). Có hardcode page-token thật trong code debug đã comment (cần lưu ý bảo mật).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo hội thoại mới | (mở chat với khách) | GET `api/Conversation/ThemMoi?pageId&khachHangId` | (key rỗng — 6 role) | public | — | **CHƯA IMPLEMENT**: `ThemMoi` throw `NotImplementedException` (ConversationService.cs:709-712). | — | Endpoint chết — gọi sẽ 500. |
| 2 | Lấy tất cả hội thoại theo list page | (load danh sách) | POST `api/Conversation/getall` (body List\<string\> page Ids) | (key rỗng — 6 role) | public | — | Map IdFanpage(GUID)→FanPageId; filter Mongo theo đơn vị+chi nhánh+ParentId In pageIds; sort LastTime desc; trang 1, size 10. | Mongo (Conversation) | Phân trang cứng 1/10. |
| 3 | Lấy hội thoại theo bộ lọc nhanh | (tab Chưa đọc / Tin nhắn / Có SĐT...) | POST `api/Conversation/GetTheoDieuKien` (filterType) | (không attribute) | **anonymous** | — | `GetConversationWithFilterByLstPageId`: filter cơ bản + switch theo `ConversationFilterType` (IsNotRead/IsMessage/HasPhone/NotHasPhone). | Mongo | Không auth. Phân trang cứng 1/10. |
| 4 | Lọc hội thoại nâng cao | Bộ lọc | POST `api/Conversation/LocTheoDieuKien` (ConversationFilterDto) | (key rỗng — 6 role) | public | — | `FilterConversation`: build filter Mongo động theo TagIds/IsMessage/IsComment/HasPhone/IsRead/khoảng LastTime/keyword (regex tên + SearchPhone). Sort LastTime desc, page size 8. | Mongo | — |
| 5 | Gửi tin nhắn (text + file) | Gửi | POST `api/Conversation/SendMessage` (form-data) | (không attribute) | **anonymous** | Fanpage tồn tại | `CreateMessing`: tìm fanpage theo PageId; gửi text (`SendMessageFB`) và từng file (`SendMessageFBFileData`) qua Graph; lưu MessageChatBox (IsFromApp=true) cho mỗi đơn vị/chi nhánh của page. | EF (FBFanpage) + Graph + Mongo | Không auth. UserLogin lấy từ DTO (`fBMessageInputDto.UserLogin`) chứ không từ token → có thể giả mạo. |
| 6 | Gửi tin nhắn qua hàng đợi | Gửi (async) | POST `api/Conversation/SendMessagePost` (form-data) | (không attribute) | **anonymous** | — | `ISendMessageRabbitService.SendMessage` — đẩy tin nhắn qua RabbitMQ (xử lý bất đồng bộ). | RabbitMQ | Không auth. |
| 7 | Làm tươi media tin nhắn/bài post | (ngầm khi render) | POST `api/Conversation/UpdateFileMedia` (UpdateFileMediaDto) | (không attribute) | **anonymous** | URL media hết hạn | `UpdateUrlMedia`: check URL còn sống; hỏng → theo Mid (message) / CommentId / PostId, gọi Graph lấy lại media, ReplaceOne Mongo. | Mongo + Graph | Không auth. `infoPost.Attachments.Data[0]` không guard rỗng (ConversationService.cs:541) → NRE. |
| 8 | Lấy info user FB (debug) | — | GET `api/Conversation/GetUserInfo?psId` | (không attribute) | **anonymous** | — | Gọi `MessageHelper.GetUserInfo` với **page-token HARDCODE thật** trong code (ConversationService.cs:89). | Graph | **Bảo mật**: hardcode access_token thật + anonymous → endpoint debug lộ token, nên gỡ. |
| 9 | Trích số ĐT từ message | — | GET `api/Conversation/GetSDTforMessage?message` | (không attribute) | **anonymous** | — | `FacebookHelper.ExtractAndNormalizePhoneNumber(message)`. | (util thuần) | Không auth. |
| 10 | Lấy link hội thoại để crawl | (extension) | GET `api/Conversation/GetConversationLinkToCrawl?fanpageId&userScopedId` | (key rỗng — `[SBAuthorize()]`) | public | Fanpage thuộc đơn vị | Lấy fanpage theo FanPageId+đơn vị+chi nhánh; `GetLinkConversation` (Graph); đẩy link qua SocketChat group `GetLinkToCrawl_{donVi}_{chiNhanh}_{user}`; trả link. | EF + Graph + SocketChat | `conversationInfo.Data[0]` không guard rỗng (ConversationService.cs:726). |
| 11 | Lưu dữ liệu crawl từ extension | (extension) | POST `api/Conversation/SaveCrawledData` (CrawledDataExtensionDto) | (key rỗng — `[SBAuthorize()]`) | public | Hội thoại tồn tại | `SaveCrawledData`: tìm FBConversationInfo theo ConversationId (null → lỗi); cập nhật Link/AdId/Workplace/Education/CurrentCity/HomeTown/FbDtsg/IdKhachHangGlobal/IdUserGlobal + audit, MarkDirty từng field. | EF (FBConversationInfo) | — |
| 12 | Lấy dữ liệu crawl hội thoại | (extension) | GET `api/Conversation/GetCrawlDataConversation?conversationId` | (key rỗng — `[SBAuthorize()]`) | public | Hội thoại tồn tại | Trả CrawledDataExtensionDto (Link/AdId/Workplace/Education/CurrentCity/HomeTown/IdKhachHangGlobal). | EF | — |
| 13 | Tạo & gửi đơn hàng sang FB | Tạo đơn FB | POST `api/Conversation/TaoDonHangFB` (SendDonHangFBDto) | (key rỗng — `[SBAuthorize]`) | public | Có PageId/KhachHangId/TienDonFB/SanPhamDonFBs | `TaoDonFB`: validate đầu vào; dựng FB Receipt Template (subtotal=Σ giá×SL, total_cost=subtotal+ship+tax−CK, adjustments=CK, elements, address); gọi Graph `TaoDonSangFB`. | EF (cache fanpage) + Graph | FB silently normalize nếu subtotal/total lệch (lưu ý ở comment code:869). |
| 14 | Gửi đơn hàng FB (Generic) | Gửi đơn (generic) | POST `api/Conversation/GuiDonHangFBGeneric` (SendDonHangFBDto) | (key rỗng — `[SBAuthorize]`) | public | Như trên | `GuiDonHangFBGeneric`: đọc cấu hình `IsGuiDonGiaSauVat` từ OrderLogistic; dựng Generic Template (tối đa 10 sản phẩm, đơn giá tính theo VAT) + gửi kèm text tóm tắt đơn; gọi Graph `GuiGenericSangFB` + `GuiTextSangFB`. | EF + Graph + OrderLogistic | — |

## Quy tắc nghiệp vụ (tạo đơn sang FB)
- **Receipt Template (TaoDonFB):** `Subtotal = Σ(DonGia × SoLuong)`; `TotalCost = Subtotal + TongTienVanChuyen + TongTienThue − TongTienCK`; nếu `TongThanhToan == 0` → ghi "Đơn hàng đã thanh toán", ngược lại "thanh toán khi nhận hàng". Address mặc định Country="VN", City/State=Country, PostalCode="000000". **Cảnh báo FB**: nếu subtotal/total lệch công thức, FB silently normalize elements về quantity:1/price:0.
- **Generic Template (GuiDonHangFBGeneric):** lấy tối đa 10 sản phẩm; đơn giá = trước VAT, nếu `IsGuiDonGiaSauVat` (cấu hình OrderLogistic của đơn vị) = true thì = DonGia + DonGia×ValueVat/100; sau khi gửi generic còn gửi thêm 1 text tóm tắt đơn (mã đơn/khách/tổng tiền/CK/ship/tổng thanh toán/địa chỉ).

## Quan hệ
- **Gọi sang:** Graph (gửi tin/file/đơn, lấy link/message), RabbitMQ (SendMessagePost), SocketChat (đẩy link crawl), Core (CauHinhCore), OrderLogistic (CauHinhOrder).
- **Dữ liệu (entities):** `Conversation` (Mongo, chính), `MessageChatBox` (Mongo, message gửi đi), `FBConversationInfo` (EF, dữ liệu crawl), `FBFanpage` (token), `FBPost` (media post).
- **Ripple / đồng bộ:** gửi tin/đơn → GHI sang Facebook + lưu MessageChatBox; lấy link crawl → đẩy socket sang extension; crawl data lưu vào FBConversationInfo.

## Lỗi đã biết / lưu ý kỹ thuật
- `ConversationService.cs:89` (`GetUserInfo`) & `:33` (comment ThemMoi) — **hardcode page access_token thật** trong source; endpoint `GetUserInfo` lại anonymous → rò rỉ token, cần gỡ.
- Nhiều endpoint gửi/đọc tin nhắn KHÔNG có `[SBAuthorize]` (SendMessage/SendMessagePost/UpdateFileMedia/GetTheoDieuKien/GetUserInfo/GetSDTforMessage) → anonymous; `SendMessage` còn lấy `UserLogin` từ DTO (`fBMessageInputDto.UserLogin`) thay vì từ token → giả mạo người gửi.
- `ThemMoi` throw NotImplementedException (ConversationService.cs:711) → endpoint #1 chết.
- Truy cập `Data[0]` không guard rỗng: `GetConversationLinkToCrawl` (`conversationInfo.Data[0]`, :726), `UpdateUrlMedia` (`infoPost.Attachments.Data[0]`, :541) → NRE/IndexOutOfRange.
- `getall` / `GetTheoDieuKien` phân trang cứng (page 1, size 10) — không nhận tham số trang.
- `long.Parse(PageId)` nhiều chỗ không try-parse; `fanpage` đôi chỗ thiếu null-check trước khi đọc AccessToken (CreateMessingPost).
