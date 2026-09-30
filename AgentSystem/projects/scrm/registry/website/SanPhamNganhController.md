---
controller: SanPhamNganhController
module: website
service: Website
man_hinh_ui: "nội bộ — API cho app mobile (MobileApi); không có màn quản trị web. Cấp dữ liệu sản phẩm theo ngành cho app khách hàng"
muc_dich: "Endpoint mobile lấy sản phẩm theo slug ngành (GetSanPhamTheoSlug) và chi tiết sản phẩm (GetSanPhamChiTiet) cho app khách hàng; xác thực token mobile."
alias: ["sản phẩm theo ngành mobile", "san pham nganh app", "chi tiết sản phẩm mobile", "product by slug mobile"]
auth: mixed                # [TokenMobileAuthentication] ở method-level
state_machine: []
deprecated: false
entities: [SanPham, DMNganhHang]
stored_procs: []
depends: [Website (ISanPhamService)]
cross_module_in: []
cross_module_out: []
---

# SanPhamNganhController — Sản phẩm theo ngành (app mobile)

> Controller `Website.API/Controllers/MobileApi/SanPhamNganhController.cs`, route `api/SanPhamNganh`, base `BaseCoreController`, `[ApiController]`. Action gắn `[TokenMobileAuthentication]` (token app mobile). 2 endpoint đọc sản phẩm cho app khách.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy SP theo slug ngành | màn ngành hàng (app) | POST `api/SanPhamNganh/GetSanPhamTheoSlug` (`DSSanPhamNganhReq`) | — (token mobile) | mixed | app đã đăng nhập | `GetSanPhamTheoSlug(req)` — list sản phẩm theo slug ngành | EF (service) | không có permission key, chỉ token mobile |
| 2 | Chi tiết sản phẩm | màn chi tiết SP (app) | POST `api/SanPhamNganh/GetSanPhamChiTiet` (`SanPhamChiTietReq`) | — (token mobile) | mixed | app đã đăng nhập | `GetSanPhamChiTiet(req)` — chi tiết 1 sản phẩm | EF (service) | - |

## Quan hệ
- **Gọi sang:** `ISanPhamService` (GetSanPhamTheoSlug, GetSanPhamChiTiet).
- **Dữ liệu (entities):** `SanPham`, `DMNganhHang` (slug ngành).
- **Ripple / đồng bộ:** không (đọc).

## Lỗi đã biết / lưu ý kỹ thuật
- Auth method-level `[TokenMobileAuthentication]` — controller-level không có `[SBAuthorize]`; action mới phải tự gắn token-attribute.
- Tương đương storefront `ProductController` nhưng cho app mobile (trả JSON thay vì View).
