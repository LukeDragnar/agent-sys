---
controller: NotificationController
module: he-thong
service: Core
man_hinh_ui: "nội bộ — không có UI riêng (web client đăng ký token push notification khi đăng nhập)"
muc_dich: "Đăng ký token thiết bị (FCM/web push) cho user để nhận thông báo, và đăng ký topic cho toàn bộ user. Lớp tích hợp push notification (SB.Core.Notification)."
alias: ["push notification", "đăng ký token", "thông báo đẩy", "FCM token", "Notification"]
auth: public
state_machine: []
deprecated: false
entities: [UserToken]
stored_procs: []
depends: ["INotificationService", "IUserService", "SB.Core.Notification"]
cross_module_in: ["Web client gọi RegisterToken khi đăng nhập để nhận push", "Admin gọi RegisterAllTopic đăng ký topic cho all user"]
cross_module_out: ["RegisterTopic/RegisterToken đẩy đăng ký lên dịch vụ push (FCM) → user nhận notification cross-module"]
---

# NotificationController — Đăng ký push notification

Kế thừa `BaseController`, controller-level `[SBAuthorize]` (login). Dùng `INotificationService` (push) + `IUserService` (lấy danh sách topic user). Có 1 endpoint `RegisterTopic` bị comment.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Đăng ký token | (auto khi login) | POST `api/Notification/RegisterToken` | — (login) | public | — | `RegisterTokenWebsite(model, CurrentUser)` lưu token web push của user. | EF + push | — |
| 2 | Đăng ký topic cho tất cả user | (admin chạy) | POST `api/Notification/RegisterAllTopic` | — (login) | public | — | Lấy `GetAllUserTopic()` rồi `RegisterTopic` từng user lên dịch vụ push; trả "Thanh Cong". | EF + push | Key `Notification_RegisterAllTopic` bị comment → chỉ cần login, không gate admin; lặp đăng ký toàn user có thể nặng |

## Quan hệ
- **Gọi sang:** INotificationService (push/FCM), IUserService.
- **Dữ liệu (entities):** `UserToken` (token thiết bị).
- **Ripple / đồng bộ:** Đăng ký token/topic lên dịch vụ push → user nhận thông báo từ các module khác.

## Lỗi đã biết / lưu ý kỹ thuật
- `NotificationController.cs:54` `RegisterAllTopic` không gắn permission key (đã comment dòng 55) → bất kỳ user login nào cũng kích hoạt vòng lặp đăng ký topic cho toàn bộ user.
- `RegisterTopic` (dòng 31–40) bị comment — chỉ còn RegisterToken/RegisterAllTopic hoạt động.
