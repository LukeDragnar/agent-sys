---
controller: FBCampaignController
module: marketing
service: Facebook
man_hinh_ui: "Facebook > Quảng cáo / Ngân sách > 'Chiến dịch' (danh sách chiến dịch FB theo tài khoản quảng cáo; toggle Bật/Tắt chiến dịch; sửa Ngân sách hằng ngày (daily budget); chỉnh nguồn kết nối)"
muc_dich: "Quản lý CHIẾN DỊCH quảng cáo Facebook (FBCampaignMG): tìm kiếm/phân trang theo tài khoản quảng cáo, cập nhật nguồn kết nối (IdLanding), bật/tắt chiến dịch (ripple xuống adset+ads), cập nhật ngân sách hằng ngày chiến dịch (đẩy lên Graph API + xóa budget adset con). Đồng bộ campaign từ Facebook chạy ở job/consumer."
alias: ["chiến dịch FB", "FBCampaign", "campaign Facebook", "chiến dịch quảng cáo Facebook", "bật tắt chiến dịch", "ngân sách chiến dịch"]
auth: mixed
state_machine: [ACTIVE, PAUSED]
deprecated: false
entities: [FBCampaignMG, FBAdsetsMG, FBAdsMG, FBAccountAds, FBInsightMG, FBUser]
stored_procs: []
depends:
  - "IFBCampaignService (FBCampaignService — MongoDB FBCampaignMG)"
  - "FacebookAdsManagerHelper.FBOnOffCampaign / FBUpdateCampaign / GetAllCampaign (Graph API; on/off+update v24.0, sync v21.0)"
  - "IMongoDbClient (FBCampaignMG, FBAdsetsMG, FBAdsMG, FBAccountAds)"
  - "IRepository<FBUser> (SQL — scoping theo NguoiTaoUserId)"
  - "IPublishEndpoint (MassTransit → EventFBAdset)"
  - "Core/TienIch (MaTienIchSCRM.TI_Facebook)"
cross_module_in:
  - "FBCampaignConsumer (IConsumer<EventFBCampaign>) gọi DongBoCampaignJob (job) / ThemMoiCampaign (thủ công)"
cross_module_out:
  - "RabbitMQ EventFBAdset → FBAdsetsConsumer → đồng bộ nhóm quảng cáo (adset) → tiếp tục EventFBAds → ads"
  - "Graph API Facebook (on/off + cập nhật ngân sách thật trên Facebook)"
---

# FBCampaignController — Chiến dịch quảng cáo Facebook

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm chiến dịch | Tìm kiếm / load grid | POST `api/FBCampaign/TimTheoDieuKien` | `FBCampaign_TimTheoDieuKien` ("Tìm kiếm chiến dịch FB" — DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-MKT, DV-MKT-LEAD, DV-QL-MKT) | internal | màn danh sách chiến dịch | Lọc Mongo `FBCampaignMG` theo đơn vị; lọc theo `LstAdsAccount` (split `;`) nếu có, ngược lại scoping theo FBUser của người tạo (trừ admin); keyword (Name regex / IdCampaign eq); phân trang; join `FBAccountAds` lấy `NameAdsAccount` + `IdLanding` fallback. | Mongo (FBCampaignMG, FBAccountAds) | Điều kiện admin `IsAdmin!=true \|\| IsAdminTong()==false \|\| IsAdminHeThong()==false` gần như LUÔN TRUE (`FBCampaignService.cs:250`) → user thường hầu như luôn bị lọc theo FBUser (đúng ý) nhưng admin cũng dính nếu chỉ là 1 loại admin → cần xác nhận. |
| 2 | Cập nhật thông tin (nguồn kết nối) | Chỉnh sửa / Lưu | POST `api/FBCampaign/CapNhatThongTin` | `FBCampaign_CapNhatThongTin` ("Chỉnh sửa thông tin" — 6 role marketing) | internal | có id chiến dịch | Set `IdLanding` cho `FBCampaignMG`, ghi `NguoiCapNhat*`. | Mongo (FBCampaignMG) | **BUG:** guard `IdLanding` null / not-found gọi `ResultWithError` nhưng KHÔNG `return` (`FBCampaignService.cs:305,307`) → có thể NRE. |
| 3 | Bật / Tắt chiến dịch | Toggle ACTIVE/PAUSED | POST `api/FBCampaign/OnOffCampign` | (none — `[SBAuthorize()]` rỗng) | public | có chiến dịch + ad account | Gọi Graph API `FBOnOffCampaign(IdCampaign, ACTIVE/PAUSED, v24.0)`. Thành công → set `Status`+`Effective_status`; **ripple xuống dưới**: ACTIVE → adset `EffectiveStatus=adset.Status`, ad theo adset (ACTIVE→ad.Status, else `ADSET_PAUSED`); PAUSED → tất cả adset `CAMPAIGN_PAUSED` + tất cả ad `CAMPAIGN_PAUSED`. Lỗi → `Error_user_msg`. | Graph API + Mongo (cascade FBAdsetsMG, FBAdsMG) | Route gõ sai chính tả `OnOffCampign` (thiếu 'a') — FE phải gọi đúng route lỗi này. `[SBAuthorize()]` rỗng cho hành động đổi state thật trên FB. |
| 4 | Cập nhật ngân sách chiến dịch | Sửa ngân sách hằng ngày | POST `api/FBCampaign/UpdateNganSachCampaign` | (none — `[SBAuthorize()]` rỗng) | public | có chiến dịch + `DailyDudget != null` | Gọi Graph API `FBUpdateCampaign(IdCampaign, DailyDudget, v24.0)`. Thành công → set `Daily_budget` campaign, và **xóa `DailyBudget` của TẤT CẢ adset con** (set null — vì bật CBO ngân sách ở cấp campaign). `DailyDudget==null` → "Ngân sách không được để trống". | Graph API + Mongo (FBCampaignMG + reset FBAdsetsMG.DailyBudget) | Tên field DTO sai chính tả `DailyDudget` (Dudget). Không kiểm ngưỡng min budget của FB → FB có thể trả lỗi. |

## Quy tắc nghiệp vụ (gating / cascade trạng thái)
- **Tiện ích:** `[TienIch(..., MaTienIchSCRM.TI_Facebook)]` controller-level.
- **Cascade ON/OFF (blueprint cho trace):**
  - Campaign → **ACTIVE**: adset.EffectiveStatus = adset.Status (giữ trạng thái riêng adset); với mỗi ad: adset ACTIVE → ad.EffectiveStatus = ad.Status, ngược lại `ADSET_PAUSED`.
  - Campaign → **PAUSED**: mọi adset.EffectiveStatus = `CAMPAIGN_PAUSED`; mọi ad (lọc theo `IdCampaignMG`).EffectiveStatus = `CAMPAIGN_PAUSED`.
- **Cập nhật ngân sách campaign**: thành công thì RESET `DailyBudget` của mọi adset con về null (chuyển sang ngân sách cấp chiến dịch / CBO).
- **Phân quyền:** search/edit gate permission-key; on/off + update ngân sách KHÔNG gate (chỉ tiện ích).

## Endpoint nội bộ (private / service-to-service)
- KHÔNG có `[PrivateSBAuthorize]`. Service `DongBoCampaignJob` / `ThemMoiCampaign` được gọi từ **`FBCampaignConsumer`** (RabbitMQ `EventFBCampaign`), không expose HTTP.

## Quan hệ
- **Gọi sang:** Graph API (`FBOnOffCampaign`, `FBUpdateCampaign`, `GetAllCampaign`). Publish `EventFBAdset`.
- **Dữ liệu (entities):** `FBCampaignMG` (chiến dịch: `Daily_budget`, `Objective`, `Status`, `Effective_status`, `Is_adset_budget_sharing_enabled`), cascade `FBAdsetsMG` + `FBAdsMG`, join `FBAccountAds`, ghi `FBInsightMG` khi sync.
- **Ripple / đồng bộ:** sau sync campaign → publish `EventFBAdset` (đồng bộ adset). On/off + budget thay đổi trực tiếp trên Facebook qua Graph API rồi cascade trạng thái xuống adset/ads trong Mongo.

## Luồng xuyên module
- **Sync (consumer):** `EventFBCampaign` → `FBCampaignConsumer` → (UserLogin null) `DongBoCampaignJob` / (có user) `ThemMoiCampaign` → upsert `FBCampaignMG` + ghi `FBInsightMG` → publish `EventFBAdset` → adset → ads.
- **On/Off thủ công:** OnOffCampign → Graph API → cascade trạng thái adset+ads trong Mongo (không gọi rabbit).

## Lỗi đã biết / lưu ý kỹ thuật
- `FBCampaignService.cs:305,307` — `CapNhatThongTin` validation thiếu `return` (BUG NRE tiềm ẩn).
- `FBCampaignService.cs:250` — biểu thức điều kiện admin dạng `!=true || ==false` → dễ luôn-đúng, cần review ý đồ scoping.
- `FBCampaignController.cs:38` — route `OnOffCampign` sai chính tả (thiếu 'a'); `FBUpdateNganSachCampaignDto.DailyDudget` sai chính tả ("Dudget").
- `FBCampaignController.cs:37,43` — `OnOffCampign` và `UpdateNganSachCampaign` `[SBAuthorize()]` rỗng — đổi state/ngân sách thật trên FB mà không gate permission-key.
- `FBCampaignService.cs:41` — `DongBoCampaignJob` tính `yesterday` bằng `DateTime.UtcNow` (khác `DateTime.Now` ở Adset/Ads service) → lệch timezone giữa các tầng sync.
- Helper Graph version trộn lẫn: sync `v21.0`, on/off + update `v24.0` (`FBCampaignService.cs:47,338,442`).
