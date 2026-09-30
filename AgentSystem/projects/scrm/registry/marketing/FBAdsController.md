---
controller: FBAdsController
module: marketing
service: Facebook
man_hinh_ui: "Facebook > Quảng cáo / Ngân sách > 'Quảng cáo' (danh sách ads theo adset/chiến dịch/tài khoản; toggle Bật/Tắt quảng cáo; chỉnh nguồn kết nối/landing)"
muc_dich: "Quản lý QUẢNG CÁO (ad, FBAdsMG) Facebook — đơn vị nhỏ nhất trong cây account→campaign→adset→ad: tìm kiếm/phân trang đa cấp, cập nhật nguồn kết nối (IdLanding), bật/tắt 1 quảng cáo. Đồng bộ ads từ Facebook (job/consumer) còn ghi insight và CỘNG NGÂN SÁCH chi tiêu (Spend) vào SanPhamLandingData bên Contact service."
alias: ["quảng cáo FB", "FBAds", "ad Facebook", "quảng cáo Facebook", "bật tắt quảng cáo", "ads"]
auth: mixed
state_machine: [ACTIVE, PAUSED]
deprecated: false
entities: [FBAdsMG, FBAdCreativeMG, FBInsightMG, FBAdsetsMG, FBCampaignMG, FBAccountAds, FBUser]
stored_procs: []
depends:
  - "IFBAdsService (FBAdsService — MongoDB FBAdsMG / FBAdCreativeMG / FBInsightMG)"
  - "FacebookAdsManagerHelper.FBOnOffCampaign / GetAllAds (Graph API; on/off v24.0, sync v21.0)"
  - "IMongoDbClient (FBAdsMG, FBAdCreativeMG, FBInsightMG, FBAdsetsMG, FBCampaignMG, FBAccountAds)"
  - "IRepository<FBUser> (SQL — scoping NguoiTaoUserId)"
  - "IClientHelper → Contact service (SanPhamLanding: TimTheoIdWithNoAuthen, GetLandingDataPrivate, CapNhat/ThemSanPhamLandingDataPrivate) + Core service (DonVi/GetDonViById) — khi sync job"
cross_module_in:
  - "FBAdsConsumer (IConsumer<EventFBAds>) gọi DongBoAdsJob / ThemMoiQuangCao"
cross_module_out:
  - "Contact service — SanPhamLandingData (cộng dồn ngân sách chi tiêu FB theo landing) — CHỈ ở luồng job DongBoAdsJob (luồng thủ công ThemMoiQuangCao đã comment khối này)"
  - "Core service — DonVi/GetDonViById (lấy TenDonVi khi tạo mới landingData)"
  - "Graph API Facebook (bật/tắt quảng cáo thật)"
---

# FBAdsController — Quảng cáo Facebook (Ad)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm quảng cáo | Tìm kiếm / load grid | POST `api/FBAds/TimTheoDieuKien` | `FBAds_TimTheoDieuKien` ("Tìm kiếm quảng cáo FB" — DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-MKT, DV-MKT-LEAD, DV-QL-MKT) | internal | màn danh sách ads | Lọc Mongo `FBAdsMG` theo đơn vị; ưu tiên `LstAdset` → `LstCampaign` → `LstAdsAccount` (đều split `;`), ngược lại scoping FBUser người tạo (trừ admin); keyword (Name regex / IdAdsFB eq); phân trang; join `FBAdCreativeMG` (bài/creative) + `FBAdsetsMG` + `FBCampaignMG` (tên) + `FBAccountAds` (tên), `IdLanding` fallback ads→adset→campaign→account. | Mongo (FBAdsMG + 4 collection join) | Điều kiện admin `!=true \|\| ==false` (`FBAdsService.cs:538`) dễ luôn-đúng. Join nhiều collection in-memory — nặng nếu data lớn. |
| 2 | Cập nhật thông tin (nguồn kết nối) | Chỉnh sửa / Lưu | POST `api/FBAds/CapNhatThongTin` | `FBAds_CapNhatThongTin` ("Chỉnh sửa thông tin" — 6 role marketing) | internal | có id ad | Set `IdLanding` cho `FBAdsMG`. | Mongo (FBAdsMG) | **BUG:** guard null/not-found thiếu `return` (`FBAdsService.cs:55,57`). |
| 3 | Bật / Tắt quảng cáo | Toggle ACTIVE/PAUSED | POST `api/FBAds/OnOffAds` | (none — `[SBAuthorize()]` rỗng) | public | có ad + ad account | Gọi Graph API `FBOnOffCampaign(IdAdsFB, ACTIVE/PAUSED, v24.0)`. Thành công → set `ad.Status` (ACTIVE/PAUSED), ghi `NguoiCapNhat*`. KHÔNG cascade (ad là lá). Lỗi → `Error_user_msg`. | Graph API + Mongo (FBAdsMG) | `[SBAuthorize()]` rỗng. Thông báo lỗi "Không tìm thấy chiến dịch" cho ad (copy-paste) `FBAdsService.cs:602`. `EffectiveStatus` được comment lại — chỉ cập nhật `Status`. |

## Quy tắc nghiệp vụ (gating)
- **Tiện ích:** `[TienIch(..., MaTienIchSCRM.TI_Facebook)]` controller-level.
- **Phân quyền:** search/edit gate permission-key (6 role); `OnOffAds` KHÔNG gate (chỉ tiện ích).
- **Ad là LÁ:** on/off ad không cascade; nhưng on/off adset/campaign sẽ cascade XUỐNG ad (đặt `ADSET_PAUSED`/`CAMPAIGN_PAUSED` — xem FBAdsetController / FBCampaignController).

## Endpoint nội bộ (private / service-to-service)
- KHÔNG có `[PrivateSBAuthorize]`. `DongBoAdsJob` / `ThemMoiQuangCao` gọi từ **`FBAdsConsumer`** (RabbitMQ `EventFBAds`) — bước CUỐI của chuỗi sync. `DongBoAdsJob` ngoài upsert ad+creative+insight còn gọi Contact để cộng dồn ngân sách chi tiêu vào landingData.

## Quan hệ
- **Gọi sang:** Graph API (`FBOnOffCampaign`, `GetAllAds`); Contact service (SanPhamLanding*) + Core (DonVi) — chỉ trong `DongBoAdsJob`.
- **Dữ liệu (entities):** `FBAdsMG` (quảng cáo: `Status`, `EffectiveStatus`, `IdLanding`), `FBAdCreativeMG` (creative/bài: image/video/link), `FBInsightMG` (số liệu: Spend/Impressions/Clicks/Cpc/Cpm/Ctr/Reach), join `FBAdsetsMG`/`FBCampaignMG`/`FBAccountAds`.
- **Ripple / đồng bộ:** luồng job `DongBoAdsJob` cộng `Spend` vào `SanPhamLandingData` (KenhQuangCao = Facebook=5) bên Contact: nếu landingData ngày đó đã có → cộng dồn `NganSach`; chưa có → tạo mới (lấy `TenDonVi` từ Core). Đây là điểm ripple cross-service THẬT của cây ngân sách FB.

## Luồng xuyên module (đồng bộ ads + cộng ngân sách — cuối chuỗi)
1. `EventFBAds` → `FBAdsConsumer` → (UserLogin null) `DongBoAdsJob` / (có user) `ThemMoiQuangCao`.
2. Kéo ads từ Graph API → với mỗi ad: tìm adset cha (`FBAdsetsMG`) → upsert `FBAdsMG` + `FBAdCreativeMG` + `FBInsightMG`.
3. **Chỉ ở `DongBoAdsJob`:** với mỗi insight, xác định `IdLanding` (ad→adset→account) → gọi Contact `TimTheoIdWithNoAuthen` lấy landing → `GetLandingDataPrivate` theo ngày → có thì cộng `NganSach += Spend` (`CapNhat...`), chưa có thì `Them...` (lấy TenDonVi từ Core). → cập nhật mức chi tiêu QC theo landing.
4. Kết thúc chuỗi (ad là tầng cuối, không publish event tiếp).

## Lỗi đã biết / lưu ý kỹ thuật
- `FBAdsService.cs:55,57` — `CapNhatThongTin` validation thiếu `return`.
- `FBAdsService.cs:538` — điều kiện admin `!=true || ==false` dễ luôn-đúng.
- `FBAdsService.cs:602` — thông báo lỗi "Không tìm thấy chiến dịch" cho ad (copy-paste nhầm nhãn).
- `FBAdsService.cs:428-492` — toàn bộ khối cộng ngân sách landingData trong `ThemMoiQuangCao` (luồng thủ công) bị COMMENT → kéo ads thủ công KHÔNG cộng ngân sách landing (chỉ luồng job mới cộng) → khác behavior giữa sync auto vs manual.
- `FBAdsService.cs:237,260` — parse `Spend`/`Clicks` bằng `double.Parse`/`int.Parse` không guard culture/null → rủi ro FormatException nếu chuỗi rỗng/format lạ.
- `FBAdsController.cs:37` — `OnOffAds` `[SBAuthorize()]` rỗng (đổi state thật trên FB không gate permission-key).
- Graph version trộn lẫn: sync `v21.0`, on/off `v24.0`.
