---
controller: WebsiteDonHangController
module: website
service: Website
man_hinh_ui: "Quản trị website — màn Quản lý đơn hàng từ website (đơn online): list đơn, xem chi tiết, cập nhật thu ngân/loại thanh toán/chi nhánh, gán id đơn hàng, và nút 'Tạo đơn (chính thức)' từ đơn website"
muc_dich: "Quản lý đơn hàng phát sinh từ website storefront: tìm/lọc, xem chi tiết, cập nhật thông tin, gán IdDonHang, tạo đơn hàng chính thức (TaoDonTuWebsite); cộng endpoint Private nhận đơn từ storefront và lấy đơn tạm."
alias: ["đơn hàng website", "đơn online website", "quản lý đơn website", "tạo đơn từ website", "website order"]
auth: public               # action UI [SBAuthorize()] rỗng (key đã comment); 2 endpoint [PrivateSBAuthorize]
state_machine: []
deprecated: false
entities: [WebsiteDonHang, WebsiteDonHangSanPham, WebsiteDonHangThe, WebsiteDonHangCombo]
stored_procs: []
depends: [Website (IWebsiteDonHangService), Booking, Order/cửa-hàng]
cross_module_in: [Website storefront/checkout (ThemMoiDonHangWebsite, GetAllTempOrders qua PrivateSBAuthorize)]
cross_module_out: [Order/cửa-hàng (TaoDonTuWebsite → tạo đơn hàng chính thức)]
---

# WebsiteDonHangController — Quản lý đơn hàng từ website

> Controller `Website.API/Controllers/Api/WebsiteDonHangController.cs`, route `api/WebsiteDonHang`, base `BaseCoreController`, `[ApiController]`, `partial`. Action UI để `[SBAuthorize()]` rỗng (permission key như `WebsiteDonHang_TimTheoDieuKien` đã comment) → chỉ cần đăng nhập. Có 2 endpoint `[PrivateSBAuthorize]` nhận đơn từ storefront. Inject `IWebsiteDonHangService` 2 lần (`_service` và `_websiteDonHangService` cùng kiểu).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid) | nút Tìm kiếm | POST `api/WebsiteDonHang/TimTheoDieuKien` | `SBAuthorize()` rỗng | public | màn list | Trả list đơn website theo filter + CurrentUser | EF | key `WebsiteDonHang_TimTheoDieuKien` đã comment |
| 2 | Gán id đơn hàng | nội bộ flow | GET `api/WebsiteDonHang/CapNhatIdDonHang` (`CapNhatIdDonHangDto`) | `SBAuthorize()` rỗng | public | — | `CapNhatIdDonHang(rq)` — gắn IdDonHang chính thức vào đơn website | EF | dùng GET nhận DTO (bind từ query) — chú ý |
| 3 | Xem chi tiết đơn | mở chi tiết | GET `api/WebsiteDonHang/TimTheoId/{id:Guid}` | `SBAuthorize()` rỗng | public | per-row | `TimTheoId(id, user)` | EF | - |
| 4 | Cập nhật thu ngân/thanh toán/chi nhánh | popup Sửa | POST `api/WebsiteDonHang/CapNhat` (`WebsiteDonHangCapNhatDto`) | `SBAuthorize()` rỗng | public | popup | `CapNhat(rq)` — cập nhật thu ngân, loại thanh toán, chi nhánh | EF | - |
| 5 | Tạo đơn hàng chính thức | nút "Tạo đơn" | POST `api/WebsiteDonHang/TaoDonTuWebsite/{id:Guid}` | `SBAuthorize()` rỗng | public | đơn website hợp lệ | `TaoDonTuWebsite(id, user)` — chuyển đơn website thành đơn hàng chính thức (ripple sang Order) | EF + Order | thao tác lớn có ripple cross-module |

## Endpoint nội bộ (private / service-to-service — `[PrivateSBAuthorize]`)
- [internal] Storefront/checkout gọi `POST ThemMoiDonHangWebsite` (`ThemMoiDonHangWebsiteDto`) — map DTO sang entity (WebsiteDonHang + list SanPham/The/Combo) rồi `ThemMoiDonHangAsync` → tạo đơn website tạm khi khách đặt online.
- [internal] `POST GetAllTempOrders` (`DonHangTempReqDto`) — lấy danh sách đơn tạm (giỏ/đơn chưa chốt).

## Quy tắc nghiệp vụ
1. Khách đặt hàng trên storefront → `ThemMoiDonHangWebsite` (Private) tạo đơn website tạm gồm: header `WebsiteDonHang` + chi tiết `WebsiteDonHangSanPham` / `WebsiteDonHangThe` / `WebsiteDonHangCombo`.
2. Nhân viên duyệt trên admin → `CapNhat` (thu ngân/thanh toán/chi nhánh) → `TaoDonTuWebsite` chuyển thành đơn hàng chính thức (Order) → `CapNhatIdDonHang` gắn id đơn chính thức ngược lại.

## Quan hệ
- **Gọi sang:** `IWebsiteDonHangService` (TimTheoDieuKienAsync, CapNhatIdDonHang, TimTheoId, CapNhat, TaoDonTuWebsite, ThemMoiDonHangAsync, GetAllTempOrdersAsync); `AutoMapperConfiguration` map DTO↔entity.
- **Dữ liệu (entities):** `WebsiteDonHang` (header) + chi tiết `WebsiteDonHangSanPham`, `WebsiteDonHangThe`, `WebsiteDonHangCombo`.
- **Ripple / đồng bộ:** `TaoDonTuWebsite` → tạo đơn hàng chính thức ở Order/cửa-hàng (đẩy đơn online vào hệ thống bán hàng).

## Luồng xuyên module
- Storefront đặt online → (Private) `ThemMoiDonHangWebsite` tạo đơn tạm → admin `CapNhat` + `TaoDonTuWebsite` → đơn chính thức Order → `CapNhatIdDonHang` đồng bộ id.

## Lỗi đã biết / lưu ý kỹ thuật
- Inject trùng: `_service` và `_websiteDonHangService` cùng `IWebsiteDonHangService` (L17-18) — dư thừa DI, dùng lẫn lộn trong các action.
- Mọi action UI `[SBAuthorize()]` rỗng → ai đăng nhập cũng thao tác được đơn website (key đã comment) — cần xác nhận phân quyền có chủ ý.
- `CapNhatIdDonHang` dùng `[HttpGet]` nhận DTO → bind từ query string; nếu DTO phức tạp dễ lỗi bind.
