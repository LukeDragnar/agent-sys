---
controller: FBFanpageController
module: marketing
service: Facebook
man_hinh_ui: "Marketing > Facebook > Quản lý Fanpage (lưới page đã kết nối; nút Đồng bộ fanpage, Chỉnh sửa/cấu hình nguồn-landing, Kiểm tra Access_Token)"
muc_dich: "Quản lý fanpage Facebook của đơn vị (service Facebook, KHÔNG phải FacebookChat): đồng bộ fanpage từ FB User về CRM (kèm page thuộc Business), tìm/lọc, cấu hình bật sử dụng + đăng ký webhook + gắn nguồn landing, kiểm tra access token."
alias: ["fanpage facebook", "quản lý page", "đồng bộ fanpage", "kết nối page FB", "FBFanpage (Facebook)"]
auth: mixed
state_machine: []
deprecated: false
entities: [FBFanpage, FBPermission, FBUserFanpage, FBBusiness, FBUser]
stored_procs: []
depends:
  - "IFBFanpageService (Facebook.Services.Generated — đồng bộ + lưu fanpage)"
  - "IFBUserService.GetByFbId (lấy FB user để đồng bộ)"
  - "IFBPermissionService (upsert quyền task của fanpage)"
  - "IFBUserFanpageService (map user ↔ fanpage)"
  - "ICommonService (GetConfigFacebook/V2, cache fanpage theo FanPageId/đơn vị, GetFbBusinessByBusinessId)"
  - "FacebookHelper (Graph API: GetSyncPageFacebook, GetSyncBusinessPageFacebook, ExchangeLongLivedAccessToken, SubcrisbeUnsubscribedTopic, FBDebugAccessToken)"
  - "FacebookWebhook service (FacebookPrivateHelper.AddFanpageServer)"
  - "ILogActivityHelper (LichSuTacDong khi đổi cấu hình page)"
  - "Core/TienIch (MaTienIchSCRM.TI_Facebook)"
cross_module_in:
  - "FBUserController_Facebook — sau LoginFb (đồng bộ FB User) FE gọi SyncPageFB để kéo fanpage về"
cross_module_out:
  - "FacebookWebhook service — khi bật IsSuDung sẽ AddFanpageServer + subscribe topic (nhận tin nhắn/bình luận realtime)"
  - "Landing/nguồn (IdLanding) — fanpage gắn nguồn để route hội thoại"
  - "FBPost / Conversation — page IsSuDung=true mới sinh post & hội thoại đồng bộ về"
  - "Các chi nhánh khác cùng FanPageId — bật page ở 1 chi nhánh sẽ TẮT page đó ở chi nhánh khác (ripple cross-branch)"
---

# FBFanpageController (Facebook) — Quản lý fanpage Facebook

> Lưu ý phân biệt: đây là controller của service **Facebook** (`scrm\Facebook`). Có 1 controller trùng tên ở service **FacebookChat** — xem `FBFanpageController.md` (node FacebookChat). Code KHÁC nhau (service Facebook có vòng sync Business + SyncPageFB V1/V2, KHÔNG có KichHoatFanPage/gói cước).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Đồng bộ fanpage của FB User | Đồng bộ fanpage | POST `api/FBFanpage/SyncPageFB`, POST `api/FBFanpage/SyncPageFB_V2` | `FBFanpage_SyncPageFB` ("Đồng bộ fanpage" — DV-AD-TONG/CN/MCN, DV-MKT, DV-MKT-LEAD, DV-QL-MKT) | public | Có FB User đã LoginFb (body `FBAuthResponseDto.UserId`) | Lặp Graph (phân trang) lấy page có `access_token`: V1 lấy cả page cá nhân (`GetSyncPageFacebook`) + page thuộc Business (`GetSyncBusinessPageFacebook`, tạo `FBBusiness`, gán Tasks admin mặc định); exchange long-lived token; **upsert** `FBFanpage` + `FBPermission` (Tasks/TaskStatus) + `FBUserFanpage` theo đơn vị/chi nhánh; trả list (AccessToken=""). V2 chỉ page cá nhân, không Business. | EF (FBFanpage/FBPermission/FBUserFanpage/FBBusiness) + Graph API | `SyncPageFB` catch rỗng nuốt mọi lỗi (FBFanpageService.cs:590-593). V2 đọc `responseFb.Accounts.Error` không null-check `Accounts` → NRE; `return null` khi accounts rỗng (FBFanpageService.cs:637,649-650). |
| 2 | Tìm/lọc fanpage | Tìm kiếm | POST `api/FBFanpage/TimTheoDieuKienAsync` | `FBFanpage_TimTheoDieuKienAsync` (6 role MKT/AD) | public | — | Lọc fanpage user **có quyền** (join FBUser→FBPermission→FBFanpage theo NguoiTaoUserId + đơn vị + chi nhánh), distinct FanPageId, keyword theo Tên/FanPageId, phân trang, OrderBy NgayTao; gán Tasks; ẩn AccessToken khi `isClientCall=true`. | EF (join 3 bảng) | — |
| 3 | Cập nhật / cấu hình fanpage | Chỉnh sửa thông tin | POST `api/FBFanpage/CapNhatThongTin` | `FBFanpage_CapNhatThongTin` (6 role) | public | Sửa page đang có quyền | Map DTO→entity, gọi `SavePageFB`: validate (bật sử dụng phải bật đồng bộ tin nhắn HOẶC bình luận, và phải chọn nguồn IdLanding); chỉ đổi `IsSuDung/DongBoTinNhan/DongBoBinhLuan/IdLanding`; khi đổi IsSuDung → tắt page cùng FanPageId ở chi nhánh khác + AddFanpageServer + subscribe/unsubscribe topic + ghi LichSuTacDong. | EF + FacebookWebhook + Graph | Lỗi `AddFanpageServer`/subscribe → trả lỗi, không Update DB (entity in-memory đã set IsSuDung nhưng chưa persist). |
| 4 | Đếm page đang dùng ở chi nhánh khác | (cảnh báo trùng page) | GET `api/FBFanpage/CountFanpageId_IsSuDung/{id}` | `[SBAuthorize]` trần (chỉ cần đăng nhập) | public | Trước khi bật 1 page | Trả list + total bản ghi cùng FanPageId đang `IsSuDung=true` ở **chi nhánh khác** → cảnh báo page đang dùng nơi khác. | EF (cache GetFbFanpageByFanPageId) | `long.Parse(pageId)` không guard → throw nếu id không phải số (FBFanpageService.cs:1273). |
| 5 | Lấy IdLanding theo PageId | (nội bộ form chọn nguồn) | GET `api/FBFanpage/GetLandingIdByPageId?pageId` | `[SBAuthorize("","",6 role)]` (key rỗng) | public | — | Trả IdLanding của page theo FanPageId+đơn vị+chi nhánh; chưa gắn nguồn → lỗi "Page chưa được gắn nguồn"; không tồn tại → "page này không tồn tại". | EF | `long.Parse(pageId)` không guard. |
| 6 | Kiểm tra Access Token | Kiểm tra Access_Token | GET `api/FBFanpage/Debug_Token?pageId` | `FBFanpage_Debug_Token` (6 role) | public | — | Gọi Graph `debug_token` bằng **App Access Token** (`{AppId}\|{AppSecret}`); `IsConnected = Is_Valid && App_Id==config.FBAppId`; trả chi tiết scope/expire + message kết nối. | EF + Graph | — |
| 7 | Subscribe lại tất cả fanpage | (không có UI) | GET `api/FBFanpage/SubcribeAllFanpage` | (none) | **anonymous** | — | **VÔ HIỆU**: `return ResultWithData("OK")` ngay đầu hàm; phần subscribe topic mọi page IsSuDung=true là dead code. | — | `[AllowAnonymous]` + return sớm → endpoint không auth, không tác dụng (FBFanpageService.cs:1179-1181). |
| 8 | HaiHN Fix bug | (không có UI) | GET `api/FBFanpage/HaiHNFixBug` | (none) | **anonymous** | — | **VÔ HIỆU**: `return ResultWithData("OK")` ngay đầu; dead code tắt webhook page theo donviId/chiNhanhId **hardcode** (nyd). | — | `[AllowAnonymous]` + GUID hardcode + return sớm (FBFanpageService.cs:1239-1247). |

## Quy tắc nghiệp vụ (gating — SavePageFB)
- **Validate bật sử dụng:** `IsSuDung=true` mà cả `DongBoTinNhan=false` và `DongBoBinhLuan=false` → lỗi "Bạn phải sử dụng đồng bộ tin nhắn hoặc đồng bộ bình luận".
- **Validate nguồn:** `IsSuDung=true` mà `IdLanding` rỗng/Empty → lỗi "Chưa chọn nguồn kết nối".
- **Độc quyền page theo chi nhánh:** khi bật page, mọi bản ghi cùng `FanPageId` đang `IsSuDung=true` ở **chi nhánh khác** bị tắt (IsSuDung=false, IsDangKyWebhook=false) + ghi LichSuTacDong + clear cache đơn vị → 1 FanPageId chỉ active ở 1 chi nhánh tại một thời điểm (invariant).
- **Đồng bộ webhook chỉ chạy khi đổi IsSuDung** (`udpateSyncServerWebook = IsSuDung cũ != mới`): AddFanpageServer sang FacebookWebhook + SubcrisbeUnsubscribedTopic. Lỗi bước này → trả lỗi, không Update DB.
- **Phân loại quyền (`FormatTaskStatus`):** Tasks chứa đủ bộ Admin/Editor/Moderator → `TaskStatus=true`; chỉ Advertiser/Analyst → `false`.

## Endpoint nội bộ (private / service-to-service)
- Không có endpoint HTTP private. Service có hàm nội bộ dùng khi sync / cho service khác qua DI: `GetByPageId`, `GetByPageIdAndDonViId`, `GetFanpageByFbUser`, `GetByBusinessIdAndDonViId`.

## Quan hệ
- **Gọi sang:** `FacebookHelper` (Graph), `FacebookPrivateHelper.AddFanpageServer` (FacebookWebhook), `IFBUserService.GetByFbId`, `IFBPermissionService`, `IFBUserFanpageService`, `ICommonService` (config + cache).
- **Dữ liệu (entities):** `FBFanpage` (chính), `FBPermission` (task quyền page), `FBUserFanpage` (map user↔page), `FBBusiness` (chỉ V1 — business sở hữu page), `FBUser`.
- **Ripple / đồng bộ:** bật page → AddFanpageServer + subscribe topic → tin nhắn/bình luận chảy về (Conversation/Comment); tắt page cùng FanPageId ở chi nhánh khác là ripple cross-branch.

## Luồng xuyên module (kết nối page end-to-end)
1. `FBUserController.LoginFb` → đồng bộ FB User + long-lived token.
2. `SyncPageFB`/`SyncPageFB_V2` → kéo danh sách page (V1 kèm Business) về, upsert FBFanpage/FBPermission/FBUserFanpage.
3. `CapNhatThongTin` (SavePageFB) → chọn nguồn landing + bật đồng bộ tin nhắn/bình luận → AddFanpageServer sang **FacebookWebhook** + subscribe topic.
4. Webhook đẩy event → đồng bộ hội thoại/bình luận/`FBPost`.

## Lỗi đã biết / lưu ý kỹ thuật
- `FBFanpageService.cs:590-593` — `SyncPageFB` catch rỗng nuốt mọi exception → đồng bộ lỗi giữa chừng vẫn trả `ResultWithData` với list dở dang, không log.
- `FBFanpageService.cs:637` — `SyncPageFB_V2` đọc `responseFb.Accounts.Error` không null-check `Accounts` → NRE khi user không có page.
- `FBFanpageService.cs:649-650` — V2 `return null` (không phải MethodResult) khi accounts rỗng → controller `ResponseResult(null)`.
- `FBFanpageService.cs:1179` (SubcribeAllFanpage) & `:1239` (HaiHNFixBug) — `[AllowAnonymous]` + `return` sớm: dead code; HaiHNFixBug có GUID đơn vị/chi nhánh hardcode.
- `FBFanpageController.cs:122-133` (`CountFanpageId_IsSuDung`, `GetLandingIdByPageId`) & service `long.Parse(pageId)` không try-parse → 500 nếu pageId không phải số.
- `FBFanpageController.cs:127` (`GetLandingIdByPageId`) & `:122` — `[SBAuthorize]` key rỗng (chỉ check đăng nhập / role).
- `[SBAuthorize]` ẩn AccessToken="" trước khi trả client (đúng) ở SyncPageFB/SavePageFB/TimTheoDieuKien — nhưng node `FBUserFanpagesController_Facebook` vẫn lộ AccessToken/PageToken qua truy vấn của nó.
