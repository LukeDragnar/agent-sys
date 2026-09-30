---
controller: PermisionController
module: he-thong
service: Core
man_hinh_ui: "Đơn vị > Phân quyền chức năng (admin đơn vị): danh sách tiện ích + bảng quyền theo role, lưu phân quyền chức năng cho đơn vị"
muc_dich: "Phân quyền chức năng cấp ĐƠN VỊ (admin đơn vị): lấy danh sách tiện ích, xem & cập nhật quyền theo role của đơn vị. Bản superadmin + thao tác mở rộng nằm ở QuanTriArea (_Core)."
alias: ["phân quyền", "phân quyền chức năng", "quyền role đơn vị", "Permision"]
auth: mixed
state_machine: []
deprecated: false
entities: [Permision, RolePermision]
stored_procs: []
depends: ["IPermisionService", "BaseController", "ServiceSettings", "IRedisCacheService"]
cross_module_in: ["FE phân quyền đơn vị gọi LayDanhSachTienIch/LayDanhSachQuyenTheoRoleDonVi/PhanQuyenChucNangDonVi"]
cross_module_out: ["PhanQuyenChucNangDonVi cập nhật quyền role đơn vị → ảnh hưởng truy cập chức năng của user trong đơn vị"]
---

# PermisionController — Phân quyền chức năng (đơn vị)

Bản `Controllers/`, kế thừa `BaseController`, controller-level `[SBAuthorize]` + `[TienIch(TI_Chung)]`. Chỉ chứa thao tác phân quyền cấp đơn vị (admin đơn vị). Inject Redis nhưng không dùng trực tiếp trong các action ở bản này.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Danh sách tiện ích | (load màn) | GET `api/Permision/LayDanhSachTienIch` | — (login) | public | — | Trả tiện ích: superadmin lấy all, user thường lấy theo đơn vị mua. | EF | — |
| 2 | Xem quyền theo role (đơn vị) | (load bảng quyền) | POST `api/Permision/LayDanhSachQuyenTheoRoleDonVi` | `Permision_LayDanhSachQuyenTheoRoleDonVi` | public | — | `LayDanhSachQuyenTheoRoleDonVi(model)` trả ma trận quyền role của đơn vị. | EF | — |
| 3 | Cập nhật quyền (đơn vị) | Lưu phân quyền | POST `api/Permision/PhanQuyenChucNangDonVi` | `Permision_PhanQuyenChucNangDonVi` | public | — | `UpdateDonViRolePermision(model)` lưu quyền role cho đơn vị. | EF | — |

## Quan hệ
- **Gọi sang:** IPermisionService (EF).
- **Dữ liệu (entities):** `Permision`, `RolePermision`.
- **Ripple / đồng bộ:** Cập nhật quyền role đơn vị → đổi quyền truy cập chức năng của user trong đơn vị.

## Lỗi đã biết / lưu ý kỹ thuật
- Bản này CHỈ có thao tác cấp đơn vị; phân quyền mặc định toàn hệ thống (superadmin), copy quyền, tạo/sửa permission handmade... ở bản QuanTriArea `PermisionController_Core`.
- Inject `IRedisCacheService` nhưng không sử dụng trong các action của bản này.
