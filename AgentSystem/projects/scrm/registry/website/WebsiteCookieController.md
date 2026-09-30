---
controller: WebsiteCookieController
module: website
service: Website
man_hinh_ui: "nội bộ — không có endpoint hoạt động (action duy nhất đã comment); placeholder cho quản lý cấu hình cookie website"
muc_dich: "Controller placeholder cho WebsiteCookie — hiện KHÔNG có endpoint nào hoạt động (TimTheoDieuKien đã comment). Chỉ inject service, chưa expose API."
alias: ["website cookie", "cấu hình cookie website", "quản lý cookie website"]
auth: anonymous            # không action nào active; controller [ApiController] không SBAuthorize
state_machine: []
deprecated: false          # không khóa cứng, nhưng rỗng action
entities: [WebsiteCookie]
stored_procs: []
depends: [Website (IWebsiteCookieService)]
cross_module_in: []
cross_module_out: []
---

# WebsiteCookieController — (placeholder, chưa có endpoint active)

> Controller `Website.API/Controllers/Api/WebsiteCookieController.cs`, route `api/WebsiteCookie`, base `BaseCoreController`, `[ApiController]`, `partial`. **Không có action hoạt động** — action `TimTheoDieuKien` duy nhất đã bị comment (L26-31). Chỉ inject `IWebsiteCookieService`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | (Tìm theo điều kiện — ĐÃ COMMENT) | — | ~~POST `api/WebsiteCookie/TimTheoDieuKien`~~ | — | — | không khả dụng | Endpoint bị comment toàn bộ → controller hiện rỗng, không expose API nào | EF (nếu mở lại) | Toàn bộ action comment → node chỉ ghi nhận sự tồn tại; có thể là partial, endpoint thật ở file partial khác (cần xác nhận) |

## Quan hệ
- **Gọi sang:** `IWebsiteCookieService.TimTheoDieuKienAsync` (chỉ khi un-comment).
- **Dữ liệu (entities):** `WebsiteCookie` (cấu hình cookie/consent của website).
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- Controller hiện **không có endpoint active** — nếu cần WebsiteCookie thật, kiểm tra file `partial` khác (`partial class WebsiteCookieController`) hoặc service được gọi nơi khác.
- Không khóa cứng (không `deprecated`), chỉ là placeholder/scaffold.
