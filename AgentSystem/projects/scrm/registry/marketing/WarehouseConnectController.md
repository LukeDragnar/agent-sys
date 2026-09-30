---
controller: WarehouseConnectController
module: marketing
service: WarehouseProduct
man_hinh_ui: "Marketing > Kết nối sàn TMĐT > Kho gian hàng (FE pages/ecommerce: ecomm-kho-danh-sach + ecomm-gian-hang-da-kho) — map kho CRM ↔ kho của gian hàng sàn để đồng bộ tồn/đẩy đơn. FE gọi qua ecommerce.service.ts."
muc_dich: "CRUD 'kết nối kho' (WarehouseConnect): map kho CRM ↔ kho phía sàn TMĐT cho từng gian hàng; lấy danh sách kho bên sàn (CRM_GetListWarehouseEcomm) để chọn ghép. Controller proxy mỏng đẩy thẳng sang ecommerce-connect gateway."
alias: ["kết nối kho", "WarehouseConnect", "map kho gian hàng", "kho gian hàng", "đồng bộ kho sàn", "kho TMĐT"]
auth: internal
state_machine: []
deprecated: false
entities: [WarehouseConnect, WarehouseConnectDetails, UnitConnect]
stored_procs: []
depends:
  - "IWarehouseConnectService (WarehouseProduct.Service.Generated.Ecommerce — wrapper try/catch)"
  - "IEcommerceConnectService (OtherService — gọi external ecommerce-connect gateway)"
  - "External ecommerce-connect gateway (CRM_GetListWarehouseConnect / CRM_GetWarehouseConnect / CRM_SaveWarehouseConnect / CRM_DeleteWarehouseConnect / CRM_GetListWarehouseEcomm)"
cross_module_in:
  - "FE ecommerce.service.ts (gọi thẳng api/WarehouseConnect/*)"
cross_module_out:
  - "External ecommerce-connect gateway (lưu/đọc map kho phía sàn)"
  - "ProductConnect (đồng bộ tồn dựa trên kho đã map — CrmProductSyncWarehouseId)"
---

# WarehouseConnectController — Kết nối / map kho CRM ↔ kho gian hàng sàn TMĐT

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy danh sách kho đã map | list màn ecomm-kho-danh-sach | POST `api/WarehouseConnect/CRM_GetListWarehouseConnect` | `[SBAuthorize()]` | internal | màn list kho | Proxy → gateway trả list `WarehouseConnect` của gian hàng. FE `CRM_GetListWarehouseConnect` | external gateway | Lỗi → "Lấy danh sách kết nối kho thất bại." |
| 2 | Lấy chi tiết 1 kết nối kho | mở chi tiết | POST `api/WarehouseConnect/CRM_GetWarehouseConnect` | `[SBAuthorize()]` | internal | per-row | Proxy → trả `WarehouseConnectDetails`. FE `CRM_GetWarehouseConnect` | external gateway | Lỗi → "Lấy chi tiết kho thất bại." |
| 3 | Lưu map kho | "Lưu" popup | POST `api/WarehouseConnect/CRM_SaveWarehouseConnect` | `[SBAuthorize()]` | internal | popup map kho | Proxy → gateway lưu map kho CRM ↔ kho sàn; trả `Guid`. FE `CRM_SaveWarehouseConnect` | external gateway | - |
| 4 | Xóa map kho | "Xóa" (confirm) | POST `api/WarehouseConnect/CRM_DeleteWarehouseConnect` | `[SBAuthorize()]` | internal | per-row | Proxy → gateway xóa map kho. FE `CRM_DeleteWarehouseConnect` | external gateway | Xóa map → ngắt đồng bộ tồn của kho đó với sàn |
| 5 | Lấy danh sách kho bên sàn | dropdown chọn kho sàn (ecomm-gian-hang-da-kho) | POST `api/WarehouseConnect/CRM_GetListWarehouseEcomm` | `[SBAuthorize()]` | internal | khi cấu hình map | Proxy → gateway trả kho phía sàn (theo gian hàng) để ghép với kho CRM. FE `CRM_GetListWarehouseEcomm` | external gateway | Return type `MethodResult<bool>` (lệch — data thực là danh sách kho bên ngoài bool) |

## Endpoint nội bộ (private / service-to-service)
- Không có endpoint `[PrivateSBAuthorize]`. Toàn bộ là proxy gọi từ FE.

## Quan hệ
- **Gọi sang:** `IWarehouseConnectService` → `IEcommerceConnectService` (OtherService) → external ecommerce-connect gateway.
- **Dữ liệu (entities):** `WarehouseConnect` / `WarehouseConnectDetails` (map kho CRM ↔ kho sàn theo gian hàng). Liên kết gián tiếp với `UnitConnect` (kho map gắn theo gian hàng + `CrmProductSyncWarehouseId`).
- **Ripple / đồng bộ:** controller không đụng EF nội bộ. Kho đã map là cơ sở để `ProductConnectService.CRM_SyncProductEcomm` xác định `warehouseId` lấy số lượng tồn (KhoChiTiet) đẩy lên sàn → đổi/xóa map kho ảnh hưởng đồng bộ tồn.

## Luồng xuyên module
1. Sau khi map gian hàng (`UnitConnectController`), user vào màn kho → `CRM_GetListWarehouseEcomm` lấy kho sàn → ghép với kho CRM → `CRM_SaveWarehouseConnect`.
2. Đồng bộ tồn: `ProductConnectController.CRM_SyncProductEcomm` đọc `CrmProductSyncWarehouseId`/`CrmOrderWarehouseId` của gian hàng → lấy `KhoChiTiet.SoLuong` của kho đó → đẩy quantity lên sàn.

## Lỗi đã biết / lưu ý kỹ thuật
- `WarehouseConnectService.cs:113-131` — `CRM_GetListWarehouseEcomm` khai báo `MethodResult<bool>` nhưng về nghiệp vụ là "lấy danh sách kho bên sàn"; kiểu trả bool gây hiểu nhầm, data thật nằm ở response gateway. Cần xác nhận FE đọc đúng field.
- `WarehouseConnectService.cs:26-31` — ctor inject `IClientHelper clientHelper` nhưng KHÔNG gán/dùng (dead param); mọi call đi qua `_otherService`.
- Toàn bộ service bọc try/catch trả `"Lỗi không xác định: {ex.Message}"` → mất lỗi gốc gateway.
- Sibling cùng `IEcommerceConnectService`: `UnitConnectController`, `ProductConnectController`. Kho map ở đây là tiền đề cho đồng bộ tồn của ProductConnect — sửa logic kho phải đối chiếu `CRM_SyncProductEcomm` (ProductConnectService.cs:129-147).
- Controller-level `[SBAuthorize()]` rỗng key per-method (giống UnitConnect/ProductConnect) — phân quyền theo policy mặc định.
