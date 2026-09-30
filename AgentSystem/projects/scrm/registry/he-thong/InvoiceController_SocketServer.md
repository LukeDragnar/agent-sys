---
controller: InvoiceController
module: he-thong
service: SocketServer
man_hinh_ui: "nội bộ — không có UI (realtime push hub hóa đơn, FE lắng nghe socket)"
muc_dich: "Push realtime kết quả/thống kê xử lý hóa đơn theo lô (batch) tới FE qua SignalR (InvoiceHub), nhóm theo chi nhánh."
alias: ["socket invoice", "InvoiceHub", "batch invoice", "thống kê hóa đơn realtime", "notify-result-from-batch"]
auth: internal
state_machine: []
deprecated: false
entities: [UpdateStatisticsBatchInvoiceSocketDto, ResultBatchInvoiceSocketDto]
stored_procs: []
depends: ["IHubContext<InvoiceHub>", "IUserPrincipalService", "FunctionCommon.GetGroupNameSocket", "SignalR"]
cross_module_in: ["Service hóa đơn (kế toán) POST notify-statistics-batch-update & notify-result-from-batch sau khi xử lý lô"]
cross_module_out: ["Push SignalR OnStatisticalBatchUpdate / OnResultBatchUpdate tới group CHINHANH_{IdChiNhanh}_* → FE bảng hóa đơn"]
---

# InvoiceController — Push realtime kết quả xử lý hóa đơn theo lô (SignalR)

Cầu nối HTTP → SignalR cho luồng phát hành/xử lý hóa đơn theo batch. Service hóa đơn gọi 2 endpoint nội bộ để báo: (1) cập nhật thống kê lô, (2) kết quả xử lý lô; controller relay tới `InvoiceHub` qua group đặt tên chuẩn `FunctionCommon.GetGroupNameSocket("CHINHANH", IdChiNhanh, <suffix>)`. Cả 2 yêu cầu `[PrivateSBAuthorize]` (chỉ service nội bộ gọi).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo thống kê lô hóa đơn | (không UI) | POST `/api/Invoice/notify-statistics-batch-update` | — (`[PrivateSBAuthorize]`) | internal | — | Push `OnStatisticalBatchUpdate(payload)` tới group `CHINHANH_{IdChiNhanh}_STATISTICS_BATCH_UPDATE`. Trả "Service Ok". | — (SignalR) | — |
| 2 | Báo kết quả xử lý lô | (không UI) | POST `/api/Invoice/notify-result-from-batch` | — (`[PrivateSBAuthorize]`) | internal | — | Push `OnResultBatchUpdate(payload)` tới group `CHINHANH_{IdChiNhanh}_RESULT_BATCH_UPDATE`. Trả "Service Ok". | — (SignalR) | try/catch nuốt lỗi → nếu push fail vẫn trả Ok (mất noti im lặng) |

## Quan hệ
- **Gọi sang:** `IHubContext<InvoiceHub>`, `FunctionCommon.GetGroupNameSocket`.
- **Dữ liệu (entities):** `UpdateStatisticsBatchInvoiceSocketDto`, `ResultBatchInvoiceSocketDto` (IdChiNhanh + payload) — DTO push.
- **Ripple / đồng bộ:** FE đang xem bảng hóa đơn của chi nhánh nhận event → cập nhật thống kê/kết quả lô realtime.

## Lỗi đã biết / lưu ý kỹ thuật
- `InvoiceController.cs:41-44` — `notify-result-from-batch` bọc `try/catch` rỗng (nuốt exception) rồi luôn trả `Ok` → push thất bại không ai biết.
- File trùng tên `InvoiceController` với module ke-toan (khác service) → node này suffix `_SocketServer`.
- Kế thừa `BaseController` (có `IUserPrincipalService`) nhưng auth thực chất là `[PrivateSBAuthorize]` service-to-service, không dùng user login.
