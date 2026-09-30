---
controller: PageController (SanboxArea / QuanTriArea)
module: he-thong
service: Core
man_hinh_ui: "Quản trị > Quản lý trang (route api/QuanTriArea/Page): cây trang (tree) + form Thêm/Sửa/Xóa trang, gán role home page"
muc_dich: "CRUD 'Page' (trang/màn web) tổ chức dạng cây: thêm/sửa/xóa trang, gán role được vào home page. Là nguồn dữ liệu phân quyền truy cập trang & menu web. Xóa trang invalidate cache menu."
alias: ["quản lý trang", "page web", "cây trang", "phân quyền trang", "Page"]
auth: public
state_machine: []
deprecated: false
entities: [Page]
stored_procs: []
depends: ["IPageService", "IHybridCache (Redis)", "BaseController", "AutoMapper"]
cross_module_in: ["FE Quản trị/Page gọi LayTatCaTheoTree/TimTheoId/ThemMoi/CapNhatThongTin/Xoa"]
cross_module_out: ["Xóa trang → RemoveWithPatternAsync(Cache_MenuPrefix) xóa cache menu → menu web build lại cho mọi user"]
---

# PageController — Quản lý trang web (Page)

Bản QuanTriArea, kế thừa `BaseController`. CRUD trang + cây trang; Thêm/Cập nhật dùng chung `InsertOrUpdateAsync` (cờ isUpdate). Controller-level `[SBAuthorize]` + `[TienIch(TI_HeThong)]`; từng method gắn key (HT-AD, HT-MB).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy cây trang | (load tree) | GET `api/QuanTriArea/Page/LayTatCaTheoTree?type=` | `Page_LayTatCaTheoTree` (HT-AD, HT-MB) | public | — | `LayTatCaTheoTreeAsync(type)` trả cây trang theo type. | EF | — |
| 2 | Xem chi tiết | (mở form) | GET `api/QuanTriArea/Page/TimTheoId/{id:Guid}` | `Page_TimTheoId` (HT-AD, HT-MB) | public | — | `GetByIdAsync(id)` + `GetRoleHomeByPage` → map sang DTO kèm ListRolesHomePage. | EF | — |
| 3 | Thêm mới | Thêm | POST `api/QuanTriArea/Page/ThemMoi` | `Page_ThemMoi` (HT-AD, HT-MB) | public | — | Valid → `InsertOrUpdateAsync(model)` (insert). | EF | — |
| 4 | Cập nhật | Sửa | POST `api/QuanTriArea/Page/CapNhatThongTin` | `Page_CapNhatThongTin` (HT-AD, HT-MB) | public | — | Valid → `InsertOrUpdateAsync(model, true)` (update). | EF | — |
| 5 | Xóa | Xóa | POST `api/QuanTriArea/Page/Xoa/{id:Guid}` | `Page_Xoa` (HT-AD, HT-MB) | public | — | Kiểm tra tồn tại → `DeleteAsync` → xóa cache menu (RemoveWithPattern `Cache_MenuPrefix`). | EF + Redis | — |

## Quan hệ
- **Gọi sang:** IPageService (EF), IHybridCache (Redis), AutoMapper Page↔PageDto.
- **Dữ liệu (entities):** `Page` (+ role home page mapping).
- **Ripple / đồng bộ:** Xóa trang invalidate cache menu (`Cache_MenuPrefix`) → MenuController build lại menu cho user.

## Lỗi đã biết / lưu ý kỹ thuật
- Chỉ tồn tại ở `Areas/SanboxArea/` (route `api/QuanTriArea/Page`), không có bản `Controllers/`.
- Thêm/Cập nhật chung `InsertOrUpdateAsync`; chỉ Xóa mới xóa cache menu — sửa trang KHÔNG tự invalidate cache menu trong controller (nếu service không tự làm thì menu có thể stale).
