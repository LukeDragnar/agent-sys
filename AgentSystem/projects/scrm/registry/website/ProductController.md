---
controller: SanPhamController            # class trong file ProductController.cs là SanPhamController
module: website
service: Website
man_hinh_ui: "Storefront công khai (theme) — trang chi tiết sản phẩm (`SanPham/{SlugCha}/...`), danh sách sản phẩm (`DanhSachProduct`), trang ngành hàng (`cat/{slugNganhNghe}`), tìm sản phẩm (`SearchDanhSachProduct`); render `~/Views/Themes/{ThemeCode}/Home/ProductDetail|DanhSachProduct*.cshtml`"
muc_dich: "Render trang sản phẩm storefront: chi tiết sản phẩm cha theo slug, danh sách sản phẩm, trang ngành hàng (có nhánh: ngành có con → hiển danh mục con; không có con → list sản phẩm), tìm kiếm sản phẩm."
alias: ["trang sản phẩm website", "product storefront", "chi tiết sản phẩm website", "trang ngành hàng", "cat ngành hàng", "search sản phẩm website"]
auth: mixed                # [TokenAuthentication] storefront resolve theo domain
state_machine: []
deprecated: false
entities: [SanPham, DMNganhHang, LinkWebsite]
stored_procs: []
depends: [Website (ICommonApiService, ILinkWebsiteService), WarehouseProduct (SanPham qua ClientHelper)]
cross_module_in: []
cross_module_out: []
---

# SanPhamController (file ProductController.cs) — Trang sản phẩm storefront

> Controller MVC `Website.API/Controllers/ProductController.cs` — **class tên `SanPhamController`** (lệch tên file). Base `Controller`, `[TokenAuthentication]`. Storefront resolve theo domain như NewsController. Gọi sản phẩm qua `ICommonApiService` và trực tiếp `WarehouseProduct/api/SanPham/LayTatCaSpCha_Private` (qua `IClientHelper`).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Chi tiết sản phẩm (theo slug) | click sản phẩm | GET `SanPham/{SlugCha}/{ThuocTinhValue1?}/{ThuocTinhValue2?}/{ThuocTinhValue3?}` | — | mixed | storefront | `TimSanPhamChaTheoSlug_Private` theo slug + đơn vị + thuộc tính; gán ngành hàng/ảnh (split `WebsiteIdImages` theo `;`); nạp 4 SP cùng ngành; render `Home/ProductDetail.cshtml` | EF qua CommonApiService | Có `IndexOld` (theo Id) **đã comment** (L46-112) — bản cũ dùng SanPhamChaId, nay dùng slug |
| 2 | Danh sách sản phẩm | menu Sản phẩm | GET `DanhSachProduct` | — | mixed | storefront | 12 SP/trang (`PrivateGetSanPham`), tính TotalPage, render `Home/DanhSachProduct.cshtml` | EF qua CommonApiService | - |
| 3 | Trang ngành hàng | menu ngành hàng | GET `cat/{slugNganhNghe}/{page?}/{search?}` | — | mixed | storefront | PAGE_SIZE=12. **Rẽ nhánh:** nếu ngành có ≥1 ngành con → với mỗi ngành con lấy 4 SP, render `DanhSachProductCase1.cshtml` (tuple model); nếu không có con → list SP của chính ngành đó, render `DanhSachProduct.cshtml`. Không có `LstDmNganhHangDto` → NotFound | EF qua CommonApiService (`GetSanPhamCha`) | - |
| 4 | Tìm sản phẩm (AJAX) | ô tìm kiếm SP | GET `SearchDanhSachProduct?keyword=&idNhom=&pageIndex=1&pageSize=12` | — | mixed | storefront | Gọi `PrivateGetSanPham` keyword (IdNhom=null), trả `Json(model)` | EF qua CommonApiService | `idNhom` rỗng ép Guid.Empty nhưng payload set `IdNhom=null` (tham số idNhom không dùng tới) |

## Quan hệ
- **Gọi sang:** `ICommonApiService` (`TimSanPhamChaTheoSlug_Private`, `GetSanPham`, `GetSanPhamCha`), `ILinkWebsiteService.GetDataByDomain`, và HTTP trực tiếp `WarehouseProductServiceUrl/api/SanPham/LayTatCaSpCha_Private` (hàm `GetSanPhamCha`).
- **Dữ liệu (entities):** `SanPham`/`SanPhamCha`, `DMNganhHang` (cây ngành hàng — ParentId), `LinkWebsite`.
- **Ripple / đồng bộ:** không (chỉ đọc/render).

## Lỗi đã biết / lưu ý kỹ thuật
- **Lệch tên file/class:** file `ProductController.cs` chứa class `SanPhamController` → tìm theo tên file ra Product, theo class ra SanPham. Node này dùng `controller: SanPhamController` (theo class), filename node = `ProductController.md` (theo file).
- L46-112: block `IndexOld` (route theo SanPhamChaId) đã comment — bản cũ; bản dùng là route theo slug (L119+).
- Ảnh: `WebsiteIdImages` là chuỗi nối bằng `;`; rỗng → fallback `IdImage` đơn.
- Logger inject `ILogger<HomeController>` (copy-paste, sai category).
