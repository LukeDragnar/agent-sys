# Dev — Kết nối Shopee

> Service: **WarehouseProduct** (cụm ecommerce-connect). Controller `ShopeeConnect` chỉ là cửa OAuth; CRUD/đồng bộ thật đi qua sibling controller (`UnitConnect`, `WarehouseConnect`, `ProductConnect`, `Authorization`) — tất cả proxy sang **ecommerce-connect gateway** ngoài qua `IEcommerceConnectService`. Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | FE page | Ghi chú |
|---|---|---|
| Danh sách gian hàng TMĐT | `pages/ecommerce/ecomm-gian-hang-danh-sach` | tham số hóa theo `EcommerceCode` (Shopee/Tiktok/Lazada dùng chung); nút Thêm kết nối / Xác thực lại |
| Chi tiết gian hàng | `pages/ecommerce/ecomm-gian-hang-chi-tiet` | cấu hình map kho / đơn vị |
| Sản phẩm gian hàng | `pages/ecommerce/ecomm-san-pham-danh-sach` + `danh-sach-san-pham-nhap` + `add-prop-san-pham-crm` | ghép tay / tự động / đồng bộ tồn |

- FE gọi qua `ecommerce.service.ts` (`CRM_GetAuthorizationUrl`, `CRM_*UnitConnect`, `CRM_*ProductConnect`). **Không** đi qua `api/ShopeeConnect/ConnectShopee`.

## Endpoint / Controller (trace registry)
**ShopeeConnectController** (service WarehouseProduct, `[TienIch("ShopeeConnectController","ShopeeConnect", MaTienIchSCRM.TI_Shopee)]`):
| Hành động | Endpoint | Quyền | Trạng thái |
|---|---|---|---|
| Khởi tạo kết nối Shopee | GET `api/ShopeeConnect/ConnectShopee` | `ShopeeConnectController_ConnectShopee` (DV-AD-TONG/CN/MCN, DV-QL-MKT, DV-MKT-LEAD, DV-MKT) | **STUB** — trả cứng `"true"` |

Luồng thật qua sibling (cùng `IEcommerceConnectService`):
| Bước | Endpoint | Controller |
|---|---|---|
| Lấy URL ủy quyền | (FE service) `CRM_GetAuthorizationUrl` | EcommerceService FE → gateway |
| Lưu token sau callback | `CRM_SaveAuthorization` | AuthorizationController |
| List / chi tiết / lưu / xóa gian hàng | `api/UnitConnect/CRM_GetListUnitConnect` · `CRM_GetUnitConnect` · `CRM_SaveUnitConnect` · `CRM_DeleteUnitConnect` | UnitConnectController (`[SBAuthorize()]` rỗng) |
| Map kho | `api/WarehouseConnect/*` | WarehouseConnectController |
| List / ghép / đồng bộ SP | `api/ProductConnect/CRM_GetListProductConnect` · `CRM_GetListProductEcomm` · `CRM_ConnectProductAuto` · `CRM_ConnectProduct` · `CRM_SyncProductEcomm` · `CRM_Disconnect/DeleteProductConnect` | ProductConnectController (`[SBAuthorize()]` rỗng) |

## Entities
- **`Authorization`** — token ủy quyền gian hàng (lưu sau callback).
- **`UnitConnect`** — gian hàng: `UnitConnectId`, `EcommerceCode`, đơn vị CRM, `CrmOrderWarehouseId`, `CrmProductSyncWarehouseId`, callback url / product url.
- **`ProductConnect`** — map SKU sàn ↔ SP CRM; **`ProductStatus`**. Phụ thuộc `SanPham` (cha/con), `KhoChiTiet` (tồn theo kho).

## Luồng code
- **ConnectShopee (stub):** `ShopeeConnectController.cs:28-32` — dòng gọi `_ecommerceConnectService.CRM_GetAuthorizationUrl(...)` bị comment, trả cứng `ResultWithData("true")`; ctor nhận `IServiceProvider inject` nhưng không resolve service. Logic thật có sẵn ở `EcommerceConnectService.cs:130-135` (POST gateway `crm/auth/GetAuthorizationUrl`), chỉ thiếu wiring.
- **CRM_SaveUnitConnect:** `UnitConnectService.cs:78-79` bơm thêm `CrmCallBackUrl` = `OrderLogisticServiceUrl/api/DonHang/NhanDuLieuDonHangTMDT` + `CrmProductUrl` = `WarehouseProductServiceUrl/api/SanPham/GetProductBySku` rồi proxy gateway.
- **CRM_SyncProductEcomm** (blueprint trace, `ProductConnectService.cs`):
  1. `UnitConnectId==null` → chặn (`:121-124`).
  2. `ListProductConnect` rỗng → chặn (`:125-128`).
  3. Lấy gian hàng → `warehouseId = CrmProductSyncWarehouseId` (fallback `CrmOrderWarehouseId`) (`:131-143`).
  4. `warehouseId` rỗng → chặn "không tìm thấy kho" (`:144-147`).
  5. Mỗi SP: tìm SP con `IdSanPhamCha==CrmProductId && Ma==PartnerSkuCode`; không thấy → skip (Quantity giữ -1); thấy → `KhoChiTiet.SoLuong` floor → set Quantity (`:149-174`).

## Bug-prone / lưu ý (path:line — từ registry, CHƯA tự sửa)
- **Controller stub:** `ShopeeConnectController.cs:28-32` — `ConnectShopee` không có tác dụng thật (trả "true"), chưa nhận `reqData` (`ReqGetAuthorizationUrl`), chưa inject `IEcommerceConnectService`. Song sinh với `TiktokConnectController.cs:28-32`. Nếu FE đi qua endpoint này → kết nối gãy.
- **Callback url ghép cứng:** `UnitConnectService.cs:78-79` lấy từ `ServiceSettings.OrderLogisticServiceUrl` + `WarehouseProductServiceUrl`; deploy sai 2 setting → sàn không callback được (mất đơn / không tra SKU), nhưng lưu vẫn thành công → khó phát hiện.
- **Mất lỗi gốc gateway:** `UnitConnectService.cs:47-50` và `ProductConnectService` bọc try/catch trả `"Lỗi không xác định: {ex.Message}"` → che lỗi gateway/EF, phải xem log gateway.
- **Đẩy quantity âm:** `ProductConnectService.cs:151,173` — SP không khớp SKU thì `Quantity` giữ `-1` và vẫn `continue` (không loại khỏi list) → có thể đẩy -1 lên sàn; cần xác nhận gateway lọc.
- **Parse kho không guard:** `ProductConnectService.cs:167` `Guid.Parse(warehouseId)` không try-parse → throw nếu `CrmProductSyncWarehouseId`/`CrmOrderWarehouseId` không phải Guid.
- **Return type lệch:** `ProductConnectService.cs:87-99` `CRM_GetListProductEcomm` khai báo `MethodResult<Guid>` nhưng data là danh sách SP sàn.
- **Phân quyền lỏng:** UnitConnect / ProductConnect controller-level `[SBAuthorize()]` rỗng (không key per-method) — cần xác nhận policy mong muốn.

## File source (path:line)
- `ShopeeConnectController.cs:28-32` (stub).
- `EcommerceConnectService.cs:130-135` (`WarehouseProduct.Service/Generated/OrtherService/`) — logic thật GetAuthorizationUrl.
- `UnitConnectService.cs:47-50`, `:78-79`.
- `ProductConnectService.cs:87-99`, `:121-174`.
- Registry: `projects/scrm/registry/marketing/ShopeeConnectController.md`, `UnitConnectController.md`, `ProductConnectController.md`, `WarehouseConnectController.md`, `AuthorizationController.md`.
