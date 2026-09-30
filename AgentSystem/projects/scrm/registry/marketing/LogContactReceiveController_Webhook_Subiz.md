---
controller: LogContactReceiveController
module: marketing
service: Webhook_Subiz
man_hinh_ui: "nội bộ — màn quản trị hệ thống (AD-HT): tra cứu log Contact nhận từ webhook (LogContact API, TienIch TI_Landingpage)."
muc_dich: "Tra cứu (read-only) log các lần nhận dữ liệu contact từ webhook trong service Webhook_Subiz — phục vụ admin hệ thống debug luồng nhận sự kiện chat/landing."
alias: ["log contact webhook", "tra cứu log nhận contact", "log contact subiz", "lịch sử nhận contact"]
auth: internal
state_machine: []
deprecated: false
entities: [LogContact]
stored_procs: []
depends: [EF WebhookSubizDataContext, ILogContactService]
cross_module_in: [SCRM UI quản trị hệ thống (AD-HT)]
cross_module_out: []
---

# LogContactReceiveController — Tra cứu log Contact (Webhook_Subiz)

> Controller READ-only chỉ còn **1 endpoint hoạt động**: tìm log Contact theo điều kiện (permission `AD-HT` — admin hệ thống). Endpoint `ResendToContact` đã bị **comment toàn bộ** (L42-47) → hiện không có chức năng gửi lại. Node này thuộc service **Webhook_Subiz** (suffix `_Webhook_Subiz` để tránh đụng node cùng tên do agent Webhook_Ladi tạo song song).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm kiếm log Contact | Lưới + lọc | `POST api/LogContactReceive/TimTheoDieuKien` | `LogContactReceive_TimTheoDieuKien` (AD-HT) | internal | màn quản trị hệ thống | Tìm log theo `LogContactFilter` qua `_logContactService.TimTheoDieuKienAsync`. | **EF** `LogContact` | — |
| 2 | ~~Gửi lại data sang Contact~~ | (đã ẩn) | ~~`POST api/LogContactReceive/ResendToContact`~~ | `LogContactReceive_ResendToContact` (AD-HT) | — | comment cứng | Toàn bộ method bị comment (L42-47) → endpoint KHÔNG tồn tại. | — | Tính năng resend đã tắt. |

## Quan hệ
- **Gọi sang:** `ILogContactService.TimTheoDieuKienAsync` (đọc EF LogContact).
- **Dữ liệu (entities):** `LogContact` (log nhận webhook do `Common.WriteLogContact` ghi từ `EventReceiveController`/`EventReceiveService`).
- **Ripple / đồng bộ:** không bắn event; thuần tra cứu.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WebhookSubiz.API/Controllers/LogContactReceiveController.cs:27-33` — constructor inject `ILogContactService` cho CẢ 2 tham số `context` và `logContactService` (param `context` kiểu `ILogContactService` nhưng được gán cho `_logContactService` ở dòng dưới); param `context` thực chất bị bỏ không dùng → DI hơi lộn xộn nhưng không lỗi runtime.
- BE `LogContactReceiveController.cs:42-47` — endpoint `ResendToContact` bị comment toàn bộ → không có cách gửi lại data từ UI; muốn resend phải dùng job nền `PRC_SCRM_Webhook_Chat_DataReceive_Job_Resend_Error` ở service.
- TienIch là `TI_Landingpage` (không phải External Chat) — log dùng chung cho cả luồng landing lẫn chat của service.
