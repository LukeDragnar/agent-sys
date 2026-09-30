---
controller: ConfigSystemController
module: marketing
service: FacebookWebhook
man_hinh_ui: "Quản trị hệ thống (Area QuanTri) > Quản lý cấu hình. Route api/QuanTriArea/ConfigSystem (Area SanboxArea của FacebookWebhookProcess.API)."
muc_dich: "CRUD cấu hình hệ thống (ConfigSystem) của service FacebookWebhook qua Area QuanTri: lấy theo mã/id/group, dạng cây + sort, thêm/sửa/xóa, lấy group code, lấy value theo code. Có phân quyền SBAuthorize + tiện ích TI_HeThong."
alias: ["ConfigSystem QuanTri FB Webhook", "cấu hình hệ thống webhook facebook", "quanLyCauHinh"]
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

# ConfigSystemController (Area QuanTri) — Cấu hình hệ thống FacebookWebhook

> Route `api/QuanTriArea/ConfigSystem`. Controller-level `[SBAuthorize]` + `[TienIch("ConfigSystemController","Quản lý ConfigSystem", TI_HeThong, true)]`. Bản giàu endpoint (khác bản Controllers rỗng).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy config theo mã | (tra cứu) | GET `.../TimTheoMa?ma=` | (SBAuthorize comment, mô tả "ALL") | public | — | Trả config theo mã. | EF | Key cụ thể comment (`cs:35`). |
| 2 | Lấy tất cả / theo id / value theo code | (load) | GET `.../LayTatCa` · `.../TimTheoId/{id}` · `.../GetConfigSystem/{code}` | `ConfigSystem_LayTatCa` · `ConfigSystem_TimTheoId` · `[SBAuthorize]` | public | — | Lấy toàn bộ / 1 config theo id (map Dto) / value theo code. | EF | — |
| 3 | Lấy theo nhóm | (cây) | GET `.../LayTatCaByGroup` · `.../LayCacNhomCauHinh` · POST `.../LayTatCaByGroupSort` | `ConfigSystem_LayTatCaTheoGroup` · `ConfigSystem_LayCacGroupConfigSystem` · `ConfigSystem_LayTatCaTheoGroupVaSort` | public | — | Lấy config dạng cây theo group, có sort; lấy danh sách group code. | EF | — |
| 4 | Thêm config | Thêm mới | POST `.../ThemMoi` | `ConfigSystem_ThemMoi` | public | ModelState hợp lệ | Map Dto→entity, set NgayTao/NguoiTao(Id,UserName)/Id mới, insert. | EF | Audit set thủ công ở controller (`cs:97-100`). |
| 5 | Sửa config | Cập nhật | POST `.../CapNhatThongTin` | `ConfigSystem_CapNhatThongTin` | public | Tồn tại theo id | Map Dto→entity, kiểm tra tồn tại (không có → 404), set NguoiCapNhat/NgayCapNhat, update. | EF | Audit thủ công (`cs:140-142`). |
| 6 | Xóa config | Xóa | POST `.../Xoa/{id}` | `ConfigSystem_XoaConfigSystem` (mô tả ONLY_SUPERUSER) | public | Tồn tại theo id | Kiểm tra tồn tại rồi xóa. | EF | Mô tả "ONLY_SUPERUSER" nhưng key không thể hiện chặn superuser (`cs:111`). |

## Quan hệ
- **Gọi sang:** `IConfigSystemService`; AutoMapper.
- **Dữ liệu (entities):** `ConfigSystem` (mã-giá trị, có group/sort).

## Lỗi đã biết / lưu ý kỹ thuật
- Audit fields set thủ công ở controller thay vì service (`cs:97-100,140-142`).
- `TimTheoMa` key SBAuthorize bị comment → chỉ chặn ở `[SBAuthorize]` controller-level (login), không gắn permission riêng.
- 3 phía trùng tên: bản Controllers rỗng cùng service (`_FbWebhook`), bản SMS, bản FbChatWebhook Area → node này suffix `_FbWebhook_Area`.
