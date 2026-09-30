---
controller: NganlqController
module: kho
service: OrderLogistic
man_hinh_ui: "nội bộ — không có UI (webhook/test endpoint nhận callback trạng thái tạo đơn từ đối tác giao vận, bảo vệ bằng passcode tĩnh)."
muc_dich: "Endpoint nội bộ/test nhận callback 'StatusCreateOrder' (kết quả tạo đơn từ đối tác giao vận) — chặn bằng passcode tĩnh trước khi đẩy vào IOrderService.StatusCreateOrder."
alias: ["nganlq", "webhook trạng thái tạo đơn", "callback tạo đơn giao vận", "status create order"]
auth: anonymous
state_machine: []
deprecated: false
entities: []
stored_procs: []
depends:
  - "IOrderService (StatusCreateOrder — xử lý callback trạng thái tạo đơn giao vận)"
  - "IDeliveryUnitConnectService (inject ở constructor nhưng KHÔNG dùng)"
cross_module_in:
  - "Đối tác giao vận (callback StatusCreateOrder) — qua passcode tĩnh"
cross_module_out: []
---

# NganlqController — Webhook/test trạng thái tạo đơn giao vận

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Nhận callback trạng thái tạo đơn | (không UI — webhook) | POST `api/Nganlq/StatusCreateOrder/{pass}` | (không) — chặn bằng `pass == "Sandbox@123!!"` | anonymous (`[AllowAnonymous]`) | gọi từ ngoài | `pass` sai → trả `401 Unauthorized`; `pass` đúng → gọi `_orderService.StatusCreateOrder(obj)` rồi trả `Ok()` (KHÔNG trả `result` về client) | IOrderService.StatusCreateOrder | Passcode hard-code trong code (`NganlqController.cs:43`) — rủi ro bảo mật; `result` được tính nhưng bị bỏ, luôn trả `Ok()` rỗng |

## Quan hệ
- **Gọi sang:** `IOrderService.StatusCreateOrder(object)`.
- **Dữ liệu (entities):** không thao tác EF trực tiếp ở controller.
- **Ripple / đồng bộ:** side-effect nằm trong `OrderService.StatusCreateOrder` (cập nhật trạng thái đơn giao vận sau khi đối tác xác nhận tạo đơn).

## Lỗi đã biết / lưu ý kỹ thuật
- `NganlqController.cs:43` passcode tĩnh `"Sandbox@123!!"` hard-code → endpoint test/tạm; nên thay bằng cơ chế ký webhook chuẩn.
- `NganlqController.cs:48-49` gọi service rồi trả `Ok()` (bỏ `result`) → client không nhận được kết quả xử lý.
- Doc-comment class ghi nhầm "EmsController" (`NganlqController.cs:13`); `IDeliveryUnitConnectService` inject nhưng không dùng → copy-paste từ controller khác.
- Trùng tên action `StatusCreateOrder` với `OrderController` (DeliveryConnect) nhưng khác route/khác cơ chế auth.
