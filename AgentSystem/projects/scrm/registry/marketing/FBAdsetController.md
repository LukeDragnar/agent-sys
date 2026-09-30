---
controller: FBAdsetController
module: marketing
service: Facebook
man_hinh_ui: "Facebook > Quảng cáo / Ngân sách > 'Nhóm quảng cáo' (danh sách adset theo chiến dịch/tài khoản; toggle Bật/Tắt nhóm; sửa Ngân sách hằng ngày nhóm; chỉnh nguồn kết nối)"
muc_dich: "Quản lý NHÓM QUẢNG CÁO (adset, FBAdsetsMG) Facebook: tìm kiếm/phân trang theo chiến dịch hoặc tài khoản, cập nhật nguồn kết nối (IdLanding), bật/tắt nhóm (ripple xuống ads), cập nhật ngân sách hằng ngày của nhóm (đẩy lên Graph API). Đồng bộ adset từ Facebook chạy ở job/consumer."
alias: ["nhóm quảng cáo FB", "FBAdset", "adset Facebook", "ad set", "bật tắt nhóm quảng cáo", "ngân sách nhóm quảng cáo"]
auth: mixed
state_machine: [ACTIVE, PAUSED]
deprecated: false
entities: [FBAdsetsMG, FBAdsMG, FBCampaignMG, FBAccountAds, FBInsightMG, FBUser]
stored_procs: []
depends:
  - "IFBAdsetService (FBAdsetService — MongoDB FBAdsetsMG)"
  - "FacebookAdsManagerHelper.FBOnOffCampaign / FBUpdateCampaign / GetAllAdset (Graph API; on/off+update v24.0, sync v21.0)"
  - "IMongoDbClient (FBAdsetsMG, FBAdsMG, FBCampaignMG, FBAccountAds)"
  - "IRepository<FBUser> (SQL — scoping NguoiTaoUserId)"
  - "IPublishEndpoint (MassTransit → EventFBAds)"
  - "Core/TienIch (MaTienIchSCRM.TI_Facebook)"
cross_module_in:
  - "FBAdsetsConsumer (IConsumer<EventFBAdset>) gọi DongBoAdsetJob / ThemMoiNhomQuangCao"
cross_module_out:
  - "RabbitMQ EventFBAds → FBAdsConsumer → đồng bộ quảng cáo (ads) + cộng ngân sách landingData (Contact)"
  - "Graph API Facebook (on/off + cập nhật ngân sách nhóm thật trên FB)"
---

# FBAdsetController — Nhóm quảng cáo Facebook (Adset)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm nhóm quảng cáo | Tìm kiếm / load grid | POST `api/FBAdset/TimTheoDieuKien` | `FBAdset_TimTheoDieuKien` ("Tìm kiếm nhóm quảng cáo" — DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-MKT, DV-MKT-LEAD, DV-QL-MKT) | internal | màn danh sách nhóm | Lọc Mongo `FBAdsetsMG` theo đơn vị; ưu tiên `LstCampaign` (split `;`), kế tiếp `LstAdsAccount`, ngược lại scoping theo FBUser người tạo (trừ admin); keyword (Name regex / IdAdsetsFB eq); phân trang; join `FBCampaignMG` + `FBAccountAds` lấy tên + `IdLanding` fallback (adset→campaign→account). | Mongo (FBAdsetsMG, FBCampaignMG, FBAccountAds) | Điều kiện admin `!=true \|\| ==false` (`FBAdsetService.cs:287`) — dễ luôn-đúng, cần review. |
| 2 | Cập nhật thông tin (nguồn kết nối) | Chỉnh sửa / Lưu | POST `api/FBAdset/CapNhatThongTin` | `FBAdset_CapNhatThongTin` ("Chỉnh sửa thông tin" — 6 role marketing) | internal | có id adset | Set `IdLanding` cho `FBAdsetsMG`. | Mongo (FBAdsetsMG) | **BUG:** guard null/not-found thiếu `return` (`FBAdsetService.cs:247,249`). |
| 3 | Bật / Tắt nhóm quảng cáo | Toggle ACTIVE/PAUSED | POST `api/FBAdset/OnOffAdset` | (none — `[SBAuthorize()]` rỗng) | public | có adset + ad account | Gọi Graph API `FBOnOffCampaign(IdAdsetsFB, ACTIVE/PAUSED, v24.0)`. Thành công → set `adset.Status`; **ripple xuống ads**: adset ACTIVE → mỗi ad `EffectiveStatus = ad.Status`, ngược lại `ADSET_PAUSED`. Lỗi → `Error_user_msg`. | Graph API + Mongo (cascade FBAdsMG) | `[SBAuthorize()]` rỗng. Thông báo lỗi dùng chữ "chiến dịch" cho adset (copy-paste) `FBAdsetService.cs:355`. |
| 4 | Cập nhật ngân sách nhóm | Sửa ngân sách hằng ngày nhóm | POST `api/FBAdset/UpdateNganSachAdset` | (none — `[SBAuthorize()]` rỗng) | public | có adset + `DailyDudget != null` | Gọi Graph API `FBUpdateCampaign(IdAdsetsFB, DailyDudget, v24.0)`. Thành công → set `adset.DailyBudget = DailyDudget`. `null` → "Ngân sách không được để trống". | Graph API + Mongo (FBAdsetsMG) | DTO sai chính tả `DailyDudget`. Method controller đặt tên nhầm `UpdateNganSachCampaign` cho route `UpdateNganSachAdset` (`FBAdsetController.cs:44`). `[SBAuthorize()]` rỗng. |

## Quy tắc nghiệp vụ (gating / cascade)
- **Tiện ích:** `[TienIch(..., MaTienIchSCRM.TI_Facebook)]` controller-level.
- **Cascade ON/OFF (blueprint trace):** Adset → ACTIVE: mỗi ad con `EffectiveStatus = ad.Status`. Adset → PAUSED: mỗi ad con `EffectiveStatus = ADSET_PAUSED`. (Lưu ý: adset không cascade ngược lên campaign.)
- **Ngân sách:** cập nhật adset budget độc lập; nhưng nếu campaign cha bật ngân sách cấp campaign (CBO) thì `UpdateNganSachCampaign` sẽ RESET budget adset về null (xem FBCampaignController) → 2 chiều có thể đè nhau.
- **Phân quyền:** search/edit gate permission-key; on/off + update ngân sách KHÔNG gate.

## Endpoint nội bộ (private / service-to-service)
- KHÔNG có `[PrivateSBAuthorize]`. `DongBoAdsetJob` / `ThemMoiNhomQuangCao` gọi từ **`FBAdsetsConsumer`** (RabbitMQ `EventFBAdset`).

## Quan hệ
- **Gọi sang:** Graph API (`FBOnOffCampaign`, `FBUpdateCampaign`, `GetAllAdset`). Publish `EventFBAds`.
- **Dữ liệu (entities):** `FBAdsetsMG` (nhóm QC: `DailyBudget`, `BudgetRemaining`, `StartTime`/`EndTime`, `Status`, `EffectiveStatus`), cascade `FBAdsMG`, join `FBCampaignMG` + `FBAccountAds`, ghi `FBInsightMG` khi sync.
- **Ripple / đồng bộ:** sau sync adset → publish `EventFBAds` (đồng bộ ads). On/off + budget đổi thật trên FB rồi cascade trạng thái xuống ads trong Mongo.

## Luồng xuyên module
- **Sync (consumer):** `EventFBAdset` → `FBAdsetsConsumer` → `DongBoAdsetJob`/`ThemMoiNhomQuangCao` → upsert `FBAdsetsMG` + `FBInsightMG` → publish `EventFBAds` → ads.
- **On/Off thủ công:** OnOffAdset → Graph API → cascade ads (`ADSET_PAUSED` / ad.Status) trong Mongo.

## Lỗi đã biết / lưu ý kỹ thuật
- `FBAdsetService.cs:247,249` — `CapNhatThongTin` validation thiếu `return`.
- `FBAdsetService.cs:287` — điều kiện admin `!=true || ==false` dễ luôn-đúng.
- `FBAdsetController.cs:43-44` — route `UpdateNganSachAdset` nhưng method tên `UpdateNganSachCampaign` (đặt nhầm tên); DTO field `DailyDudget` sai chính tả.
- `FBAdsetController.cs:36,42` — `OnOffAdset` + `UpdateNganSachAdset` `[SBAuthorize()]` rỗng.
- `FBAdsetService.cs:355,431` — thông báo lỗi "Không tìm thấy chiến dịch" dùng cho adset (copy-paste nhầm nhãn).
- Graph version trộn lẫn: sync `v21.0`, on/off + update `v24.0`.
