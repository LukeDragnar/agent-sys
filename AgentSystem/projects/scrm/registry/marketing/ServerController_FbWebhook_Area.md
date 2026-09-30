---
controller: ServerController
module: marketing
service: FacebookWebhook
man_hinh_ui: "Quản trị hệ thống (Area QuanTri) > Quản lý server webhook. Route api/QuanTriArea/Server (Area SanboxArea)."
muc_dich: "CRUD danh mục Server (cụm server/queue webhook): xem theo id, thêm, sửa, xóa, tìm theo điều kiện. Khi thêm/xóa thì clear cache Server hệ thống. Bản giàu endpoint (khác bản Controllers scaffold)."
alias: ["Server QuanTri FB Webhook", "quản lý server webhook", "server rabbit"]
auth: public
state_machine: []
deprecated: false
entities: [Server]
stored_procs: []
depends:
  - "IServerService (CRUD)"
  - "ICommonCacheService.ClearCache_Server_System — clear cache sau insert/delete"
  - "IRedisCacheService (inject, không dùng trực tiếp ở đây)"
  - "AutoMapperConfiguration.Mapper — ServerDto ↔ Server"
  - "MaTienIchSCRM.TI_HeThong — TienIch gate (param4=true)"
cross_module_in: []
cross_module_out:
  - "Redis cache 'Server hệ thống' — invalidate khi thêm/xóa server"
---

# ServerController (Area QuanTri) — Quản lý server webhook

> Route `api/QuanTriArea/Server`. `[SBAuthorize]` + `[TienIch("ServerController","Quản lý server webhook", TI_HeThong, true)]`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Xem chi tiết | (load) | GET `.../TimTheoId/{id}` | (SBAuthorize controller-level) | public | — | Trả Server theo id. | EF | Key riêng comment. |
| 2 | Thêm server | Thêm | POST `.../InsertAsync` | (controller-level) | public | ModelState hợp lệ | Map Dto→entity, insert, rồi `ClearCache_Server_System()`. | EF | — |
| 3 | Sửa server | Lưu | POST `.../UpdateOneAsync` | (controller-level) | public | Tồn tại theo id | Map Dto→entity, kiểm tra tồn tại (không có → 404), update. | EF | Không clear cache sau update (chỉ insert/delete clear) (`cs:71-89`). |
| 4 | Xóa server | Xóa | POST `.../DeleteOneItemAsync/{id}` | (controller-level) | public | Tồn tại theo id | Kiểm tra tồn tại, xóa, `ClearCache_Server_System()`. | EF | — |
| 5 | Tìm theo điều kiện | (grid) | POST `.../TimTheoDieuKienAsync` | (controller-level) | public | — | Filter theo `ServerFilterDto`. | EF | — |

## Quan hệ
- **Gọi sang:** `IServerService`; `ICommonCacheService.ClearCache_Server_System`; AutoMapper.
- **Dữ liệu (entities):** `Server` — định nghĩa cụm server/queue webhook.
- **Ripple:** thêm/xóa server → clear Redis cache "Server hệ thống" để route webhook cập nhật.

## Lỗi đã biết / lưu ý kỹ thuật
- `UpdateOneAsync` KHÔNG gọi clear cache (chỉ Insert/Delete clear) → sau sửa server, cache cũ có thể còn dùng cho định tuyến (`ServerController.cs:71-89`).
- `IRedisCacheService` inject nhưng không dùng trực tiếp trong file.
- 3 phía trùng tên (bản Controllers scaffold cùng service + bản FbChatWebhook Area) → node suffix `_FbWebhook_Area`.
