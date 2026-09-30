---
controller: DoiSoatBankController
module: ke-toan
service: WebhookBank
man_hinh_ui: "nội bộ — không có UI. Hiện KHÔNG có endpoint sống (toàn bộ bị comment)."
muc_dich: "(Dự kiến) đối soát giao dịch ngân hàng MB tự động qua DoiSoatService.DoiSoatBankMB. Hiện toàn bộ controller + endpoint DoiSoatTuDong bị COMMENT — không hoạt động."
alias: ["đối soát bank", "đối soát MB", "DoiSoatTuDong"]
auth: anonymous
state_machine: []
deprecated: true
entities: []
stored_procs: []
depends: [DoiSoatService (DoiSoatBankMB)]
cross_module_in: []
cross_module_out: []
---

# DoiSoatBankController — Đối soát giao dịch ngân hàng (CHẾT — toàn bộ bị comment)

> **File rỗng về mặt runtime.** Toàn bộ class `DoiSoatBankController` (kế thừa `BaseController`) cùng endpoint `POST DoiSoatTuDong` (gọi `DoiSoatService.DoiSoatBankMB(ngày)`) đều bị **comment** (L7-28). Không có endpoint nào được expose, không route. `deprecated: true`. Giữ node để khi cần khôi phục đối soát MB biết logic gốc nằm ở đâu.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có endpoint sống) | — | — | — | — | — | Toàn bộ bị comment | `DoiSoatService.DoiSoatBankMB` (khi bật) | Controller chưa hoạt động. |

## Quan hệ
- **Gọi sang:** (khi bật) `DoiSoatService.DoiSoatBankMB(DateTime)`.
- **Dữ liệu (entities):** —
- **Ripple / đồng bộ:** —

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WebhookBank/Payment.API/Controllers/DoiSoatBankController.cs:7-28` — TOÀN BỘ class + endpoint bị comment. Ngày đối soát còn hardcode `new DateTime(2022,08,03)` (L21) trong code mẫu → nếu khôi phục phải bỏ hardcode. Không có auth khi bật lại → cần gắn auth trước khi expose.
