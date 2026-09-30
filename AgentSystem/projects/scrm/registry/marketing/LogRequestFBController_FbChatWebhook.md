---
controller: LogRequestFBController
module: marketing
service: FacebookChatWebhook
man_hinh_ui: "Quản trị/giám sát — tra cứu log request Facebook chat. Route api/LogRequestFB."
muc_dich: "Tra cứu log request Facebook (TimTheoDieuKien) và gửi lại data lỗi (ResendToWebhook) cho service chat."
alias: ["LogRequestFB chat", "log request facebook chat", "resend webhook chat"]
auth: public
state_machine: []
deprecated: false
entities: [LogRequestFacebook]
stored_procs: []
depends:
  - "ILogRequestFacebookService.TimTheoDieuKienAsync / ResendToWebhook"
cross_module_in: []
cross_module_out:
  - "Gửi lại payload vào luồng webhook (ResendToWebhook) → re-process sự kiện chat lỗi"
---

# LogRequestFBController — Log request Facebook chat + gửi lại

> Route `api/LogRequestFB`. SBAuthorize comment. Mã giống bản FacebookWebhook.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm log theo điều kiện | (grid) | POST `api/LogRequestFB/TimTheoDieuKien` | (comment `LogContactReceive_TimTheoDieuKien`, AD-HT) | public | — | Filter log theo `LogContactFilter`. | EF (LogRequestFacebook) | Permission comment (`cs:31`). |
| 2 | Gửi lại data lỗi | Gửi lại | POST `api/LogRequestFB/ResendToWebhook` | (comment `LogContactReceive_ResendToContact`, AD-HT) | public | Có data lỗi | Re-process payload lỗi qua `ResendToWebhook(ReqResendDataError)`. | EF + forward | Permission comment (`cs:43`). |

## Quan hệ
- **Gọi sang:** `ILogRequestFacebookService`.
- **Dữ liệu (entities):** `LogRequestFacebook`.
- **Ripple:** `ResendToWebhook` đẩy lại sự kiện lỗi vào luồng xử lý.

## Lỗi đã biết / lưu ý kỹ thuật
- `[SBAuthorize]` comment (`cs:31,43`) → endpoint (gồm Resend ghi) không gắn quyền AD-HT.
- Trùng tên với bản FbWebhook → node suffix `_FbChatWebhook`.
