---
controller: LogContactReceiveController
module: marketing
service: Webhook_Ladi
man_hinh_ui: "Quản trị (AD-HT) > Log Contact LandingPage: tra cứu log contact nhận từ LadiPage và gửi lại data sang Contact."
muc_dich: "Tra cứu log contact đã nhận từ LadiPage (TimTheoDieuKien) và cho phép admin resend data landing sang hệ Contact."
alias: ["LogContactReceive Ladi", "log contact landingpage", "tra cứu log contact", "resend landing to contact", "TimTheoDieuKien LogContact"]
auth: internal
state_machine: []
deprecated: false
entities: [LogContact]
stored_procs: []
depends:
  - "ILogContactService (WebhookLadi.Service.Generated) — TimTheoDieuKienAsync"
  - "IDataReceiveService — ResendToContactByUser"
  - "TienIch TI_Landingpage (gate controller)"
cross_module_in:
  - "Admin (AD-HT) — màn quản trị log contact"
cross_module_out:
  - "Hệ Contact SCRM — ResendToContact đẩy lại lead sang Contact"
---

# LogContactReceiveController (Webhook_Ladi) — Quản lý log contact LandingPage

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm log contact theo điều kiện | Tìm kiếm | POST `api/LogContactReceive/TimTheoDieuKien` | `LogContactReceive_TimTheoDieuKien` ("Tìm kiếm log Contact" — AD-HT) | **internal** | màn quản trị | `logContactService.TimTheoDieuKienAsync(gridInfo)` → trả danh sách log contact lọc theo `LogContactFilter`. | EF (qua service) | — |
| 2 | Gửi lại data Landing → Contact | Gửi lại | POST `api/LogContactReceive/ResendToContact` | `LogContactReceive_ResendToContact` ("Gửi lại data Landing To Contact" — AD-HT) | **internal** | màn quản trị | `dataReceiveService.ResendToContactByUser(model)` → đẩy lại lead (`ReqResendDto`) sang hệ Contact. | service | — |

## Quan hệ
- **Gọi sang:** `ILogContactService.TimTheoDieuKienAsync`, `IDataReceiveService.ResendToContactByUser`.
- **Dữ liệu (entities):** `LogContact`.
- **Ripple / đồng bộ:** `ResendToContact` → tạo/ghép contact bên hệ Contact SCRM.

## Bảo mật / lưu ý
- Controller gate `[TienIch(... TI_Landingpage)]` + mỗi action `[SBAuthorize(... "AD-HT")]` → chỉ admin hệ thống dùng được (an toàn, không phải lỗ hổng anonymous như các DataReceive webhook).

## Lỗi đã biết / lưu ý kỹ thuật
- `LogContactReceiveController.cs:28-34` — ctor inject `WebhookLadiDataContext context` nhưng **không gán/không dùng** (dead param).
