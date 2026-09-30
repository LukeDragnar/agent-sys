---
controller: LogDataProcessController
module: he-thong
service: SocketServer
man_hinh_ui: "nội bộ — không có UI (realtime push hub, FE lắng nghe socket)"
muc_dich: "Push realtime sự kiện xóa đơn hàng tới FE qua SignalR (LogDataProcessHub), nhóm theo đơn vị_chi nhánh."
alias: ["socket xóa đơn", "LogDataProcessHub", "ReceiveDonHangUpdate", "log đơn hàng xóa"]
auth: anonymous
state_machine: []
deprecated: false
entities: [HubSendMessageLogDonHangXoa]
stored_procs: []
depends: ["IHubContext<LogDataProcessHub>", "SignalR"]
cross_module_in: ["Service xử lý đơn hàng POST /api/LogDataProcess/DeleteDonHang khi có đơn bị xóa"]
cross_module_out: ["Push SignalR 'ReceiveDonHangUpdate' tới group {IdDonVi}_{IdChiNhanh}_DonHangXoa → FE cập nhật danh sách đơn"]
---

# LogDataProcessController — Push realtime sự kiện xóa đơn hàng (SignalR)

Cầu nối HTTP → SignalR cho sự kiện xóa đơn. Service nghiệp vụ POST payload, controller relay tới `LogDataProcessHub` group `{IdDonVi}_{IdChiNhanh}_DonHangXoa` qua event `ReceiveDonHangUpdate`. Không lưu DB, không state.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo đơn hàng bị xóa | (không UI) | POST `/api/LogDataProcess/DeleteDonHang` | — (không attr) | anonymous | process != null (validate `ValidationCommon.IsNull`) | Push `ReceiveDonHangUpdate(process)` tới group `{IdDonVi}_{IdChiNhanh}_DonHangXoa`. Lỗi → 500. | — (SignalR) | Không auth → spoof noti được |

## Quan hệ
- **Gọi sang:** `IHubContext<LogDataProcessHub>.Clients.Group(...).SendAsync("ReceiveDonHangUpdate", ...)`.
- **Dữ liệu (entities):** `HubSendMessageLogDonHangXoa` (IdDonVi, IdChiNhanh, ...) — DTO push.
- **Ripple / đồng bộ:** FE join group `{IdDonVi}_{IdChiNhanh}_DonHangXoa` nhận event → refresh/đánh dấu đơn bị xóa realtime.

## Lỗi đã biết / lưu ý kỹ thuật
- `LogDataProcessController.cs:29` — endpoint **không có auth attribute** → anonymous; nên gắn `[PrivateSBAuthorize]` vì là service-to-service.
- `LogDataProcessController.cs:39` — `$"{...}" ?? ""` thừa null-coalescing trên string nội suy.
