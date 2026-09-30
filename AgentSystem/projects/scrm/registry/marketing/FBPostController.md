---
controller: FBPostController
module: marketing
service: FacebookChat
man_hinh_ui: "ChatHub > Quản lý bài post (danh sách bài viết fanpage, lọc theo page/đã gắn-chưa gắn landing, bật sử dụng + gắn nguồn landing cho post, xem chi tiết bài post)"
muc_dich: "Quản lý bài viết (post) của fanpage trong ChatHub: tìm/lọc danh sách post theo quyền page của user, cập nhật cấu hình post (IsSuDung + landing), và lấy chi tiết 1 bài post (kéo từ Graph + cache nếu chưa có trong DB)."
alias: ["bài post FB", "quản lý post", "post fanpage", "gắn landing cho post", "bài viết facebook"]
auth: public
state_machine: []
deprecated: false
entities: [FBPost, FBFanpage, FBPermission, FBUser]
stored_procs: []
depends:
  - "IFBPostService (FacebookChat.Services.Generated)"
  - "ICommonService.GetConfigFacebook / GetFbFanpageByFanPageId — config FB + page token"
  - "ICommonCacheService — GetOrSetCache_FBChatPost / ClearCache_FBChatPost (cache post Redis)"
  - "FacebookHelper.GetPostFB — kéo chi tiết bài post + attachment từ Graph"
  - "IMongoDbClient — lưu FBPostAttachmentMG (ảnh/video bài post)"
cross_module_in:
  - "ChatHub UI — danh sách/cấu hình/xem chi tiết post"
cross_module_out:
  - "Facebook Graph API — GetPostFB (lấy bài post + media) khi cache/DB chưa có"
  - "Mongo (FBPostAttachmentMG) + Redis (FBChatPost) — persist + cache media bài post"
---

# FBPostController — Quản lý bài post Facebook (ChatHub)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm/lọc danh sách bài post | Tìm kiếm | POST `api/FBPost/TimTheoDieuKienAsync` | `FBPost_FacebookChat_TimTheoDieuKienAsync` ("Xem chi tiết" — AD-TONG/CN/MCN, MKT, MKT-LEAD, QL-MKT, QL-SALE, SALE, SALE-LEAD, TRUC-PAGE) | public | — | Join FBUser→FBPermission→FBFanpage→FBPost (chỉ post của page **user có quyền**, đúng đơn vị+chi nhánh). Lọc `KeyWord` (FanPageId/PostId/Content), `IdPages` (split `;`), `TrangThaiGanLanding` (đã/chưa gắn landing). Phân trang `PageInfo`, sort `NgayTao`, map `FBPostDto`, trả total. | EF (FBUser/FBPermission/FBFanpage/FBPost) | Cả 2 nhánh `if(Sorts...)/else` đều `OrderBy(NgayTao)` giống nhau (`FBPostService.cs:159-166`) — sort param vô tác dụng. |
| 2 | Lưu cấu hình bài post | Lưu | POST `api/FBPost/CapNhatThongTin` | `FBPost_FacebookChat_CapNhatThongTin` ("Xem chi tiết" — AD-TONG/CN/MCN, MKT, MKT-LEAD, QL-MKT, QL-SALE, SALE, SALE-LEAD, TRUC-PAGE) | public | Post thuộc page user có quyền | Join kiểm tra quyền → cập nhật `IsSuDung` + `IdLanding` cho post (chỉ 2 field), MarkDirty, EF update + clear cache `FBChatPost`. Không tìm thấy → "Không tìm thấy fanpage". | EF FBPost | Nhãn quyền ghi "Xem chi tiết" nhưng action là **cập nhật** → label key sai ngữ nghĩa. |
| 3 | Xem chi tiết bài post | (mở post) | POST `api/FBPost/GetPostByPostId?pageId&postId` | (không key — `[SBAuthorize]`) | public | Có pageId + postId | Lấy từ cache `GetOrSetCache_FBChatPost`. Miss → tìm fanpage theo pageId, gọi Graph `GetPostFB` (`{pageId}_{postId}`), **lưu media vào Mongo** (FBPostAttachmentMG: video=Source / image=Image.Src), **insert FBPost mới** (NguoiTao="webhook"), map `FBPostRedis` + gắn attachments. Set `LinkPostFB`. | Redis cache → Graph → Mongo + EF insert | Hardcode Graph `"v21.0"`/`https://graph.facebook.com/`/appId-secret rỗng (`FBPostService.cs:259`). `infoPost.Attachments.Data[0]` truy cập index 0 không guard rỗng → throw nếu post không có attachment data. |

## Quy tắc nghiệp vụ (lọc)
- **Tiện ích gate:** `[TienIch("FBPostController","Quản lý bài post Facebook", MaTienIchSCRM.TI_ChatHub)]`.
- **Lọc trạng thái landing** (`TrangThaiGanLanding`): `DaGanLanding` → `IdLanding != null && != Guid.Empty`; `ChuaGanLanding` → ngược lại (enum `EnumFBPostFilterLanding`).
- **Lọc nhiều page** (`IdPages`): split `;`, match theo `IdFBFanpage` **hoặc** `FanPageId`.
- **Quyền hàng (row-level):** mọi query join qua FBPermission → user chỉ thấy/sửa post của page mình có quyền (theo FB Tasks đã sync).

## Quan hệ
- **Gọi sang:** Graph API (`GetPostFB`) khi xem chi tiết post chưa cache; Mongo (FBPostAttachmentMG); Redis (FBChatPost).
- **Dữ liệu (entities):** `FBPost` (chính) join `FBFanpage`/`FBPermission`/`FBUser` để áp quyền.
- **Ripple / đồng bộ:** cập nhật post → `ClearCache_FBChatPost`; xem chi tiết lần đầu → insert FBPost + media (auto-seed từ Graph).

## Lỗi đã biết / lưu ý kỹ thuật
- `FBPostService.cs:259` — `GetPostByPostId` hardcode Graph version `"v21.0"`, link, appId/secret rỗng thay vì `GetConfigFacebook` (drift so với pattern dùng config).
- `FBPostService.cs:272-307` — chuỗi `infoPost.Attachments.Data[0]...` truy cập index 0 không kiểm tra `Data` rỗng → NRE/IndexOutOfRange khi post không có attachment đúng cấu trúc.
- `FBPostService.cs:159-166` — hai nhánh sort y hệt (`OrderBy(NgayTao)`) → `gridInfo.Sorts` không có tác dụng.
- `GetPostByPostId` `[SBAuthorize]` rỗng (chỉ check đăng nhập); 2 endpoint còn lại có permission key nhưng nhãn "Xem chi tiết" dùng chung cho cả action cập nhật.
- Insert post từ luồng xem chi tiết đặt `NguoiTaoUserName="webhook"`, `NguoiTaoUserId=Guid.Empty` → post seed kiểu hệ thống, không gắn user thật.
