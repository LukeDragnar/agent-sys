---
controller: LogRequestFBController
module: marketing
service: FacebookWebhook
man_hinh_ui: "Quản trị/giám sát — tra cứu log request Facebook (admin). Route api/LogRequestFB."
muc_dich: "Tra cứu log request Facebook (TimTheoDieuKien) và gửi lại data lỗi vào webhook (ResendToWebhook) để xử lý lại."
alias: ["LogRequestFB", "log request facebook", "resend webhook", "gửi lại data lỗi FB"]
auth: public
state_machine: []
deprecated: false
entities: [LogRequestFacebook]
stored_procs: []
depends:
  - "ILogRequestFacebookService.TimTheoDieuKienAsync / ResendToWebhook"
cross_module_in: []
cross_module_out:
  - "Gửi lại payload vào luồng webhook (ResendToWebhook) → re-process sự kiện FB lỗi"
---

# LogRequestFBController — Log request Facebook + gửi lại

> Route `api/LogRequestFB`. SBAuthorize đang comment → endpoint không gắn key (rủi ro nếu service public).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm log theo điều kiện | (grid) | POST `api/LogRequestFB/TimTheoDieuKien` | (comment `LogContactReceive_TimTheoDieuKien`, AD-HT) | public | — | Filter log request FB theo `LogContactFilter`. | EF (LogRequestFacebook) | Permission comment (`cs:31`). |
| 2 | Gửi lại data lỗi | Gửi lại | POST `api/LogRequestFB/ResendToWebhook` | (comment `LogContactReceive_ResendToContact`, AD-HT) | public | Có data lỗi | Re-publish/re-process payload FB lỗi qua `ResendToWebhook(ReqResendDataError)`. | EF + forward | Permission comment (`cs:43`). |

## Quan hệ
- **Gọi sang:** `ILogRequestFacebookService`.
- **Dữ liệu (entities):** `LogRequestFacebook` — log request webhook nhận từ FB.
- **Ripple:** `ResendToWebhook` đẩy lại sự kiện lỗi vào luồng xử lý webhook → tạo lại Contact/hội thoại.

## Lỗi đã biết / lưu ý kỹ thuật
- `[SBAuthorize]` comment (`cs:31,43`) → 2 endpoint (gồm Resend ghi dữ liệu) không gắn quyền AD-HT như ý định.
- Trùng tên với bản FbChatWebhook → node suffix `_FbWebhook`.
