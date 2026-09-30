---
controller: SendMessageRabbitController
module: marketing
service: FacebookChat
man_hinh_ui: "ChatHub > khung hội thoại Facebook — nút Gửi tin nhắn / Trả lời bình luận; màn Gửi tin nhắn hàng loạt (chọn tệp khách → soạn nội dung → Gửi); job xử lý hàng đợi gửi"
muc_dich: "Đẩy yêu cầu gửi tin nhắn / trả lời bình luận Facebook qua RabbitMQ (gửi bất đồng bộ qua queue, không gọi Graph trực tiếp tại controller). Hỗ trợ gửi đơn lẻ, gửi hàng loạt (lưu lịch + lên schedule), và job tiêu thụ schedule để bắn từng tin."
alias: ["gửi tin nhắn FB", "trả lời bình luận", "gửi chat hàng loạt", "SendMessageRabbit", "gửi tin qua rabbit", "chat hàng loạt ChatHub"]
auth: public
state_machine: [Status0_ChoXuLy, Status2_DangXuLy, Status1_DaGui]
deprecated: false
entities: [FBFanpage, DataSendChatFB, ScheduleChatFB, Conversation, FBUser, FBTagChat]
stored_procs: []
depends:
  - "ISendMessageRabbitService (FacebookChat.Service.Generated.SendMessageRabbitMQService)"
  - "IPublishEndpointContactReceiveFB — publish RabbitMQ message gửi tin/bình luận sang consumer xử lý Graph"
  - "IRepository<FBFanpage> — kiểm tra fanpage tồn tại theo PageId"
  - "IRepository<DataSendChatFB> / IRepository<ScheduleChatFB> — lưu nội dung gửi hàng loạt + lịch từng khách"
  - "IMongoDbClient (Conversation) — chỉ dùng ở CheckPolixy24h (hiện đã comment)"
cross_module_in:
  - "ChatHub UI — gửi tin/bình luận đơn lẻ, gửi hàng loạt"
  - "Job/cron — gọi ProccessChatFBHL để tiêu thụ schedule"
cross_module_out:
  - "RabbitMQ (queue ContactReceiveFB) → consumer gửi tin lên Facebook Graph API"
  - "EF (DataSendChatFB / ScheduleChatFB) — persist hàng đợi gửi hàng loạt"
---

# SendMessageRabbitController — Gửi tin nhắn / bình luận Facebook qua RabbitMQ (ChatHub)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Gửi tin nhắn (inbox) | Gửi | POST `api/SendMessageRabbit/SendMessage` (form: FBMessageInputDto_V1) | `SendMessageRabbit_FacebookChat_SendMessage` | public | Hội thoại inbox đang mở | Tìm `FBFanpage` theo `PageId` (`long.Parse`); không có → "Không tìm thấy Fanpage". Convert file đính kèm sang base64, gói `FBMessageInputRabbitDto` (PageId/ConversationId/RecipientId/Message/Files + UserLogin) rồi `Publish` lên RabbitMQ. Trả "Thành công" ngay (gửi thật do consumer làm). | EF FBFanpage + RabbitMQ | `UserLogin` lấy từ `CurrentUser` (token) — OK; nhưng `long.Parse(PageId)` không guard → throw nếu PageId rỗng/không phải số (`SendMessageRabbitMQService.cs:66`). Check policy 24h đã comment (`:58-62`) → cho gửi cả khách quá 24h. |
| 2 | Trả lời bình luận | Gửi | POST `api/SendMessageRabbit/SendMessagePost` (form: FBMessageCommentDto_V1) | `SendMessageRabbit_FacebookChat_SendMessagePost` | public | Hội thoại comment đang mở | Tương tự (1) nhưng cho comment: tìm fanpage theo PageId, gói `FBMessageCommentRabbitDto` (kèm `CommentId`) → Publish RabbitMQ. Trả "Thành công". | EF FBFanpage + RabbitMQ | `long.Parse(PageId)` không guard (`:109`). |
| 3 | Gửi tin nhắn / bình luận hàng loạt | Gửi hàng loạt | POST `api/SendMessageRabbit/SendChatFBHL` (IFormCollection) | `SendMessageRabbit_FacebookChat_SendChatFBHL` | public | Màn gửi hàng loạt | Đọc `ConvertDataKhachHangs` (JSON) + Message + Files từ form. Lưu 1 `DataSendChatFB` (nội dung chung) rồi insert N `ScheduleChatFB` (Status=0) cho từng khách. KHÔNG gửi ngay — chỉ xếp hàng. | EF DataSendChatFB + ScheduleChatFB | Không validate `ConvertDataKhachHangs` rỗng/null trước khi loop; file đính kèm convert base64 đồng bộ (block request nếu nhiều/nặng). |
| 4 | Xử lý hàng đợi gửi hàng loạt | (job) | GET `api/SendMessageRabbit/ProccessChatFBHL` | `SendMessageRabbit_FacebookChat_ProccessChatFBHL` | public | Gọi bởi job/cron | Lấy tối đa 100 `ScheduleChatFB` Status=0 (join DataSendChatFB). Rỗng → "HET_TIN_NHAN". Mỗi item: set Status=2 (đang xử lý) → `SendChatFBById` (Publish RabbitMQ) → thành công set Status=1. | EF ScheduleChatFB/DataSendChatFB + RabbitMQ | Update Status=2 rồi nếu Publish lỗi/throng giữa chừng item kẹt Status=2 (không có rollback / retry-back-to-0). Take(100) không sort → thứ tự không xác định. |

## Quy tắc nghiệp vụ (gating)
- **Tiện ích gate:** `[TienIch("SendMessageRabbitController","Gửi tin nhắn qua rabbitmq", MaTienIchSCRM.TI_ChatHub)]`.
- **State machine schedule (`ScheduleChatFB.Status`):** `0` = chờ xử lý → `2` = đang xử lý (đã claim) → `1` = đã gửi. Không có trạng thái lỗi → tin lỗi nằm lại `2`.
- **Policy 24h (đã tắt):** `CheckPolixy24h` so `DateTime.Now - Conversation.LastTimeReceiveMessage` > 1440 phút để chặn gửi khách cũ — **hiện comment**, nên rule không hiệu lực.

## Quan hệ
- **Gọi sang:** RabbitMQ (`IPublishEndpointContactReceiveFB`) → consumer gọi Facebook Graph API gửi tin/bình luận thật.
- **Dữ liệu (entities):** `FBFanpage` (kiểm tra tồn tại), `DataSendChatFB` + `ScheduleChatFB` (hàng đợi gửi hàng loạt).
- **Ripple / đồng bộ:** mỗi message publish → consumer ContactReceiveFB xử lý gửi + ghi message vào Conversation/Mongo (phía consumer).

## Luồng xuyên module
- **Gửi hàng loạt:** UI `SendChatFBHL` (xếp hàng) → cron `ProccessChatFBHL` claim 100 bản ghi → mỗi bản `SendChatFBById` Publish RabbitMQ → consumer gửi Graph + cập nhật hội thoại → Status=1.

## Lỗi đã biết / lưu ý kỹ thuật
- `SendMessageRabbitMQService.cs:66,109` — `long.Parse(PageId)` không try/guard → exception khi PageId không hợp lệ.
- `SendMessageRabbitMQService.cs:58-62` — `CheckPolixy24h` bị comment trong `SendMessage` → bỏ chặn gửi khách >24h.
- `SendMessageRabbitMQService.cs:249-294` — `ProcessSendHangLoat`: claim Status=2 trước khi Publish, không rollback nếu lỗi → item kẹt "đang xử lý"; `Take(100)` thiếu `OrderBy`.
- Convert file → base64 đồng bộ trong request (`ConvertFileToBase64`) → tải nặng có thể chậm/timeout request.
