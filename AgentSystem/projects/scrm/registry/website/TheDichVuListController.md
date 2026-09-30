---
controller: TheDichVuListController
module: website
service: Website
man_hinh_ui: "Storefront công khai (theme) — trang Danh sách thẻ dịch vụ (`TheDichVuList`): vừa list thẻ đang bán, vừa list thẻ khách đã mua; ô tìm (`SearchListTheDichVu`); render `~/Views/Themes/{ThemeCode}/Home/TheDichVuList.cshtml`"
muc_dich: "Render trang danh sách thẻ dịch vụ storefront: đồng thời nạp 2 nguồn — thẻ dịch vụ đang bán (theo đơn vị) và thẻ đã mua (theo token khách), có phân trang + tìm kiếm."
alias: ["danh sách thẻ dịch vụ website", "the dich vu list storefront", "thẻ của tôi website", "list thẻ đã mua"]
auth: mixed                # [TokenAuthentication] ở method-level + token khách qua ITokenService
state_machine: []
deprecated: false
entities: [TheDichVu, TheDichVuKhachHang, LinkWebsite]
stored_procs: []
depends: [Website (ICommonApiService, ILinkWebsiteService, ITokenService)]
cross_module_in: []
cross_module_out: []
---

# TheDichVuListController — Danh sách thẻ dịch vụ storefront

> Controller MVC `Website.API/Controllers/TheDichVuListController.cs`, base `Controller`. **Auth ở method-level** (`[TokenAuthentication]` trên từng action, KHÔNG ở controller). Mỗi action nạp song song 2 nguồn: thẻ đang bán (`GetTheDichVu` theo IdDonVi) + thẻ đã mua (`GetListTheDaMua` theo token khách).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Trang danh sách thẻ | menu Thẻ dịch vụ | GET `TheDichVuList` | — (token) | mixed | storefront | 12/trang. Nạp `PrivateGetListThe` (thẻ bán, set `TotalPage`=TotalRecord) + `PrivateGetTheDaMua` (thẻ đã mua theo token, set `TotalRecord`); tính 2 bộ phân trang (TheDichVu & TheDaMua); render `Home/TheDichVuList.cshtml` | EF qua CommonApiService | dùng `TotalPage`/`TotalRecord` cho 2 nguồn khác nhau — dễ nhầm |
| 2 | Tìm thẻ (AJAX) | ô tìm kiếm thẻ | GET `SearchListTheDichVu?keyword=&pageIndex=1&pageSize=12` | — (token) | mixed | storefront | Như #1 nhưng có keyword + page; trả `Json(model)` | EF qua CommonApiService | chia trang hardcode `/12` dù `pageSize` param (xem lưu ý) |

## Quan hệ
- **Gọi sang:** `ICommonApiService` (`GetTheDichVu` — thẻ bán theo IdDonVi; `GetListTheDaMua` — thẻ đã mua theo token), `ITokenService.GetToken`, `ILinkWebsiteService.GetDataByDomain`.
- **Dữ liệu (entities):** `TheDichVu` (đang bán), thẻ dịch vụ khách hàng đã mua, `LinkWebsite`.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- Tính số trang dùng hằng `/12` (L99-110, L159-171) kể cả khi `pageSize` truyền khác 12 → phân trang lệch nếu pageSize ≠ 12.
- `model.TotalPage` lúc đầu chứa TotalRecord (số bản ghi) rồi mới chuyển thành số trang — đọc kỹ vòng gán.
- Auth method-level: action mới phải tự gắn `[TokenAuthentication]`.
