---
controller: PromotionController
module: website
service: Website
man_hinh_ui: "Storefront công khai (theme) — chi tiết khuyến mãi (`Promotion/{id}`), danh sách khuyến mãi (`DanhSachKhuyenMai`), tìm khuyến mãi (`SearchDanhSachKhuyenMai`); render `~/Views/Themes/{ThemeCode}/Home/PromotionDetail|DanhSachKhuyenMai.cshtml`"
muc_dich: "Render trang khuyến mãi storefront: chi tiết 1 KM, danh sách KM có phân trang, tìm KM theo keyword — lấy data từ module Promotion qua CommonApiService."
alias: ["trang khuyến mãi website", "promotion storefront", "chi tiết khuyến mãi website", "danh sách khuyến mãi website"]
auth: mixed                # [TokenAuthentication] storefront resolve theo domain
state_machine: []
deprecated: false
entities: [KhuyenMai, LinkWebsite]
stored_procs: []
depends: [Website (ICommonApiService, ILinkWebsiteService), Promotion]
cross_module_in: []
cross_module_out: []
---

# PromotionController — Trang khuyến mãi storefront

> Controller MVC `Website.API/Controllers/PromotionController.cs`, base `Controller`, `[TokenAuthentication]`. Storefront resolve theo domain. Lấy KM qua `ICommonApiService` (proxy sang module Promotion).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Chi tiết khuyến mãi | click KM | GET `Promotion/{promotionId:Guid}` | — | mixed | storefront | `GetChiTietKhuyenMai` theo Id + đơn vị; `NotFound` nếu không thấy; gán `KhuyenMaiGetByIdDto`, render `Home/PromotionDetail.cshtml` | EF qua CommonApiService → Promotion | - |
| 2 | Danh sách khuyến mãi | menu KM | GET `DanhSachKhuyenMai` | — | mixed | storefront | 12 KM/trang (`PrivateGetKhuyenMai`), set `PaginationKhuyenMai.TotalPage`, render `Home/DanhSachKhuyenMai.cshtml` | EF qua CommonApiService | - |
| 3 | Tìm khuyến mãi (AJAX) | ô tìm kiếm KM | GET `SearchDanhSachKhuyenMai?keyword=&idNhom=&pageIndex=1&pageSize=12` | — | mixed | storefront | Gọi `PrivateGetKhuyenMai` keyword (IdNhom=null), trả `Json(model)` | EF qua CommonApiService | tham số `idNhom` ép Guid.Empty nhưng payload set IdNhom=null (không dùng) |

## Quan hệ
- **Gọi sang:** `ICommonApiService` (`GetChiTietKhuyenMai`, `GetKhuyenMai`), `ILinkWebsiteService.GetDataByDomain`.
- **Dữ liệu (entities):** `KhuyenMai` (qua `Promotion.Client.Dto.KhuyenMai`), `LinkWebsite`.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- Logger inject `ILogger<HomeController>` (copy-paste pattern).
- `model.PaginationKhuyenMai` được giả định non-null khi tính TotalPage — nếu service trả lỗi và không khởi tạo Pagination có thể NRE (cần xác nhận model khởi tạo sẵn).
