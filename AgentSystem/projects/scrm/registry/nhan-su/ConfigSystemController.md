---
controller: ConfigSystemController
module: nhan-su
service: CommissionFee
man_hinh_ui: "Quản trị > Cấu hình hệ thống (area QuanTriArea — config system của riêng service CommissionFee); tiện ích ẩn (hidden)"
muc_dich: "CRUD bảng ConfigSystem nội bộ của service CommissionFee (cấu hình kỹ thuật theo mã/group), phục vụ quản trị hệ thống — không phải nghiệp vụ hoa hồng."
alias: [ConfigSystem, "config hệ thống CommissionFee", "cấu hình kỹ thuật"]
auth: internal
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends: []
cross_module_in: []
cross_module_out: []
---

# ConfigSystem (CommissionFee) — Quản trị cấu hình hệ thống nội bộ

> Controller hạ tầng (TI_HeThong, tiện ích ẩn) lặp lại ở mọi service SCRM. Không liên quan logic hoa hồng; route `api/QuanTriArea/ConfigSystem`.

## Hành động
| # | Hành động | Endpoint | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn |
|---|---|---|---|---|---|---|
| 1 | Lấy config theo mã | GET `TimTheoMa?ma=` | `ConfigSystem_CommissionFee_TimTheoMa` | internal | Trả 1 config theo mã. | EF |
| 2 | Lấy tất cả | GET `LayTatCa` | `ConfigSystem_CommissionFee_LayTatCa` | internal | Trả toàn bộ config. | EF |
| 3 | Lấy theo group + sort | POST `LayTatCaByGroupSort` | `ConfigSystem_CommissionFee_LayTatCaTheoGroupVaSort` | internal | Trả dạng cây theo group, sort. | EF |
| 4 | Lấy theo group | GET `LayTatCaByGroup` | `ConfigSystem_CommissionFee_LayTatCaTheoGroup` | internal | Trả dạng cây theo group. | EF |
| 5 | Thêm mới | POST `ThemMoi` | `ConfigSystem_CommissionFee_ThemMoi` | internal | Map DTO→entity, set NgayTao/người tạo, Id mới, insert. | EF |
| 6 | Xóa theo Id | POST `Xoa/{id}` | `ConfigSystem_CommissionFee_XoaConfigSystem` (ONLY_SUPERUSER) | internal | Check tồn tại → xóa. | EF |
| 7 | Cập nhật | POST `CapNhatThongTin` | `ConfigSystem_CommissionFee_CapNhatThongTin` | internal | Check tồn tại → set người/ngày cập nhật → update. | EF |
| 8 | Lấy theo Id | GET `TimTheoId/{id}` | `ConfigSystem_CommissionFee_TimTheoId` | internal | Trả config theo Id (map DTO). | EF |
| 9 | Lấy danh sách group | GET `LayCacNhomCauHinh` | `ConfigSystem_CommissionFee_LayCacGroupConfigSystem` | internal | Trả các group code. | EF |

## Quan hệ
- **Dữ liệu (entities):** `ConfigSystem` (bảng config kỹ thuật của service).
- **Ripple:** không có cross-service nghiệp vụ.

## Lỗi đã biết / lưu ý kỹ thuật
- Controller boilerplate quản trị; `Xoa` yêu cầu ONLY_SUPERUSER. Không nằm trong luồng tính hoa hồng — chỉ liệt kê cho đủ inventory service.
