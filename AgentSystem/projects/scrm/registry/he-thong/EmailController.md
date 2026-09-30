---
controller: EmailController
module: he-thong
service: Notification
man_hinh_ui: "nội bộ — không có UI (service-to-service gửi email)"
muc_dich: "Nhận yêu cầu gửi email (SMTP) với tiêu đề/nội dung HTML + đính kèm (tải file đính kèm qua URL). HIỆN TẠI gần như vô hiệu: toàn bộ cấu hình SMTP trong constructor đã bị comment → _client/_hostEmail null."
alias: ["Email", "gửi email", "SMTP", "SendMailEvent"]
auth: anonymous
state_machine: []
deprecated: true   # body controller còn nhưng SMTP client bị comment → gọi sẽ NRE; coi như tạm khóa
entities: [SendMailEvent]
stored_procs: []
depends: ["SmtpClient (System.Net.Mail)", "HttpClient/HttpWebRequest (tải file đính kèm)"]
cross_module_in: ["(thiết kế ban đầu) service khác POST tới /Email để gửi mail — hiện không an toàn để gọi"]
cross_module_out: ["SMTP server (khi bật lại cấu hình)"]
---

# EmailController — Gửi email qua SMTP (HIỆN VÔ HIỆU)

Controller gửi email theo `SendMailEvent` (To nhiều địa chỉ ngăn ';'/','? — code split theo ','; Title, Body HTML, Attachments là chuỗi URL ngăn ','). **Toàn bộ phần khởi tạo SmtpClient trong constructor đã bị comment**, nên `_client`/`_hostEmail` = null → gọi `SendEmail` sẽ ném NullReferenceException (đã nuốt trong try/catch, chỉ log). Coi như đã **tạm khóa**, kênh gửi mail thực tế chuyển sang nơi khác (cần xác nhận).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Gửi email | (không UI) | POST `/Email` | — | anonymous (không có `[SBAuthorize]`) | — | Dựng MailMessage (From=_hostEmail, To split ',', Body HTML, Subject=Title), tải từng đính kèm qua URL, `_client.Send`. **Thực tế:** _client null → NRE bị nuốt, chỉ log error → email **không gửi**. | SMTP (disabled) | Constructor comment hết cấu hình SMTP → no-op/lỗi |

## Quan hệ
- **Gọi sang:** SmtpClient (disabled), HttpClient/HttpWebRequest (tải file đính kèm từ URL).
- **Dữ liệu (entities):** `SendMailEvent` (To, Title, Body, Attachments) — DTO, không lưu DB.
- **Ripple / đồng bộ:** (khi bật lại) → SMTP server gửi mail tới người nhận.

## Lỗi đã biết / lưu ý kỹ thuật
- `EmailController.cs:25-60` — **constructor comment toàn bộ** khởi tạo `_client`/`_hostEmail` → mọi field null; `SendEmail` chạy sẽ NRE (bị catch → chỉ log) → **email không bao giờ gửi** ở trạng thái hiện tại.
- `EmailController.cs:62` — `POST /Email` **không có `[SBAuthorize]`** (anonymous) → nếu bật lại SMTP mà không thêm auth thì thành open relay nội bộ (ai cũng gửi mail được).
- `EmailController.cs:110-128` `GetAttachFiles` — tải đính kèm bằng cả `HttpClient.SendAsync().Result` (block) **lẫn** `HttpWebRequest` (gọi 2 lần, lãng phí + nguy cơ deadlock với `.Result`).
- `EmailController.cs:71` — To split bằng `','` trong khi nhiều chỗ hệ thống dùng `';'` → lệch convention tách chuỗi.
- Đề xuất: đánh dấu deprecated, route gửi mail thực tế cần trace lại (có thể nằm ở service khác / queue SendMailEvent).
