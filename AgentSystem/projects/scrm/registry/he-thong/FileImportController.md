---
controller: FileImportController
module: he-thong
service: SocketServer
man_hinh_ui: "nội bộ — không có UI (realtime push hub, FE lắng nghe socket)"
muc_dich: "Đẩy realtime cập nhật tiến trình xóa danh bạ theo file import tới FE qua SignalR (FileImportHub), nhóm theo đơn vị_chi nhánh_type."
alias: ["socket file import", "FileImportHub", "ReceiveFileImportUpdate", "xóa danh bạ theo file"]
auth: anonymous
state_machine: []
deprecated: false
entities: [HubSendMessageFileImportDto]
stored_procs: []
depends: ["IHubContext<FileImportHub>", "SignalR"]
cross_module_in: ["Service xử lý import danh bạ POST /api/FileImport/DeleteContactByFileImport để báo tiến trình"]
cross_module_out: ["Push SignalR 'ReceiveFileImportUpdate' tới group {IdDonVi}_{IdChiNhanh}_{Type} → FE cập nhật UI"]
---

# FileImportController — Push realtime tiến trình import/xóa danh bạ (SignalR)

Controller cầu nối HTTP → SignalR. Service nghiệp vụ (xử lý file import / xóa danh bạ) POST payload vào đây, controller relay sang `FileImportHub` cho group `{IdDonVi}_{IdChiNhanh}_{Type}` qua event `ReceiveFileImportUpdate`. Không lưu DB, không state — chỉ push.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo tiến trình xóa danh bạ theo file | (không UI) | POST `/api/FileImport/DeleteContactByFileImport` | — (không attr) | anonymous | process != null (validate `ValidationCommon.IsNull`) | Push `ReceiveFileImportUpdate(process)` tới group `{IdDonVi}_{IdChiNhanh}_{Type}`. Lỗi → 500. | — (SignalR, không DB) | Không auth → ai biết route + đoán group là spoof được noti |

## Quan hệ
- **Gọi sang:** `IHubContext<FileImportHub>.Clients.Group(...).SendAsync("ReceiveFileImportUpdate", ...)`.
- **Dữ liệu (entities):** `HubSendMessageFileImportDto` (IdDonVi, IdChiNhanh, Type, ...) — DTO push, không lưu.
- **Ripple / đồng bộ:** FE đang join group `{IdDonVi}_{IdChiNhanh}_{Type}` nhận event → cập nhật progress UI realtime.

## Lỗi đã biết / lưu ý kỹ thuật
- `FileImportController.cs:29` — endpoint **không có `[SBAuthorize]`/`[PrivateSBAuthorize]`** → anonymous; bất kỳ ai gọi được có thể bơm noti giả vào group bất kỳ.
- `FileImportController.cs:39` — `$"{...}" ?? ""` áp `??` lên string nội suy (không bao giờ null) → null-coalescing thừa, vô tác dụng.
