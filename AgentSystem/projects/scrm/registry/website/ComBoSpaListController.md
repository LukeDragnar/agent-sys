---
controller: ComBoSpaListController
module: website
service: Website
man_hinh_ui: "Trang storefront khách hàng (Razor view) — trang danh sách combo spa: `/ComboSpaList` render `ComBoSpaList.cshtml` theo theme đơn vị (hiển thị combo spa đang bán + combo đã mua của khách đăng nhập); search combo (ajax)"
muc_dich: "MVC controller render trang list combo spa trên website của đơn vị: combo đang bán (theo IdDonVi) + combo khách đã mua (theo token đăng nhập), cùng phân trang + search."
alias: ["combo spa website", "danh sách combo spa", "list combo storefront", "combo đã mua", "ComBoSpaList"]
auth: anonymous
state_machine: []
deprecated: false
entities: [LinkWebsite, ComBoSpa, ComBoSpaDaMua]
stored_procs: []
depends: [Website/ILinkWebsiteService, Website/ICommonApiService, Website/ITokenService (token khách hàng từ cookie), WarehouseProduct/Promotion (qua private endpoint)]
cross_module_in: []
cross_module_out: [WarehouseProduct/Promotion (GetComBoSpa — theo IdDonVi), Customer/Order (GetListComboDaMua — theo token khách)]
---

# ComBoSpaListController — Trang danh sách combo spa (storefront website)

> MVC controller (kế thừa `Controller`, trả `View(...)`), **không phải API**. Có `[TokenAuthentication]` ở từng action — **filter không chặn**: nếu không có token vẫn chạy (chỉ là sẽ không có "combo đã mua"). Định danh đơn vị qua **domain**; combo đã mua lấy theo **token khách hàng** (`_tokenService.GetToken()`). Không gắn `[SBAuthorize]`/permission key.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Mở trang list combo spa | menu "Combo spa" | GET `/ComboSpaList` `[TokenAuthentication]` | — | anonymous | luôn (combo đã mua chỉ khi có token) | Resolve đơn vị theo domain → load combo spa đang bán (page1,size12 theo IdDonVi) + combo đã mua (theo token) → render `ComBoSpaList.cshtml`; tính `ViewBag.TotalPageComboSpa` + `TotalPageComboDaMua`. NotFound nếu domain không map đơn vị | private API (CommonApiService) | - |
| 2 | Search combo | ô tìm kiếm + phân trang (ajax) | GET `/SearchListCombo?keyword=&pageIndex=&pageSize=` `[TokenAuthentication]` | — | anonymous | trên trang combo | Trả `Json(model)` combo đang bán theo keyword (phân trang) + combo đã mua theo token; cập nhật `TotalPage`, `PageNumber` | private API | - |

## Endpoint nội bộ (private / service-to-service)
- Controller là caller: `GetComBoSpa` (payload `LayTatCaPrivateFilterDto` có `IdDonVi`) cho combo đang bán; `GetListComboDaMua` (payload `LayTatCaPrivateWithoutIdDonViFilterDto` có `Token`) cho combo khách đã mua.

## Quan hệ
- **Gọi sang:** `ILinkWebsiteService.GetDataByDomain`; `ITokenService.GetToken` (cookie token khách); `ICommonApiService` → admin-API combo spa + combo đã mua.
- **Dữ liệu (entities):** `LinkWebsite`, `ComBoSpa`, `ComBoSpaDaMua` (chỉ đọc).
- **Ripple / đồng bộ:** chỉ đọc. "Combo đã mua" phụ thuộc đơn hàng combo của khách (đăng nhập qua cookie token).

## Lỗi đã biết / lưu ý kỹ thuật
- Trang `Index` tính `totalServices = model.TotalPage` rồi lại `Ceiling(totalServices/12)` (ComBoSpaListController.cs:99-102) — `TotalPage` vừa dùng làm "tổng bản ghi" vừa bị ghi đè thành "số trang"; field bị reuse 2 nghĩa, cẩn thận khi đọc view.
- `[TokenAuthentication]` là `ActionFilterAttribute` **không-chặn** (token null → `return` không set Result) → trang vẫn render cho khách chưa đăng nhập; chỉ thiếu phần combo đã mua. (TokenAuthorizationFilter.cs:58-63)
