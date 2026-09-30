---
controller: ConfigSystemController
module: website
service: Website
man_hinh_ui: "nội bộ — Area QuanTri/SanboxArea (TienIch ẩn, cờ `true`): màn quản trị cấu hình hệ thống của service Website (CRUD config theo mã/nhóm), chỉ super-admin"
muc_dich: "API quản trị bảng ConfigSystem riêng của service Website: CRUD cấu hình (mã/tên/nhóm/giá trị) + lấy theo mã / theo nhóm / dạng cây sort. Dùng cho cấu hình vận hành website, không phải nghiệp vụ khách hàng."
alias: ["config system website", "cấu hình hệ thống website", "config website", "ConfigSystem QuanTri"]
auth: internal
state_machine: []
deprecated: false
entities: [ConfigSystem]
stored_procs: []
depends: [Website/IConfigSystemService, Authen (CurrentUser)]
cross_module_in: []
cross_module_out: []
---

# ConfigSystemController (Website) — Quản trị cấu hình hệ thống service Website

> API controller (`[ApiController]`) trong **Area SandBoxArea**, route prefix `api/QuanTriArea/ConfigSystem`. `[TienIch("ConfigSystemController_Website", ..., MaTienIchSCRM.TI_HeThong, true)]` — cờ `true` = tiện ích **nội bộ/ẩn** (quản trị hệ thống). `[SBAuthorize]` ở controller-level + permission key `ConfigSystem_Website_*` per-action. **Trùng tên** với `ConfigSystemController` ở các service khác (he-thong...) → file dùng suffix `_Website`. Quyền key đều mang tiền tố riêng `ConfigSystem_Website_` tách biệt bản service khác.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy config theo mã | (load 1 cấu hình) | GET `api/QuanTriArea/ConfigSystem/TimTheoMa?ma=` | `ConfigSystem_Website_TimTheoMa` | internal | - | Trả config theo `Ma` | EF | - |
| 2 | Lấy tất cả config | grid list config | GET `.../LayTatCa` | `ConfigSystem_Website_LayTatCa` | internal | - | Trả toàn bộ config | EF | - |
| 3 | Lấy theo group + sort | view cây/nhóm | POST `.../LayTatCaByGroupSort` (FilterCustomDto) | `ConfigSystem_Website_LayTatCaTheoGroupVaSort` | internal | - | Trả config dạng cây, sắp xếp theo group + sort | EF | - |
| 4 | Lấy theo group | view nhóm | GET `.../LayTatCaByGroup` | `ConfigSystem_Website_LayTatCaTheoGroup` | internal | - | Trả config gom theo group | EF | - |
| 5 | Lấy danh sách group | dropdown nhóm cấu hình | GET `.../LayCacNhomCauHinh` | `ConfigSystem_Website_LayCacGroupConfigSystem` | internal | - | Trả danh sách GroupCode | EF | - |
| 6 | Tìm theo id | mở chi tiết/sửa | GET `.../TimTheoId/{id}` | `ConfigSystem_Website_TimTheoId` | internal | per-row | Trả ConfigSystemDto theo id (map Entity→Dto) | EF | - |
| 7 | Thêm mới | "Thêm mới" | POST `.../ThemMoi` (ConfigSystemDto) | `ConfigSystem_Website_ThemMoi` | internal | - | Validate ModelState → map Dto→Entity, set NgayTao/NguoiTao/Id=NewGuid → Insert | EF | - |
| 8 | Cập nhật | "Lưu" | POST `.../CapNhatThongTin` (ConfigSystemDto) | `ConfigSystem_Website_CapNhatThongTin` | internal | per-row | Validate ModelState → check tồn tại theo Id (404 nếu null) → set NguoiCapNhat/NgayCapNhat → Update | EF | - |
| 9 | Xóa | "Xóa" (chỉ super-user) | POST `.../Xoa/{id}` | `ConfigSystem_Website_XoaConfigSystem` | internal | super-user | Check tồn tại → Delete. Mô tả quyền ghi rõ "ONLY_SUPERUSER" | EF | gating super-user thực thi ở tầng quyền/role, không thấy guard trong controller |

## Quan hệ
- **Gọi sang:** `IConfigSystemService` (TimTheoMa, GetAll, GetLikeTreeSort, GetLikeTree, GetByIdAsync, Insert, Update, Delete, GetGroupCode).
- **Dữ liệu (entities):** `ConfigSystem` (Ma, Ten, GroupCode, KieuLoai, MoTa, Value, SuDung) — cấu hình vận hành riêng của service Website.
- **Ripple / đồng bộ:** config được đọc bởi logic nội bộ Website (theo Ma/GroupCode). Không cross-service.

## Lỗi đã biết / lưu ý kỹ thuật
- **Trùng tên controller** (`ConfigSystemController` tồn tại ở nhiều service) → file node dùng suffix `_Website`; định danh thật phân biệt qua route Area `api/QuanTriArea/...` + permission key tiền tố `ConfigSystem_Website_` + TienIch `ConfigSystemController_Website`.
- Endpoint cũ `GetConfigSystem/{code}` đã comment out (ConfigSystemController.cs:127-133).
- `Xoa` ghi chú "ONLY_SUPERUSER" trong description quyền nhưng **không có guard role super-user trong code controller** — phụ thuộc hoàn toàn cấu hình permission key ở tầng auth; cần xác nhận key này chỉ gán super-user.
- TienIch cờ `true` (nội bộ/ẩn) → AI/registry coi đây là màn quản trị hệ thống, không phải tính năng end-user.
