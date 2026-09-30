---
controller: ConfigSystemController
module: he-thong
service: TrackMap
man_hinh_ui: "nội bộ — Quản trị > Cấu hình hệ thống (ConfigSystem) của service TrackMap"
muc_dich: "CRUD cấu hình hệ thống (ConfigSystem) cho service TrackMap: tìm theo mã/Id, lấy tất cả, lấy theo group (có sort), thêm/sửa/xóa, lấy danh sách nhóm cấu hình."
alias: ["Cấu hình hệ thống TrackMap", "ConfigSystem TrackMap", "config system"]
auth: internal
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends: ["IConfigSystemService", "AutoMapper (ConfigSystemDto <-> ConfigSystem)"]
cross_module_in: ["Màn hình Quản trị cấu hình hệ thống (UI nội bộ TrackMap) gọi CRUD"]
cross_module_out: ["Các tính năng TrackMap đọc ConfigSystem để lấy tham số cấu hình"]
---

# ConfigSystemController (TrackMap) — Cấu hình hệ thống

CRUD bảng `ConfigSystem` cho service TrackMap. Tiện ích `TI_HeThong`. Controller KHÔNG có `[SBAuthorize]` ở cấp class (khác bản OriginalProject), chỉ gắn key ở từng action → mọi action vẫn yêu cầu key riêng → coi là `internal`. Lưu ý: tiền tố key là `ConfigSystem_Invoice_*` (dùng lại key của Invoice, KHÔNG đặt theo service TrackMap) → dễ nhầm khi phân quyền.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm config theo mã | — | GET `api/QuanTriArea/ConfigSystem/TimTheoMa?ma=` | `ConfigSystem_Invoice_TimTheoMa` | internal | — | Trả ConfigSystem theo `ma`. | EF (service) | — |
| 2 | Lấy tất cả config | — | GET `.../LayTatCa` | `ConfigSystem_Invoice_LayTatCa` | internal | — | Trả toàn bộ ConfigSystem. | EF | — |
| 3 | Lấy theo group + sort | — | POST `.../LayTatCaByGroupSort` | `ConfigSystem_Invoice_LayTatCaTheoGroupVaSort` | internal | — | Trả cây config nhóm theo group + sort (FilterCustomDto). | EF | — |
| 4 | Lấy theo group | — | GET `.../LayTatCaByGroup` | `ConfigSystem_Invoice_LayTatCaTheoGroup` | internal | — | Trả cây config nhóm theo group. | EF | — |
| 5 | Thêm mới | nút Thêm | POST `.../ThemMoi` | `ConfigSystem_Invoice_ThemMoi` | internal | — | Map DTO→entity, set NgayTao/NguoiTao=CurrentUser, Id=new Guid, Insert. ModelState invalid → trả lỗi. | EF | — |
| 6 | Xóa theo Id | nút Xóa | POST `.../Xoa/{id:Guid}` | `ConfigSystem_Invoice_XoaConfigSystem` | internal | ONLY_SUPERUSER (theo mô tả key) | Kiểm tra tồn tại → không có thì báo NotFound → Delete. | EF | Mô tả key ghi "ONLY_SUPERUSER" nhưng gating thực tế do hệ thống phân quyền, code không tự chặn. |
| 7 | Cập nhật theo Id | nút Lưu | POST `.../CapNhatThongTin` | `ConfigSystem_Invoice_CapNhatThongTin` | internal | — | Map DTO→entity, kiểm tra tồn tại (404 nếu không), set NguoiCapNhat/NgayCapNhat, Update. | EF | — |
| 8 | Tìm theo Id | — | GET `.../TimTheoId/{id:Guid}` | `ConfigSystem_Invoice_TimTheoId` | internal | — | Trả ConfigSystemDto theo Id. | EF | — |
| 9 | Lấy các nhóm cấu hình | — | GET `.../LayCacNhomCauHinh` | `ConfigSystem_Invoice_LayCacGroupConfigSystem` | internal | — | Trả danh sách group code. | EF | — |

## Quan hệ
- **Gọi sang:** `IConfigSystemService` (TrackMap.Service.Generated).
- **Dữ liệu (entities):** `ConfigSystem` (TrackMap.DataContext.Entity).
- **Ripple / đồng bộ:** Cấu hình thay đổi → các tính năng TrackMap đọc lại ConfigSystem; không có event cross-service.

## Lỗi đã biết / lưu ý kỹ thuật
- `ConfigSystemController.cs:22-118` — tất cả key mang tiền tố `ConfigSystem_Invoice_*` (dùng chung namespace key với module Invoice) trong khi controller thuộc service TrackMap → rủi ro nhầm phân quyền giữa các service.
- `ConfigSystemController.cs:11-14` — không có `[SBAuthorize]` cấp class (chỉ ở action); nếu action nào quên gắn key sẽ thành anonymous.
