---
controller: ProductProccessController
module: he-thong
service: SocketServer
man_hinh_ui: "nội bộ — không có UI (realtime push hub đồng bộ sản phẩm TMĐT, FE lắng nghe socket)"
muc_dich: "Push realtime tiến trình đồng bộ sản phẩm với sàn TMĐT tới FE qua SignalR (ProcessEcommerceHub): lấy SP từ sàn, upload SP nháp lên sàn, tạo SP nháp."
alias: ["socket product ecommerce", "ProcessEcommerceHub", "ReceiveProcessUpdate", "đồng bộ sản phẩm sàn", "GetProductFromEcomm", "draft product"]
auth: anonymous
state_machine: []
deprecated: false
entities: [HubSendMessageProcessDto]
stored_procs: []
depends: ["IHubContext<ProcessEcommerceHub>", "SignalR"]
cross_module_in: ["Service đồng bộ TMĐT (ecommerce_connect) POST các endpoint sản phẩm khi xử lý"]
cross_module_out: ["Push SignalR 'ReceiveProcessUpdate' tới group ecommerce → FE cập nhật tiến trình đồng bộ SP"]
---

# ProductProccessController — Push realtime tiến trình đồng bộ sản phẩm TMĐT (SignalR)

Cầu nối HTTP → SignalR cho luồng đồng bộ sản phẩm với sàn TMĐT. 3 endpoint cùng push event `ReceiveProcessUpdate` lên `ProcessEcommerceHub` nhưng **khác cách dựng group**: lấy SP từ sàn dùng `{UnitConnectId}_{Type}`; upload/tạo SP nháp dùng `{CrmUnitId}_{CrmUnitChildId}_{Type}`. Không lưu DB.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo tiến trình lấy SP từ sàn | (không UI) | POST `/api/ProductProccess/GetProductFromEcomm` | — (không attr) | anonymous | process != null | Push `ReceiveProcessUpdate` tới group `{UnitConnectId}_{Type}`. Lỗi → 500. | — (SignalR) | group key khác 2 endpoint dưới (UnitConnectId) |
| 2 | Báo upload SP nháp lên sàn | (không UI) | POST `/UploadDraftProductToEcomm` | — (không attr) | anonymous | process != null | Push `ReceiveProcessUpdate` tới group `{CrmUnitId}_{CrmUnitChildId}_{Type}`. Lỗi → 500. | — (SignalR) | — |
| 3 | Báo tạo SP nháp | (không UI) | POST `/CreateDraftProduct` | — (không attr) | anonymous | process != null | Push `ReceiveProcessUpdate` tới group `{CrmUnitId}_{CrmUnitChildId}_{Type}`. Lỗi → 500. | — (SignalR) | — |

## Quan hệ
- **Gọi sang:** `IHubContext<ProcessEcommerceHub>.Clients.Group(...).SendAsync("ReceiveProcessUpdate", ...)`.
- **Dữ liệu (entities):** `HubSendMessageProcessDto` (UnitConnectId / CrmUnitId / CrmUnitChildId / Type) — DTO push.
- **Ripple / đồng bộ:** dùng chung `ProcessEcommerceHub` với `OrderProcessController`; FE join group tương ứng nhận tiến trình đồng bộ SP realtime.

## Lỗi đã biết / lưu ý kỹ thuật
- `ProductProccessController.cs:29,53,72` — cả 3 endpoint **không có auth** → anonymous (nên `[PrivateSBAuthorize]`).
- Tên class sai chính tả `ProductProccessController` (2 chữ 'c') → giữ nguyên theo code thật, route `/api/ProductProccess`.
- Group key không nhất quán giữa các action: `GetProductFromEcomm` dùng `UnitConnectId`, 2 action draft dùng `CrmUnitId_CrmUnitChildId` → FE phải join đúng key tương ứng.
- `$"{...}" ?? ""` thừa null-coalescing trên string nội suy (cs:39,63,82).
