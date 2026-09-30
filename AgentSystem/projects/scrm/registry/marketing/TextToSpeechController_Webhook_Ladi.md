---
controller: TextToSpeechController
module: marketing
service: Webhook_Ladi
man_hinh_ui: "nội bộ / tiện ích — không có UI riêng. Trả file audio (mp3) đọc text tiếng Việt."
muc_dich: "Tiện ích Text-to-Speech: nhận text, gọi GetVoice.GetSpeechAsync sinh audio tiếng Việt, trả về file mp3."
alias: ["TextToSpeech Ladi", "đọc văn bản", "text to speech", "speak", "TTS landing"]
auth: anonymous
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends:
  - "WebhookLadi.API.Service.GetVoice.GetSpeechAsync (external TTS)"
cross_module_in:
  - "Client (external) — GET api/TextToSpeech/speak?text=..."
cross_module_out: []
---

# TextToSpeechController (Webhook_Ladi) — Text-to-Speech tiếng Việt

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Đọc text → audio | (gọi trực tiếp) | GET `api/TextToSpeech/speak?text=...` | — | **anonymous** (không attribute auth) | luôn mở | `text` rỗng → 400 "Text is required". Ngược lại `GetVoice.GetSpeechAsync(text, "vi")` → trả `File(audioData, "audio/mpeg", "output.mp3")`. Exception → 500 "Error generating speech." | external TTS service | Anonymous, không rate-limit — xem Bảo mật. |

## Quan hệ
- **Gọi sang:** `GetVoice.GetSpeechAsync` (service TTS ngoài).
- **Dữ liệu (entities):** không có.
- **Ripple / đồng bộ:** không có.

## Bảo mật / lưu ý
- Anonymous + không rate-limit: có thể bị lạm dụng gọi TTS hàng loạt (tốn quota/chi phí TTS ngoài, DoS gián tiếp). Cân nhắc giới hạn độ dài `text`, rate-limit, hoặc gác auth.

## Lỗi đã biết / lưu ý kỹ thuật
- `TextToSpeechController.cs:13-30` — endpoint anonymous, không giới hạn độ dài/tần suất `text`.
