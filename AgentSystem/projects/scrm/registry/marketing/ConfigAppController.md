---
controller: ConfigAppController
module: marketing
service: TiktokWebhook
man_hinh_ui: "trang HTML public — Chính sách quyền riêng tư & Điều khoản dịch vụ (TikTok yêu cầu khi duyệt app). Người dùng/TikTok mở trực tiếp qua URL."
muc_dich: "Phục vụ 2 trang pháp lý (Privacy Policy / Terms of Service) cho app TikTok bằng cách proxy nội dung HTML từ docs.sandbox.com.vn."
alias: ["ConfigApp", "privacy policy Tiktok", "terms of service", "chính sách quyền riêng tư", "điều khoản dịch vụ"]
auth: anonymous
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends:
  - "External: https://docs.sandbox.com.vn (proxy HTML qua HttpClient)"
cross_module_in:
  - "TikTok app review / người dùng cuối — mở URL trang pháp lý"
cross_module_out: []
---

# ConfigAppController — Trang pháp lý app TikTok (Privacy / Terms)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Trang Chính sách quyền riêng tư | (mở URL) | GET `api/ConfigApp/PrivacyPolicy` | — | **anonymous** (không attribute auth) | luôn mở | `HttpClient` GET `docs.sandbox.com.vn/chinh-sach-quyen-rieng-tu` → trả `Content(html, "text/html")`. Lỗi status → trả status + "Lỗi khi tải nội dung"; exception → trả text "Lỗi: ...". | External HTTP (proxy) | Proxy đồng bộ, không cache; lệ thuộc site ngoài. |
| 2 | Trang Điều khoản dịch vụ | (mở URL) | GET `api/ConfigApp/TermsOfService` | — | **anonymous** | luôn mở | `HttpClient` GET `docs.sandbox.com.vn/dieu-khoan-...vietqr...` → trả HTML. Xử lý lỗi như trên. | External HTTP (proxy) | Như trên. |

## Quan hệ
- **Gọi sang:** site tĩnh `docs.sandbox.com.vn` qua `HttpClient` mới tạo mỗi request (`using var httpClient`).
- **Dữ liệu (entities):** không có.
- **Ripple / đồng bộ:** không có.

## Bảo mật / lưu ý
- Anonymous, chỉ phục vụ nội dung pháp lý công khai → rủi ro thấp. Nhưng `catch` trả thẳng `ex.Message` ra response (rò thông tin nội bộ) và tạo `HttpClient` mới mỗi request (anti-pattern, nên dùng `IHttpClientFactory`).

## Lỗi đã biết / lưu ý kỹ thuật
- `ConfigAppController.cs:21,44` — `new HttpClient()` mỗi request (socket exhaustion tiềm ẩn) thay vì `IHttpClientFactory`.
- `ConfigAppController.cs:36,59` — `catch` trả `$"Lỗi: {ex.Message}"` lộ chi tiết lỗi.
- URL nguồn hardcode trong code.
