---
controller: ServerController
module: marketing
service: FacebookChatWebhook
man_hinh_ui: "Quản trị hệ thống (Area QuanTri) > Quản lý server webhook. Route api/QuanTriArea/Server (Area SanboxArea)."
muc_dich: "CRUD danh mục Server (cụm server/queue webhook) cho service chat: xem theo id, thêm, sửa, xóa, tìm theo điều kiện. Thêm/xóa thì clear cache Server hệ thống."
alias: ["Server QuanTri FB Chat Webhook", "quản lý server webhook chat"]
auth: public
state_machine: []
deprecated: false
entities: [Server]
stored_procs: []
depends:
  - "IServerService (CRUD)"
  - "ICommonCacheService.ClearCache_Server_System — clear cache sau insert/delete"
  - "IRedisCacheService (inject, không dùng trực tiếp)"
  - "AutoMapperConfiguration.Mapper — ServerDto ↔ Server"
  - "MaTienIchSCRM.TI_HeThong — TienIch gate (param4=true)"
cross_module_in: []
cross_module_out:
  - "Redis cache 'Server hệ thống' — invalidate khi thêm/xóa server"
---

# ServerController (Area QuanTri) — Quản lý server webhook (chat)

> Route `api/QuanTriArea/Server`. `[SBAuthorize]` + `[TienIch(..., TI_HeThong, true)]`. Mã giống hệt bản FacebookWebhook Area.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Xem chi tiết | (load) | GET `.../TimTheoId/{id}` | (controller-level) | public | — | Trả Server theo id. | EF | — |
| 2 | Thêm server | Thêm | POST `.../InsertAsync` | (controller-level) | public | ModelState hợp lệ | Map Dto→entity, insert, `ClearCache_Server_System()`. | EF | — |
| 3 | Sửa server | Lưu | POST `.../UpdateOneAsync` | (controller-level) | public | Tồn tại theo id | Map+kiểm tra tồn tại (không có → 404), update. | EF | Không clear cache sau update (`cs:71-89`). |
| 4 | Xóa server | Xóa | POST `.../DeleteOneItemAsync/{id}` | (controller-level) | public | Tồn tại theo id | Kiểm tra tồn tại, xóa, `ClearCache_Server_System()`. | EF | — |
| 5 | Tìm theo điều kiện | (grid) | POST `.../TimTheoDieuKienAsync` | (controller-level) | public | — | Filter theo `ServerFilterDto`. | EF | — |

## Quan hệ
- **Gọi sang:** `IServerService`; `ICommonCacheService.ClearCache_Server_System`; AutoMapper.
- **Dữ liệu (entities):** `Server`.
- **Ripple:** thêm/xóa server → clear Redis cache "Server hệ thống".

## Lỗi đã biết / lưu ý kỹ thuật
- `UpdateOneAsync` không clear cache (chỉ Insert/Delete clear) (`ServerController.cs:71-89`).
- `IRedisCacheService` inject không dùng trực tiếp.
- Nhiều phía trùng tên → node suffix `_FbChatWebhook_Area`.
