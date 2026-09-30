---
controller: PhanLoaiNhomController
module: nhan-su
service: CommissionFee
man_hinh_ui: "Hoa hồng CTV > Danh mục > Phân loại nhóm hoa hồng (danh sách + thêm/sửa)"
muc_dich: "CRUD danh mục phân loại nhóm hoa hồng — dùng để gom/áp cấu hình nhóm hoa hồng CTV theo phân loại (NhomHoaHongCTV.Apply)."
alias: [PhanLoaiNhom, "phân loại nhóm", "danh mục phân loại hoa hồng"]
auth: public
state_machine: []
deprecated: false
entities: [PhanLoaiNhom, NhomHoaHongCTV]
stored_procs: []
depends: []
cross_module_in: []
cross_module_out: [NhomHoaHongCTV (Apply theo phân loại)]
---

# PhanLoaiNhom — Quản lý phân loại nhóm hoa hồng

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Thêm mới | Lưu | POST `ThemMoi` | `PhanLoaiNhom_ThemMoi` (AD-TONG/CN/MCN) | public | Validate ModelState + insert phân loại. | EF | — |
| 2 | Cập nhật | Lưu | POST `CapNhat` | `PhanLoaiNhom_CapNhat` | public | Update phân loại. | EF | — |
| 3 | Xóa | Xóa | POST `Xoa/{id}` | `PhanLoaiNhom_Xoa` | public | Xóa phân loại. **Không** check ModelState. | EF | — |
| 4 | Lấy theo Id | Mở edit | POST `LayTheoId/{id}` | `PhanLoaiNhom_LayTheoId` | public | Trả chi tiết phân loại. | EF | — |
| 5 | Tìm theo điều kiện (grid) | Danh sách/Lọc | POST `TimTheoDieuKien` | `PhanLoaiNhom_TimTheoDieuKien` | public | Lọc phân loại. | EF | — |
| 6 | Autocomplete | (combo) | POST `AutoComplete` | `PhanLoaiNhom_AutoComplete` | public | Autocomplete phân loại. | EF | — |

## Quan hệ
- **Gọi sang:** chỉ EF.
- **Dữ liệu (entities):** `PhanLoaiNhom` (danh mục). Được tham chiếu bởi `NhomHoaHongCTV`.
- **Ripple/đồng bộ:** `NhomHoaHongCTV.Apply` dùng phân loại để áp cấu hình hàng loạt cho các nhóm cùng phân loại.

## Lỗi đã biết / lưu ý kỹ thuật
- `PhanLoaiNhomController.cs:39` — `Xoa` không check `ModelState` (các endpoint khác cũng đa phần bỏ check ở controller này, dồn validate xuống service).
- Xóa phân loại đang được nhóm tham chiếu có thể để lại nhóm trỏ phân loại không tồn tại — cần kiểm tra ràng buộc ở service.
