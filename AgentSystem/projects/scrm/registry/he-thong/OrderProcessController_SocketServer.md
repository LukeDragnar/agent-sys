---
controller: OrderProcessController
module: he-thong
service: SocketServer
man_hinh_ui: "nội bộ — không có UI (realtime push hub đồng bộ đơn TMĐT, FE lắng nghe socket)"
muc_dich: "Push realtime tiến trình tạo đơn CRM từ sàn TMĐT tới FE qua SignalR (ProcessEcommerceHub), nhóm theo CrmUnitId_CrmUnitChildId_Type."
alias: ["socket order ecommerce", "ProcessEcommerceHub", "ReceiveProcessUpdate", "tạo đơn CRM từ sàn", "CreateOrderCrm"]
auth: anonymous
state_machine: []
deprecated: false
entities: [HubSendMessageProcessDto]
stored_procs: []
depends: ["IHubContext<ProcessEcommerceHub>", "SignalR"]
cross_module_in: ["Service đồng bộ TMĐT (ecommerce_connect) POST /api/OrderProcess/CreateOrderCrm khi xử lý đơn từ sàn"]
cross_module_out: ["Push SignalR 'ReceiveProcessUpdate' tới group {CrmUnitId}_{CrmUnitChildId}_{Type} → FE cập nhật tiến trình đồng bộ đơn"]
---

# OrderProcessController — Push realtime tiến trình đồng bộ đơn TMĐT (SignalR)

Cầu nối HTTP → SignalR cho luồng đồng bộ đơn hàng từ sàn TMĐT về CRM. Service xử lý đơn POST payload, controller relay tới `ProcessEcommerceHub` group `{CrmUnitId}_{CrmUnitChildId}_{Type}` qua event `ReceiveProcessUpdate`. Không lưu DB.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo tiến trình tạo đơn CRM | (không UI) | POST `/api/OrderProcess/CreateOrderCrm` | — (không attr) | anonymous | process != null (validate `ValidationCommon.IsNull`) | Push `ReceiveProcessUpdate(process)` tới group `{CrmUnitId}_{CrmUnitChildId}_{Type}`. Lỗi → 500. | — (SignalR) | Không auth → spoof noti |

## Quan hệ
- **Gọi sang:** `IHubContext<ProcessEcommerceHub>.Clients.Group(...).SendAsync("ReceiveProcessUpdate", ...)`.
- **Dữ liệu (entities):** `HubSendMessageProcessDto` (CrmUnitId, CrmUnitChildId, Type, ...) — DTO push.
- **Ripple / đồng bộ:** dùng chung `ProcessEcommerceHub` với `ProductProccessController`; FE join group `{CrmUnitId}_{CrmUnitChildId}_{Type}` nhận tiến trình tạo đơn realtime.

## Lỗi đã biết / lưu ý kỹ thuật
- `OrderProcessController.cs:29` — endpoint **không có auth** → anonymous (nên `[PrivateSBAuthorize]`).
- `OrderProcessController.cs:39` — `$"{...}" ?? ""` thừa null-coalescing trên string nội suy.
- Trùng tên `OrderProcessController` với service khác → node suffix `_SocketServer`.
- Hub `ProcessEcommerceHub` dùng chung cho cả Order & Product → phân biệt qua `Type` trong group name.
