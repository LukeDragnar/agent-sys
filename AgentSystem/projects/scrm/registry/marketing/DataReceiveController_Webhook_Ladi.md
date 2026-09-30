---
controller: DataReceiveController
module: marketing
service: Webhook_Ladi
man_hinh_ui: "nội bộ — không có UI. Endpoint webhook public nhận contact/lead từ LadiPage, website, WooCommerce."
muc_dich: "Cổng nhận data contact/lead từ LadiPage & WooCommerce (POST/GET, nhiều content-type), log request rồi đẩy vào RabbitMQ để service log-contact xử lý; có endpoint resend từ service log."
alias: ["DataReceive Ladi", "webhook LadiPage", "nhận contact landingpage", "ReceiveData ld-api", "webhook woocommerce", "nhận lead landing"]
auth: anonymous
state_machine: []
deprecated: false
entities: [LogContact, WebhookLadiDataContext]
stored_procs: []
depends:
  - "IDataReceiveService (WebhookLadi.Service.Generated) — SendDataToLogAndWebhook / ReceiveDataWoocommerceAsync / ReceiveDataAsync / SendDataToServiceLogContact / ResendToContactByUser"
  - "WebhookLadiDataContext (EF) — Common.WriteLogContact ghi log lỗi"
  - "RabbitMQ (đẩy contact đã nhận)"
  - "TienIch TI_Landingpage (gate controller)"
cross_module_in:
  - "LadiPage / website / WooCommerce (external) — POST/GET api/DataReceive/ReceiveData/ld-api/lid/{lid}/sc/{sc}"
  - "Service log-contact — POST ResendDataLogAsync để bắn lại data"
cross_module_out:
  - "RabbitMQ → service log-contact (SendDataToServiceLogContact) → tạo/ghép contact trong SCRM"
---

# DataReceiveController (Webhook_Ladi) — Nhận contact/lead từ LadiPage & WooCommerce

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Nhận contact (POST) | (LadiPage gọi) | POST `api/DataReceive/ReceiveData/ld-api/lid/{lid}/sc/{sc}` | — | **anonymous** (`[AllowAnonymous]`) | luôn mở | Log full request → parse theo content-type (json/multipart/x-www-form-urlencoded) → nếu rỗng trả 400 đúng loại → `SendDataToLogAndWebhook(jObject, lid, sc, idRandom)` đẩy RabbitMQ. Exception → ghi `WriteLogContact` + trả Ok (nuốt lỗi). | EF `WebhookLadiDataContext` (log) + RabbitMQ | Anonymous, không verify nguồn — xem Bảo mật. |
| 2 | Nhận contact (GET) | (LadiPage gọi) | GET `api/DataReceive/ReceiveData/ld-api/lid/{lid}/sc/{sc}` | — | **anonymous** | luôn mở | Như trên + hỗ trợ thêm content-type `null` → parse query string. Rỗng → 400 đúng loại; lỗi → 400. | EF + RabbitMQ | Như trên. |
| 3 | Nhận contact WooCommerce | (Woo webhook) | POST `api/DataReceive/ReceiveDataWoocommerce/ld-api/lid/{lid}/sc/{sc}` | — | **anonymous** (`[DisableFormValueModelBinding]`) | luôn mở | Log (IsWoocommerce=true) → parse content-type → `ReceiveDataWoocommerceAsync`. | EF + RabbitMQ | Như trên. |
| 4 | Resend data từ service log | (service-to-service) | POST `api/DataReceive/ResendDataLogAsync` | — | **anonymous** | service log gọi lại | Parse `request.contactReceive` (mẫu mới có `method`/`query`/`body`/`form`/`isWoocommerce`) → dựng lại `itemData` → gọi `ReceiveDataWoocommerceAsync` hoặc `ReceiveDataAsync`. | service | [internal] nhưng để anonymous. |

## Quy tắc nghiệp vụ (parse content-type)
- Phân loại `type`: 1=query string, 2=application/json, 3=multipart/form-data, 4=x-www-form-urlencoded; default → "Nhận dữ liệu thành công" (POST) hoặc 400 (GET).
- `jObject` rỗng/không có values → `WriteLogContact` + trả 400 với message đúng từng `type`.
- Resend: nếu `obj["method"]` tồn tại → mẫu mới, lấy data từ `query`/`body`/`form`; `isWoocommerce=="1"` → đi nhánh Woo.

## Endpoint nội bộ (private / service-to-service)
- [internal] Service log-contact gọi `ResendDataLogAsync` để bắn lại contact đã log (retry).
- [internal] `GetFullRequestLogAsync` → `SendDataToServiceLogContact` đẩy log mỗi request sang service log.

## Quan hệ
- **Gọi sang:** `IDataReceiveService` (đẩy RabbitMQ + log), `Common.WriteLogContact` (EF log lỗi).
- **Dữ liệu (entities):** `WebhookLadiDataContext` (LogContact); contact đẩy qua RabbitMQ sang service log.
- **Ripple / đồng bộ:** RabbitMQ → service log-contact → tạo/ghép contact + đẩy webhook tiếp.

## Luồng xuyên module
LadiPage/Woo bắn lead → `ReceiveData*` (anonymous) → log request → parse → `SendDataToLogAndWebhook` đẩy RabbitMQ → service log-contact consume → tạo contact SCRM → (lỗi) `ResendDataLogAsync` bắn lại.

## Bảo mật / lỗ hổng (anonymous nhận data)
- Tất cả endpoint `[AllowAnonymous]`, **không verify chữ ký/token nguồn** — bất kỳ ai biết `lid`/`sc` đều POST được lead giả vào hệ thống (spam contact, bơm lead rác, làm nhiễu báo cáo marketing). Cần xác thực nguồn (secret theo `lid`, signature, hoặc IP allowlist của LadiPage).
- `ResendDataLogAsync` anonymous nhưng là endpoint nội bộ service→service → nên giới hạn internal/IP.
- Catch ở action POST (`:120-125`) **nuốt lỗi và trả Ok** → mất dấu vết khi xử lý hỏng (chỉ còn log DB).

## Lỗi đã biết / lưu ý kỹ thuật
- `DataReceiveController.cs:47-126` — `ReceiveDataAsync` anonymous, catch trả Ok (nuốt lỗi).
- `DataReceiveController.cs:420-482` — `ResendDataLogAsync` anonymous (service-to-service không gác).
- `DataReceiveController.cs:33-37` — inject `WebhookLadiDataContext` chỉ để `WriteLogContact`/`SaveChangesAsync`.
- Nhiều private parser trùng logic (Json/Multipart/FormUrlEncoded/QueryString) — cồng kềnh nhưng cần cho đa content-type.
