---
controller: ThongBaoNguoiNhanController
module: he-thong
service: Notification
man_hinh_ui: "nội bộ — không có UI (toàn bộ body controller đang bị comment)"
muc_dich: "Controller (bản nháp) cho bảng map người-nhận của thông báo; hiện toàn bộ code bị comment nên không phát endpoint nào."
alias: ["ThongBaoNguoiNhan", "người nhận thông báo"]
auth: internal
state_machine: []
deprecated: true
entities: [ThongBaoNguoiNhan]
stored_procs: []
depends: ["NotificationDataContext (SQL)"]
cross_module_in: []
cross_module_out: []
---

# ThongBaoNguoiNhanController — (deprecated) map người nhận thông báo

Controller rỗng: class + constructor + action `TimTheoDieuKien` đều bị comment toàn bộ (`Controllers/ThongBaoNguoiNhanController.cs`). Không có route/endpoint nào hoạt động.

Nghiệp vụ người-nhận thông báo thực tế được xử lý gộp trong `ThongBaoController` (he-thong) qua entity `ThongBaoNguoiNhan` (chuỗi ChiNhanhds/IdDonVis/Roles, ghi/xóa trong transaction khi thêm/sửa thông báo). Node này chỉ giữ chỗ để biết file controller tồn tại nhưng đã khóa.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có) | — | — | — | — | — | Controller bị comment toàn bộ, không expose endpoint. | — | Code dead, dùng `ThongBaoController` thay thế |

## Lỗi đã biết / lưu ý kỹ thuật
- Toàn bộ thân class bị comment: `Notification/Notification.API/Controllers/ThongBaoNguoiNhanController.cs:11-35`.
