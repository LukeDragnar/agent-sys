---
controller: FBPostController
module: marketing
service: Facebook
man_hinh_ui: "Marketing > Facebook > Quản lý bài post (lưới post fanpage: tìm/lọc theo page + đã/chưa gắn landing; gắn landing + bật sử dụng; đăng bài mới lên FB; sửa bài; ẩn/hiện bài; đồng bộ post thủ công)"
muc_dich: "Quản lý bài viết fanpage ở service Facebook: tìm/lọc post theo quyền page của user, cập nhật cấu hình post (IsSuDung + landing), xem chi tiết post, và CÁC THAO TÁC GHI LÊN FACEBOOK: đăng bài mới (AddFbPost_Feed), sửa bài (UpdateInfoFBPost), ẩn/hiện bài, đồng bộ post thủ công từ Graph về DB + Mongo media."
alias: ["bài post FB", "đăng bài facebook", "quản lý post", "gắn landing cho post", "ẩn hiện bài viết", "đồng bộ post FB"]
auth: mixed
state_machine: []
deprecated: false
entities: [FBPost, FBFanpage, FBPermission, FBUser]
stored_procs: []
depends:
  - "IFBPostService (Facebook.Services.Generated)"
  - "ICommonService.GetConfigFacebook / GetFbFanpageByFanPageId — config FB + page"
  - "ICommonCacheService.ClearCache_FbPost_PostId — clear cache post"
  - "FBPostsHelper (Graph: PostMedia/AddPostFB/EditFBPost/HiddenPost)"
  - "FacebookHelper.GetPostFB / GetAllPostByPage — kéo bài post + media từ Graph"
  - "IMongoDbClient — FBPostAttachmentMG (ảnh/video bài post)"
cross_module_in:
  - "Marketing/Facebook UI — lưới + thao tác bài post"
cross_module_out:
  - "Facebook Graph API — AddPostFB/EditFBPost/HiddenPost/PostMedia (GHI lên FB) + GetPostFB/GetAllPostByPage (đọc)"
  - "Mongo (FBPostAttachmentMG) — persist media bài post"
  - "Landing (IdLanding) — post gắn nguồn landing để route hội thoại"
---

# FBPostController (Facebook) — Quản lý + đăng/sửa bài post Facebook

> Lưu ý phân biệt: đây là `FBPostController` của service **Facebook** (`scrm\Facebook`). Có 1 node trùng tên ở service **FacebookChat** (`FBPostController.md`). Bản Facebook này KHÁC: ngoài tìm/lưu/xem chi tiết còn có các action GHI lên Graph (đăng bài, sửa bài, ẩn/hiện, đồng bộ thủ công). `[TienIch("FBPostController","Quản lý bài post Facebook",TI_Facebook)]`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm/lọc danh sách post | Tìm kiếm | POST `api/FBPost/TimTheoDieuKienAsync` | `FBPost_TimTheoDieuKienAsync` ("Xem chi tiết" — DV-AD-TONG/CN/MCN, DV-MKT, DV-MKT-LEAD, DV-QL-MKT) | public | — | Join FBUser→FBPermission→FBFanpage→FBPost (chỉ post của page user có quyền, đúng đơn vị+chi nhánh), Distinct. Lọc KeyWord (FanPageId/PostId/Content), IdPages (split `;`), TrangThaiGanLanding (đã/chưa gắn landing). Phân trang, OrderBy NgayTao, map FBPostDto, trả total. | EF (FBUser/FBPermission/FBFanpage/FBPost) | Hai nhánh `if(Sorts)/else` đều `OrderBy(NgayTao)` (FBPostService.cs:170-177) → Sorts vô tác dụng. |
| 2 | Lưu cấu hình bài post | Lưu | POST `api/FBPost/CapNhatThongTin` | `FBPost_CapNhatThongTin` ("Xem chi tiết" — 6 role) | public | Post thuộc page user có quyền | Join kiểm tra quyền → cập nhật `IsSuDung` + `IdLanding` (chỉ 2 field), MarkDirty, base.UpdateAsync + clear cache post. Không thấy → "Không tìm thấy fanpage". | EF FBPost | Nhãn quyền "Xem chi tiết" nhưng action là cập nhật → label key sai ngữ nghĩa. |
| 3 | Xem chi tiết bài post | (mở post) | POST `api/FBPost/GetPostByPostId?pageId&postId` | (key rỗng — `[SBAuthorize]` ngầm mặc định BaseController? KHÔNG có attribute) | **anonymous** | Có postId | Tìm FBPost theo `PostId == long.Parse(postId)`; null → trả lỗi "looix". Có PostAttachment → join Mongo `FBPostAttachmentMG`, trả {Id,PostId,FanPageId,Content,PostAttachment}; không → trả nguyên post. | EF + Mongo | `long.Parse(postId)` không guard. Trả message lỗi typo "looix" (FBPostService.cs:257). Không attribute auth → anonymous. |
| 4 | Kiểm tra/cập nhật hạn media post | (ngầm khi render) | POST `api/FBPost/KiemTraHanMediaPost` | (không attribute) | **anonymous** | Media URL có thể hết hạn | `UpdateUrlMedia`: check `IsFacebookUrlAccessible(Url)`; còn sống → "không thay đổi"; hỏng → gọi Graph `GetPostFB`, rebuild FBPostMedias (video=Source/image=Image.Src), ReplaceOne Mongo. | EF + Graph + Mongo | Không auth. `infoPost.Attachments.Data[0]` truy cập index 0 không guard rỗng (FBPostService.cs:707) → NRE. |
| 5 | Sửa bài viết trên FB | Chỉnh sửa bài viết | POST `api/FBPost/UpdateInfoFBPost` (form-data media) | `FBPost_UpdateInfoFBPost` ("Chỉnh sửa bài viết trên facebook" — 6 role) | public | Bài đang tồn tại + page có token | `UpdateFBPost`: upload media (`PostMedia`), gọi Graph `EditFBPost({pageId}_{postId})`; thành công → `UpdateFBPostOfSandBox` đồng bộ lại Content + media vào DB/Mongo. | EF + Graph + Mongo | Graph version/link hardcode `"v21.0"`/`https://graph.facebook.com/` (FBPostService.cs:511,523). `Data[0]` không guard. |
| 6 | Đăng bài mới lên FB | Đăng bài viết | POST `api/FBPost/AddFbPost_Feed` (form-data media) | `FBPost_AddFbPost_Feed` ("Đăng bài viết trên facebook" — 6 role) | public | Page có token | `AddFBPost_Feed`: upload media → `AddPostFB`; nếu post chưa có trong DB → kéo `GetPostFB`, lưu media Mongo + insert FBPost mới (`NguoiTao="webhook"`, NguoiTaoUserId=Guid.Empty). | EF + Graph + Mongo | Insert post đặt NguoiTao="webhook" thay vì user thật (FBPostService.cs:475-476). Điều kiện `fbPosts == null && fbPosts.Count<1` mâu thuẫn (nếu null thì `.Count` NRE) (FBPostService.cs:409). `Data[0]` không guard. |
| 7 | Ẩn/hiện bài viết | Ẩn/hiện | POST `api/FBPost/Hidden_ShowPost` | `FBPost_Hidden_ShowPost` ("Ẩn/hiện bài viết" — 6 role) | public | Bài tồn tại | `Hidden_ShowPost`: gọi Graph `HiddenPost` (Is_Hidden=false→forced_allow, true→hidden); thành công → set `IsHidden` cho mọi FBPost cùng PostId + UpdateAsync. | EF + Graph | `long.Parse(PageId/PostId)` không guard. `fanpage` không null-check trước khi đọc AccessToken. |
| 8 | Đồng bộ post thủ công | Đồng bộ bài viết | POST `api/FBPost/DongBoFBPost?pageId&limit` | `FBPost_DongBoFBPost` ("Đồng bộ bài viết Facebook thủ công" — 6 role) | public | Page tồn tại | `DongBoPostFB`: lấy fanpage theo pageId (null → "fanpage không tồn tại!"); `GetAllPostByPage(limit)`; với mỗi post chưa có trong DB → lưu media Mongo + insert FBPost mới (NguoiTao = user thật). | EF + Graph + Mongo | `long.Parse(pageId)` không guard. `post.Attachments.Data[0]` truy cập index 0 không guard (FBPostService.cs:306,328) → NRE nếu Data rỗng. |

## Quy tắc nghiệp vụ (lọc + quyền)
- **Lọc trạng thái landing** (`TrangThaiGanLanding`): `DaGanLanding` → `IdLanding != null && != Guid.Empty`; `ChuaGanLanding` → ngược lại (enum `EnumFBPostFilterLanding`).
- **Lọc nhiều page** (`IdPages`): split `;`, match theo `IdFBFanpage` HOẶC `FanPageId`.
- **Quyền hàng (row-level):** mọi query đọc/sửa cấu hình join qua FBPermission → user chỉ thấy/sửa post của page mình có quyền. Tuy nhiên các action GHI lên Graph (5/6/7/8) lấy fanpage theo `FanPageId + IdDonVi` (không join FBPermission) → kiểm soát bằng permission-key của endpoint là chính.

## Quan hệ
- **Gọi sang:** Graph (`AddPostFB/EditFBPost/HiddenPost/PostMedia/GetPostFB/GetAllPostByPage`), Mongo (FBPostAttachmentMG), cache post.
- **Dữ liệu (entities):** `FBPost` (chính) join `FBFanpage`/`FBPermission`/`FBUser` để áp quyền.
- **Ripple / đồng bộ:** đăng/sửa/ẩn-hiện bài → GHI trực tiếp lên Facebook + cập nhật DB/Mongo + clear cache; đồng bộ thủ công auto-insert post chưa có.

## Lỗi đã biết / lưu ý kỹ thuật
- Hardcode Graph `"v21.0"` + `https://graph.facebook.com/` rải rác (FBPostService.cs:288,391,402,411,511,523,568,700,765...) thay vì lấy từ `GetConfigFacebook`; AddFBPost_Feed truyền appId/secret = "" (FBPostService.cs:411).
- Truy cập `Attachments.Data[0]` không kiểm tra `Data` rỗng ở DongBoPostFB/AddFBPost_Feed/UpdateFBPostOfSandBox/UpdateUrlMedia (FBPostService.cs:306,328,423,445,585,607,707,729) → IndexOutOfRange/NRE khi post không có attachment đúng cấu trúc.
- `AddFBPost_Feed` (FBPostService.cs:409) — điều kiện `fbPosts == null && fbPosts.Count<1`: nếu `fbPosts==null` thì `.Count` ném NRE; logic check trùng sai.
- Insert post từ AddFBPost_Feed đặt `NguoiTaoUserName="webhook"`, `NguoiTaoUserId=Guid.Empty` (FBPostService.cs:475-476) → post seed kiểu hệ thống dù do user đăng.
- `GetPostByPostId` trả message lỗi typo "looix"; `long.Parse(postId)` + nhiều `long.Parse(pageId)` không try-parse → 500 nếu không phải số.
- `GetPostByPostId`/`KiemTraHanMediaPost` không có `[SBAuthorize]` → anonymous, không lọc đơn vị.
- Hai nhánh sort trong TimTheoDieuKienAsync y hệt → `Sorts` không tác dụng.
