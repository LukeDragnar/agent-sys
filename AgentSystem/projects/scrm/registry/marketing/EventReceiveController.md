---
controller: EventReceiveController
module: marketing
service: Webhook_Subiz
man_hinh_ui: "nội bộ — không có UI. Endpoint webhook công khai để Subiz (live-chat) POST sự kiện chat về SCRM."
muc_dich: "Nhận webhook sự kiện chat từ Subiz (contact_info_updated / message_sent) qua mọi content-type (json, query-string, form-data, x-www-form-urlencoded) → xác thực account_id + chữ ký HMAC → tạo EventReceive → đẩy sang service Contact tạo contact/hội thoại."
alias: ["webhook subiz", "nhận sự kiện subiz", "receive event subiz", "subiz chat webhook", "ingest chat subiz"]
auth: anonymous
state_machine: []
deprecated: false
entities: [EventReceive, ConfigChat (SubizConfig), AgentUser, LogContact]
stored_procs: [PRC_SCRM_Webhook_Chat_DataReceive_Job_Clear, PRC_SCRM_Webhook_Chat_DataReceive_Job_Resend_Error]
depends: [Contact service (/api/DataReceive/ReceiveContactFromWebhook), Redis (idempotent), MassTransit (publish), EF WebhookSubizDataContext]
cross_module_in: [Subiz (đối tác live-chat ngoài — POST webhook)]
cross_module_out: [Contact (tạo contact/hội thoại từ EventReceive), LogContact (ghi log nhận)]
---

# EventReceiveController — Nhận webhook sự kiện chat Subiz

> **Endpoint webhook PUBLIC, `[AllowAnonymous]` ở controller.** Subiz POST sự kiện chat về đây. Controller chỉ làm nhiệm vụ ĐỌC body theo content-type (json / query-string GET / multipart/form-data / x-www-form-urlencoded), gom thành `JObject`, rồi đẩy xuống `EventReceiveService.SendDataToLogAndWebhook(jObject, signatures, rawBody)`. **Xác thực thật (account_id + HMAC chữ ký) nằm ở service** (`ReceiveDataAsync`), KHÔNG ở controller. Mọi nhánh đều trả `Ok(...)` (HTTP 200) kể cả khi lỗi — chủ đích để Subiz không retry webhook.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Nhận webhook sự kiện chat Subiz | (không UI — Subiz gọi) | `POST api/EventReceive/ReceiveEventSubiz` | (none) | anonymous | webhook ngoài | Đọc body theo `Request.ContentType`: null→query-string GET; `application/json`→đọc raw; `multipart/form-data`→đọc form/section; `x-www-form-urlencoded`→parse `key=value&...`. Gom thành `JObject` + lấy header `X-Hub-Signature-256` → gọi `SendDataToLogAndWebhook`. Service validate `account_id` khớp `ConfigChat`, tính HMAC `account.Password`, so chữ ký → mới tạo `EventReceive` + đẩy sang Contact. Mọi lỗi vẫn trả `Ok(...)` (200). | **EF** `EventReceive`, `ConfigChat`, `AgentUser` (qua service) | **Anonymous data-ingest** (xem Lỗi đã biết). Chỉ nhánh json truyền `rawBody` cho service → các content-type khác ký HMAC trên chuỗi rỗng. |

## Endpoint nội bộ (private / service-to-service)
- Không có endpoint HTTP private. Ripple đi qua service:
  - [out] `EventReceiveService.SendToContact` → HTTP POST `Contact /api/DataReceive/ReceiveContactFromWebhook` (tạo contact/hội thoại bên service Contact).
  - [out] `EventReceiveService.SendToWebhook` (chỉ khi có `Phone`) — đẩy tiếp data đã chuẩn hóa.
  - Job nền: `PRC_SCRM_Webhook_Chat_DataReceive_Job_Clear`, `..._Resend_Error` (dọn + gửi lại bản lỗi).

## Quy tắc nghiệp vụ (gating / xác thực)
1. `type` phải ∈ {`contact_info_updated`, `message_sent`}; `data`/`event_id`/`type` null → trả `Lỗi xác thực` (DataReceiveService.cs:291).
2. Tìm `ConfigChat` theo `account_id` + (domain khớp `DomainWebsite` HOẶC `DomainWebsite` rỗng HOẶC có `agent_id`). Không thấy → `Lỗi xác thực` (L340-342).
3. `account.IdLandingDefault == null` → "Tài khoản chưa được gán landing" (L343-344).
4. `contact_info_updated`: khóa idempotent Redis theo (`account_id`,`profile_id`) TTL 10s → trùng thì bỏ (L353-363) — chống dup do Subiz bắn 2 webhook cùng lúc.
5. Tính `signatureHMAC = CreateSignature(account.Password, rawSringObj)`; chỉ tạo `EventReceive` khi 1 header `X-Hub-Signature-256` trùng `signatureHMAC` (L364-366). Không trùng → vòng lặp kết thúc, trả `Lỗi xác thực` (L457).
6. Sau khi qua chữ ký: bắt buộc có `Phone` (hoặc `AgentId`) mới insert (L374-377); map `UserId/IdNhomUser/IdLanding/ChiNhanh` theo `ConfigChat` hoặc `AgentUser`.

## Quan hệ
- **Gọi sang:** Contact service `/api/DataReceive/ReceiveContactFromWebhook`; Redis (idempotent); MassTransit publish (LogContact — hiện comment ở `SendDataToServiceLogContact`).
- **Dữ liệu (entities):** ghi `EventReceive` (1 bản/sự kiện hợp lệ); đọc `ConfigChat`/`SubizConfig` (account_id, Password=secret HMAC, DonViId, ChiNhanhId, IdLandingDefault, IdUserSaleDefault); đọc `AgentUser` (map agent→user).
- **Ripple / đồng bộ:** EventReceive → service Contact tạo contact/hội thoại → SCRM CRM. Job nền resend bản lỗi.

## Luồng xuyên module (Subiz chat → contact SCRM)
```
[Subiz] sự kiện chat → POST api/EventReceive/ReceiveEventSubiz (anonymous)
  → controller đọc body theo content-type → JObject + header X-Hub-Signature-256
  → EventReceiveService.SendDataToLogAndWebhook → SendDataToServiceWebhookSubiz → ReceiveDataAsync:
       • validate type ∈ {contact_info_updated, message_sent}
       • tìm ConfigChat theo account_id (+domain) → lấy secret/landing/đơn vị
       • idempotent Redis (contact_info_updated)
       • so X-Hub-Signature-256 == HMACSHA256(account.Password, rawBody)
       • insert EventReceive + map user/landing/chi nhánh
       • SendToContact → Contact /api/DataReceive/ReceiveContactFromWebhook (tạo contact/hội thoại)
       • nếu có Phone → SendToWebhook
  → controller luôn trả Ok(...) (200) để Subiz không retry
```

## Lỗi đã biết / lưu ý kỹ thuật
- **[BẢO MẬT] Endpoint anonymous data-ingest:** BE `WebhookSubiz.API/Controllers/EventReceiveController.cs:29-31` — `ReceiveEventSubiz` gắn `[AllowAnonymous]`, không permission key. Bất kỳ ai cũng POST được. Lớp bảo vệ duy nhất là service so khớp `account_id` + HMAC `X-Hub-Signature-256` (DataReceiveService.cs:340-366). Nếu lộ `account_id`/`Password` (secret) hoặc bypass được chữ ký → có thể tiêm sự kiện giả tạo contact/hội thoại rác. Cần rà rate-limit/IP allowlist của Subiz.
- **[BẢO MẬT] Chữ ký chỉ ký trên rawBody ở nhánh JSON:** controller chỉ truyền `bodyData` (rawBody) cho service ở nhánh `application/json` (L102); các nhánh query-string/form-data/x-www-form-urlencoded gọi `SendDataToLogAndWebhook(jObject, signatures)` KHÔNG có `rawString` (L66, L155, L197, L248, L285) → service tính `CreateSignature(account.Password, null/empty)` → HMAC trên chuỗi rỗng → kiểm tra chữ ký vô nghĩa với các content-type này. Đây là lỗ hổng bypass chữ ký nếu Subiz gửi non-JSON.
- BE `EventReceiveController.cs` — mọi catch/nhánh lỗi đều `return Ok(...)` (200) CHỦ ĐÍCH để Subiz không gửi lại webhook (comment L70). Nghĩa là client gọi không bao giờ thấy lỗi 4xx/5xx → khó phát hiện sai từ phía gọi; phải tra `LogContact` qua `Common.WriteLogContact`.
- BE `DataReceiveService.cs:445-454` — insert `EventReceive` + `SendToWebhook` bọc trong `try/catch {}` rỗng → nuốt exception, dữ liệu có thể mất âm thầm.
- BE `DataReceiveService.cs:340` — điều kiện `string.IsNullOrEmpty(x.DomainWebsite)` cho phép ConfigChat KHÔNG ràng buộc domain match account_id bất kỳ → nới lỏng xác thực domain.
- Controller KHÔNG kế thừa `[SBAuthorize]` như các controller khác trong service; là endpoint ingest thuần.
