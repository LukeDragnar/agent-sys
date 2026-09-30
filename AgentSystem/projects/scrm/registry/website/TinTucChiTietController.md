---
controller: TinTucChiTietController
module: website
service: Website
man_hinh_ui: "Storefront công khai (theme) — trang tin tức theo nhóm (`NhomTinTuc/{id}`) + ô tìm tin (`SearchTinTuc`); render `~/Views/Themes/{ThemeCode}/Home/DmNhomTinTucDetail.cshtml`"
muc_dich: "Render trang danh sách tin theo nhóm tin tức trên storefront (phân trang), và tìm tin theo nhóm + keyword — dùng endpoint Private LayTatCaTheoNhom."
alias: ["trang nhóm tin tức website", "tin tức theo nhóm storefront", "DmNhomTinTuc detail", "search tin theo nhóm"]
auth: anonymous            # KHÔNG có [TokenAuthentication] ở controller/action — storefront resolve theo domain
state_machine: []
deprecated: false
entities: [TinTuc, DMNhomTinTuc, LinkWebsite]
stored_procs: []
depends: [Website (ITinTucService, ILinkWebsiteService)]
cross_module_in: []
cross_module_out: []
---

# TinTucChiTietController — Trang tin tức theo nhóm (storefront)

> Controller MVC `Website.API/Controllers/TinTucChiTietController.cs`, base `Controller`. **KHÔNG gắn `[TokenAuthentication]`** (khác các storefront controller khác) → truy cập anonymous, chỉ resolve theo domain. Lấy tin theo nhóm qua `ITinTucService.LayTatCaTheoNhom_Private`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tin tức theo nhóm | click nhóm tin | GET `NhomTinTuc/{NhomTinTucId:Guid}` | — | anonymous | storefront | Resolve domain; lấy detail nhóm từ `dmNhomTinTucs`; nạp 12 tin/trang theo nhóm (`LayTatCaTheoNhom_Private`); render `Home/DmNhomTinTucDetail.cshtml` | EF qua service Private | - |
| 2 | Tìm tin theo nhóm (AJAX) | ô tìm | GET `SearchTinTuc?keyword=&idNhom=&pageIndex=1&pageSize=12` | — | anonymous | storefront | `idNhom` rỗng → Guid.Empty; nạp tin theo nhóm + keyword; trả `Json(model)` | EF qua service Private | tên controller "ChiTiet" nhưng làm danh sách theo nhóm (lệch ngữ nghĩa) |

## Quan hệ
- **Gọi sang:** `ITinTucService.LayTatCaTheoNhom_Private`, `ILinkWebsiteService.GetDataByDomain`.
- **Dữ liệu (entities):** `TinTuc`, `DMNhomTinTuc` (model.dmNhomTinTucs nạp sẵn từ cấu hình website), `LinkWebsite`.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- Tên controller `TinTucChiTiet` nhưng thực chất render **danh sách tin theo nhóm** (route `NhomTinTuc/...`) — lệch ngữ nghĩa, chi tiết 1 tin nằm ở `NewsController`.
- Khác biệt auth: controller này KHÔNG có `[TokenAuthentication]` (anonymous) trong khi News/Promotion/TheDichVu có — chú ý khi đồng bộ behavior storefront.
- Logger inject `ILogger<HomeController>` (copy-paste pattern).
