---
controller: DataReceiveController
module: ke-toan
service: AccountingReceive
man_hinh_ui: "nội bộ — không có UI (endpoint nhận dữ liệu kế toán từ đối tác/phần mềm ngoài theo partnerName)"
muc_dich: "Cổng NHẬN dữ liệu kết quả từ đối tác kế toán: nhận body JSON theo {partnerName}, enqueue nền để lưu DataReceiveResultPartner và publish message AccountingResultPartnerMessage cho consumer xử lý tiếp."
alias: ["nhận data kế toán", "DataReceive", "receive partner data", "cổng nhận đối tác"]
auth: anonymous
state_machine: [INPROCESS]
deprecated: false
entities: [DataReceiveResultPartner]
stored_procs: []
depends: ["IDataReceiveResultPartnerService", "IBackgroundTaskQueue", "IPublishEndpoint (MassTransit)"]
cross_module_in: ["Đối tác/phần mềm kế toán bên ngoài POST dữ liệu vào theo partnerName"]
cross_module_out: ["RabbitMQ: publish AccountingResultPartnerMessage → consumer kế toán xử lý/đối soát chứng từ"]
---

# DataReceiveController (AccountingReceive) — Cổng nhận dữ liệu kế toán từ đối tác

Endpoint nhận dữ liệu kết quả từ đối tác kế toán. Route `api/DataReceive/{partnerName}`. Controller-level KHÔNG có `[SBAuthorize]` → anonymous (mở cho đối tác bắn vào). Luồng: parse body JSON → enqueue background task → `SaveData` lưu bản ghi `DataReceiveResultPartner` (Status=INPROCESS) → publish `AccountingResultPartnerMessage` cho consumer xử lý bất đồng bộ.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Nhận dữ liệu đối tác | (nội bộ/đối tác) | POST `api/DataReceive/{partnerName}` body=object | — | anonymous | — | `ReceiveData`: upper partnerName → parse JObject (null → trả lỗi "Dữ liệu null" + log) → enqueue task gọi `SaveData` (insert DataReceiveResultPartner INPROCESS + publish AccountingResultPartnerMessage) → trả "Nhận data thành công" NGAY (không chờ lưu) | EF + RabbitMQ publish | Trả thành công trước khi thực sự lưu/publish (fire-and-forget); lỗi trong background chỉ ghi log |

## Endpoint nội bộ (private / service-to-service)
- [anonymous] Đối tác kế toán POST `{partnerName}` → nhận chứng từ/kết quả → service xử lý nền + đẩy queue.

## Luồng xuyên module
- Trigger: đối tác POST data theo partnerName.
- Bước: validate JSON → enqueue → `SaveData` insert `DataReceiveResultPartner` (Status=INPROCESS, IdTransaction mới) → `Publish<AccountingResultPartnerMessage>`.
- Side-effect cross-module: consumer phía kế toán nhận message → xử lý/đối soát chứng từ; dữ liệu cũ được dọn bởi job (LiveController/JobData).

## Lỗi đã biết / lưu ý kỹ thuật
- `DataReceiveController.cs:6-8` — không có `[SBAuthorize]` → anonymous: bất kỳ ai biết URL + partnerName đều POST được, không xác thực đối tác → cần whitelist/secret ở gateway.
- `DataReceiveResultPartnerService.cs:64-73` — `ReceiveData` trả "thành công" NGAY sau khi enqueue, chưa lưu DB/publish xong → client nhận 200 nhưng data có thể fail trong background (chỉ log, không báo lại caller).
- `DataReceiveResultPartnerService.cs:54` — `JObject.Parse` body: body không phải JSON hợp lệ sẽ ném exception → vào catch trả lỗi (không phải null-check), thông điệp lỗi là exception format.
