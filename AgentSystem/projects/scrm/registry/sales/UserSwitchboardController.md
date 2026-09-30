---
controller: UserSwitchboardController
module: sales
service: Switchboard
man_hinh_ui: "Quản trị hệ thống (SanboxArea) > User tổng đài (đăng ký/cập nhật/xóa user, đổi trạng thái, đổi mật khẩu)"
muc_dich: "Quản lý user tổng đài (user switchboard) ở khu vực quản trị (SanboxArea, role HT-KT/HT-AD): đăng ký/cập nhật/xóa user, đổi trạng thái, đổi mật khẩu — auto-scope đơn vị nếu không phải super/kế-toán. (Có bản trùng tên ở folder Controllers — rỗng)."
alias: ["user tổng đài", "đăng ký user tổng đài", "user switchboard", "đổi mật khẩu user tổng đài"]
auth: public
state_machine: []
deprecated: false
entities: [UserSwitchboard]
stored_procs: []
depends: [IUserSwitchboardService, "Partner tổng đài (external)"]
cross_module_in: []
cross_module_out: ["Partner tổng đài (đăng ký/cập nhật user)"]
---

# UserSwitchboard (SanboxArea) — User tổng đài (quản trị)

> ⚠️ Có 2 UserSwitchboardController. Bản ACTIVE này ở `Areas\SandBoxArea\UserSwitchboardController.cs` (route `api/SanboxArea/UserSwitchboard`, `TI_HeThong`). Bản `Controllers\UserSwitchboardController.cs` (route `api/UserSwitchboard`, MyHub v1) rỗng → node `UserSwitchboardController_Switchboard.md`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Đăng ký user tổng đài | Đăng ký | POST `RegisterUser` | `UserSwitchboardController_SanboxArea_RegisterUser` (HT-KT, HT-AD) | public | màn user tổng đài | Invalid → lỗi; không super/kế-toán → ép `IdDonVi=DonViId`; đăng ký user (đẩy partner). | EF + partner | — |
| 2 | Cập nhật user tổng đài | Lưu | POST `UpdateUser` | `UserSwitchboardController_SanboxArea_UpdateUser` (HT-KT, HT-AD) | public | dòng user | Invalid → lỗi; auto-scope `IdDonVi`; cập nhật user. | EF + partner | — |
| 3 | Xóa user tổng đài | Xóa | GET `DeleteUser/{id}` | `UserSwitchboardController_SanboxArea_DeleteUser` (HT-KT, HT-AD) | public | dòng user | Xóa user; super/kế-toán → xóa toàn hệ thống, ngược lại scope đơn vị `IdDonVi`. | EF + partner | — |
| 4 | Đổi trạng thái user | Bật/tắt | GET `ChangeTrangThai/{id}` | `UserSwitchboardController_SanboxArea_ChangeTrangThai` (HT-KT, HT-AD) | public | dòng user | Đổi trạng thái user (scope đơn vị nếu không super/kế-toán). | EF | — |
| 5 | Đổi mật khẩu user | Đổi mật khẩu | POST `ChangePassword` | `UserSwitchboardController_SanboxArea_ChangePassword` (HT-KT, HT-AD) | public | dòng user | Invalid → lỗi; auto-scope `IdDonVi`; đổi mật khẩu user. | partner | — |

## Quan hệ
- **Gọi sang:** `IUserSwitchboardService` → partner tổng đài (CRUD user, đổi mật khẩu/trạng thái).
- **Dữ liệu (entities):** user tổng đài (user switchboard).
- **Ripple / đồng bộ:** đăng ký/cập nhật user đẩy sang partner tổng đài.

## Lỗi đã biết / lưu ý kỹ thuật
- `Areas\SandBoxArea\UserSwitchboardController.cs:55-67` — `TimTheoDieuKien` đã comment (chuyển sang `MyHubUserSwitchboardController.TimTheoDieuKien`).
- Toàn bộ action auto-scope `IdDonVi` khi `IsSuperUser != true && IsHeThongKeToan() != true` → user thường thao tác trong phạm vi đơn vị mình.
