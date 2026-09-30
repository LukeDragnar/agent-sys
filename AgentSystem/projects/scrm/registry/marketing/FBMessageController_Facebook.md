---
controller: FBMessageController
module: marketing
service: Facebook
man_hinh_ui: "ChatHub > Hội thoại > chi tiết 1 contact (mở 1 bình luận/inbox khách → kéo toàn bộ comment cha-con hoặc message của hội thoại từ Graph, kèm avatar + che số điện thoại)"
muc_dich: "Lấy chi tiết tương tác của 1 contact theo CommentId: từ FBComment gốc trong DB, gọi Graph kéo comment cha + comment con (nếu là COMMENT) hoặc toàn bộ message của conversation (nếu là MESSAGE), gán avatar, lấy bài post liên quan, áp che số điện thoại theo cấu hình che số của Core."
alias: ["chi tiết hội thoại", "xem comment", "message theo contact", "GetByContactId", "FBMessage"]
auth: public
state_machine: []
deprecated: false
entities: [FBComment, FBPost, FBFanpage]
stored_procs: []
depends:
  - "IFBMessageService (Facebook.Services.Generated)"
  - "ICommonService.GetConfigFacebook — config app FB"
  - "FacebookHelper (Graph: GetCommentIncludeParent/GetCommentChild/GetMessageFB/GetConversationFBBySenderId/GetFBPictureInfo* + ConvertPostFbToPostDto)"
  - "IClientHelper → Core /api/CauHinh/GetCauHinhCore (lấy cấu hình che số điện thoại)"
  - "SecurityHelper.CheThongTin / CommonHelper.ValidateCheSoCommon — che SĐT trong nội dung"
cross_module_in:
  - "ChatHub UI — mở chi tiết 1 contact/bình luận"
cross_module_out:
  - "Facebook Graph API — kéo comment/message/avatar/post realtime"
  - "Core service — đọc cấu hình che số điện thoại (CauHinhCore)"
---

# FBMessageController (Facebook) — Chi tiết hội thoại theo contact

> 1 endpoint công khai (`GetByContactId`). Logic nặng: kết hợp DB (FBComment) + Graph (comment cha/con hoặc message) + avatar + bài post + che SĐT. Toàn bộ bọc trong try-catch nuốt lỗi.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy chi tiết hội thoại theo contact | (mở 1 contact) | POST `api/FBMessage/GetByContactId` | (key rỗng — `[SBAuthorize()]`; key có sẵn nhưng đang bị comment) | public | Có `contactId` (+ optional `messageId`) | Tìm FBComment theo `IdContact == contactId` (+ CommentId nếu messageId) join FBPost/FBFanpage theo đơn vị. Nếu `TypeContent==COMMENT`: gọi Graph `GetCommentIncludeParent` + `GetCommentChild` (MAX 50), lấy bài post (`ConvertPostFbToPostDto`). Ngược lại (message): nếu là parent thì lấy conversation theo senderId rồi `GetMessageFB`, sort theo NgayTao. Gán avatar (`GetFBPictureInfoByStringIdUsers`), dedupe theo CommentId, áp che SĐT theo CauHinhCore → trả `GetByContactIdDto`. | EF (FBComment/FBPost/FBFanpage) + Graph + Core config | Try-catch nuốt mọi exception, trả `data=null` không log (FBMessageService.cs:317-321) → lỗi Graph "im lặng" thành rỗng. Truy cập `configFB.FBAppVersion` không null-check sau `GetConfigFacebook` (FBMessageService.cs:100) → NRE nếu config null. `GetConfigFacebook(false).Result` block sync. |

## Quy tắc nghiệp vụ (che số điện thoại)
- Mặc định `IsCheSo = true` (che). Gọi Core `GetCauHinhCore/{DonViId}`; nếu lấy được cấu hình → `ValidateCheSoCommon` quyết định theo role user (ListRoleCheSoDienThoai, UuTienCheSoDienThoaiChuaRoleBiChan, ListUserMacDinhXemSoDienThoai...).
- Mỗi item: `SecurityHelper.CheThongTin(Content, SDTInMessage, !IsCheSo)` — `IsCheSo=true` thì che SĐT trong nội dung; user có quyền xem thì hiện.

## Quan hệ
- **Gọi sang:** Graph (comment/message/avatar/post), Core (cấu hình che số).
- **Dữ liệu (entities):** `FBComment` (gốc), `FBPost` (bài post liên quan), `FBFanpage` (AccessToken để gọi Graph).
- **Ripple / đồng bộ:** chỉ-đọc, không persist; nhưng gọi Graph realtime mỗi lần view → phụ thuộc token page còn hạn.

## Lỗi đã biết / lưu ý kỹ thuật
- `FBMessageService.cs:317-321` — try-catch nuốt exception, không log (chỉ comment `_logsService.Add`) → lỗi thành kết quả rỗng, khó debug.
- `FBMessageService.cs:100` — đọc `configFB.FBAppVersion`/`FBAppId` không null-check sau `GetConfigFacebook(false).Result` → NRE nếu config FB chưa cấu hình; lại block sync `.Result`.
- `GetByContactId` `[SBAuthorize()]` key rỗng (chỉ đăng nhập) — version có permission key `FBMessage_GetByContactId` đang bị comment (FBMessageController.cs:24).
- Mọi call Graph dùng `commentInDB.FBFanpage.AccessToken`; nếu token rỗng (`== ""`) thì bỏ qua nhánh kéo Graph → chỉ trả comment gốc DB.
