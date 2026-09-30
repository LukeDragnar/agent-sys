---
controller: CartController
module: website
service: Website
man_hinh_ui: "Website bán hàng (storefront công khai) — trang giỏ hàng (Cart.cshtml), trang thanh toán (CheckOutNow.cshtml / Checkout tách đơn), trang chuyển khoản (OrderBankTranfers.cshtml). Theme động theo domain (ThemeCode)."
muc_dich: "Giỏ hàng + checkout của website bán hàng public: thêm/sửa/xóa sản phẩm-dịch vụ-thẻ-combo trong cookie giỏ hàng, gom/tách đơn theo kho, tính VAT, tạo đơn website + đẩy contact, render trang thanh toán & chuyển khoản."
alias: ["giỏ hàng", "cart", "thêm vào giỏ", "thanh toán đơn hàng", "checkout", "đặt mua website", "lên đơn website", "chuyển khoản đơn hàng"]
auth: anonymous
state_machine: []
deprecated: false
entities: [WebsiteDonHang, WebsiteDonHangSanPham, WebsiteDonHangThe, WebsiteDonHangCombo, ProductCartGroup, ProductCart, OrderLinkAffliateModel]
stored_procs: []
depends: [CommonApiService (Website.Service), LinkWebsiteService, WebsiteCookieService, WebsiteDonHangService, TokenService, WarehouseProduct (sản phẩm/dịch vụ/combo/thẻ qua private API), Promotion, Contact (InsertContactWebsitePrivate), Core (AddressSystem/CauHinhCore VAT)]
cross_module_in: []
cross_module_out: [Contact (InsertContactWebsitePrivateAsync — tạo contact khi lên đơn), WebsiteDonHang (lưu đơn hàng website), WarehouseProduct/Promotion (đọc giá sản phẩm-dịch vụ-combo-thẻ để gán vào giỏ)]
---

# CartController — Giỏ hàng & thanh toán (website bán hàng public)

> MVC `Controller` (trả về **View** Razor, KHÔNG phải API admin). Gắn `[TokenAuthentication]` cấp controller — đây là **soft cookie filter**: nếu không có token thì vẫn chạy tiếp với khách vô danh (không chặn). → Auth thực tế = `anonymous` (storefront ai cũng truy cập được; chỉ "đăng nhập" để prefill thông tin khách). Mọi state giỏ hàng nằm trong **cookie DB** key `{domain}_cart` / `{domain}_info` / `{domain}_idWarehouse`, không phải session admin. Theme + đơn vị resolve theo **domain** qua `LinkWebsiteService.GetDataByDomain`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Mở trang giỏ hàng | menu "Giỏ hàng" | GET `Cart/Index` (route mặc định) | — | anonymous | luôn | Resolve domain→data, đọc cookie `{domain}_cart`, gọi `ShopCartGroup` gán full giá/ảnh, render `Home/Cart.cshtml` theo ThemeCode | EF (qua private API) | `return new NotFound()` nếu domain không map được link website |
| 2 | Thêm nhanh (sản phẩm/combo/thẻ) | nút "+" / "Mua ngay" | GET `HomeCartPlus/{id}/{quantity}/{type}` | — | anonymous | luôn | Theo `type` (1=SanPham,3=Combo,4=The) verify tồn tại qua CommonApiService rồi cộng dồn vào giỏ; gom về kho `-1` (kho mặc định) nếu giỏ đã tách; trả `countProduct` | EF (private API verify) | type 2/5/6 (DichVu/NapThe/ThuKhac) KHÔNG verify → productItemId=Empty, item vẫn add (CartController.cs:115-142) |
| 3 | Lấy tóm tắt giỏ (badge) | icon giỏ header | GET `GetShopCart` | — | anonymous | luôn | Đọc cookie, `ShopCartGroup` tính `Count` + `TotalBill` (bỏ kho `-2`), trả JSON `CartSummaryModel` | EF | `if (1 == 1)` dead-guard (CartController.cs:266) |
| 4 | Thêm / set số lượng | thêm vào giỏ (set quantity) | GET `Cart/{id}/{productItemId}/{quantity}` | — | anonymous | luôn | Set quantity (không cộng dồn — gán đè); **reset toàn bộ KM** của group (PreDiscount/TotalDiscount/NetPrice/Gift); gom kho `-1` nếu đã tách | EF | Add và HomeAddPlus logic trùng lặp lớn (copy-paste) |
| 5 | Xóa khỏi giỏ | nút xóa dòng | DELETE `Cart/{id}/{productItemId}` | — | anonymous | per-row | Lọc bỏ item theo (id, productItemId); xóa cả group nếu rỗng; reset KM group | EF | - |
| 6 | Mở trang thanh toán | "Thanh toán" | GET `/CheckOutNow` · `/thanh-toan-don-hang` | — | anonymous | giỏ != rỗng (rỗng→redirect `/`) | Tính VAT (`TinhToanVat`), prefill thông tin khách nếu đã login, set địa chỉ (tỉnh/huyện/xã), nếu giỏ **>1 group (đã tách kho)** → redirect `/Checkout`; render `Home/CheckOutNow.cshtml` | EF (CauHinhCore VAT + AddressSystem) | - |
| 7 | Lưu thông tin bước 1 | form địa chỉ/khách (step1) | POST `/Cart/SaveCheckOutStep1` | — | anonymous | màn checkout | `TryUpdateModelAsync` → lưu `OrderLinkAffliateModel` vào cookie `{domain}_info` | cookie | `[ValidateAntiForgeryToken]` bị comment (CartController.cs:571) — không chống CSRF |
| 8 | Tạo đơn hàng | "Đặt hàng" / "Hoàn tất" | POST `Cart/CreateOrder` | — | anonymous | giỏ + info hợp lệ | Dựng `WebsiteDonHang` từ cookie info + giỏ, tách sản phẩm/thẻ/combo, map UTM từ cookie `UtmTracking`, **lưu Contact** (InsertContactWebsitePrivate) + **lưu đơn website** (ThemMoiDonHangAsync, transaction), clear cookie giỏ; nếu LoaiThanhToan=ChuyenKhoan→trả link `/OrderBankTranfers/{maThanhToan}` | EF (transaction) | `[ValidateAntiForgeryToken]` bị comment; chỉ lấy `productCarts.FirstOrDefault()` → nếu giỏ đã tách nhiều group thì CHỈ tạo đơn cho group đầu (xem Lỗi đã biết) |
| 9 | Dropdown quận/huyện | select địa chỉ | POST `Cart/Districts` (codeProvince) | — | anonymous | màn checkout | Trả list quận/huyện theo mã tỉnh (GetAddressByLevelPrivate level 2) | EF | - |
| 10 | Dropdown xã/phường | select địa chỉ | POST `Cart/Wards` (codeDistric) | — | anonymous | màn checkout | Trả list xã/phường theo mã huyện (level 3) | EF | - |
| 11 | Trang chuyển khoản | sau khi đặt đơn chuyển khoản | GET `OrderBankTranfers/{code}` | — | anonymous | sau tạo đơn CK | `GetOrderBankTranfers(domain, code)` → render `Home/OrderBankTranfers.cshtml` (thông tin chuyển khoản) | EF | - |

## Quy tắc nghiệp vụ (gating giỏ hàng / tách đơn)
1. **Loại item (`TypeChiTietDonHang`)**: 1=SanPham, 2=DichVu, 3=Combo, 4=The, 5=NapThe, 6=ThuKhac. Thêm nhanh (`HomeCartPlus`) chỉ verify tồn tại cho SanPham/Combo/The; loại khác add thẳng.
2. **Kho mặc định = `-1`** ("Kho mặc định"). Kho `-2` là group đặc biệt **bị loại khỏi đếm/tổng tiền** và bỏ qua khi gom lại.
3. **Gom đơn**: khi thêm item mới mà giỏ KHÔNG có kho `-1` HOẶC có >1 group → dồn toàn bộ item về 1 group kho `-1` (để tách lại sau ở backend). Hàm `AddToProductCart` cộng dồn theo (Id, IdProductItem).
4. **Tách đơn ở checkout**: `CheckOutNow` nếu `cartGroup.Count > 1` (đã tách kho) → **redirect `/Checkout`** (màn tách đơn riêng — controller khác). Đơn 1 group đi tiếp CheckOutNow.
5. **Cleanup mỗi thao tác**: item `Quantity <= 0` bị lọc bỏ; group rỗng bị xóa khỏi cookie.
6. **VAT**: chỉ tính nếu `CauHinhCore.IsSuDungVat == true`; giá trị = `Lib.TinhGiaTri(ValueThueVat, TypeThueVat, thanhTien)` — cộng vào ThanhTien.
7. **Tạo đơn**: lưu **Contact trước** (InsertContactWebsitePrivate) rồi **lưu đơn website** (transaction, ThemMoiDonHangAsync). Đơn CK → redirect trang chuyển khoản; đơn khác → về `/`.

## Quan hệ
- **Gọi sang:** `ICommonApiService` (ShopCartGroup gán giá, TimSanPhamTheoId/TimComBoSpaTheoId/TimTheDichVuTheoId verify, GetAddressByLevelPrivate, GetCauHinhCore VAT, InsertContactWebsitePrivateAsync) → các private API của CommonApiController + service Contact/Core; `IWebsiteDonHangService.ThemMoiDonHangAsync` (lưu đơn); `ILinkWebsiteService` (resolve domain→đơn vị/theme); `IWebsiteCookieService` (cookie DB giỏ hàng); `ITokenService` (khách đăng nhập).
- **Dữ liệu (entities):** `WebsiteDonHang` (+ chi tiết `WebsiteDonHangSanPham`/`The`/`Combo`), giỏ tạm `ProductCartGroup`/`ProductCart` (cookie), `OrderLinkAffliateModel` (thông tin khách + địa chỉ + UTM).
- **Ripple / đồng bộ:** `CreateOrder` → ghi **Contact** (cross-service) + **WebsiteDonHang** (DB website). Đổi giá/ngừng bán sản phẩm-dịch vụ-combo-thẻ bên WarehouseProduct ảnh hưởng trực tiếp giá hiển thị trong giỏ (giá gán lúc ShopCartGroup, không snapshot trong cookie).

## Luồng xuyên module (tạo đơn website)
`CreateOrder` (POST) → đọc cookie giỏ + info → `ShopCartGroup` (gán giá hiện tại) → dựng `WebsiteDonHang` + tách SanPham/The/Combo → map UTM từ cookie `UtmTracking` → `InsertContactWebsitePrivateAsync` (tạo/ghi Contact, kèm danh sách sản phẩm/combo/thẻ) → `ThemMoiDonHangAsync` (transaction: insert đơn + chi tiết, rollback nếu lỗi) → clear cookie giỏ → trả link chuyển khoản hoặc về trang chủ.

## Lỗi đã biết / lưu ý kỹ thuật
- **Tạo đơn chỉ xử lý group đầu tiên** (`CartController.cs:641` `var lstProduct = productCarts.FirstOrDefault();`): nếu giỏ đã tách nhiều kho, CreateOrder chỉ lấy `Cart` của group đầu → các group còn lại bị bỏ khi lên đơn ở luồng này. (Luồng tách đơn nhiều kho lẽ ra đi qua `/Checkout` — controller khác.)
- **Thêm nhanh không verify với type DichVu/NapThe/ThuKhac** (`CartController.cs:115-142`): chỉ SanPham/Combo/The được `Tim...TheoId`. Type khác → `productItemId = Guid.Empty`, vẫn add vào giỏ (không chặn item không tồn tại).
- **CSRF**: `[ValidateAntiForgeryToken]` bị comment ở `SaveCheckOutStep1` (:571) và không có ở `CreateOrder` → POST tạo đơn không chống giả mạo.
- **Dead code / guard giả**: `if (1 == 1)` (`GetShopCart` :266), nhiều khối reset KM bị comment lại trong Add/Remove.
- **Auth `anonymous` có chủ ý** nhưng cần lưu: `[TokenAuthentication]` KHÔNG chặn — chỉ best-effort refresh token + prefill khách; mọi endpoint giỏ/checkout truy cập được không cần đăng nhập (đúng bản chất storefront), khách vô danh vẫn tạo được đơn (IdKhachHang = Guid.Empty).
- **Cookie là source of truth của giỏ** (`{domain}_cart`) — không persist DB cho tới khi CreateOrder; mất cookie = mất giỏ.
