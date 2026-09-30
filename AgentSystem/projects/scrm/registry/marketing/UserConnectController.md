---
controller: UserConnectController
module: marketing
service: TiktokWebhook
man_hinh_ui: "nội bộ — không có UI riêng. Tra cứu trạng thái kết nối user ↔ gian hàng TikTok (phục vụ màn kết nối gian hàng / re-auth)."
muc_dich: "Tìm danh sách UserConnect (liên kết user SCRM ↔ tài khoản/gian hàng TikTok) theo điều kiện lọc."
alias: ["UserConnect Tiktok", "kết nối user TikTok", "TimTheoDieuKien UserConnect", "danh sách kết nối TikTok"]
auth: internal      # ý đồ internal; thực tế anonymous vì [SBAuthorize()] bị comment
state_machine: []
deprecated: false
entities: [UserConnect]
stored_procs: []
depends:
  - "IUserConnectService (TiktokWebhook.Service.Generated) — TimTheoDieuKienAsync"
  - "BaseController.CurrentUser"
cross_module_in:
  - "Core SCRM / màn kết nối gian hàng TikTok — query trạng thái kết nối"
cross_module_out:
  - "AuthorizationController (re-auth) — userConnectId dùng để pack state OAuth"
---

# UserConnectController — Liên kết user ↔ gian hàng TikTok

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm kết nối theo điều kiện | (danh sách kết nối) | POST `api/tiktok/UserConnect/TimTheoDieuKienAsync` | — (`[SBAuthorize()]` controller bị comment) | **internal** (kỳ vọng), thực tế **anonymous** | khi mở màn kết nối / re-auth | `userConnectService.TimTheoDieuKienAsync(reqData, CurrentUser)` → trả danh sách `UserConnect` lọc theo `ReqGetUserConnects`. | service `IUserConnectService` | `[SBAuthorize()]` controller bị comment (`:10`) → không gác quyền; `CurrentUser` rỗng nếu anonymous. |

## Quan hệ
- **Gọi sang:** `IUserConnectService.TimTheoDieuKienAsync`.
- **Dữ liệu (entities):** `UserConnect` (map user SCRM ↔ tài khoản TikTok).
- **Ripple / đồng bộ:** `userConnectId` từ kết quả này được `AuthorizationController.ResultAuth` dùng để phân biệt nhánh re-auth.

## Bảo mật / lưu ý
- `[SBAuthorize()]` controller **bị comment** (`UserConnectController.cs:10`) → endpoint không gác phân quyền dù trả dữ liệu kết nối gian hàng (lộ thông tin liên kết TikTok ↔ user nếu để anonymous). Cần bật lại SBAuthorize.

## Lỗi đã biết / lưu ý kỹ thuật
- `UserConnectController.cs:10` — `[SBAuthorize()]` bị comment.
- `UserConnectController.cs:25` — truyền `CurrentUser` xuống service nhưng controller hiện anonymous → user context rỗng.
