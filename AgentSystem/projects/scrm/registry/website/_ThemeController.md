---
controller: ThemeController                # trùng tên với api/Theme → node này là bản superadmin (Areas/SanboxArea)
module: website
service: Website
man_hinh_ui: "Quản trị hệ thống (SuperAdmin / QuanTriArea) — CRUD Theme website: thêm/sửa/xóa giao diện storefront. Không phải màn của đơn vị thường"
muc_dich: "CRUD Theme website ở khu vực SuperAdmin (QuanTriArea, TI_HeThong): thêm/sửa/xóa/tìm-theo-id một giao diện storefront. Đây là controller QUẢN TRỊ thực sự cho Theme; bản api/Theme chỉ còn đọc/gán."
alias: ["quản lý theme superadmin", "thêm theme website", "sửa theme website", "xóa theme website", "CRUD theme quản trị"]
auth: public               # [SBAuthorize("key", ..., "HT-AD")] — chỉ admin hệ thống
state_machine: []
deprecated: false
entities: [Theme]
stored_procs: []
depends: [Website (IThemeService)]
cross_module_in: []
cross_module_out: [Website/api/Theme (theme tạo ở đây được dropdown api/Theme dùng để gán link website)]
---

# ThemeController (SuperAdmin / QuanTriArea) — CRUD Theme website

> Controller `Website.API/Areas/SanboxArea/ThemeController.cs` (namespace `Website.API.Areas.SandBoxArea`), route `api/QuanTriArea/Theme`, base `BaseCoreController`, `[ApiController]`, `partial`. `[TienIch("ThemeController", "Quản lý Theme", TI_HeThong, true)]` (cờ `true` = superadmin). Permission scope `HT-AD`.
> ⚠️ **Trùng tên class `ThemeController`** với bản `Controllers/Api/ThemeController.cs` (route `api/Theme`, node `ThemeController.md`). Node này = bản quản trị hệ thống (CRUD thật), đặt tên `_ThemeController.md` theo quy ước trùng.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới Theme | nút Thêm (SA) | POST `api/QuanTriArea/Theme/ThemMoi` | `Theme_ThemMoi` | public | popup Thêm (SA) | `ThemMoiAsync(body, user)` | EF (Theme) | scope `HT-AD` |
| 2 | Tìm theo id | mở popup Sửa | GET `api/QuanTriArea/Theme/TimTheoId/{id:Guid}` | `Theme_TimTheoId` | public | per-row | Map `Theme→Dto`; không thấy → lỗi (message copy nhầm "Không tìm thấy nhà cung cấp!") | EF | message lỗi sai (NCC thay vì Theme) |
| 3 | Cập nhật Theme | nút Sửa | POST `api/QuanTriArea/Theme/CapNhatThongTin` | `Theme_CapNhatThongTin` | public | popup Sửa | `CapNhatThongTin(model, user)` | EF | - |
| 4 | Xóa Theme | nút Xóa | POST `api/QuanTriArea/Theme/Xoa/{id:Guid}` | `Theme_Xoa` | public | per-row | Tồn tại → `DeleteAsync(id)`; không thấy → MESS_NotFound | EF | KHÔNG guard cross-tenant (theme là tài nguyên hệ thống — chủ ý SA) |

## Quan hệ
- **Gọi sang:** `IThemeService` (ThemMoiAsync, GetByIdAsync, CapNhatThongTin, DeleteAsync).
- **Dữ liệu (entities):** `Theme` (ThemeCode dựng path view storefront).
- **Ripple / đồng bộ:** theme tạo/sửa ở đây → bản `api/Theme` (`GetThemeToSetLinkWebsite`) dùng để gán cho LinkWebsite → ảnh hưởng giao diện storefront mọi website dùng theme đó.

## Lỗi đã biết / lưu ý kỹ thuật
- `TimTheoId` (L41) trả message "Không tìm thấy nhà cung cấp!" — copy nhầm từ NhaCungCap (cosmetic).
- Đây là controller QUẢN TRỊ Theme thực sự (CRUD); bản `api/Theme` đã comment CRUD, chỉ đọc/gán.
- Không có guard cross-tenant (Theme là tài nguyên dùng chung cấp hệ thống) — đúng ý SuperAdmin.
