---
controller: LogDataWebhookFbController
module: marketing
service: Facebook
man_hinh_ui: "Tra cứu log webhook FB (màn quản trị/hệ thống). Tìm log + gửi lại (resend) data webhook lỗi — CHỈ SuperUser."
muc_dich: "Tra cứu log dữ liệu webhook Facebook (RequestLogs) theo điều kiện và gửi lại (resend) các bản ghi data lỗi vào lại webhook. Dữ liệu thật nằm bên FacebookPrivateHelper (server webhook private); service chặn cứng nếu không phải SuperUser."
alias: ["log webhook FB", "resend webhook", "gửi lại data webhook lỗi", "LogDataWebhookFb"]
auth: public
state_machine: []
deprecated: false
entities: [RequestLogs]
stored_procs: []
depends:
  - "ILogDataWebhookFbService.TimTheoDieuKien / ResendToWebhook (Facebook.Service.Generated)"
  - "FacebookPrivateHelper.LOGWEBHOOK_TimTheoDieuKien / LOGWEBHOOK_ResendToWebhook — gọi server webhook private"
cross_module_in:
  - "UI quản trị — tra cứu + resend log webhook"
cross_module_out:
  - "Server webhook private (FacebookPrivateHelper) — đọc log + bắn lại data lỗi"
---

# LogDataWebhookFbController — Tra cứu & resend log webhook FB

> Route `api/LogDataWebhookFb`. Permission attr bị comment (chỉ `[ApiController]` + `[HttpPost]`, **không `[SBAuthorize]`**) → bảo vệ thực tế nằm ở SERVICE: chặn `IsSuperUser==false`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm log webhook | Tìm kiếm | POST `api/LogDataWebhookFb/TimTheoDieuKienAsync` | (không key — chặn ở service: SuperUser) | public | — | `userLogin==null \|\| IsSuperUser==false` → "Không có quyền truy cập"; ngược lại gọi `FacebookPrivateHelper.LOGWEBHOOK_TimTheoDieuKien` → list `RequestLogs`; null → "Không tìm thấy dữ liệu". | private helper (server webhook) | — |
| 2 | Gửi lại data lỗi | Gửi lại | POST `api/LogDataWebhookFb/ResendToWebhook` | (không key — chặn ở service: SuperUser) | public | Có data lỗi | Check SuperUser; gọi `FacebookPrivateHelper.LOGWEBHOOK_ResendToWebhook(reqData)` → `ResendResult`. | private helper | — |

## Quan hệ
- **Gọi sang:** `ILogDataWebhookFbService` → `FacebookPrivateHelper` (HTTP sang server webhook private), không đụng EF cục bộ.
- **Dữ liệu (entities):** `RequestLogs` (log webhook bên server private).
- **Ripple / đồng bộ:** resend → bắn lại payload webhook lỗi để xử lý lại Contact/hội thoại.

## Lỗi đã biết / lưu ý kỹ thuật
- `LogDataWebhookFbController.cs:31,43` — attribute `[SBAuthorize]` bị comment ở cả 2 action; quyền chỉ dựa vào check `IsSuperUser` trong service (`LogDataWebhookFbService.cs:29,53`). Endpoint vẫn cần đăng nhập (BaseController/CurrentUser) nhưng key permission không khai báo → khó audit phân quyền.
- Cả 2 method service wrap try/catch trả `ex.Message` ra client → có thể lộ chi tiết lỗi nội bộ.
