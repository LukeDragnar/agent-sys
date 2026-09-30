---
controller: NganhNgheController (SanboxArea / QuanTriArea)
module: he-thong
service: Core
man_hinh_ui: "Quản trị > Quản lý ngành nghề (route api/QuanTriArea/NganhNghe): lưới ngành nghề + nút Thêm/Sửa/Xóa/Xem chi tiết"
muc_dich: "CRUD danh mục Ngành nghề (lĩnh vực kinh doanh của đơn vị). Cung cấp danh sách ngành nghề cho cấu hình đơn vị và khởi tạo dữ liệu mẫu theo ngành nghề."
alias: ["ngành nghề", "danh mục ngành nghề", "lĩnh vực kinh doanh", "NganhNghe"]
auth: mixed
state_machine: []
deprecated: false
entities: [NganhNghe]
stored_procs: []
depends: ["INganhNgheService", "SBBaseController<NganhNghe, NganhNgheDto>"]
cross_module_in: ["FE Quản trị/Ngành nghề gọi TimTheoDieuKien/ThemMoi/CapNhatThongTin/Xoa/TimTheoId", "Service nội bộ gọi LayTatCaNganhNghe (PrivateSBAuthorize) để build dropdown"]
cross_module_out: ["Ngành nghề là gốc khởi tạo dữ liệu mẫu (nhóm nhân viên, cấu hình) cho đơn vị mới theo ngành"]
---

# NganhNgheController — Danh mục ngành nghề

CRUD ngành nghề kế thừa `SBBaseController` (override `TimTheoId`/`ThemMoi`/`CapNhatThongTin`/`XoaAsync` chỉ để gắn key). Thêm `TimTheoDieuKien` (search) và `LayTatCaNganhNghe` (private). Controller-level `[TienIch(TI_HeThong)]`, từng method gắn key (HT-AD) → auth = mixed.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện | (load lưới) | POST `api/QuanTriArea/NganhNghe/TimTheoDieuKien` | `NganhNghe_TimTheoDieuKien` (HT-AD) | public | — | Lọc ngành nghề theo `NganhNgheFilterDto`. | EF | — |
| 2 | Xem chi tiết | (mở form) | GET `api/QuanTriArea/NganhNghe/TimTheoId/{id:Guid}` | `NganhNghe_TimTheoId` (HT-AD) | public | — | `base.TimTheoId(id)` trả 1 ngành nghề. | EF | — |
| 3 | Thêm mới | Thêm | POST `api/QuanTriArea/NganhNghe/ThemMoi` | `NganhNghe_ThemMoi` (HT-AD) | public | — | `base.ThemMoi(model)` insert ngành nghề. | EF | — |
| 4 | Cập nhật | Sửa | POST `api/QuanTriArea/NganhNghe/CapNhatThongTin` | `NganhNghe_CapNhatThongTin` (HT-AD) | public | — | `base.CapNhatThongTin(model)` update. | EF | — |
| 5 | Xóa | Xóa | POST `api/QuanTriArea/NganhNghe/Xoa/{id:Guid}` | `NganhNghe_Xoa` (HT-AD) | public | — | `base.XoaAsync(id)` xóa ngành nghề. | EF | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] **GET `LayTatCaNganhNghe`** `[PrivateSBAuthorize]` — service khác gọi để lấy toàn bộ ngành nghề (build dropdown / khởi tạo dữ liệu mẫu) không cần token user.

## Quan hệ
- **Gọi sang:** INganhNgheService (EF).
- **Dữ liệu (entities):** `NganhNghe`.
- **Ripple / đồng bộ:** Danh mục nền cho ngành nghề đơn vị; dùng khi InitStaffGroupSystem (NhomNhanVien) tạo dữ liệu mẫu theo ngành.

## Lỗi đã biết / lưu ý kỹ thuật
- Chỉ tồn tại 1 file ở `Areas/SanboxArea/` (route `api/QuanTriArea/NganhNghe`), không có bản `Controllers/`.
