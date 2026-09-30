---
controller: DMLoaiQuyHeThongController
module: he-thong
service: Core
man_hinh_ui: "nội bộ — không có UI (controller rỗng, toàn bộ action đã bị comment)"
muc_dich: "Danh mục Loại quỹ hệ thống — HIỆN VÔ HIỆU: cả constructor (DI service) lẫn 6 action đều bị comment, không có endpoint nào hoạt động. Chỉ còn vỏ class."
alias: ["DMLoaiQuyHeThong", "loại quỹ hệ thống", "danh mục loại quỹ"]
auth: internal
state_machine: []
deprecated: true
entities: [DMLoaiQuyHeThong]
stored_procs: []
depends: ["IDMLoaiQuyHeThongService (DI bị comment → không inject)"]
cross_module_in: []
cross_module_out: []
---

# DMLoaiQuyHeThongController — Danh mục loại quỹ hệ thống (HIỆN VÔ HIỆU)

Controller `Core.API.Controllers`, route `[Route("api/[controller]")]` → `/api/DMLoaiQuyHeThong/...`. Kế thừa `BaseController`. **Toàn bộ nội dung bị comment**: constructor (inject `IDMLoaiQuyHeThongService`) và 6 action (`GetByIdAsync`, `DeleteAsync`, `TimTheoDieuKien`, `CreateAsync`, `UpdateAsync`, `GetListTaiKhoanTheoLoaiQuyChiNhanh`) đều nằm trong comment. Field `_dmLoaiQuyHeThongService` khai báo nhưng không bao giờ được gán (constructor comment) → kể cả khi un-comment action sẽ NRE. Hiện **không expose endpoint nào**.

File: `Core/Core.API/Controllers/DMLoaiQuyHeThongController.cs`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| — | (không có endpoint) | — | — | — | — | — | Controller rỗng, mọi action bị comment → không route nào hoạt động | — | Cả class chỉ còn vỏ; cần un-comment + gán DI mới dùng được |

## Quan hệ
- **Gọi sang:** (thiết kế cũ) `IDMLoaiQuyHeThongService` — hiện không inject (constructor comment).
- **Dữ liệu (entities):** `DMLoaiQuyHeThong` (DTO `DMLoaiQuyHeThongDTO`, info `DMLoaiQuyHeThongInfo`) — chỉ tham chiếu trong code comment.
- **Ripple / đồng bộ:** không có (vô hiệu).

## Lỗi đã biết / lưu ý kỹ thuật
- `DMLoaiQuyHeThongController.cs:17-95` — constructor + toàn bộ 6 action bị comment → controller không có endpoint. Đánh dấu `deprecated`.
- `DMLoaiQuyHeThongController.cs:15` — field `_dmLoaiQuyHeThongService` khai báo nhưng constructor gán đang comment → nếu un-comment action mà quên DI sẽ NRE.
- Chức năng "loại quỹ hệ thống" nếu còn dùng có thể đã chuyển sang controller/service khác — cần trace lại trước khi khôi phục.
