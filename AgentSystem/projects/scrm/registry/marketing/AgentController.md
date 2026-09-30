---
controller: AgentController
module: marketing
service: Webhook_Subiz
man_hinh_ui: "nội bộ — cấu hình. Màn quản lý 'Agent' chat nguồn ngoài (Subiz) trong khu External Chat (TI_ExternalChat): list + thêm/sửa/xóa/chi tiết cấu hình agent."
muc_dich: "CRUD cấu hình Agent (nhân viên chat Subiz) — map agent_id của Subiz sang user/nhóm/landing/chi nhánh SCRM để khi sự kiện chat của agent về thì gán đúng người phụ trách."
alias: ["cấu hình agent subiz", "quản lý agent chat", "agent external chat", "map agent subiz"]
auth: internal
state_machine: []
deprecated: false
entities: [AgentUser]
stored_procs: []
depends: [EF WebhookSubizDataContext]
cross_module_in: [SCRM UI External Chat (admin đơn vị/chi nhánh cấu hình)]
cross_module_out: [EventReceiveController/EventReceiveService (đọc AgentUser để map agent→user khi nhận sự kiện)]
---

# AgentController — Cấu hình Agent chat nguồn ngoài (Subiz)

> CRUD thuần cấu hình **AgentUser** (TienIch `TI_ExternalChat`). Mỗi endpoint có `[SBAuthorize(key, mô tả, role)]` riêng → cần đăng nhập + đúng permission key. Dữ liệu agent ở đây được `EventReceiveService` đọc lại khi nhận webhook Subiz để map `agent_id` → user/nhóm/landing/chi nhánh SCRM phụ trách.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm kiếm / load lưới | Lưới + lọc | `POST api/Agent/TimTheoDieuKienAsync` | `Agent_TimTheoDieuKienAsync` (DV-AD-TONG, DV-AD-CN) | internal | màn cấu hình agent | Tìm cấu hình agent theo `AgentFilterDto` + `CurrentUser` (lọc theo đơn vị/chi nhánh user). | **EF** `AgentUser` | — |
| 2 | Thêm mới | "Thêm" | `POST api/Agent/AddAgent` | `Agent_AddAgent` | internal | nút thêm | Tạo cấu hình agent từ `AgentDto`. | **EF** `AgentUser` | — |
| 3 | Cập nhật | "Lưu" | `POST api/Agent/UpdateAgent` | `Agent_UpdateAgent` | internal | sửa 1 agent | Cập nhật cấu hình agent. | **EF** `AgentUser` | — |
| 4 | Xem chi tiết | (mở 1 agent) | `GET api/Agent/LayChiTiet/{id:guid}` | `Agent_LayChiTiet` | internal | click 1 dòng | Trả chi tiết cấu hình theo id. | **EF** `AgentUser` | — |
| 5 | Xóa | "Xóa" | `POST api/Agent/DeleteAgent/{id:guid}` | `Agent_DeleteConfig` | internal | nút xóa | Xóa cấu hình agent theo id. | **EF** `AgentUser` | — |

## Quan hệ
- **Gọi sang:** chỉ EF `WebhookSubizDataContext` (CRUD AgentUser).
- **Dữ liệu (entities):** `AgentUser` (AgentId, UserId, IdNhomUser, IdLanding, ChiNhanhId, DomainWebsite).
- **Ripple / đồng bộ:** không bắn event. AgentUser được `EventReceiveService.ReceiveDataAsync` ĐỌC LẠI để gán người phụ trách khi sự kiện `message_sent`/agent về (DataReceiveService.cs:405-428).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WebhookSubiz.API/Controllers/AgentController.cs` — 5 endpoint đều có permission key riêng (`Agent_*`); dòng `//[SBAuthorize]` rỗng bị comment, attribute thật là bản có key → OK.
- `DeleteAgent` dùng `[HttpPost]` (không phải DELETE) với key `Agent_DeleteConfig` (lệch tiền tố so với key các action khác `Agent_*` nhưng vẫn là key hợp lệ).
- Controller kế thừa `BaseController` (`CurrentUser` từ `HttpContext.Items["Account"]`).
