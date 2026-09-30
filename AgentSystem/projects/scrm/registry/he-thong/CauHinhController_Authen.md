---
controller: CauHinhController
module: he-thong
service: Authen
man_hinh_ui: "nội bộ — không có UI (controller đã bị comment toàn bộ)"
muc_dich: "(Đã vô hiệu) lưu/lấy cấu hình đăng nhập của service Authen (CauHinhSettingDto). Toàn bộ controller bị comment, không build vào API."
alias: ["cấu hình đăng nhập", "cấu hình authen", "login config"]
auth: internal
state_machine: []
deprecated: true
entities: []
stored_procs: []
depends: []
cross_module_in: []
cross_module_out: []
---

# CauHinhController (Authen) — Cấu hình đăng nhập (ĐÃ VÔ HIỆU)

Controller này trong service **Authen** đã bị **comment toàn bộ** (`CauHinhController.cs` chỉ còn code trong khối `//`). Không có endpoint nào được đăng ký vào pipeline — không thể gọi. File `_Authen` để tránh trùng node `CauHinhController.md` của module khác.

Theo phần code đã comment, ý định ban đầu:
- `[Route("api/[controller]")]`, dự kiến `[SBAuthorize("CauHinhController")]`.
- `ICauHinhService _service` (impl tại `Authen.Service/Generated/CauHinhService.cs`).
- `POST Save` (`CauHinh_LuuCauHinhDangNhap`, role DV-AD-TONG) — lưu `CauHinhSettingDto` cấu hình đăng nhập.
- `GET Get` (`CauHinh_LayCauHinhDangNhap`) — lấy cấu hình đăng nhập hiện tại.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có) | — | — | — | — | Controller bị comment | Không endpoint nào hoạt động | — | Toàn bộ class nằm trong comment (CauHinhController.cs:1-41) |

## Lỗi đã biết / lưu ý kỹ thuật
- `CauHinhController.cs:1-41` — toàn bộ class bị comment → `deprecated: true`. AI bỏ qua nhanh; nếu cần cấu hình đăng nhập, thực tế dùng `UserSession` (captcha/otp/2FA) qua `UserSessionController`, không qua controller này.
- `ICauHinhService` vẫn tồn tại (`Authen.Service/Generated/CauHinhService.cs`) nhưng không có controller phơi ra ngoài.
