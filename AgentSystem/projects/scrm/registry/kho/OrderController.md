---
controller: OrderController
module: kho
service: OrderLogistic
man_hinh_ui: "Giao vận / Đơn hàng — luồng đăng đơn giao vận: tính phí (CalculateFee), xem chi tiết đơn đối tác (ViewDetail), lấy URL theo dõi (CRM_GetTrackingUrl), in vận đơn (PrintOrders). Các endpoint StatusCreateOrder/StatusCancelOrder/ReceiveStatusOrder là webhook đối tác (không UI)."
muc_dich: "Cổng nghiệp vụ đơn giao vận của OrderLogistic qua IOrderService: tính phí ship, xem chi tiết, lấy tracking URL, in vận đơn; đồng thời nhận webhook trạng thái tạo/hủy/cập nhật đơn từ đối tác giao vận."
alias: ["đăng đơn giao vận", "tính phí ship", "calculate fee", "in vận đơn", "tracking url", "webhook trạng thái đơn giao vận", "order delivery connect"]
auth: mixed
state_machine: []
deprecated: false
entities: [DonHangLogistic]
stored_procs: []
depends:
  - "IOrderService (CalculateFee/ViewDetail/CRM_GetTrackingUrl/PrintOrders/ReceiveStatusOrder/StatusCreateOrder/StatusCancelOrder)"
  - "IDeliveryUnitConnectService (inject qua IServiceProvider, dùng trong IOrderService cho call đối tác)"
cross_module_in:
  - "Đối tác giao vận (webhook ReceiveStatusOrder/StatusCreateOrder/StatusCancelOrder) — AllowAnonymous"
  - "FE/DonHangLogistic (CalculateFee/ViewDetail/PrintOrders/CRM_GetTrackingUrl khi đăng đơn)"
cross_module_out:
  - "DeliveryConnect (gọi xuống đối tác giao vận để tính phí / lấy chi tiết / in / tracking)"
  - "DonHangLogistic (cập nhật trạng thái đơn theo webhook trả về)"
---

# OrderController — Nghiệp vụ đơn giao vận + webhook trạng thái (DeliveryConnect)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tính phí giao vận | "Tính phí" (đăng đơn) | POST `api/Order/CalculateFee` | `[SBAuthorize()]` | public | màn đăng/chỉnh đơn | Nhận `CalculateFeeDto` → `_orderService.CalculateFee(reqBody, CurrentUser)` → trả phí ship; trả thẳng `Ok(result)` (không bọc ResponseResult) | IOrderService → DeliveryConnect đối tác | Trả `Ok(result)` thô (khác convention ResponseResult của controller cùng service) |
| 2 | Xem chi tiết đơn đối tác | (mở chi tiết đơn giao vận) | POST `api/Order/ViewDetail` | `[SBAuthorize()]` | public | per-đơn | Nhận entity `DonHangLogistic` (donHangDb) → `_orderService.ViewDetail(...)` → trả chi tiết từ đối tác | IOrderService (entity DonHangLogistic) | Nhận nguyên entity DB từ body làm input → cần tin tưởng caller nội bộ |
| 3 | Lấy URL theo dõi đơn | "Theo dõi đơn" / link tracking | POST `api/Order/CRM_GetTrackingUrl` | `[SBAuthorize]` | public | đơn đã có mã vận đơn | Nhận `ReqGetTrackingUrlDonHang` → `_orderService.CRM_GetTrackingUrl(...)` → trả URL tracking; bọc `ResponseResult` | IOrderService | - |
| 4 | In vận đơn | "In vận đơn" | POST `api/Order/PrintOrders` | `[SBAuthorize]` | public | đơn đã đăng đối tác | Nhận `ReqPrintOrder` → `_orderService.PrintOrders(...)` → trả ảnh/PDF vận đơn; bọc `ResponseResult` | IOrderService → đối tác | - |
| 5 | Webhook nhận trạng thái đơn | (không UI — webhook đối tác) | POST `api/Order/ReceiveStatusOrder` | (không) | anonymous (`[AllowAnonymous]`) | đối tác push | Nhận `ReqReceiveStatusOrder` → `_orderService.ReceiveStatusOrder(reqBody)` → cập nhật trạng thái đơn; trả `Ok(result)` | IOrderService | Không xác thực chữ ký webhook ở controller |
| 6 | Webhook kết quả tạo đơn | (không UI — webhook) | POST `api/Order/StatusCreateOrder` | (không) | anonymous (`[AllowAnonymous]`) | đối tác push | Nhận `object?` → `_orderService.StatusCreateOrder(reqData)` → cập nhật kết quả tạo đơn; trả `Ok(result)` | IOrderService | Input `object?` lỏng kiểu; trùng tên với `NganlqController.StatusCreateOrder` (khác auth: Nganlq chặn passcode) |
| 7 | Webhook kết quả hủy đơn | (không UI — webhook) | POST `api/Order/StatusCancelOrder` | (không) | anonymous (`[AllowAnonymous]`) | đối tác push | Nhận `object?` → `_orderService.StatusCancelOrder(reqData)` → cập nhật kết quả hủy đơn; trả `Ok(result)` | IOrderService | Input `object?` lỏng kiểu |

## Endpoint nội bộ (private / service-to-service)
- [webhook] 3 endpoint `ReceiveStatusOrder` / `StatusCreateOrder` / `StatusCancelOrder` là callback bất đồng bộ từ đối tác giao vận (qua DeliveryConnect) → cập nhật `DonHangLogistic` trạng thái → ripple sang luồng đơn hàng logistic.
- [disabled] `CreateOrder` và `CancelOrder` đã bị comment-out (OrderController.cs:50-56, 76-82) — không còn expose; tạo/hủy đơn đăng đối tác đi đường khác.

## Quan hệ
- **Gọi sang:** `IOrderService` (toàn bộ); IOrderService nội bộ gọi DeliveryConnect đối tác (tính phí/chi tiết/in/tracking/tạo-hủy đơn).
- **Dữ liệu (entities):** `DonHangLogistic` (input ViewDetail; webhook cập nhật trạng thái).
- **Ripple / đồng bộ:** webhook đối tác → cập nhật trạng thái đơn logistic → ảnh hưởng luồng don-hang-logistic / kế toán / kho tác nghiệp downstream.

## Luồng xuyên module
- Đăng đơn: FE → `CalculateFee` (ước phí) → đăng đơn (đường khác) → đối tác xử lý → `StatusCreateOrder` (webhook) cập nhật kết quả tạo đơn.
- Vận hành: đối tác cập nhật hành trình → `ReceiveStatusOrder` (webhook) → đổi trạng thái `DonHangLogistic`.
- Hủy: hủy đơn (đường khác) → đối tác → `StatusCancelOrder` (webhook) cập nhật kết quả hủy.

## Lỗi đã biết / lưu ý kỹ thuật
- `OrderController.cs:50-56, 76-82` `CreateOrder`/`CancelOrder` bị comment-out → đừng kỳ vọng có endpoint này.
- `OrderController.cs:42, 67, 119, 133, 145` các endpoint trả `Ok(result)` thô (không qua `ResponseResult`) → response shape khác các endpoint dùng `ResponseResult`.
- 3 webhook `[AllowAnonymous]` không kiểm chữ ký tại controller → bảo mật dựa vào service/network.
- `StatusCreateOrder` trùng tên với `NganlqController` (route khác, Nganlq có passcode tĩnh) — phân biệt rõ khi triage webhook.
