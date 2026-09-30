---
controller: CauHinhController
module: marketing
service: FacebookChatWebhook
man_hinh_ui: "nội bộ — không có UI rõ (CRUD CauHinh service FacebookChatWebhook). Route api/CauHinh."
muc_dich: "CRUD bảng CauHinh nội bộ của service FacebookChatWebhook: lấy theo id, thêm, sửa, xóa, tìm theo điều kiện. SBAuthorize đang comment."
alias: ["CauHinh FB Chat Webhook", "cấu hình webhook facebook chat"]
auth: public
state_machine: []
deprecated: false
entities: [CauHinh]
stored_procs: []
depends:
  - "ICauHinhService (CRUD + TimTheoDieuKienAsync)"
  - "AutoMapperConfiguration.Mapper — CauHinhDto ↔ CauHinh"
cross_module_in: []
cross_module_out: []
---

# CauHinhController — CRUD cấu hình nội bộ (FacebookChatWebhook)

> Route `api/CauHinh`. Template CRUD; mọi `[SBAuthorize]` comment. Mã giống bản FacebookWebhook.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Xem chi tiết theo id | (load) | GET `api/CauHinh/TimTheoId/{id}` | (comment) | public | — | Trả CauHinh theo id. | EF | Không gắn permission. |
| 2 | Thêm mới | Thêm | POST `api/CauHinh/ThemMoi` | (comment `CauHinh_ThemMoi`) | public | ModelState hợp lệ | Map Dto→entity, insert. | EF | — |
| 3 | Cập nhật | Lưu | POST `api/CauHinh/CapNhatThongTin` | (comment) | public | Tồn tại theo id | Map+kiểm tra tồn tại, update; không có → 404. | EF | — |
| 4 | Xóa | Xóa | POST `api/CauHinh/Xoa/{id}` | (comment) | public | Tồn tại theo id | Kiểm tra rồi xóa. | EF | — |
| 5 | Tìm theo điều kiện | (grid) | POST `api/CauHinh/TimTheoDieuKien` | — | public | — | Filter theo `CauHinhFilterDto`. | EF | — |

## Quan hệ
- **Gọi sang:** `ICauHinhService`; AutoMapper.
- **Dữ liệu (entities):** `CauHinh`.

## Lỗi đã biết / lưu ý kỹ thuật
- Mọi `[SBAuthorize]` comment (`CauHinhController.cs:31,43,61,87`) → CRUD không phân quyền attribute.
- Trùng tên với bản FbWebhook → node suffix `_FbChatWebhook`.
