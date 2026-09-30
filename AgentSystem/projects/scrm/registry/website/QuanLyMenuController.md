---
controller: QuanLyMenuController
module: website
service: Website
man_hinh_ui: "Quản trị website — màn Quản lý Menu/Danh mục ngành hàng của website (tree menu); CRUD trang/danh mục hiển thị trên storefront"
muc_dich: "CRUD menu/danh mục ngành hàng (DM_NganhHang) của website: lấy theo tree, theo đơn vị, theo slug, thêm/sửa/xóa — dùng dựng cây menu storefront."
alias: ["quản lý menu website", "danh mục ngành hàng website", "menu tree website", "DM_NganhHang website", "quản lý trang website"]
auth: public               # [SBAuthorize()] rỗng — chỉ cần đăng nhập (permission key đã comment)
state_machine: []
deprecated: false
entities: [DM_NganhHang]
stored_procs: []
depends: [Website (IQuanLyMenuService, ICommonCacheService)]
cross_module_in: []
cross_module_out: []
---

# QuanLyMenuController — Quản lý menu / danh mục ngành hàng website

> Controller `Website.API/Controllers/Api/QuanLyMenuController.cs`, route `api/QuanLyMenu`, base `BaseCoreController`, `[ApiController]`, `partial`. **Mọi action `[SBAuthorize()]` rỗng** (permission key gốc như `Page_LayTatCaTheoTree`, `Page_ThemMoi` đã bị comment) → chỉ cần đăng nhập là gọi được. Thao tác trên entity `DM_NganhHang` (dùng làm menu/danh mục website).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy menu theo tree | load cây menu | GET `api/QuanLyMenu/LayTatCaTheoTree` | `SBAuthorize()` rỗng | public | màn menu | Trả cây menu theo `CurrentUser.DonViId` | EF (DM_NganhHang) | key `Page_LayTatCaTheoTree` đã comment |
| 2 | Lấy node + list con | expand node | GET `api/QuanLyMenu/LayTatCaCoListChildren/{id:Guid}` | `SBAuthorize()` rỗng | public | per-node | Trả node kèm danh sách con theo id | EF | - |
| 3 | Lấy theo đơn vị | load list | GET `api/QuanLyMenu/LayTatCaTheoDonViAsync` | `SBAuthorize()` rỗng | public | màn menu | Trả tất cả menu theo `DonViId` | EF | - |
| 4 | Thêm mới menu | nút Thêm | POST `api/QuanLyMenu/ThemMoi` | `SBAuthorize()` rỗng | public | popup Thêm | Validate ModelState → `InsertOrUpdateAsync(model, user)` (insert) | EF | dùng chung service insert/update; key `Page_ThemMoi` comment |
| 5 | Tìm theo id | mở popup Sửa | GET `api/QuanLyMenu/TimTheoId/{id:Guid}` | `SBAuthorize()` rỗng | public | per-row | Map `DM_NganhHang→Dto`; không thấy → lỗi "Không tìm thấy menu!" | EF | - |
| 6 | Lấy theo slug | resolve slug | GET `api/QuanLyMenu/GetDataBySlug/{slug}` | `SBAuthorize()` rỗng | public | — | Map theo slug; không thấy → "Không tìm thấy menu!" | EF | - |
| 7 | Cập nhật menu | nút Sửa | POST `api/QuanLyMenu/CapNhatThongTin` | `SBAuthorize()` rỗng | public | popup Sửa | Validate → `InsertOrUpdateAsync(model, user, true)` (update flag) | EF | - |
| 8 | Xóa menu | nút Xóa | POST `api/QuanLyMenu/Xoa/{id:Guid}` | `SBAuthorize()` rỗng | public | per-row | `XoaById(id, user)` | EF | KHÔNG có guard cross-tenant rõ ở controller (dựa service) |

## Quan hệ
- **Gọi sang:** `IQuanLyMenuService` (LayTatCaTheoTreeAsync, InsertOrUpdateAsync, GetByIdAsync, GetDataBySlug, XoaById), `ICommonCacheService` (inject nhưng chưa thấy dùng trong các action liệt kê).
- **Dữ liệu (entities):** `DM_NganhHang` (vừa là danh mục ngành hàng, vừa làm menu website — có Slug, ParentId).
- **Ripple / đồng bộ:** menu là nguồn dựng cây ngành hàng storefront (`ProductController.Cat` dùng `LstDmNganhHangDto`).

## Lỗi đã biết / lưu ý kỹ thuật
- Toàn bộ permission key bị comment, để `[SBAuthorize()]` rỗng → ai đăng nhập cũng CRUD được menu; cần xác nhận có chủ ý (rủi ro phân quyền).
- Một số XML-doc/comment `[SBAuthorize("Theme_...")]` còn sót (copy từ ThemeController) — không phản ánh đúng nghiệp vụ menu.
- `ICommonCacheService` inject nhưng không dùng trong các action hiện có → kiểm tra phần partial khác (controller `partial`).
