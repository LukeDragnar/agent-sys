---
controller: NhomTienIchController (SanboxArea / QuanTriArea)
module: he-thong
service: Core
man_hinh_ui: "Quản trị > Quản lý nhóm tiện ích (route api/QuanTriArea/NhomTienIch): lưới nhóm tiện ích + nút Thêm/Sửa/Xóa/Xem chi tiết"
muc_dich: "CRUD danh mục Nhóm tiện ích (gom các tiện ích/chức năng hệ thống thành nhóm để phân quyền & cấu hình gói)."
alias: ["nhóm tiện ích quản trị", "quản lý nhóm tiện ích", "NhomTienIch QuanTriArea"]
auth: public
state_machine: []
deprecated: false
entities: [NhomTienIch]
stored_procs: []
depends: ["INhomTienIchService", "BaseController", "AutoMapper"]
cross_module_in: ["FE Quản trị/Nhóm tiện ích gọi TimTheoDieuKien/ThemMoi/CapNhatThongTin/Xoa/TimTheoId"]
cross_module_out: ["Nhóm tiện ích là khung gom tiện ích → ảnh hưởng cấu trúc phân quyền (Permision) và danh mục gói sử dụng"]
---

# NhomTienIchController (QuanTriArea) — Quản lý nhóm tiện ích

Bản QuanTriArea (partial class), kế thừa `BaseController`. CRUD đầy đủ với validate + map DTO↔entity. Controller-level `[SBAuthorize]` + `[TienIch(TI_HeThong)]`; từng method gắn key (HT-AD, HT-KT).

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | (load lưới) | POST `api/QuanTriArea/NhomTienIch/TimTheoDieuKien` | `NhomTienIch_TimTheoDieuKien` (HT-AD, HT-KT) | public | — | Lọc nhóm tiện ích theo `NhomTienIchFilterDto`. | EF | — |
| 2 | Xem chi tiết | (mở form) | GET `api/QuanTriArea/NhomTienIch/TimTheoId/{id:Guid}` | `NhomTienIch_TimTheoId` (HT-AD, HT-KT) | public | — | `GetByIdAsync(id)` → map sang DTO trả về. | EF | — |
| 3 | Thêm mới | Thêm | POST `api/QuanTriArea/NhomTienIch/ThemMoi` | `NhomTienIch_ThemMoi` (HT-AD, HT-KT) | public | — | ModelState valid → map DTO→entity → `InsertAsync`. | EF | — |
| 4 | Cập nhật | Sửa | POST `api/QuanTriArea/NhomTienIch/CapNhatThongTin` | `NhomTienIch_CapNhatThongTin` (HT-AD, HT-KT) | public | — | Valid → kiểm tra tồn tại (GetById) → `UpdateAsync`. | EF | — |
| 5 | Xóa | Xóa | POST `api/QuanTriArea/NhomTienIch/Xoa/{id:Guid}` | `NhomTienIch_Xoa` (HT-AD, HT-KT) | public | — | Kiểm tra tồn tại → `DeleteAsync`. | EF | — |

## Quan hệ
- **Gọi sang:** INhomTienIchService (EF), AutoMapper NhomTienIchDto↔NhomTienIch.
- **Dữ liệu (entities):** `NhomTienIch`.
- **Ripple / đồng bộ:** Sửa/xóa nhóm tiện ích → ảnh hưởng cấu trúc phân quyền & danh mục gói.

## Lỗi đã biết / lưu ý kỹ thuật
- Khác bản `Controllers/NhomTienIchController` (chỉ `LayTatCa` đọc): CRUD chỉ có ở bản QuanTriArea này.
