---
controller: ZengiBankController
module: ke-toan
service: WebhookBank
man_hinh_ui: "nội bộ — service-to-service. Tạo phiếu chi hộ (chuyển tiền) qua đối tác Zengi (Payment.API)."
muc_dich: "Tạo phiếu chi hộ (lệnh chuyển tiền/chi hộ ra ngoài) qua API Zengi (CreatePhieuChiHo) + endpoint giả lập kết quả thanh toán (fake-thanhtoan). CẢ HAI hiện KHÔNG có auth."
alias: ["tạo phiếu chi hộ", "chi hộ zengi", "CreatePhieuChiHo", "chuyển tiền zengi"]
auth: anonymous
state_machine: []
deprecated: false
entities: [PhieuChiHo, DataReceiveZengi]
stored_procs: []
depends: [ZengiApi (GetToken, TaoPhieuChiHo), ZengiService (UpdateDataReceive, GetChiHoByMaHoaDon)]
cross_module_in: [Order / kế toán (yêu cầu chi hộ)]
cross_module_out: [Zengi (tạo lệnh chi hộ THẬT), Order/kế toán (cập nhật báo có)]
---

# ZengiBankController — Tạo phiếu chi hộ qua Zengi (CẢNH BÁO: không auth)

> 2 endpoint live, **CẢ HAI KHÔNG có attribute auth** (controller cũng không; `[BankAuthorizeAttribute]` bị comment L36) → **anonymous**. `CreatePhieuChiHo` gọi `ZengiApi.TaoPhieuChiHo` để **tạo lệnh chi hộ/chuyển tiền THẬT ra ngoài**. `fake-thanhtoan` (GET) giả lập callback kết quả thành công (amount hardcode 10000) → `ZengiService.UpdateDataReceive` đánh dấu phiếu đã nhận. **Đây là endpoint mutate luồng TIỀN RA mà không xác thực → lỗ hổng nghiêm trọng nếu reachable từ ngoài.**

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo phiếu chi hộ | (service) | `POST api/ZengiBank/CreatePhieuChiHo` | (không) | anonymous | — | `ZengiApi.GetToken()` → `TaoPhieuChiHo(gridInfo)` tạo lệnh chi hộ THẬT trên Zengi. | service `ZengiApi.TaoPhieuChiHo` | **Anonymous + tạo lệnh CHI TIỀN RA**. `[BankAuthorizeAttribute]` bị comment (L36) → lỗ hổng chi tiền không cần auth. KHÔNG có IsProduction guard. |
| 2 | Giả lập báo có (fake) | (tool test còn sót) | `GET api/ZengiBank/fake-thanhtoan?mahoadon=&ketqua=` | (không) | anonymous | dev/test | Lấy phiếu theo `mahoadon`; nếu có → dựng `ZGPhieuChiHoResult` (amount 10000 hardcode) → `ZengiService.UpdateDataReceive` đánh dấu đã nhận. | service `ZengiService.UpdateDataReceive` | Tool test fake nằm chung controller nghiệp vụ, anonymous → giả lập báo có. `ketqua` không dùng. Nên tách khỏi controller production. |

## Quan hệ
- **Gọi sang:** Zengi (`ZengiApi.TaoPhieuChiHo`), `ZengiService.UpdateDataReceive`/`GetChiHoByMaHoaDon`.
- **Dữ liệu (entities):** phiếu chi hộ Zengi (qua service).
- **Ripple / đồng bộ:** tạo phiếu chi hộ → Zengi xử lý → kết quả trả qua webhook `ZengiReceiveController` (`txn-result`). `fake-thanhtoan` đi tắt qua webhook đó.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WebhookBank/Payment.API/Controllers/ZengiBankController.cs:36-44` — `CreatePhieuChiHo` **KHÔNG auth** (attribute comment) → **lỗ hổng chi tiền: ai gọi được endpoint đều tạo lệnh chuyển tiền thật**. Phải bật `[BankAuthorizeAttribute]` (như QrcodeBankController) hoặc giới hạn internal/IP.
- `ZengiBankController.cs:49-96` — `fake-thanhtoan` là endpoint test giả lập báo có (amount/STK/binCode hardcode), anonymous, KHÔNG có IsProduction guard → tách hoặc xóa khỏi build production.
- Khác với `FakeDataController.fake-thanhtoan` (Payment.API) gần như trùng → trùng lặp code test rải rác nhiều controller.
