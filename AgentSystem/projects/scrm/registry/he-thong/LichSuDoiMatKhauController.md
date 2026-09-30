---
controller: LichSuDoiMatKhauController
module: he-thong
service: LogService
man_hinh_ui: "Lịch sử > Lịch sử đổi mật khẩu (màn tra cứu)"
muc_dich: "Tra cứu lịch sử đổi mật khẩu của user theo bộ lọc."
alias: ["lịch sử đổi mật khẩu", "đổi mật khẩu"]
auth: internal
state_machine: []
deprecated: false
entities: [LichSuDoiMatKhau]
stored_procs: []
depends: [ILichSuDoiMatKhauService]
cross_module_in: ["FE màn lịch sử đổi mật khẩu gọi TimTheoDieuKien"]
cross_module_out: []
---

# LichSuDoiMatKhauController — Lịch sử đổi mật khẩu

TienIch: `LichSuDoiMatKhauController` "Quản lý lịch sử xem thông tin" (TI_Chung). Route `api/LichSuDoiMatKhau`. Chỉ 1 action, có `[SBAuthorize]` → internal.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | Tìm | POST `api/LichSuDoiMatKhau/TimTheoDieuKien` | LichSuDoiMatKhauController_TimTheoDieuKien | internal | — | Lọc lịch sử đổi mật khẩu theo điều kiện + CurrentUser | EF | — |

## Quan hệ
- **Gọi sang:** `ILichSuDoiMatKhauService.TimTheoDieuKienAsync`.
- **Dữ liệu (entities):** `LichSuDoiMatKhau`.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- `LichSuDoiMatKhauController.cs:10` — mô tả TienIch là "Quản lý lịch sử xem thông tin" (copy-paste sai, không khớp đổi mật khẩu).
