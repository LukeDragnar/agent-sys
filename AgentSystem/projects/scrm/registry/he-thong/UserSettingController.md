---
controller: UserSettingController
module: he-thong
service: Core
man_hinh_ui: "Hệ thống/Đơn vị > Thiết lập bảo mật user: lưới user thiết lập + nút Thiết lập bảo mật (cấu hình bảo mật/session cho user theo đơn vị)"
muc_dich: "Tra cứu danh sách thiết lập bảo mật của user và thiết lập/cập nhật cấu hình bảo mật (session) cho user theo đơn vị."
alias: ["UserSetting", "thiết lập bảo mật user", "cấu hình bảo mật user", "user session setting"]
auth: internal
state_machine: []
deprecated: false
entities: [UserSetting]
stored_procs: []
depends: ["IUserSettingService", "BaseController"]
cross_module_in: ["FE Thiết lập bảo mật user gọi TimTheoDieuKien (load lưới) + ThietLapBaoMatUser (lưu cấu hình)"]
cross_module_out: ["Thiết lập bảo mật user → ảnh hưởng quy tắc session/đăng nhập của user trong đơn vị"]
---

# UserSettingController — Thiết lập bảo mật user

Controller route `api/UserSetting`. ⚠️ `[TienIch("UserController", ..., TI_Chung)]` — attribute tiện ích gắn **mã "UserController"** (dùng chung gating với màn quản lý nhân viên), KHÔNG phải "UserSettingController". Controller-level `[SBAuthorize]` bị comment; key `UserSetting_*` (DV-AD-TONG, DV-AD-CN) → auth = internal.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | (load lưới) | POST `api/UserSetting/TimTheoDieuKien` | `UserSetting_TimTheoDieuKien` (DV-AD-TONG, DV-AD-CN) | internal | — | Lọc danh sách user thiết lập theo `UserSessionFilterDto`. | EF | — |
| 2 | Thiết lập bảo mật user | Lưu thiết lập | POST `api/UserSetting/ThietLapBaoMatUser` | `UserSetting_ThietLapBaoMatUser` (DV-AD-TONG, DV-AD-CN) | internal | — | `ThietLapBaoMatUser(req)` theo `UserSessionDto` — lưu cấu hình bảo mật/session cho user. | EF | — |

## Quan hệ
- **Gọi sang:** IUserSettingService (EF).
- **Dữ liệu (entities):** `UserSetting` (cấu hình bảo mật/session user).
- **Ripple / đồng bộ:** thiết lập → ảnh hưởng quy tắc session/đăng nhập của user; không bắn event.

## Lỗi đã biết / lưu ý kỹ thuật
- `UserSettingController.cs:11` `[TienIch("UserController", ...)]` gắn mã tiện ích **"UserController"** (không phải UserSetting) → gating tính năng đi theo màn quản lý nhân viên.
- Controller-level `[SBAuthorize]` bị comment (dòng 10) → bảo vệ dựa hoàn toàn vào key từng method.
