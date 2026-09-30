---
controller: CaptChaController
module: he-thong
service: Authen
man_hinh_ui: "Màn đăng nhập (web): ảnh captcha hiển thị khi tài khoản/đơn vị bật captcha hoặc khi đăng nhập sai quá ngưỡng."
muc_dich: "Sinh ảnh captcha (4 ký tự) và xác thực mã captcha người dùng nhập, gắn theo deviceid để chống brute-force login."
alias: ["captcha", "mã xác nhận đăng nhập", "ảnh captcha", "validate captcha"]
auth: anonymous
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: ["IImageCaptchaService (Authen.Service/Captcha — sinh ảnh + lưu code theo deviceid trong Session/storage provider)"]
cross_module_in: ["Màn login web → GetCaptcha (lấy ảnh), AuthenService.LoginV2/KiemTraOtp → ValidateCode (xác thực khi gửi form login)"]
cross_module_out: []
---

# CaptChaController — Sinh & xác thực ảnh captcha đăng nhập

Controller phụ trợ cho luồng login: trả về ảnh JPEG captcha 4 ký tự và endpoint validate. Logic nằm trong `IImageCaptchaService` (`Authen.Service/Captcha/ImageCaptchaService.cs`). Captcha gắn theo `deviceid` — khi login (`AuthenService.LoginV2`/`KiemTraOtp`) sẽ gọi `_imageCaptchaService.ValidateCode(model.captcha, deviceId)` với deviceId lấy từ header `Deviceid`. Controller anonymous (không gắn auth).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy ảnh captcha | (ảnh captcha trên form) | GET `api/CaptCha/GetCaptcha?deviceid=` | — | anonymous | Khi cần nhập captcha | `_imageCaptchaService.GenerateCode(4, deviceid)` sinh ảnh 4 ký tự, lưu code theo deviceid, trả `File(..., "image/jpeg")`. | — (in-memory/session) | — |
| 2 | Xác thực captcha | (ngầm khi submit) | GET `api/CaptCha/ValidateCaptcha?code=&deviceid=` | — | anonymous | — | `_imageCaptchaService.ValidateCode(code)` trả bool đúng/sai. | — | **Bug nhỏ:** overload gọi `ValidateCode(code)` KHÔNG truyền `deviceid` (AuthenService thì truyền) → validate ở đây không gắn thiết bị, có thể lệch với code lưu theo deviceid |

## Quan hệ
- **Gọi sang:** `IImageCaptchaService` (sinh/lưu/đối chiếu mã captcha).
- **Dữ liệu (entities):** không có entity DB; code captcha lưu in-memory/session theo deviceid.
- **Ripple / đồng bộ:** kết quả validate được tái sử dụng trong `AuthenService.LoginV2`/`KiemTraOtp`.

## Lỗi đã biết / lưu ý kỹ thuật
- `CaptChaController.cs:36` — `ValidateCaptcha` gọi `ValidateCode(code)` (overload không truyền `deviceid`), trong khi login dùng `ValidateCode(code, deviceId)` → hai đường validate có thể không nhất quán theo thiết bị.
- `CaptChaController.cs:1-11` — import thừa (EFCore, Redis, WebSockets, Mapper) không dùng trong controller.
