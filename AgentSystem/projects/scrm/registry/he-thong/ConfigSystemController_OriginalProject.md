---
controller: ConfigSystemController
module: he-thong
service: OriginalProject
man_hinh_ui: "nội bộ — Quản trị > Cấu hình hệ thống (ConfigSystem) của service OriginalProject"
muc_dich: "CRUD cấu hình hệ thống (ConfigSystem) cho service OriginalProject: tìm theo mã/Id, lấy tất cả, lấy theo group (có sort), thêm/sửa/xóa, lấy danh sách nhóm cấu hình."
alias: ["Cấu hình hệ thống OriginalProject", "ConfigSystem OriginalProject", "config system"]
auth: internal
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends: ["IConfigSystemService", "AutoMapper (ConfigSystemDto <-> ConfigSystem)"]
cross_module_in: ["Màn hình Quản trị cấu hình hệ thống (UI nội bộ OriginalProject) gọi CRUD"]
cross_module_out: ["Các tính năng OriginalProject đọc ConfigSystem để lấy tham số cấu hình"]
---

# ConfigSystemController (OriginalProject) — Cấu hình hệ thống

CRUD bảng `ConfigSystem` cho service OriginalProject. Tiện ích `TI_HeThong`. Có `[SBAuthorize]` cấp class + key riêng từng action → `internal`. Key đặt theo tiền tố `ConfigSystem_OriginalProject_*` (đúng service, khác bản TrackMap dùng tiền tố Invoice).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm config theo mã | — | GET `api/QuanTriArea/ConfigSystem/TimTheoMa?ma=` | `ConfigSystem_OriginalProject_TimTheoMa` | internal | — | Trả ConfigSystem theo `ma`. | EF (service) | — |
| 2 | Lấy tất cả config | — | GET `.../LayTatCa` | `ConfigSystem_OriginalProject_LayTatCa` | internal | — | Trả toàn bộ ConfigSystem. | EF | — |
| 3 | Lấy theo group + sort | — | POST `.../LayTatCaByGroupSort` | `ConfigSystem_OriginalProject_LayTatCaTheoGroupVaSort` | internal | — | Trả cây config nhóm theo group + sort (FilterCustomDto). | EF | — |
| 4 | Lấy theo group | — | GET `.../LayTatCaByGroup` | `ConfigSystem_OriginalProject_LayTatCaTheoGroup` | internal | — | Trả cây config nhóm theo group. | EF | — |
| 5 | Thêm mới | nút Thêm | POST `.../ThemMoi` | `ConfigSystem_OriginalProject_ThemMoi` | internal | — | Map DTO→entity, set NgayTao/NguoiTao=CurrentUser, Id=new Guid, Insert. ModelState invalid → trả lỗi. | EF | — |
| 6 | Xóa theo Id | nút Xóa | POST `.../Xoa/{id:Guid}` | `ConfigSystem_OriginalProject_XoaConfigSystem` | internal | ONLY_SUPERUSER (theo mô tả key) | Kiểm tra tồn tại → NotFound nếu không → Delete. | EF | Mô tả key ghi "ONLY_SUPERUSER" nhưng code không tự chặn, phụ thuộc hệ thống phân quyền. |
| 7 | Cập nhật theo Id | nút Lưu | POST `.../CapNhatThongTin` | `ConfigSystem_OriginalProject_CapNhatThongTin` | internal | — | Map DTO→entity, kiểm tra tồn tại (404 nếu không), set NguoiCapNhat/NgayCapNhat, Update. | EF | — |
| 8 | Tìm theo Id | — | GET `.../TimTheoId/{id:Guid}` | `ConfigSystem_OriginalProject_TimTheoId` | internal | — | Trả ConfigSystemDto theo Id. | EF | — |
| 9 | Lấy các nhóm cấu hình | — | GET `.../LayCacNhomCauHinh` | `ConfigSystem_OriginalProject_LayCacGroupConfigSystem` | internal | — | Trả danh sách group code. | EF | — |

## Quan hệ
- **Gọi sang:** `IConfigSystemService` (OriginalProject.Service.Generated).
- **Dữ liệu (entities):** `ConfigSystem` (OriginalProject.DataContext.Entity).
- **Ripple / đồng bộ:** Cấu hình thay đổi → tính năng OriginalProject đọc lại ConfigSystem; không có event cross-service.

## Lỗi đã biết / lưu ý kỹ thuật
- `ConfigSystemController.cs:11-16` — bản OriginalProject ĐÚNG tiền tố key theo service (`ConfigSystem_OriginalProject_*`); đối chiếu bản TrackMap dùng tiền tố `ConfigSystem_Invoice_*` → 2 service cùng controller nhưng namespace key khác nhau, cần chú ý khi gán quyền.
