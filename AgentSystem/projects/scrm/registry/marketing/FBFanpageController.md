---
controller: FBFanpageController
module: marketing
service: FacebookChat
man_hinh_ui: "ChatHub > Cấu hình Fanpage (đồng bộ fanpage sau khi đăng nhập FB, bật/tắt sử dụng, gắn nguồn landing, kích hoạt fanpage, kiểm tra token, dropdown chọn page)"
muc_dich: "Quản lý vòng đời fanpage Facebook trong ChatHub: đồng bộ danh sách page từ tài khoản FB về CRM, cập nhật cấu hình (đồng bộ tin nhắn/bình luận, landing), kích hoạt/hủy kích hoạt page (gating gói cước + webhook), kiểm tra access_token."
alias: ["fanpage FB", "quản lý fanpage", "kết nối page facebook", "kích hoạt fanpage", "đồng bộ page", "cấu hình page ChatHub"]
auth: mixed
state_machine: [ChuaSuDung, DangSuDung, ChuaKichHoat, DaKichHoat]
deprecated: false
entities: [FBFanpage, FBPermission, FBUser, FBUserFanpage]
stored_procs: []
depends:
  - "IFBFanpageService (FacebookChat.Services.Generated) — toàn bộ logic fanpage"
  - "IDongBoChatFBService.DongBoTinNhanChat — đồng bộ tin nhắn thủ công + qua RabbitMQ"
  - "ICommonService — GetConfigFacebook/V2, cache fanpage/permission, LayGoiCuocHienTai (gói cước ChatHub)"
  - "IFBPermissionService / IFBUserFanpageService — lưu quyền page + map user↔fanpage khi sync"
  - "FacebookHelper / FacebookPrivateHelper — gọi Graph API (ExchangeLongLivedAccessToken, SubcrisbeUnsubscribedTopic, FBDebugAccessToken) + AddFanpageServer (service FacebookWebhook)"
cross_module_in:
  - "Core/GoiCuoc (ChatHub) — gating số page tối đa qua LayGoiCuocHienTai khi KichHoatFanPage"
cross_module_out:
  - "FacebookWebhook service — FacebookPrivateHelper.AddFanpageServer (đăng ký fanpage lên server webhook chung) + SubcrisbeUnsubscribedTopic lên Graph API"
  - "RabbitMQ (IPublishEndpointContactReceiveFB) — SyncPageFB_V2 publish FBDongBoMessageRabbitMQDto để đồng bộ tin nhắn cũ khi fanpage mới insert"
  - "DongBoChatFBService → Conversation/FBCustomer (Mongo) — đồng bộ hội thoại + tạo contact/khách hàng từ tin nhắn FB"
---

# FBFanpageController — Quản lý fanpage Facebook (ChatHub)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Đồng bộ fanpage từ tài khoản FB | Đồng bộ fanpage | POST `api/FBFanpage/SyncPageFB_V2` | `FBFanpage_FacebookChat_SyncPageFB` ("Đồng bộ fanpage" — AD-TONG/CN/MCN, MKT, MKT-LEAD, QL-MKT, QL-SALE, SALE, SALE-LEAD, TRUC-PAGE) | public | Sau khi user đăng nhập FB (có `req.UserId`) | Lặp Graph API `me/accounts` (phân trang) → mỗi page: đổi long-lived token, **upsert** `FBFanpage` (mới: `IsSuDung=true`, `IsActive=false`), upsert `FBPermission` (TaskStatus theo role FB), upsert `FBUserFanpage`. Page mới → publish RabbitMQ đồng bộ tin nhắn cũ. Trả list page (AccessToken=""). | EF upsert FBFanpage/FBPermission/FBUserFanpage + Graph API | Toàn bộ `SyncPageFB_V2` bọc `try{}catch(ex){}` **nuốt lỗi** (`FBFanpageService.cs:853-856`) → exception giữa chừng vẫn trả Success rỗng. `accounts==null/empty` giữa loop `return null` (`:557-558`) → response data null. |
| 2 | Đồng bộ tin nhắn thủ công | (nút sync tin nhắn) | GET `api/FBFanpage/DongBoMessageThuCong?pageId&access_token` | (không key — `[SBAuthorize]` rỗng) | public | Có pageId + access_token hợp lệ | Gọi `_dongBoChatFBService.DongBoTinNhanChat` → kéo conversation+message từ Graph, đồng bộ Conversation/FBCustomer (Mongo), set `IsSyncOldDone=true` cho fanpage. | Graph API + Mongo (Conversation/FBCustomer) | `access_token` nhận qua query string (log/URL leak token). |
| 3 | Lưu cấu hình fanpage | Lưu / Cập nhật | POST `api/FBFanpage/CapNhatThongTin` | `FBFanpage_FacebookChat_CapNhatThongTin` ("Chỉnh sửa thông tin" — AD-TONG/CN/MCN, MKT, MKT-LEAD, QL-MKT) | public | Page đã sync về | Validate: bật `IsSuDung` thì phải bật ít nhất 1 trong (`DongBoTinNhan`/`DongBoBinhLuan`) và phải có `IdLanding`. Đổi `IsSuDung` → `AddFanpageServer` (server webhook) + `SubcrisbeUnsubscribedTopic` (Graph). Lưu EF + clear cache. | EF FBFanpage + Graph + FacebookWebhook | Chỉ update khi user **có quyền page** (join FBUser/FBPermission); sai quyền → "Không tìm thấy fanpage". |
| 4 | Kích hoạt / Hủy kích hoạt fanpage | Kích hoạt | POST `api/FBFanpage/KichHoatFanPage/{pageId:Guid}/{isConfirm:bool}` | (không key — `[SBAuthorize]`; key cũ `FBFanpage_KichHoatFanPage` đã comment) | public | Page thuộc đơn vị+chi nhánh user | **Hủy** (đang Active, isConfirm=false): unsubscribe webhook, `IsActive=false`. **Kích hoạt**: nếu page đang Active ở chi nhánh khác → cần `isConfirm=true` mới tắt page kia (else trả code `EXISTED`). Gate gói cước: `goiCuoc.SoPage <= total page active` → chặn. OK → `IsActive=true` + AddFanpageServer + subscribe webhook. | EF FBFanpage + Core/GoiCuoc + FacebookWebhook + Graph | Gating số page là **invariant**: `SoPage` từ `LayGoiCuocHienTai`; lỗi gọi gói cước → trả error (`FBFanpageService.cs:1211-1216`). |
| 5 | Gắn / lấy nguồn landing | (chọn nguồn) | GET `api/FBFanpage/GetLandingIdByPageId?pageId` · POST `api/FBFanpage/UpdateLandingFanpage?pageId&landingId` | (không key) | public | Page đã sync | Get: trả `IdLanding` (lỗi nếu page chưa gắn nguồn). Update: set `IdLanding` cho **tất cả** bản ghi page cùng FanPageId trong đơn vị + clear cache. | EF FBFanpage | `UpdateLandingFanpage`: `Guid.Parse(landingId)` không guard → landingId rỗng/sai format throw. |
| 6 | Kiểm tra access_token | Debug Token | GET `api/FBFanpage/Debug_Token?pageId` | `FBFanpage_FacebookChat_Debug_Token` ("Kiểm tra Access_Token" — AD-TONG/CN/MCN, MKT, MKT-LEAD, QL-MKT, QL-SALE, SALE, SALE-LEAD, TRUC-PAGE) | public | Page thuộc đơn vị | Gọi Graph `debug_token` bằng **App Access Token** (`{AppId}|{AppSecret}`, KHÔNG dùng page token). `IsConnected = Is_Valid && App_Id==config.FBAppId`. Trả chi tiết scope/expires + message kết nối. | EF FBFanpage + Graph debug_token | — |
| 7 | Đếm page đang kích hoạt | (badge số page) | GET `api/FBFanpage/SoPageDangKichHoat` · GET `api/FBFanpage/SoPageDangKichHoatPrivate/{idDonVi:Guid}` | (không key) | public | — | Đếm `FBFanpage` `IsActive=true` theo đơn vị (current hoặc idDonVi truyền vào — endpoint Private cho service-to-service). | EF FBFanpage `CountAsync` | `SoPageDangKichHoatPrivate` tên "Private" nhưng vẫn `[SBAuthorize]` (không anonymous), nhận idDonVi tự do → caller nội bộ phải truyền đúng. |
| 8 | Dropdown chọn fanpage | (combobox page) | GET `api/FBFanpage/DropDownFanpage` | (không key) | public | — | Trả list fanpage của đơn vị, lọc theo `IdChiNhanh` của user (lấy từ cache `GetFbFanpageByIdDonVi`). | Cache (EF nền) | — |

## Quy tắc nghiệp vụ (gating / ngưỡng)
- **Tiện ích gate (controller-level):** `[TienIch("FBFanpageController","Quản lý fanpage FB", MaTienIchSCRM.TI_ChatHub)]` → đơn vị chưa mua tiện ích ChatHub không vào được.
- **Gating gói cước khi kích hoạt** (`KichHoatFanPage`): `goiCuoc = LayGoiCuocHienTai(userLogin)`. Nếu page chưa active và `goiCuoc.SoPage <= COUNT(page IsActive=true của đơn vị)` → chặn: "Đơn vị chỉ được kích hoạt tối đa {SoPage} FanPage!". → **invariant** số page active ≤ SoPage gói cước.
- **Unique active per FanPageId:** 1 FanPageId chỉ được Active ở 1 chi nhánh; kích hoạt page đã active ở chi nhánh khác cần `isConfirm=true` (tắt page cũ), else trả code `EXISTED`.
- **Điều kiện bật sử dụng** (`SavePageFB`): `IsSuDung=true` ⇒ phải bật ≥1 đồng bộ (tin nhắn/bình luận) và phải có `IdLanding`.

## Quan hệ
- **Gọi sang:** Graph API (long-lived token, subscribe/unsubscribe webhook topic, debug_token); FacebookWebhook service (`AddFanpageServer`); Core/GoiCuoc (`LayGoiCuocHienTai`); RabbitMQ publish đồng bộ tin nhắn.
- **Dữ liệu (entities):** `FBFanpage` (chính) + `FBPermission` (quyền page theo FB Tasks) + `FBUserFanpage` (map user↔page) + `FBUser`.
- **Ripple / đồng bộ:** kích hoạt/đổi IsSuDung → đăng ký webhook lên server chung; insert page mới → publish RabbitMQ kéo tin nhắn cũ → tạo `Conversation`/`FBCustomer` (Mongo).

## Luồng xuyên module (kết nối fanpage end-to-end)
1. User đăng nhập FB → POST `SyncPageFB_V2` → upsert FBFanpage/Permission/UserFanpage; page mới publish RabbitMQ đồng bộ tin nhắn cũ.
2. Cấu hình `CapNhatThongTin`: chọn landing + bật đồng bộ → AddFanpageServer + subscribe webhook.
3. `KichHoatFanPage` (isConfirm): gate gói cước → IsActive=true → AddFanpageServer + subscribe webhook.
4. Webhook FB về (service khác) → tin nhắn/bình luận chảy vào ChatHub (FBMessageController đọc).

## Lỗi đã biết / lưu ý kỹ thuật
- `FBFanpageService.cs:853-856` (và `:498-501` cho V1) — `catch(Exception ex){}` rỗng nuốt mọi lỗi sync, không log (logsService đã comment) → khó debug, response trả Success kể cả lỗi giữa chừng.
- `FBFanpageService.cs:557-558` — trong loop sync, `accounts==null || Count<=0` thì `return null` (không phải MethodResult) → controller trả data null.
- `FBFanpageController.cs:67` — `DongBoMessageThuCong` nhận `access_token` qua query string → nguy cơ lộ token qua log/URL.
- `FBFanpageService.cs:1088` — `Guid.Parse(landingId)` trong `UpdateLandingFanpgae` không guard format → throw nếu landingId rỗng/sai.
- `FBFanpageController.cs:124-176` — phần lớn endpoint (`GetLandingIdByPageId`, `UpdateLandingFanpage`, `KichHoatFanPage`, `SoPageDangKichHoat*`, `DropDownFanpage`) dùng `[SBAuthorize]` rỗng (chỉ check đăng nhập, không có permission key riêng).
- Nhiều endpoint cũ (`SyncPageFB` V1, `TimTheoDieuKienAsync`, `CapNhatThongTin` bản cũ, `Xoa`) đã **comment** trong controller → V2 là bản đang dùng.
