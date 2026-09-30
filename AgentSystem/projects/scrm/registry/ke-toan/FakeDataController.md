---
controller: FakeDataController
module: ke-toan
service: WebhookBank
man_hinh_ui: "nội bộ — không có UI. Công cụ DEV/TEST tạo dữ liệu giả (Payment.API)."
muc_dich: "Công cụ dev/test: tạo phiếu chi hộ giả (Zengi) và giả lập kết quả 'đã thanh toán' để kiểm thử luồng chi hộ — KHÔNG dùng cho nghiệp vụ thật."
alias: ["fake data", "fake phieu chi", "fake thanh toan", "tool test chi hộ"]
auth: anonymous
state_machine: []
deprecated: false
entities: [PhieuChiHo, DataReceiveZengi]
stored_procs: []
depends: [ZengiApi (TaoPhieuChiHo, GetToken), ZengiService (UpdateDataReceive, GetChiHoByMaHoaDon)]
cross_module_in: []
cross_module_out: [Zengi (tạo phiếu chi hộ thật), Order (callback UrlCallBack hardcode localhost)]
---

# FakeDataController — Công cụ tạo dữ liệu giả test chi hộ (DEV/TEST)

> **Tool dev/test, KHÔNG phải nghiệp vụ.** 2 endpoint, **KHÔNG auth** (attribute `[BankAuthorizeAttribute]` bị comment L36). `fake-phieuchi` tạo `RequestPhieuChiHoDto` với **IdDonVi/IdChiNhanh/IdNganHangChi/STK người nhận HARDCODE** + `UrlCallBack=http://localhost:5000/...` rồi gọi `ZengiApi.TaoPhieuChiHo` THẬT (tạo lệnh chi hộ thật trên Zengi). `fake-thanhtoan` giả lập webhook kết quả thành công (`ZGPhieuChiHoResult`, amount hardcode 10000) → `ZengiService.UpdateDataReceive` cập nhật phiếu như đã nhận tiền. **Nguy hiểm nếu deploy lên Production** vì anonymous + tạo lệnh chi/giả lập báo có.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo phiếu chi hộ giả | (tool) | `POST api/FakeData/fake-phieuchi` | (không) | anonymous | dev/test | Dựng `RequestPhieuChiHoDto` toàn field HARDCODE → `ZengiApi.GetToken()` + `TaoPhieuChiHo` (tạo lệnh chi hộ THẬT trên Zengi). | service `ZengiApi.TaoPhieuChiHo` | Anonymous + tạo lệnh chi hộ thật; IdDonVi/STK/UrlCallBack hardcode (L44-54). Lộ ra Production = lỗ hổng tạo lệnh chi. |
| 2 | Giả lập báo có thành công | (tool) | `GET api/FakeData/fake-thanhtoan?mahoadon=&ketqua=` | (không) | anonymous | dev/test | Lấy phiếu theo `mahoadon`; nếu có → dựng `ZGPhieuChiHoResult` (amount 10000, transType "C", binCode/STK hardcode) → `ZengiService.UpdateDataReceive` đánh dấu đã nhận. Không có → `Ok("Hóa đơn không tồn tại")`. | service `ZengiService.UpdateDataReceive` | Anonymous + ép trạng thái "đã thanh toán" cho phiếu chi hộ → có thể bị lạm dụng giả lập báo có. `ketqua` nhận vào nhưng KHÔNG dùng. |

## Quan hệ
- **Gọi sang:** Zengi (`TaoPhieuChiHo`), `ZengiService.UpdateDataReceive` (giả lập callback Zengi).
- **Dữ liệu (entities):** phiếu chi hộ Zengi (qua service).
- **Ripple / đồng bộ:** giống webhook thật — đánh dấu báo có/đã chi (ripple sang Order/kế toán).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WebhookBank/Payment.API/Controllers/FakeDataController.cs:36` — `[BankAuthorizeAttribute]` bị comment → **cả 2 endpoint anonymous**. Đây là tool test nhưng **KHÔNG chặn Production** (không có IsProduction guard như BankSend) → nếu lên Production = lỗ hổng tạo lệnh chi + giả lập báo có.
- `fake-phieuchi` (L42-55): Guid đơn vị/chi nhánh/ngân hàng + STK người nhận + UrlCallBack `localhost:5000` HARDCODE.
- `fake-thanhtoan` (L65): tham số `ketqua` không được dùng; amount/binCode/STK hardcode (L80-88).
- **Khuyến nghị:** loại controller này khỏi build Production hoặc gắn `[BankAuthorizeAttribute]` + IsProduction guard.
