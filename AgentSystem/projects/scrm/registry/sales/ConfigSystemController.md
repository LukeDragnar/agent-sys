---
controller: ConfigSystemController
module: sales
service: Contact
man_hinh_ui: "nội bộ — quản trị (QuanTriArea), route api/QuanTriArea/ConfigSystem (cấu hình hệ thống, SanboxArea)"
muc_dich: "CRUD cấu hình hệ thống (ConfigSystem) cấp quản trị/hệ thống của module Contact — khu vực admin SanboxArea (QuanTriArea)."
alias: [ConfigSystem, "cấu hình hệ thống", "config system"]
auth: public
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends: [ConfigSystemService]
cross_module_in: []
cross_module_out: []
---

# ConfigSystem — Cấu hình hệ thống (QuanTriArea)

> Controller admin trong `Areas/SanboxArea`, route `api/QuanTriArea/[controller]`. `TienIch(..., MaTienIchSCRM.TI_HeThong, true)` → thuộc tiện ích Hệ thống, cờ admin = true.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Thêm cấu hình | Lưu | POST `api/QuanTriArea/ConfigSystem/InsertConfig` | `[SBAuthorize()]` (login) | public | màn quản trị | Insert 1 ConfigSystem. | EF | Key rỗng (khu quản trị) |
| 2 | Cập nhật cấu hình | Lưu | POST `.../UpdateConfig` | `[SBAuthorize()]` (login) | public | màn quản trị | Update ConfigSystem. | EF | Key rỗng |
| 3 | Tìm theo điều kiện | Danh sách | POST `.../TimTheoDieuKien` | `[SBAuthorize()]` (login) | public | grid cấu hình | Trả danh sách ConfigSystem theo filter. | EF | Key rỗng |
| 4 | Chi tiết theo id | Mở | GET `.../GetDetail?id=` | `[SBAuthorize()]` (login) | public | mở dòng | Trả chi tiết ConfigSystem theo id. | EF | Key rỗng |
| 5 | Xóa cấu hình | Xóa | POST `.../DeleteConfig` | `[SBAuthorize()]` (login) | public | dòng cấu hình | Xóa ConfigSystem theo id (POST). | EF | Key rỗng |

## Quan hệ
- **Gọi sang:** `ConfigSystemService`.
- **Dữ liệu (entities):** ConfigSystem (cấu hình hệ thống dạng key-value cấp hệ thống).
- **Ripple / đồng bộ:** cấu hình hệ thống có thể ảnh hưởng nhiều luồng — thay đổi cần thận trọng (khu quản trị).

## Lỗi đã biết / lưu ý kỹ thuật
- `ConfigSystemController.cs:27,39,52,65,78` — toàn bộ 5 endpoint dùng `[SBAuthorize()]` **rỗng** (chỉ cần login), không có permission key → chỉ dựa vào việc route nằm trong QuanTriArea/SanboxArea để hạn chế. Cấu hình cấp hệ thống nhưng phân quyền lỏng → rủi ro nếu route lộ ra ngoài.
