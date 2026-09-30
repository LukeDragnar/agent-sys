---
controller: AccountController
module: website
service: Website
man_hinh_ui: "Website bán hàng (storefront public) — trang Đăng nhập (Login.cshtml) và Đăng ký (Register.cshtml) theo theme động (ThemeCode). Logout là POST AJAX (xóa cookie)."
muc_dich: "Xác thực khách hàng trên website bán hàng public: đăng nhập (set cookie token mã hóa theo domain), đăng ký tài khoản khách, đăng xuất (clear cookie)."
alias: ["đăng nhập website", "login khách", "đăng ký tài khoản", "register", "đăng xuất", "logout", "tài khoản khách website"]
auth: anonymous
state_machine: []
deprecated: false
entities: [LoginViewModel, RegisterViewModel, LinkWebsite, CustomerToken]
stored_procs: []
depends: [AuthService (Login/Register/TokenRefresh/GetCurrentUserByToken), LinkWebsiteService, TokenService, Configuration (CookieDomain/CookieWebsitePrefix), AuthenCustomer service]
cross_module_in: []
cross_module_out: [AuthenCustomer (Login/Register/TokenRefresh — xác thực + tạo tài khoản khách), Core (đọc đơn vị theo domain)]
---

# AccountController — Đăng nhập / Đăng ký / Đăng xuất (khách website)

> MVC `Controller` (trả về **View** + một số trả **JSON** cho AJAX). KHÔNG có `[SBAuthorize]` — đây là cổng xác thực storefront công khai. Chỉ action `Index` (GET Login) gắn `[TokenAuthentication]` (soft filter: nếu đã có token còn hạn → redirect về Home). Đơn vị + theme resolve theo **domain** (`LinkWebsiteService.GetDataByDomain`). Token lưu trong cookie mã hóa, tên cookie = prefix (`CookieWebsitePrefix`) + tên, domain = `CookieDomain` (mặc định `.sandbox.com.vn`).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Mở trang đăng nhập | menu "Đăng nhập" | GET `Account/Login` | — | anonymous | đã login→redirect Home | Resolve domain→theme; nếu đã có token → `RedirectToAction("Index","Home")`; ngược lại render `Home/Login.cshtml` | EF (đơn vị theo domain) | `[TokenAuthentication]` chỉ kiểm tra có token, không kiểm hết hạn thực sự ở action |
| 2 | Đăng nhập | nút "Đăng nhập" (form) | POST `Account/Login` (LoginViewModel) | — | anonymous | ModelState valid | `AuthService.Login(username, password, MaDonVi)`; thành công→ set cookie mã hóa TOKEN + REFRESH (1 tháng) + EXP; trả JSON `response` (kể cả khi fail) | EF/AuthenCustomer | `[ValidateAntiForgeryToken]` bị comment (:70); fallback view hardcode theme `T0001` (:73) |
| 3 | Mở trang đăng ký | menu "Đăng ký" | GET `Account/Register` (route mặc định) | — | anonymous | luôn | Resolve domain→theme, render `Home/Register.cshtml` | EF | - |
| 4 | Đăng ký tài khoản | nút "Đăng ký" (form) | POST `Account/Register` (RegisterViewModel) | — | anonymous | ModelState valid + có MaDonVi | Validate đơn vị/MaDonVi; `AuthService.Register(tên, giới tính, ngày sinh, email, username, password, confirm, MaDonVi)`; trả JSON kết quả | EF/AuthenCustomer | `[ValidateAntiForgeryToken]` bị comment (:129); không tự đăng nhập sau đăng ký |
| 5 | Đăng xuất | nút "Đăng xuất" (AJAX) | POST `logout` | — | anonymous | đang login | Clear cookie ERROR/EXP/REFRESH/TOKEN/UID (theo domain), trả JSON `{success:true}` | cookie | `virtual` (có thể override); không gọi server LogOut (đã comment :172-173) |

## Quan hệ
- **Gọi sang:** `IAuthService` (Login/Register/TokenRefresh/GetCurrentUserByToken) → service **AuthenCustomer**; `ILinkWebsiteService.GetDataByDomain` (đơn vị + theme theo domain); `ITokenService` (đọc token hiện tại).
- **Dữ liệu (entities):** `LoginViewModel`, `RegisterViewModel` (input); LinkWebsite/Donvi (resolve domain); token khách lưu cookie (không entity DB tại controller).
- **Ripple / đồng bộ:** Register → tạo tài khoản khách bên AuthenCustomer (cross-service). Login/Logout chỉ thao tác cookie + xác thực, không ghi DB website.

## Lỗi đã biết / lưu ý kỹ thuật
- **CSRF**: `[ValidateAntiForgeryToken]` bị comment ở cả Login (:70) và Register (:129) → POST không chống giả mạo.
- **Theme fallback hardcode `T0001`** ở Login/Register (`:73`, `:132`) trước khi resolve domain — nếu resolve fail vẫn dùng T0001.
- **`IsCaptchaValid` dùng `secretKey = "YOUR_SECRET_KEY"`** (`:201`) — placeholder, captcha chưa cấu hình thật; hàm không được gọi ở luồng Login/Register hiện tại.
- **Login luôn trả `Json(response)` kể cả khi fail** (:93) → client phải đọc `response.success`; không redirect server-side.
- **Cookie token mã hóa, HttpOnly, Secure, SameSite=Lax**, domain `.sandbox.com.vn`; tên = prefix `dev_website` + key. Logout phải xóa đúng tên prefix mới sạch.
- **`GetDomain()`** (private, :244) có logic phức tạp đổi `fullbooking.vn`↔`sandbox.com.vn` theo môi trường và localhost-debug — nhưng hiện KHÔNG được gọi trong Login/Register (dùng `Request.Host.Value` trực tiếp). Dead-ish nhưng giữ lại.
