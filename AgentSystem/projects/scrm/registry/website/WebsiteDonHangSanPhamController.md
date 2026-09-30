---
controller: WebsiteDonHangSanPhamController
module: website
service: Website
man_hinh_ui: "Quản trị website — chi tiết sản phẩm trong đơn hàng website (grid sản phẩm của 1 đơn online); thường nhúng trong màn chi tiết đơn website"
muc_dich: "Tra cứu dòng sản phẩm của đơn hàng website (WebsiteDonHangSanPham) theo điều kiện — chi tiết sản phẩm thuộc đơn online. Chỉ 1 endpoint đọc."
alias: ["sản phẩm đơn hàng website", "chi tiết sản phẩm đơn website", "website don hang san pham"]
auth: anonymous            # [ApiController] không [SBAuthorize] ở controller/action
state_machine: []
deprecated: false
entities: [WebsiteDonHangSanPham]
stored_procs: []
depends: [Website (IWebsiteDonHangSanPhamService)]
cross_module_in: []
cross_module_out: []
---

# WebsiteDonHangSanPhamController — Chi tiết sản phẩm đơn hàng website

> Controller `Website.API/Controllers/Api/WebsiteDonHangSanPhamController.cs`, route `api/WebsiteDonHangSanPham`, base `BaseCoreController`, `[ApiController]`, `partial`. 1 action đọc, **không gắn `[SBAuthorize]`** → anonymous.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | grid SP trong đơn | POST `api/WebsiteDonHangSanPham/TimTheoDieuKien` | — (không SBAuthorize) | anonymous | màn chi tiết đơn | `TimTheoDieuKienAsync(gridInfo)` — trả list dòng sản phẩm của đơn website theo filter | EF (WebsiteDonHangSanPham) | KHÔNG có permission key/SBAuthorize |

## Quan hệ
- **Gọi sang:** `IWebsiteDonHangSanPhamService.TimTheoDieuKienAsync`.
- **Dữ liệu (entities):** `WebsiteDonHangSanPham` (dòng sản phẩm thuộc `WebsiteDonHang`).
- **Ripple / đồng bộ:** không (đọc). Đơn cha xem `WebsiteDonHangController`.

## Lỗi đã biết / lưu ý kỹ thuật
- Không có `[SBAuthorize]` → endpoint không kiểm quyền; `TimTheoDieuKienAsync(gridInfo)` không truyền `CurrentUser` → không scope user/đơn vị (xác nhận filter).
- `partial class` — có thể còn endpoint ở file partial khác.
- Cùng khuôn với `WebsiteDonHangComboController` / `WebsiteDonHangTheController` (3 controller chi tiết đơn website cùng pattern thin 1-action).
