---
controller: FBUserController
module: marketing
service: FacebookChat
man_hinh_ui: "ChatHub > Kết nối Facebook (nút 'Đăng nhập Facebook' → đồng bộ tài khoản FB user vào hệ thống)"
muc_dich: "Điểm vào đăng nhập/đồng bộ Facebook user vào ChatHub: nhận access token ngắn hạn từ FE (FB SDK login), đổi sang long-lived token, lấy profile FB rồi lưu/cập nhật FBUser theo đơn vị."
alias: ["login facebook", "đăng nhập FB", "kết nối facebook user", "đồng bộ fb user", "FBUser", "LoginFb"]
auth: internal
state_machine: []
deprecated: false
entities: [FBUser]
stored_procs: []
depends:
  - "IFBUserService.SyncAccessToken (FBUserService)"
  - "ICommonService.GetConfigFacebook (FBAppId/Secret/LinkApi/AppVersion)"
  - "FacebookHelper.ExchangeLongLivedAccessToken + GetUserInfoFB (gọi Graph API)"
  - "ICommonCacheService.ClearCache_FbUser_FbUserId"
  - "DomainHelper.GetDomain(Request)"
  - "Core/TienIch (MaTienIchSCRM.TI_ChatHub)"
cross_module_in:
  - "FBUserFanpages / fanpage-sync flow — sau khi FBUser kết nối, dùng access token để kéo & phân quyền fanpage"
cross_module_out:
  - "Facebook Graph API (đổi long-lived token + lấy profile user)"
  - "Cache FbUser (ClearCache_FbUser_FbUserId) sau khi lưu token"
---

# FBUserController — Đăng nhập / đồng bộ Facebook user (ChatHub)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Đăng nhập / đồng bộ FB user | Đăng nhập Facebook | POST `api/FBUser/LoginFb` | `FbUser_FacebookChat_LoginFb` | internal | Có TI_ChatHub; FE đã login FB SDK (có access token + userId) | Validate ModelState → lấy `AccessToken`/`UserId` từ body + domain từ Request → `SyncAccessToken`. Service: load config FB (lỗi nếu null), **đổi sang long-lived token**, gọi Graph lấy profile, tìm FBUser theo (FBUserId + DonViId + người tạo); **có** → update token + người/ngày cập nhật; **chưa có** → insert FBUser mới (gắn IdDonVi/IdChiNhanh, IsDelete=false); clear cache; trả FBUser (đã ẩn AccessToken). | EF `FBUser` + Facebook Graph API | Xem lỗi bên dưới. |

## Quy tắc nghiệp vụ (gating)
- **Tiện ích gate:** `[TienIch("FBUserController", "Quản lý facebook user kết nối", MaTienIchSCRM.TI_ChatHub)]`.
- **Phân quyền:** AD-TONG/CN/MCN, MKT, MKT-LEAD, QL-MKT, QL-SALE, SALE, SALE-LEAD (KHÔNG có DV-TRUC-PAGE — khác các controller khác trong service).
- **Dedup FBUser:** key trùng = `FBUserId == fbUserId && IdDonVi == DonViId && NguoiTaoUserId == userLogin.Id` (qua cache `GetFbUserByFBUserId`). Trùng → chỉ refresh token; mới → insert.
- **Bảo mật:** AccessToken được set rỗng (`""`) trước khi trả về FE.

## Quan hệ
- **Gọi sang:** `IFBUserService.SyncAccessToken` → `ICommonService.GetConfigFacebook`, `FacebookHelper.ExchangeLongLivedAccessToken`, `FacebookHelper.GetUserInfoFB`, `GetByFbId` (đọc qua cache `GetFbUserByFBUserId`), `ClearCache_FbUser_FbUserId`.
- **Dữ liệu (entities):** `FBUser` (FBUserId, FBTen, FBProfileUrl, AccessToken, IdDonVi, IdChiNhanh, IsDelete, audit fields).
- **Ripple / đồng bộ:** sau khi có FBUser + token, các luồng fanpage (FBUserFanpages, sync page) dùng token này; cache FbUser bị clear để các consumer đọc lại token mới.

## Luồng xuyên module (kết nối FB user end-to-end)
1. FE đăng nhập FB SDK → lấy short-lived access token + FB userId → POST `LoginFb`.
2. Service đổi long-lived token (Graph), lấy profile, upsert `FBUser` theo đơn vị.
3. Clear cache FbUser → token mới sẵn sàng cho các bước sync fanpage / nhận webhook chat.

## Lỗi đã biết / lưu ý kỹ thuật
- `FBUserService.cs:91-96` — nếu `ExchangeLongLivedAccessToken` trả null/empty thì giữ token ngắn hạn (không fail rõ ràng) → token có thể hết hạn sớm.
- `FBUserService.cs:110,139` — `GetUserInfoFB` trả `rp` dùng `rp.Name` không null-check → NullRef nếu Graph lỗi/rỗng khi tạo mới (`fbUserDb.FBTen = rp.Name`).
- `FBUserService.cs:154` — sau khi insert mới, `fbUserDb` được dùng để trả về nhưng `InsertAsync` rs không kiểm tra Success; trả về object dù insert lỗi.
- `FBUserService.cs:82-88` — logic xác thực server theo domain (`TimTheoDomain` / `ServerId`) đã bị comment toàn bộ → FBUser không còn gắn server; field `ServerId/ServerCode` bỏ trống.
- `FBUserService.cs:78` — `GetConfigFacebook(false).Result` (block `.Result` trong async) → rủi ro deadlock/đồng bộ trong môi trường có sync-context.
- Controller chỉ có 1 endpoint UI; `partial class` → có thể có phần generated khác cùng tên (chưa thấy file khác trong scope đọc).
