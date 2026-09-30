---
controller: ShopeeConnectController
module: marketing
service: WarehouseProduct
man_hinh_ui: "Marketing > Kết nối sàn > Shopee (nút 'Kết nối Shopee' khởi tạo OAuth authorize)"
muc_dich: "Điểm vào kết nối gian hàng Shopee qua OAuth: trả về URL ủy quyền (authorization url) để redirect người dùng sang Shopee. Bản hiện tại là STUB (trả cứng 'true'), logic thật nằm ở EcommerceConnectService.CRM_GetAuthorizationUrl chưa được wire."
alias: ["ShopeeConnect", "kết nối Shopee", "OAuth Shopee", "ủy quyền Shopee", "connect gian hàng Shopee"]
auth: internal
state_machine: []
deprecated: false
entities: [Authorization, UnitConnect]
stored_procs: []
depends:
  - "EcommerceConnectService (IEcommerceConnectService.CRM_GetAuthorizationUrl — đang COMMENT, chưa inject)"
  - "External ecommerce gateway (POST {GetUrlEcommerce()}crm/auth/GetAuthorizationUrl qua _clientHelper)"
  - "Core/TienIch (MaTienIchSCRM.TI_Shopee — đơn vị phải mua tiện ích Shopee)"
cross_module_in: []
cross_module_out:
  - "External ecommerce-connect gateway (Shopee OAuth) — nơi redirect ủy quyền và sau đó callback lưu token"
  - "UnitConnect / WarehouseConnect / ProductConnect / AdsUnitConnect — sau khi ủy quyền xong, đồng bộ gian hàng/kho/sản phẩm qua các controller này (cùng IEcommerceConnectService)"
---

# ShopeeConnectController — Kết nối gian hàng Shopee (OAuth)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Khởi tạo kết nối Shopee | Kết nối Shopee | GET `api/ShopeeConnect/ConnectShopee` | `ShopeeConnectController_ConnectShopee` ("Kết nối Shopee" — DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-QL-MKT, DV-MKT-LEAD, DV-MKT) | internal | Đơn vị có tiện ích `TI_Shopee` (gate qua `[TienIch(..., MaTienIchSCRM.TI_Shopee)]`) | **Đúng (kỳ vọng cuối):** gọi `EcommerceConnectService.CRM_GetAuthorizationUrl(reqData, CurrentUser)` → external gateway `crm/auth/GetAuthorizationUrl` → trả authorization url để FE redirect sang Shopee. **Hiện tại (stub):** bỏ qua service, trả cứng `MethodResult<string>.ResultWithData("true")`. | (chưa nối EF/HTTP — gọi service bị comment) | **STUB** — `ShopeeConnectController.cs:30-31`: call service bị comment, trả cứng `"true"`; chưa nhận `reqData`, chưa inject service. |

## Quy tắc nghiệp vụ (gating)
- **Tiện ích gate:** `[TienIch("ShopeeConnectController", "ShopeeConnect", MaTienIchSCRM.TI_Shopee)]` ở controller-level → đơn vị chưa mua tiện ích Shopee thì không vào được.
- **Phân quyền:** `[SBAuthorize("ShopeeConnectController_ConnectShopee", ...)]` chỉ cho 6 role marketing/admin (AD-TONG/CN/MCN, QL-MKT, MKT-LEAD, MKT). Trùng pattern với `TiktokConnectController.ConnectTiktok`.

## Endpoint nội bộ (private / service-to-service)
- Controller này KHÔNG có endpoint private. Khi hoàn thiện, nó chỉ là 1 trigger OAuth; toàn bộ CRUD/đồng bộ thật đi qua các sibling controller cùng `IEcommerceConnectService`:
  - `AuthorizationController` — lưu token sau callback (`CRM_SaveAuthorization`).
  - `UnitConnectController` — map gian hàng Shopee ↔ đơn vị CRM (`CRM_GetListUnitConnect` / `CRM_SaveUnitConnect` / `CRM_DeleteUnitConnect`).
  - `WarehouseConnectController` — map kho.
  - `ProductConnectController` / `ProductCustomController` — đồng bộ & ghép sản phẩm (`CRM_SyncProductEcomm`, `CRM_ConnectProductAuto`).
  - `AdsUnitConnectController` / `CampaignAdsController` — kết nối quảng cáo.

## Quan hệ
- **Gọi sang:** (kỳ vọng) `EcommerceConnectService.CRM_GetAuthorizationUrl` → external gateway `{GetUrlEcommerce()}crm/auth/GetAuthorizationUrl` qua `_clientHelper.PostAsync<ResGetAuthorizationUrl>`. Hiện chưa inject service nào (ctor `IServiceProvider inject` không dùng).
- **Dữ liệu (entities):** OAuth flow lưu vào `Authorization` (token gian hàng) sau callback; gian hàng map qua `UnitConnect`. Controller này không trực tiếp đụng EF.
- **Ripple / đồng bộ:** sau khi ủy quyền thành công, side-effect không nằm ở đây mà ở các sibling connect-controller (đồng bộ kho/sản phẩm/quảng cáo Shopee). Đây chỉ là cửa vào OAuth.

## Luồng xuyên module (kết nối Shopee end-to-end — kỳ vọng)
1. User (role MKT/AD) bấm **Kết nối Shopee** → GET `ConnectShopee` → (đúng) lấy authorization url từ gateway → FE redirect sang Shopee.
2. User ủy quyền trên Shopee → Shopee callback về CRM → `AuthorizationController.CRM_SaveAuthorization` lưu token (entity `Authorization`).
3. Map gian hàng/kho: `UnitConnectController` + `WarehouseConnectController`.
4. Đồng bộ sản phẩm: `ProductConnectController.CRM_SyncProductEcomm` / `CRM_ConnectProductAuto` (kéo SP Shopee về, ghép với SP CRM).
> Hiện chuỗi này GÃY ở bước 1 vì `ConnectShopee` trả cứng `"true"`, chưa trả url thật.

## Lỗi đã biết / lưu ý kỹ thuật
- `ShopeeConnectController.cs:28-32` — **STUB**: `ConnectShopee` trả cứng `ResultWithData("true")`, dòng gọi `_ecommerceConnectService.CRM_GetAuthorizationUrl(...)` bị comment; ctor nhận `IServiceProvider inject` nhưng không resolve service nào → endpoint hiện không có tác dụng thật.
- `ShopeeConnectController.cs:28` — chưa nhận `reqData` (`ReqGetAuthorizationUrl`) trong khi service thật cần param này → khi wire phải bổ sung body + inject `IEcommerceConnectService`.
- Pattern song sinh với `TiktokConnectController.cs:28-32` (cùng stub) → khi hoàn thiện nên dùng chung `EcommerceConnectService`, tránh drift giữa 2 sàn.
- `EcommerceConnectService.cs:130-135` — logic thật `CRM_GetAuthorizationUrl` đã có sẵn (POST tới gateway `crm/auth/GetAuthorizationUrl`), chỉ thiếu wiring ở controller.
