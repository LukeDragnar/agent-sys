---
controller: DMKhachHangController
module: khach-hang
service: Customer
man_hinh_ui: "Customer — danh mục khách hàng (dropdown chọn loại KH/đối tượng trong form KH, đơn hàng); không có màn hình quản trị riêng rõ ràng"
muc_dich: "CRUD danh mục khách hàng (loại/nhóm phân loại KH) + cung cấp dropdown cho các form khác"
alias: ["DM Khách hàng", "Danh mục khách hàng", "DMKhachHang"]
auth: mixed
state_machine: []
deprecated: false
entities: [DMKhachHang]
stored_procs: []
depends: [IDMKhachHangService, BaseController]
cross_module_in: []
cross_module_out: []
---

# DMKhachHangController — Danh mục khách hàng

> Controller danh mục (CRUD gọn) + endpoint dropdown phục vụ form khác. Controller không gắn `[SBAuthorize]` cấp class → mỗi action tự khai báo quyền; có action `[PrivateSBAuthorize]` (private) nên auth = **mixed**.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | Lưới danh mục KH | `POST api/DMKhachHang/TimTheoDieuKien` | DMKhachHangController_TimTheoDieuKien | public | — | `TimTheoDieuKien(gridInfo, CurrentUser)` lọc theo scope đơn vị | EF (service) | — |
| 2 | Dropdown theo kiểu loại | Combo chọn loại KH | `POST api/DMKhachHang/DropdownDMKhachHangByKieuLoai/{kieuloai}` | `[SBAuthorize]` (chỉ cần đăng nhập) | public | — | `DropdownDMKhachHangByKieuLoai(kieuloai, CurrentUser)` | EF | Quyền nới lỏng (chỉ yêu cầu đăng nhập) — khác các action CRUD siết role DV-AD-TONG |
| 3 | Dropdown danh mục KH | Combo chọn KH | `POST api/DMKhachHang/DropdownDMKhachHang` | `[SBAuthorize]` | public | — | `DropdownDMKhachHang(CurrentUser.DonViId)` | EF | như #2 |
| 4 | Thêm mới | Nút "Thêm mới" | `POST api/DMKhachHang/Create` | DMKhachHangController_Create | public | — | ModelState invalid → `MESS_Model_UnValidate`; `Create(model, CurrentUser)` | EF | — |
| 5 | Cập nhật | Nút "Lưu" | `POST api/DMKhachHang/Update` | DMKhachHangController_Update | public | — | ModelState invalid → lỗi; `Update(model, CurrentUser)` | EF | — |
| 6 | Xóa | Nút "Xóa" | `POST api/DMKhachHang/Delete/{id}` | DMKhachHangController_Delete | public | — | `Delete(id, CurrentUser)` | EF | — |
| 7 | Lấy theo Id | Mở chi tiết | `POST api/DMKhachHang/GetById/{id}` | DMKhachHangController_GetById | public | — | `GetById(id, CurrentUser)` | EF | Method GET-by-id nhưng khai báo `[HttpPost]` |

## Endpoint nội bộ (private / service-to-service)
- [internal] `POST api/DMKhachHang/DropdownDMKhachHang_Private/{idDonVi}` (`[PrivateSBAuthorize]`) — dropdown danh mục KH theo đơn vị, gọi service-to-service (không qua token user).

## Quan hệ
- **Gọi sang:** `IDMKhachHangService` (toàn bộ CRUD + dropdown).
- **Dữ liệu (entities):** `DMKhachHang`.
- **Ripple / đồng bộ:** không có cross-service rõ ràng; dropdown được form KH/đơn hàng tiêu thụ.

## Lỗi đã biết / lưu ý kỹ thuật
- `DMKhachHangController.cs:88` (`GetById`) — endpoint đọc theo Id nhưng dùng `[HttpPost]` thay vì `[HttpGet]`, lệch convention REST.
- `DMKhachHangController.cs:33,41` (`DropdownDMKhachHangByKieuLoai`, `DropdownDMKhachHang`) — chỉ `[SBAuthorize]` không kèm key/role, lỏng hơn các action CRUD (DV-AD-TONG).
