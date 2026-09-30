---
controller: DMThuongPhatController
module: nhan-su
service: Branch
man_hinh_ui: "Cấu hình > Thưởng phạt > Danh mục thưởng phạt (danh sách + thêm/sửa/xóa)"
muc_dich: "CRUD danh mục thưởng phạt (mã loại thưởng/phạt dùng chung cho đơn vị) — nguồn dữ liệu chọn khi áp thưởng/phạt cho nhân viên."
alias: [DMThuongPhat, "danh mục thưởng phạt", "loại thưởng phạt", "cấu hình thưởng phạt"]
auth: public
state_machine: []
deprecated: false
entities: [DMThuongPhat]
stored_procs: []
depends: [Branch.DataContext]
cross_module_in: [ThuongPhatNhanVien (chọn loại thưởng/phạt khi áp cho NV)]
cross_module_out: []
---

# DMThuongPhat — Danh mục thưởng phạt

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Lấy theo Id | Mở edit | GET `TimTheoId/{id:Guid}` | `DMThuongPhat_TimTheoId` | public | Trả chi tiết 1 danh mục (`GetByIdAsync`). | EF | GET theo route id |
| 2 | Tìm theo điều kiện (grid) | Danh sách/Lọc | POST `TimTheoDieuKien` | `DMThuongPhat_TimTheoDieuKien` | public | Lọc danh mục thưởng phạt theo filter. | EF | — |
| 3 | Thêm mới | Lưu | POST `ThemMoi` | `DMThuongPhat_ThemMoi` (AD-TONG) | public | Insert danh mục mới. Chỉ AD-TONG. | EF | — |
| 4 | Cập nhật | Lưu | POST `CapNhatThongTin` | `DMThuongPhat_CapNhatThongTin` (AD-TONG) | public | Update theo Id. Chỉ AD-TONG. | EF | — |
| 5 | Xóa | Xóa | POST `Xoa/{id:Guid}` | `DMThuongPhat_Xoa` (AD-TONG) | public | Xóa danh mục theo id. Chỉ AD-TONG. | EF | — |
| 6 | Lấy tất cả theo đơn vị | (combo) | GET `LayTatCaThuongTheoDonVi` | `DMThuongPhat_LayTatCaThuongTheoDonVi` | public | Trả toàn bộ danh mục thưởng/phạt của đơn vị hiện tại (theo `CurrentUser`). | EF | — |

## Quan hệ
- **Gọi sang:** không (chỉ EF nội bộ Branch).
- **Dữ liệu (entities):** `DMThuongPhat` (mã loại thưởng/phạt theo đơn vị).
- **Ripple/đồng bộ:** là danh mục nguồn — `ThuongPhatNhanVien` tham chiếu loại khi áp thưởng/phạt cho nhân viên.

## Lỗi đã biết / lưu ý kỹ thuật
- `DMThuongPhatController.cs:23,92` — CRUD mở (AD-TONG để ghi), nhưng các thao tác đọc (`TimTheoId`, `TimTheoDieuKien`, `LayTatCaThuongTheoDonVi`) cho cả KT/QLDV.
- `DMThuongPhatController.cs:93` — comment để lại key cũ `DMThuongPhat_Xoa` ở action `LayTatCaThuongTheoDonVi`, đã thay bằng key đúng dòng 94 (lưu ý khi grep key).
