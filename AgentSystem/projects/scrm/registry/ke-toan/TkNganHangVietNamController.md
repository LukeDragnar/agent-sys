---
controller: TkNganHangVietNamController
module: ke-toan
service: WebhookBank
man_hinh_ui: "nội bộ — service-to-service / màn cấu hình. Kiểm tra tên người thụ hưởng theo số tài khoản qua Zengi (Payment.API)."
muc_dich: "Tra cứu/đối chiếu tên chủ tài khoản (người thụ hưởng) theo số tài khoản + ngân hàng qua API Zengi — phục vụ nhập tài khoản chi hộ chính xác."
alias: ["check người thụ hưởng", "kiểm tra số tài khoản", "tra tên chủ TK", "CheckNguoiThuHuong"]
auth: internal
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends: [ZengiApi (GetToken, CheckNguoiThuHuong)]
cross_module_in: [FE/cấu hình chi hộ (nhập STK người nhận)]
cross_module_out: [Zengi (tra cứu tên chủ TK)]
---

# TkNganHangVietNamController — Kiểm tra người thụ hưởng theo STK (Zengi)

> Controller-level `[SBAuthorize()]` + method `[SBAuthorize]` → **internal (yêu cầu đăng nhập)**. 1 endpoint: lấy token Zengi rồi tra tên chủ tài khoản theo số TK + bank code. Chỉ READ/tra cứu, không mutate tiền.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Kiểm tra người thụ hưởng | "Kiểm tra STK" | `POST api/TkNganHangVietNam/CheckNguoiThuHuong` | (login `[SBAuthorize]`) | internal | nhập STK + ngân hàng | `ZengiApi.GetToken()` → `CheckNguoiThuHuong(gridInfo)` trả tên chủ TK. | service `ZengiApi.CheckNguoiThuHuong` (gọi Zengi) | — |

## Quan hệ
- **Gọi sang:** Zengi (`ZengiApi.GetToken` + `CheckNguoiThuHuong`).
- **Dữ liệu (entities):** không ghi DB — chỉ tra cứu API ngoài.
- **Ripple / đồng bộ:** dùng trước khi tạo phiếu chi hộ (`ZengiBankController`) để xác nhận đúng người nhận.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WebhookBank/Payment.API/Controllers/TkNganHangVietNamController.cs:17,27` — `[SBAuthorize()]` cấp class + method → internal. Read-only, không mutate tiền → rủi ro thấp.
