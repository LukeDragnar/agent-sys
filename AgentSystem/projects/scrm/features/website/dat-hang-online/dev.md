# Đặt hàng online — Kỹ thuật

> Service: **Website** (`Website.API`, MVC + API). Storefront công khai (Razor view), không phải màn admin Angular. Chỉ dành cho dev.
> Nguồn trace: registry `website/CartController.md`, `website/OrderController.md`, `website/WebsiteDonHangController.md`.

## Màn hình & route
| Màn hình | Route | Loại | Controller |
|---|---|---|---|
| Giỏ hàng | GET `Cart/Index` (mặc định) | Razor View `Home/Cart.cshtml` | `CartController` |
| Thêm nhanh vào giỏ | GET `HomeCartPlus/{id}/{quantity}/{type}` | action | `CartController` |
| Set/sửa số lượng | GET `Cart/{id}/{productItemId}/{quantity}` | action | `CartController` |
| Xóa dòng giỏ | DELETE `Cart/{id}/{productItemId}` | action | `CartController` |
| Trang thanh toán | GET `/CheckOutNow` · `/thanh-toan-don-hang` | Razor View `Home/CheckOutNow.cshtml` | `CartController` |
| Lưu thông tin bước 1 | POST `/Cart/SaveCheckOutStep1` | action (cookie) | `CartController` |
| Tạo đơn website | POST `Cart/CreateOrder` | action (transaction) | `CartController` |
| Trang chuyển khoản | GET `OrderBankTranfers/{code}` | Razor View `Home/OrderBankTranfers.cshtml` | `CartController` |
| Tạo đơn từ app mobile | POST `api/Order/CreateOrder` | API (token mobile) | `OrderController` (MobileApi) |

> Theme + đơn vị resolve theo **domain** qua `LinkWebsiteService.GetDataByDomain` (mỗi đơn vị 1 tên miền, ThemeCode động). Giỏ tách nhiều kho → redirect `/Checkout` (controller tách đơn riêng, ngoài phạm vi node này).

## Endpoint / Controller (trace registry)
| Endpoint | Quyền / Auth | Mục đích |
|---|---|---|
| GET `Cart/Index`, `GetShopCart`, `HomeCartPlus/...`, `Cart/{...}` | anonymous (`[TokenAuthentication]` = soft cookie, không chặn) | Quản lý giỏ trong cookie DB |
| POST `Cart/SaveCheckOutStep1` | anonymous | Lưu `OrderLinkAffliateModel` vào cookie `{domain}_info` |
| POST `Cart/CreateOrder` | anonymous | Dựng `WebsiteDonHang` → lưu Contact + đơn website (transaction) |
| POST `api/Order/CreateOrder` | `[TokenMobileAuthentication]` (không có SBAuthorize key) | Tạo đơn từ app mobile → `IOrderService.CreateOrder` |
| POST `api/WebsiteDonHang/ThemMoiDonHangWebsite` | `[PrivateSBAuthorize]` (service-to-service) | Storefront/checkout đẩy đơn website tạm |
| POST `api/WebsiteDonHang/GetAllTempOrders` | `[PrivateSBAuthorize]` | Lấy đơn tạm (giỏ/chưa chốt) |

> Quản lý/duyệt đơn website đã đặt (TimTheoDieuKien, CapNhat, TaoDonTuWebsite, CapNhatIdDonHang) thuộc registry `WebsiteDonHangController` — là bước *sau* đặt hàng, không thuộc feature này.

## Entities
- **`WebsiteDonHang`** (header đơn website) + chi tiết: **`WebsiteDonHangSanPham`**, **`WebsiteDonHangThe`**, **`WebsiteDonHangCombo`**.
- Giỏ tạm (cookie, KHÔNG persist DB tới khi CreateOrder): **`ProductCartGroup`** / **`ProductCart`**.
- **`OrderLinkAffliateModel`** — thông tin khách + địa chỉ + UTM lưu cookie `{domain}_info`.
- Cookie key: `{domain}_cart` (giỏ), `{domain}_info` (thông tin checkout), `{domain}_idWarehouse`, `UtmTracking` (UTM).

## Luồng code (CreateOrder — storefront)
`POST Cart/CreateOrder` → đọc cookie giỏ (`{domain}_cart`) + info (`{domain}_info`) → `ICommonApiService.ShopCartGroup` gán **giá hiện tại** (không snapshot) → dựng `WebsiteDonHang` + tách SanPham/The/Combo → map UTM từ cookie `UtmTracking` → `InsertContactWebsitePrivateAsync` (tạo/ghi **Contact** cross-service, kèm danh sách SP/combo/thẻ) → `IWebsiteDonHangService.ThemMoiDonHangAsync` (**transaction**: insert đơn + chi tiết, rollback nếu lỗi) → clear cookie giỏ → nếu `LoaiThanhToan = ChuyenKhoan` trả link `/OrderBankTranfers/{maThanhToan}`, ngược lại về `/`.

**Gating giỏ / tách kho:**
- Loại item (`TypeChiTietDonHang`): 1=SanPham, 2=DichVu, 3=Combo, 4=The, 5=NapThe, 6=ThuKhac. `HomeCartPlus` chỉ verify tồn tại cho SanPham/Combo/The.
- Kho mặc định = `-1`; kho `-2` = group đặc biệt **bị loại** khỏi đếm/tổng tiền.
- Thêm item khi giỏ không có kho `-1` hoặc có >1 group → dồn về 1 group kho `-1`.
- Checkout: `cartGroup.Count > 1` → redirect `/Checkout` (màn tách đơn). VAT chỉ tính nếu `CauHinhCore.IsSuDungVat == true`.

## Bug-prone / lưu ý (path:line)
- **CreateOrder chỉ xử lý group đầu** — `CartController.cs:641` `var lstProduct = productCarts.FirstOrDefault();`: giỏ đã tách nhiều kho → chỉ tạo đơn cho group đầu, các group còn lại bị bỏ ở luồng này (đúng ra đi qua `/Checkout`).
- **Thêm nhanh không verify type 2/5/6** — `CartController.cs:115-142`: DichVu/NapThe/ThuKhac → `productItemId = Guid.Empty` vẫn add (không chặn item không tồn tại).
- **CSRF** — `[ValidateAntiForgeryToken]` bị comment ở `SaveCheckOutStep1` (`CartController.cs:571`) và không có ở `CreateOrder` → POST tạo đơn không chống giả mạo.
- **Dead guard** — `if (1 == 1)` ở `GetShopCart` (`CartController.cs:266`); nhiều khối reset KM bị comment trong Add/Remove.
- **Giá không snapshot** — giá gán lúc `ShopCartGroup`, không lưu vào cookie → đổi giá/ngừng bán bên WarehouseProduct ảnh hưởng trực tiếp giỏ đang mở.
- **Cookie = source of truth** — mất cookie `{domain}_cart` = mất giỏ (không persist DB tới CreateOrder).
- **OrderController (mobile)** — `Controllers/MobileApi/OrderController.cs:21`: auth chỉ ở method-level (`TokenMobileAuthentication`), controller không có `[SBAuthorize]` → action mới phải tự gắn token-attribute.
- Domain không map được link website → `CartController` trả `NotFound()`.

## File source
- BE storefront: `Website.API/Controllers/CartController.cs` (`:115-142`, `:266`, `:571`, `:641`).
- BE mobile: `Website.API/Controllers/MobileApi/OrderController.cs:21`.
- BE đơn website (sync): `Website.API/Controllers/Api/WebsiteDonHangController.cs` (ThemMoiDonHangWebsite / TaoDonTuWebsite).
- Service: `IWebsiteDonHangService.ThemMoiDonHangAsync`, `ICommonApiService` (ShopCartGroup, InsertContactWebsitePrivateAsync), `ILinkWebsiteService.GetDataByDomain`, `IWebsiteCookieService`.
- Path gốc cụ thể: hỏi user (Website service nằm trong solution SCRM — xác nhận đường dẫn khi cần sửa code).
