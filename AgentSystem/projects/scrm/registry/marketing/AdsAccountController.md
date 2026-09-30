---
controller: AdsAccountController
module: marketing
service: Facebook
man_hinh_ui: "Facebook > Quảng cáo / Ngân sách > 'Tài khoản quảng cáo' (danh sách Ads Account đã kết nối; nút 'Thêm tài khoản quảng cáo' theo fbUserId; nút 'Chỉnh sửa' chọn nguồn kết nối/landing)"
muc_dich: "Quản lý TÀI KHOẢN QUẢNG CÁO Facebook (FBAccountAds) của đơn vị: kéo (sync) ads account từ Facebook Graph API về MongoDB, tìm kiếm/phân trang theo đơn vị + người kết nối, cập nhật nguồn kết nối (IdLanding). Là điểm khởi đầu chuỗi đồng bộ ngân sách FB (account → campaign → adset → ads qua RabbitMQ)."
alias: ["tài khoản quảng cáo FB", "Ads Account", "AdsAccount", "tài khoản ads Facebook", "ad account", "quản lý ngân sách Facebook"]
auth: mixed
state_machine: []
deprecated: false
entities: [FBAccountAds, FBPermissionAdAccount, FBUser]
stored_procs: []
depends:
  - "IFBAdsAccountService (FBAdsAccountService — MongoDB collection FBAccountAds / FBPermissionAdAccount)"
  - "ICommonService.GetFbUserByFBUserId (lấy FBUser + AccessToken theo fbUserId)"
  - "FacebookAdsManagerHelper.GetAcountAdsInfo (Graph API v21.0, https://graph.facebook.com/)"
  - "IRepository<FBUser> (SQL Server — join lấy FBTen, lọc theo NguoiTaoUserId)"
  - "IMongoDbClient (FBAccountAds collection)"
  - "IPublishEndpoint (MassTransit → EventFBCampaign khởi động chuỗi sync)"
  - "Core/TienIch (MaTienIchSCRM.TI_Facebook — đơn vị phải mua tiện ích Facebook)"
cross_module_in:
  - "AdsAccountConsumer (IConsumer<EventFBAdsAccount>) gọi service ThemMoiAdsAccount khi nhận event sync tài khoản"
cross_module_out:
  - "RabbitMQ EventFBCampaign → FBCampaignConsumer → FBCampaignController/Service đồng bộ chiến dịch (chuỗi account→campaign→adset→ads)"
  - "Contact service (SanPhamLanding) — IdLanding gắn ở account làm fallback khi tính ngân sách landingData ở tầng Ads"
---

# AdsAccountController — Tài khoản quảng cáo Facebook (Ads Account)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm tài khoản quảng cáo (kéo từ FB) | Thêm tài khoản quảng cáo | GET `api/AdsAccount/ThemMoiAdsAccount?fbUserId=...` | (none — `[SBAuthorize()]` rỗng) | public (chỉ gate `[TienIch]`) | có FBUser đã kết nối thuộc đơn vị | Lấy FBUser theo `fbUserId` + đơn vị → gọi Graph API `GetAcountAdsInfo` → upsert từng `FBAccountAds` (insert mới + lưu `FBPermissionAdAccount`; nếu có sẵn chỉ refresh `Access_Token`) → publish `EventFBCampaign` cho TỪNG account để chạy chuỗi sync. Trả "Thành công". | EF (FBUser join) + Mongo (FBAccountAds, FBPermissionAdAccount) + Graph API | `[SBAuthorize()]` rỗng — endpoint sync nặng nhưng không gate permission-key (chỉ chặn bằng tiện ích). |
| 2 | Tìm tài khoản quảng cáo | Tìm kiếm / load grid | POST `api/AdsAccount/TimTheoDieuKien` | `AdsAccount_TimTheoDieuKien` ("Tìm kiếm tài khoản quảng cáo" — DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-MKT, DV-MKT-LEAD, DV-QL-MKT) | internal | màn danh sách | Lọc Mongo `FBAccountAds` theo đơn vị + keyword (Name regex / Account_id eq), phân trang `PageInfo`, join SQL `FBUser` lấy `FBTen`. Admin/AdminTong/AdminHeThong thấy toàn đơn vị; user thường chỉ thấy account của FBUser do mình tạo (`NguoiTaoUserId == userLogin.Id`). | Mongo (FBAccountAds) + EF (FBUser join) | Lọc theo người tạo làm trong memory (`rs.Where(...)`) sau khi đã query toàn đơn vị — không tối ưu nếu data lớn. |
| 3 | Cập nhật thông tin (nguồn kết nối) | Chỉnh sửa / Lưu | POST `api/AdsAccount/CapNhatThongTin` | `AdsAccount_CapNhatThongTin` ("Chỉnh sửa thông tin" — DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-MKT, DV-MKT-LEAD, DV-QL-MKT) | internal | có id account | Set `IdLanding` (nguồn kết nối/landing) cho `FBAccountAds`, ghi `NguoiCapNhat*`. Trả "Cập nhật thành công". | Mongo (FBAccountAds) | **BUG validation:** guard `IdLanding==null/empty` và `not found` gọi `MethodResult<bool>.ResultWithError(...)` nhưng KHÔNG `return` → vẫn chạy tiếp, NRE nếu account không tồn tại (`FBAdsAccountService.cs:282,284`). |

## Quy tắc nghiệp vụ (gating)
- **Tiện ích gate:** `[TienIch("AdsAccountController", "Quản lý tài khoản quảng cáo FB", MaTienIchSCRM.TI_Facebook)]` controller-level → đơn vị chưa mua tiện ích Facebook không vào được.
- **Phân quyền không đồng nhất:** `ThemMoiAdsAccount` để `[SBAuthorize()]` rỗng (chỉ gate tiện ích), 2 endpoint còn lại gate permission-key marketing (6 role).
- **Phân tách dữ liệu (data scoping):** admin (IsAdmin/IsAdminTong/IsAdminHeThong) thấy toàn đơn vị; user thường chỉ thấy ads account của các FBUser do mình kết nối.

## Endpoint nội bộ (private / service-to-service)
- Controller này KHÔNG có endpoint `[PrivateSBAuthorize]`. Tuy nhiên service `ThemMoiAdsAccount` cũng được gọi từ **`AdsAccountConsumer`** (RabbitMQ `EventFBAdsAccount`) — cùng logic kéo account từ FB, không qua HTTP.
- `DongBoNganSachJob()` / `GetAllAdsAccount()` có trong service nhưng KHÔNG được controller expose (job nền + dùng nội bộ).

## Quan hệ
- **Gọi sang:** Graph API Facebook (`GetAcountAdsInfo`); Contact service ở tầng dưới (qua Ads sync). Publish `EventFBCampaign`.
- **Dữ liệu (entities):** `FBAccountAds` (tài khoản quảng cáo, chứa `Access_Token`, `IdLanding`, `IsProcess`, `LastTimeSync`), `FBPermissionAdAccount` (quyền user trên ad account), `FBUser` (SQL, lấy tên + scoping).
- **Ripple / đồng bộ:** mỗi lần thêm/sync account → publish `EventFBCampaign` (per account) → kích hoạt chuỗi đồng bộ campaign → adset → ads.

## Luồng xuyên module (đồng bộ ngân sách FB — account đầu chuỗi)
1. User bấm **Thêm tài khoản quảng cáo** (hoặc `AdsAccountConsumer` nhận `EventFBAdsAccount`) → `ThemMoiAdsAccount` kéo account từ Graph API, upsert `FBAccountAds` + lưu permission.
2. Mỗi account → publish `EventFBCampaign` → `FBCampaignConsumer` → `DongBoCampaignJob` (job) / `ThemMoiCampaign` (thủ công).
3. Campaign xong → publish `EventFBAdset` → adset → publish `EventFBAds` → ads (tầng ads ghi insight + cộng ngân sách vào Contact/SanPhamLandingData).
4. Job nền `DongBoNganSachJob()` định kỳ chọn account đến hạn (theo `LastTimeSync`/`IsProcess`), khóa `IsProcess=1`, publish `EventFBCampaign` để re-sync.

## Lỗi đã biết / lưu ý kỹ thuật
- `FBAdsAccountService.cs:282,284` — `CapNhatThongTin`: validation thiếu `return` → guard vô hiệu, có thể NRE khi account không tồn tại.
- `AdsAccountController.cs:24` — `ThemMoiAdsAccount` `[SBAuthorize()]` rỗng, endpoint chạy Graph API + publish event (nặng) mà không gate permission-key.
- `FBAdsAccountService.cs:137,301,...` — token Graph API version hard-code rải rác (`v21.0` ở sync, `v24.0` ở on/off ở các service khác) → version drift giữa các luồng FB.
- `FBAdsAccountService.cs:258` — điều kiện phân quyền `IsAdmin == true || IsAdminTong() || IsAdminHeThong()` ở đây dùng OR (đúng ý "là admin bất kỳ loại"); nhưng các service Ads/Adset/Campaign dùng biểu thức `!=true || ...==false` dễ gây luôn-đúng (xem các node tương ứng).
- `FBAdsAccountService.cs:71,75,99` — cộng `AddHours(7)` thủ công để chỉnh timezone (UTC+7) rải rác trong job → rủi ro lệch giờ.
