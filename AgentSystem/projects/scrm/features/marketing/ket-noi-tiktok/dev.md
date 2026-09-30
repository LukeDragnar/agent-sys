# Dev — Kết nối TikTok

> Service: **WarehouseProduct** (cụm ecommerce-connect). Controller `TiktokConnect` là **STUB + DEAD** (FE không gọi). CRUD/đồng bộ thật đi qua sibling (`UnitConnect`, `WarehouseConnect`, `ProductConnect`, `Authorization`) proxy sang **ecommerce-connect gateway** ngoài. TikTok dùng chung màn + service FE với Shopee/Lazada, chỉ khác `EcommerceCode`. Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | FE page | Ghi chú |
|---|---|---|
| Danh sách gian hàng TMĐT | `pages/ecommerce/ecomm-gian-hang-danh-sach` | tham số hóa `EcommerceCode=Tiktok`; `onClickThemKetNoi` / `onReAuthenticate` gọi thẳng `_ecommerceService.CRM_GetAuthorizationUrl` → `window.location.href = res.data` |
| Chi tiết gian hàng | `pages/ecommerce/ecomm-gian-hang-chi-tiet` | map kho / đơn vị |
| Sản phẩm gian hàng | `pages/ecommerce/ecomm-san-pham-danh-sach` + `danh-sach-san-pham-nhap` + `add-prop-san-pham-crm` | ghép / đồng bộ tồn |

- FE: `ecomm-gian-hang-danh-sach.component.ts:262` (`onClickThemKetNoi`), `:309` (`onReAuthenticate`) → `ecommerce.service.ts#CRM_GetAuthorizationUrl`.

## Endpoint / Controller (trace registry)
**TiktokConnectController** (service WarehouseProduct, `[TienIch("TiktokConnectController","TiktokConnect", MaTienIchSCRM.TI_Tiktok)]`):
| Hành động | Endpoint | Quyền | Trạng thái |
|---|---|---|---|
| Khởi tạo kết nối TikTok | GET `api/TiktokConnect/ConnectTiktok` | `TiktokConnectController_ConnectTiktok` (DV-AD-TONG/CN/MCN, DV-QL-MKT, DV-MKT-LEAD, DV-MKT) | **STUB + DEAD** — trả cứng `"true"`, FE không gọi |

Luồng thật qua sibling (giống Shopee — cùng `IEcommerceConnectService`):
| Bước | Endpoint | Controller |
|---|---|---|
| Lấy URL ủy quyền | (FE service) `CRM_GetAuthorizationUrl` | EcommerceService FE → gateway |
| Lưu token | `CRM_SaveAuthorization` | AuthorizationController |
| Gian hàng | `api/UnitConnect/CRM_*UnitConnect` | UnitConnectController (`[SBAuthorize()]` rỗng) |
| Map kho | `api/WarehouseConnect/*` | WarehouseConnectController |
| Sản phẩm | `api/ProductConnect/CRM_*` (ConnectProductAuto / ConnectProduct / SyncProductEcomm…) | ProductConnectController (`[SBAuthorize()]` rỗng) |

## Entities
- `Authorization` (token), `UnitConnect` (gian hàng + kho + callback url), `ProductConnect` (map SKU sàn ↔ SP CRM), phụ thuộc `SanPham` / `KhoChiTiet`.

## Luồng code
- **ConnectTiktok (stub):** `TiktokConnectController.cs:28-32` — call service comment, trả `ResultWithData("true")`, chưa nhận `reqData`, ctor `IServiceProvider inject` không dùng. Logic thật `EcommerceConnectService.cs:130-135`.
- **Map gian hàng / ghép SP / đồng bộ tồn:** giống Shopee — xem `dev.md` của Kết nối Shopee (cùng `UnitConnectService` / `ProductConnectService`). `CRM_SyncProductEcomm` blueprint: `ProductConnectService.cs:121-174`.
- Mã tiện ích: `SB.Core/Contants.cs:800` `TI_Tiktok = "TI_Tiktok"`.

## Bug-prone / lưu ý (path:line — từ registry, CHƯA tự sửa)
- **Controller stub + dead:** `TiktokConnectController.cs:28-32` — trả "true", chưa nhận `reqData`, chưa inject service. **FE KHÔNG gọi** (đi qua `_ecommerceService.CRM_GetAuthorizationUrl` ở `ecomm-gian-hang-danh-sach.component.ts:262,309`) → OAuth TikTok thực tế **không phụ thuộc** controller này. Khi dọn nên xóa cả 2 stub (Shopee + Tiktok) hoặc wire chung.
- **Callback url ghép cứng:** `UnitConnectService.cs:78-79` (chung cho mọi sàn) — deploy sai `OrderLogisticServiceUrl`/`WarehouseProductServiceUrl` → TikTok không callback được đơn.
- **Mất lỗi gốc gateway:** `UnitConnectService.cs:47-50`, `ProductConnectService` try/catch trả "Lỗi không xác định".
- **Đẩy quantity -1:** `ProductConnectService.cs:151,173` SP không khớp SKU vẫn `continue` với Quantity=-1.
- **Parse kho không guard:** `ProductConnectService.cs:167` `Guid.Parse(warehouseId)`.
- **Phân quyền lỏng:** UnitConnect / ProductConnect controller-level `[SBAuthorize()]` rỗng.

## File source (path:line)
- `TiktokConnectController.cs:28-32` (stub).
- `EcommerceConnectService.cs:130-135` (logic thật GetAuthorizationUrl).
- `UnitConnectService.cs:47-50`, `:78-79`.
- `ProductConnectService.cs:87-99`, `:121-174`.
- FE: `ecomm-gian-hang-danh-sach.component.ts:262`, `:309`.
- `SB.Core/Contants.cs:800` (`TI_Tiktok`).
- Registry: `projects/scrm/registry/marketing/TiktokConnectController.md`, `UnitConnectController.md`, `ProductConnectController.md`, `AuthorizationController.md`, `WarehouseConnectController.md`.
