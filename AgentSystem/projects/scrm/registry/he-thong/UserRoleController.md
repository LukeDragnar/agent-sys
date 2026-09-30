---
controller: UserRoleController
module: he-thong
service: Core
man_hinh_ui: "nội bộ — không có UI (toàn bộ controller bị comment, không expose endpoint)"
muc_dich: "(Đã vô hiệu) Dự kiến CRUD/tìm kiếm bảng map User–Role — hiện toàn bộ thân class bị comment, controller rỗng."
alias: ["UserRole", "user role", "map user role", "gán role nhân viên"]
auth: internal
state_machine: []
deprecated: true
entities: [UserRole]
stored_procs: []
depends: ["IUserRoleService", "SBBaseController<UserRole, UserRoleDto>"]
cross_module_in: []
cross_module_out: []
---

# UserRoleController — (Vô hiệu / không expose)

Toàn bộ thân controller trong `UserRoleController.cs` (dòng 19-44) bị comment — kế thừa `SBBaseController<UserRole, UserRoleDto>` + endpoint `TimTheoDieuKien` đều bị vô hiệu. Không expose route nào. AI bỏ qua khi triage: controller **không hoạt động**; việc gán role cho user xử lý ở UserController (Core admin) + RoleController.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có) | — | — | — | — | — | Controller bị comment toàn bộ — không expose endpoint. | — | deprecated: true |

## Quan hệ
- **Ripple / đồng bộ:** map User–Role thực tế xử lý qua `UserController_Core` (ThemMoi/CapNhat) + RoleController; entity `UserRole` vẫn tồn tại trong DB.

## Lỗi đã biết / lưu ý kỹ thuật
- `UserRoleController.cs:19-44` toàn bộ định nghĩa class (SBBaseController<UserRole, UserRoleDto>) + `TimTheoDieuKien` bị comment → không build ra controller chạy được.
