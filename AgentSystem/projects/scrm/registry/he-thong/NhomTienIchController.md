---
controller: NhomTienIchController
module: he-thong
service: Core
man_hinh_ui: "nội bộ — không có UI riêng (cung cấp danh sách nhóm tiện ích cho dropdown phân quyền/cấu hình gói)"
muc_dich: "Chỉ phơi đọc danh sách nhóm tiện ích (LayTatCa) cho client. Phần CRUD nhóm tiện ích nằm ở bản QuanTriArea (_Core)."
alias: ["nhóm tiện ích", "nhóm chức năng", "NhomTienIch"]
auth: public
state_machine: []
deprecated: false
entities: [NhomTienIch]
stored_procs: []
depends: ["INhomTienIchService", "BaseController"]
cross_module_in: ["Màn phân quyền/cấu hình gói gọi LayTatCa để lấy danh sách nhóm tiện ích"]
cross_module_out: []
---

# NhomTienIchController — Nhóm tiện ích (đọc)

Bản `Controllers/` (partial class), kế thừa `BaseController`. Controller-level `[SBAuthorize]` + `[TienIch(TI_Chung)]`. Chỉ 1 endpoint đọc; CRUD đầy đủ ở bản QuanTriArea (`NhomTienIchController_Core`).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy tất cả | (dropdown) | GET `api/NhomTienIch/LayTatCa` | — (login, `[SBAuthorize]` rỗng) | public | — | `GetAllAsync()` trả tất cả nhóm tiện ích. | EF | — |

## Quan hệ
- **Gọi sang:** INhomTienIchService (EF).
- **Dữ liệu (entities):** `NhomTienIch`.
- **Ripple / đồng bộ:** chỉ đọc.

## Lỗi đã biết / lưu ý kỹ thuật
- `partial class` — CRUD (TimTheoDieuKien/ThemMoi/CapNhat/Xoa) nằm ở bản QuanTriArea, KHÔNG ở bản này; bản này chỉ đọc.
