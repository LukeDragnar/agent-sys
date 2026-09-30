---
controller: FBDataWebhookController
module: marketing
service: FacebookChat
man_hinh_ui: "nội bộ — không có UI. Route api/FBDataWebhook (trong solution FacebookChat)."
muc_dich: "Controller scaffold cho entity FBDataWebhook (log + xử lý dữ liệu webhook Facebook: post/comment/permission/user) TRONG SERVICE FacebookChat. HIỆN TẠI rỗng — mọi action CRUD đã comment, không expose endpoint. Logic xử lý webhook thật nằm trong FBDataWebhookService (gọi từ consumer/pipeline khác)."
alias: ["FBDataWebhook (FacebookChat)", "log webhook chat FB", "xử lý webhook FB ChatHub"]
auth: anonymous
state_machine: []
deprecated: true
entities: [FBDataWebhook, FBUser, FBPermission, FBFanpage, FBPost, FBComment]
stored_procs: []
depends:
  - "IFBDataWebhookService (BaseService<FBDataWebhook>) — orchestrate xử lý webhook; inject nhiều sub-service"
  - "IFBUserService / IFBPermissionService / IFBFanpageService / IFBPostService / IFBCommentService"
  - "ICommonService / ICommonCacheService"
  - "IPublishEndpoint + IPublishEndpointContactReceiveFB (MassTransit/RabbitMQ) — đẩy tạo contact từ webhook"
  - "EF: IRepository<FBDataWebhook|FBUser|FBPermission|FBFanpage|FBPost|FBComment>"
cross_module_in:
  - "Pipeline/consumer nhận webhook Facebook — gọi IFBDataWebhookService để parse & lưu dữ liệu (controller này không phải điểm nhận thật)"
cross_module_out:
  - "Contact service — publish event tạo contact từ khách FB (IPublishEndpointContactReceiveFB, RabbitMQ); flag IsUseFBCreateContactByQueue=true"
---

# FBDataWebhookController (FacebookChat) — Xử lý webhook FB (scaffold, chưa active)

> Route `api/FBDataWebhook` trong solution **FacebookChat** (suffix `_FacebookChat` để tránh nhầm với node `FBDataWebhookController_FbWebhook` của service FacebookWebhook). Inject `IFBDataWebhookService` nhưng **mọi action đều comment** → 0 endpoint runtime.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | (Tất cả CRUD) Xem chi tiết / Thêm / Cập nhật / Xóa / Tìm theo điều kiện | — | (không có — đều comment) | `FBDataWebhook_*` (chỉ còn trong comment) | anonymous | — | **Không hoạt động** — các action đã comment (`FBDataWebhookController.cs:26-111`). `[TienIch]` cũng comment (`:13`). Controller chỉ giữ ctor inject service. | — | Controller rỗng: 0 endpoint active. |

## Endpoint nội bộ (private / service-to-service)
- Controller không có endpoint active. `IFBDataWebhookService` là **orchestrator xử lý webhook** thật, được gọi từ pipeline/consumer ngoài controller:
  - Inject đủ sub-service (FBUser, FBPermission, FBFanpage, FBPost, FBComment) → parse payload webhook FB thành các entity tương ứng.
  - `UpdateAsync` override — MarkDirty toàn bộ field (IdDonVi, FanPageId, Data, LogMessage, Retry, IsProcessing, IsForwardRequest, IsComplete, ...) → entity là **bản ghi log webhook có cờ retry/processing/complete**.
  - Khi parse được khách hàng → publish event tạo contact qua `IPublishEndpointContactReceiveFB` (cờ `IsUseFBCreateContactByQueue=true`, mặc định đi qua queue).

## Quan hệ
- **Gọi sang:** nhiều sub-service FacebookChat (FBUser/FBPermission/FBFanpage/FBPost/FBComment) + EF.
- **Dữ liệu (entities):** `FBDataWebhook` (Data, LogMessage, Retry, IsProcessing, IsForwardRequest, IsComplete) + các entity FB phái sinh khi parse.
- **Ripple / đồng bộ:** publish RabbitMQ tạo contact (sang Contact service) qua `IPublishEndpointContactReceiveFB`.

## Lỗi đã biết / lưu ý kỹ thuật
- `FBDataWebhookController.cs:13` — `[TienIch(... TI_Facebook)]` bị comment; `:26-111` — mọi action comment → controller rỗng. `deprecated: true`.
- **Trùng tên với service khác:** marketing đã có `FBDataWebhookController_FbWebhook.md` (service `FacebookWebhook`). Node này (`_FacebookChat`) là bản trong solution FacebookChat — service xử lý webhook khác nhau, đừng gộp.
- `IFBDataWebhookService` active (orchestrator có retry + publish contact) dù controller rỗng — điểm nhận webhook thật KHÔNG ở controller này.
