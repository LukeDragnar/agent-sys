---
controller: CheckerController
module: ke-toan
service: Invoice
man_hinh_ui: "nội bộ — không có UI; endpoint health-check/sandbox-test của service Invoice"
muc_dich: "Health-check đơn giản service Invoice (trả 'HI SandboxVn'); phần code test gọi Viettel createInvoice đã bị comment"
alias: ["health check", "checker", "ping invoice"]
auth: anonymous   # không attribute auth — public/anonymous
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: []
cross_module_in:
  - hạ tầng monitor/health-check (gọi CheckCreateInvoice để kiểm tra service sống)
cross_module_out: []
---

# CheckerController — Health-check / sandbox test

> Controller debug/health-check, KHÔNG attribute auth (anonymous). Chỉ 1 endpoint sống trả chuỗi tĩnh; phần thân thực sự (gọi Viettel `createInvoice` qua WebClient + cert bypass) **đã comment**.
> Giữ vài helper Base64/Authorization tĩnh (di sản test gọi partner) — không phát sinh endpoint.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Health-check service | (monitor/ping) | `GET CheckCreateInvoice` | không key | anonymous | — | Trả `Ok("HI SandboxVn")`; exception → ResponseResult lỗi | — | thân gọi Viettel createInvoice đã comment (controller:28-52) |

## Quan hệ
- **Gọi sang:** không (bản sống chỉ trả chuỗi). Bản comment từng gọi `https://demo-sinvoice.viettel.vn:8443/.../createInvoice` với cert validation bypass.
- **Dữ liệu (entities):** không có.
- **Ripple / đồng bộ:** không có.

## Lỗi đã biết / lưu ý kỹ thuật (path:line)
- `CheckerController.cs:28-52` — code gọi Viettel createInvoice (WebClient, `ServerCertificateValidationCallback => true` bỏ qua SSL, TLS 1.1/1.2) đã comment; `GetData()` trả 1 payload demo hard-code (tax code 0100109106-850).
- Endpoint anonymous + tên "CheckCreateInvoice" gây hiểu nhầm là phát hành HĐ — thực chất chỉ health-check. Nếu uncomment sẽ là rủi ro bảo mật (cert bypass + credential demo).
- Helper `CreateAuthorization`/`Base64Encode`/`Base64Decode` tĩnh — không dùng ở bản hiện tại.
