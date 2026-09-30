---
controller: SendMessageRabbitController
module: marketing
service: Facebook
man_hinh_ui: "ChatHub — gửi tin nhắn/bình luận FB qua RabbitMQ (nút Gửi tin / Trả lời bình luận trong hội thoại fanpage)."
muc_dich: "Đẩy yêu cầu gửi tin nhắn (SendMessage) và gửi bình luận lên bài post (SendMessagePost) của fanpage vào RabbitMQ để service consumer thực thi gọi Graph API. Controller thin: nhận form DTO_V1, gắn UserLogin, đẩy queue, trả 'success' ngay (fire-and-forget)."
alias: ["gửi tin nhắn FB", "trả lời bình luận FB", "send message rabbit", "gửi tin qua queue"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends:
  - "ISendMessageRabbitService.SendMessage / SendMessagePost (Facebook.Service.Generated) → SendMessageRabbitMQService"
  - "RabbitMQ — publish FBMessageInputDto / FBMessageCommentDto cho consumer xử lý"
cross_module_in:
  - "ChatHub UI — gửi tin nhắn / trả lời bình luận"
cross_module_out:
  - "RabbitMQ → consumer gọi Facebook Graph API gửi message/comment"
---

# SendMessageRabbitController — Gửi tin nhắn/bình luận FB qua RabbitMQ

> Route `api/SendMessageRabbit`. Cả 2 endpoint nhận `[FromForm]` (kèm media), gắn `CurrentUser`, đẩy queue rồi `Ok("success")` ngay — KHÔNG chờ Graph trả về.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Gửi tin nhắn | Gửi | POST `api/SendMessageRabbit/SendMessage` (form `FBMessageInputDto_V1`) | (không key — chỉ role: DV-AD-TONG/CN/MCN, MKT, MKT-LEAD, QL-MKT) | public | Hội thoại đang mở | Bọc DTO_V1 vào `FBMessageInputDto`, gắn `UserLogin=CurrentUser`, `SendMessage` → publish queue. Trả `Ok("success")` ngay. | (queue, không EF) | `[SBAuthorize("","",roles)]` key+desc rỗng → chỉ check role, không có permission key để audit. |
| 2 | Gửi bình luận lên post | Trả lời | POST `api/SendMessageRabbit/SendMessagePost` (form `FBMessageCommentDto_V1`) | (không key — chỉ role: DV-AD-TONG/CN/MCN, MKT, MKT-LEAD, QL-MKT) | public | Có post/comment đích | Bọc DTO_V1 vào `FBMessageCommentDto`, gắn `UserLogin`, `SendMessagePost` → publish queue. Trả `Ok("success")`. | (queue, không EF) | Như #1: key permission rỗng. |

## Quan hệ
- **Gọi sang:** `ISendMessageRabbitService` → publish RabbitMQ.
- **Dữ liệu (entities):** không ghi DB ở tầng controller (consumer xử lý).
- **Ripple / đồng bộ:** consumer nhận queue → gọi Graph gửi message/comment → cập nhật hội thoại/Mongo phía consumer.

## Luồng xuyên module
- Gửi tin: UI → controller → RabbitMQ → consumer Graph send → kết quả gửi không trả ngược trong request (async). Lỗi gửi (token hết hạn, page revoke...) chỉ thấy phía consumer/log, không phản hồi trực tiếp về UI.

## Lỗi đã biết / lưu ý kỹ thuật
- `SendMessageRabbitController.cs:19,30` — `[SBAuthorize("","", roles)]`: permission key + description rỗng, chỉ chặn theo role chuỗi → khó query phân quyền theo key.
- Trả `Ok("success")` ngay sau publish (fire-and-forget) → client luôn thấy thành công kể cả khi consumer gửi Graph thất bại; cần theo dõi qua log consumer.
