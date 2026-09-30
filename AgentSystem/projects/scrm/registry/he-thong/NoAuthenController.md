---
controller: NoAuthenController
module: he-thong
service: Core
man_hinh_ui: "nội bộ — không có UI; placeholder controller, toàn bộ endpoint đang bị comment"
muc_dich: "Controller dự kiến chứa các endpoint không cần xác thực (no-auth) liên quan ConfigSystem, nhưng hiện TẤT CẢ action đều bị comment → không phơi endpoint nào."
alias: ["no authen", "không xác thực", "NoAuthen"]
auth: anonymous
state_machine: []
deprecated: true
entities: []
stored_procs: []
depends: ["IConfigSystemService"]
cross_module_in: []
cross_module_out: []
---

# NoAuthenController — (rỗng / placeholder)

Kế thừa `BaseController`, inject `IConfigSystemService` nhưng KHÔNG có route attribute ở controller-level và **toàn bộ method đều bị comment**. Hiện không phơi endpoint hoạt động nào.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | (không có) | — | — | — | anonymous | — | Không có endpoint hoạt động — tất cả action bị comment. | — | Toàn bộ controller là placeholder |

## Quan hệ
- **Gọi sang:** IConfigSystemService (inject nhưng chưa dùng).
- **Dữ liệu (entities):** không.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- `NoAuthenController.cs` — tất cả method (LayTatCaAdDlTheoListIdDonVi, TimTheoMa, ...) bị comment (dòng 15–38); controller không có `[Route]`/`[ApiController]` → coi như deprecated/placeholder, AI bỏ qua.
