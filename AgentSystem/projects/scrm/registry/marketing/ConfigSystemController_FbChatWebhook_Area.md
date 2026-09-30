---
controller: ConfigSystemController
module: marketing
service: FacebookChatWebhook
man_hinh_ui: "Quản trị hệ thống (Area QuanTri) > Quản lý cấu hình. Route api/QuanTriArea/ConfigSystem (Area SanboxArea của FacebookChatWebhookProcess.API)."
muc_dich: "CRUD cấu hình hệ thống (ConfigSystem) service FacebookChatWebhook qua Area QuanTri: lấy theo mã/id/group, dạng cây + sort, thêm/sửa/xóa, group code, value theo code. Có SBAuthorize + tiện ích TI_HeThong."
alias: ["ConfigSystem QuanTri FB Chat Webhook", "cấu hình hệ thống webhook facebook chat"]
auth: public
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends:
  - "IConfigSystemService (CRUD + GetLikeTree/GetLikeTreeSort/GetGroupCode/GetValueByCode)"
  - "AutoMapperConfiguration.Mapper — ConfigSystemDto ↔ ConfigSystem"
  - "MaTienIchSCRM.TI_HeThong — TienIch gate (param4=true)"
cross_module_in: []
cross_module_out: []
---

# ConfigSystemController (Area QuanTri) — Cấu hình hệ thống FacebookChatWebhook

> Route `api/QuanTriArea/ConfigSystem`. `[SBAuthorize]` + `[TienIch(..., TI_HeThong, true)]`. Mã giống hệt bản FacebookWebhook Area (chỉ khác namespace/service).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy config theo mã | (tra cứu) | GET `.../TimTheoMa?ma=` | (SBAuthorize comment) | public | — | Trả config theo mã. | EF | Key cụ thể comment (`cs:35`). |
| 2 | Lấy tất cả / theo id / value theo code | (load) | GET `.../LayTatCa` · `.../TimTheoId/{id}` · `.../GetConfigSystem/{code}` | `ConfigSystem_LayTatCa` · `ConfigSystem_TimTheoId` · `[SBAuthorize]` | public | — | Lấy toàn bộ / 1 config theo id / value theo code. | EF | — |
| 3 | Lấy theo nhóm | (cây) | GET `.../LayTatCaByGroup` · `.../LayCacNhomCauHinh` · POST `.../LayTatCaByGroupSort` | `ConfigSystem_LayTatCaTheoGroup` · `ConfigSystem_LayCacGroupConfigSystem` · `ConfigSystem_LayTatCaTheoGroupVaSort` | public | — | Config dạng cây theo group, có sort; danh sách group code. | EF | — |
| 4 | Thêm config | Thêm mới | POST `.../ThemMoi` | `ConfigSystem_ThemMoi` | public | ModelState hợp lệ | Map Dto→entity, set NgayTao/NguoiTao/Id mới, insert. | EF | Audit thủ công (`cs:97-100`). |
| 5 | Sửa config | Cập nhật | POST `.../CapNhatThongTin` | `ConfigSystem_CapNhatThongTin` | public | Tồn tại theo id | Map+kiểm tra tồn tại (không có → 404), set NguoiCapNhat/NgayCapNhat, update. | EF | Audit thủ công (`cs:140-142`). |
| 6 | Xóa config | Xóa | POST `.../Xoa/{id}` | `ConfigSystem_XoaConfigSystem` (ONLY_SUPERUSER) | public | Tồn tại theo id | Kiểm tra tồn tại rồi xóa. | EF | Mô tả ONLY_SUPERUSER nhưng key không thể hiện (`cs:111`). |

## Quan hệ
- **Gọi sang:** `IConfigSystemService`; AutoMapper.
- **Dữ liệu (entities):** `ConfigSystem`.

## Lỗi đã biết / lưu ý kỹ thuật
- Audit set thủ công ở controller (`cs:97-100,140-142`).
- `TimTheoMa` key SBAuthorize comment → chỉ chặn controller-level.
- Nhiều phía trùng tên → node này suffix `_FbChatWebhook_Area`.
