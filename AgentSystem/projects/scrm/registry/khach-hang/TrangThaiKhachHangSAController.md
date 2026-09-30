---
controller: TrangThaiKhachHangController
module: khach-hang
service: Customer
man_hinh_ui: "Customer/quan-ly-trangthai-khachhang-admin (Quản lý trạng thái khách hàng — biến thể admin/SandBox) — route api/SanboxArea/TrangThaiKhachHangController; phân biệt với TrangThaiKhachHangController non-SA"
muc_dich: "CRUD danh mục Trạng thái khách hàng ở cấp ADMIN/SandBox (endpoint suffix _SA, role HT-AD): tìm theo điều kiện, thêm/sửa, cập nhật nhiều bản ghi, xóa"
alias: ["TrangThaiKhachHang_SA", "Trạng thái khách hàng (admin)", "DM Trạng thái KH SandBox", "TrangThaiKhachHangController (SandBoxArea)"]
auth: public
state_machine: []
deprecated: false
entities: [TrangThaiKhachHang]
stored_procs: []
depends: [ITrangThaiKhachHangService, BaseController]
cross_module_in: []
cross_module_out: []
---

# TrangThaiKhachHangController (SandBoxArea) — Quản lý trạng thái khách hàng (biến thể admin _SA)

> **Biến thể SA/admin** trong `Customer.API.Areas.SandBoxArea`, route `api/SanboxArea/TrangThaiKhachHangController`.
> Cùng class name `TrangThaiKhachHangController` với controller non-SA; node này tài liệu hóa bản SandBox (endpoint `*_SA`). Dùng chung service `ITrangThaiKhachHangService` với bản non-SA.
> Attribute `TienIch` ghi tên "TrangThaiKhachHanhController" (typo có "h" dư) nhưng class thật là `TrangThaiKhachHangController`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm trạng thái KH theo điều kiện | Lưới (quan-ly-trangthai-khachhang-admin) | `POST api/SanboxArea/TrangThaiKhachHangController/TimTheoDieuKien_SA` | TrangThaiKhachHang_TimTheoDieuKien_SA (role **HT-AD**) | public | — | `TimTheoDieuKienTemplate(gridInfo)` trả danh sách phân trang | EF (ITrangThaiKhachHangService) | — |
| 2 | Cập nhật nhiều bản ghi | Lưu hàng loạt (inline grid) | `POST .../UpdateMany_SA` | TrangThaiKhachHang_UpdateMany_SA (HT-AD) | public | — | ModelState invalid → `MESS_Model_UnValidate`; `UpdateManyTemplate(gridInfo)` cập nhật list trạng thái | EF | — |
| 3 | Thêm mới trạng thái KH | Nút "Thêm mới" | `POST .../ThemMoi_SA` | TrangThaiKhachHang_ThemMoi_SA (HT-AD) | public | — | `InsertAsyncTemplate(model)` (nhận thẳng entity `TrangThaiKhachHang`) | EF | Nhận trực tiếp entity (không qua DTO) |
| 4 | Cập nhật trạng thái KH | Nút "Lưu" | `POST .../CapNhatThongTin_SA` | TrangThaiKhachHang_CapNhatThongTin_SA (HT-AD) | public | — | `UpdateAsyncTemplate(model)` (nhận thẳng entity) | EF | Nhận trực tiếp entity (không qua DTO) |
| 5 | Xóa trạng thái KH | Nút "Xóa" | `POST .../Xoa_SA/{id:Guid}` | TrangThaiKhachHang_Xoa_SA (role **DV-AD-TONG, DV-QLDV**) | public | — | `GetByIdAsync(id)` → null trả về result (không lỗi rõ ràng); ngược lại `DeleteAsync(id)` rồi trả "Xóa thành công" | EF | Quyền xóa dùng role **DV-AD-TONG/DV-QLDV** (KHÁC HT-AD của các action còn lại); nhánh null trả `ResultWithData(result)` không phải lỗi NotFound tường minh |

## Quan hệ
- **Gọi sang:** `ITrangThaiKhachHangService` (TimTheoDieuKienTemplate, UpdateManyTemplate, InsertAsyncTemplate, UpdateAsyncTemplate, GetByIdAsync, DeleteAsync).
- **Dữ liệu (entities):** `TrangThaiKhachHang` (controller nhận trực tiếp entity ở Insert/Update; filter qua `TrangThaiKhachHangFilterDto`, update-many qua `List<TrangThaiKhachHangDto>`).
- **Ripple / đồng bộ:** Trạng thái KH là danh mục dùng để gán trạng thái cho khách hàng (KH360) — thay đổi danh mục ảnh hưởng lựa chọn trạng thái KH toàn hệ thống.

## Lỗi đã biết / lưu ý kỹ thuật
- `TrangThaiKhachHangController.cs:12` — `TienIch` khai báo tên "TrangThaiKhachHanhController" (thừa chữ "h") ≠ class `TrangThaiKhachHangController`.
- `TrangThaiKhachHangController.cs:62-75` (`DeleteAsync_SA`) — quyền `TrangThaiKhachHang_Xoa_SA` dùng role **DV-AD-TONG, DV-QLDV**, khác **HT-AD** của 4 action còn lại → bất nhất phân quyền trong cùng controller.
- `TrangThaiKhachHangController.cs:67-71` — khi không tìm thấy bản ghi, trả `ResultWithData(result)` (bọc result rỗng) thay vì lỗi NotFound rõ ràng → FE khó phân biệt "không tồn tại" với "thành công".
- `TrangThaiKhachHangController.cs:48,57` — Insert/Update nhận thẳng entity `TrangThaiKhachHang` (không qua DTO), khác các controller SA khác map DTO→entity.
- Trùng class name với bản non-SA; route `api/SanboxArea/[controller]`.
