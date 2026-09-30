---
controller: MenuController
module: he-thong
service: Core
man_hinh_ui: "nội bộ — hạ tầng menu/phân quyền truy cập trang: FE gọi để dựng menu (LayTatCaMenu) và kiểm tra quyền vào URL (CheckUserAccess); không phải 1 màn quản trị riêng"
muc_dich: "Cung cấp dữ liệu menu cho FE và kiểm tra quyền truy cập 1 URL của người dùng đăng nhập. Mỏng — ủy quyền toàn bộ cho IPageService."
alias: ["menu", "danh mục menu", "phân quyền trang", "page access", "CheckUserAccess"]
auth: public
state_machine: []
deprecated: false
entities: [Page]
stored_procs: []
depends: ["IPageService", "IRedisCacheService (inject nhưng controller không dùng)"]
cross_module_in: ["FE shell gọi LayTatCaMenu khi load app để dựng menu; gọi CheckUserAccess mỗi lần điều hướng để gate route"]
cross_module_out: []
---

# MenuController — Lấy menu & kiểm tra quyền truy cập trang

Controller hạ tầng (namespace `Core.API.Areas.SanboxArea` dù file nằm trong thư mục Controllers; route vẫn `api/Menu`). Controller-level `[SBAuthorize]` → cần login. 2 endpoint: trả toàn bộ menu và kiểm tra user có quyền vào 1 URL. Inject `IRedisCacheService` nhưng **không sử dụng** trong code hiện tại.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy tất cả menu | (dựng menu FE) | GET `api/Menu/LayTatCaMenu` | — (chỉ `[SBAuthorize]` controller-level) | public (login) | — | `LayTatCaMenuAsync()` — trả toàn bộ menu. | EF `Page` | Không lọc theo role ở đây (lọc hiển thị do FE / CheckUserAccess) |
| 2 | Kiểm tra quyền vào URL | (gate route) | POST `api/Menu/CheckUserAccess` | — (chỉ `[SBAuthorize]` controller-level) | public (login) | — | Nếu `url == "/"` → trả true ngay; ngược lại `CheckUserAccess(CurrentUser, model)`. | EF | **Luôn ép `rs.Data = true` sau khi gọi service** → kết quả check thực sự bị ghi đè (xem Lỗi) |

## Quan hệ
- **Gọi sang:** `IPageService` (`LayTatCaMenuAsync`, `CheckUserAccess`).
- **Dữ liệu (entities):** `Page` (menu/trang + quyền truy cập).
- **Ripple / đồng bộ:** không (read-only).

## Lỗi đã biết / lưu ý kỹ thuật
- `MenuController.cs:47-49` `CheckUserAccess` — gọi `_service.CheckUserAccess(...)` rồi **gán cứng `rs.Data = true`** trước khi trả về → kết quả phân quyền thực sự của service bị **vô hiệu hóa**, endpoint luôn báo "có quyền". Cần xác nhận đây là tạm tắt gating hay bug.
- `MenuController.cs:22,27` inject `IRedisCacheService` nhưng không dùng → dead dependency.
- Namespace `Core.API.Areas.SanboxArea` (không phải `Core.API.Controllers`) dù file nằm trong thư mục Controllers — chú ý khi tìm theo namespace.
