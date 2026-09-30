---
controller: CommonController
module: marketing
service: Facebook
man_hinh_ui: "nội bộ — không có UI. Cung cấp config Facebook (appId/version/link/secret) cho FE + service khác đọc, và dropdown số rabbit server."
muc_dich: "Trả về cấu hình hệ thống dùng chung cho luồng Facebook: GetConfig (1 mã config), GetConfigFacebook / _V2 (gom các mã FB_APP_ID/VERSION/LINK_API/SECRET/TOKEN/ACCESSTOKEN thành DTO), và dropdown EnumRabbitServerNumber. Server-side bật isClientCall=false → trả luôn cả secret/token."
alias: ["config Facebook", "GetConfigFacebook", "lấy appId FB", "dropdown rabbit server"]
auth: mixed
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends:
  - "ICommonService.GetConfig / GetConfigFacebook / GetConfigFacebookV2 / DropdownRabbitServerNumber"
  - "ConfigSystem repo (TableWithNolock) — đọc bảng config theo Ma"
cross_module_in:
  - "FE / service Facebook khác — đọc appId, version, link Graph, token webhook"
cross_module_out: []
---

# CommonController — Config dùng chung Facebook

> Route `api/Common`. Controller-level `[SBAuthorize()]` (chỉ check đăng nhập), nhưng 4/5 endpoint phủ đè `[AllowAnonymous]` → mixed. Mọi nghiệp vụ ở `ICommonService`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy 1 config theo mã | (không UI) | GET `api/Common/GetConfig?ma=` | (chỉ check đăng nhập) | public | — | `GetConfig(ma)` → query `ConfigSystem` WHERE `Ma==ma`, FirstOrDefault → map `ConfigSystemDto`. | EF ConfigSystem | — |
| 2 | Lấy config Facebook (v1) | (không UI) | GET `api/Common/GetConfigFacebook` | — | anonymous | — | `GetConfigFacebook(isClientCall=false)`: gom `FB_APP_ID/_VERSION/_LINK_API/_MAX_AUTOCREATE_SOURCE`; vì server-call (isClientCall=false) → **trả luôn `FB_APP_SECRET/_TOKEN_WEBHOOK/_ACCESSTOKEN`**. | EF ConfigSystem | **`[AllowAnonymous]` + hard-code `isClientCall=false` → secret/token FB lộ qua endpoint công khai** (`CommonController.cs:37-45`, `CommonService.cs:177-182`). |
| 3 | Lấy config Facebook (v2) | (không UI) | GET `api/Common/GetConfigFacebook_V2` | — | anonymous | — | Như #2 nhưng đọc bộ mã `*_V2` (`FB_APP_ID_V2`...). isClientCall=false → trả secret/token v2. | EF ConfigSystem | Cùng vấn đề lộ secret như #2 (`CommonController.cs:52-60`). |
| 4 | Dropdown số rabbit server | (không UI) | GET `api/Common/DropdownRabbitServerNumber` | — | anonymous | — | Enum `EnumRabbitServerNumber` → list `{Id, Text=Description}`. | (enum, không EF) | — |

## Quy tắc nghiệp vụ
- `isClientCall` hard-code `false` ở cả 3 endpoint config → luôn đi nhánh "server call server" trả thêm secret/token. Nhánh `isClientCall=true` (ẩn secret) tồn tại trong service nhưng controller không kích hoạt.
- `GetConfigFacebookV2` là biến thể app FB thứ 2 (mã config `*_V2`) — dùng cho đơn vị/app dự phòng.

## Quan hệ
- **Gọi sang:** `ICommonService` (đọc bảng `ConfigSystem`).
- **Dữ liệu (entities):** `ConfigSystem` (đọc theo `Ma`).
- **Ripple / đồng bộ:** không ghi; chỉ cấp config cho FE + helper Graph của các controller FB khác.

## Lỗi đã biết / lưu ý kỹ thuật
- `CommonController.cs:37,52` — `GetConfigFacebook`/`_V2` `[AllowAnonymous]` nhưng `isClientCall=false` → trả `FBAppSecret`/`FBTokenWebhook`/`FBAccessToken` cho mọi caller chưa đăng nhập (rò rỉ bí mật).
- `CommonController.cs:23` — `GetConfig` chỉ `[SBAuthorize()]` (không key) → ai đăng nhập cũng đọc bất kỳ mã config nào.
