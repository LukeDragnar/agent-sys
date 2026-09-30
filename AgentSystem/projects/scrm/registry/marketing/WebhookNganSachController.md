---
controller: WebhookNganSachController
module: marketing
service: Facebook
man_hinh_ui: "nội bộ — không có UI (webhook receiver Facebook Ads gọi vào)"
muc_dich: "Endpoint NHẬN WEBHOOK của Facebook cho mảng quảng cáo/ngân sách (Ads): GET dùng để Facebook verify subscription (hub.challenge), POST dùng để nhận payload sự kiện ads từ Facebook. Bản hiện tại là STUB — POST chỉ deserialize body rồi echo lại body, KHÔNG xử lý/lưu (dòng gọi service xử lý webhook đang COMMENT)."
alias: ["webhook ngân sách FB", "WebhookNganSach", "Facebook ads webhook", "webhook quảng cáo Facebook", "hub.challenge ads"]
auth: anonymous
state_machine: []
deprecated: false
entities: [FacebookRequestData]
stored_procs: []
depends:
  - "Models.Core.DTO.Facebook.Client.FacebookWebhook.FacebookRequestData (model payload)"
  - "(kỳ vọng) _serviceData.XuLyWebhook + _settingService.GetBySettingNameAsync('FB_TOKEN_WEBHOOK') — đều đang COMMENT"
cross_module_in:
  - "Facebook (bên ngoài) — Facebook Graph webhook gọi GET verify + POST sự kiện ads tới đây"
cross_module_out:
  - "(kỳ vọng) xử lý sự kiện ads → cập nhật FBCampaignMG/FBAdsetsMG/FBAdsMG hoặc trigger re-sync — HIỆN CHƯA WIRE (XuLyWebhook bị comment)"
---

# WebhookNganSachController — Webhook nhận sự kiện quảng cáo Facebook (Ads/Ngân sách)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Verify subscription (handshake) | (không UI — Facebook gọi) | GET `api/WebhookNganSach?hub.mode=&hub.verify_token=&hub.challenge=` | (none) | anonymous (`[AllowAnonymous]`) | Facebook gọi khi đăng ký webhook | Nếu `hub.mode=="subscribe"` và `hub.verify_token == "TokenSandboxVaFacebook"` → trả về `hub.challenge` (xác thực thành công); ngược lại trả chuỗi rỗng. | (không EF) | **Verify token HARD-CODE** `"TokenSandboxVaFacebook"` (`WebhookNganSachController.cs:23`); dòng lấy token từ setting `FB_TOKEN_WEBHOOK` bị comment (`:22`) → token cố định, không cấu hình được. |
| 2 | Nhận sự kiện ads (payload) | (không UI — Facebook POST) | POST `api/WebhookNganSach` | (none) | anonymous (`[AllowAnonymous]`) | Facebook POST sự kiện ads | **Đúng (kỳ vọng):** đọc raw body → deserialize `FacebookRequestData` → gọi `_serviceData.XuLyWebhook(...)` xử lý/lưu sự kiện → cập nhật dữ liệu ads liên quan. **Hiện tại (STUB):** đọc body, deserialize, rồi `return ResponseResult(MethodResult<object>.ResultWithData(bodyData))` — chỉ ECHO lại body, KHÔNG xử lý. Exception → nuốt lỗi, `return null`. | (không EF — service xử lý bị comment) | **STUB**: `_serviceData.XuLyWebhook(...)` comment (`WebhookNganSachController.cs:44`) → không lưu/không ripple. `catch{}` nuốt lỗi rồi `return null` (`:47-51`) → trả 204/empty, Facebook coi như fail, không trace được. |

## Quy tắc nghiệp vụ (gating)
- **Anonymous toàn controller-method:** cả GET và POST `[AllowAnonymous]` (webhook public theo chuẩn Facebook). Dòng `[SBAuthorize(...)]` cho POST đang comment (`:29`) — không áp.
- **Verify token cố định:** so khớp `hub.verify_token` với hằng `"TokenSandboxVaFacebook"` (chưa đọc từ setting) — chỉ phù hợp sandbox.

## Endpoint nội bộ (private / service-to-service)
- KHÔNG có endpoint private. Đây là cửa NHẬN từ Facebook (anonymous), không phải service-to-service nội bộ.

## Quan hệ
- **Gọi sang:** (kỳ vọng) `_serviceData.XuLyWebhook` để xử lý payload — chưa inject/đang comment. Không gọi service nào ở bản hiện tại.
- **Dữ liệu (entities):** payload map vào `FacebookRequestData`; (kỳ vọng) khi xử lý sẽ chạm `FBCampaignMG`/`FBAdsetsMG`/`FBAdsMG`.
- **Ripple / đồng bộ:** hiện KHÔNG có side-effect (chỉ echo). Khi hoàn thiện, sự kiện ads từ FB → cập nhật trạng thái/ngân sách hoặc trigger re-sync (publish `EventFBCampaign`/...).

## Luồng xuyên module (webhook ads — kỳ vọng)
1. Facebook đăng ký webhook → GET verify → trả `hub.challenge` (hiện token hard-code).
2. Có sự kiện ads (thay đổi trạng thái/ngân sách trên FB) → Facebook POST tới `ReceiveWebhook`.
3. (Đúng) deserialize → `XuLyWebhook` lưu log + cập nhật dữ liệu ads tương ứng → có thể trigger re-sync cây account→campaign→adset→ads.
> Hiện chuỗi GÃY ở bước 3: `XuLyWebhook` bị comment, POST chỉ echo body → không có dữ liệu nào được cập nhật từ webhook.

## Lỗi đã biết / lưu ý kỹ thuật
- `WebhookNganSachController.cs:23` — verify token hard-code `"TokenSandboxVaFacebook"`, không đọc từ setting (`FB_TOKEN_WEBHOOK` comment ở `:22`).
- `WebhookNganSachController.cs:44` — **STUB**: `_serviceData.XuLyWebhook(...)` bị comment; POST chỉ echo `bodyData`, không lưu/không ripple.
- `WebhookNganSachController.cs:47-51` — `catch {}` nuốt lỗi rồi `return null` (trả empty body) → Facebook không nhận 200, không có log lỗi để trace.
- `WebhookNganSachController.cs:17-19` — GET không null-check `hub_mode` trước `.ToLower()` → NRE nếu Facebook gọi thiếu `hub.mode`.
- Khi hoàn thiện: nên inject `IServiceData`/`ISettingService`, đọc verify token từ setting, và xử lý payload (idempotent theo event id của FB) thay vì echo.
