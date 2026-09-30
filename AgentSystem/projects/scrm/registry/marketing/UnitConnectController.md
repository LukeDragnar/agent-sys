---
controller: UnitConnectController
module: marketing
service: WarehouseProduct
man_hinh_ui: "Marketing > Kết nối sàn TMĐT > danh sách gian hàng (FE pages/ecommerce: ecomm-gian-hang-danh-sach + ecomm-gian-hang-chi-tiet + chon-cac-gian-hang) — list gian hàng đã kết nối, popup cấu hình map gian hàng ↔ kho/đơn vị CRM. FE gọi qua ecommerce.service.ts."
muc_dich: "CRUD 'kết nối gian hàng' (UnitConnect): map 1 gian hàng sàn TMĐT (Shopee/Tiktok/Lazada) đã ủy quyền ↔ đơn vị CRM, cấu hình callback nhận đơn + url tra sản phẩm. Là controller proxy mỏng đẩy thẳng sang ecommerce-connect gateway qua EcommerceConnectService."
alias: ["kết nối gian hàng", "UnitConnect", "map gian hàng", "cấu hình gian hàng TMĐT", "gian hàng kết nối", "đồng bộ gian hàng sàn"]
auth: internal
state_machine: []
deprecated: false
entities: [UnitConnect, Authorization]
stored_procs: []
depends:
  - "IUnitConnectService (WarehouseProduct.Service.Generated.Ecommerce — wrapper try/catch quanh EcommerceConnectService)"
  - "IEcommerceConnectService (OtherService — gọi external ecommerce-connect gateway qua _clientHelper)"
  - "External ecommerce-connect gateway (CRM_GetListUnitConnect / CRM_GetUnitConnect / CRM_SaveUnitConnect / CRM_DeleteUnitConnect)"
  - "ServiceSettings (OrderLogisticServiceUrl, WarehouseProductServiceUrl — bơm callback url khi Save)"
cross_module_in:
  - "FE ecommerce.service.ts (gọi thẳng api/UnitConnect/* — KHÔNG qua TiktokConnect/ShopeeConnect stub)"
cross_module_out:
  - "External ecommerce-connect gateway (lưu/đọc cấu hình gian hàng phía sàn)"
  - "OrderLogistic (CrmCallBackUrl = OrderLogisticServiceUrl/api/DonHang/NhanDuLieuDonHangTMDT — nơi sàn callback đẩy đơn về)"
  - "WarehouseProduct SanPham (CrmProductUrl = WarehouseProductServiceUrl/api/SanPham/GetProductBySku — sàn tra SP theo SKU)"
---

# UnitConnectController — Kết nối / map gian hàng sàn TMĐT ↔ đơn vị CRM

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy danh sách gian hàng đã kết nối | list màn ecomm-gian-hang-danh-sach | POST `api/UnitConnect/CRM_GetListUnitConnect` | `[SBAuthorize()]` (controller-level) | internal | màn list gian hàng | Proxy → gateway trả list `UnitConnect` (kèm TotalRecord) theo đơn vị/EcommerceCode. FE gọi `ecommerce.service.ts#CRM_GetListUnitConnect` | external gateway | - |
| 2 | Lấy chi tiết 1 gian hàng | mở chi tiết (ecomm-gian-hang-chi-tiet) | POST `api/UnitConnect/CRM_GetUnitConnect` | `[SBAuthorize()]` | internal | per-row | Proxy → trả 1 `UnitConnect` theo `UnitConnectId`. FE `CRM_GetUnitConnect` | external gateway | - |
| 3 | Lưu cấu hình gian hàng | "Lưu" popup cấu hình | POST `api/UnitConnect/CRM_SaveUnitConnect` | `[SBAuthorize()]` | internal | popup cấu hình | Service **bơm thêm** `CrmCallBackUrl` (OrderLogistic NhanDuLieuDonHangTMDT) + `CrmProductUrl` (SanPham GetProductBySku) rồi proxy → gateway lưu; trả `Guid` id gian hàng. FE `CRM_SaveUnitConnect` | external gateway | Callback url ghép cứng từ ServiceSettings; sai cấu hình URL → sàn không callback được đơn |
| 4 | Xóa kết nối gian hàng | "Xóa kết nối" (confirm) | POST `api/UnitConnect/CRM_DeleteUnitConnect` | `[SBAuthorize()]` | internal | per-row | Proxy → gateway xóa kết nối gian hàng. FE `CRM_DeleteUnitConnect` | external gateway | Xóa map gian hàng → ngắt nhận đơn + đồng bộ SP/kho của gian hàng đó |

## Endpoint nội bộ (private / service-to-service)
- Không có endpoint `[PrivateSBAuthorize]`. Mọi method là proxy gọi từ FE. Lưu ý: `CRM_GetUnitConnect` còn được gọi NỘI BỘ bởi `ProductConnectService.CRM_SyncProductEcomm` (qua EcommerceConnectService trực tiếp, không qua controller) để lấy `CrmProductSyncWarehouseId` khi đồng bộ tồn.

## Quan hệ
- **Gọi sang:** `IUnitConnectService` → `IEcommerceConnectService` (OtherService) → external ecommerce-connect gateway. Khi Save còn đọc `ServiceSettings` để ghép callback/product url.
- **Dữ liệu (entities):** `UnitConnect` (gian hàng: UnitConnectId, EcommerceCode, đơn vị CRM, CrmOrderWarehouseId, CrmProductSyncWarehouseId, callback/product url...). Token ủy quyền lưu ở `Authorization` (do `AuthorizationController` quản, không phải controller này).
- **Ripple / đồng bộ:** controller không đụng EF nội bộ; ripple nằm ở gateway + downstream: gian hàng đã map mới nhận đơn (OrderLogistic) + đồng bộ kho (WarehouseConnect) + sản phẩm (ProductConnect).

## Luồng xuyên module (kết nối gian hàng end-to-end)
1. Ủy quyền OAuth: FE (`ecomm-gian-hang-danh-sach`) gọi `EcommerceService.CRM_GetAuthorizationUrl` → redirect sàn → callback → `AuthorizationController.CRM_SaveAuthorization` lưu token (KHÔNG đi qua TiktokConnect/ShopeeConnect — 2 controller đó là stub).
2. Map gian hàng: FE `CRM_SaveUnitConnect` (controller này) → gateway lưu UnitConnect, gắn callback nhận đơn về OrderLogistic.
3. Map kho: `WarehouseConnectController` (sibling). Đồng bộ SP: `ProductConnectController` (sibling).
4. Khi sàn có đơn → callback `OrderLogistic/api/DonHang/NhanDuLieuDonHangTMDT`.

## Lỗi đã biết / lưu ý kỹ thuật
- `UnitConnectService.cs:78-79` — `CRM_SaveUnitConnect` ghép cứng callback url từ `ServiceSettings.OrderLogisticServiceUrl` + `WarehouseProductServiceUrl`; deploy sai 2 setting này → sàn không gọi lại được (mất đơn / không tra được SKU), khó phát hiện vì lưu vẫn thành công.
- Toàn bộ service bọc try/catch trả `"Lỗi không xác định: {ex.Message}"` (UnitConnectService.cs:47-50...) → mất lỗi gốc từ gateway, debug phải xem log gateway.
- Sibling cùng `IEcommerceConnectService`: `WarehouseConnectController`, `ProductConnectController`, `AdsUnitConnectController`, `AuthorizationController`. `TiktokConnectController`/`ShopeeConnectController` chỉ là stub OAuth — luồng thật đi qua đây.
- Controller-level `[SBAuthorize()]` không gắn permission key cụ thể per-method → phân quyền dựa role mặc định của SBAuthorize rỗng; cần xác nhận policy mong muốn (so với GiaoHangTaiKhoan có key rõ).
