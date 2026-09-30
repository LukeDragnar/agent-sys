---
controller: FBDataWebhook1stController
module: marketing
service: FacebookChat
man_hinh_ui: "nội bộ — không có UI. Route api/FBDataWebhook1st (trong solution FacebookChat)."
muc_dich: "Controller scaffold cho entity FBDataWebhook1st (log webhook Facebook tầng 1 / nhận đầu vào trước khi xử lý) TRONG SERVICE FacebookChat. HIỆN TẠI rỗng — mọi action CRUD đã comment, không expose endpoint. Service chỉ có CRUD cơ bản + TimTheoDieuKienAsync."
alias: ["FBDataWebhook1st (FacebookChat)", "log webhook tầng 1", "webhook FB 1st"]
auth: anonymous
state_machine: []
deprecated: true
entities: [FBDataWebhook1st]
stored_procs: []
depends:
  - "IFBDataWebhook1stService (BaseService<FBDataWebhook1st>) — CRUD + TimTheoDieuKienAsync (phân trang)"
  - "EF: IRepository<FBDataWebhook1st>"
cross_module_in:
  - "Pipeline/consumer nhận webhook Facebook tầng 1 — gọi IFBDataWebhook1stService để lưu raw log (controller này không phải điểm nhận thật)"
cross_module_out: []
---

# FBDataWebhook1stController (FacebookChat) — Log webhook FB tầng 1 (scaffold, chưa active)

> Route `api/FBDataWebhook1st` trong solution **FacebookChat** (suffix `_FacebookChat` để tránh nhầm với node `FBDataWebhook1stController_FbWebhook` của service FacebookWebhook). Inject `IFBDataWebhook1stService` nhưng **mọi action đều comment** → 0 endpoint runtime.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | (Tất cả CRUD) Xem chi tiết / Thêm / Cập nhật / Xóa / Tìm theo điều kiện | — | (không có — đều comment) | `FBDataWebhook1st_*` (chỉ còn trong comment) | anonymous | — | **Không hoạt động** — các action đã comment (`FBDataWebhook1stController.cs:26-111`). `[TienIch]` cũng comment (`:13`). Controller chỉ giữ ctor inject service. | — | Controller rỗng: 0 endpoint active. |

## Endpoint nội bộ (private / service-to-service)
- Controller không có endpoint active. `IFBDataWebhook1stService` chỉ là CRUD + 1 hàm query:
  - `TimTheoDieuKienAsync(FBDataWebhook1stFilterDto)` — phân trang FBDataWebhook1st (order theo NgayTao), map sang `FBDataWebhook1stDto`.
  - `UpdateAsync` override — MarkDirty toàn bộ field (IdDonVi, FanPageId, Data, LogMessage, Retry, IsProcessing, IsForwardRequest, IsComplete, ...). Entity giống FBDataWebhook nhưng là **tầng nhận trước (1st)** — log raw, có cờ retry/forward.
  - Service này **không inject sub-service hay publish event** (khác hẳn FBDataWebhookService) → thuần lưu/đọc log webhook tầng đầu.

## Quan hệ
- **Gọi sang:** chỉ EF `FBDataWebhook1st`.
- **Dữ liệu (entities):** `FBDataWebhook1st` (Data, LogMessage, Retry, IsProcessing, IsForwardRequest, IsComplete) — bản ghi log webhook tầng 1.
- **Ripple / đồng bộ:** không có (không publish event). Dữ liệu tầng 1 sau đó được pipeline đẩy sang xử lý ở `FBDataWebhookService`.

## Lỗi đã biết / lưu ý kỹ thuật
- `FBDataWebhook1stController.cs:13` — `[TienIch(... TI_ChatHub)]` bị comment; `:26-111` — mọi action comment → controller rỗng. `deprecated: true`.
- **Trùng tên với service khác:** marketing đã có `FBDataWebhook1stController_FbWebhook.md` (service `FacebookWebhook`). Node này (`_FacebookChat`) là bản trong solution FacebookChat — đừng gộp.
- So với `FBDataWebhookService` (orchestrator có sub-service + publish contact), service 1st đơn giản hơn nhiều: chỉ CRUD + paging, không ripple.
