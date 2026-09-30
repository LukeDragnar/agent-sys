# Dev — Kết nối Facebook

> 3 controller, 2 service BE: **FacebookChat** (FBFanpage, FBPost — tiện ích `TI_ChatHub`) và **Facebook** (AdsAccount — tiện ích `TI_Facebook`). FE Angular 15 + ng-zorro (module ChatHub / Facebook Marketing). Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Khu vực | Ghi chú |
|---|---|---|
| Cấu hình fanpage (đồng bộ / bật sử dụng / kích hoạt / kiểm tra token) | ChatHub | đăng nhập FB → SyncPageFB_V2 → cấu hình + kích hoạt page |
| Quản lý bài post (list / gắn landing / chi tiết) | ChatHub | lọc theo page + trạng thái gắn landing |
| Tài khoản quảng cáo (list / thêm / gắn nguồn) | Facebook > Quảng cáo / Ngân sách | màn `Ads Account` |

> Route FE cụ thể: *cần trace FE* (chưa xác minh component path trong session này).

## Endpoint / Controller (trace registry)

**FBFanpageController** — service FacebookChat, `[TienIch("FBFanpageController","Quản lý fanpage FB", MaTienIchSCRM.TI_ChatHub)]`:
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Đồng bộ fanpage | POST `api/FBFanpage/SyncPageFB_V2` | `FBFanpage_FacebookChat_SyncPageFB` |
| Đồng bộ tin nhắn thủ công | GET `api/FBFanpage/DongBoMessageThuCong?pageId&access_token` | `[SBAuthorize]` rỗng |
| Lưu cấu hình fanpage | POST `api/FBFanpage/CapNhatThongTin` | `FBFanpage_FacebookChat_CapNhatThongTin` |
| Kích hoạt / hủy | POST `api/FBFanpage/KichHoatFanPage/{pageId:Guid}/{isConfirm:bool}` | `[SBAuthorize]` rỗng (key cũ `FBFanpage_KichHoatFanPage` đã comment) |
| Get / Update landing | GET `GetLandingIdByPageId?pageId` · POST `UpdateLandingFanpage?pageId&landingId` | `[SBAuthorize]` rỗng |
| Kiểm tra token | GET `api/FBFanpage/Debug_Token?pageId` | `FBFanpage_FacebookChat_Debug_Token` |
| Đếm page active | GET `SoPageDangKichHoat` · GET `SoPageDangKichHoatPrivate/{idDonVi:Guid}` | `[SBAuthorize]` rỗng |
| Dropdown page | GET `api/FBFanpage/DropDownFanpage` | `[SBAuthorize]` rỗng |

**FBPostController** — service FacebookChat, `[TienIch("FBPostController","Quản lý bài post Facebook", MaTienIchSCRM.TI_ChatHub)]`:
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Tìm/lọc bài post | POST `api/FBPost/TimTheoDieuKienAsync` | `FBPost_FacebookChat_TimTheoDieuKienAsync` |
| Lưu cấu hình bài post | POST `api/FBPost/CapNhatThongTin` | `FBPost_FacebookChat_CapNhatThongTin` |
| Xem chi tiết bài post | POST `api/FBPost/GetPostByPostId?pageId&postId` | `[SBAuthorize]` rỗng |

**AdsAccountController** — service Facebook, `[TienIch("AdsAccountController","Quản lý tài khoản quảng cáo FB", MaTienIchSCRM.TI_Facebook)]`:
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Thêm tài khoản ads (kéo từ FB) | GET `api/AdsAccount/ThemMoiAdsAccount?fbUserId=...` | `[SBAuthorize()]` rỗng |
| Tìm tài khoản ads | POST `api/AdsAccount/TimTheoDieuKien` | `AdsAccount_TimTheoDieuKien` (DV-AD-TONG/CN/MCN, DV-MKT, DV-MKT-LEAD, DV-QL-MKT) |
| Cập nhật nguồn kết nối | POST `api/AdsAccount/CapNhatThongTin` | `AdsAccount_CapNhatThongTin` (cùng 6 role) |

## Entities
- **`FBFanpage`** — page đã đồng bộ: token, `IsSuDung`, `IsActive`, `IdLanding`, `FanPageId`, đơn vị/chi nhánh. State: ChuaSuDung / DangSuDung / ChuaKichHoat / DaKichHoat.
- **`FBPermission`** — quyền page theo FB Tasks; **`FBUserFanpage`** — map user ↔ page; **`FBUser`** — tài khoản FB đã kết nối (chứa AccessToken).
- **`FBPost`** — bài viết page: `IsSuDung`, `IdLanding`, FanPageId/PostId/Content; media lưu Mongo `FBPostAttachmentMG`, cache Redis `FBChatPost`.
- **`FBAccountAds`** (Mongo) — tài khoản quảng cáo: `Access_Token`, `IdLanding`, `IsProcess`, `LastTimeSync`; **`FBPermissionAdAccount`** — quyền user trên ad account.

## Luồng code
- **SyncPageFB_V2** (`FBFanpageService`): lặp Graph `me/accounts` (phân trang) → đổi long-lived token → upsert FBFanpage (mới: IsSuDung=true, IsActive=false) + FBPermission + FBUserFanpage; page mới publish RabbitMQ (`IPublishEndpointContactReceiveFB`) đồng bộ tin nhắn cũ.
- **CapNhatThongTin (page)**: validate `IsSuDung ⇒ (DongBoTinNhan||DongBoBinhLuan) && IdLanding`; đổi IsSuDung → `AddFanpageServer` (service FacebookWebhook) + `SubcrisbeUnsubscribedTopic` (Graph) + clear cache.
- **KichHoatFanPage**: `LayGoiCuocHienTai` → gate `SoPage <= COUNT(active)`; unique active per FanPageId (isConfirm để tắt page chi nhánh khác) → set IsActive + AddFanpageServer + subscribe webhook.
- **FBPost.TimTheoDieuKienAsync**: join FBUser→FBPermission→FBFanpage→FBPost (row-level quyền theo page); lọc KeyWord / IdPages (split `;`) / TrangThaiGanLanding.
- **GetPostByPostId**: cache → miss → Graph `GetPostFB` (`{pageId}_{postId}`) → lưu media Mongo + insert FBPost (NguoiTao="webhook").
- **AdsAccount.ThemMoiAdsAccount**: `GetFbUserByFBUserId` → Graph `GetAcountAdsInfo` → upsert FBAccountAds + FBPermissionAdAccount → publish `EventFBCampaign` per account (chuỗi account→campaign→adset→ads). Cũng gọi từ `AdsAccountConsumer` (RabbitMQ `EventFBAdsAccount`).

## Bug-prone / lưu ý (path:line — từ registry, CHƯA tự sửa)
- **Đồng bộ fanpage nuốt lỗi:** `FBFanpageService.cs:853-856` (và `:498-501` cho V1) — `catch(Exception){}` rỗng, không log → exception giữa chừng vẫn trả Success rỗng. Khó debug "đồng bộ xong mà không có page".
- **Sync trả null giữa loop:** `FBFanpageService.cs:557-558` — `accounts==null || Count<=0` thì `return null` (không phải MethodResult) → controller trả data null.
- **Token qua query string:** `FBFanpageController.cs:67` `DongBoMessageThuCong` nhận `access_token` qua URL → leak qua log/URL.
- **Parse landing không guard:** `FBFanpageService.cs:1088` `Guid.Parse(landingId)` trong `UpdateLandingFanpgae` (lưu ý typo tên hàm) → throw nếu landingId rỗng/sai.
- **Phân quyền lệch:** `FBFanpageController.cs:124-176` nhiều endpoint (`GetLandingIdByPageId`, `UpdateLandingFanpage`, `KichHoatFanPage`, `SoPageDangKichHoat*`, `DropDownFanpage`) `[SBAuthorize]` rỗng — chỉ check đăng nhập.
- **Post sort vô tác dụng:** `FBPostService.cs:159-166` cả hai nhánh `if/else` đều `OrderBy(NgayTao)` → param sort không có hiệu lực.
- **Post media không guard:** `FBPostService.cs:272-307` `infoPost.Attachments.Data[0]...` truy cập index 0 không kiểm tra rỗng → NRE/IndexOutOfRange khi post không có attachment đúng cấu trúc. `:259` hardcode Graph `v21.0` + appId/secret rỗng thay vì `GetConfigFacebook`.
- **Label quyền post sai ngữ nghĩa:** `FBPost_FacebookChat_CapNhatThongTin` nhãn là "Xem chi tiết" nhưng action là cập nhật.
- **Ads validation thiếu return:** `FBAdsAccountService.cs:282,284` — `CapNhatThongTin` guard `IdLanding null/empty` và `not found` gọi `ResultWithError(...)` nhưng **không** `return` → chạy tiếp, NRE khi account không tồn tại.
- **Ads thêm tài khoản không gate quyền:** `AdsAccountController.cs:24` `ThemMoiAdsAccount` `[SBAuthorize()]` rỗng dù chạy Graph API + publish event nặng (chỉ gate tiện ích).
- **Version Graph drift + timezone thủ công:** `FBAdsAccountService.cs:137,301` (v21/v24 lẫn lộn); `:71,75,99` cộng `AddHours(7)` thủ công rải rác.
- **Endpoint cũ đã comment:** SyncPageFB V1, TimTheoDieuKienAsync (fanpage), Xoa, CapNhatThongTin bản cũ — V2 là bản dùng.

## File source (path:line)
- `FBFanpageController.cs` (BE FacebookChat) — `:67` (token query), `:124-176` (auth rỗng).
- `FBFanpageService.cs` — `:498-501`, `:557-558`, `:853-856`, `:1088`, `:1211-1216`.
- `FBPostService.cs` — `:159-166`, `:259`, `:272-307`.
- `FBAdsAccountService.cs` — `:71,75,99`, `:137,301`, `:258`, `:282,284`.
- `AdsAccountController.cs` — `:24`.
- Registry: `projects/scrm/registry/marketing/FBFanpageController.md`, `FBPostController.md`, `AdsAccountController.md` (+ webhook nodes `FBFanpageServerController_FbWebhook.md`, `FbChatWebhook.md`).
