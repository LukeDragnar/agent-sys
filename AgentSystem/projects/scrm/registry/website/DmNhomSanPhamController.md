---
controller: DmNhomSanPhamController
module: website
service: Website
man_hinh_ui: "Trang storefront khách hàng (Razor view) — trang chi tiết nhóm sản phẩm: `/NhomSanPham/{id}` render `DmNhomSanPhamDetail.cshtml` theo theme đơn vị; form search sản phẩm trong nhóm (ajax)"
muc_dich: "MVC controller render trang chi tiết một nhóm sản phẩm trên website bán hàng của đơn vị: list nhóm sản phẩm + chi tiết nhóm + danh sách sản phẩm trong nhóm (có phân trang + search)."
alias: ["nhóm sản phẩm website", "trang nhóm sản phẩm", "danh mục nhóm sản phẩm storefront", "category sản phẩm", "DmNhomSanPham"]
auth: anonymous
state_machine: []
deprecated: false
entities: [LinkWebsite, NhomSanPham, SanPham]
stored_procs: []
depends: [Website/ILinkWebsiteService (resolve domain→đơn vị), Website/ICommonApiService (gọi sang admin-API private), WarehouseProduct (qua private endpoint)]
cross_module_in: []
cross_module_out: [WarehouseProduct (GetDMNhomSanPham / GetChiTietNhomSanPham / GetSanPham — private theo IdDonVi)]
---

# DmNhomSanPhamController — Trang chi tiết nhóm sản phẩm (storefront website)

> MVC controller (kế thừa `Controller`, trả `View(...)`), **không phải API** — song sinh với `DmNhomDichVuController` nhưng cho **sản phẩm**. Định danh đơn vị qua **domain** (`GetDataByDomain`), không qua login; không gắn `[SBAuthorize]`/permission key. Dữ liệu thật lấy từ admin-side qua `_commonApiService.*` (private theo `IdDonVi`).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Mở trang nhóm sản phẩm | click vào 1 nhóm sản phẩm | GET `/NhomSanPham/{NhomSanPhamId}` | — | anonymous | luôn | Resolve đơn vị theo domain → load DS nhóm sản phẩm (page 1, size 12) + chi tiết nhóm + DS sản phẩm trong nhóm → render `DmNhomSanPhamDetail.cshtml` theo `ThemeCode`; set `model.TotalPage` + `ViewBag.TotalPagesSp`. NotFound nếu domain không map đơn vị | private API (CommonApiService) | - |
| 2 | Search/lọc sản phẩm trong nhóm | ô tìm kiếm + phân trang (ajax) | GET `/SearchSanPham?keyword=&idNhom=&pageIndex=&pageSize=` | — | anonymous | trên trang nhóm | Trả `Json(model)` DS sản phẩm theo keyword + idNhom (mặc định Guid.Empty nếu trống) phân trang; set `model.TotalPage`=totalPages, `PageNumber`=pageIndex | private API | - |

## Endpoint nội bộ (private / service-to-service)
- Không expose endpoint internal; controller là caller gọi sang admin-API: `GetDMNhomSanPham`, `GetChiTietNhomSanPham`, `GetSanPham` (payload `LayTatCaPrivateFilterDto` / `LayChiTietSpPrivateDto` mang `IdDonVi` + `IdChiNhanhKhoDefault`).

## Quan hệ
- **Gọi sang:** `ILinkWebsiteService.GetDataByDomain`; `ICommonApiService` → admin-API nhóm sản phẩm / sản phẩm (kho `WarehouseProduct`).
- **Dữ liệu (entities):** `LinkWebsite`, `NhomSanPham`, `SanPham` (chỉ đọc).
- **Ripple / đồng bộ:** chỉ đọc. Đổi nhóm/sản phẩm bên kho → phản ánh ngay lên trang storefront.

## Lỗi đã biết / lưu ý kỹ thuật
- Cùng pattern `new NotFound()` (class nội bộ) khi domain không resolve được đơn vị.
- Storefront public — không `[TokenAuthentication]`, không chặn login.
- Comment trong code copy nhầm từ bản dịch vụ ("Tổng số lượng dịch vụ", "Get chi tiết nhóm dịch vụ") nhưng thực tế thao tác trên sản phẩm — chỉ là comment lệch, logic đúng.
