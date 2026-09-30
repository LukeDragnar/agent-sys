---
controller: TheDichVuController
module: website
service: Website
man_hinh_ui: "Storefront công khai (theme) — trang chi tiết Thẻ dịch vụ (`TheDichVu/{id}`); render `~/Views/Themes/{ThemeCode}/Home/TheDichVuDetail.cshtml`"
muc_dich: "Render trang chi tiết 1 thẻ dịch vụ trên storefront + danh sách thẻ dịch vụ liên quan (4 thẻ)."
alias: ["trang thẻ dịch vụ website", "chi tiết thẻ dịch vụ storefront", "the dich vu detail website"]
auth: mixed                # [TokenAuthentication] storefront resolve theo domain
state_machine: []
deprecated: false
entities: [TheDichVu, LinkWebsite]
stored_procs: []
depends: [Website (ICommonApiService, ILinkWebsiteService)]
cross_module_in: []
cross_module_out: []
---

# TheDichVuController — Trang chi tiết thẻ dịch vụ storefront

> Controller MVC `Website.API/Controllers/TheDichVuController.cs`, base `Controller`, `[TokenAuthentication]`. Storefront resolve theo domain. Chỉ 1 action `Index` (chi tiết).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Chi tiết thẻ dịch vụ | click thẻ DV | GET `TheDichVu/{TheDichVuId:Guid}` | — | mixed | storefront | `TimTheDichVuTheoId` theo Id + đơn vị; `NotFound` nếu không thấy; gán `model.TheDichVu`; nạp 4 thẻ liên quan (`PrivateGetTheDichVu` PageSize=4); render `Home/TheDichVuDetail.cshtml` | EF qua CommonApiService | - |

## Quan hệ
- **Gọi sang:** `ICommonApiService` (`TimTheDichVuTheoId`, `GetTheDichVu`), `ILinkWebsiteService.GetDataByDomain`.
- **Dữ liệu (entities):** `TheDichVu`, `LinkWebsite`.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- Logger inject `ILogger<HomeController>` (copy-paste pattern).
- Storefront read-only; danh sách thẻ + thẻ đã mua xem `TheDichVuListController`.
