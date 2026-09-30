# Dev — Quản lý nhân viên xử lý (Cấu hình xếp nhân viên)

> Service: **Order** (BE .NET) · FE module Order (config-order). Chỉ dành cho dev. Trace registry: `registry/cua-hang/CauHinhXepNhanVienController.md`.

## Màn hình & route (FE)
| Màn hình | Route / vị trí | Component |
|---|---|---|
| Tab cấu hình nhân viên (trong popup Auto gán user) | `pages/caidat-hethong/configsystem/config-order/popup-auto-gan-user/tab-config-nv/` | `TabConfigNvComponent` |

- FE service: `services/order/cauhinh-xepnhanvien.ts`.

## Endpoint / Controller (trace registry)
**`CauHinhXepNhanVienController`** (service Order):
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Lấy danh sách cấu hình | POST `api/CauHinhXepNhanVien/LayDSCauHinhXepNhanVien` | (rỗng — chỉ login) |
| Xem chi tiết cấu hình | POST `api/CauHinhXepNhanVien/GetCauHinhXepNhanVien` | `CauHinhXepNhanVien_GetCauHinhXepNhanVien` |
| Lưu cấu hình | POST `api/CauHinhXepNhanVien/SaveCauHinhNhanVien` | `CauHinhXepNhanVien_SaveCauHinhNhanVien` |

## Entities
- **`CauHinhXepNhanVien`** — bản ghi cấu hình quy tắc xếp nhân viên phục vụ đơn (cấu trúc field cụ thể *cần product xác nhận*).

## Luồng code
- 3 hành động đều ủy quyền cho `ICauHinhXepNhanVienService`:
  - `LayDSCauHinhXepNhanVien(reqBody, CurrentUser)` — list theo điều kiện.
  - `GetCauHinhXepNhanVien(reqBody, CurrentUser)` — chi tiết 1 cấu hình.
  - `SaveCauHinhNhanVien(reqBody, CurrentUser)` — lưu cấu hình.
- **Ripple:** cấu hình này quyết định cách gán nhân viên ở bước "Xếp nhân viên" trong luồng tạo/phục vụ đơn POS (ViewXepNv / LuuXepNv) — `cross_module_out` của node.

## Bug-prone / lưu ý
- **`LayDSCauHinhXepNhanVien` chỉ `[SBAuthorize]` rỗng** (chỉ login), trong khi Get/Save có permission key → đọc list không gắn quyền riêng. Cần xác nhận có chủ ý (list cấu hình có lộ thông tin nhạy cảm không).
- Không có endpoint xóa — chỉ Lấy DS / Get / Save.

## File source (path:line)
- BE controller: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\Order\Order.API\Controllers\CauHinhXepNhanVienController.cs`.
- FE service: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\services\order\cauhinh-xepnhanvien.ts`.
- FE page: `...\pages\caidat-hethong\configsystem\config-order\popup-auto-gan-user\tab-config-nv\`.
