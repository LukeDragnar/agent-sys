---
controller: ProductCustomController
module: marketing
service: WarehouseProduct
man_hinh_ui: "Marketing > Kết nối sàn > 'Sản phẩm ghép sàn' / Tạo sản phẩm nháp đăng sàn: màn 'Thêm sản phẩm nhập' (add-san-pham-nhap), 'Danh sách sản phẩm nhập' (danh-sach-san-pham-nhap), 'Thêm thuộc tính SP CRM' (add-prop-san-pham-crm), 'Danh sách ngành hàng' (danh-sach-nganh-hang)"
muc_dich: "Soạn & đẩy SẢN PHẨM GHÉP SÀN (custom product / product draft) lên sàn TMĐT: lấy danh mục ngành hàng/thuộc tính/kênh, map sản phẩm CRM, tạo-sửa-gửi bản nháp sản phẩm sang sàn. Controller forward sang external ecommerce-connect gateway qua IEcommerceConnectService (ProductCustomService có inject ISqlSugarClient nhưng các path chính đều đi gateway, không CRUD EF nội bộ)."
alias: ["sản phẩm ghép sàn", "ProductCustom", "sản phẩm nhập", "product draft", "sản phẩm nháp", "đăng sản phẩm lên sàn", "ngành hàng sàn", "custom product"]
auth: public
state_machine: []
deprecated: false
entities: [ProductDraft, ProductDraftEdit, CategoryModel, AttributeModel, ChannelModel, ParentProduct]
stored_procs: []
depends:
  - "IEcommerceConnectService (forward toàn bộ category/attribute/channel/product-draft sang external gateway)"
  - "ISqlSugarClient (inject vào ProductCustomService — dùng cho một số truy vấn phụ; path chính đi gateway)"
cross_module_in: []
cross_module_out:
  - "External ecommerce-connect gateway (custom product / product draft) — nguồn dữ liệu + nơi đẩy bản nháp lên sàn"
  - "ProductConnectController — đồng bộ/ghép sản phẩm CRM↔sàn (cùng nghiệp vụ product)"
---

# ProductCustomController — Sản phẩm ghép sàn (custom product / product draft)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo bản nháp sản phẩm | "Lưu/Tạo" (màn thêm SP nhập) → `CreateProductDraft` | POST `api/ProductCustom/CRM_CreateProductDraft` | (none — `[SBAuthorize()]` rỗng) | public | màn add-san-pham-nhap | Tạo product draft (sản phẩm nháp) → forward `productCustomService.CRM_CreateProductDraft`; trả `bool` thành công | External gateway | `[SBAuthorize()]` rỗng toàn controller — không permission-key |
| 2 | Lưu/cập nhật thuộc tính SP CRM | "Lưu thuộc tính" (add-prop-san-pham-crm) → `CRM_SaveCrmProduct` | POST `api/ProductCustom/CRM_SaveCrmProduct` | (none) | public | màn thêm thuộc tính SP CRM | Tạo/cập nhật thuộc tính của sản phẩm (map sản phẩm CRM) qua gateway; trả `bool` | External gateway | - |
| 3 | Gửi bản nháp lên sàn | "Gửi lên sàn" (danh-sach-san-pham-nhap) → `SendProductDraftToEcomm` | POST `api/ProductCustom/CRM_SendProductDraftToEcomm` | (none) | public | có draft đã soạn | Đẩy product draft sang sàn TMĐT qua gateway; trả `bool` | External gateway | Side-effect THẬT (đẩy lên sàn) nhưng không gate quyền riêng |
| 4 | Cập nhật danh sách bản nháp | (chỉnh sửa SP nhập) | POST `api/ProductCustom/CRM_UpdateListProductDraft` | (none) | public | sửa draft | Cập nhật hàng loạt product draft qua gateway; trả `bool` | External gateway | - |

## Endpoint nội bộ (private / service-to-service)
- Controller KHÔNG có `[PrivateSBAuthorize]`. Các endpoint còn lại là **lookup/danh mục read-only phục vụ form đăng SP** (gom theo mục đích, không tách dòng):
  - **Danh mục ngành hàng:** `CRM_GetListCategory`, `CRM_GetListCategoryByUnitConnectId` → cây ngành hàng của sàn (toàn bộ / theo gian hàng) cho dropdown chọn ngành.
  - **Thuộc tính & kênh:** `CRM_GetAttributes` (thuộc tính theo ngành hàng), `CRM_GetChannelList` (danh sách kênh/sàn), `CRM_GetProductProperty` (thuộc tính 1 sản phẩm) → đổ form thuộc tính SP.
  - **Sản phẩm CRM nguồn:** `CRM_GetListCrmProduct` → list sản phẩm CRM để chọn ghép.
  - **Đọc bản nháp:** `CRM_GetListProductDraft`, `CRM_GetListProductDraftByIds` → list / chi tiết bản nháp đã tạo.
  > Tất cả forward `IEcommerceConnectService` tương ứng; đều `[SBAuthorize()]` rỗng (public-FE), không phải private — chỉ là endpoint hỗ trợ form.

## Quy tắc nghiệp vụ (gating)
- Controller-level `[SBAuthorize()]` rỗng; KHÔNG `[TienIch]`, KHÔNG permission-key per-endpoint → mọi role đăng nhập vào màn đều gọi được, kể cả endpoint ghi (`CRM_CreateProductDraft`, `CRM_SendProductDraftToEcomm`).
- Logic ghép/đăng SP nằm ở gateway; controller không validate nghiệp vụ.

## Quan hệ
- **Gọi sang:** `IEcommerceConnectService` (CRM_GetListCategory / CRM_GetAttributes / CRM_GetListCrmProduct / CRM_SaveCrmProduct / CRM_CreateProductDraft / CRM_SendProductDraftToEcomm / CRM_UpdateListProductDraft ...) → external ecommerce-connect gateway. `ProductCustomService` còn inject `ISqlSugarClient _dbDapper` (`ProductCustomService.cs:33,37`) cho truy vấn phụ.
- **Dữ liệu (entities):** `CategoryModel` (ngành hàng), `AttributeModel` (thuộc tính), `ChannelModel` (kênh), `ParentProduct` (SP CRM), `ProductCrmProperty`, `ProductDraft` / `ProductDraftEdit` (bản nháp SP đăng sàn). Dữ liệu thật ở gateway.
- **Ripple / đồng bộ:** `CRM_SendProductDraftToEcomm` đẩy SP lên sàn → tạo/đồng bộ sản phẩm bên sàn (ripple ra ngoài hệ thống). Liên quan `ProductConnectController` (ghép/đồng bộ SP CRM↔sàn).

## Luồng xuyên module (đăng sản phẩm ghép sàn)
1. User mở "Thêm sản phẩm nhập" → FE nạp danh mục: `CRM_GetListCategory(ByUnitConnectId)`, `CRM_GetAttributes`, `CRM_GetChannelList`, `CRM_GetListCrmProduct`.
2. Soạn thuộc tính → `CRM_SaveCrmProduct` (add-prop-san-pham-crm.component.ts:758).
3. Tạo nháp → `CRM_CreateProductDraft` (add-san-pham-nhap.component.ts:199).
4. Từ "Danh sách sản phẩm nhập" → `CRM_SendProductDraftToEcomm` (danh-sach-san-pham-nhap.component.ts:335) đẩy lên sàn.

## Lỗi đã biết / lưu ý kỹ thuật
- `ProductCustomController.cs:14,35..178` — controller + mọi endpoint dùng `[SBAuthorize()]` rỗng, KHÔNG permission-key, KHÔNG `[TienIch]`, kể cả endpoint ghi/đẩy-lên-sàn (`CRM_CreateProductDraft`:127, `CRM_SendProductDraftToEcomm`:166). Rủi ro: bất kỳ role đăng nhập nào vào được màn đều đẩy SP lên sàn. Cần xác nhận chủ ý.
- FE gọi THẲNG controller qua `EcommerceService` (`ecommerce.service.ts:154-212`) — controller LÀ đường thật (khác Tiktok stub).
- `ProductCustomService.cs:32-42` — forward `IEcommerceConnectService`, try/catch nuốt lỗi thành `"Lỗi không xác định: {ex.Message}"`; có inject `ISqlSugarClient` nhưng các method category/draft chính đều đi gateway (kiểm tra method nào thực sự dùng `_dbDapper` khi sửa logic).
- Sibling cùng họ ecommerce-connect: `ProductConnectController` (đồng bộ SP), `AdsUnitConnectController`, `CampaignAdsController`, `UnitConnectController`. Nghiệp vụ product chia 2 controller (ProductConnect = ghép/đồng bộ SP đã có; ProductCustom = soạn-đăng SP nháp mới) — debug "SP không lên sàn" phải xác định đúng controller.
