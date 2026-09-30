---
controller: AuthorizationController
module: marketing
service: TiktokWebhook
man_hinh_ui: "nội bộ + callback OAuth. `GetAuthorizationUrl` do core SCRM gọi để lấy URL ủy quyền; `ResultAuth` là endpoint TikTok redirect về sau khi user ủy quyền."
muc_dich: "OAuth gian hàng TikTok Shop: sinh authorization url và xử lý callback (đổi code lấy token, lưu Authorization), rồi redirect người dùng về CRM."
alias: ["Authorization Tiktok", "OAuth TikTok", "ủy quyền TikTok", "GetAuthorizationUrl", "ResultAuth callback", "tiktok auth"]
auth: mixed         # GetAuthorizationUrl ý đồ internal (SBAuthorize bị comment → anonymous); ResultAuth bắt buộc anonymous (TikTok callback)
state_machine: []
deprecated: false
entities: [Authorization]
stored_procs: []
depends:
  - "IAuthorizationService (TiktokWebhook.Service.Generated) — GetAuthorizationUrl / SaveAuthorization"
  - "FunctionCommon.Base64Encode/Decode (SB.Core) — pack/unpack state"
  - "BaseController.CurrentUser (token SCRM)"
cross_module_in:
  - "Core SCRM (EcommerceConnect) — gọi GetAuthorizationUrl để khởi tạo kết nối gian hàng TikTok"
  - "TikTok Shop (external) — redirect về ResultAuth kèm code/scopes/state"
cross_module_out:
  - "Authorization (lưu token gian hàng TikTok) → các luồng đồng bộ TikTok sau ủy quyền"
  - "Redirect về crmCallBackUrlAuthorization (CRM) đính token đã lưu"
---

# AuthorizationController — OAuth ủy quyền gian hàng TikTok Shop

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy URL ủy quyền TikTok | (FE "Kết nối/Xác thực") | POST `api/tiktok/auth/GetAuthorizationUrl` | — (`[SBAuthorize()]` controller bị comment) | **internal** (kỳ vọng), thực tế **anonymous** vì auth comment | khi user khởi tạo kết nối gian hàng | `authorizationService.GetAuthorizationUrl(reqData)` → trả URL ủy quyền TikTok (pack `state` = callbackUrl|userConnectId base64) → FE redirect. | service `IAuthorizationService` | `[SBAuthorize()]` controller bị comment (`:11`) → endpoint không gác quyền. |
| 2 | Callback sau ủy quyền | (TikTok redirect) | GET `api/tiktok/auth/ResultAuth` (`?code&scopes&state`) | — | **anonymous** | TikTok gọi sau khi user đồng ý | Decode `state` → `callbackUrl`, `userConnectId`. Nếu có `userConnectId` → `Redirect("")` (re-auth, hiện URL rỗng — bug). Nếu không → `SaveAuthorization(code, CurrentUser)` lưu token → redirect `callbackUrl + token`. | EF: `Authorization` (qua service) | `redirectUrl` rỗng khi nhánh `userConnectId` (`:50,54`); `CurrentUser` rỗng vì anonymous. |

## Quy tắc nghiệp vụ (OAuth state)
- `state = Base64( crmCallBackUrlAuthorization + "|" + userConnectId )` được pack ở bước GetAuthorizationUrl, unpack ở ResultAuth (`crmData.Split("|")`).
- Rẽ nhánh callback:
  - `userConnectId` rỗng → **kết nối mới**: đổi `code` lấy token → `SaveAuthorization` → redirect kèm token.
  - `userConnectId` có giá trị → **re-auth**: hiện chỉ `Redirect(redirectUrl)` với `redirectUrl=""` → **chưa hoàn thiện** (không lưu, không redirect đúng).

## Quan hệ
- **Gọi sang:** `IAuthorizationService.GetAuthorizationUrl` / `.SaveAuthorization`.
- **Dữ liệu (entities):** `Authorization` (token gian hàng TikTok, ghi qua service).
- **Ripple / đồng bộ:** sau khi có token, các luồng đồng bộ TikTok (đơn/sản phẩm/video) dùng token này.

## Luồng xuyên module (OAuth TikTok end-to-end)
1. Core SCRM gọi `GetAuthorizationUrl(reqData)` (pack state) → trả URL → FE redirect user sang TikTok.
2. User đồng ý trên TikTok → TikTok redirect `GET api/tiktok/auth/ResultAuth?code&scopes&state`.
3. Controller decode state → đổi `code` lấy token (`SaveAuthorization`) → lưu `Authorization` → redirect về `callbackUrl + token` (CRM hoàn tất kết nối).

## Bảo mật / lỗ hổng (auth comment)
- `[SBAuthorize()]` ở controller **bị comment** (`AuthorizationController.cs:11`) → cả 2 endpoint **không gác phân quyền**. `GetAuthorizationUrl` lẽ ra phải internal/SBAuthorize (chỉ caller hợp lệ mới sinh được URL ủy quyền).
- `ResultAuth` bắt buộc anonymous (TikTok gọi) nhưng **không verify `state`/CSRF** ngoài việc decode → kẻ tấn công có thể chèn `state` giả để redirect token tới `callbackUrl` tùy ý (open-redirect / chiếm token). Cần ký/validate state và whitelist callbackUrl.
- Trong nhánh anonymous, `CurrentUser` trống → `SaveAuthorization(code, CurrentUser)` lưu token thiếu chủ thể.

## Lỗi đã biết / lưu ý kỹ thuật
- `AuthorizationController.cs:11` — `[SBAuthorize()]` controller bị comment → mất phân quyền.
- `AuthorizationController.cs:50,54` — nhánh `userConnectId` redirect URL rỗng → re-auth chưa hoạt động.
- `AuthorizationController.cs:58` — `SaveAuthorization(code, CurrentUser)` với `CurrentUser` rỗng (anonymous callback).
- `AuthorizationController.cs:48` — `dataTiktok` (base64 code) tạo ra nhưng không dùng → dead.
