---
controller: WebsiteDonHangTheController
module: website
service: Website
man_hinh_ui: "Quản trị website — chi tiết thẻ dịch vụ trong đơn hàng website (grid thẻ của 1 đơn online); thường nhúng trong màn chi tiết đơn website"
muc_dich: "Tra cứu dòng thẻ dịch vụ của đơn hàng website (WebsiteDonHangThe) theo điều kiện — chi tiết thẻ thuộc đơn online. Chỉ 1 endpoint đọc."
alias: ["thẻ đơn hàng website", "chi tiết thẻ đơn website", "website don hang the"]
auth: anonymous            # [ApiController] không [SBAuthorize] ở controller/action
state_machine: []
deprecated: false
entities: [WebsiteDonHangThe]
stored_procs: []
depends: [Website (IWebsiteDonHangTheService)]
cross_module_in: []
cross_module_out: []
---

# WebsiteDonHangTheController — Chi tiết thẻ dịch vụ đơn hàng website

> Controller `Website.API/Controllers/WebsiteDonHangTheController.cs`, route `api/WebsiteDonHangThe`, base `BaseCoreController`, `[ApiController]`, `partial`. 1 action đọc, **không gắn `[SBAuthorize]`** → anonymous.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | grid thẻ trong đơn | POST `api/WebsiteDonHangThe/TimTheoDieuKien` | — (không SBAuthorize) | anonymous | màn chi tiết đơn | `TimTheoDieuKienAsync(gridInfo)` — trả list dòng thẻ dịch vụ của đơn website theo filter | EF (WebsiteDonHangThe) | KHÔNG có permission key/SBAuthorize |

## Quan hệ
- **Gọi sang:** `IWebsiteDonHangTheService.TimTheoDieuKienAsync`.
- **Dữ liệu (entities):** `WebsiteDonHangThe` (dòng thẻ dịch vụ thuộc `WebsiteDonHang`).
- **Ripple / đồng bộ:** không (đọc). Đơn cha xem `WebsiteDonHangController`.

## Lỗi đã biết / lưu ý kỹ thuật
- Không có `[SBAuthorize]` → endpoint không kiểm quyền; `TimTheoDieuKienAsync(gridInfo)` không truyền `CurrentUser` → không scope user/đơn vị (xác nhận filter).
- `partial class` — có thể còn endpoint ở file partial khác.
- Cùng khuôn với `WebsiteDonHangComboController` / `WebsiteDonHangSanPhamController`.
