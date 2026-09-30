---
controller: WebsiteDonHangComboController
module: website
service: Website
man_hinh_ui: "Quản trị website — chi tiết combo trong đơn hàng website (grid combo của 1 đơn online); thường nhúng trong màn chi tiết đơn website"
muc_dich: "Tra cứu dòng combo của đơn hàng website (WebsiteDonHangCombo) theo điều kiện — chi tiết combo thuộc đơn online. Chỉ 1 endpoint đọc."
alias: ["combo đơn hàng website", "chi tiết combo đơn website", "website don hang combo"]
auth: anonymous            # [ApiController] không [SBAuthorize] ở controller/action
state_machine: []
deprecated: false
entities: [WebsiteDonHangCombo]
stored_procs: []
depends: [Website (IWebsiteDonHangComboService)]
cross_module_in: []
cross_module_out: []
---

# WebsiteDonHangComboController — Chi tiết combo đơn hàng website

> Controller `Website.API/Controllers/WebsiteDonHangComboController.cs`, route `api/WebsiteDonHangCombo`, base `BaseCoreController`, `[ApiController]`, `partial`. 1 action đọc, **không gắn `[SBAuthorize]`** → anonymous (chỉ qua pipeline auth mặc định nếu có).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | grid combo trong đơn | POST `api/WebsiteDonHangCombo/TimTheoDieuKien` | — (không SBAuthorize) | anonymous | màn chi tiết đơn | `TimTheoDieuKienAsync(gridInfo)` — trả list dòng combo của đơn website theo filter | EF (WebsiteDonHangCombo) | KHÔNG có permission key/SBAuthorize — truy cập không kiểm quyền |

## Quan hệ
- **Gọi sang:** `IWebsiteDonHangComboService.TimTheoDieuKienAsync`.
- **Dữ liệu (entities):** `WebsiteDonHangCombo` (dòng combo thuộc `WebsiteDonHang`).
- **Ripple / đồng bộ:** không (đọc). Đơn cha xem `WebsiteDonHangController`.

## Lỗi đã biết / lưu ý kỹ thuật
- Không có `[SBAuthorize]` → endpoint không kiểm quyền (cân nhắc rủi ro lộ chi tiết combo đơn). `TimTheoDieuKienAsync(gridInfo)` không truyền `CurrentUser` → không scope theo user/đơn vị (cần xác nhận filter có chứa ràng buộc).
- `partial class` — có thể còn endpoint ở file partial khác.
