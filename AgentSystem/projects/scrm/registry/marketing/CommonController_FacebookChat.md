---
controller: CommonController
module: marketing
service: FacebookChat
man_hinh_ui: "nội bộ — không có UI (config endpoint dùng bởi FE/client khi khởi tạo Facebook SDK & dropdown)"
muc_dich: "Cung cấp cấu hình hệ thống cho ChatHub: lấy config theo mã, lấy config Facebook App (v1/v2) cho client khởi tạo Facebook SDK/OAuth, và dropdown số server RabbitMQ. Bản client-facing chỉ trả phần config an toàn (ẩn secret/token khi isClientCall)."
alias: ["common config ChatHub", "GetConfigFacebook", "config Facebook app", "lấy appid facebook", "dropdown rabbit server"]
auth: mixed
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends:
  - "ICommonService (FacebookChat.Services.Generated.Common.CommonService)"
  - "IRepository<ConfigSystem> (SQL — bảng cấu hình hệ thống, lọc theo Ma/ConfigCode)"
  - "EnumRabbitServerNumber (dropdown)"
cross_module_in:
  - "FE ChatHub / client khởi tạo Facebook SDK gọi GetConfigFacebook / GetConfigFacebook_V2 (anonymous) để lấy FBAppId/Version/LinkApi"
cross_module_out: []
---

# CommonController (FacebookChat) — Cấu hình chung & config Facebook

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy config theo mã | (nội bộ) | GET `api/Common/GetConfig?ma=` | `[SBAuthorize()]` (đăng nhập, không key cụ thể) | internal | — | `GetConfig(ma)`: query `ConfigSystem` lọc `Ma==ma` (nếu có), trả 1 `ConfigSystemDto`. | EF `ConfigSystem` | Nếu `ma` rỗng → trả bản ghi đầu tiên của bảng (FirstOrDefault không filter). |
| 2 | Lấy config Facebook (v1) | (nội bộ/client) | GET `api/Common/GetConfigFacebook` | — (`[AllowAnonymous]`) | anonymous | client khởi tạo FB SDK | `GetConfigFacebook(isClientCall=false)`: gom `ConfigSystem` theo `ConfigCode.FB_APP_ID/VERSION/LINK_API/MAX_AUTOCREATE_SOURCE`. Vì gọi từ controller truyền `false` → TRẢ CẢ secret/token/accesstoken. | EF `ConfigSystem` | **Lộ secret:** controller dòng 41 gắn cứng `isClientCall=false` → endpoint anonymous trả về `FBAppSecret`, `FBTokenWebhook`, `FBAccessToken`. |
| 3 | Lấy config Facebook (v2) | (nội bộ/client) | GET `api/Common/GetConfigFacebook_V2` | — (`[AllowAnonymous]`) | anonymous | client khởi tạo FB SDK (app v2) | `GetConfigFacebookV2(false)`: như (2) nhưng đọc bộ mã `_V2` (`FB_APP_ID_V2`...). Cũng trả secret v2 do `isClientCall=false`. | EF `ConfigSystem` | Cùng vấn đề lộ secret như (2), bộ config v2. |
| 4 | Dropdown số server Rabbit | (dropdown) | GET `api/Common/DropdownRabbitServerNumber` | — (`[AllowAnonymous]`) | anonymous | — | Enum `EnumRabbitServerNumber` → list `{Id, Text=ToDescription()}`. | (enum, không DB) | — |

## Endpoint nội bộ (private / service-to-service)
- `ICommonService` có nhiều method KHÔNG expose ở controller này (dùng bởi service khác trong FacebookChat: webhook/socket/crawl):
  - `ServerDonViCauHinh_*` — cấu hình đơn vị ↔ server Facebook webhook (qua `FacebookPrivateHelper`, ServerCode từ `FacebookWebhookPrivateSetting`).
  - `XoaRedis/GetAll/LayTatCa/ConfigSystemLayTheoMa/LuuRedis CauHinhHeThong` — quản lý cache Redis cấu hình hệ thống (`CacheKey.CauHinhFacebookChatHeThong`).
  - `GetFbFanpageBy*`, `GetFbUserByFBUserId`, `GetFBPermissionByFanPageId` — cache tra cứu fanpage/user/permission (qua `ICommonCacheService`).
  - `SendSocketChat` → POST `{SocketChatServiceUrl}/api/SocketChat/SocketChatSB` (đẩy socket realtime), `GetGroupNameChiaChat` (tính group socket theo kiểu xem chia chat 1/2/3).
  - `GetNhomQuanLy` (cơ cấu tổ chức), `LayGoiCuocHienTai` → POST `{CoreServiceUrl}/api/GoiCuocChatHub/GoiCuocHienTai_Unit`, `CrawlPhoneChat`/`CrawlPhoneComment` (tách SĐT từ webhook FB).

## Quan hệ
- **Gọi sang:** `ICommonService` → EF `ConfigSystem`; (qua các method nội bộ) external: Socket service, Core service (`/api/QuanTriArea/DonVi/LayTatca`, `/api/GoiCuocChatHub/GoiCuocHienTai_Unit`), Facebook private helper.
- **Dữ liệu (entities):** `ConfigSystem` (Ma/Value/GroupCode/KieuLoai...). Config Facebook map qua hằng `ConfigCode.FB_*` (+ biến thể `_V2`).
- **Ripple / đồng bộ:** controller chỉ đọc config; ripple thực nằm ở các method nội bộ (SendSocketChat đẩy realtime, ServerDonViCauHinh đồng bộ cấu hình server webhook) — không trigger từ 4 endpoint trên.

## Lỗi đã biết / lưu ý kỹ thuật
- `CommonController.cs:37-44` & `:52-59` — `GetConfigFacebook`/`_V2` đặt `[AllowAnonymous]` NHƯNG controller gọi service với `isClientCall=false` (dòng 41, 56) → trả về `FBAppSecret`/`FBTokenWebhook`/`FBAccessToken` cho request không cần auth. Đúng ra client phải truyền `isClientCall=true` để ẩn secret (`CommonService.cs:177-182, 200-205`). **Rủi ro lộ secret app Facebook.**
- `CommonService.cs:146-153` — `GetConfig(ma)` không trả lỗi khi `ma` rỗng, lấy `FirstOrDefault` bản ghi bất kỳ.
- `CommonService.cs:380` — trong `GetFBPermissionByFanPageId`, sau khi build `result` lại gọi `GetCache...` (đáng lẽ `SetCache`) → permission fanpage không được ghi cache (chỉ ảnh hưởng method nội bộ, không qua controller).
- Controller-level `[SBAuthorize()]` nhưng 3/4 endpoint override bằng `[AllowAnonymous]` → auth ở mức `mixed`.
