---
controller: CauHinhController
module: marketing
service: Facebook
man_hinh_ui: "Cấu hình nghiệp vụ Facebook (danh mục CauHinh trong service Facebook). Thao tác CRUD cấu hình, chỉ DV-AD-TONG."
muc_dich: "CRUD danh mục cấu hình nghiệp vụ (CauHinh) của service Facebook: thêm/sửa/xóa/xem chi tiết + tìm theo điều kiện. Controller danh mục chuẩn (scaffold sinh tự động), nghiệp vụ đẩy hết xuống ICauHinhService."
alias: ["cấu hình Facebook", "CauHinh FB", "danh mục cấu hình nghiệp vụ"]
auth: public
state_machine: []
deprecated: false
entities: [CauHinh]
stored_procs: []
depends:
  - "ICauHinhService (Facebook.Services.Generated) — GetByIdAsync/InsertAsync/UpdateAsync/DeleteAsync/TimTheoDieuKienAsync"
  - "AutoMapperConfiguration — map CauHinhDto ↔ CauHinh"
cross_module_in:
  - "UI cấu hình Facebook — CRUD danh mục CauHinh"
cross_module_out: []
---

# CauHinhController — Cấu hình nghiệp vụ Facebook (danh mục)

> Route `api/CauHinh`. Controller danh mục CRUD chuẩn scaffold, mọi action thuộc quyền `DV-AD-TONG`. Gate tiện ích `[TienIch("CauHinh_Facebook_Controller","Quản lý danh mục", TI_Facebook)]`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Xem chi tiết theo id | (mở) | GET `api/CauHinh/TimTheoId/{id:Guid?}` | `CauHinh_Facebook_TimTheoIdAsync` ("Tìm cấu hình theo id" — DV-AD-TONG) | public | — | `_service.GetByIdAsync(id)` → trả CauHinh. | EF CauHinh | — |
| 2 | Thêm mới | Thêm | POST `api/CauHinh/ThemMoi` | `CauHinh_Facebook_ThemMoiAsync` ("Thêm mới cấu hình nghiệp vụ" — DV-AD-TONG) | public | ModelState hợp lệ | ModelState invalid → lỗi `MESS_Model_UnValidate`; hợp lệ → map DTO→CauHinh, `InsertAsync`. | EF CauHinh | — |
| 3 | Cập nhật | Lưu | POST `api/CauHinh/CapNhatThongTin` | `CauHinh_Facebook_CapNhatAsync` ("Lưu cấu hình nghiệp vụ" — DV-AD-TONG) | public | Tồn tại theo Id | Map DTO→CauHinh, `GetByIdAsync(Id)`; null → `MESS_NotFound`; có → `UpdateAsync`. | EF CauHinh | — |
| 4 | Xóa | Xóa | POST `api/CauHinh/Xoa/{id:Guid}` | `CauHinh_Facebook_Xoa` ("Xóa cấu hình nghiệp vụ" — DV-AD-TONG) | public | Tồn tại theo Id | `GetByIdAsync(id)` null → `MESS_NotFound`; có → `DeleteAsync` + trả `MESS_Delete_Success`. | EF CauHinh | — |
| 5 | Tìm theo điều kiện | Tìm kiếm | POST `api/CauHinh/TimTheoDieuKien` | `CauHinh_Facebook_TimTheoDieuKien` ("Tìm kiếm cấu hình nghiệp vụ" — DV-AD-TONG) | public | — | `_service.TimTheoDieuKienAsync(gridInfo)` → danh sách phân trang. | EF CauHinh | — |

## Quan hệ
- **Gọi sang:** `ICauHinhService` (toàn bộ nghiệp vụ); AutoMapper map DTO.
- **Dữ liệu (entities):** `CauHinh`.
- **Ripple / đồng bộ:** không có cross-service trực tiếp ở tầng controller.

## Lỗi đã biết / lưu ý kỹ thuật
- Controller scaffold CRUD chuẩn — không có logic riêng; mọi rule nằm trong `ICauHinhService`.
- 5 action đều khóa quyền `DV-AD-TONG` (chỉ admin tổng).
