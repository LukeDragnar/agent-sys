---
controller: WebhookController
module: marketing
service: TiktokWebhook
man_hinh_ui: "nội bộ — không có UI. Endpoint webhook (bản cũ/khác) nhận dữ liệu dạng Facebook-style, hiện gần như rỗng."
muc_dich: "Webhook nhận dữ liệu (dạng FacebookRequestData) cho TiktokWebhookProcess — bản hiện tại chỉ deserialize body rồi KHÔNG xử lý gì, trả null. Là endpoint dead/stub."
alias: ["WebhookController Tiktok", "webhook nhận Tiktok (cũ)", "ReceiveData TiktokProcess"]
auth: anonymous
state_machine: []
deprecated: true
entities: []
stored_procs: []
depends:
  - "ITTDataWebhookService (TiktokWebhook.Service.Generated) — inject nhưng không gọi (logic SaveAndProcessWebhookForward bị comment)"
cross_module_in:
  - "External (TikTok/FB-style) — POST api/Webhook (nếu được route tới)"
cross_module_out: []
---

# WebhookController (TiktokWebhookProcess) — Webhook nhận dữ liệu (DEAD/STUB)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Nhận webhook (cũ) | (không UI) | POST `api/Webhook` | — | **anonymous** (`[AllowAnonymous]`) | luôn mở | **Hiện tại (stub):** đọc body → `JsonConvert.DeserializeObject<FacebookRequestData>` → bỏ qua kết quả → **trả `null`**. Dòng `SaveAndProcessWebhookForward` bị comment. **Kỳ vọng (nếu wire):** lưu & xử lý webhook qua `_webhookservice`. | `ITTDataWebhookService` (không gọi) | **DEAD STUB** — trả `null`, không lưu, không publish; `[TienIch]` cũng bị comment. |

## Quan hệ
- **Gọi sang:** (kỳ vọng) `ITTDataWebhookService.SaveAndProcessWebhookForward` — hiện comment.
- **Dữ liệu (entities):** không ghi gì.
- **Ripple / đồng bộ:** không có (luồng webhook TikTok thật đi qua `TiktokWebhooksController.ReceiveData` + 10 consumer, không qua controller này).

## Bảo mật / lưu ý
- `[AllowAnonymous]`; tuy nhiên action trả `null` và không xử lý → không có side-effect, rủi ro hiện tại thấp. Nếu sau này wire `_webhookservice` mà vẫn anonymous + không verify signature → giống lỗ hổng của `TiktokWebhooksController`.

## Lỗi đã biết / lưu ý kỹ thuật
- `Webhook\WebhookController.cs:54` — `ReceiveData` trả `null` (không xử lý) → dead stub.
- `Webhook\WebhookController.cs:46-47` — `SaveAndProcessWebhookForward` bị comment.
- `Webhook\WebhookController.cs:10` — `[TienIch(... TI_Chung)]` bị comment.
- Dùng `FacebookRequestData` (copy từ FB webhook) cho service TikTok → naming/DTO lệch ngữ cảnh.
- `partial class` nhưng chỉ có 1 file → không cần partial.
