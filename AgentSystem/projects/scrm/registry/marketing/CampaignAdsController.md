---
controller: CampaignAdsController
module: marketing
service: WarehouseProduct
man_hinh_ui: "Marketing > Quảng cáo sàn > 'Danh sách chiến dịch quảng cáo' (component ecomm-danh-sach-chien-dich-quang-cao) — list chiến dịch; mở 'Chi tiết chiến dịch' (ecomm-chi-tiet-chien-dich-quang-cao) xem detail + biểu đồ hiệu suất"
muc_dich: "Đọc dữ liệu CHIẾN DỊCH QUẢNG CÁO của các gian hàng TMĐT đã ủy quyền: danh sách chiến dịch, chi tiết 1 chiến dịch, số liệu hiệu suất (performance) theo thời gian. Read-only; controller forward toàn bộ sang external ecommerce-connect gateway qua IEcommerceConnectService — KHÔNG đụng EF/DB."
alias: ["chiến dịch quảng cáo", "CampaignAds", "quảng cáo sàn", "ads campaign", "hiệu suất chiến dịch", "danh sách chiến dịch quảng cáo"]
auth: public
state_machine: []
deprecated: false
entities: [AdsCampaign, CampaignAdsDetails, CampaignAdsPerformance]
stored_procs: []
depends:
  - "IEcommerceConnectService (forward CRM_GetListCampaignAds / CRM_GetCampaignAdsDetails / CRM_GetCampaignAdsPerformance sang external gateway)"
  - "AdsUnitConnect (cấu hình đồng bộ quảng cáo của gian hàng — tiền đề có data)"
cross_module_in: []
cross_module_out:
  - "External ecommerce-connect gateway (ads campaign data) — nguồn dữ liệu thật, read-only"
---

# CampaignAdsController — Chiến dịch quảng cáo sàn TMĐT (read-only)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách chiến dịch quảng cáo | màn list chiến dịch → load grid | POST `api/CampaignAds/CRM_GetListCampaignAds` | (none — `[SBAuthorize()]` rỗng) | public | màn danh sách chiến dịch | Trả `List<AdsCampaign>` (kèm `TotalRecord` để phân trang) của gian hàng theo filter; forward `ecommerceConnectService.CRM_GetListCampaignAds`; gateway `Success=false` → error | External gateway (không EF) | `[SBAuthorize()]` rỗng — không gate permission/tiện ích |
| 2 | Chi tiết 1 chiến dịch | mở row → màn chi tiết | POST `api/CampaignAds/CRM_GetCampaignAdsDetails` | (none) | public | có id chiến dịch | Trả `CampaignAdsDetails` (thông tin chi tiết 1 chiến dịch) qua gateway | External gateway (không EF) | - |
| 3 | Hiệu suất chiến dịch | tab/biểu đồ hiệu suất (màn chi tiết) | POST `api/CampaignAds/CRM_GetCampaignAdsPerformance` | (none) | public | màn chi tiết, có khoảng thời gian | Trả `List<CampaignAdsPerformance>` (số liệu hiệu suất theo mốc thời gian) qua gateway | External gateway (không EF) | - |

## Endpoint nội bộ (private / service-to-service)
- Controller này KHÔNG có endpoint `[PrivateSBAuthorize]`. Toàn bộ 3 endpoint là read-only public-FE.

## Quy tắc nghiệp vụ (gating)
- Controller-level chỉ `[SBAuthorize()]` rỗng; KHÔNG `[TienIch]`, KHÔNG permission-key cụ thể. Mọi role đăng nhập vào màn đều gọi được.
- Có data hay không phụ thuộc gian hàng đã bật `isSyncCampaignPerformance` ở `AdsUnitConnectController` chưa (tiền đề upstream, không kiểm ở đây).

## Quan hệ
- **Gọi sang:** `IEcommerceConnectService.CRM_GetListCampaignAds` / `CRM_GetCampaignAdsDetails` / `CRM_GetCampaignAdsPerformance` → external ecommerce-connect gateway.
- **Dữ liệu (entities):** `AdsCampaign` (đầu chiến dịch), `CampaignAdsDetails` (chi tiết), `CampaignAdsPerformance` (số liệu hiệu suất). Đều là model trả từ gateway, không persist ở DB WarehouseProduct.
- **Ripple / đồng bộ:** read-only, không tạo side-effect. Phụ thuộc cấu hình `AdsUnitConnect` (đồng bộ quảng cáo) đã bật.

## Lỗi đã biết / lưu ý kỹ thuật
- `CampaignAdsController.cs:21,29,37` — cả 3 endpoint `[SBAuthorize()]` rỗng (không permission-key, không `[TienIch]`) — đồng bộ pattern lỏng với `AdsUnitConnectController`. Cần xác nhận có chủ ý.
- FE gọi THẲNG controller qua `EcommerceService` (`ecommerce.service.ts:238-249`): list ở `ecomm-danh-sach-chien-dich-quang-cao.component.ts:99`, chi tiết+hiệu suất ở `ecomm-chi-tiet-chien-dich-quang-cao.component.ts:46,84`. Controller LÀ đường thật (khác Tiktok stub).
- `CampaignAdsService.cs:30-91` — chỉ forward gateway, try/catch nuốt lỗi thành `"Lỗi không xác định: {ex.Message}"`; ctor inject `IOptions<ServiceSettings>` nhưng không dùng (dead field, `CampaignAdsService.cs:20,27`).
- Sibling cùng `IEcommerceConnectService`: `AdsUnitConnectController` (cấu hình), `UnitConnectController`, `ProductConnectController`, `ProductCustomController`. Cùng họ ecommerce-connect.
