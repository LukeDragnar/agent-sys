---
controller: ZengiReceiveController
module: ke-toan
service: WebhookBank
man_hinh_ui: "nội bộ — không có UI. Webhook nhận kết quả giao dịch từ đối tác Zengi (BankReceive.API)."
muc_dich: "Nhận callback kết quả giao dịch chi hộ/báo có từ đối tác Zengi qua endpoint api/txn-result → ZengiService.UpdateDataReceive cập nhật trạng thái phiếu chi hộ + báo có."
alias: ["webhook zengi", "txn-result", "callback zengi", "kết quả chi hộ"]
auth: anonymous
state_machine: []
deprecated: false
entities: [DataReceiveZengi, PhieuChiHo]
stored_procs: []
depends: [BankReceive.Service/ZengiService, ZengiSetting]
cross_module_in: [Đối tác Zengi (webhook gọi vào)]
cross_module_out: [Order / kế toán (cập nhật trạng thái chi hộ + báo có)]
---

# ZengiReceiveController — Webhook nhận kết quả giao dịch Zengi

> Controller **1 endpoint live** `POST api/txn-result` (route đặt ở method, controller KHÔNG có `[Route]`). Nhận `ZGPhieuChiHoResult` từ Zengi → `IZengiService.UpdateDataReceive(request)`. Method có `[ZengiAuthorizeAttribute]` → **có xác thực riêng theo cơ chế Zengi** (Basic-auth / token Zengi), KHÔNG hoàn toàn anonymous. Endpoint `gen-token` (Basic auth → cấp JWT) đã bị COMMENT toàn bộ (L42-86). Hằng `ValidBasicAuth` hardcode trong source (L24).

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Nhận kết quả giao dịch | (không UI — Zengi gọi) | `POST api/txn-result` | (không key — `[ZengiAuthorizeAttribute]`) | mixed | webhook | Nhận `ZGPhieuChiHoResult` → `ZengiService.UpdateDataReceive(request)` cập nhật trạng thái phiếu chi hộ / báo có theo `transactionId` → `Ok(rs)`. | service `ZengiService.UpdateDataReceive` | Auth dựa vào filter `ZengiAuthorizeAttribute` (cần verify nó thực sự check Basic/token Zengi). `ValidBasicAuth` hardcode trong code (L24) — credential nằm trong source. |

## Endpoint nội bộ (private / service-to-service)
- Không có endpoint nội bộ. `gen-token` (cấp JWT từ Basic auth, dùng `ZengiSetting.Secret`) đang COMMENT (L42-86) → không hoạt động.

## Quan hệ
- **Gọi sang:** `ZengiService.UpdateDataReceive` → cập nhật phiếu chi hộ + (ripple) báo có sang Order/kế toán.
- **Dữ liệu (entities):** giao dịch Zengi / phiếu chi hộ (qua service, match theo `transactionId`).
- **Ripple / đồng bộ:** kết quả chi hộ Zengi → cập nhật trạng thái thanh toán chi hộ.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WebhookBank/BankReceive.API/Controllers/ZengiReceiveController.cs:88-96` — `UpdateDataReceive` bảo vệ bằng `[ZengiAuthorizeAttribute]` (auth riêng của Zengi), KHÔNG dùng SBAuthorize. Mức độ an toàn = chính filter này; nếu filter chỉ so chuỗi tĩnh thì yếu.
- `ZengiReceiveController.cs:24` — `ValidBasicAuth` hardcode credential trong source code (rủi ro lộ secret qua repo).
- Controller KHÔNG có `[Route("api/[controller]")]` (bị comment L18) — path duy nhất do `[Route("api/txn-result")]` ở method quyết định.
