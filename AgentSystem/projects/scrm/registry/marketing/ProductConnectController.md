---
controller: ProductConnectController
module: marketing
service: WarehouseProduct
man_hinh_ui: "Marketing > Kết nối sàn TMĐT > Sản phẩm gian hàng (FE pages/ecommerce: ecomm-san-pham-danh-sach + danh-sach-san-pham-nhap + add-prop-san-pham-crm) — list SP sàn ↔ SP CRM, ghép tay/ghép tự động, đồng bộ tồn, gỡ/ xóa kết nối. FE gọi qua ecommerce.service.ts."
muc_dich: "CRUD + đồng bộ 'kết nối sản phẩm' (ProductConnect): map SKU sản phẩm sàn TMĐT ↔ sản phẩm CRM (thường + combo), ghép tự động theo SKU, đẩy tồn kho CRM lên sàn (CRM_SyncProductEcomm), gỡ/xóa kết nối. Controller proxy nhưng có XỬ LÝ NGHIỆP VỤ THẬT ở service (resolve SP cha, tính tồn)."
alias: ["kết nối sản phẩm", "ProductConnect", "ghép sản phẩm sàn", "đồng bộ sản phẩm TMĐT", "đồng bộ tồn sàn", "map SKU", "connect product"]
auth: internal
state_machine: []
deprecated: false
entities: [ProductConnect, ProductStatus, SanPham, KhoChiTiet, UnitConnect]
stored_procs: []
depends:
  - "IProductConnectService (WarehouseProduct.Service.Generated.Ecommerce — proxy + logic resolve SP cha & tính tồn)"
  - "IEcommerceConnectService (OtherService — gọi external ecommerce-connect gateway)"
  - "ISanPhamEcommerceService (GetSanPhamChaEcom — lấy SP cha + thuộc tính khi ghép)"
  - "IRepository<SanPham>, IRepository<KhoChiTiet> (tính số lượng tồn khi đồng bộ)"
  - "External ecommerce-connect gateway (CRM_* product endpoints)"
cross_module_in:
  - "FE ecommerce.service.ts (gọi thẳng api/ProductConnect/*)"
cross_module_out:
  - "External ecommerce-connect gateway (đẩy/đọc map + tồn sản phẩm phía sàn)"
  - "WarehouseProduct SanPham/KhoChiTiet (đọc SP con + tồn kho để đẩy quantity lên sàn)"
---

# ProductConnectController — Kết nối & đồng bộ sản phẩm CRM ↔ sản phẩm sàn TMĐT

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách SP đã kết nối | list màn ecomm-san-pham-danh-sach | POST `api/ProductConnect/CRM_GetListProductConnect` | `[SBAuthorize()]` | internal | màn list | Proxy → gateway trả list `ProductConnect` (SKU sàn ↔ SP CRM). FE `CRM_GetListProductConnect` | external gateway | - |
| 2 | Danh sách trạng thái SP | filter trạng thái | POST `api/ProductConnect/CRM_GetListProductStatus` | `[SBAuthorize()]` | internal | màn list | Proxy → trả list `ProductStatus`. FE `CRM_GetListProductStatus` | external gateway | - |
| 3 | Danh sách SP bên sàn | dropdown/list SP sàn (danh-sach-san-pham-nhap) | POST `api/ProductConnect/CRM_GetListProductEcomm` | `[SBAuthorize()]` | internal | khi ghép | Proxy → trả SP phía sàn để ghép. FE `CRM_GetListProductEcomm` | external gateway | Return `MethodResult<Guid>` (lệch — data là danh sách SP sàn) |
| 4 | Ghép sản phẩm tự động | "Ghép tự động" | POST `api/ProductConnect/CRM_ConnectProductAuto` | `[SBAuthorize()]` | internal | có gian hàng | Proxy → gateway tự ghép SKU sàn ↔ SP CRM theo mã. FE `CRM_ConnectProductAuto` | external gateway | - |
| 5 | Ghép 1 sản phẩm (tay) | "Ghép" (add-prop-san-pham-crm) | POST `api/ProductConnect/CRM_ConnectProduct` | `[SBAuthorize()]` | internal | chọn SP CRM | Service resolve SP cha + thuộc tính từ `CrmSkuId`: nếu `IsCombo` → dùng trực tiếp; nếu là SP thường → `GetSanPhamChaEcom` lấy SP cha + 3 cặp thuộc tính, rồi proxy gateway ghép. FE `CRM_ConnectProduct` | external gateway + EF (SP) | Nếu không tìm thấy SP cha → "Lỗi không tìm thấy sản phẩm cha." |
| 6 | Đồng bộ tồn lên sàn | "Đồng bộ tồn" | POST `api/ProductConnect/CRM_SyncProductEcomm` | `[SBAuthorize()]` | internal | đã ghép + có gian hàng | Validate `UnitConnectId` + `ListProductConnect` non-empty; lấy `warehouseId` từ gian hàng (`CrmProductSyncWarehouseId`, fallback `CrmOrderWarehouseId`); với từng SP con lấy `KhoChiTiet.SoLuong` (floor) → set Quantity → proxy gateway đẩy tồn. FE `CRM_SyncProductEcomm` | external gateway + EF (SanPham, KhoChiTiet) | Thiếu gian hàng/SP/kho → trả lỗi rõ; SP con không tìm thấy SKU → bỏ qua (continue, quantity giữ -1) |
| 7 | Gỡ kết nối 1 SP | "Gỡ kết nối" | POST `api/ProductConnect/CRM_DisconnectProduct` | `[SBAuthorize()]` | internal | per-row | Proxy → gateway gỡ ghép (giữ bản ghi). FE `CRM_DisconnectProduct` | external gateway | - |
| 8 | Xóa kết nối SP | "Xóa" (confirm) | POST `api/ProductConnect/CRM_DeleteProductConnect` | `[SBAuthorize()]` | internal | per-row | Proxy → gateway xóa bản ghi ProductConnect. FE `CRM_DeleteProductConnect` | external gateway | - |

## Endpoint nội bộ (private / service-to-service)
- Không có `[PrivateSBAuthorize]`. Khác 2 sibling: service ProductConnect có **xử lý nghiệp vụ nội bộ thật** (resolve SP cha + tính tồn), không chỉ proxy.

## Quy tắc nghiệp vụ (CRM_SyncProductEcomm — blueprint để trace)
1. `UnitConnectId == null` → chặn "Lỗi không được để trống gian hàng." (ProductConnectService.cs:121-124)
2. `ListProductConnect` null/rỗng → chặn "Lỗi không được để trống sản phẩm." (ProductConnectService.cs:125-128)
3. Lấy gian hàng (`CRM_GetUnitConnect`) → `warehouseId = CrmProductSyncWarehouseId`; nếu rỗng → fallback `CrmOrderWarehouseId`. (ProductConnectService.cs:131-143)
4. `warehouseId` vẫn rỗng → chặn "Lỗi không tìm thấy kho để đồng bộ." (ProductConnectService.cs:144-147)
5. Mỗi `productConnect`: tìm SP con CRM theo `IdSanPhamCha == CrmProductId && Ma == PartnerSkuCode`; không thấy → skip (Quantity giữ -1). Thấy → lấy `KhoChiTiet.SoLuong` (kho `warehouseId`) floor về int → set Quantity. (ProductConnectService.cs:149-174)

## Quan hệ
- **Gọi sang:** `IProductConnectService` → `IEcommerceConnectService` (gateway) + `ISanPhamEcommerceService.GetSanPhamChaEcom` + repo `SanPham`/`KhoChiTiet`.
- **Dữ liệu (entities):** `ProductConnect` (map SKU sàn ↔ SP CRM), `ProductStatus`, `SanPham` (SP cha/con + thuộc tính), `KhoChiTiet` (tồn theo kho), `UnitConnect` (lấy kho đồng bộ).
- **Ripple / đồng bộ:** đẩy tồn CRM → sàn (1 chiều khi sync); ghép SP gắn theo gian hàng (`UnitConnect`) + kho (`WarehouseConnect`). Là controller "đầu ra" của cụm ecommerce-connect: gian hàng → kho → sản phẩm.

## Luồng xuyên module (đồng bộ SP end-to-end)
1. Map gian hàng (`UnitConnectController`) + map kho (`WarehouseConnectController`).
2. `CRM_GetListProductEcomm` lấy SP sàn → `CRM_ConnectProductAuto` (theo SKU) hoặc `CRM_ConnectProduct` (tay, resolve SP cha CRM).
3. `CRM_SyncProductEcomm` → lấy kho từ gian hàng → đọc tồn `KhoChiTiet` của SP con → đẩy quantity lên sàn qua gateway.

## Lỗi đã biết / lưu ý kỹ thuật
- `ProductConnectService.cs:87-99` — `CRM_GetListProductEcomm` khai báo `MethodResult<Guid>` nhưng nghiệp vụ là "lấy danh sách SP bên sàn"; kiểu trả Guid gây hiểu nhầm.
- `ProductConnectService.cs:151,173` — khi không khớp SP con/SKU thì `Quantity` giữ giá trị khởi tạo `-1` và vẫn đẩy lên gateway (continue, không loại khỏi list) → có thể đẩy quantity âm; cần xác nhận gateway lọc -1.
- `ProductConnectService.cs:167` — `Guid.Parse(warehouseId)` không try-parse: `CrmProductSyncWarehouseId`/`CrmOrderWarehouseId` không phải Guid hợp lệ → ném exception, bị catch chung trả "Lỗi không xác định".
- Toàn bộ service bọc try/catch trả `"Lỗi không xác định: {ex.Message}"` → che lỗi gateway/EF.
- Sibling cùng cụm: `UnitConnectController` (gian hàng), `WarehouseConnectController` (kho — cung cấp warehouseId để sync tồn). Đây là controller có business logic thật nhất trong 3 — trace ưu tiên `CRM_SyncProductEcomm`.
- Controller-level `[SBAuthorize()]` rỗng key per-method (giống 2 sibling).
