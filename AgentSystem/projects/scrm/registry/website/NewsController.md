---
controller: NewsController
module: website
service: Website
man_hinh_ui: "Storefront công khai (theme) — trang chi tiết tin tức (route `TinTuc/{id}`), trang danh sách tin (route `DanhSachNews`) + ô tìm kiếm tin (`SearchListTinTuc`); render view `~/Views/Themes/{ThemeCode}/Home/*.cshtml`"
muc_dich: "Render trang tin tức của website bán hàng (storefront): chi tiết 1 tin, danh sách tin có phân trang, tìm kiếm tin theo nhóm/keyword — lấy data tin tức từ Website service qua endpoint Private."
alias: ["trang tin tức website", "news storefront", "chi tiết tin tức", "danh sách tin tức website", "search tin tức website"]
auth: mixed                # controller [TokenAuthentication]; nhưng là storefront resolve theo domain, không phải user nội bộ
state_machine: []
deprecated: false
entities: [TinTuc, LinkWebsite]
stored_procs: []
depends: [Website (ITinTucService, ILinkWebsiteService), CommonApiService]
cross_module_in: []
cross_module_out: []
---

# NewsController — Trang tin tức storefront (website bán hàng)

> Controller MVC `Website.API/Controllers/NewsController.cs`, base `Controller` (KHÔNG phải `BaseCoreController`), attribute `[TokenAuthentication]`. Đây là **storefront public**: mỗi request resolve dữ liệu website theo `Request.Host.Value` (domain) qua `ILinkWebsiteService.GetDataByDomain`; nếu không match domain → `NotFound`. Trả `View(...)` (Razor) hoặc `Json(model)` cho AJAX, KHÔNG trả API CRUD.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Xem chi tiết 1 tin | click tin | GET `TinTuc/{TinTucId:Guid}` | — | mixed | trang storefront | Resolve domain → lấy tin qua `GetByIdAsync_Private`; `NotFound` nếu không thấy. Convert MoTa sang iframe (`Helper.ConvertToIframe`), nạp 5 tin liên quan (`PrivateGetTinTuc` PageSize=5), render `Home/NewsDetail.cshtml` | EF (TinTuc) qua service Private | MoTa render iframe — chú ý XSS/encode |
| 2 | Danh sách tin (trang) | menu Tin tức | GET `DanhSachNews` | — | mixed | storefront | Lấy 12 tin/trang (`PrivateGetTinTuc` PageSize=12), tính `TotalPage`, set ViewBag phân trang, render `Home/DanhSachNews.cshtml` | EF qua service Private | TotalPage ban đầu gán = TotalRecord rồi mới chia 12 (xem lưu ý) |
| 3 | Tìm/lọc tin (AJAX) | ô tìm kiếm tin | GET `SearchListTinTuc?keyword=&idNhom=&pageIndex=1&pageSize=12` | — | mixed | storefront | `idNhom` rỗng → ép Guid.Empty; gọi `PrivateGetTinTuc` theo nhóm + keyword + chi nhánh kho default; trả `Json(model)` | EF qua service Private | Trả JSON cả model lớn `AllDataCommonModel` — payload nặng |

## Quan hệ
- **Gọi sang:** `ILinkWebsiteService.GetDataByDomain` (resolve website theo domain + set cookie cart), `ITinTucService.LayTatCa_Private` / `GetByIdAsync_Private`.
- **Dữ liệu (entities):** `TinTuc` (qua DTO), `LinkWebsite` (cấu hình domain→đơn vị, ThemeCode, IdChiNhanhKhoDefault).
- **Ripple / đồng bộ:** không (chỉ đọc/ render).

## Lỗi đã biết / lưu ý kỹ thuật
- `Controllers/NewsController.cs:20` inject `ILogger<HomeController>` (logger category của controller khác) — pattern copy-paste lặp ở hầu hết controller storefront.
- L109-112 / L158-161: gán `model.TotalPage = TotalRecord` (số bản ghi) rồi sau đó `model.TotalPage = totalPages` (số trang) — tính qua biến trung gian; nếu refactor đừng nhầm record vs page.
- Mọi action phụ thuộc resolve domain thành công — domain lạ/chưa cấu hình LinkWebsite → trả `NotFound` (không phải lỗi auth).
