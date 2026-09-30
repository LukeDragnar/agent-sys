---
controller: CauHinhController
module: marketing
service: FacebookWebhook
man_hinh_ui: "nội bộ — không có UI rõ (CRUD CauHinh của service FacebookWebhook). Route api/CauHinh."
muc_dich: "CRUD bảng CauHinh nội bộ của service FacebookWebhook: lấy theo id, thêm, sửa, xóa, tìm theo điều kiện (phân trang). Toàn bộ SBAuthorize đang comment → mở (chỉ chặn ở middleware nếu có)."
alias: ["CauHinh FB Webhook", "cấu hình webhook facebook", "CRUD CauHinh"]
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

# CauHinhController — CRUD cấu hình nội bộ (FacebookWebhook)

> Route `api/CauHinh`. Template CRUD scaffold; tất cả `[SBAuthorize(...)]` đang bị comment → endpoint không gắn permission key cụ thể.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Xem chi tiết theo id | (load) | GET `api/CauHinh/TimTheoId/{id}` | (SBAuthorize comment) | public | — | Trả CauHinh theo id. | EF | Không gắn permission (key comment). |
| 2 | Thêm mới | Thêm | POST `api/CauHinh/ThemMoi` | (comment `CauHinh_ThemMoi`) | public | ModelState hợp lệ | Map Dto→entity, insert. | EF | — |
| 3 | Cập nhật | Lưu | POST `api/CauHinh/CapNhatThongTin` | (comment `CauHinh_CapNhatThongTin`) | public | Tồn tại theo id | Map Dto→entity, kiểm tra tồn tại, update; không có → 404. | EF | — |
| 4 | Xóa | Xóa | POST `api/CauHinh/Xoa/{id}` | (comment `CauHinh_Xoa`) | public | Tồn tại theo id | Kiểm tra tồn tại rồi xóa. | EF | — |
| 5 | Tìm theo điều kiện | (grid) | POST `api/CauHinh/TimTheoDieuKien` | — | public | — | Filter/paging theo `CauHinhFilterDto`. | EF | — |

## Quan hệ
- **Gọi sang:** `ICauHinhService`; AutoMapper.
- **Dữ liệu (entities):** `CauHinh` (bảng cấu hình nội bộ service).

## Lỗi đã biết / lưu ý kỹ thuật
- Mọi `[SBAuthorize]` đang comment (`CauHinhController.cs:31,43,61,87`) → endpoint CRUD không có phân quyền ở attribute; nếu service public thì rủi ro sửa/xóa cấu hình tùy tiện.
- Trùng tên với `CauHinhController` các service marketing khác + bản FbChatWebhook → node suffix `_FbWebhook`.
