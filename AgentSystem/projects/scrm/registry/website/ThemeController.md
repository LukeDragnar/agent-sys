---
controller: ThemeController
module: website
service: Website
man_hinh_ui: "Quản trị website — chọn/gán Theme cho website (route gán LinkWebsite): tìm theme theo điều kiện + lấy theme để gán link website"
muc_dich: "Đọc/tìm Theme website ở khu vực quản trị đơn vị (TI_Website): tìm theo điều kiện + lấy danh sách theme để gán cho link website. CRUD (thêm/sửa/xóa) đã bị comment ở controller này — nằm ở bản superadmin (_ThemeController)."
alias: ["chọn theme website", "tìm theme website", "gán theme cho website", "theme to set link website"]
auth: public               # [SBAuthorize("key", ...)]; scope HT-AD + nhiều role DV
state_machine: []
deprecated: false
entities: [Theme]
stored_procs: []
depends: [Website (IThemeService)]
cross_module_in: []
cross_module_out: [Website/LinkWebsite (theme được gán cho link website)]
---

# ThemeController — Tìm/gán Theme website (khu vực đơn vị)

> Controller `Website.API/Controllers/Api/ThemeController.cs`, route `api/Theme`, base `BaseCoreController`, `[ApiController]`, `partial`. `[TienIch("ThemeController", "Quản lý Theme", TI_Website)]`. **Chỉ còn 2 action đọc**; phần ThemMoi/TimTheoId/CapNhat/Xoa **đã comment** (CRUD chuyển sang bản superadmin — xem `_ThemeController.md`).
> ⚠️ Trùng tên với controller superadmin `Areas/SanboxArea/ThemeController.cs` → node đó để ở `_ThemeController.md`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theme theo điều kiện | grid theme | POST `api/Theme/TimTheoDieuKien` | `Theme_TimTheoDieuKien` | public | màn theme | Trả list theme theo filter + CurrentUser | EF (Theme) | scope `HT-AD` |
| 2 | Lấy theme để gán link website | dropdown gán theme | POST `api/Theme/GetThemeToSetLinkWebsite` | `Theme_GetThemeToSetLinkWebsite` | public | màn gán LinkWebsite | Trả theme khả dụng để gán vào link website | EF | scope rộng (HT-AD + DV-AD-TONG/CN/MCN/KT/QLDV/NV) |

## Endpoint (đã comment — KHÔNG còn dùng ở controller này)
<!-- ThemMoi / TimTheoId / CapNhatThongTin / Xoa (L47-79) đã comment block; CRUD Theme thực hiện ở bản superadmin Areas/SanboxArea (_ThemeController.md), route api/QuanTriArea/Theme. -->

## Quan hệ
- **Gọi sang:** `IThemeService` (TimTheoDieuKienAsync, GetThemeToSetLinkWebsite).
- **Dữ liệu (entities):** `Theme` (giao diện storefront — `ThemeCode` dùng dựng path view `~/Views/Themes/{ThemeCode}/...`).
- **Ripple / đồng bộ:** theme được gán cho LinkWebsite → quyết định view render storefront.

## Lỗi đã biết / lưu ý kỹ thuật
- Block CRUD (L47-79) đã comment ở bản này — sửa/thêm Theme đi qua bản superadmin (`_ThemeController.md`, route `api/QuanTriArea/Theme`).
- `ThemeCode` của Theme là khóa dựng đường dẫn view storefront — đổi code phải đảm bảo folder `Views/Themes/{code}` tồn tại.
