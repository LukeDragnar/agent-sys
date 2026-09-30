---
controller: TiktokConnectController
module: marketing
service: WarehouseProduct
man_hinh_ui: "Marketing > Kết nối sàn / Gian hàng TMĐT (màn ecomm-gian-hang-danh-sach, lọc theo EcommerceCode=Tiktok). LƯU Ý: endpoint stub này KHÔNG được FE gọi — nút 'Thêm kết nối/Xác thực' trên UI gọi thẳng EcommerceService.CRM_GetAuthorizationUrl."
muc_dich: "Điểm vào kết nối gian hàng TikTok Shop qua OAuth: kỳ vọng trả authorization url để redirect người dùng sang TikTok ủy quyền. Bản hiện tại là STUB (trả cứng 'true'), logic thật nằm ở EcommerceConnectService.CRM_GetAuthorizationUrl chưa được wire vào controller này."
alias: ["TiktokConnect", "kết nối Tiktok", "kết nối TikTok Shop", "OAuth Tiktok", "ủy quyền Tiktok", "connect gian hàng TikTok"]
auth: internal
state_machine: []
deprecated: false
entities: [Authorization, UnitConnect]
stored_procs: []
depends:
  - "EcommerceConnectService (IEcommerceConnectService.CRM_GetAuthorizationUrl — đang COMMENT, chưa inject)"
  - "External ecommerce gateway (POST {GetUrlEcommerce()}crm/auth/GetAuthorizationUrl qua _clientHelper)"
  - "Core/TienIch (MaTienIchSCRM.TI_Tiktok — đơn vị phải mua tiện ích Tiktok; const SB.Core/Contants.cs:800)"
cross_module_in: []
cross_module_out:
  - "External ecommerce-connect gateway (TikTok OAuth) — nơi redirect ủy quyền và sau đó callback lưu token"
  - "UnitConnect / WarehouseConnect / ProductConnect / AdsUnitConnect — sau khi ủy quyền xong, đồng bộ gian hàng/kho/sản phẩm/quảng cáo qua các controller này (cùng IEcommerceConnectService)"
---

# TiktokConnectController — Kết nối gian hàng TikTok Shop (OAuth)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Khởi tạo kết nối Tiktok | Kết nối Tiktok | GET `api/TiktokConnect/ConnectTiktok` | `TiktokConnectController_ConnectTiktok` ("Kết nối Tiktok" — DV-AD-TONG, DV-AD-CN, DV-AD-MCN, DV-QL-MKT, DV-MKT-LEAD, DV-MKT) | internal | Đơn vị có tiện ích `TI_Tiktok` (gate qua `[TienIch(..., MaTienIchSCRM.TI_Tiktok)]`) | **Đúng (kỳ vọng cuối):** gọi `EcommerceConnectService.CRM_GetAuthorizationUrl(reqData, CurrentUser)` → external gateway `crm/auth/GetAuthorizationUrl` → trả authorization url để FE redirect sang TikTok. **Hiện tại (stub):** bỏ qua service, trả cứng `MethodResult<string>.ResultWithData("true")`. | (chưa nối EF/HTTP — gọi service bị comment) | **STUB + DEAD** — `TiktokConnectController.cs:30-31`: call service bị comment, trả cứng `"true"`; chưa nhận `reqData`, chưa inject service. THÊM: FE KHÔNG gọi endpoint này (xem ghi chú dưới). |

## Endpoint nội bộ (private / service-to-service)
- Controller này KHÔNG có endpoint private. Khi hoàn thiện, nó chỉ là 1 trigger OAuth; toàn bộ CRUD/đồng bộ thật đi qua các sibling controller cùng `IEcommerceConnectService`:
  - `AuthorizationController` — lưu token sau callback (`CRM_SaveAuthorization`).
  - `UnitConnectController` — map gian hàng TikTok ↔ đơn vị CRM (`CRM_GetListUnitConnect` / `CRM_SaveUnitConnect` / `CRM_DeleteUnitConnect`).
  - `WarehouseConnectController` — map kho.
  - `ProductConnectController` / `ProductCustomController` — đồng bộ & ghép sản phẩm (`CRM_SyncProductEcomm`, `CRM_ConnectProductAuto`).
  - `AdsUnitConnectController` / `CampaignAdsController` — kết nối quảng cáo.

## Quy tắc nghiệp vụ (gating)
- **Tiện ích gate:** `[TienIch("TiktokConnectController", "TiktokConnect", MaTienIchSCRM.TI_Tiktok)]` ở controller-level → đơn vị chưa mua tiện ích Tiktok thì không vào được.
- **Phân quyền:** `[SBAuthorize("TiktokConnectController_ConnectTiktok", ...)]` chỉ cho 6 role marketing/admin (AD-TONG/CN/MCN, QL-MKT, MKT-LEAD, MKT). Trùng pattern với `ShopeeConnectController.ConnectShopee`.

## Quan hệ
- **Gọi sang:** (kỳ vọng) `EcommerceConnectService.CRM_GetAuthorizationUrl` → external gateway `{GetUrlEcommerce()}crm/auth/GetAuthorizationUrl` qua `_clientHelper.PostAsync<ResGetAuthorizationUrl>`. Hiện chưa inject service nào (ctor `IServiceProvider inject` không dùng).
- **Dữ liệu (entities):** OAuth flow lưu vào `Authorization` (token gian hàng) sau callback; gian hàng map qua `UnitConnect`. Controller này không trực tiếp đụng EF.
- **Ripple / đồng bộ:** sau khi ủy quyền thành công, side-effect không nằm ở đây mà ở các sibling connect-controller (đồng bộ kho/sản phẩm/quảng cáo TikTok). Đây chỉ là cửa vào OAuth (về danh nghĩa).

## Luồng xuyên module (kết nối TikTok end-to-end)
**Thực tế FE đi đường này (KHÔNG qua controller stub):** màn `ecomm-gian-hang-danh-sach` được tham số hóa theo `EcommerceCode`; nút "Thêm kết nối" (`onClickThemKetNoi`) và "Xác thực lại" (`onReAuthenticate`) gọi thẳng `_ecommerceService.CRM_GetAuthorizationUrl({ ecommerceCode, callBackUrlAuthorization, ... })` rồi `window.location.href = res.data` → redirect sang sàn. TikTok dùng chung màn này, chỉ khác `EcommerceCode`.
1. User (role MKT/AD) mở màn gian hàng (EcommerceCode=Tiktok) → bấm **Thêm kết nối** → FE gọi `CRM_GetAuthorizationUrl` (qua EcommerceService FE, không qua `api/TiktokConnect/ConnectTiktok`) → lấy authorization url → redirect sang TikTok.
2. User ủy quyền trên TikTok → callback về CRM → `AuthorizationController.CRM_SaveAuthorization` lưu token (entity `Authorization`).
3. Map gian hàng/kho: `UnitConnectController` + `WarehouseConnectController`.
4. Đồng bộ sản phẩm: `ProductConnectController.CRM_SyncProductEcomm` / `CRM_ConnectProductAuto`.
> `TiktokConnectController.ConnectTiktok` là endpoint song sinh với `ShopeeConnectController.ConnectShopee`, hiện trả cứng `"true"` và KHÔNG nằm trên luồng FE thật → là dead stub.

## Lỗi đã biết / lưu ý kỹ thuật
- `TiktokConnectController.cs:30-31` — **STUB**: `ConnectTiktok` trả cứng `ResultWithData("true")`, dòng gọi `_ecommerceConnectService.CRM_GetAuthorizationUrl(...)` bị comment; ctor nhận `IServiceProvider inject` nhưng không resolve service nào → endpoint hiện không có tác dụng thật.
- `TiktokConnectController.cs:28` — chưa nhận `reqData` (`ReqGetAuthorizationUrl`) trong khi service thật cần param này → khi wire phải bổ sung body + inject `IEcommerceConnectService`.
- **FE không gọi endpoint này**: nút kết nối trên `ecomm-gian-hang-danh-sach.component.ts:262,309` gọi thẳng `_ecommerceService.CRM_GetAuthorizationUrl` (FE service tự gọi gateway), nên `api/TiktokConnect/ConnectTiktok` là dead — đồng nghĩa OAuth TikTok thực tế ĐÃ chạy qua đường FE-service, không phụ thuộc controller stub.
- Pattern song sinh với `ShopeeConnectController.cs:28-32` (cùng stub, cùng 6 role, cùng [TienIch]) → khi dọn nên xóa cả 2 hoặc wire chung `IEcommerceConnectService`, tránh drift giữa 2 sàn.
- `EcommerceConnectService.cs:130-135` (`WarehouseProduct.Service/Generated/OrtherService/EcommerceConnectService.cs`) — logic thật `CRM_GetAuthorizationUrl` đã có sẵn (POST tới gateway `crm/auth/GetAuthorizationUrl`), chỉ thiếu wiring ở controller.
- `SB.Core/Contants.cs:800` — `TI_Tiktok = "TI_Tiktok"` là mã tiện ích gate màn TikTok.
