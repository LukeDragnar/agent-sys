---
controller: ConfigSystemController
module: sales
service: Switchboard
man_hinh_ui: "Quản trị hệ thống (QuanTriArea) > Config System tổng đài (CRUD cấu hình hệ thống tổng đài, theo nhóm/group)"
muc_dich: "CRUD cấu hình hệ thống (ConfigSystem) cấp quản trị của service Switchboard — khu vực QuanTriArea. (Trùng tên ConfigSystemController của Contact → node này hậu tố _Switchboard)."
alias: ["config system tổng đài", "cấu hình hệ thống tổng đài", "ConfigSystem Switchboard"]
auth: internal
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends: [IConfigSystemService, "AutoMapper"]
cross_module_in: []
cross_module_out: []
---

# ConfigSystem (Switchboard) — Cấu hình hệ thống tổng đài (QuanTriArea)

> ⚠️ Trùng tên `ConfigSystemController` với module Contact. Node này = bản Switchboard (`Areas\SandBoxArea\ConfigSystemController.cs`, route `api/QuanTriArea/ConfigSystem`, `TI_HeThong`) → hậu tố `_Switchboard`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm config theo mã | (load) | GET `TimTheoMa` | `ConfigSystem_TongDai_TimTheoMa` | internal | màn quản trị config | Trả config theo `ma`. | EF | — |
| 2 | Lấy tất cả / theo group / theo group+sort | (load) | GET `LayTatCa`, GET `LayTatCaByGroup`, POST `LayTatCaByGroupSort`, GET `LayCacNhomCauHinh` | `_LayTatCa` / `_LayTatCaTheoGroup` / `_LayTatCaTheoGroupVaSort` / `_LayCacGroupConfigSystem` | internal | màn quản trị | Trả toàn bộ config / dạng cây theo group / group+sort / danh sách group code. | EF | — |
| 3 | Thêm mới config | Thêm | POST `ThemMoi` | `ConfigSystem_TongDai_ThemMoi` | internal | màn quản trị | ModelState invalid → lỗi; hợp lệ → map DTO→entity, set NgàyTạo/người tạo/Id mới → insert. | EF + AutoMapper | — |
| 4 | Cập nhật config | Lưu | POST `CapNhatThongTin` | `ConfigSystem_TongDai_CapNhatThongTin` | internal | dòng config | Invalid → lỗi; không tìm thấy id → 404; hợp lệ → set người/ngày cập nhật → update. | EF + AutoMapper | — |
| 5 | Xóa config | Xóa | POST `Xoa/{id}` | `ConfigSystem_TongDai_XoaConfigSystem` (ONLY_SUPERUSER) | internal | dòng config | Không tìm thấy → lỗi NotFound; có → xóa. | EF | Mô tả key ghi "ONLY_SUPERUSER" |
| 6 | Lấy 1 config theo id | (load) | GET `TimTheoId/{id}` | `ConfigSystem_TongDai_TimTheoId` | internal | mở chi tiết | Trả config theo id (map sang DTO). | EF + AutoMapper | — |

## Quan hệ
- **Gọi sang:** `IConfigSystemService` (CRUD config), AutoMapper (DTO ↔ entity).
- **Dữ liệu (entities):** `ConfigSystem` (cấu hình hệ thống tổng đài).
- **Ripple / đồng bộ:** không (config nội bộ).

## Lỗi đã biết / lưu ý kỹ thuật
- Class-level `[SBAuthorize]` + key per-action → toàn bộ là quản trị hệ thống.
- `ConfigSystemController.cs (Switchboard):cuối` — endpoint `GetConfigSystemByCodeAsync` đã comment.
