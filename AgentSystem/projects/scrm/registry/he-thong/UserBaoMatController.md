---
controller: UserBaoMatController
module: he-thong
service: Core
man_hinh_ui: "Bảo mật tài khoản: nút Yêu cầu đổi mật khẩu (admin đơn vị buộc nhân viên đổi mật khẩu)"
muc_dich: "Cho admin đơn vị gửi yêu cầu (bắt buộc) đổi mật khẩu cho tài khoản nhân viên — tăng cường bảo mật."
alias: ["UserBaoMat", "bảo mật tài khoản", "yêu cầu đổi mật khẩu", "buộc đổi mật khẩu"]
auth: internal
state_machine: []
deprecated: false
entities: [User]
stored_procs: []
depends: ["IUserBaoMatService", "BaseController"]
cross_module_in: ["FE Bảo mật tài khoản gọi YeuCauDoiMatKhau (admin đơn vị buộc nhân viên đổi MK)"]
cross_module_out: ["Đặt cờ buộc đổi mật khẩu trên user → lần đăng nhập sau user phải đổi MK"]
---

# UserBaoMatController — Bảo mật tài khoản

Controller nhỏ (route `api/UserBaoMat`), 1 hành động: admin đơn vị gửi yêu cầu buộc nhân viên đổi mật khẩu. `[TienIch(..., TI_Chung)]`, key `UserBaoMat_YeuCauDoiMatKhau` (DV-AD-TONG/CN/MCN) → auth = internal.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Yêu cầu đổi mật khẩu | Yêu cầu đổi MK | POST `api/UserBaoMat/YeuCauDoiMatKhau` | `UserBaoMat_YeuCauDoiMatKhau` (DV-AD-TONG, DV-AD-CN, DV-AD-MCN) | internal | — | `YeuCauDoiMatKhau(req, CurrentUser)` theo `ReqYeuCauDoiMatKhauDto` — đặt cờ buộc đổi MK cho user mục tiêu. | EF | — |

## Quan hệ
- **Gọi sang:** IUserBaoMatService (EF).
- **Dữ liệu (entities):** `User` (cờ buộc đổi mật khẩu).
- **Ripple / đồng bộ:** lần đăng nhập sau, user bị yêu cầu phải đổi mật khẩu.

## Lỗi đã biết / lưu ý kỹ thuật
- Chỉ 1 endpoint; toàn bộ logic (kiểm tra quyền tác động lên user nào) nằm trong IUserBaoMatService.
