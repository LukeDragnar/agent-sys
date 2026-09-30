---
controller: GroupController
module: sales
service: Switchboard
man_hinh_ui: "Quản trị hệ thống (SanboxArea) > Đăng ký tổng đài cho đơn vị (RegisterGroup, cập nhật, đổi mật khẩu, kích hoạt tổng đài)"
muc_dich: "Đăng ký & quản lý tổng đài (group) cho đơn vị ở khu vực quản trị (SanboxArea, role HT-AD/HT-KT): đăng ký tổng đài, cập nhật, đổi mật khẩu tổng đài, lấy danh sách/tổng đài active của đơn vị. (Có bản trùng tên ở folder Controllers — bản đó rỗng)."
alias: ["đăng ký tổng đài", "group tổng đài", "tổng đài active đơn vị"]
auth: public
state_machine: []
deprecated: false
entities: [Group, TongDaiDonVi]
stored_procs: []
depends: [IGroupService, "Partner tổng đài (external)"]
cross_module_in: []
cross_module_out: ["Partner tổng đài (đăng ký group, đổi mật khẩu)"]
---

# Group (SanboxArea) — Đăng ký tổng đài cho đơn vị

> ⚠️ Có 2 GroupController. Bản ACTIVE này ở `Areas\SandBoxArea\GroupController.cs` (route `api/SanboxArea/Group`, `TI_HeThong`). Bản trong `Controllers\GroupController.cs` (route `api/Group`) toàn bộ comment, rỗng (xem GroupController_Switchboard.md).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Đăng ký tổng đài cho đơn vị | Đăng ký | POST `RegisterGroup` | `GroupController_SanboxArea_RegisterGroup` (HT-AD, HT-KT) | public | đơn vị chưa có tổng đài | Đăng ký group tổng đài cho đơn vị (đẩy partner). | EF + partner | — |
| 2 | Cập nhật tổng đài | Lưu | POST `UpdateGroup` | `GroupController_SanboxArea_UpdateGroup` (HT-AD, HT-KT) | public | group đã đăng ký | Cập nhật thông tin tổng đài đơn vị. | EF + partner | — |
| 3 | Đổi mật khẩu tổng đài | Đổi mật khẩu | POST `ChangePasswordGroup` | `GroupController_SanboxArea_ChangePasswordGroup` (HT-AD, HT-KT) | public | group đã đăng ký | Đổi mật khẩu tổng đài. | partner | — |
| 4 | Tất cả tổng đài của đơn vị | (load) | GET `GetAllTongDaiDonVi/{id}` | `GroupController_SanboxArea_GetAllTongDaiDonVi` (HT-AD, HT-KT) | public | màn quản trị | Trả tất cả tổng đài đơn vị đã đăng ký theo id. | EF | — |
| 5 | Chi tiết / tổng đài active | (load) | GET `GetById/{id}`, GET `GetTongDaiActive` | `_GetById` / `_GetTongDaiActive` (HT-AD, HT-KT) | public | mở chi tiết / load | Trả chi tiết group theo id / tổng đài đang kích hoạt của đơn vị. | EF | — |

## Quan hệ
- **Gọi sang:** `IGroupService` → partner tổng đài (đăng ký/đổi mật khẩu).
- **Dữ liệu (entities):** group tổng đài, tổng đài đơn vị.
- **Ripple / đồng bộ:** đăng ký/đổi mật khẩu đẩy sang partner tổng đài.

## Lỗi đã biết / lưu ý kỹ thuật
- `Areas\SandBoxArea\GroupController.cs:46-72` — `TimTheoDieuKien` và `ChangeActiveTongDai` đã comment.
- Trùng tên với `Controllers\GroupController` (bản đó rỗng) — node bản rỗng đặt hậu tố `_Switchboard`.
