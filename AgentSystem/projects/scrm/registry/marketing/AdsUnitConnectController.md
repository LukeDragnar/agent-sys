---
controller: AdsUnitConnectController
module: marketing
service: WarehouseProduct
man_hinh_ui: "Marketing > Kết nối sàn > popup 'Ủy quyền chiến dịch' của gian hàng (component ecomm-gian-hang-uy-quyen-chien-dich) — bật/tắt đồng bộ hiệu suất chiến dịch quảng cáo của 1 gian hàng đã kết nối (unitConnectId)"
muc_dich: "Quản lý cấu hình kết nối QUẢNG CÁO của 1 gian hàng TMĐT đã ủy quyền: lấy + lưu thông tin AdsUnitConnect (cờ đồng bộ hiệu suất chiến dịch). Controller chỉ là lớp mỏng forward sang external ecommerce-connect gateway qua IEcommerceConnectService — KHÔNG đụng EF/DB trực tiếp."
alias: ["kết nối quảng cáo", "AdsUnitConnect", "ủy quyền chiến dịch", "đồng bộ quảng cáo sàn", "cấu hình ads gian hàng", "ads unit connect"]
auth: public
state_machine: []
deprecated: false
entities: [AdsUnitConnect, UnitConnect]
stored_procs: []
depends:
  - "IEcommerceConnectService (forward CRM_GetAdsUnitConnect / CRM_SaveAdsUnitConnect sang external gateway)"
  - "External ecommerce-connect gateway (lưu/lấy cấu hình quảng cáo theo gian hàng)"
cross_module_in: []
cross_module_out:
  - "External ecommerce-connect gateway (ads connect config) — nguồn dữ liệu thật"
  - "CampaignAdsController — sau khi bật đồng bộ, list/chi tiết/hiệu suất chiến dịch quảng cáo lấy qua controller này"
  - "UnitConnect (gian hàng) — AdsUnitConnect gắn với 1 unitConnectId đã kết nối"
---

# AdsUnitConnectController — Kết nối quảng cáo theo gian hàng TMĐT

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cấu hình kết nối quảng cáo của gian hàng | (mở popup "Ủy quyền chiến dịch" → nạp form) | POST `api/AdsUnitConnect/CRM_GetAdsUnitConnect` | (none — `[SBAuthorize()]` rỗng) | public | popup ủy quyền, có `unitConnectId` | Trả `AdsUnitConnect` (id, ecommerceCode, cờ `isSyncCampaignPerformance`...) bằng cách forward `ecommerceConnectService.CRM_GetAdsUnitConnect(reqData, CurrentUser)`; gateway lỗi/`Success=false` → trả error message | External gateway (không EF) | `[SBAuthorize()]` rỗng → chỉ cần đăng nhập, không gate permission-key riêng |
| 2 | Lưu cấu hình kết nối quảng cáo | "Lưu" / "Xác nhận" (popup) → `onSave` | POST `api/AdsUnitConnect/CRM_SaveAdsUnitConnect` | (none — `[SBAuthorize()]` rỗng) | public | popup ủy quyền | Lưu cấu hình (bật/tắt đồng bộ hiệu suất chiến dịch) → forward `ecommerceConnectService.CRM_SaveAdsUnitConnect`, trả về `Guid` id bản ghi; lỗi gateway → error message | External gateway (không EF) | - |

## Endpoint nội bộ (private / service-to-service)
- Controller này KHÔNG có endpoint `[PrivateSBAuthorize]`. Cả 2 endpoint đều public-FE.

## Quy tắc nghiệp vụ (gating)
- Controller-level chỉ có `[SBAuthorize()]` (rỗng) — KHÔNG có `[TienIch]` gate (khác hẳn TiktokConnect/ShopeeConnect vốn gate `MaTienIchSCRM.TI_Tiktok`). Tức màn ủy quyền chiến dịch không kiểm tra tiện ích riêng ở controller này.
- Không có permission-key cụ thể → mọi role đăng nhập gọi được nếu vào được màn.

## Quan hệ
- **Gọi sang:** `IEcommerceConnectService.CRM_GetAdsUnitConnect` / `CRM_SaveAdsUnitConnect` → external ecommerce-connect gateway. Service WarehouseProduct chỉ wrap kết quả vào `MethodResult`.
- **Dữ liệu (entities):** `AdsUnitConnect` (cấu hình quảng cáo của gian hàng: ecommerceCode, isSyncCampaignPerformance...) liên kết `UnitConnect` (gian hàng đã ủy quyền). Lưu thật ở phía gateway, không ở DB WarehouseProduct.
- **Ripple / đồng bộ:** bật `isSyncCampaignPerformance` → kích hoạt đồng bộ hiệu suất chiến dịch; dữ liệu chiến dịch sau đó đọc qua `CampaignAdsController`. Là bước cấu hình tiền đề cho luồng báo cáo quảng cáo.

## Luồng xuyên module (kết nối quảng cáo)
1. Gian hàng đã ủy quyền OAuth (qua Authorization/UnitConnect) → user mở popup "Ủy quyền chiến dịch" (truyền `unitConnectId`).
2. FE gọi `CRM_GetAdsUnitConnect` (ecommerce-gian-hang-uy-quyen-chien-dich.component.ts:64) nạp cấu hình hiện tại.
3. User bật/tắt đồng bộ hiệu suất → `CRM_SaveAdsUnitConnect` (component:86) → gateway lưu.
4. Sau đó `CampaignAdsController` đọc list/chi tiết/hiệu suất chiến dịch của gian hàng đã bật đồng bộ.

## Lỗi đã biết / lưu ý kỹ thuật
- `AdsUnitConnectController.cs:30,43` — cả 2 endpoint dùng `[SBAuthorize()]` rỗng (không permission-key, không `[TienIch]`). Khác convention các connect-controller khác (Tiktok/Shopee có gate tiện ích). Cần xác nhận có chủ ý.
- FE gọi THẲNG 2 endpoint này qua `EcommerceService.CRM_GetAdsUnitConnect` / `CRM_SaveAdsUnitConnect` (`ecommerce.service.ts:250-257`, base URL `SERVICE_API_WarehouseProduct`) — KHÁC pattern Tiktok stub (Tiktok FE bỏ qua controller). Controller này LÀ đường thật.
- `AdsUnitConnectService.cs:30-68` — service chỉ forward gateway, bọc try/catch trả `"Lỗi không xác định: {ex.Message}"` → lỗi gateway bị nuốt thành message chung, khó phân biệt timeout vs business error.
- Tham số ctor `IOptions<ServiceSettings> _serviceSettings` được inject nhưng không dùng trong 2 method (`AdsUnitConnectService.cs:20,27`) — dead field.
- Sibling cùng họ ecommerce connect (cùng `IEcommerceConnectService`): `UnitConnectController`, `WarehouseConnectController`, `ProductConnectController`, `CampaignAdsController`, `ProductCustomController`. Sửa hợp đồng gateway nhớ đối chiếu cả nhóm.
