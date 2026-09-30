---
controller: ThongTinChungController
module: he-thong
service: Core
man_hinh_ui: "nội bộ — không có UI (toàn bộ controller bị comment, không expose endpoint)"
muc_dich: "(Đã vô hiệu) Dự kiến CRUD/tìm kiếm danh mục ThongTinChung — hiện toàn bộ thân class bị comment, controller rỗng."
alias: ["ThongTinChung", "thông tin chung"]
auth: internal
state_machine: []
deprecated: true
entities: [ThongTinChung]
stored_procs: []
depends: ["IThongTinChungService", "SBBaseController<ThongTinChung, ThongTinChungDto>"]
cross_module_in: []
cross_module_out: []
---

# ThongTinChungController — (Vô hiệu / không expose)

Toàn bộ thân controller bị comment trong `ThongTinChungController.cs` (dòng 19-47) — không có route, không có `[ApiController]`, không method nào được biên dịch ra endpoint. Class rỗng. AI bỏ qua khi triage: controller này **không hoạt động**.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có) | — | — | — | — | — | Controller bị comment toàn bộ — không expose endpoint. | — | deprecated: true |

## Lỗi đã biết / lưu ý kỹ thuật
- `ThongTinChungController.cs:19-47` toàn bộ định nghĩa class (kế thừa `SBBaseController<ThongTinChung, ThongTinChungDto>`) + endpoint `TimTheoDieuKien` bị comment → không build ra controller chạy được.
- Nếu cần dùng lại: bỏ comment + sửa attribute `[SBAuthorize("ThongTinChung_TimTheoDieuKien", "...",)]` (đang thiếu tham số role nên không compile).
