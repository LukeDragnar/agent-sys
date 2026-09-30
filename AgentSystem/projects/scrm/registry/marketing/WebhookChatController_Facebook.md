---
controller: WebhookChatController
module: marketing
service: Facebook
man_hinh_ui: "nội bộ — không có UI (cổng webhook chat FB). Route api/WebhookChat."
muc_dich: "Cổng webhook chat Facebook (xử lý ĐỒNG BỘ): GET verify challenge + POST nhận payload rồi gọi IFBWebhookService.XuLyWebhook1 — duyệt từng Entry, map fanpage→đơn vị/chi nhánh, gọi Graph lấy conversation + thông tin khách rồi đồng bộ tin nhắn vào Mongo. Xử lý ngay trong request, không qua queue."
alias: ["webhook chat FB", "WebhookChat", "api/WebhookChat", "ReceiveWebhook", "đồng bộ tin nhắn FB"]
auth: anonymous
state_machine: []
deprecated: false
entities: [FBDataWebhook, FBFanpage, Conversation]
stored_procs: []
depends:
  - "IFBWebhookService.XuLyWebhook1 — xử lý đồng bộ Entry → conversation/tin nhắn"
  - "IWebhookService (inject, không dùng — XuLyWebhook bị comment)"
  - "FBFanpage repo (TableWithNolock) — map FanPageId → đơn vị/chi nhánh + AccessToken"
  - "MessageHelper.GetConversationBySenderId / GetUserInfo — Graph v21.0"
  - "IMongoDbClient — đọc/ghi Conversation (tin nhắn)"
cross_module_in:
  - "Facebook/Meta platform — GET verify + POST sự kiện chat"
cross_module_out:
  - "Facebook Graph API (v21.0) — lấy conversation + user info"
  - "Mongo (Conversation) — đồng bộ tin nhắn hội thoại"
---

# WebhookChatController — Cổng webhook chat Facebook (xử lý đồng bộ)

> Route `api/WebhookChat`. Nhận payload chat → `XuLyWebhook1` xử lý ngay (gọi Graph + ghi Mongo). Khác `WebhookController` (bắn queue).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Verify webhook (Meta challenge) | (không UI) | GET `api/WebhookChat?hub.mode=&hub.verify_token=&hub.challenge=` | — | (mặc định BaseController) | Facebook gọi khi subscribe | `hub.mode=="subscribe"` và `hub.verify_token == "TokenSandboxVaFacebook"` → `Ok(hub.challenge)`; ngược lại `Unauthorized()`. | (không EF) | **Verify token HARD-CODE** `"TokenSandboxVaFacebook"` (`WebhookChatController.cs:48`). GET không gắn `[AllowAnonymous]` → có thể bị chặn nếu controller yêu cầu đăng nhập. |
| 2 | Nhận + xử lý payload chat | (không UI) | POST `api/WebhookChat` | — | anonymous | Facebook POST sự kiện chat | Đọc raw body → deserialize `FacebookRequestData` → `XuLyWebhook1(webhookData, bodyData)`: với mỗi Entry, query `FBFanpage` theo `FanPageId` lấy đơn vị/chi nhánh + token, gọi Graph lấy conversation/user, đồng bộ tin nhắn vào Mongo. Trả `MethodResult<FBDataWebhook>` (objLog). | Mongo Conversation + Graph; EF FBFanpage | Xem mục lỗi: catch nuốt lỗi → trả null; Graph URL/version hard-code. |

## Quy tắc nghiệp vụ
- `XuLyWebhook1`: với mỗi `entry.Messaging[0]`, `khachHangId` = Sender.Id nếu `entry.Id != Sender.Id` (tin từ khách), ngược lại = Recipient.Id (tin từ page). Tìm Conversation Mongo theo `ParentId==entry.Id && IdKhachHang && IsMessage==true`; có → cập nhật `LastTimeReceiveMessage` + `UnreadCount+1`, đồng bộ tin; chưa có → tạo mới (logic tiếp trong service).
- `objLog.FanPageId = long.Parse(webhookData.Entry[0].Id)`, `NguoiTaoUserName="Webhook"`, `IdDonVi=Guid.Empty`, `IsProcessing=WAIT`.

## Quan hệ
- **Gọi sang:** `IFBWebhookService.XuLyWebhook1` → Graph (MessageHelper) + Mongo.
- **Dữ liệu (entities):** đọc `FBFanpage` (map đơn vị + token), ghi `Conversation` (Mongo); dựng `FBDataWebhook` objLog (không chắc persist trong nhánh này).
- **Ripple / đồng bộ:** đồng bộ tin nhắn hội thoại vào Mongo ngay trong request.

## Lỗi đã biết / lưu ý kỹ thuật
- `WebhookChatController.cs:48` — verify token hard-code `"TokenSandboxVaFacebook"`; bản verify cũ (dòng 33-42) bị comment.
- `WebhookChatController.cs:61-62` — POST `[AllowAnonymous]`, **không validate X-Hub-Signature-256**.
- `WebhookChatController.cs:81-85` — `catch { webhookData=null; } return null;` → mọi exception bị nuốt, FB nhận 204/empty, không log.
- `IWebhookService _serviceData` inject nhưng không dùng (`XuLyWebhook` qua `_serviceData` bị comment dòng 78) → dead dependency.
- `FBWebhookService.cs:79` — `long.Parse(webhookData.Entry[0].Id)` truy cập `Entry[0]` không guard rỗng → NRE nếu payload không có Entry.
- `FBWebhookService.cs:120,123` — Graph URL `"https://graph.facebook.com/"` + version `"v21.0"` hard-code (dòng 123 có khoảng trắng đầu URL `" https://..."`).
