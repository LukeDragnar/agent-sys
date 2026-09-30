---
controller: FBUserController
module: marketing
service: Facebook
man_hinh_ui: "Marketing > Facebook > Kết nối Facebook (nút Đăng nhập/Kết nối FB — sau khi login FB SDK ở FE sẽ gọi LoginFb để đồng bộ user về CRM)"
muc_dich: "Đồng bộ Facebook User vào hệ thống: nhận access_token ngắn hạn từ FE (sau khi user login FB SDK), đổi sang long-lived token, lấy info user, upsert FBUser theo đơn vị/chi nhánh, và phát event đồng bộ tài khoản quảng cáo."
alias: ["kết nối facebook", "đăng nhập FB", "đồng bộ fb user", "login fb", "FBUser"]
auth: public
state_machine: []
deprecated: false
entities: [FBUser]
stored_procs: []
depends:
  - "IFBUserService (Facebook.Services.Generated — SyncAccessToken/GetByFbId)"
  - "ICommonService.GetConfigFacebook — config app FB (AppId/Secret/Version/LinkApi)"
  - "FacebookHelper.ExchangeLongLivedAccessToken / GetUserInfoFB — Graph"
  - "ICommonCacheService.ClearCache_FbUser_FbUserId — clear cache FB user"
  - "IPublishEndpoint (MassTransit) — publish EventFBAdsAccount đồng bộ tài khoản QC"
cross_module_in:
  - "Facebook/Marketing UI — sau login FB SDK gọi LoginFb"
cross_module_out:
  - "FBFanpageController_Facebook — sau LoginFb mới gọi SyncPageFB kéo fanpage"
  - "Đồng bộ tài khoản quảng cáo (EventFBAdsAccount) — consumer xử lý FB Ads Account theo FBUserId"
---

# FBUserController (Facebook) — Kết nối/đồng bộ Facebook User

> 1 endpoint công khai (`LoginFb`). Là bước ĐẦU TIÊN của luồng kết nối FB end-to-end: có FBUser + long-lived token rồi mới sync được fanpage/post/hội thoại.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Đồng bộ FB user vào hệ thống | Kết nối Facebook / Đăng nhập FB | POST `api/FBUser/LoginFb` | `FbUser_LoginFb` ("Đồng bộ Fb user vào hệ thống" — DV-AD-TONG/CN/MCN, DV-MKT, DV-MKT-LEAD, DV-QL-MKT) | public | ModelState valid + có `AccessToken`/`UserId` từ FB SDK | `SyncAccessToken`: lấy config FB (null → MESS_Config_FB_NOT_EXIST); đổi long-lived token (`ExchangeLongLivedAccessToken`); lấy info (`GetUserInfoFB`); GetByFbId theo đơn vị+chi nhánh+user. Đã có → update AccessToken + audit; chưa có → insert FBUser mới (FBTen từ Graph, scope đơn vị/chi nhánh). Publish `EventFBAdsAccount` (đồng bộ tài khoản QC), clear cache FbUser, **ẩn AccessToken=""** trước khi trả. | EF (FBUser) + Graph + RabbitMQ | `GetConfigFacebook(false).Result` gọi `.Result` đồng bộ (block, dễ deadlock) (FBUserService.cs:83). Server/domain logic bị comment-out → FBUser không gắn ServerId/ServerCode (FBUserService.cs:87-93,148-149). |

## Luồng xuyên module (kết nối FB end-to-end)
1. FE login FB SDK → có short-lived `AccessToken` + `UserId`.
2. `FBUser/LoginFb` → đổi long-lived token + upsert FBUser + publish event đồng bộ Ads Account.
3. → `FBFanpage/SyncPageFB` (FBFanpageController_Facebook) kéo danh sách fanpage về.
4. → cấu hình page (bật sử dụng, gắn landing) → webhook đẩy hội thoại/bình luận/post.

## Quan hệ
- **Gọi sang:** Graph (`ExchangeLongLivedAccessToken`, `GetUserInfoFB`), RabbitMQ (`EventFBAdsAccount`).
- **Dữ liệu (entities):** `FBUser` (chính), scope `IdDonVi + IdChiNhanh + NguoiTaoUserId`.
- **Ripple / đồng bộ:** publish event đồng bộ tài khoản quảng cáo; clear cache FbUser theo FBUserId.

## Lỗi đã biết / lưu ý kỹ thuật
- `FBUserService.cs:83` — `_commonService.GetConfigFacebook(false).Result` block synchronous trên async → rủi ro deadlock/thread-starvation.
- `FBUserService.cs:87-93,148-149` — logic Server/domain comment-out → FBUser không lưu ServerId/ServerCode (drift so với pattern cũ).
- `GetByFbId` (FBUserService.cs:177-189) match cả `NguoiTaoUserId == user.Id` → mỗi user chỉ thấy/cập nhật FBUser do chính mình kết nối.
- AccessToken được set `""` trước khi trả về controller → đúng (không lộ token ra FE).
