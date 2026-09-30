---
controller: SanPhamLandingController
module: marketing
service: Webhook_Ladi
man_hinh_ui: "nội bộ — không có UI. Toàn bộ controller bị comment (chưa kích hoạt)."
muc_dich: "(Dự kiến) quản lý sản phẩm LandingPage (CRUD + tìm kiếm). HIỆN TẠI toàn bộ class + action bị comment → controller không tồn tại lúc runtime, không expose endpoint nào."
alias: ["SanPhamLanding Ladi", "sản phẩm landingpage", "SanPhamLanding Webhook Ladi"]
auth: internal
state_machine: []
deprecated: true
entities: [SanPhamLanding]
stored_procs: []
depends:
  - "(dự kiến) ISanPhamLandingService — đang comment, chưa wire"
cross_module_in: []
cross_module_out: []
---

# SanPhamLandingController (Webhook_Ladi) — Sản phẩm LandingPage (TOÀN BỘ COMMENT / DEAD)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có action runtime) | — | (không expose endpoint) | — | — | — | **Toàn bộ class + 5 action dự kiến (ThemMoi/CapNhatThongTin/TimTheoDieuKienAsync/TimTheoId/Xoa) đều bị comment** → controller không được đăng ký, không có route. | — | **DEAD** — file chỉ chứa code comment, chưa kích hoạt. |

## Quan hệ
- **Gọi sang:** (dự kiến) `ISanPhamLandingService` — đang comment.
- **Dữ liệu (entities):** `SanPhamLanding` (khi kích hoạt).
- **Ripple / đồng bộ:** chưa có.

## Lỗi đã biết / lưu ý kỹ thuật
- `SanPhamLandingController.cs:11-70` — **toàn bộ controller bị comment** (class, ctor, 5 action CRUD đều dạng `//`). File chỉ là placeholder/skeleton chưa bật.
- Khi kích hoạt: cần bật `[TienIch(... TI_Landingpage)]`, `[SBAuthorize]`, inject `ISanPhamLandingService` và điền permission key (hiện để rỗng `""`).
- LƯU Ý phân biệt với `sales/SanPhamLandingController.md` (service `Contact`) — node này thuộc `Webhook_Ladi`, trùng tên controller nên hậu tố `_Webhook_Ladi`.
