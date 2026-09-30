---
controller: ChatController
module: he-thong
service: AIChat
man_hinh_ui: "nội bộ / widget — endpoint nhận tin nhắn chat, đẩy vào RabbitMQ cho consumer AI xử lý nền; client lấy reply qua SignalR/poll."
muc_dich: "Cổng nhận tin nhắn chat: validate message, sinh/dùng sessionId, publish ChatSendMessage vào RabbitMQ để consumer AI xử lý bất đồng bộ. Trả sessionId + status 'queued' ngay."
alias: ["Chat AI", "send chat", "nhận tin nhắn ai", "chat send message", "queue chat"]
auth: anonymous
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends:
  - IPublishEndpoint (MassTransit)
  - IPublishEndpointContactReceiveFB
cross_module_in: ["widget / client gọi Send để bot AI trả lời"]
cross_module_out: ["publish ChatSendMessage → consumer AIChat xử lý nền → reply qua SignalR"]
---

# Chat — Cổng nhận tin nhắn AI (publish RabbitMQ)

> Endpoint async: nhận `SendChatRequest`, validate Message không rỗng, sinh `sessionId` (Guid N nếu client không gửi), publish `ChatSendMessage` (IdKhacHang/PageId/SessionId/Message/IdDonVi/IdChiNhanh) vào RabbitMQ. **Không xử lý đồng bộ** — trả ngay `{ SessionId, Status="queued" }`; reply do consumer xử lý rồi đẩy qua SignalR/poll. Controller **không có `[SBAuthorize]`** trên `Send` → công khai.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Gửi tin nhắn chat | (widget gửi message) | `POST api/Chat/Send` | (không có attribute auth) | anonymous | luôn | Message rỗng → lỗi 400 "Message không được rỗng". Hợp lệ → sinh/dùng sessionId, publish `ChatSendMessage`, trả `{SessionId, Status:"queued"}`. | RabbitMQ (publish) | **Không gắn `[SBAuthorize]`** → endpoint mở; xử lý thực nằm ở consumer. |

## Endpoint nội bộ (private / service-to-service)
- `IPublishEndpointContactReceiveFB` được inject (cho nhánh test FB message, hiện comment toàn bộ `TestSendChat` L56-75) — không có endpoint active dùng nó.

## Luồng xuyên module
- Client `POST Send` → publish `ChatSendMessage` → consumer AIChat (đọc AgentProfile/PageAI/TokenAI) sinh reply → trả về client qua SignalR. Controller chỉ là cổng enqueue.

## Quan hệ
- **Gọi sang:** `IPublishEndpoint.Publish(ChatSendMessage)`.
- **Dữ liệu (entities):** không trực tiếp (chỉ message bus).
- **Ripple/đồng bộ:** mọi tin chat đi qua đây vào queue AI.

## Lỗi đã biết / lưu ý kỹ thuật
- `ChatController.cs:27` — `Send` không có `[SBAuthorize]` (controller cũng không có class-level auth) → endpoint công khai; cần validate nguồn ở consumer hoặc thêm gating.
- `IPublishEndpointContactReceiveFB` inject nhưng không dùng (chỉ ở block comment) — dead injection.
