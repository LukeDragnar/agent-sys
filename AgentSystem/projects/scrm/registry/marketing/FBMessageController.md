---
controller: FBMessageController
module: marketing
service: FacebookChat
man_hinh_ui: "ChatHub > Hộp thoại chat (mở 1 contact/conversation → load lịch sử tin nhắn & bình luận của comment/inbox đó)"
muc_dich: "Lấy chi tiết hội thoại (inbox) hoặc chuỗi bình luận (comment) của 1 contact để hiển thị trong khung chat ChatHub: đọc bản ghi gốc trong DB/Mongo rồi gọi Graph API kéo message/comment con realtime, gắn avatar và che số điện thoại theo cấu hình."
alias: ["tin nhắn FB", "chat box ChatHub", "lịch sử hội thoại", "load message", "xem hội thoại facebook", "comment + inbox"]
auth: public
state_machine: []
deprecated: false
entities: [FBComment, FBPost, FBFanpage]
stored_procs: []
depends:
  - "IFBMessageService (FacebookChat.Services.Generated)"
  - "ICommonService.GetConfigFacebook / GetFbFanpageByFanPageId — config app FB + page token"
  - "FacebookHelper — GetCommentIncludeParent/GetCommentChild/GetConversationFBBySenderId/GetMessageFB/GetPostFB + GetFBPictureInfo* (avatar)"
  - "IMongoDbClient — MessageChatBox (lịch sử chat SB lưu Mongo) cho luồng GetByContactId_ChatSB"
  - "IClientHelper → Core/CauHinh — GetCauHinhCore (cấu hình che số điện thoại)"
  - "SecurityHelper.CheThongTin / CommonHelper.ValidateCheSoCommon — che SĐT trong nội dung message"
cross_module_in:
  - "ChatHub UI / Conversation service — mở 1 contact gọi GetByContactId / GetByContactId_ChatSB"
cross_module_out:
  - "Core/CauHinh — GetCauHinhCore (đọc cấu hình che số) qua HTTP _clientHelper"
  - "Facebook Graph API — kéo message/comment/conversation/avatar realtime mỗi lần view"
---

# FBMessageController — Lấy tin nhắn / hội thoại Facebook (ChatHub)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Mở hội thoại theo contact (bản gốc) | (click contact mở chat) | POST `api/FBMessage/GetByContactId` | (không key — `[SBAuthorize()]`; key cũ `FBMessage_GetByContactId` đã comment) | public | Contact có `FBComment` gốc trong đơn vị | Tìm `FBComment` theo `IdContact` (+`CommentId` nếu có) join `FBPost`+`FBFanpage`. Nếu `TypeContent=COMMENT`: kéo comment cha+con qua Graph (`GetCommentIncludeParent`/`GetCommentChild`) + lấy post. Nếu là message: lấy conversation theo senderId rồi `GetMessageFB`. Gắn avatar người gửi, **dedup theo CommentId**, sort theo NgayTao. | EF (FBComment/FBPost/FBFanpage) + Graph API | Toàn hàm bọc `try{}catch(ex){}` rỗng (`FBMessageService.cs:296-299`) → lỗi Graph nuốt im, trả data null. Cần `AccessToken != ""` mới gọi Graph, else trả list rỗng. |
| 2 | Mở hội thoại theo contact (Chat SB / Mongo) | (click contact mở chat) | POST `api/FBMessage/GetByContactId_ChatSB` | (không key — `[SBAuthorize()]`) | public | Có `FanpageId` + (`ConversationId` hoặc `KhachHangId`) | Lấy fanpage theo `FanpageId` (lọc đúng đơn vị). `ConversationId` không bắt đầu `t_` → đọc `MessageChatBox` (Mongo) theo ConversationId. Ngược lại → Graph: lấy conversation theo `KhachHangId` rồi `GetMessageFB`. Dedup CommentId, sort NgayTao. **Che SĐT** theo cấu hình core (`GetCauHinhCore`). | Mongo (MessageChatBox) + Graph API + HTTP Core/CauHinh | `GetConversationFBBySenderId`/`GetMessageFB` **hardcode** `"v21.0"` + `https://graph.facebook.com/` + appId/secret rỗng (`FBMessageService.cs:344,357`) thay vì lấy từ config. |

## Quy tắc nghiệp vụ (che số điện thoại — chỉ luồng ChatSB)
- Mặc định `checkCheSo.IsCheSo = true` (che). Gọi `GetCauHinhCore/{DonViId}` lấy cấu hình che → `ValidateCheSoCommon(cauHinhCheSo, userLogin)` quyết định user có được xem SĐT không (role, ưu tiên chặn, danh sách user mặc định xem...).
- Lỗi gọi core → giữ mặc định `IsCheSo=true` (an toàn: vẫn che). Mỗi message: `SecurityHelper.CheThongTin(Content, SDTInMessage, !IsCheSo)`.

## Quan hệ
- **Gọi sang:** Graph API (comment/message/conversation/avatar/post); Core/CauHinh (`GetCauHinhCore`) qua HTTP; Mongo (`MessageChatBox`).
- **Dữ liệu (entities):** `FBComment` (bản ghi gốc inbox/comment), `FBPost`, `FBFanpage` (lấy AccessToken page) — controller read-only, không ghi DB.
- **Ripple / đồng bộ:** không tạo side-effect ghi; chỉ đọc realtime từ Graph + che số.

## Lỗi đã biết / lưu ý kỹ thuật
- `FBMessageService.cs:296-299` & `:425-426` — `catch(Exception ex){}` rỗng (logsService comment) → lỗi Graph/Mongo nuốt im, trả `ResultWithData(null)`.
- `FBMessageService.cs:344,357` — `GetByContactId_ChatSB` hardcode version Graph `"v21.0"`, link `https://graph.facebook.com/`, appId/secret rỗng `""` thay vì đọc `GetConfigFacebook` → khác hẳn pattern của `GetByContactId` (luồng 1 lấy từ config).
- Cả 2 endpoint chỉ `[SBAuthorize()]` rỗng (check đăng nhập), không có permission key riêng → ai đăng nhập + có tiện ích ChatHub đều gọi được.
- Dữ liệu trả phụ thuộc `FBFanpage.AccessToken != ""`; token rỗng/hết hạn → list comment rỗng (không báo lỗi rõ).
