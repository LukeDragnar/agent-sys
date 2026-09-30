---
controller: CaLamViecNhanVienNangCaoController
module: nhan-su
service: Branch
man_hinh_ui: "Chấm công > Phân ca nâng cao (tạo ca theo nhóm cơ bản/nâng cao, theo 1 NV, xem dạng danh sách/lịch calendar)"
muc_dich: "Quản lý ca làm việc nâng cao: tạo ca hàng loạt (cơ bản/nâng cao) hoặc đơn lẻ 1 NV-1 khung giờ, xóa theo nhóm/theo id, tìm kiếm theo điều kiện và theo dạng lịch calendar."
alias: [CaLamViecNhanVienNangCao, "phân ca nâng cao", "ca nâng cao", "xếp ca nâng cao"]
auth: mixed
state_machine: []
deprecated: false
entities: [CaLamViecNhanVien]
stored_procs: []
depends: [KhungGioNhanVien, NhanVien/User (Core)]
cross_module_in: []
cross_module_out: []
---

# CaLamViecNhanVienNangCao — Phân ca làm việc nâng cao

> Không có `[SBAuthorize]` controller-level; mỗi action tự khai báo. Action create/delete để key SBAuthorize **commented** → fallback `[SBAuthorize()]` login-only; chỉ 2 action tìm kiếm có key thật. TienIch `TI_ChamCong`.

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tạo ca hàng loạt (cơ bản) | Tạo nhanh | POST `CreateBasic` | (key commented → login-only) | public | — | Validate → tạo ca theo cấu hình cơ bản cho nhiều NV/khung giờ. | EF | Key commented → login-only |
| 2 | Tạo ca hàng loạt (nâng cao) | Tạo nâng cao | POST `CreateAdvanced` | (key commented → login-only) | public | — | Validate → tạo ca theo cấu hình nâng cao. | EF | Key commented → login-only |
| 3 | Tạo ca đơn lẻ | Thêm ca | POST `CreateSingle` | (key commented → login-only) | public | — | Tạo 1 ca: 1 NV × 1 khung giờ. | EF | Key commented → login-only |
| 4 | Xóa ca theo nhóm (cơ bản) | Xóa | POST `DeleteBasic` | (key commented → login-only) | public | — | Xóa các ca theo điều kiện nhóm cơ bản. | EF | Key commented → login-only |
| 5 | Xóa ca theo id | Xóa | POST `DeleteById/{id}` | (key commented → login-only) | public | — | Xóa 1 ca theo id. | EF | Key commented → login-only |
| 6 | Tìm ca theo điều kiện (list) | Danh sách | POST `TimTheoDieuKien` | `CaLamViecNhanVienNangCao_TimTheoDieuKien` | public | — | Validate → trả ca theo bộ lọc (dạng danh sách). | EF | — |
| 7 | Tìm ca theo lịch (calendar) | Lịch | POST `TimTheoDieuKienTypeCalendar` | `CaLamViecNhanVienNangCao_TimTheoDieuKienTypeCalendar` | public | — | Trả ca dạng calendar theo bộ lọc. | EF | — |

## Quan hệ
- **Gọi sang:** Core/User (NV), KhungGioNhanVien.
- **Dữ liệu (entities):** `CaLamViecNhanVien` (cùng bảng phân ca, tạo theo lô nâng cao).
- **Ripple/đồng bộ:** không phát event; dữ liệu ca là đầu vào cho chấm công.

## Lỗi đã biết / lưu ý kỹ thuật
- `CaLamViecNhanVienNangCaoController.cs:15` — không có `[SBAuthorize]` controller-level; 5/7 action có key bị comment → toàn bộ create/delete chỉ login-only, mất phân quyền role.
