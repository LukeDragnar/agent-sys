---
controller: ConfigSystemController
module: kho
service: OrderLogistic
man_hinh_ui: "Khu vực Quản trị hệ thống (HT) > Cấu hình hệ thống OrderLogistic — CRUD config key-value theo group (cây cấu hình hệ thống); chỉ HT/super dùng"
muc_dich: "CRUD cấu hình hệ thống dạng key-value/group (ConfigSystem) của service OrderLogistic: tìm theo mã/Id, lấy tất cả / theo group (dạng cây + sort), thêm/sửa/xóa config; danh mục cấu hình hệ thống cấp quản trị."
alias: ["config system order logistic", "cấu hình hệ thống order", "config key-value order logistic", "cấu hình group order"]
auth: internal
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends:
  - "IConfigSystemService (OrderLogistic.Service/Generated)"
cross_module_in:
  - "FE Quản trị HT — màn cấu hình hệ thống OrderLogistic"
cross_module_out: []
---

# ConfigSystemController (OrderLogistic) — Cấu hình hệ thống (QuanTriArea)

> **Area:** `OrderLogistic.API.Areas.SandboxArea` — route prefix **`api/QuanTriArea/ConfigSystem`**. `[TienIchAttribute("ConfigSystemController", "Cấu hình hệ thống", TI_HeThong, true)]` → tiện ích hệ thống. **Trùng tên** controller module khác → node suffix `_OrderLogistic`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm config theo mã | filter mã | GET `api/QuanTriArea/ConfigSystem/TimTheoMa?ma=` | `ConfigSystem_OrderLogistic_TimTheoMa` | internal | - | Trả config theo `ma` | EF | - |
| 2 | Lấy tất cả config | grid | GET `api/QuanTriArea/ConfigSystem/LayTatCa` | `ConfigSystem_OrderLogistic_LayTatCa` | internal | - | Trả toàn bộ config | EF | - |
| 3 | Lấy theo group + sort | cây cấu hình | POST `api/QuanTriArea/ConfigSystem/LayTatCaByGroupSort` | `ConfigSystem_OrderLogistic_LayTatCaTheoGroupVaSort` | internal | - | Trả config dạng cây group + sort (`GetLikeTreeSort` theo filter) | EF | - |
| 4 | Lấy theo group | cây cấu hình | GET `api/QuanTriArea/ConfigSystem/LayTatCaByGroup` | `ConfigSystem_OrderLogistic_LayTatCaTheoGroup` | internal | - | Trả config dạng cây group (`GetLikeTree`) | EF | - |
| 5 | Thêm mới config | "Thêm mới" | POST `api/QuanTriArea/ConfigSystem/ThemMoi` | `ConfigSystem_OrderLogistic_ThemMoi` | internal | - | Validate → map DTO→entity → set NgayTao/NguoiTao/Id mới → insert | EF | - |
| 6 | Xóa config | "Xóa" | POST `api/QuanTriArea/ConfigSystem/Xoa/{id}` | `ConfigSystem_OrderLogistic_XoaConfigSystem` | internal | item tồn tại (ONLY_SUPERUSER) | GetById → null trả MESS_NotFound; Delete | EF | Mô tả quyền ghi "ONLY_SUPERUSER" nhưng controller KHÔNG enforce SuperUser trong code — chỉ dựa permission key (ConfigSystemController.cs:73) |
| 7 | Cập nhật config | "Lưu" | POST `api/QuanTriArea/ConfigSystem/CapNhatThongTin` | `ConfigSystem_OrderLogistic_CapNhatThongTin` | internal | item tồn tại | Validate → map → GetById check null (trả 404 nếu thiếu) → set NguoiCapNhat/NgayCapNhat → update | EF | - |
| 8 | Lấy config theo Id | mở item | GET `api/QuanTriArea/ConfigSystem/TimTheoId/{id}` | `ConfigSystem_OrderLogistic_TimTheoId` | internal | - | Trả config theo Id (map entity→DTO) | EF | - |
| 9 | Lấy các group config | dropdown group | GET `api/QuanTriArea/ConfigSystem/LayCacNhomCauHinh` | `ConfigSystem_OrderLogistic_LayCacGroupConfigSystem` | internal | - | Trả danh sách group code (`GetGroupCode`) | EF | - |

## Quan hệ
- **Gọi sang:** `IConfigSystemService` (TimTheoMa, GetAll, GetLikeTree/GetLikeTreeSort, Insert/Update/Delete, GetByIdAsync, GetGroupCode).
- **Dữ liệu (entities):** `ConfigSystem` (key-value + group/sort; field audit NgayTao/NguoiTao, NgayCapNhat/NguoiCapNhat).
- **Ripple / đồng bộ:** không có ripple cross-module; config dùng nội bộ service OrderLogistic.

## Lỗi đã biết / lưu ý kỹ thuật
- Action `Xoa` mô tả permission "ONLY_SUPERUSER" nhưng **không check SuperUser trong code** (ConfigSystemController.cs:73) — chỉ dựa vào việc gán permission key cho role; nếu role được gán key thì vẫn xóa được dù không phải super.
- Toàn bộ action `virtual` (có thể override ở controller con) — pattern Sandbox/Area generated.
- Trùng tên với ConfigSystemController module khác → node suffix `_OrderLogistic`; route khác hẳn (`api/QuanTriArea/...`).
