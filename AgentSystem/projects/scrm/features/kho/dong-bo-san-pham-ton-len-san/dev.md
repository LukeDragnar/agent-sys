# Dev — Đồng bộ sản phẩm & tồn kho lên sàn TMĐT

> Chiều **SCRM → sàn** (push). Logic chính ở service **EcommerceConnect** (solution riêng, KHÔNG nằm trong registry SCRM). SCRM-side chỉ bắn số lượng sang. Chỉ dành cho dev.
> Lưu ý phạm vi: registry `projects/scrm/registry/` không có node EcommerceConnect (service tách riêng) → trace dưới đây bám source thật, không phải registry node.

## Màn hình & route (FE — scrm_frontend, module ecommerce)
| Màn hình | Route | Component |
|---|---|---|
| Danh sách SP gian hàng (sàn) + kết nối | `/ecommerce/san-pham-danh-sach` | `EcommSanPhamDanhSachComponent` |
| Danh sách SP CRM để map | `/ecommerce/crm-san-pham-danh-sach` | `CrmSanPhamDanhSachComponent` |
| Kết nối Shopee / SP Shopee | `/ecommerce/shopee-ket-noi`, `/ecommerce/shopee-san-pham-danh-sach`, `/ecommerce/shopee-crm-san-pham-danh-sach` | `ShopeeKetNoiComponent`, … |
| Kết nối TikTok / SP TikTok | `/ecommerce/tiktok-ket-noi`, `/ecommerce/tiktok-san-pham-danh-sach`, `/ecommerce/tiktok-crm-san-pham-danh-sach` | `TiktokKetNoiComponent`, … |
| Modal kết nối SP (CRM ↔ sàn) | (dialog) | `EcommSanPhamKetNoiComponent` (`app-ecomm-san-pham-ket-noi`) |

- Routing: `scrm_frontend/src/app/pages/ecommerce/ecommerce-routing.module.ts:32` (san-pham-danh-sach), `:38`, `:40`, `:44`, `:49`, `:50`, `:53`.
- Component list SP sàn: `.../ecommerce/ecomm-san-pham-danh-sach/ecomm-san-pham-danh-sach.component.ts:25` (modal kết nối import ở `:10`).
- Modal kết nối: `.../ecommerce/ecomm-san-pham-ket-noi/ecomm-san-pham-ket-noi.component.ts:18`.

## Endpoint / Controller
**EcommerceConnect.API — CrmProductConnectController** (`/api/crm/product`), file `EcommerceConnect.API/Controllers/Crm/CrmProductConnectController.cs:13`:
| Method | Line | Mục đích |
|---|---|---|
| `GetListProductConnectV2` | `:47` | DS sản phẩm đã kết nối |
| `GetListProductStatus` | `:60` | Trạng thái kết nối SP |
| `GetListProductEcomm` | `:73` | Lấy SP trên sàn |
| `DeleteProductConnect` | `:87` | Xóa kết nối |
| `ConnectProductAuto` | `:100` | Kết nối tự động theo SKU |
| `SyncProductEcomm` | `:112` | Đồng bộ tồn thủ công lên sàn |
| `ConnectProduct` | `:124` | Kết nối thủ công 1 cặp |
| `DisconnectProduct` | `:136` | Hủy kết nối |
| `SaveProductQuantity` | `:150` | **Nhận số lượng từ SCRM** → ghi ProductQuantity |

Đăng SP nháp: `CrmCustomProductController` — `CreateProductDraft`, `GetListProductDraft`, `SendProductDraftToEcomm`, `UpdateListProductDraft`.
Mapping kho: `CrmWarehouseConnectController` — `GetListWarehouseEcomm` (sync kho sàn), `SaveWarehouseConnect`.

**SCRM-side (đẩy số lượng sang ecomm)** — `WarehouseProduct.API/Controllers/Ecommerce/ProductConnectController.cs:14` (proxy/manage); job đẩy số lượng: `WarehouseProduct.API/Jobs/JobSendQuantityToEcommerce.cs:10`.

## Entities (EcommerceConnect.DataContext)
- **`ProductConnect`** — `Entity/ProductConnect.cs:5`. Map SP sàn↔CRM. Trường: `UnitConnectId`, `EcommerceCode`, `CrmProductId/Code/Name`, `CrmSkuId/Code/Name`, `PartnerProductId/Code/Name`, `PartnerSkuId/Code/Name`, `WeightGram/HeightCm/LengthCm/WidthCm`, `SyncQuantity`, `Quantity`, `SyncPrice`, `Price`, `IsCombo`, `SyncWait`, `SyncSuccess`, `InProcess`, `Status`.
- **`ProductQuantity`** — `Entity/ProductQuantity.cs:5`. Hàng chờ đồng bộ tồn từ SCRM: `CrmUnitId/ChildId`, `CrmWarehouseId`, `IsCombo`, `CrmProductId/Code/Name`, `CrmSkuId/Code/Name`, `Quantity`, `Status`, `InProcess`, `ResendNo`.
- **`UnitConnect`** — `Entity/UnitConnect.cs:6`. Gian hàng + cấu hình đồng bộ: `ProductSyncStatus`, `ProductSyncQuantity`, `ProductSyncPrice`, `ProductSyncAutoOn/Off`, `CrmProductSyncUnitChildId/Name`, `CrmProductSyncWarehouseId/Name`, `ProductSyncOutOfStockWhenQuantityCrmBelow` (`:186`), `ProductSyncRate` (`:192`).
- **`WarehouseConnect`** — `Entity/WarehouseConnect.cs:6`. Map kho sàn↔CRM: `ProductSyncOutOfStockWhenQuantityCrmBelow` (`:82`), `ProductSyncRate` (`:84`), `CrmProductSyncWarehouseId/Name`, `IsDefault`.
- `ProductDraft`/`ProductDraftChildren` — SP nháp để đăng lên sàn.

## Luồng code
**Push tồn (tự động):**
1. SCRM: `KhoChiTietService.JobSendProductQuantityToEcommerce` (`WarehouseProduct.Service/Generated/KhoChiTietService.cs:539`): gom SP có tồn đổi → build `ReqSaveProductQuantity` (`:575`–`:631`) → `ecommerceConnectService.SaveProductQuantity(...)` (`:636`). Job wrapper: `JobSendQuantityToEcommerce.cs:51`.
2. Ecomm: `CrmProductService.SaveProductQuantity` (`EcommerceConnect.Service/Generated/Crm/CrmProductService.cs:1773`) → insert bảng `ProductQuantity`.
3. Ecomm job `JobProduct` (`EcommerceConnect.Service/Generated/Schedule/JobProduct.cs:8`; đăng ký `EcommerceConnect.API/Service/JobExtention.cs:23`):
   - timer `ProcessProductQuantityHandleData` (`JobProduct.cs:33`, ~15s) → `ProductService.ProcessProductQuantityHandleData`.
   - timer `ProcessProductQuantitySyncToEcomm` (`JobProduct.cs:34`, ~15s) → `ProductService.ProcessProductQuantitySyncToEcomm` (`EcommerceConnect.Service/Generated/MySystem/ProductService.cs:307`).
4. Lọc UnitConnect bật đồng bộ (`ProductService.cs:158`: `ProductSyncStatus==true && ProductSyncQuantity==true`); lọc ProductConnect đã map + `SyncQuantity` null/true (`:170`); khớp kho đồng bộ (`:191`–`:246`).
5. **Tính số lượng đẩy** (`ProductService.cs:251`):
   ```csharp
   var tmpQuanCalc = FunctionCommon.ConvertToInt(productQuantityLast.Quantity * (unitConnect.ProductSyncRate ?? 100.0) / 100);
   if (tmpQuanCalc <= (unitConnect.ProductSyncOutOfStockWhenQuantityCrmBelow ?? 0))
       tmpQuanCalc = 0;
   ```
   → sinh `ProductQuantityTemp { Quantity = tồn thật, QuantitySync = tmpQuanCalc }` (`:257`).
6. Pha sync (`:307`+): Shopee `shopeeProductService.UpdateStock(...)` (`:542`); TikTok build `ReqUpdateStock` (`:564`) → `tiktokProductService.UpdateStock(...)` (`:581`).

**Push tồn (thủ công):** `CrmProductService.SyncProductEcomm` (`CrmProductService.cs:1035`): duyệt `reqData.ListProductConnect`, theo `EcommerceCode` gọi thẳng `shopeeProductService.UpdateStock` (`:1098`) / `tiktokProductService.UpdateStock` (`:1128`) với số lượng truyền vào. **Chỉ stock, không giá.**

**Map SP:** `ConnectProductAuto` (`CrmProductService.cs:892`), `ConnectProduct` (`:1156`), `DisconnectProduct` (`:1290`), `SaveProductConnect` (`:1847`, nhận từ consumer `ProductEcommReceiveConsumer.cs:58`).

**Update SP API sàn:** `ShopeeProductService` (`Generated/Shopee/ShopeeProductService.cs:18`): `UpdateItem` (`:346`), `UpdatePrice` (`:380`), `UpdateStock` (`:408`). `TikTokProductService` (`Generated/Tiktok/TikTokProductService.cs:13`): `UpdatePrice` (`:165`), `UpdateStock` (`:194`).

## Bug-prone / lưu ý
- **Đồng bộ GIÁ chưa được wire-up.** `ShopeeProductService.UpdatePrice` (`:380`) và `TikTokProductService.UpdatePrice` (`:165`) tồn tại nhưng **không có callsite hoạt động** trong job/`SyncProductEcomm` (chỉ 1 match comment-out trong `Shopee/ProductController.cs`). `ProductSyncPrice`/`SyncPrice`/`Price`/`ProductSyncRate-for-price` được lưu DB nhưng KHÔNG đẩy lên sàn. ⚠️ UI có ô "đồng bộ giá" → khách tưởng giá tự đồng bộ. Cần product xác nhận đây là chưa làm hay cố ý.
- **`ProductSyncRate` áp lên SỐ LƯỢNG, không phải giá** (`ProductService.cs:251`: `Quantity * Rate/100`). Tên gây hiểu nhầm là "tỷ lệ giá". Đừng dùng nó cho giá.
- **Lọc kho cứng theo cấu hình** (`ProductService.cs:191`–`246`): tồn đổi ở kho không khớp `CrmProductSyncWarehouseId`/`CrmOrderWarehouseId` → không đẩy. Sai cấu hình kho = "tồn sàn không nhảy" thầm lặng (không báo lỗi).
- **Chỉ xét record tồn MỚI NHẤT** theo `CreatedDate` (`ProductService.cs:150` `OrderByDescending(CreatedDate).FirstOrDefault()`): nếu nhiều thay đổi dồn, các record cũ trong cùng batch bị bỏ → đẩy giá trị cuối (đúng theo ý đồ "tồn hiện tại" nhưng cẩn thận khi debug "sao thiếu lần đồng bộ").
- **`SyncQuantity is null` được coi như TRUE** (`:172`): SP mới map mà chưa set cờ vẫn bị đồng bộ. Muốn không đồng bộ phải set `SyncQuantity=false` rõ ràng.
- **Đồng bộ TikTok trong `SyncProductEcomm` đánh dấu TODO** (`CrmProductService.cs:1103` `//TODO: Thêm tiktok`) — đã build `ReqUpdateStock` và gọi nhưng vùng này còn comment TODO, cần verify đầy đủ flow TikTok thủ công.
- **Ngưỡng dùng `<=`**: tồn (sau nhân tỷ lệ) **bằng** ngưỡng cũng thành 0 (hết hàng), không phải "dưới ngưỡng" thuần. Khách cấu hình ngưỡng=3 sẽ hết hàng ở qtySync=3.
- **2 chỗ cấu hình tỷ lệ/ngưỡng** (UnitConnect và WarehouseConnect) — job hiện đọc `unitConnect.ProductSyncRate`/`...OutOfStock` (`:251`), KHÔNG đọc WarehouseConnect-level → cấu hình per-kho có thể bị bỏ qua. Cần product/dev xác nhận chỗ nào là nguồn đúng.
- **Push 1 chiều**: SCRM không nhận lại tồn từ sàn ở luồng này; oversell trên sàn (khách đặt trước khi đồng bộ kịp) vẫn xảy ra trong cửa sổ trễ ~15s+.

## File source (path:line)
- Ecomm controller: `D:\PROJECT\Git\_SourceForCode\ecommerce_connect\EcommerceConnect.API\Controllers\Crm\CrmProductConnectController.cs:13` (`:47`,`:100`,`:112`,`:124`,`:136`,`:150`).
- Ecomm service connect: `...\EcommerceConnect.Service\Generated\Crm\CrmProductService.cs:892` (`:1035` SyncProductEcomm, `:1156`,`:1290`,`:1773` SaveProductQuantity, `:1847` SaveProductConnect).
- Ecomm sync logic + công thức: `...\EcommerceConnect.Service\Generated\MySystem\ProductService.cs:158`,`:251`,`:307`,`:542`,`:581`.
- Ecomm job: `...\EcommerceConnect.Service\Generated\Schedule\JobProduct.cs:8` (`:33`,`:34`); đăng ký `...\EcommerceConnect.API\Service\JobExtention.cs:23`.
- Ecomm consumer nhận SP từ sàn: `...\EcommerceConnect.API\Consumer\ProductEcommReceiveConsumer.cs:19` (`:58`).
- Ecomm API sàn: `...\EcommerceConnect.Service\Generated\Shopee\ShopeeProductService.cs:18` (`:346`,`:380`,`:408`); `...\Generated\Tiktok\TikTokProductService.cs:13` (`:165`,`:194`).
- Ecomm entities: `...\EcommerceConnect.DataContext\Entity\ProductConnect.cs:5`, `ProductQuantity.cs:5`, `UnitConnect.cs:6` (`:186`,`:192`), `WarehouseConnect.cs:6` (`:82`,`:84`).
- SCRM job đẩy số lượng: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\WarehouseProduct\WarehouseProduct.API\Jobs\JobSendQuantityToEcommerce.cs:10` (`:51`).
- SCRM service build payload: `...\Group2\scrm\WarehouseProduct\WarehouseProduct.Service\Generated\KhoChiTietService.cs:539` (`:575`–`:639`, gọi `:636`).
- SCRM controller proxy: `...\Group2\scrm\WarehouseProduct\WarehouseProduct.API\Controllers\Ecommerce\ProductConnectController.cs:14`.
- FE routing: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\ecommerce\ecommerce-routing.module.ts:32`.
- FE list SP sàn: `...\scrm_frontend\src\app\pages\ecommerce\ecomm-san-pham-danh-sach\ecomm-san-pham-danh-sach.component.ts:25`.
- FE modal kết nối: `...\scrm_frontend\src\app\pages\ecommerce\ecomm-san-pham-ket-noi\ecomm-san-pham-ket-noi.component.ts:18`.
