---
controller: DMDonHangController
module: cua-hang
service: Order
man_hinh_ui: "Quản trị hệ thống (SandBox / superadmin) — danh mục đơn hàng (master data dùng chung). FE có màn `pages/Order/dm-donhang/` (list dm-donhang.component + modal dm-donhang-edit.component) trỏ vào route này — vị trí menu chính xác chưa xác nhận"
muc_dich: "CRUD danh mục đơn hàng (DMDonHang) ở mức superadmin/hệ thống — master data dùng chung; quản lý qua area SandBox, clear cache hệ thống mỗi lần ghi."
alias: ["quản lý danh mục đơn hàng", "danh mục đơn hàng superadmin", "DMDonHang quản trị", "master data đơn hàng"]
auth: internal
state_machine: []
deprecated: false
entities: [DMDonHang]
stored_procs: []
depends: [Order, Cache (CommonCacheService)]
cross_module_in: [FE pages/Order/dm-donhang — DMDonHangService gọi TimTheoDieuKien/ThemMoi/CapNhat/Xoa]
cross_module_out: [Cache DMDonHang hệ thống (ClearCache_DMDonHang_HT) — clear khi Xoa/ThemMoi]
---

# DMDonHangController (QuanTri / SandBox) — Quản lý danh mục đơn hàng (superadmin)

> Controller `Order.API/Areas/SandBoxArea/DMDonHangController.cs`, route **`api/SanboxArea/DMDonHang`** (lưu ý chính tả "Sanbox" thiếu 'd' — copy verbatim từ code). `[TienIch("DMDonHangController", ..., MaTienIchSCRM.TI_Chung, true)]` → cờ `true` = **superadmin/hệ thống**. Master data danh mục đơn hàng dùng chung. Controller-level `[SBAuthorize("Quản lý danh mục đơn hàng")]` đã comment; auth thực thi qua từng method + cờ superadmin của TienIch.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (list) | "Tìm kiếm" / load grid | POST `api/SanboxArea/DMDonHang/TimTheoDieuKien` | `DMDonHang_TimTheoDieuKien` | internal | màn list | `_dmDonHangService.TimTheoDieuKien(gridInfo)` (filter `DMDonHangFilterDto`); trả grid | EF | - |
| 2 | Xem chi tiết / mở sửa | (mở modal edit) | GET `api/SanboxArea/DMDonHang/TimTheoId/{id:Guid}` | — (`[SBAuthorize()]` rỗng → login-only) | internal | - | `GetByIdAsync(id)` → map `DMDonHangDto` | EF | - |
| 3 | Thêm mới | "Thêm mới" (modal) | POST `api/SanboxArea/DMDonHang/ThemMoi` | `DMDonHang_ThemMoi` | internal | nút Thêm | `Create(payload)` | EF | **KHÔNG clear cache sau ThemMoi** trong controller (chỉ Xoa mới clear) — bản ghi mới có thể chưa phản ánh tới cache HT cho tới khi có thao tác clear khác. (Lưu ý: prompt nói ThemMoi clear cache — code thực tế controller KHÔNG gọi `ClearCache_DMDonHang_HT()` ở ThemMoi; chỉ ở Xoa) |
| 4 | Cập nhật | "Sửa" (modal) | POST `api/SanboxArea/DMDonHang/CapNhat` | `DMDonHang_CapNhat` | internal | nút Sửa | `Update(payload)` | EF | Không clear cache sau CapNhat (controller) |
| 5 | Xóa | "Xóa" (confirm) | **GET** `api/SanboxArea/DMDonHang/Xoa/{id:Guid}` | `DMDonHang_Xoa` | internal | nút Xóa | `DeleteAsync(id)` rồi `ClearCache_DMDonHang_HT()` | EF | **Xóa dùng `[HttpGet]`** (không POST) — thao tác ghi qua GET, lệch convention REST; rủi ro bị trigger qua link/crawler (Areas/SandBoxArea/DMDonHangController.cs:43) |

## Quan hệ
- **Gọi sang:** `IDMDonHangService` (TimTheoDieuKien / GetByIdAsync / Create / Update / DeleteAsync), `ICommonCacheService.ClearCache_DMDonHang_HT`.
- **Dữ liệu (entities):** `DMDonHang` (master data dùng chung toàn hệ thống).
- **Ripple / đồng bộ:** Xoa → clear cache DMDonHang hệ thống (`ClearCache_DMDonHang_HT`) → dropdown DMDonHang (kể cả endpoint public `api/DMDonHang/DropDownList`) lấy lại dữ liệu mới.

## Lỗi đã biết / lưu ý kỹ thuật
- **Route misspelling "Sanbox"** — `Areas/SandBoxArea/DMDonHangController.cs:14` `[Route("api/SanboxArea/[controller]")]` (thiếu 'd'); FE `dmdonhang.service.ts` cũng dùng đúng chuỗi sai này → giữ nguyên, đừng "sửa" route lẻ một bên.
- **`Xoa` qua HttpGet** (dòng 43) — thao tác xóa bằng GET; chỉ clear cache ở Xoa, không ở Thêm/Sửa.
- Trùng tên class `DMDonHangController` với controller public ở `Controllers/DMDonHangController.cs` (route `api/DMDonHang`) — đây là bản **CRUD superadmin**, bản kia chỉ là dropdown public.
- FE: `services/order/dmdonhang.service.ts` gọi CRUD vào area này; màn `pages/Order/dm-donhang/`.
