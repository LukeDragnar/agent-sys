---
controller: MisaAmisCallbackController
module: ke-toan
service: Accounting
man_hinh_ui: "nội bộ — không có UI. Webhook endpoint nhận callback từ MISA AMIS Kế toán (đối tác hạch toán) báo kết quả cất/xóa chứng từ đồng bộ."
muc_dich: "Nhận webhook callback từ MISA AMIS sau khi đẩy chứng từ sang AMIS: verify chữ ký HMAC-SHA256 theo app_id, hợp lệ thì xử lý bất đồng bộ (Task riêng) cập nhật trạng thái đồng bộ từng org_refid."
alias: ["misa callback", "misa amis", "callback misa", "webhook misa", "đồng bộ misa", "amis kế toán", "call_back_data"]
auth: anonymous
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends:
  - IMisaService                 # inject nhưng KHÔNG dùng trong nhánh callback (xem known-issue)
  - HMACSHA256 (verify signature theo _app_id hardcode)
cross_module_in:
  - MISA AMIS Kế toán (đối tác external — gọi webhook POST call_back_data)
cross_module_out: []
---

# MisaAmisCallbackController — Webhook nhận callback từ MISA AMIS Kế toán

> Endpoint webhook **không UI, không xác thực token** — `[AllowAnonymous]`. MISA AMIS gọi POST `call_back_data` báo kết quả cất/xóa chứng từ. Controller verify chữ ký: `signature = HMAC-SHA256(param.data, app_id)` (app_id **hardcode** `7bb9ce7a-...`); khác → trả `Success=false, ErrorCode=InvalidParam`. Hợp lệ → khởi 1 `Task` chạy nền `DoCallBackData` (trả response ngay, không chờ xử lý). Bản hiện tại là **TEMPLATE demo của MISA** — phần xử lý thực (cập nhật trạng thái org_refid, Save/Delete kho tạm) còn để TRỐNG (chỉ comment). `IMisaService` được inject nhưng không dùng ở luồng callback.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Nhận callback đồng bộ chứng từ | (webhook — không UI) | `POST api/MisaAmisCallback/call_back_data` | — (none) | anonymous | MISA AMIS gọi sau khi cất/xóa chứng từ | Sinh `signature=HMAC-SHA256(param.data, _app_id)`; ≠ `param.signature` → `Success=false/InvalidParam/"Signature invalid"`. Khớp → `new Task(DoCallBackData).Start()` (fire-and-forget) rồi trả `Success=true` ngay. Exception → `Success=false/Exception/ex.Message`. | — (in-memory; xử lý thực để trống) | (a) `new Task(...).Start()` fire-and-forget không TaskScheduler/await → lỗi trong task **nuốt im lặng**, không retry, không log. (b) `DoCallBackData` body rỗng: `SaveCallBack`/`DeleteCallBack` chỉ comment, vòng `foreach` org_refid **không làm gì** → callback **không thực sự cập nhật** trạng thái đồng bộ (TEMPLATE chưa hiện thực hóa). (c) `_app_id` hardcode trong code. |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Verify chữ ký:** chỉ chấp nhận khi `HMAC-SHA256(data, app_id_hex_lowercase) == param.signature`. Sai chữ ký → từ chối (InvalidParam), KHÔNG xử lý data.
- **CallBackDataType** (enum nội bộ): `SaveVoucher=1`, `DeleteVoucher=2` → đi nhánh deserialize `List<CallbackDataDetail>` (nhưng body xử lý còn trống); `UpdateDocumentRef=4`, `UpdateTaxInfoASP=5` (chỉ dùng cho ASP MISA, đối tác bỏ qua); `None=0`. default → bỏ qua.

## Quan hệ
- **Gọi sang:** không (luồng callback không gọi service/DB nào — bản template).
- **Dữ liệu (entities):** không persist (kho tạm Save/Delete chưa hiện thực).
- **Ripple / đồng bộ:** Là điểm NHẬN của luồng đồng bộ chứng từ kế toán Accounting → MISA AMIS (chiều về). Khi hiện thực hóa, sẽ cập nhật trạng thái đồng bộ từng `org_refid`/chứng từ. Hiện tại không ripple thực tế.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/Misa/MisaAmisCallbackController.cs`
  - L17 `_app_id` hardcode `"7bb9ce7a-6411-499c-baf8-34a21121a383"`.
  - L28 `[AllowAnonymous]` — webhook không auth token; bảo vệ duy nhất là chữ ký HMAC theo app_id.
  - L45-49 `new Task(() => DoCallBackData(param)).Start()` fire-and-forget → exception trong task không bắt/log; không đảm bảo hoàn thành nếu process recycle.
  - L68-104 `DoCallBackData`/`SaveCallBack`/`DeleteCallBack` body chỉ comment (`//.....`) → **bản TEMPLATE chưa hiện thực hóa**: callback nhận xong KHÔNG cập nhật trạng thái thực.
  - L24 inject `IMisaService` nhưng nhánh callback không dùng; method `GetBranchId` (L61-66) đã comment-out.
  - Các class DTO (CallbackDataInput/Output/Detail) + enum khai báo lồng trong controller (L131-185).
